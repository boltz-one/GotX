using GotX;

/// <summary>
/// Ảnh mẫu sinh bằng code (không kèm tệp nhị phân) cho avatar và ảnh xem trước của tệp đính kèm. Mỗi ảnh là một
/// <see cref="ImageSource"/> tạo một lần: ảnh giữ texture riêng nên không được tạo lại mỗi frame.
/// </summary>
public static class DemoImages
{
    /// <summary>Chân dung cách điệu trên nền chuyển màu tím → xanh.</summary>
    public static readonly ImageSource Portrait = Make(96, 96, static (x, y) =>
    {
        // Đầu (tròn) và vai (elip) màu sáng; phần còn lại là nền chuyển màu theo đường chéo.
        float head = Dist(x, y, 0.5f, 0.38f) / 0.19f, body = MathF.Sqrt(Sq((x - 0.5f) / 0.36f) + Sq((y - 1.02f) / 0.42f));
        if (head < 1 || body < 1) return (250, 240, 228);
        float t = (x + y) / 2;
        return ((int)(124 - 90 * t), (int)(92 + 80 * t), (int)(246 - 30 * t));
    });

    /// <summary>Phong cảnh: trời chuyển màu, mặt trời, hai dãy núi – ảnh ngang 4:3 để thử <c>object-cover</c>.</summary>
    public static readonly ImageSource Landscape = Make(160, 120, static (x, y) =>
    {
        if (Dist(x * 4 / 3, y, 0.95f, 0.3f) < 0.11f) return (255, 236, 150);
        float near = 0.62f + 0.2f * MathF.Abs(MathF.Sin(x * 5.1f + 0.4f)), far = 0.5f + 0.16f * MathF.Abs(MathF.Sin(x * 3.3f + 2f));
        if (y > near) return (38, 84, 72);
        if (y > far) return (72, 128, 118);
        return ((int)(96 + 120 * y), (int)(165 + 60 * y), (int)(235 - 20 * y));
    });

    static float Sq(float v) => v * v;
    static float Dist(float x, float y, float cx, float cy) => MathF.Sqrt(Sq(x - cx) + Sq(y - cy));

    /// <summary>Tô từng pixel bằng hàm màu nhận toạ độ chuẩn hoá 0..1.</summary>
    static ImageSource Make(int width, int height, Func<float, float, (int R, int G, int B)> color)
    {
        var rgba = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var (r, g, b) = color((x + 0.5f) / width, (y + 0.5f) / height);
                int i = (y * width + x) * 4;
                rgba[i] = (byte)Math.Clamp(r, 0, 255); rgba[i + 1] = (byte)Math.Clamp(g, 0, 255); rgba[i + 2] = (byte)Math.Clamp(b, 0, 255); rgba[i + 3] = 255;
            }
        return ImageSource.FromRgba(width, height, rgba);
    }
}
