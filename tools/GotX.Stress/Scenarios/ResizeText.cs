namespace GotX.Stress;

static class ResizeScenario
{
    static readonly float[] Dpis = [1f, 1.25f, 1.5f, 2f, 1f];

    public static Dictionary<string, double>? Run(GotX.Text.TextSystem text)
    {
        var rig = new Rig(text);
        var win = rig.Add(a => a.New(new MixedPage()), new Size(1000, 700));
        var m = Scenarios.Begin(rig, win);
        // Sinusoidal sweep so widths cross every breakpoint; DPI changes every 25 frames (forces full rebuild of caches keyed by scale).
        var trace = Environment.GetEnvironmentVariable("STRESS_RESIZE_TRACE") == "1";
        for (int f = 0; f < 300; f++)
        {
            double t = f / 300.0 * Math.PI * 4;
            var s = rig.Frame(win, () =>
            {
                win.Size = new Size((float)(960 + 640 * Math.Sin(t)), (float)(700 + 300 * Math.Cos(t * 1.7)));
                win.Scale = Dpis[f / 25 % Dpis.Length];
            });
            m.Add(s);
            if (trace) Console.WriteLine($"    f{f,3} {s.Total,6:0.00} ms (layout {s.Layout:0.00} paint {s.Paint:0.00} raster {s.Raster:0.00}) alloc {s.Alloc / 1024} KB prims {s.Prims} dpi {win.Scale} {win.Size}");
        }
        return m.Finish();
    }
}

/// <summary>Pathological text: very long wrapped lines, one huge unbreakable word, emoji ZWJ/flags, RTL, Vietnamese precomposed + decomposed + stacked marks.</summary>
public sealed class TextPage : View
{
    public static readonly string[] Blocks =
    [
        string.Concat(Enumerable.Repeat("The quick brown fox jumps over the lazy dog, again and again. ", 330)), // ~20k chars
        new string('W', 5000), // no break opportunity
        string.Concat(Enumerable.Repeat("😀👨‍👩‍👧‍👦🇻🇳❤️🏳️‍🌈👍🏽 ", 200)),
        string.Concat(Enumerable.Repeat("مرحبا بالعالم هذا نص طويل للاختبار والتفاف الأسطر ", 80)),
        string.Concat(Enumerable.Repeat("שלום עולם זהו טקסט ארוך לבדיקה mixed with English 123 ", 80)),
        string.Concat(Enumerable.Repeat("Tiếng Việt có dấu: Nguyễn Thị Thu Hương, đường Điện Biên Phủ. ", 120)), // precomposed
        string.Concat(Enumerable.Repeat("Tiếng Việt có dấu: Nguyễn Thị Thu Huơng. ", 120)), // decomposed
        string.Concat(Enumerable.Repeat("à́̂̃̄̅̆̇ ë̉̊̋ ", 300)), // stacked combining
    ];

    GotX.Widgets.TextInput _input = null!;

    protected override void OnCreated()
    {
        _input = App.New(new GotX.Widgets.TextInput { Multiline = true, Text = string.Concat(Enumerable.Repeat("long multiline editable line with ñ and Việt\n", 400)), BoxClass = "h-48 w-full px-3 border rounded-md" });
    }

    public override Div Render(Window w)
    {
        var col = w.Div().Class("flex flex-col gap-4 p-4");
        for (int i = 0; i < Blocks.Length; i++)
        {
            col.Child(w.Div().Class("text-sm leading-6 text-foreground").Child(Blocks[i]));
            if (i % 2 == 0) col.Child(w.Div().Class("w-40 text-xs truncate border").Child(Blocks[i]));
        }
        col.Child(w.InputArea(_input));
        return w.Div().Class("size-full overflow-y-auto bg-background text-foreground").Key("text-scroll").Child(col);
    }
}

static class TextScenario
{
    public static Dictionary<string, double>? Run(GotX.Text.TextSystem text)
    {
        var rig = new Rig(text);
        var win = rig.Add(a => a.New(new TextPage()), new Size(900, 700));
        // Unmeasured pass over the whole page (several widths, scroll to the bottom and back): scene / shape / damage arrays reach their final size
        // before measuring (long-text culling makes the scene size depend on the scroll position, so growth would otherwise land inside the window).
        rig.Settle(win);
        var wp = rig.ScrollTarget(win);
        for (int f = 0; f < 4; f++) rig.Frame(win, () => win.Size = new Size(280 + f * 300, 700));
        for (int f = 0; f < 40; f++) rig.Send(win, new InputEvent(InputKind.Scroll, wp, Delta: new Point(0, 400)));
        for (int f = 0; f < 40; f++) rig.Send(win, new InputEvent(InputKind.Scroll, wp, Delta: new Point(0, -400)));
        rig.Frame(win, () => win.Size = new Size(900, 700));
        var m = Scenarios.Begin(rig, win, 2);
        var rng = new Random(7);
        double sweepPaint = 0, sweepLayout = 0, sweepTotal = 0; var c0 = PerfCounters.Snapshot.Now(); long sh0 = text.ShapeHits, sm0 = text.ShapeMisses;
        for (int f = 0; f < 160; f++) // new width every frame: mostly re-wrap/re-shape cache misses
        {
            var s = rig.Frame(win, () => win.Size = new Size(280 + (f * 53) % 1200, 700));
            m.Add(s); sweepPaint += s.Paint; sweepLayout += s.Layout; sweepTotal += s.Total;
        }
        // Counters of the 160 re-wrap frames only (the later phases would dilute them).
        var d = PerfCounters.Snapshot.Now() - c0;
        m.Set("wrapMissPerFrame", d.WrapMiss / 160.0); m.Set("wrapMsPerFrame", PerfCounters.TicksToMs(d.WrapTicks) / 160);
        m.Set("sweepPaintMs", sweepPaint / 160); m.Set("sweepLayoutMs", sweepLayout / 160); m.Set("sweepTotalMs", sweepTotal / 160);
        m.Set("shapeMissPerFrame", (text.ShapeMisses - sm0) / 160.0); m.Set("shapeHitPerFrame", (text.ShapeHits - sh0) / 160.0);
        var p = rig.ScrollTarget(win);
        for (int f = 0; f < 80; f++) m.Add(rig.Send(win, new InputEvent(InputKind.Scroll, p, Delta: new Point(0, 160))));
        // Focus the multiline input (scroll it into view first), then type mixed-script text into 400 lines of content.
        for (int f = 0; f < 120; f++) m.Add(rig.Send(win, new InputEvent(InputKind.Scroll, p, Delta: new Point(0, 400))));
        for (int i = 0; i < win.Window.HitboxCount; i++)
            if (win.Window.HitboxIsInput(i) && win.Window.HitboxBounds(i) is { Width: > 8, Height: > 8 })
            {
                var c = rig.Center(win, i);
                m.Add(rig.Send(win, new InputEvent(InputKind.MouseDown, c))); m.Add(rig.Send(win, new InputEvent(InputKind.MouseUp, c)));
                break;
            }
        string[] chars = ["é", "😀", "ب", "ố"];
        for (int f = 0; f < 60; f++) m.Add(rig.Send(win, new InputEvent(InputKind.TextInput, Text: chars[rng.Next(chars.Length)])));
        return m.Finish();
    }
}
