using GotX;
using GotX.Raster;
using GotX.Test;

/// <summary>
/// Cuộn bằng blit (SceneDamage.TryShift + RenderDamaged) phải cho ĐÚNG TỪNG PIXEL như vẽ cả frame: trang có vùng cuộn lồng nhau (bo góc + cắt trong),
/// tiêu đề dính, thanh cuộn, hover đổi; wheel ngẫu nhiên (seed cố định) ở nhiều DPI, so canvas giữ lại với raster đầy đủ sau mỗi bước.
/// Cũng kiểm tra loại view ngoài tầm nhìn (Window.CullViews) không đổi pixel so với tắt.
/// </summary>
static class ScrollBlitTests
{
    sealed class Row(int i) : View
    {
        public override Div Render(Window w) =>
            w.Div().Class("flex items-center gap-2 px-3 py-2 border-b text-sm hover:bg-muted").Child(w.Div().Class("size-3 rounded-full bg-primary"))
                .Child(w.Div().Class("truncate").Child($"Row {i} - lorem ipsum dolor sit amet"));
    }

    sealed class Page : View
    {
        View[] _rows = [];
        protected override void OnCreated() => _rows = Enumerable.Range(0, 120).Select(i => (View)App.New(new Row(i))).ToArray();
        public override Div Render(Window w)
        {
            var inner = w.Div().Class("h-64 overflow-y-auto rounded-lg border m-3").Key("inner");
            for (int i = 0; i < 80; i++) inner.Child(w.Div().Class("px-3 py-1 text-sm border-b truncate hover:bg-accent").Child("Inner " + i));
            var outer = w.Div().Class("size-full overflow-y-auto bg-background").Key("outer")
                .Child(w.Div().Class("sticky top-0 bg-card border-b px-3 py-2 font-medium").Child("Sticky header"))
                .Child(inner);
            foreach (var r in _rows) outer.Child(w.View(r));
            return outer;
        }
    }

    static int Run(float scale, int seed, int steps, bool blit, bool cull, out int shifted)
    {
        double now = 1000;
        var app = new App { Clock = () => now };
        var win = new Window(app, TestFonts.Shared) { Clock = () => now, ScrollBlit = blit, CullViews = cull };
        win.Root = app.New(new Page());
        var size = new Size(640, 480);
        var dmg = new SceneDamage { AcceptShift = true, ShiftPrimCostPx = 0 };
        var canvas = new Canvas(); var full = new Canvas();
        var rng = new Random(seed);
        int bad = 0; shifted = 0;
        for (int i = 0; i < steps; i++)
        {
            now += 0.016;
            if (i > 1)
            {
                var pos = new Point(rng.Next(640), rng.Next(480));
                win.DispatchInput(rng.Next(6) == 0 ? new InputEvent(InputKind.MouseMove, pos)
                    : new InputEvent(InputKind.Scroll, pos, Delta: new Point(0, rng.Next(-5, 6) * 24 + rng.Next(-3, 4))));
            }
            app.RunTimers();
            var scene = win.Draw(size, scale);
            app.Flush();
            int w = (int)(size.Width * scale), h = (int)(size.Height * scale);
            canvas.Resize(w, h); full.Resize(w, h);
            var dr = dmg.Update(scene, w, h);
            if (dr.HasShift) shifted++;
            SoftwareRenderer.RenderDamaged(scene, canvas, dr);
            SoftwareRenderer.Render(scene, full);
            if (!full.Pixels.AsSpan(0, w * h).SequenceEqual(canvas.Pixels.AsSpan(0, w * h))) bad++;
        }
        return bad;
    }

    public static void Register(TestSuite suite)
    {
        foreach (float scale in new[] { 1f, 1.25f, 1.5f })
            suite.Add($"perf/scrollblit/parity-dpi-{scale}", t =>
            {
                int shifts = 0, bad = 0;
                for (int seed = 1; seed <= 3; seed++) { bad += Run(scale, seed, 60, blit: true, cull: true, out int s); shifts += s; }
                t.Assert.Equal(bad, 0, $"{bad} frame blit khác raster đầy đủ (dpi {scale})");
                t.Assert.True(shifts > 0, "không có frame nào dùng blit: bài test không đo gì");
            });
        suite.Add("perf/scrollblit/cull-parity", t =>
        {
            int bad = Run(1f, 7, 60, blit: false, cull: true, out _) + Run(1.5f, 8, 60, blit: false, cull: true, out _);
            t.Assert.Equal(bad, 0, $"{bad} frame loại view ngoài tầm nhìn khác raster đầy đủ");
        });
    }
}
