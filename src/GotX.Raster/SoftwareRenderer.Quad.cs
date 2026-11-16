namespace GotX.Raster;

public static partial class SoftwareRenderer
{
    /// <summary>Tắt đường nhanh của Quad (tô đoạn SIMD + lặp một pixel giữa cạnh) để so từng pixel với đường SDF gốc trong test.</summary>
    public static bool QuadFastPaths { get; set; } = true;

    // ---------- Quad: nền + viền từng cạnh (liền/gạch/chấm) + bo góc (+ biến đổi) ----------

    static void Quad(Canvas c, Scene scene, in Primitive p, in Shape clip)
    {
        var r = Area(p, 1, c);
        if (r.Empty) return;

        var q = new QuadShader(scene, p);
        var b = p.Bounds;
        bool opaque = q.Bg.A >= 0.999f;
        uint solid = Canvas.Pack(q.BgR, q.BgG, q.BgB, 1);

        // Vùng chắc chắn nằm trong: tô nhanh không cần SDF (chỉ khi không biến đổi, không cắt hình, màu đặc)
        bool canFast = QuadFastPaths && !q.F.T && !q.HasGrad;
        float inset = q.Inset + 1;
        int fastX0 = (int)MathF.Ceiling(b.X + inset), fastX1 = (int)MathF.Floor(b.Right - inset);
        // Đoạn giữa của hàng sát cạnh (xa mọi góc >= bán kính + độ dày viền + 2): kết quả SDF chỉ phụ thuộc y (xem QuadShader) nên tính một pixel rồi lặp.
        // Không dùng khi có vùng cắt hình / viền gạch-chấm / bốn cạnh khác màu (phụ thuộc x); gradient đã loại ở canFast.
        bool canSpan = canFast && !clip.On && (!q.HasBorder || (p.BorderStyle == BorderStyle.Solid && q.UniformBorder));
        float spanInset = q.Inset + q.MaxBorder + 2;
        int spanX0 = (int)MathF.Ceiling(b.X + spanInset), spanX1 = (int)MathF.Floor(b.Right - spanInset);

        using var run = new BlendRun();
        var px = c.Pixels;
        for (int y = r.Y0; y < r.Y1; y++)
        {
            int row = y * c.Width;
            float py = y + 0.5f;
            bool rowFast = canFast && MathF.Abs(py - q.F.Cy) <= q.Hh - inset;
            int rx0 = fastX0, rx1 = fastX1;
            if (rowFast && clip.On)
            {
                // Dưới vùng cắt theo hình: chỉ tô nhanh phần nằm chắc chắn trong hình (độ phủ đúng 1).
                if (clip.SolidRow(py, out float cx0, out float cx1)) { rx0 = Math.Max(rx0, (int)MathF.Ceiling(cx0 - 0.5f)); rx1 = Math.Min(rx1, (int)MathF.Floor(cx1 - 0.5f) + 1); }
                else rowFast = false;
            }
            // Đoạn chắc chắn nằm trong: tô một lượt (SIMD Fill khi đặc), hai đầu đi SDF từng pixel.
            int fx0 = r.X1, fx1 = r.X1;
            if (rowFast) fx0 = Math.Max(rx0, r.X0);
            else if (canSpan) fx0 = Math.Max(spanX0, r.X0);
            if (fx0 < r.X1) { fx1 = Math.Min(rowFast ? rx1 : spanX1, r.X1); if (fx0 >= fx1) fx0 = fx1 = r.X1; }
            if (fx0 < fx1)
            {
                if (rowFast)
                {
                    if (opaque) px.AsSpan(row + fx0, fx1 - fx0).Fill(solid);
                    else if (q.Bg.A > 0) for (int x = fx0; x < fx1; x++) run.Blend(ref px[row + x], q.BgR, q.BgG, q.BgB, q.Bg.A);
                }
                else if (q.Shade(fx0 + 0.5f, py, clip, out float sr, out float sg, out float sb, out float sa)) // hàng sát cạnh: một pixel đại diện
                {
                    if (sa >= 0.999f) px.AsSpan(row + fx0, fx1 - fx0).Fill(Canvas.Pack(sr, sg, sb, 1));
                    else for (int x = fx0; x < fx1; x++) run.Blend(ref px[row + x], sr, sg, sb, sa);
                }
            }
            for (int x = r.X0; x < r.X1; x++)
            {
                if (x == fx0 && fx0 < fx1) { x = fx1 - 1; continue; }
                if (q.Shade(x + 0.5f, py, clip, out float cr, out float cg, out float cb, out float ca)) Blend(ref px[row + x], cr, cg, cb, ca);
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
            float period = len / MathF.Max(1, Snap(len / (4 * w)));
            float u = t - Snap(t / period) * period;
            return band * Math.Clamp(period / 4 - MathF.Abs(u) + 0.5f, 0, 1);
        }
        else // Dotted
        {
            float period = len / MathF.Max(1, Snap(len / (2 * w)));
            float u = t - Snap(t / period) * period, v = across - w / 2;
            return Math.Clamp(w / 2 - MathF.Sqrt(u * u + v * v) + 0.5f, 0, 1);
        }
    }

    /// <summary>Làm tròn nửa lên (như floor(x + 0.5) trong HLSL): MathF.Round chẵn-gần-nhất còn GPU lệch ở đúng điểm .5 (vd 150/12) nên chu kỳ gạch/chấm lệch giữa hai renderer.</summary>
    static float Snap(float x) => MathF.Floor(x + 0.5f);
}
