using System.Text;

namespace GotX.Text;

public sealed partial class TextSystem
{
    /// <summary>
    /// Chia chữ thành các dòng vừa <paramref name="maxWidth"/> (NaN = không giới hạn).
    /// Ngắt tại khoảng trắng; từ dài hơn một dòng thì ngắt theo ký tự; '\n' là xuống dòng cứng.
    /// Mỗi dòng gồm cả khoảng trắng cuối, nhưng Width không tính phần đó (để căn lề đúng).
    /// </summary>
    public void Wrap(string text, in TextStyle style, float maxWidth, List<TextLine> lines)
    {
        lines.Clear();
        if (float.IsNaN(maxWidth)) maxWidth = float.PositiveInfinity;
        var font = FontFor(style);

        // Cùng chuỗi, font, cỡ chữ và độ rộng thì kết quả ngắt dòng không đổi: tra cache trước.
        // (Một node chữ được wrap ít nhất hai lần mỗi lần view render: lúc đo và lúc vẽ.)
        var key = new WrapKey(text, font.Id, style.FontSize, maxWidth, style.Tracking, style.TabularNums);
        if (_wrapIndex.TryGetValue(key, out var hit))
        {
            for (int c = 0; c < hit.Count; c++) lines.Add(_wrapLines[hit.Start + c]);
            PerfCounters.WrapHit++;
            return;
        }
        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        WrapUncached(text, font, style.FontSize, maxWidth, style.LetterSpacing, style.TabularNums, lines);
        PerfCounters.WrapMiss++;
        PerfCounters.WrapTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
        if (MaxCachedWraps <= 0) return;
        if (_wrapIndex.Count >= MaxCachedWraps || _wrapChars > MaxCachedWrapChars)
        {
            _wrapIndex.Clear(); _wrapLines.Clear(); _wrapChars = 0;
        }
        _wrapIndex[key] = (_wrapLines.Count, lines.Count);
        _wrapLines.AddRange(lines);
        _wrapChars += text.Length;
    }

    readonly record struct WrapKey(string Text, int Font, float FontSize, float MaxWidth, float Tracking, bool Tnum);

    // Kết quả wrap nằm liền nhau trong một danh sách chung, nên thêm mục mới không cấp phát mảng riêng.
    readonly Dictionary<WrapKey, (int Start, int Count)> _wrapIndex = new();
    readonly List<TextLine> _wrapLines = new();
    long _wrapChars;

    /// <summary>Số kết quả ngắt dòng được nhớ (0 = tắt cache). Chạm ngưỡng thì xoá hết.</summary>
    public int MaxCachedWraps { get; set; } = 4096;

    /// <summary>Tổng số ký tự của các chuỗi được cache giữ lại; vượt quá thì xoá hết (chặn văn bản dài bị giữ nhiều bản).</summary>
    public long MaxCachedWrapChars { get; set; } = 1 << 20;

    static void WrapUncached(string text, Font font, float fontSize, float maxWidth, float spacing, bool tnum, List<TextLine> lines)
    {
        float k = fontSize / font.UnitsPerEm;
        float spaceW = font.AdvanceWidth(font.GlyphIndex(' ')) * k + spacing;
        bool kern = font.HasKerning;
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
            int j = i; float wordW = 0;
            ushort prev = 0;
            while (j < n && text[j] != ' ' && text[j] != '\n')
            {
                Rune.DecodeFromUtf16(text.AsSpan(j), out var r, out int len);
                ushort g = font.Glyph(r.Value, tnum);
                wordW += font.AdvanceWidth(g) * k + spacing + (kern ? font.Kern(prev, g) * k : 0); // khớp với lúc đo/vẽ
                prev = g;
                j += len;
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
                float x = 0;
                ushort before = 0;
                for (int p = i; p < j;)
                {
                    Rune.DecodeFromUtf16(text.AsSpan(p), out var r, out int len);
                    ushort g = font.Glyph(r.Value, tnum);
                    float a = font.AdvanceWidth(g) * k + spacing + (kern && p > lineStart ? font.Kern(before, g) * k : 0);
                    before = g;
                    if (x + a > maxWidth && p > lineStart)
                    {
                        lines.Add(new TextLine(lineStart, p - lineStart, x));
                        lineStart = p; x = 0;
                    }
                    x += a; p += len;
                }
                lineW = lineVisible = x;
            }
            else { lineW += wordW; lineVisible = lineW; }
            lineW += spaces;
            i = e;
        }
        lines.Add(new TextLine(lineStart, n - lineStart, lineVisible));
    }
}
