namespace GotX.Raster;

public static partial class SoftwareRenderer
{
    // ---------- Quad: nền + viền từng cạnh (liền/gạch/chấm) + bo góc (+ biến đổi) ----------

    static void Quad(Canvas c, Scene scene, in Primitive p, in Shape clip)
    {
        var r = Area(p, 1, c);
        if (r.Empty) return;

        var b = p.Bounds;
        float hw = b.Width / 2, hh = b.Height / 2;
        var f = new Frame(b, p.Transform);
        var corners = ClampCorners(p.Corners, MathF.Min(hw, hh));

        // Viền từng cạnh → hộp bên trong (tâm lệch nếu các cạnh dày khác nhau)
        var w = p.BorderWidths;
        bool hasBorder = p.HasBorder && p.BorderStyle != BorderStyle.None;
        float bt = MathF.Min(w.Top, b.Height), bb = MathF.Min(w.Bottom, b.Height), bl = MathF.Min(w.Left, b.Width), br = MathF.Min(w.Right, b.Width);
        float ix0 = -hw + bl, ix1 = hw - br, iy0 = -hh + bt, iy1 = hh - bb;
        float icx = (ix0 + ix1) / 2, icy = (iy0 + iy1) / 2, ihw = (ix1 - ix0) / 2, ihh = (iy1 - iy0) / 2;
        var inner = new Corners(
            MathF.Max(0, corners.TopLeft - MathF.Max(bl, bt)), MathF.Max(0, corners.TopRight - MathF.Max(br, bt)),
            MathF.Max(0, corners.BottomRight - MathF.Max(br, bb)), MathF.Max(0, corners.BottomLeft - MathF.Max(bl, bb)));

        var bg = p.Color;
        float bgR = bg.R * bg.A, bgG = bg.G * bg.A, bgB = bg.B * bg.A;
        uint solid = Canvas.Pack(bgR, bgG, bgB, 1);
        bool opaque = bg.A >= 0.999f;

        // Gradient (background-image) phủ lên màu nền, tính theo toạ độ cục bộ như shader
        bool hasGrad = p.Gradient != 0;
        var grad = hasGrad ? new GradientSampler(scene.Gradient(p.Gradient), hw, hh) : default;

        // Vùng chắc chắn nằm trong: tô nhanh không cần SDF (chỉ khi không biến đổi, không cắt hình, màu đặc)
        bool canFast = !f.T && !hasGrad;
        float maxR = MathF.Max(MathF.Max(corners.TopLeft, corners.TopRight), MathF.Max(corners.BottomRight, corners.BottomLeft));
        float maxB = MathF.Max(MathF.Max(bt, bb), MathF.Max(bl, br));
        float inset = MathF.Max(maxR, maxB) + 1;
        int fastX0 = (int)MathF.Ceiling(b.X + inset), fastX1 = (int)MathF.Floor(b.Right - inset);

        using var run = new BlendRun();
        var px = c.Pixels;
        for (int y = r.Y0; y < r.Y1; y++)
        {
            int row = y * c.Width;
            float py = y + 0.5f;
            bool rowFast = canFast && MathF.Abs(py - f.Cy) <= hh - inset;
            int rx0 = fastX0, rx1 = fastX1;
            if (rowFast && clip.On)
            {
                // Dưới vùng cắt theo hình: chỉ tô nhanh phần nằm chắc chắn trong hình (độ phủ đúng 1).
                if (clip.SolidRow(py, out float cx0, out float cx1)) { rx0 = Math.Max(rx0, (int)MathF.Ceiling(cx0 - 0.5f)); rx1 = Math.Min(rx1, (int)MathF.Floor(cx1 - 0.5f) + 1); }
                else rowFast = false;
            }
            for (int x = r.X0; x < r.X1; x++)
            {
                if (rowFast && x >= rx0 && x < rx1)
                {
                    if (opaque) px[row + x] = solid;
                    else run.Blend(ref px[row + x], bgR, bgG, bgB, bg.A);
                    continue;
                }
                float pxx = x + 0.5f;
                f.Local(pxx, py, out float lx, out float ly);
                float d = RoundedBox(lx, ly, hw, hh, PickRadius(corners, lx, ly));
                float cov = Math.Clamp(0.5f - d, 0, 1);
                if (clip.On && cov > 0) cov *= clip.Coverage(pxx, py);
                if (cov <= 0) continue;

                float cr = bgR, cg = bgG, cb = bgB, ca = bg.A;
                if (hasGrad)
                {
                    var (gr, gg, gb, ga) = grad.Sample(lx, ly);
                    cr = gr + cr * (1 - ga); cg = gg + cg * (1 - ga); cb = gb + cb * (1 - ga); ca = ga + ca * (1 - ga);
                }
                if (hasBorder)
                {
                    float ex = lx - icx, ey = ly - icy;
                    float din = ihw <= 0 || ihh <= 0 ? 1 : RoundedBox(ex, ey, ihw, ihh, PickRadius(inner, ex, ey));
                    float band = 1 - Math.Clamp(0.5f - din, 0, 1); // 1 = trên viền, 0 = bên trong
                    float amount = p.BorderStyle == BorderStyle.Solid ? band
                        : Pattern(p.BorderStyle, lx, ly, hw, hh, bt, br, bb, bl, band);
                    if (amount > 0)
                    {
                        var side = SideColor(p, lx, ly, hw, hh, bt, br, bb, bl);
                        float sr = side.R * side.A, sg = side.G * side.A, sb = side.B * side.A;
                        if (side.A >= 1) { cr += (sr - cr) * amount; cg += (sg - cg) * amount; cb += (sb - cb) * amount; ca += (side.A - ca) * amount; }
                        else
                        {
                            // viền trong suốt / mờ vẽ ĐÈ LÊN nền (CSS: nền nằm dưới viền), không thay thế nền
                            float k = 1 - side.A * amount;
                            cr = sr * amount + cr * k; cg = sg * amount + cg * k; cb = sb * amount + cb * k; ca = side.A * amount + ca * k;
                        }
                    }
                }
                Blend(ref px[row + x], cr * cov, cg * cov, cb * cov, ca * cov);
            }
        }
    }

