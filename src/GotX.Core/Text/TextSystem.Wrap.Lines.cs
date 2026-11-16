namespace GotX.Text;

public sealed partial class TextSystem
{
    /// <summary>
    /// Ngắt dòng không qua cache. Đơn vị ngắt: từ (tới khoảng trắng) và, nhờ <see cref="LineBreak"/> (UAX#14 rút gọn),
    /// từng chữ biểu ý CJK; chữ Latin chỉ ngắt ở khoảng trắng như trước. Bề rộng lấy từ <see cref="Shape"/> nên khớp
    /// với lúc đo/vẽ (ligature, font dự phòng, dấu kết hợp).
    /// </summary>
    void WrapUncached(string text, in TextStyle style, float maxWidth, List<TextLine> lines)
    {
        var font = FontFor(style);
        float size = style.FontSize, spacing = style.LetterSpacing;
        float spaceW = font.AdvanceWidth(font.GlyphIndex(' ')) * (size / font.UnitsPerEm) + spacing;
        int n = text.Length, lineStart = 0, i = 0;
        float lineW = 0, lineVisible = 0;

        while (i < n)
        {
            if (text[i] == '\n')
            {
                lines.Add(new TextLine(lineStart, i - lineStart, lineVisible));
                lineStart = ++i; lineW = lineVisible = 0;
                continue;
            }
            int j = i;
            while (j < n && text[j] != ' ' && text[j] != '\n')
            {
                j += char.IsHighSurrogate(text[j]) && j + 1 < n ? 2 : 1;
                if (j < n && text[j] != ' ' && text[j] != '\n' && (text[j] >= 0x2E80 || text[j - 1] >= 0x2E80) && BreakAt(text, j)) break;
            }
            int gn = -1; // số glyph chỉ tính khi cần (từ quá dài phải ngắt giữa chừng)
            float wordW = 0;
            if (!AsciiWordWidth(font, text, i, j, size, spacing, style.TabularNums, out wordW))
            {
                gn = Shape(text, i, j, style, false);
                wordW = 0;
                for (int q = 0; q < gn; q++)
                {
                    ref var s = ref Glyphs[q];
                    float k = size / s.Font.UnitsPerEm;
                    wordW += s.Advance * k + (s.IsMark ? 0 : spacing) + s.Pre * k; // khớp với lúc đo/vẽ
                }
            }
            int e = j; float spaces = 0;
            while (e < n && text[e] == ' ') { spaces += spaceW; e++; }

            if (lineW > 0 && lineW + wordW > maxWidth)
            {
                lines.Add(new TextLine(lineStart, i - lineStart, lineVisible));
                lineStart = i; lineW = lineVisible = 0;
            }
            if (wordW > maxWidth && lineW == 0)
            {
                if (gn < 0) gn = Shape(text, i, j, style, false);
                float x = 0;
                for (int q = 0; q < gn;)
                {
                    int g = q + 1;
                    while (g < gn && Glyphs[g].IsMark) g++;
                    int p = Glyphs[q].Cluster;
                    float a = 0;
                    for (int m = q; m < g; m++)
                    {
                        ref var s = ref Glyphs[m];
                        float k = size / s.Font.UnitsPerEm;
                        a += s.Advance * k + (s.IsMark ? 0 : spacing) + (p > lineStart ? s.Pre * k : 0);
                    }
                    if (x + a > maxWidth && p > lineStart)
                    {
                        lines.Add(new TextLine(lineStart, p - lineStart, x));
                        lineStart = p; x = 0;
                    }
                    x += a; q = g;
                }
                lineW = lineVisible = x;
            }
            else { lineW += wordW; lineVisible = lineW; }
            lineW += spaces;
            i = e;
        }
        lines.Add(new TextLine(lineStart, n - lineStart, lineVisible));
    }

    /// <summary>
    /// Đường nhanh cho từ ASCII in được mà font không có glyph cần định hình (ligature): cộng advance + kerning thẳng,
    /// cùng công thức với <see cref="Shape"/>. Trả false nếu phải đi qua Shape.
    /// </summary>
    static bool AsciiWordWidth(Font font, string text, int i, int j, float size, float spacing, bool tnum, out float width)
    {
        width = 0;
        if (j <= i) return true;
        float k = size / font.UnitsPerEm, w = 0;
        bool kern = font.HasKerning;
        ushort prev = 0;
        for (int q = i; q < j; q++)
        {
            int cp = text[q];
            if ((uint)(cp - 0x20) > 0x5E) return false;
            ushort g = font.Glyph(cp, tnum);
            if (g == 0 || font.MayShape(g)) return false;
            if (kern && prev != 0) w += font.Kern(prev, g) * k;
            w += font.AdvanceWidth(g) * k + spacing;
            prev = g;
        }
        width = w;
        return true;
    }

    static bool BreakAt(string text, int j)
    {
        int next = char.IsHighSurrogate(text[j]) && j + 1 < text.Length ? char.ConvertToUtf32(text[j], text[j + 1]) : text[j];
        if (char.IsLowSurrogate(text[j])) return false;
        int prev = char.IsLowSurrogate(text[j - 1]) && j >= 2 ? char.ConvertToUtf32(text[j - 2], text[j - 1]) : text[j - 1];
        return LineBreak.CanBreakBetween(prev, next);
    }
}
