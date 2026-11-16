namespace GotX.Text;

public sealed partial class TextSystem
{
    // Bảng từ: chữ dài được ngắt dòng ở nhiều bề rộng khác nhau (kéo giãn cửa sổ) chỉ cần quét ký tự và định hình từng từ MỘT lần;
    // mỗi bề rộng mới sau đó là một vòng cộng số học qua bảng, không đụng tới chữ. Kết quả (dòng, khoảng bề rộng) giống hệt WrapUncached:
    // cùng phép cộng float theo cùng thứ tự. Chỉ dựng ở lần trượt thứ hai của cùng chuỗi (bề rộng khác), nên chuỗi chỉ ngắt một lần không tốn bảng.
    const int WrapTableMinChars = 512, WrapTableMaxChars = 1 << 16, WrapTableSlots = 24;

    sealed class WordTable
    {
        public string Text = "";
        public int Font, ShapeGen;
        public float Size, Tracking;
        public bool Tnum;
        public int Count;
        public int[] Pos = []; // mỗi mục 3 số: đầu từ, cuối từ, cuối khoảng trắng; cuối từ = -1 là một '\n' tại đầu từ
        public float[] W = []; // mỗi mục 2 số: bề rộng từ, bề rộng các khoảng trắng theo sau
    }

    readonly WordTable?[] _wordTables = new WordTable?[WrapTableSlots];
    int _wordTableNext;

    WordTable BuildWordTable(string text, in TextStyle style, Font font)
    {
        var t = new WordTable { Text = text, Font = font.Id, ShapeGen = _shapeGen, Size = style.FontSize, Tracking = style.Tracking, Tnum = style.TabularNums };
        float size = style.FontSize, spacing = style.LetterSpacing;
        float spaceW = font.AdvanceWidth(font.GlyphIndex(' ')) * (size / font.UnitsPerEm) + spacing;
        int n = text.Length, i = 0, c = 0, cap = Math.Max(16, n / 4);
        t.Pos = new int[cap * 3]; t.W = new float[cap * 2];
        while (i < n)
        {
            if (c == cap) { cap *= 2; Array.Resize(ref t.Pos, cap * 3); Array.Resize(ref t.W, cap * 2); }
            if (text[i] == '\n') { t.Pos[c * 3] = i; t.Pos[c * 3 + 1] = -1; c++; i++; continue; }
            int j = i;
            while (j < n && text[j] != ' ' && text[j] != '\n')
            {
                j += char.IsHighSurrogate(text[j]) && j + 1 < n ? 2 : 1;
                if (j < n && text[j] != ' ' && text[j] != '\n' && (text[j] >= 0x2E80 || text[j - 1] >= 0x2E80) && BreakAt(text, j)) break;
            }
            float wordW = 0;
            if (!AsciiWordWidth(font, text, i, j, size, spacing, style.TabularNums, out wordW))
            {
                int gn = Shape(text, i, j, style, false);
                wordW = 0;
                for (int q = 0; q < gn; q++)
                {
                    ref var s = ref Glyphs[q];
                    float k = size / s.Font.UnitsPerEm;
                    wordW += s.Advance * k + (s.IsMark ? 0 : spacing) + s.Pre * k;
                }
            }
            int e = j; float spaces = 0;
            while (e < n && text[e] == ' ') { spaces += spaceW; e++; }
            t.Pos[c * 3] = i; t.Pos[c * 3 + 1] = j; t.Pos[c * 3 + 2] = e;
            t.W[c * 2] = wordW; t.W[c * 2 + 1] = spaces;
            c++; i = e;
        }
        t.Count = c;
        return t;
    }

    WordTable GetWordTable(string text, in TextStyle style, Font font)
    {
        foreach (var t in _wordTables)
            if (t is not null && ReferenceEquals(t.Text, text) && t.Font == font.Id && t.Size == style.FontSize && t.Tracking == style.Tracking
                && t.Tnum == style.TabularNums && t.ShapeGen == _shapeGen) return t;
        var built = BuildWordTable(text, style, font);
        _wordTables[_wordTableNext] = built;
        _wordTableNext = (_wordTableNext + 1) % WrapTableSlots;
        return built;
    }

    /// <summary>Cùng thuật toán <see cref="WrapUncached"/> nhưng đi qua bảng từ dựng sẵn (không quét chữ, không định hình từ thường).</summary>
    void WrapTabled(string text, in TextStyle style, float maxWidth, List<TextLine> lines)
    {
        var font = FontFor(style);
        var t = GetWordTable(text, style, font);
        float size = style.FontSize, spacing = style.LetterSpacing;
        int n = text.Length, lineStart = 0;
        float lineW = 0, lineVisible = 0;
        _whi = float.PositiveInfinity; _wexact = false;
        var pos = t.Pos; var wd = t.W;
        for (int c = 0; c < t.Count; c++)
        {
            int i = pos[c * 3], j = pos[c * 3 + 1];
            if (j < 0)
            {
                lines.Add(new TextLine(lineStart, i - lineStart, lineVisible));
                lineStart = i + 1; lineW = lineVisible = 0;
                continue;
            }
            float wordW = wd[c * 2];
            if (lineW > 0 && lineW + wordW > maxWidth)
            {
                _whi = MathF.Min(_whi, lineW + wordW);
                lines.Add(new TextLine(lineStart, i - lineStart, lineVisible));
                lineStart = i; lineW = lineVisible = 0;
            }
            if (wordW > maxWidth && lineW == 0)
            {
                _wexact = true;
                int gn = Shape(text, i, j, style, false);
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
            lineW += wd[c * 2 + 1];
        }
        lines.Add(new TextLine(lineStart, n - lineStart, lineVisible));
        float lo = 0;
        for (int q = 0; q < lines.Count; q++) lo = MathF.Max(lo, lines[q].Width);
        _wlo = lo;
        if (_wexact) { _wlo = maxWidth; _whi = float.BitIncrement(maxWidth); }
    }
}
