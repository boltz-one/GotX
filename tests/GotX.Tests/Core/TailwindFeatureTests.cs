using GotX;
using GotX.Test;

/// <summary>
/// Utility thêm cho độ trung thực với shadcn: gradient (so pixel renderer CPU với công thức CSS tính độc lập ngay trong test),
/// line-clamp, text-balance/pretty, tabular-nums, aspect, vị trí grid, breakpoint tuỳ ý, calc(). Phần chữ/layout: file .Layout.cs.
/// </summary>
static partial class TailwindFeatureTests
{
    static Div Box(Window w, string key, string classes) => w.Div().Key(key).Class(classes);

    /// <summary>Màu pixel tại cột / hàng của hộp (toạ độ trong hộp, tâm pixel).</summary>
    static Rgba At(UiTest t, string key, float x, float y) { var b = t.Box(key); return t.PixelAt(new Point(b.X + x, b.Y + y)); }

    static bool Close(Rgba a, (float R, float G, float B) e, float tol255 = 2.5f) =>
        MathF.Abs(a.R - e.R) * 255 <= tol255 && MathF.Abs(a.G - e.G) * 255 <= tol255 && MathF.Abs(a.B - e.B) * 255 <= tol255;

    static string Show(Rgba a) => $"({Fmt.N(a.R * 255)}, {Fmt.N(a.G * 255)}, {Fmt.N(a.B * 255)})";
    static string Show((float R, float G, float B) e) => $"({Fmt.N(e.R * 255)}, {Fmt.N(e.G * 255)}, {Fmt.N(e.B * 255)})";

