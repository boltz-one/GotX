using GotX.Text;

namespace GotX;

internal static partial class PathRasterizer
{
    /// <summary>
    /// Nét = HỢP các capsule (đoạn + đầu tròn) quanh đa tuyến: độ phủ mỗi pixel lấy từ KHOẢNG CÁCH tới đa tuyến (min qua mọi đoạn), không cộng dồn
    /// diện tích từng hình – cộng dồn làm các pixel mép nằm trong nhiều hình chồng nhau bị đếm lặp, nét 1.3 px trông như 2 px.
    /// coverage = clamp(half − d + 0.5): với mép thẳng bằng đúng diện tích phủ của pixel; đầu tròn / khớp nối tròn có sẵn.
    /// </summary>
    static byte[] StrokeAlpha(VectorPath path, int w, int h, float sx, float sy, float half)
    {
        var d2 = System.Buffers.ArrayPool<float>.Shared.Rent(w * h);
        try
        {
            Array.Fill(d2, float.MaxValue, 0, w * h);
            var pts = path.Points;
            foreach (var (start, count, closed) in path.Contours)
            {
                int segments = closed ? count : count - 1;
                if (count == 1) Segment(d2, w, h, pts[2 * start] * sx + Margin, pts[2 * start + 1] * sy + Margin, pts[2 * start] * sx + Margin, pts[2 * start + 1] * sy + Margin, half);
                for (int i = 0; i < segments; i++)
                {
                    int a = start + i, b = start + (i + 1) % count;
                    Segment(d2, w, h, pts[2 * a] * sx + Margin, pts[2 * a + 1] * sy + Margin, pts[2 * b] * sx + Margin, pts[2 * b + 1] * sy + Margin, half);
                }
            }
            var alpha = new byte[w * h];
            for (int i = 0; i < alpha.Length; i++)
            {
                if (d2[i] == float.MaxValue) continue;
                float cov = Math.Clamp(half - MathF.Sqrt(d2[i]) + 0.5f, 0, 1);
                alpha[i] = (byte)(cov * 255f + 0.5f);
            }
            return alpha;
        }
        finally { System.Buffers.ArrayPool<float>.Shared.Return(d2); }
    }

    static void Segment(float[] d2, int w, int h, float ax, float ay, float bx, float by, float half)
    {
        float pad = half + 1;
        int x0 = Math.Max(0, (int)MathF.Floor(MathF.Min(ax, bx) - pad)), x1 = Math.Min(w, (int)MathF.Ceiling(MathF.Max(ax, bx) + pad));
        int y0 = Math.Max(0, (int)MathF.Floor(MathF.Min(ay, by) - pad)), y1 = Math.Min(h, (int)MathF.Ceiling(MathF.Max(ay, by) + pad));
        float dx = bx - ax, dy = by - ay, len2 = dx * dx + dy * dy;
        for (int y = y0; y < y1; y++)
        {
            float py = y + 0.5f;
            for (int x = x0; x < x1; x++)
            {
                float px = x + 0.5f;
                float t = len2 > 1e-12f ? Math.Clamp(((px - ax) * dx + (py - ay) * dy) / len2, 0, 1) : 0;
                float ex = px - (ax + t * dx), ey = py - (ay + t * dy);
                float dd = ex * ex + ey * ey;
                ref float cur = ref d2[y * w + x];
                if (dd < cur) cur = dd;
            }
        }
    }
}
