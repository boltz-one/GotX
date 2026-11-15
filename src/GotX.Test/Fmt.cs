using System.Globalization;

namespace GotX.Test;

/// <summary>Định dạng số/hộp/màu ngắn gọn và ổn định (không phụ thuộc culture) cho mọi thông báo.</summary>
public static class Fmt
{
    public static string N(float v) => (MathF.Abs(v) < 0.005f ? 0 : v).ToString("0.##", CultureInfo.InvariantCulture);
    public static string N(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>Hộp dạng <c>[x,y w×h]</c> (logical px).</summary>
    public static string B(Bounds b) => $"[{N(b.X)},{N(b.Y)} {N(b.Width)}×{N(b.Height)}]";

    public static string P(Point p) => $"({N(p.X)},{N(p.Y)})";

    /// <summary>Màu dạng <c>#RRGGBB</c>, thêm <c>/aa</c> khi không đặc.</summary>
    public static string C(Rgba c)
    {
        static int Q(float v) => (int)(Math.Clamp(v, 0, 1) * 255 + 0.5f);
        string hex = $"#{Q(c.R):X2}{Q(c.G):X2}{Q(c.B):X2}";
        return c.A >= 0.999f ? hex : $"{hex}/{N(c.A)}";
    }

    public static Point Center(Bounds b) => new(b.X + b.Width / 2, b.Y + b.Height / 2);

    /// <summary>Cắt chuỗi dài, thêm dấu ba chấm.</summary>
    public static string Cut(string s, int max) => s.Length <= max ? s : s[..max] + "…";

    public static string Bytes(long n) => n < 1024 ? $"{n} B" : n < 1 << 20 ? $"{N(n / 1024.0)} KB" : $"{N(n / 1048576.0)} MB";
}
