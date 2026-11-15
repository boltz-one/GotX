namespace GotX.Raster;

public static partial class SoftwareRenderer
{
    // ---------- Shadow: SDF + làm mờ Gauss (dùng hàm erf) ----------

    static void Shadow(Canvas c, in Primitive p, in Shape clip)
    {
        float sigma = p.Blur / 2;
        var r = Area(p, sigma * 3 + 1, c);
        if (r.Empty) return;

        var b = p.Bounds;
        float hw = b.Width / 2, hh = b.Height / 2;
        var f = new Frame(b, p.Transform);
        var corners = ClampCorners(p.Corners, MathF.Max(0, MathF.Min(hw, hh)));
        var col = p.Color;
        float inv = sigma > 0 ? 1f / (sigma * 1.41421356f) : 0;
        var px = c.Pixels;
        // Vùng loại trừ: box-shadow ngoài chỉ hiện ngoài hộp viền của phần tử (nền trong suốt không được để bóng lộ ra bên trong).
        bool excl = p.HasExclusion;
        float ehw = 0, ehh = 0, eox = 0, eoy = 0;
        var ecorners = default(Corners);
        if (excl)
        {
            ehw = p.Source.Width / 2; ehh = p.Source.Height / 2;
            ecorners = ClampCorners(new Corners(p.BorderWidths.Top, p.BorderWidths.Right, p.BorderWidths.Bottom, p.BorderWidths.Left), MathF.Max(0, MathF.Min(ehw, ehh)));
            f.Local(p.Source.X + ehw, p.Source.Y + ehh, out eox, out eoy); // tâm hộp trong hệ toạ độ cục bộ của bóng
        }

        // Vùng thẳng của hộp (không phải góc, không biến đổi): khoảng cách d chỉ phụ thuộc MỘT trục → a (tốn exp) giống nhau trên cả
        // hàng (dải trên/dưới) hoặc cả cột (dải trái/phải). Nhớ theo hàng / theo cột thay vì tính lại mỗi pixel; công thức giữ nguyên
        // nên kết quả đúng từng bit.
        bool memo = sigma > 0 && !f.T;
        int width = r.X1 - r.X0;
        float[]? colVal = null;
        bool[]? colSet = null;
        if (memo)
        {
            colVal = System.Buffers.ArrayPool<float>.Shared.Rent(2 * width);
            colSet = System.Buffers.ArrayPool<bool>.Shared.Rent(2 * width);
            Array.Clear(colSet, 0, 2 * width);
        }
        var run = new BlendRun();
        {
            Span<float> rowVal = stackalloc float[2];
            Span<bool> rowSet = stackalloc bool[2];
            for (int y = r.Y0; y < r.Y1; y++)
            {
                int row = y * c.Width;
                float py = y + 0.5f;
                rowSet.Clear();
                for (int x = r.X0; x < r.X1; x++)
                {
                    float pxx = x + 0.5f;
                    f.Local(pxx, py, out float lx, out float ly);
                    float rad = PickRadius(corners, lx, ly);
                    float a;
                    if (sigma > 0)
                    {
                        // Ngoài vùng chuyển tiếp thì kết quả đã biết chính xác, khỏi tính exp(): x ≥ 2.25 → a < 0.002 (bị bỏ ngay bên dưới),
                        // x ≤ -4.5 → xấp xỉ erf cho đúng -1 (số hạng hiệu chỉnh < 2^-25 nên 1 - … làm tròn về 1) → a = 1.
                        int zone = 0; // 1 = dải ngang (trên/dưới), 2 = dải dọc (trái/phải)
                        int side = 0;
                        if (memo)
                        {
                            float qx = MathF.Abs(lx) - hw + rad, qy = MathF.Abs(ly) - hh + rad;
                            if (qx <= 0 && qy > 0) { zone = 1; side = lx < 0 ? 0 : 1; }
                            else if (qx > 0 && qy <= 0) { zone = 2; side = (ly < 0 ? 0 : width) + (x - r.X0); }
                        }
                        if (zone == 1 && rowSet[side]) a = rowVal[side];
                        else if (zone == 2 && colSet![side]) a = colVal![side];
                        else
                        {
                            float xe = RoundedBox(lx, ly, hw, hh, rad) * inv;
                            a = xe >= 2.25f ? -1f : xe <= -4.5f ? 1f : 0.5f * (1 - Erf(xe));
                            if (zone == 1) { rowSet[side] = true; rowVal[side] = a; }
                            else if (zone == 2) { colSet![side] = true; colVal![side] = a; }
                        }
                        if (a < 0) continue;
                    }
                    else a = Math.Clamp(0.5f - RoundedBox(lx, ly, hw, hh, rad), 0, 1);
                    a *= col.A;
                    if (excl && a > 0.002f)
                    {
                        float elx = lx - eox, ely = ly - eoy;
                        a *= 1 - Math.Clamp(0.5f - RoundedBox(elx, ely, ehw, ehh, PickRadius(ecorners, elx, ely)), 0, 1);
                    }
                    if (clip.On && a > 0.002f) a *= clip.Coverage(pxx, py);
                    if (a > 0.002f) run.Blend(ref px[row + x], col.R * a, col.G * a, col.B * a, a);
                }
            }
        }
        run.Dispose();
        if (colVal is not null) System.Buffers.ArrayPool<float>.Shared.Return(colVal);
        if (colSet is not null) System.Buffers.ArrayPool<bool>.Shared.Return(colSet);
    }

    /// <summary>Xấp xỉ Abramowitz–Stegun 7.1.26 (sai số ~1.5e-7).</summary>
    static float Erf(float x)
    {
        float s = MathF.Sign(x); x = MathF.Abs(x);
        float t = 1f / (1f + 0.3275911f * x);
        float y = 1f - ((((1.061405429f * t - 1.453152027f) * t + 1.421413741f) * t - 0.284496736f) * t + 0.254829592f) * t * MathF.Exp(-x * x);
        return s * y;
    }
}