    // ---- Oklab viết lại độc lập (Björn Ottosson) để làm kỳ vọng, không gọi GradientMath của Core ----
    static float Lin(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
    static float Gam(float c) { c = Math.Clamp(c, 0, 1); return c <= 0.0031308f ? 12.92f * c : 1.055f * MathF.Pow(c, 1 / 2.4f) - 0.055f; }

    static (float L, float A, float B) Lab(float r, float g, float b)
    {
        r = Lin(r); g = Lin(g); b = Lin(b);
        float l = MathF.Cbrt(0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b);
        float m = MathF.Cbrt(0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b);
        float s = MathF.Cbrt(0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b);
        return (0.2104542553f * l + 0.7936177850f * m - 0.0040720468f * s, 1.9779984951f * l - 2.4285922050f * m + 0.4505937099f * s,
            0.0259040371f * l + 0.7827717662f * m - 0.8086757660f * s);
    }

    static (float R, float G, float B) Rgb((float L, float A, float B) c)
    {
        float l = c.L + 0.3963377774f * c.A + 0.2158037573f * c.B, m = c.L - 0.1055613458f * c.A - 0.0638541728f * c.B;
        float s = c.L - 0.0894841775f * c.A - 1.2914855480f * c.B;
        l = l * l * l; m = m * m * m; s = s * s * s;
        return (Gam(4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s), Gam(-1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s),
            Gam(-0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s));
    }

    /// <summary>Đỏ → xanh dương tại t, nội suy Oklab (mặc định của bg-linear-to-r trong Tailwind v4).</summary>
    static (float, float, float) RedBlueOklab(float t)
    {
        var a = Lab(1, 0, 0); var b = Lab(0, 0, 1);
        return Rgb((a.L + (b.L - a.L) * t, a.A + (b.A - a.A) * t, a.B + (b.B - a.B) * t));
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("core/tw/gradient-linear-oklab-vs-srgb", t =>
        {
            t.Open(new Fx(w => Fx.Page(w)
                .Child(Box(w, "ok", "w-[200px] h-10 bg-linear-to-r from-[#ff0000] to-[#0000ff]"))
                .Child(Box(w, "srgb", "w-[200px] h-10 bg-linear-to-r/srgb from-[#ff0000] to-[#0000ff]"))));
            foreach (int col in (int[])[0, 37, 99, 150, 199])
            {
                float tt = (col + 0.5f) / 200;
                var ok = At(t, "ok", col + 0.5f, 20); var expOk = RedBlueOklab(tt);
                t.Assert.True(Close(ok, expOk), $"oklab cột {col}: {Show(ok)} ≈ {Show(expOk)}");
                var sr = At(t, "srgb", col + 0.5f, 20); var expSr = (1 - tt, 0f, tt);
                t.Assert.True(Close(sr, expSr), $"srgb cột {col}: {Show(sr)} ≈ {Show(expSr)}");
            }
            // Oklab giữa đường không bị xỉn như sRGB: kênh đỏ + xanh lớn hơn hẳn
            var mid = At(t, "ok", 100, 20); var midS = At(t, "srgb", 100, 20);
            t.Assert.True(mid.R + mid.B > midS.R + midS.B + 0.15f, $"oklab sáng hơn sRGB ở giữa ({Show(mid)} vs {Show(midS)})");
        });

        suite.Add("core/tw/gradient-corner-angle-radial", t =>
        {
            t.Open(new Fx(w => Fx.Page(w)
                .Child(Box(w, "br", "w-[200px] h-[50px] bg-linear-to-br/srgb from-black to-white"))
                .Child(Box(w, "a90", "w-[200px] h-4 bg-linear-90/srgb from-black to-white"))
                .Child(Box(w, "tor", "w-[200px] h-4 bg-linear-to-r/srgb from-black to-white"))
                .Child(Box(w, "rad", "size-[100px] bg-radial/srgb from-white to-black"))));
            // to bottom right: đường 50% đi qua góc trên-phải và dưới-trái (hướng phụ thuộc tỉ lệ hộp như CSS)
            var tr = At(t, "br", 199.5f, 0.5f); var bl = At(t, "br", 0.5f, 49.5f); var c = At(t, "br", 100, 25);
            t.Assert.True(MathF.Abs(tr.R - 0.5f) < 0.03f && MathF.Abs(bl.R - 0.5f) < 0.03f && MathF.Abs(c.R - 0.5f) < 0.01f,
                $"to-br: góc trên-phải {Show(tr)}, dưới-trái {Show(bl)}, tâm {Show(c)} đều ≈ 50%");
            t.Assert.True(At(t, "br", 0.5f, 0.5f).R < 0.02f && At(t, "br", 199.5f, 49.5f).R > 0.98f, "to-br: góc trên-trái đen, dưới-phải trắng");
            for (int col = 0; col < 200; col += 33)
                t.Assert.Near(At(t, "a90", col + 0.5f, 8).R, At(t, "tor", col + 0.5f, 8).R, 0.004f, $"bg-linear-90 = to-r (cột {col})");
            t.Assert.True(At(t, "rad", 50, 50).R > 0.98f, "radial: tâm là màu from");
            // farthest-corner: bán kính theo x = 50·√2 → t = 0.5 tại 35.36px từ tâm
            t.Assert.Near(At(t, "rad", 50 + 35.36f, 50).R, 0.5f, 0.02f, "radial: t = 0.5 tại nửa bán kính ellipse farthest-corner");
            t.Assert.True(At(t, "rad", 0.5f, 0.5f).R < 0.03f, "radial: góc gần màu to");
        });

        suite.Add("core/tw/gradient-stops-fade-rounded", t =>
        {
            t.Open(new Fx(w => Fx.Page(w)
                .Child(Box(w, "stops", "w-[200px] h-4 bg-linear-to-r/srgb from-[#ff0000] from-20% via-[#00ff00] via-50% to-[#0000ff] to-80%"))
                .Child(w.Div().Class("bg-[#2563eb] p-2").Child(Box(w, "fade", "w-[200px] h-[100px] bg-linear-to-b from-white to-transparent")))
                .Child(Box(w, "round", "w-[200px] h-10 rounded-xl border-4 border-[#000000] bg-linear-to-r from-[#ff0000] to-[#ff0000]"))));
            t.Assert.True(Close(At(t, "stops", 20, 8), (1, 0, 0)) && Close(At(t, "stops", 180, 8), (0, 0, 1)),
                "from-20%: đỏ đặc tới 20%, via-50% xanh lá ở giữa, to-80%: xanh dương đặc từ 80%");
            t.Assert.True(At(t, "stops", 100.5f, 8).G > 0.99f, "giữa (50%) là màu via");
            t.Assert.True(Close(At(t, "stops", 65.5f, 8), (1 - (65.5f - 40) / 60, (65.5f - 40) / 60, 0)), "20%→50%: nội suy tuyến tính đỏ→xanh lá");
            // Trắng → transparent nội suy premultiplied: màu giữ trắng, chỉ alpha giảm → trộn đều với nền xanh
            var blue = (0x25 / 255f, 0x63 / 255f, 0xeb / 255f);
            var midFade = At(t, "fade", 100, 50);
            var expMid = (0.5f + 0.5f * blue.Item1, 0.5f + 0.5f * blue.Item2, 0.5f + 0.5f * blue.Item3);
            t.Assert.True(Close(midFade, expMid), $"fade giữa: {Show(midFade)} ≈ {Show(expMid)} (không bị ám xám/đen)");
            t.Assert.True(Close(At(t, "fade", 100, 0.5f), (1, 1, 1), 4) && Close(At(t, "fade", 100, 99.5f), blue, 4), "fade: trên trắng, dưới lộ nền");
            t.Assert.True(Close(At(t, "round", 0.5f, 0.5f), (1, 1, 1)), "góc bo của hộp gradient không bị tô");
            t.Assert.True(Close(At(t, "round", 100, 1.5f), (0, 0, 0)) && Close(At(t, "round", 100, 20), (1, 0, 0)), "viền vẽ trên gradient, gradient bên trong");
        });

        suite.Add("core/tw/gradient-replay-keeps-index", t =>
        {
            var host = t.Open(new GradHost());
            t.Assert.True(Close(At(t, "bottom", 0.5f, 10), (1, 0, 0)) && Close(At(t, "bottom", 199.5f, 10), (0, 0, 1)), "frame đầu: đỏ → xanh dương");
            // Lấy toạ độ nút trước: dựng UiTree để tra khoá có thể vẽ lại toàn bộ (chế độ inspect), làm sai bộ đếm render
            var more = t.PointOf("more");
            var bb = t.Box("bottom"); // đọc pixel theo toạ độ cố định: tra cây sau đó sẽ vẽ lại không dùng cache
            t.Click(more); t.Frames();
            int before = host.Bottom.Renders;
            // Đúng frame view trên render lại (thêm gradient đứng trước) còn view dưới phát lại: vẽ chính Scene đó bằng CPU.
            // (PixelAt dùng frame soi không cache nên không kiểm được đường phát lại.)
            t.Click(more);
            var scene = t.Draw();
            t.Assert.Equal(host.Bottom.Renders, before, "view dưới được phát lại từ cache (không render lại)");
            t.Assert.True(scene.GradientCount >= 3, "view trên đã thêm gradient đứng trước → chỉ số gradient của view dưới phải dời");
            var canvas = new GotX.Raster.Canvas();
            canvas.Resize(t.PixelWidth, t.PixelHeight);
            GotX.Raster.SoftwareRenderer.Render(scene, canvas);
            Rgba Px(float x, float y) { uint v = canvas.Pixels[(int)y * canvas.Width + (int)x]; return new(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, 1); }
            var left = Px(bb.X + 0.5f, bb.Y + 10); var right = Px(bb.X + 199.5f, bb.Y + 10);
            t.Assert.True(Close(left, (1, 0, 0)) && Close(right, (0, 0, 1)), $"sau phát lại vẫn đúng gradient của chính nó ({Show(left)} → {Show(right)})");
        });

        RegisterLayout(suite);
    }

    sealed class GradHost : View
    {
        public GradTop Top = null!;
        public GradBottom Bottom = null!;
        protected override void OnCreated() { Top = App.New(new GradTop()); Bottom = App.New(new GradBottom()); }
        public override Div Render(Window w) => Fx.Page(w).Child(w.View(Top)).Child(w.View(Bottom));
    }

    sealed class GradTop : View
    {
        public int N;
        public override Div Render(Window w)
        {
            var row = w.Div().Class("flex gap-1").Child(w.Div().Key("more").Class("size-6 bg-slate-300 cursor-pointer")
                .OnClick(this, static (v, _) => { v.N++; v.Notify(); }));
            for (int i = 0; i < N; i++) row.Child(w.Div().Class("size-6 bg-linear-to-r from-green-500 to-green-700"));
            return row;
        }
    }

    sealed class GradBottom : View
    {
        public int Renders;
        public override Div Render(Window w) { Renders++; return w.Div().Key("bottom").Class("w-[200px] h-5 bg-linear-to-r/srgb from-[#ff0000] to-[#0000ff]"); }
    }
}
