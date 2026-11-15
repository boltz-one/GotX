using System.Globalization;
using System.Text;

namespace GotX;

/// <summary>
/// Hình vector một màu (icon, đường biểu đồ): path SVG đã làm phẳng thành các đường gấp khúc trong hệ toạ độ
/// viewBox. Vẽ bằng màu chữ hiện tại (currentColor). Được raster thành mask alpha theo đúng kích thước pixel
/// lúc vẽ rồi nằm chung atlas với glyph, nên cả renderer CPU lẫn GPU vẽ nó như một glyph.
/// Tạo một lần rồi giữ lại (static readonly): mỗi instance có mask riêng trong atlas.
/// </summary>
public sealed class VectorPath
{
    static int s_nextId;

    internal readonly float[] Points;                              // x0, y0, x1, y1…
    internal readonly (int Start, int Count, bool Closed)[] Contours; // Start/Count tính theo điểm

    VectorPath(float width, float height, float strokeWidth, float[] points, (int, int, bool)[] contours)
    {
        Id = Interlocked.Increment(ref s_nextId);
        Width = width; Height = height; StrokeWidth = strokeWidth;
        Points = points; Contours = contours;
    }

    public int Id { get; }
    /// <summary>Kích thước viewBox.</summary>
    public float Width { get; }
    public float Height { get; }
    /// <summary>Độ dày nét theo đơn vị viewBox; 0 = tô kín.</summary>
    public float StrokeWidth { get; }

    /// <summary>Hình tô kín từ thuộc tính <c>d</c> của SVG.</summary>
    public static VectorPath Fill(string d, float width = 24, float height = 24) => Build([d], width, height, 0);

    /// <summary>Hình nét (đầu và khớp nối tròn, như icon Lucide) từ thuộc tính <c>d</c> của SVG.</summary>
    public static VectorPath Stroke(string d, float strokeWidth = 2, float width = 24, float height = 24) =>
        Build([d], width, height, strokeWidth);

    /// <summary>
    /// Icon nét ghép từ nhiều phần tử SVG, mỗi phần là một chuỗi <c>d</c> hoặc dạng tắt:
    /// <c>"circle cx cy r"</c>, <c>"ellipse cx cy rx ry"</c>, <c>"line x1 y1 x2 y2"</c>,
    /// <c>"rect x y w h [rx]"</c>, <c>"polyline x,y x,y…"</c>, <c>"polygon x,y x,y…"</c>.
    /// </summary>
    public static VectorPath Icon(params string[] parts) => Build(parts, 24, 24, 2);

    /// <summary>Như <see cref="Icon(string[])"/> nhưng tô kín.</summary>
    public static VectorPath FilledIcon(params string[] parts) => Build(parts, 24, 24, 0);

    /// <summary>Đường gấp khúc qua các điểm (đường của biểu đồ). <paramref name="strokeWidth"/> = 0 thì tô kín đa giác.</summary>
    public static VectorPath Polyline(ReadOnlySpan<Point> points, float width, float height, float strokeWidth, bool closed = false)
    {
        var pts = new float[points.Length * 2];
        for (int i = 0; i < points.Length; i++) { pts[2 * i] = points[i].X; pts[2 * i + 1] = points[i].Y; }
        return new VectorPath(width, height, strokeWidth, pts, [(0, points.Length, closed || strokeWidth <= 0)]);
    }

    static VectorPath Build(string[] parts, float width, float height, float strokeWidth)
    {
        var flat = new PathFlattener(MathF.Max(width, height) / 400f);
        // Mỗi phần là một phần tử SVG riêng: điểm hiện tại bắt đầu lại từ gốc, nên lệnh "m" tương đối mở đầu
        // một phần được hiểu là tuyệt đối (như mỗi thẻ <path> trong SVG), không nối theo điểm cuối của phần trước.
        foreach (string part in parts)
        {
            PathParser.Parse(Shorthand(part.Trim()), flat);
            flat.End();
        }
        return new VectorPath(width, height, strokeWidth, flat.Points.ToArray(), flat.Contours.ToArray());
    }

    /// <summary>Đổi phần tử SVG dạng tắt thành path; chuỗi <c>d</c> thì trả nguyên.</summary>
    static string Shorthand(string s)
    {
        int sp = s.IndexOf(' ');
        if (sp <= 0) return s;
        string kind = s[..sp];
        if (kind is not ("circle" or "ellipse" or "line" or "rect" or "polyline" or "polygon")) return s;
        var parts = s[(sp + 1)..].Split((char[])[' ', ','], StringSplitOptions.RemoveEmptyEntries);
        var n = new float[parts.Length];
        for (int i = 0; i < n.Length; i++) n[i] = float.Parse(parts[i], CultureInfo.InvariantCulture);
        static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        switch (kind)
        {
            case "circle": return Ellipse(n[0], n[1], n[2], n[2]);
            case "ellipse": return Ellipse(n[0], n[1], n[2], n[3]);
            case "line": return $"M{F(n[0])} {F(n[1])}L{F(n[2])} {F(n[3])}";
            case "rect":
            {
                float x = n[0], y = n[1], w = n[2], h = n[3], r = n.Length > 4 ? MathF.Min(n[4], MathF.Min(w, h) / 2) : 0;
                if (r <= 0) return $"M{F(x)} {F(y)}h{F(w)}v{F(h)}h{F(-w)}Z";
                return $"M{F(x + r)} {F(y)}h{F(w - 2 * r)}a{F(r)} {F(r)} 0 0 1 {F(r)} {F(r)}v{F(h - 2 * r)}a{F(r)} {F(r)} 0 0 1 {F(-r)} {F(r)}" +
                       $"h{F(2 * r - w)}a{F(r)} {F(r)} 0 0 1 {F(-r)} {F(-r)}v{F(2 * r - h)}a{F(r)} {F(r)} 0 0 1 {F(r)} {F(-r)}Z";
            }
            default:
            {
                var sb = new StringBuilder();
                for (int i = 0; i + 1 < n.Length; i += 2) sb.Append(i == 0 ? 'M' : 'L').Append(F(n[i])).Append(' ').Append(F(n[i + 1]));
                if (kind == "polygon") sb.Append('Z');
                return sb.ToString();
            }
        }
        static string Ellipse(float cx, float cy, float rx, float ry) =>
            $"M{F(cx - rx)} {F(cy)}a{F(rx)} {F(ry)} 0 1 0 {F(2 * rx)} 0a{F(rx)} {F(ry)} 0 1 0 {F(-2 * rx)} 0Z";
    }
}
