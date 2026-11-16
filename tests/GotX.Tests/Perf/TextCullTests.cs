using GotX;
using GotX.Raster;
using GotX.Test;

/// <summary>
/// Chữ dài (>= 48 dòng) bỏ các dòng ngoài vùng cắt (Window.VisibleLines): mỗi bước (cuộn ngoài, cuộn trong khối cuộn lồng, đổi cỡ cửa sổ, đổi DPI)
/// canvas phải GIỐNG TỪNG PIXEL với cửa sổ tắt cắt (CullViews = false), và cắt phải thật sự bớt primitive.
/// </summary>
static class TextCullTests
{
    static readonly string Long = string.Concat(Enumerable.Repeat("The quick brown fox jumps over the lazy dog, Việt Nam đường Điện Biên Phủ. ", 120));

    sealed class Page : View
    {
        View[] _tail = [];
        sealed class Tail(string s) : View { public override Div Render(Window w) => w.Div().Class("p-3 text-xs leading-4").Child(s); }
        protected override void OnCreated() => _tail = [App.New(new Tail(Long + "A")), App.New(new Tail(Long + "B"))];
        public override Div Render(Window w) =>
            w.Div().Class("size-full overflow-y-auto bg-background text-foreground").Key("outer")
                .Child(w.Div().Class("sticky top-0 bg-card border-b px-3 py-2").Child("Header"))
                .Child(w.View(_tail[0]))
                .Child(w.Div().Class("p-3 text-sm leading-6 underline").Child(Long))
                .Child(w.Div().Class("h-40 m-3 overflow-y-auto rounded-md border p-2 text-[13px] leading-5").Key("inner").Child(Long + "!"))
                .Child(w.Div().Class("p-3 text-base leading-5 tracking-wide").Child(Long + "?"))
                .Child(w.View(_tail[1]));
    }

    static (int Bad, long PrimsOn, long PrimsOff, int Replayed) Run(float scale, int seed)
    {
        double now = 1000;
        var app = new App { Clock = () => now };
        var on = new Window(app, TestFonts.Shared) { Clock = () => now, CullViews = true, Root = app.New(new Page()) };
        var off = new Window(app, TestFonts.Shared) { Clock = () => now, CullViews = false, Root = app.New(new Page()) };
        var rng = new Random(seed);
        var size = new Size(640, 480);
        var a = new Canvas(); var b = new Canvas();
        int bad = 0, replayed = 0; long pOn = 0, pOff = 0;
        for (int i = 0; i < 80; i++)
        {
            now += 0.016;
            if (i > 1)
            {
                var pos = new Point(rng.Next(640), rng.Next(480));
                var ev = rng.Next(8) == 0 ? new InputEvent(InputKind.MouseMove, pos)
                    : new InputEvent(InputKind.Scroll, pos, Delta: new Point(0, rng.Next(-4, 9) * 24 + rng.Next(-3, 4)));
                on.DispatchInput(ev); off.DispatchInput(ev);
                if (i % 17 == 0) size = new Size(400 + rng.Next(400), 400 + rng.Next(200));
            }
            app.RunTimers();
            var s1 = on.Draw(size, scale); var s2 = off.Draw(size, scale);
            app.Flush();
            int w = (int)(size.Width * scale), h = (int)(size.Height * scale);
            a.Resize(w, h); b.Resize(w, h);
            SoftwareRenderer.Render(s1, a); SoftwareRenderer.Render(s2, b);
            if (!a.Pixels.AsSpan(0, w * h).SequenceEqual(b.Pixels.AsSpan(0, w * h))) bad++;
            pOn += s1.Count; pOff += s2.Count; replayed += on.LastFrame.ReplayedViews;
        }
        return (bad, pOn, pOff, replayed);
    }

    public static void Register(TestSuite suite)
    {
        foreach (float scale in new[] { 1f, 1.5f })
            suite.Add($"perf/text-cull/parity-dpi-{scale}", t =>
            {
                int bad = 0, rep = 0; long on = 0, off = 0;
                for (int seed = 1; seed <= 3; seed++) { var r = Run(scale, seed); bad += r.Bad; on += r.PrimsOn; off += r.PrimsOff; rep += r.Replayed; }
                t.Notes.Add($"view phát lại: {rep}");
                t.Assert.Equal(bad, 0, $"{bad} frame cắt dòng khác pixel với không cắt (dpi {scale})");
                t.Assert.True(on * 3 < off, $"cắt dòng phải bớt primitive rõ rệt: {on} so với {off}");
            });
    }
}
