using GotX.Text;

namespace GotX;

/// <summary>
/// Raster một <see cref="VectorPath"/> thành mask alpha <paramref name="width"/>×<paramref name="height"/> pixel
/// (thêm lề 1px mỗi phía cho anti-alias). Dùng chung bộ tích luỹ diện tích với glyph.
/// Nét được dựng thành hợp của các hình cùng chiều quay (hình chữ nhật cho mỗi đoạn + hình tròn ở mỗi đỉnh):
/// phần chồng nhau cộng dồn rồi bị kẹp về 1 nên cho ra đầu tròn và khớp nối tròn mà không cần tính giao.
/// </summary>
internal static class PathRasterizer
{
    const int Margin = 1;

    public static GlyphMask Rasterize(VectorPath path, int width, int height, GlyphRasterizer raster)
    {
        var mask = new GlyphMask();
        int w = width + 2 * Margin, h = height + 2 * Margin;
        if (width <= 0 || height <= 0 || w > 2048 || h > 2048 || path.Width <= 0 || path.Height <= 0) return mask;
        float sx = width / path.Width, sy = height / path.Height;
        raster.Reset(w, h);
        var pts = path.Points;
        float half = path.StrokeWidth * MathF.Sqrt(sx * sy) / 2;
        foreach (var (start, count, closed) in path.Contours)
        {
            if (path.StrokeWidth <= 0)
            {
                for (int i = 0; i < count; i++)
                {
                    float x = pts[2 * (start + i)] * sx + Margin, y = pts[2 * (start + i) + 1] * sy + Margin;
                    if (i == 0) raster.MoveTo(x, y); else raster.LineTo(x, y);
                }
                raster.Close();
                continue;
            }
            int segments = closed ? count : count - 1;
            for (int i = 0; i < count; i++)
            {
                float ax = pts[2 * (start + i)] * sx + Margin, ay = pts[2 * (start + i) + 1] * sy + Margin;
                Disc(raster, ax, ay, half);
                if (i >= segments) continue;
                int j = start + (i + 1) % count;
                Segment(raster, ax, ay, pts[2 * j] * sx + Margin, pts[2 * j + 1] * sy + Margin, half);
            }
        }
        mask.Width = w; mask.Height = h; mask.Left = -Margin; mask.Top = -Margin;
        mask.Alpha = raster.ToAlpha();
        return mask;
    }

    static void Segment(GlyphRasterizer r, float ax, float ay, float bx, float by, float half)
    {
        float dx = bx - ax, dy = by - ay, len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 1e-4f) return;
        float nx = -dy / len * half, ny = dx / len * half;
        r.MoveTo(ax + nx, ay + ny);
        r.LineTo(bx + nx, by + ny);
        r.LineTo(bx - nx, by - ny);
        r.LineTo(ax - nx, ay - ny);
        r.Close();
    }

    /// <summary>Đa giác xấp xỉ hình tròn, cùng chiều quay với <see cref="Segment"/> (góc giảm dần).</summary>
    static void Disc(GlyphRasterizer r, float cx, float cy, float radius)
    {
        int n = Math.Clamp((int)MathF.Ceiling(radius * 4), 8, 40);
        for (int i = 0; i < n; i++)
        {
            float a = -2 * MathF.PI * i / n;
            float x = cx + MathF.Cos(a) * radius, y = cy + MathF.Sin(a) * radius;
            if (i == 0) r.MoveTo(x, y); else r.LineTo(x, y);
        }
        r.Close();
    }
}
