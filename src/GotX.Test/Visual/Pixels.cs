using GotX.Raster;

namespace GotX.Test;

/// <summary>Đọc/ghi pixel của <see cref="Canvas"/> (BGRA premultiplied, đóng gói 0xAARRGGBB).</summary>
public static class Pixels
{
    /// <summary>Màu (không premultiply) tại pixel thiết bị; ngoài canvas trả về trong suốt.</summary>
    public static Rgba Get(Canvas c, int x, int y)
    {
        if ((uint)x >= (uint)c.Width || (uint)y >= (uint)c.Height) return default;
        return Unpack(c.Pixels[y * c.Width + x]);
    }

    public static Rgba Unpack(uint p)
    {
        float a = (p >> 24) / 255f;
        if (a <= 0) return default;
        return new Rgba(((p >> 16) & 255) / 255f / a, ((p >> 8) & 255) / 255f / a, (p & 255) / 255f / a, a);
    }

    public static uint Pack(byte r, byte g, byte b) => 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;

    /// <summary>Chênh lệch lớn nhất theo kênh (0..255) giữa hai màu.</summary>
    public static int Delta(Rgba a, Rgba b)
    {
        static int Q(float v) => (int)(Math.Clamp(v, 0, 1) * 255 + 0.5f);
        return Math.Max(Math.Max(Math.Abs(Q(a.R * a.A) - Q(b.R * b.A)), Math.Abs(Q(a.G * a.A) - Q(b.G * b.A))),
            Math.Max(Math.Abs(Q(a.B * a.A) - Q(b.B * b.A)), Math.Abs(Q(a.A) - Q(b.A))));
    }

    public static Canvas Clone(Canvas src)
    {
        var copy = new Canvas();
        copy.Resize(src.Width, src.Height);
        src.Pixels.AsSpan(0, src.Width * src.Height).CopyTo(copy.Pixels);
        return copy;
    }

    /// <summary>Canvas → mảng R,G,B,A 8 bit không premultiply (cùng định dạng <see cref="PngDecoder"/> trả về).</summary>
    public static byte[] ToRgba(Canvas c)
    {
        var rgba = new byte[c.Width * c.Height * 4];
        for (int i = 0; i < c.Width * c.Height; i++)
        {
            uint p = c.Pixels[i];
            byte a = (byte)(p >> 24);
            float un = a == 0 ? 0 : 255f / a; // cùng phép bỏ premultiply với PngWriter để so khớp tuyệt đối
            rgba[i * 4] = (byte)Math.Min(255, ((p >> 16) & 0xFF) * un + 0.5f);
            rgba[i * 4 + 1] = (byte)Math.Min(255, ((p >> 8) & 0xFF) * un + 0.5f);
            rgba[i * 4 + 2] = (byte)Math.Min(255, (p & 0xFF) * un + 0.5f);
            rgba[i * 4 + 3] = a;
        }
        return rgba;
    }

    public static void FillRect(Canvas c, int x0, int y0, int x1, int y1, uint color)
    {
        x0 = Math.Max(0, x0); y0 = Math.Max(0, y0); x1 = Math.Min(c.Width, x1); y1 = Math.Min(c.Height, y1);
        for (int y = y0; y < y1; y++) c.Pixels.AsSpan(y * c.Width + x0, Math.Max(0, x1 - x0)).Fill(color);
    }

    /// <summary>Khung chữ nhật dày <paramref name="thickness"/> px, vẽ ra phía ngoài hộp [x0,x1)×[y0,y1).</summary>
    public static void StrokeRect(Canvas c, int x0, int y0, int x1, int y1, int thickness, uint color)
    {
        FillRect(c, x0 - thickness, y0 - thickness, x1 + thickness, y0, color);
        FillRect(c, x0 - thickness, y1, x1 + thickness, y1 + thickness, color);
        FillRect(c, x0 - thickness, y0, x0, y1, color);
        FillRect(c, x1, y0, x1 + thickness, y1, color);
    }
}