    /// <summary>Cạnh của điểm (lx, ly): khoảng cách chuẩn hoá nhỏ nhất. 0 = trên, 1 = phải, 2 = dưới, 3 = trái.</summary>
    static int Side(float lx, float ly, float hw, float hh, float bt, float br, float bb, float bl)
    {
        float t = bt > 0 ? (ly + hh) / bt : float.MaxValue;
        float r = br > 0 ? (hw - lx) / br : float.MaxValue;
        float b = bb > 0 ? (hh - ly) / bb : float.MaxValue;
        float l = bl > 0 ? (lx + hw) / bl : float.MaxValue;
        float m = MathF.Min(MathF.Min(t, r), MathF.Min(b, l));
        return m == t ? 0 : m == r ? 1 : m == b ? 2 : 3;
    }

    /// <summary>
    /// Màu viền tại một điểm theo cạnh của nó. Ranh giới giữa hai cạnh là đường chéo nối góc ngoài với góc trong –
    /// giống cách CSS vẽ góc viền.
    /// </summary>
    static Rgba SideColor(in Primitive p, float lx, float ly, float hw, float hh, float bt, float br, float bb, float bl) =>
        Side(lx, ly, hw, hh, bt, br, bb, bl) switch { 0 => p.BorderTop, 1 => p.BorderRight, 2 => p.BorderBottom, _ => p.BorderLeft };

    /// <summary>
    /// Viền gạch / chấm: tham số hoá theo chiều dài cạnh, số gạch (chấm) được làm tròn để chia đều cạnh và
    /// luôn có gạch (chấm) ở góc. Gạch dài bằng nửa chu kỳ 4×độ dày; chấm tròn đường kính bằng độ dày, cách 2×độ dày.
    /// </summary>
    static float Pattern(BorderStyle style, float lx, float ly, float hw, float hh, float bt, float br, float bb, float bl, float band)
    {
        int side = Side(lx, ly, hw, hh, bt, br, bb, bl);
        float t, len, across, w;
        switch (side)
        {
            case 0: t = lx + hw; len = 2 * hw; across = ly + hh; w = bt; break;
            case 1: t = ly + hh; len = 2 * hh; across = hw - lx; w = br; break;
            case 2: t = lx + hw; len = 2 * hw; across = hh - ly; w = bb; break;
            default: t = ly + hh; len = 2 * hh; across = lx + hw; w = bl; break;
        }
        if (w <= 0) return 0;
        if (style == BorderStyle.Dashed)
        {
            float period = len / MathF.Max(1, MathF.Round(len / (4 * w)));
            float u = t - MathF.Round(t / period) * period;
            return band * Math.Clamp(period / 4 - MathF.Abs(u) + 0.5f, 0, 1);
        }
        else // Dotted
        {
            float period = len / MathF.Max(1, MathF.Round(len / (2 * w)));
            float u = t - MathF.Round(t / period) * period, v = across - w / 2;
            return Math.Clamp(w / 2 - MathF.Sqrt(u * u + v * v) + 0.5f, 0, 1);
        }
    }
}
