using GotX;
using GotX.Raster;
using GotX.Test;

/// <summary>
/// Chữ định vị 1/4 pixel (Window.SubpixelText, mặc định tắt): tắt thì pixel y hệt mặc định; bật thì xác định (hai lần dựng giống nhau),
/// khác tắt, và gần ảnh chuẩn (dựng ở DPI 4 rồi thu nhỏ hộp 4x4) hơn chữ làm tròn pixel nguyên (sai số bình phương trung bình thấp hơn).
/// </summary>
static class SubpixelTextTests
{
    const string Line = "Subpixel positioning: 11 illicit flim-flam, Việt Nam 0123456789 wiwiwi";

    sealed class Page : View
    {
        public override Div Render(Window w) =>
            w.Div().Class("size-full bg-background p-2 text-foreground").Child(w.Div().Class("text-xs").Child(Line)).Child(w.Div().Class("text-[13px] mt-1").Child(Line))
                .Child(w.Div().Class("text-sm mt-1").Child(Line)).Child(w.Div().Class("text-sm mt-1 font-medium tracking-tight").Child(Line));
    }

    static uint[] Render(bool? subpixel, float scale, out int w, out int h)
    {
        double now = 1000;
        var app = new App { Clock = () => now };
        var win = new Window(app, TestFonts.Shared) { Clock = () => now, Root = app.New(new Page()) };
        if (subpixel is { } s) win.SubpixelText = s;
        var size = new Size(480, 96);
        win.Draw(size, scale); app.Flush();
        var scene = win.Draw(size, scale);
        w = (int)(size.Width * scale); h = (int)(size.Height * scale);
        var c = new Canvas(); c.Resize(w, h);
        SoftwareRenderer.Render(scene, c);
        return c.Pixels.AsSpan(0, w * h).ToArray();
    }

    // Độ sáng (kênh G) của ảnh chuẩn thu nhỏ hộp 4x4 về cỡ ảnh thử.
    static double Mse(uint[] img, uint[] hi, int w, int h)
    {
        double sum = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                double r = 0;
                for (int j = 0; j < 4; j++) for (int i = 0; i < 4; i++) r += (hi[(y * 4 + j) * w * 4 + x * 4 + i] >> 8) & 0xFF;
                double d = ((img[y * w + x] >> 8) & 0xFF) - r / 16;
                sum += d * d;
            }
        return sum / (w * h);
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("perf/subpixel-text/off-is-default-and-unchanged", t =>
        {
            var a = Render(null, 1, out _, out _); var b = Render(false, 1, out _, out _);
            t.Assert.True(a.AsSpan().SequenceEqual(b), "SubpixelText=false phải giống mặc định từng pixel");
        });
        suite.Add("perf/subpixel-text/on-is-deterministic-and-closer-to-reference", t =>
        {
            var on1 = Render(true, 1, out int w, out int h); var on2 = Render(true, 1, out _, out _);
            var off = Render(false, 1, out _, out _);
            t.Assert.True(on1.AsSpan().SequenceEqual(on2), "bật: hai lần dựng phải giống nhau");
            t.Assert.True(!on1.AsSpan().SequenceEqual(off), "bật phải khác tắt");
            var hi = Render(false, 4, out _, out _);
            double mOn = Mse(on1, hi, w, h), mOff = Mse(off, hi, w, h);
            t.Notes.Add($"MSE so với ảnh chuẩn DPI4: tắt {mOff:0.0}, bật {mOn:0.0}");
            t.Assert.True(mOn < mOff, $"bật phải gần ảnh chuẩn hơn: {mOn:0.0} so với {mOff:0.0}");
        });
    }
}
