namespace GotX.Raster;

public static partial class SoftwareRenderer
{
    // ---------- Image: lấy mẫu song tuyến vùng nguồn, cắt theo hình chữ nhật bo góc ----------

    static void Image(Canvas c, in Primitive p, in Shape clip)
    {
        if (ImageSource.Find(p.Glyph) is not { } image) return;
        var r = Area(p, 1, c);
        var b = p.Bounds;
        if (r.Empty || b.IsEmpty) return;
        float hw = b.Width / 2, hh = b.Height / 2;
        var f = new Frame(b, p.Transform);
        var corners = ClampCorners(p.Corners, MathF.Min(hw, hh));
        var src = p.Source;
        float sx = src.Width / b.Width, sy = src.Height / b.Height, opacity = p.Color.A;
        var px = c.Pixels;
        var data = image.Pixels;
        int iw = image.Width, ih = image.Height;

        for (int y = r.Y0; y < r.Y1; y++)
        {
            int row = y * c.Width;
            for (int x = r.X0; x < r.X1; x++)
            {
                f.Local(x + 0.5f, y + 0.5f, out float lx, out float ly);
                float cov = Math.Clamp(0.5f - RoundedBox(lx, ly, hw, hh, PickRadius(corners, lx, ly)), 0, 1);
                if (clip.On && cov > 0) cov *= clip.Coverage(x + 0.5f, y + 0.5f);
                if (cov <= 0) continue;
                // Toạ độ trong ảnh (tâm texel tại +0.5), kẹp vào vùng nguồn để mép không lấy lẫn phần bị cắt
                float u = Math.Clamp(src.X + (lx + hw) * sx, src.X + 0.5f, src.Right - 0.5f) - 0.5f;
                float v = Math.Clamp(src.Y + (ly + hh) * sy, src.Y + 0.5f, src.Bottom - 0.5f) - 0.5f;
                int x0 = Math.Clamp((int)u, 0, iw - 1), y0 = Math.Clamp((int)v, 0, ih - 1);
                int x1 = Math.Min(x0 + 1, iw - 1), y1 = Math.Min(y0 + 1, ih - 1);
                float fx = Math.Clamp(u - x0, 0, 1), fy = Math.Clamp(v - y0, 0, 1);
                int i00 = (y0 * iw + x0) * 4, i10 = (y0 * iw + x1) * 4, i01 = (y1 * iw + x0) * 4, i11 = (y1 * iw + x1) * 4;
                float k = cov * opacity / 255f;
                float Mix(int ch)
                {
                    float top = data[i00 + ch] + (data[i10 + ch] - data[i00 + ch]) * fx;
                    float bot = data[i01 + ch] + (data[i11 + ch] - data[i01 + ch]) * fx;
                    return (top + (bot - top) * fy) * k;
                }
                Blend(ref px[row + x], Mix(2), Mix(1), Mix(0), Mix(3));
            }
        }
    }
}