using GotX.Text;

namespace GotX;

/// <summary>
/// Raster một <see cref="VectorPath"/> thành mask alpha <paramref name="width"/>×<paramref name="height"/> pixel
/// (thêm lề 1px mỗi phía cho anti-alias). Dùng chung bộ tích luỹ diện tích với glyph.
/// Nét được dựng thành hợp của các hình cùng chiều quay (hình chữ nhật cho mỗi đoạn + hình tròn ở mỗi đỉnh):
/// phần chồng nhau cộng dồn rồi bị kẹp về 1 nên cho ra đầu tròn và khớp nối tròn mà không cần tính giao.
/// </summary>
internal static partial class PathRasterizer
{
    const int Margin = 1;

    public static GlyphMask Rasterize(VectorPath path, int width, int height, GlyphRasterizer raster)
    {
        var mask = new GlyphMask();
        int w = width + 2 * Margin, h = height + 2 * Margin;
        if (width <= 0 || height <= 0 || w > 2048 || h > 2048 || path.Width <= 0 || path.Height <= 0) return mask;
        float sx = width / path.Width, sy = height / path.Height;
        float half = path.StrokeWidth * MathF.Sqrt(sx * sy) / 2;
        if (path.StrokeWidth > 0)
        {
            mask.Width = w; mask.Height = h; mask.Left = -Margin; mask.Top = -Margin;
            mask.Alpha = StrokeAlpha(path, w, h, sx, sy, half);
            return mask;
        }
        raster.Reset(w, h);
        var pts = path.Points;
        foreach (var (start, count, _) in path.Contours)
        {
            for (int i = 0; i < count; i++)
            {
                float x = pts[2 * (start + i)] * sx + Margin, y = pts[2 * (start + i) + 1] * sy + Margin;
                if (i == 0) raster.MoveTo(x, y); else raster.LineTo(x, y);
            }
            raster.Close();
        }
        mask.Width = w; mask.Height = h; mask.Left = -Margin; mask.Top = -Margin;
        mask.Alpha = raster.ToAlpha();
        return mask;
    }
}
