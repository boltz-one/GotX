namespace GotX;

public sealed partial class Window
{
    /// <summary>
    /// Đưa gradient của Style vào Scene (background-image, vẽ trên màu nền trong cùng một Quad). Trả về chỉ số 1-based,
    /// 0 nếu không có gradient hoặc mọi điểm dừng đều trong suốt. Vị trí px (from-[20px]) được quy ra phần của đường
    /// gradient theo kích thước hộp layout – như CSS, nơi độ dài đường gradient phụ thuộc hộp.
    /// </summary>
    int AddGradientFill(in Style s, Bounds lb, float op)
    {
        ref readonly var g = ref s.Gradient;
        if (g.Kind == GradientKind.None) return 0;
        var c0 = Fade(g.From ?? Rgba.Transparent, op);
        var c2 = Fade(g.To ?? Rgba.Transparent, op);
        var c1 = g.Via is { } via ? Fade(via, op) : c2;
        if (c0.A <= 0 && c1.A <= 0 && c2.A <= 0) return 0;

        float dx = 0, dy = 0, hw = lb.Width / 2, hh = lb.Height / 2;
        bool corner = g.CornerX != 0 || g.CornerY != 0;
        if (corner) { dx = g.CornerX; dy = g.CornerY; }
        else if (g.Kind == GradientKind.Linear)
        {
            float rad = g.Angle * MathF.PI / 180;
            dx = MathF.Sin(rad); dy = -MathF.Cos(rad);
        }

        // Độ dài đường gradient (đơn vị layout) để đổi vị trí px sang phần trăm
        var probe = new GradientFill(g.Kind, dx, dy, corner, g.Oklab, default, default, default, 0, 0, 0, 2);
        float span = g.Kind == GradientKind.Radial ? hw * 1.41421356f : LineLength(probe, hw, hh);

        float p0 = Stop(g.FromPos, 0, span);
        float p1, p2;
        int count;
        if (g.Via is not null)
        {
            p1 = MathF.Max(p0, Stop(g.ViaPos, 0.5f, span));
            p2 = MathF.Max(p1, Stop(g.ToPos, 1, span));
            count = 3;
        }
        else
        {
            p1 = MathF.Max(p0, Stop(g.ToPos, 1, span));
            p2 = p1;
            count = 2;
        }
        // Hai điểm dừng: C1 là màu "to" (C2 không dùng)
        return _scene.AddGradient(new GradientFill(g.Kind, dx, dy, corner, g.Oklab,
            c0, count == 3 ? c1 : c2, c2, p0, p1, p2, count));
    }

    /// <summary>Độ dài đường gradient tuyến tính theo CSS: |W·sinθ| + |H·cosθ| (hướng góc tính theo tỉ lệ hộp).</summary>
    static float LineLength(in GradientFill probe, float hw, float hh)
    {
        float dx = probe.DirX, dy = probe.DirY;
        if (probe.Corner)
        {
            dx = probe.DirX * hh; dy = probe.DirY * hw;
            float n = MathF.Sqrt(dx * dx + dy * dy);
            if (n > 0) { dx /= n; dy /= n; }
        }
        return 2 * (MathF.Abs(hw * dx) + MathF.Abs(hh * dy));
    }

    float Stop(Length? pos, float fallback, float span) => pos switch
    {
        null => fallback,
        { Kind: LengthKind.Fraction } l => l.Value,
        { } l => span > 0 ? (l.Resolve(float.NaN, RemSize) ?? 0) / span : 0,
    };
}
