using GotX;
using static GpuKit;

/// <summary>Scene dựng tay cho test parity: mỗi cái dồn một nhóm tính năng của shader (viền, gradient, bóng, cắt hình, biến đổi, ảnh).</summary>
static class GpuParityScenes
{
    public const int W = 360, H = 260;
    static readonly Bounds V = new(0, 0, W, H);
    // ImageSource chỉ được tham chiếu yếu: giữ sống suốt phiên test.
    static readonly List<ImageSource> Keep = new();
    static Rgba C(uint hex, float a = 1) => Rgba.FromHex(hex, a);
    static Mat2 Rot(float deg) { float r = deg * MathF.PI / 180, c = MathF.Cos(r), s = MathF.Sin(r); return new(c, -s, s, c); }

    public static IEnumerable<(string Name, Scene Scene)> All()
    {
        yield return ("solid-rounded-alpha", SolidRounded());
        yield return ("borders", Borders());
        yield return ("translucent-borders", TranslucentBorders());
        yield return ("gradients", Gradients());
        yield return ("shadows", Shadows());
        yield return ("clip-shapes", ClipShapes());
        yield return ("transforms", Transforms());
        yield return ("images", Images());
    }

    static Scene SolidRounded()
    {
        var s = NewScene(C(0xF4F6FA));
        s.Add(Quad(new Bounds(10, 10, 160, 100), V, C(0x2563EB), 14));
        s.Add(Quad(new Bounds(90, 50, 160, 100), V, C(0xEF4444, 0.6f), 30));
        s.Add(Quad(new Bounds(200, 20, 120, 120), V, C(0x10B981, 0.35f), 60));
        s.Add(Quad(new Bounds(20.5f, 160.25f, 99.3f, 60.7f), V, C(0x111827), 5.5f));
        s.Add(Quad(new Bounds(150, 170, 40, 40), V, C(0xF59E0B), 20));
        return s;
    }

    static Scene Borders()
    {
        var s = NewScene(C(0xFFFFFF));
        var p = Quad(new Bounds(12, 12, 150, 90), V, C(0xDBEAFE), 12);
        p.BorderWidths = new Edges(3, 6, 2, 4);
        p.BorderTop = C(0x1D4ED8); p.BorderRight = C(0xDC2626); p.BorderBottom = C(0x16A34A); p.BorderLeft = C(0x9333EA);
        s.Add(p);
        var d = Quad(new Bounds(190, 12, 150, 90), V, C(0xFEF3C7), 8);
        d.BorderWidths = new Edges(3, 3, 3, 3); d.BorderStyle = BorderStyle.Dashed;
        d.BorderTop = d.BorderRight = d.BorderBottom = d.BorderLeft = C(0x92400E);
        s.Add(d);
        var o = Quad(new Bounds(12, 130, 150, 100), V, C(0xFFFFFF), 50);
        o.BorderWidths = new Edges(4, 4, 4, 4); o.BorderStyle = BorderStyle.Dotted;
        o.BorderTop = o.BorderRight = o.BorderBottom = o.BorderLeft = C(0x0F172A);
        s.Add(o);
        var t = Quad(new Bounds(190, 130, 150, 100), V, C(0xE2E8F0), 0);
        t.BorderWidths = new Edges(1, 1, 1, 1);
        t.BorderTop = t.BorderRight = t.BorderBottom = t.BorderLeft = C(0x64748B);
        s.Add(t);
        return s;
    }

    static Scene TranslucentBorders()
    {
        var s = NewScene(C(0xFAFAFA));
        s.Add(Quad(new Bounds(0, 0, W, 130), V, C(0x1E3A8A), 0));
        foreach (var (x, a, bga) in new[] { (12f, 0.3f, 1f), (130f, 0.5f, 0.5f), (248f, 0.8f, 0f) })
        {
            var p = Quad(new Bounds(x, 20, 100, 90), V, C(0xFFFFFF, bga), 16);
            p.BorderWidths = new Edges(8, 8, 8, 8);
            p.BorderTop = p.BorderRight = p.BorderBottom = p.BorderLeft = C(0xF97316, a);
            s.Add(p);
            var q = Quad(new Bounds(x, 150, 100, 90), V, C(0x22C55E, bga), 0);
            q.BorderWidths = new Edges(2, 10, 6, 14);
            q.BorderTop = C(0x000000, a); q.BorderRight = C(0xFFFFFF, a); q.BorderBottom = C(0xEF4444, a); q.BorderLeft = C(0x3B82F6, a);
            s.Add(q);
        }
        return s;
    }

