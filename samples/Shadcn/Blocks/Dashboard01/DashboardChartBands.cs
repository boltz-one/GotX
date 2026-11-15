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

    public readonly record struct Strip(VectorPath Path, string Class);

    public static Strip[] Build(Point[] polygon, (float Top, float Bottom) box, float topOpacity, float width)
    {
        var strips = new List<Strip>();
        float span = Math.Max(1, box.Bottom - box.Top);
        for (int y0 = (int)MathF.Floor(box.Top); y0 < box.Bottom; y0 += Band)
        {
            var piece = Clip(polygon, y0, y0 + Band);
            if (piece.Count < 3) continue;
            for (int i = 0; i < piece.Count; i++) piece[i] = new Point(piece[i].X, piece[i].Y - y0);
            float t = Math.Clamp((y0 + Band / 2f - box.Top) / span, 0.05f, 0.95f);
            float alpha = 0.6f * (topOpacity + (0.1f - topOpacity) * (t - 0.05f) / 0.9f);
            string cls = "absolute left-0 object-fill text-primary/[" + alpha.ToString("0.###", CultureInfo.InvariantCulture) + "] top-["
                + y0.ToString(CultureInfo.InvariantCulture) + "px] h-[" + Band.ToString(CultureInfo.InvariantCulture) + "px] w-["
                + width.ToString("0.###", CultureInfo.InvariantCulture) + "px]";
            strips.Add(new Strip(VectorPath.Polyline(piece.ToArray(), width, Band, 0), cls));
        }
        return [.. strips];
    }

    /// <summary>Cắt đa giác vào dải lo ≤ y ≤ hi (hai lượt Sutherland–Hodgman). Cạnh nối trùng nhau trên biên có diện tích 0.</summary>
    static List<Point> Clip(Point[] poly, float lo, float hi)
    {
        var a = ClipEdge(new List<Point>(poly), lo, keepAbove: false);
        return ClipEdge(a, hi, keepAbove: true);
    }

    static List<Point> ClipEdge(List<Point> input, float y, bool keepAbove)
    {
        var output = new List<Point>(input.Count + 4);
        if (input.Count == 0) return output;
        bool Inside(Point p) => keepAbove ? p.Y <= y : p.Y >= y;
        var prev = input[^1];
        foreach (var cur in input)
        {
            bool ci = Inside(cur), pi = Inside(prev);
            if (ci != pi)
            {
                float t = (y - prev.Y) / (cur.Y - prev.Y);
                output.Add(new Point(prev.X + t * (cur.X - prev.X), y));
            }
            if (ci) output.Add(cur);
            prev = cur;
        }
        return output;
    }
}
