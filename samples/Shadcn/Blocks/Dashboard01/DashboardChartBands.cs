using System.Globalization;
using GotX;

/// <summary>
/// Gradient dọc của một vùng tô (linearGradient x1=0 y1=0 x2=0 y2=1 theo hộp bao – objectBoundingBox – điểm dừng 5 % và
/// 95 %, nhân fillOpacity 0.6 của &lt;Area&gt;). Core chỉ tô path một màu, nên vùng được cắt thành các dải ngang cao
/// <see cref="Band"/> px (Sutherland–Hodgman theo hai cạnh ngang), mỗi dải là một path riêng tô một độ trong suốt.
/// </summary>
static class DashboardChartBands
{
    public const int Band = 3;

    public readonly record struct Strip(VectorPath Path, string Class, int Y);

    public static Strip[] Build(Point[] polygon, (float Top, float Bottom) box, float topOpacity, float width)
    {
        var strips = new List<Strip>();
        float span = Math.Max(1, box.Bottom - box.Top);
        // Hai bộ đệm dùng chung cho mọi dải (đa giác ~1000 điểm cắt cho ~100 dải: trước đây mỗi dải cấp phát 4 danh sách).
        // Mỗi lượt cắt thêm tối đa một điểm cho mỗi cạnh giao nên đầu ra ≤ 2 × đầu vào.
        var a = new Point[polygon.Length * 2 + 8];
        var b = new Point[a.Length * 2];
        for (int y0 = (int)MathF.Floor(box.Top); y0 < box.Bottom; y0 += Band)
        {
            int na = ClipEdge(polygon, a, y0, keepAbove: false);
            int n = ClipEdge(a.AsSpan(0, na), b, y0 + Band, keepAbove: true);
            if (n < 3) continue;
            for (int i = 0; i < n; i++) b[i] = new Point(b[i].X, b[i].Y - y0);
            float t = Math.Clamp((y0 + Band / 2f - box.Top) / span, 0.05f, 0.95f);
            float alpha = 0.6f * (topOpacity + (0.1f - topOpacity) * (t - 0.05f) / 0.9f);
            // Chỉ màu nằm trong class (độ trong suốt lặp lại giữa các kích thước nên trúng cache); top / h / w đặt thẳng vào Style.
            string cls = "absolute left-0 object-fill text-primary/[" + alpha.ToString("0.###", CultureInfo.InvariantCulture) + "]";
            strips.Add(new Strip(VectorPath.Polyline(b.AsSpan(0, n), width, Band, 0), cls, y0));
        }
        return [.. strips];
    }

    /// <summary>Một lượt Sutherland–Hodgman theo đường ngang y; trả số điểm ghi vào <paramref name="output"/>. Cạnh nối trùng nhau trên biên có diện tích 0.</summary>
    static int ClipEdge(ReadOnlySpan<Point> input, Point[] output, float y, bool keepAbove)
    {
        if (input.Length == 0) return 0;
        int n = 0;
        var prev = input[^1];
        bool pi = keepAbove ? prev.Y <= y : prev.Y >= y;
        foreach (var cur in input)
        {
            bool ci = keepAbove ? cur.Y <= y : cur.Y >= y;
            if (ci != pi)
            {
                float t = (y - prev.Y) / (cur.Y - prev.Y);
                output[n++] = new Point(prev.X + t * (cur.X - prev.X), y);
            }
            if (ci) output[n++] = cur;
            prev = cur;
            pi = ci;
        }
        return n;
    }
}
