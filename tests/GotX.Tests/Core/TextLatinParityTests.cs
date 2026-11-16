using System.Text;
using GotX;
using GotX.Test;
using GotX.Text;

/// <summary>
/// Chữ Latin / Việt dựng sẵn không được đổi so với bộ đo cũ (từng codepoint: glyph → kern → advance + spacing, ngắt dòng theo
/// khoảng trắng): so TextSystem mới với bản tính lại độc lập chỉ bằng API của Font. Bảo vệ fidelity so với Chrome.
/// </summary>
static class TextLatinParityTests
{
    static readonly string[] Samples =
    [
        "The quick brown fox jumps over the lazy dog. 0123456789",
        "AVATAR To Ty WA Yo Te -- ff fi fl ffi office affinity",
        "Việt Nam – Tiếng Việt có dấu: ệ ở ặ ố Ừ ữ",
        "Total: 1,234.56 USD (12.5%) #hash @user_name/path?query=1&x=2",
        "A very long unbroken_identifier_that_must_be_split_by_character_when_too_wide_for_the_line",
    ];

    static float OldMeasure(Font font, string s, in TextStyle st)
    {
        float k = st.FontSize / font.UnitsPerEm, sp = st.LetterSpacing, w = 0;
        ushort prev = 0;
        for (int i = 0; i < s.Length;)
        {
            Rune.DecodeFromUtf16(s.AsSpan(i), out var r, out int len);
            ushort g = font.Glyph(r.Value, st.TabularNums);
            if (font.HasKerning) w += font.Kern(prev, g) * k;
            w += font.AdvanceWidth(g) * k + sp;
            prev = g; i += len;
        }
        return w;
    }

    /// <summary>Thuật toán ngắt dòng cũ (chỉ ở khoảng trắng; từ quá dài ngắt theo ký tự).</summary>
    static List<(int Start, int Length)> OldWrap(Font font, string text, in TextStyle st, float maxWidth)
    {
        var lines = new List<(int, int)>();
        float k = st.FontSize / font.UnitsPerEm, spacing = st.LetterSpacing;
        float spaceW = font.AdvanceWidth(font.GlyphIndex(' ')) * k + spacing;
        int n = text.Length, lineStart = 0, i = 0;
        float lineW = 0;
        while (i < n)
        {
            int j = i; float wordW = 0; ushort prev = 0;
            while (j < n && text[j] != ' ')
            {
                ushort g = font.Glyph(text[j], st.TabularNums);
                wordW += font.AdvanceWidth(g) * k + spacing + (font.HasKerning ? font.Kern(prev, g) * k : 0);
                prev = g; j++;
            }
            int e = j; float spaces = 0;
            while (e < n && text[e] == ' ') { spaces += spaceW; e++; }
            if (lineW > 0 && lineW + wordW > maxWidth) { lines.Add((lineStart, i - lineStart)); lineStart = i; lineW = 0; }
            if (wordW > maxWidth && lineW == 0)
            {
                float x = 0; ushort before = 0;
                for (int p = i; p < j; p++)
                {
                    ushort g = font.Glyph(text[p], st.TabularNums);
                    float a = font.AdvanceWidth(g) * k + spacing + (font.HasKerning && p > lineStart ? font.Kern(before, g) * k : 0);
                    before = g;
                    if (x + a > maxWidth && p > lineStart) { lines.Add((lineStart, p - lineStart)); lineStart = p; x = 0; }
                    x += a;
                }
                lineW = x;
            }
            else lineW += wordW;
            lineW += spaces; i = e;
        }
        lines.Add((lineStart, n - lineStart));
        return lines;
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("core/text/latin-metrics-match-per-codepoint-reference", t =>
        {
            var ts = TextShapingTests.Make(false);
            if (ts is null) { t.Notes.Add("SKIP: không có segoeui.ttf"); return; }
            int checks = 0;
            foreach (var tnum in new[] { false, true })
            foreach (float size in new[] { 12f, 14f, 16f, 30f })
            foreach (float tracking in new[] { 0f, 0.05f })
            foreach (var weight in new[] { FontWeight.Normal, FontWeight.Bold })
            {
                var st = TextStyle.Default with { FontSize = size, Tracking = tracking, Weight = weight, TabularNums = tnum };
                var font = ts.FontFor(st);
                foreach (var s in Samples)
                {
                    t.Assert.Equal(ts.Measure(s, st), OldMeasure(font, s, st), $"đo '{s[..12]}…' size {size} tracking {tracking} tnum {tnum}");
                    foreach (float width in new[] { 60f, 140f, 260f })
                    {
                        var got = new List<TextLine>();
                        ts.Wrap(s, st, width, got);
                        var want = OldWrap(font, s, st, width);
                        bool same = got.Count == want.Count;
                        for (int i = 0; same && i < got.Count; i++) same = got[i].Start == want[i].Start && got[i].Length == want[i].Length;
                        t.Assert.True(same, $"ngắt dòng '{s[..12]}…' size {size} rộng {width}: {got.Count} dòng, cũ {want.Count}");
                        checks++;
                    }
                }
            }
            t.Notes.Add($"{checks} phép so ngắt dòng + đo khớp bản tham chiếu từng codepoint");
        });
    }
}