    static Scene Gradients()
    {
        var s = NewScene(C(0xF8FAFC));
        var items = new (GradientFill G, Bounds B, float R, Rgba Bg)[]
        {
            (new(GradientKind.Linear, 0, 1, false, false, C(0xFF0000), C(0x0000FF), default, 0, 1, 1, 2), new Bounds(10, 10, 110, 70), 10, default),
            (new(GradientKind.Linear, 1, -1, true, true, C(0x22D3EE), C(0xA855F7), default, 0, 1, 1, 2), new Bounds(130, 10, 110, 70), 20, default),
            (new(GradientKind.Linear, 0.7f, 0.7f, false, true, C(0xFACC15), C(0xEF4444), C(0x1E40AF), 0, 0.4f, 1, 3), new Bounds(250, 10, 100, 70), 6, default),
            (new(GradientKind.Radial, 0, 0, false, true, C(0xFFFFFF), C(0x6366F1), default, 0, 1, 1, 2), new Bounds(10, 95, 110, 70), 35, C(0x000000)),
            (new(GradientKind.Linear, 1, 0, false, false, C(0x000000, 0), C(0x000000, 0.8f), default, 0, 1, 1, 2), new Bounds(130, 95, 110, 70), 8, C(0xF97316)),
            (new(GradientKind.Linear, 0, 1, false, true, C(0x10B981, 0.2f), C(0x3B82F6), default, 0.2f, 0.8f, 1, 2), new Bounds(250, 95, 100, 70), 0, C(0xFFFFFF, 0.5f)),
            (new(GradientKind.Radial, 0, 0, false, false, C(0xFDE68A), C(0xB45309, 0.5f), default, 0.1f, 0.9f, 1, 2), new Bounds(10, 180, 340, 70), 24, C(0x1F2937)),
        };
        foreach (var (g, b, r, bg) in items)
        {
            var p = Quad(b, V, bg, r);
            p.Gradient = s.AddGradient(g);
            s.Add(p);
        }
        return s;
    }

    static Scene Shadows()
    {
        var s = NewScene(C(0xF1F5F9));
        s.Add(Shadow(new Bounds(30, 30, 100, 70), V, C(0x000000, 0.35f), 10, 0));
        s.Add(Quad(new Bounds(30, 30, 100, 70), V, C(0xFFFFFF), 10));
        s.Add(Shadow(new Bounds(160, 30, 100, 70), V, C(0x000000, 0.3f), 12, 8));
        s.Add(Quad(new Bounds(160, 30, 100, 70), V, C(0xFFFFFF), 12));
        var ex = Shadow(new Bounds(40, 130, 120, 80), V, C(0x2563EB, 0.5f), 16, 24);
        ex.Gradient = 1; ex.Source = new Bounds(40, 130, 120, 80); ex.BorderWidths = new Edges(16, 16, 16, 16);
        s.Add(ex);
        s.Add(Quad(new Bounds(40, 130, 120, 80), V, C(0xFFFFFF, 0.2f), 16));
        s.Add(Shadow(new Bounds(220, 140, 90, 60), V, C(0x000000, 0.5f), 30, 40));
        s.Add(Quad(new Bounds(220, 140, 90, 60), V, C(0xFFFFFF), 30));
        return s;
    }

