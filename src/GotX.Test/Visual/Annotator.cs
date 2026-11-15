using GotX.Raster;

namespace GotX.Test;

/// <summary>Một vùng cần khoanh trên ảnh (logical px) kèm màu; <paramref name="Label"/> &gt; 0 thì in số đó ở góc.</summary>
public readonly record struct Mark(Bounds Area, uint Color, int Label = 0);

/// <summary>
/// Vẽ khung (và số thứ tự) lên một bản sao của ảnh, để agent mở một PNG là thấy ngay vị trí có vấn đề.
/// Vẽ thẳng vào pixel – không đi qua Scene nên không ảnh hưởng tới thứ đang được kiểm tra.
/// </summary>
public static class Annotator
{
    public const uint Red = 0xFFE11D48, Orange = 0xFFF59E0B, Blue = 0xFF2563EB, Magenta = 0xFFC026D3;

    // Chữ số 3×5, mỗi hàng 3 bit (bit cao = cột trái).
    static readonly byte[][] Digits =
    [
        [7, 5, 5, 5, 7], [2, 6, 2, 2, 7], [7, 1, 7, 4, 7], [7, 1, 7, 1, 7], [5, 5, 7, 1, 1],
        [7, 4, 7, 1, 7], [7, 4, 7, 5, 7], [7, 1, 1, 1, 1], [7, 5, 7, 5, 7], [7, 5, 7, 1, 7],
    ];

    public static Canvas Annotate(Canvas source, float scale, IReadOnlyList<Mark> marks)
    {
        var canvas = Pixels.Clone(source);
        int thick = Math.Max(2, (int)MathF.Round(2 * scale));
        foreach (var m in marks)
        {
            var b = m.Area.Scale(scale);
            int x0 = (int)MathF.Floor(b.X), y0 = (int)MathF.Floor(b.Y), x1 = (int)MathF.Ceiling(b.Right), y1 = (int)MathF.Ceiling(b.Bottom);
            // Hộp rỗng (cao/rộng 0) vẫn phải thấy được: nới ra tối thiểu 4 px.
            if (x1 - x0 < 4) { x0 -= 2; x1 = x0 + 4; }
            if (y1 - y0 < 4) { y0 -= 2; y1 = y0 + 4; }
            Pixels.StrokeRect(canvas, x0, y0, x1, y1, thick, m.Color);
            if (m.Label > 0) DrawLabel(canvas, x0 - thick, y0 - thick, m.Label, m.Color, Math.Max(2, (int)MathF.Round(2 * scale)));
        }
        return canvas;
    }

    /// <summary>Nhãn số nằm ngay trên góc trái-trên của khung (tụt vào trong nếu chạm mép ảnh).</summary>
    static void DrawLabel(Canvas c, int x, int y, int number, uint color, int unit)
    {
        string text = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int w = (text.Length * 4 + 1) * unit, h = 7 * unit;
        int top = y - h >= 0 ? y - h : y;
        x = Math.Clamp(x, 0, Math.Max(0, c.Width - w));
        Pixels.FillRect(c, x, top, x + w, top + h, color);
        for (int i = 0; i < text.Length; i++)
        {
            var glyph = Digits[text[i] - '0'];
            for (int row = 0; row < 5; row++)
                for (int col = 0; col < 3; col++)
                    if ((glyph[row] & (4 >> col)) != 0)
                    {
                        int px = x + (1 + i * 4 + col) * unit, py = top + (1 + row) * unit;
                        Pixels.FillRect(c, px, py, px + unit, py + unit, 0xFFFFFFFF);
                    }
        }
    }
}
