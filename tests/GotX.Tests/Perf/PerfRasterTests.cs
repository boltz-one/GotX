using GotX;
using GotX.Raster;
using GotX.Test;
using GotX.Text;

/// <summary>
/// Các đường nhanh của renderer CPU (tô nhanh dưới vùng cắt theo hình, nhớ alpha bóng theo hàng/cột, bảng blend) phải cho ĐÚNG TỪNG BIT
/// như phép tính từng pixel nguyên bản – ở đây dựng lại phép tính đó thẳng từ công thức và so với <see cref="SoftwareRenderer"/>.
/// </summary>
static class PerfRasterTests
{
    // ---- Phép tính tham chiếu: đúng công thức của renderer, không đường nhanh nào ----

    static uint Pack(float r, float g, float b, float a) =>
        ((uint)(Math.Clamp(a, 0, 1) * 255 + 0.5f) << 24) | ((uint)(Math.Clamp(r, 0, 1) * 255 + 0.5f) << 16) |
        ((uint)(Math.Clamp(g, 0, 1) * 255 + 0.5f) << 8) | (uint)(Math.Clamp(b, 0, 1) * 255 + 0.5f);

    static void Blend(ref uint dst, float r, float g, float b, float a)
    {
        if (a <= 0) return;
        if (a >= 0.999f) { dst = Pack(r, g, b, 1); return; }
        float inv = 1 - a;
        uint d = dst;
        dst = Pack(r + ((d >> 16) & 0xFF) / 255f * inv, g + ((d >> 8) & 0xFF) / 255f * inv, b + (d & 0xFF) / 255f * inv, a + (d >> 24) / 255f * inv);
    }

    static float Box(float px, float py, float hw, float hh, float r)
    {
        float qx = MathF.Abs(px) - hw + r, qy = MathF.Abs(py) - hh + r;
        float ox = MathF.Max(qx, 0), oy = MathF.Max(qy, 0);
        return MathF.Sqrt(ox * ox + oy * oy) + MathF.Min(MathF.Max(qx, qy), 0) - r;
    }

    static float Pick(in Corners c, float px, float py) => px < 0 ? (py < 0 ? c.TopLeft : c.BottomLeft) : (py < 0 ? c.TopRight : c.BottomRight);

    static Corners Clamp(in Corners c, float max) => new(MathF.Min(c.TopLeft, max), MathF.Min(c.TopRight, max), MathF.Min(c.BottomRight, max), MathF.Min(c.BottomLeft, max));

    static float Erf(float x)
    {
        float s = MathF.Sign(x); x = MathF.Abs(x);
        float t = 1f / (1f + 0.3275911f * x);
        float y = 1f - ((((1.061405429f * t - 1.453152027f) * t + 1.421413741f) * t - 0.284496736f) * t + 0.254829592f) * t * MathF.Exp(-x * x);
        return s * y;
    }

    static (int x0, int y0, int x1, int y1) Area(Bounds b, Bounds clip, int w, int h)
    {
        var i = b.Intersect(clip);
        if (i.IsEmpty) return (0, 0, 0, 0);
        return (Math.Max(0, (int)MathF.Floor(i.X)), Math.Max(0, (int)MathF.Floor(i.Y)), Math.Min(w, (int)MathF.Ceiling(i.Right)), Math.Min(h, (int)MathF.Ceiling(i.Bottom)));
    }

    static float ShapeCoverage(in ClipShape s, float px, float py)
    {
        float hw = s.Rect.Width / 2, hh = s.Rect.Height / 2;
        var c = Clamp(s.Corners, MathF.Min(hw, hh));
        float lx = px - (s.Rect.X + hw), ly = py - (s.Rect.Y + hh);
        return Math.Clamp(0.5f - Box(lx, ly, hw, hh, Pick(c, lx, ly)), 0, 1);
    }

    static void RefShadow(uint[] px, int w, int h, in Primitive p, ClipShape? shape)
    {
        float sigma = p.Blur / 2;
        var (x0, y0, x1, y1) = Area(p.Bounds.Inflate(sigma * 3 + 1), p.Clip, w, h);
        float hw = p.Bounds.Width / 2, hh = p.Bounds.Height / 2, cx = p.Bounds.X + hw, cy = p.Bounds.Y + hh;
        var corners = Clamp(p.Corners, MathF.Max(0, MathF.Min(hw, hh)));
        float inv = sigma > 0 ? 1f / (sigma * 1.41421356f) : 0;
        var col = p.Color;
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
            {
                float pxx = x + 0.5f, py = y + 0.5f, lx = pxx - cx, ly = py - cy;
                float d = Box(lx, ly, hw, hh, Pick(corners, lx, ly));
                float a = sigma > 0 ? 0.5f * (1 - Erf(d * inv)) : Math.Clamp(0.5f - d, 0, 1);
                a *= col.A;
                if (shape is { } s && a > 0.002f) a *= ShapeCoverage(s, pxx, py);
                if (a > 0.002f) Blend(ref px[y * w + x], col.R * a, col.G * a, col.B * a, a);
            }
    }