    static Scene ClipShapes()
    {
        var s = NewScene(C(0xFFFFFF));
        int round = s.AddClipShape(new ClipShape(new Bounds(20, 20, 140, 100), Corners.All(30), default));
        int rotated = s.AddClipShape(new ClipShape(new Bounds(200, 40, 120, 80), Corners.All(12), Rot(20)));
        var a = Quad(new Bounds(0, 0, 200, 140), V, C(0x2563EB), 0); a.ClipShape = round; s.Add(a);
        var b = Quad(new Bounds(60, 60, 200, 100), V, C(0xF97316, 0.7f), 18); b.ClipShape = round; s.Add(b);
        var c = Quad(new Bounds(180, 20, 160, 120), V, C(0x10B981), 0); c.ClipShape = rotated; s.Add(c);
        var d = Shadow(new Bounds(190, 150, 120, 60), V, C(0x000000, 0.5f), 10, 14); d.ClipShape = round; s.Add(d);
        var e = Quad(new Bounds(20, 150, 150, 90), V, C(0x9333EA), 0);
        e.ClipShape = s.AddClipShape(new ClipShape(new Bounds(20, 150, 150, 90), new Corners(40, 0, 40, 0), default));
        s.Add(e);
        return s;
    }

    static Scene Transforms()
    {
        var s = NewScene(C(0xF8FAFC));
        var a = Quad(new Bounds(40, 40, 120, 70), V, C(0x2563EB), 14); a.Transform = Rot(25); s.Add(a);
        var b = Quad(new Bounds(200, 40, 110, 70), V, C(0xFFFFFF), 6);
        b.BorderWidths = new Edges(4, 4, 4, 4); b.BorderTop = b.BorderRight = b.BorderBottom = b.BorderLeft = C(0xDC2626);
        b.Transform = new Mat2(1, 0.4f, 0, 1); s.Add(b);
        var c = Shadow(new Bounds(60, 150, 100, 60), V, C(0x000000, 0.4f), 14, 16); c.Transform = Rot(-15); s.Add(c);
        var d = Quad(new Bounds(60, 150, 100, 60), V, C(0xFACC15), 14); d.Transform = Rot(-15); s.Add(d);
        var e = Quad(new Bounds(220, 150, 100, 60), V, C(0x14B8A6, 0.6f), 10); e.Transform = new Mat2(1.2f, 0, 0, 1 / 1.2f); s.Add(e);
        return s;
    }

    static Scene Images()
    {
        var s = NewScene(C(0xE5E7EB));
        var px = new byte[48 * 32 * 4];
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 48; x++)
            {
                int i = (y * 48 + x) * 4;
                px[i] = (byte)(x * 5); px[i + 1] = (byte)(y * 8); px[i + 2] = (byte)(255 - x * 4);
                px[i + 3] = (byte)(x < 8 ? 120 : 255);
            }
        var inv = new byte[px.Length];
        for (int i = 0; i < px.Length; i++) inv[i] = i % 4 == 3 ? px[i] : (byte)(255 - px[i]);
        var a = ImageSource.FromRgba(48, 32, px);
        var b = ImageSource.FromRgba(48, 32, inv);
        Keep.Add(a); Keep.Add(b);
        Primitive Img(ImageSource im, Bounds bounds, float radius, float alpha, Bounds src)
        {
            var p = Quad(bounds, V, new Rgba(1, 1, 1, alpha), radius);
            p.Kind = PrimitiveKind.Image; p.Glyph = im.Id; p.Source = src;
            return p;
        }
        var full = new Bounds(0, 0, 48, 32);
        s.Add(Img(a, new Bounds(10, 10, 144, 96), 0, 1, full));
        s.Add(Img(b, new Bounds(170, 10, 96, 64), 16, 1, full));
        s.Add(Img(a, new Bounds(280, 10, 64, 64), 32, 0.6f, new Bounds(8, 0, 32, 32)));
        s.Add(Img(b, new Bounds(10, 130, 100, 100), 0, 1, new Bounds(0, 0, 24, 16)));
        s.Add(Img(a, new Bounds(130, 140, 80, 80), 10, 1, full));
        s.Add(Img(b, new Bounds(230, 140, 100, 70), 0, 0.9f, full));
        return s;
    }
}