    static void RefQuad(uint[] px, int w, int h, in Primitive p, ClipShape? shape)
    {
        var (x0, y0, x1, y1) = Area(p.Bounds.Inflate(1), p.Clip, w, h);
        float hw = p.Bounds.Width / 2, hh = p.Bounds.Height / 2, cx = p.Bounds.X + hw, cy = p.Bounds.Y + hh;
        var corners = Clamp(p.Corners, MathF.Min(hw, hh));
        var bg = p.Color;
        float bgR = bg.R * bg.A, bgG = bg.G * bg.A, bgB = bg.B * bg.A;
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
            {
                float pxx = x + 0.5f, py = y + 0.5f, lx = pxx - cx, ly = py - cy;
                float cov = Math.Clamp(0.5f - Box(lx, ly, hw, hh, Pick(corners, lx, ly)), 0, 1);
                if (shape is { } s && cov > 0) cov *= ShapeCoverage(s, pxx, py);
                if (cov <= 0) continue;
                Blend(ref px[y * w + x], bgR * cov, bgG * cov, bgB * cov, bg.A * cov);
            }
    }

    static Primitive Prim(PrimitiveKind kind, Bounds b, Rgba color, float radius, Bounds clip, float blur = 0, int shape = 0) =>
        new() { Kind = kind, Bounds = b, Color = color, Corners = Corners.All(radius), Clip = clip, Blur = blur, ClipShape = shape };

    public static void Register(TestSuite suite)
    {
        suite.Add("perf/raster/shadow-and-clipped-quad-match-per-pixel-reference", t =>
        {
            const int W = 360, H = 260;
            var view = new Bounds(0, 0, W, H);
            var cases = new List<(string Name, Primitive[] Prims, ClipShape? Shape)>();
            var round = new ClipShape(new Bounds(30, 24, 300, 200), Corners.All(18), default);
            var square = new ClipShape(new Bounds(30, 24, 300, 200), Corners.All(0), default);
            var tall = new ClipShape(new Bounds(60, 10, 40, 240), new Corners(20, 4, 20, 4), default);
            foreach (var (shapeName, shape) in new (string, ClipShape?)[] { ("no-shape", null), ("round", round), ("square", square), ("tall-mixed", tall) })
            {
                cases.Add(($"{shapeName}/shadow-sm", [Prim(PrimitiveKind.Shadow, new Bounds(70, 60, 220, 120), new Rgba(0, 0, 0, 0.10f), 12, view, blur: 6)], shape));
                cases.Add(($"{shapeName}/shadow-wide", [Prim(PrimitiveKind.Shadow, new Bounds(50, 40, 260, 160), new Rgba(0.1f, 0.2f, 0.9f, 0.5f), 24, view, blur: 30)], shape));
                cases.Add(($"{shapeName}/shadow-sharp", [Prim(PrimitiveKind.Shadow, new Bounds(80.5f, 50.25f, 100, 100), new Rgba(0, 0, 0, 0.3f), 6, view, blur: 0)], shape));
                cases.Add(($"{shapeName}/shadow-mixed-corners", [new Primitive { Kind = PrimitiveKind.Shadow, Bounds = new Bounds(60, 50, 200, 140), Color = new Rgba(0, 0, 0, 0.2f), Corners = new Corners(30, 2, 14, 0), Clip = view, Blur = 16 }], shape));
                cases.Add(($"{shapeName}/quad-opaque", [Prim(PrimitiveKind.Quad, new Bounds(20, 16, 320, 228), new Rgba(0.9f, 0.3f, 0.2f, 1), 22, view)], shape));
                cases.Add(($"{shapeName}/quad-translucent", [Prim(PrimitiveKind.Quad, new Bounds(10.5f, 8.5f, 340, 240), new Rgba(0.2f, 0.4f, 0.8f, 0.37f), 10, view)], shape));
            }
            foreach (var (name, prims, shape) in cases)
            {
                var atlas = new GlyphAtlas();
                var scene = new Scene(atlas) { ClearColor = new Rgba(0.96f, 0.97f, 0.99f, 1) };
                int idx = shape is { } s ? scene.AddClipShape(s) : 0;
                var reference = new uint[W * H];
                Array.Fill(reference, Pack(scene.ClearColor.R, scene.ClearColor.G, scene.ClearColor.B, 1));
                foreach (var prim in prims)
                {
                    var p = prim;
                    p.ClipShape = idx;
                    scene.Add(p);
                    if (p.Kind == PrimitiveKind.Shadow) RefShadow(reference, W, H, p, shape); else RefQuad(reference, W, H, p, shape);
                }
                var canvas = new Canvas();
                canvas.Resize(W, H);
                SoftwareRenderer.Render(scene, canvas);
                int diff = 0, first = -1;
                for (int i = 0; i < W * H; i++)
                    if (canvas.Pixels[i] != reference[i]) { diff++; if (first < 0) first = i; }
                t.Assert.Equal(diff, 0, $"{name}: renderer CPU khác phép tính từng pixel tại {diff} pixel" + (first >= 0 ? $" (đầu tiên ở {first % W},{first / W}: {canvas.Pixels[first]:X8} ≠ {reference[first]:X8})" : ""));
            }
        });
    }
}
