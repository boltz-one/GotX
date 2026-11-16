using System.Globalization;

namespace GotX.Text;

public sealed partial class TextSystem
{
    /// <summary>Độ rộng một dòng chữ (logical px), không xuống dòng.</summary>
    public float Measure(string text, in TextStyle style) => MeasureRange(text, 0, text.Length, style);

    /// <summary>
    /// Độ rộng đoạn [start, end) theo đơn vị UTF-16 (gồm cả letter-spacing sau mỗi ký tự). Nếu end rơi vào giữa một cụm
    /// (ligature, ký tự ghép) thì tính theo tỉ lệ ký tự trong cụm – như caret trong ligature của trình duyệt.
    /// Với font có ligature/ccmp, vùng định hình được kéo dài tới hết "từ" (≤ 24 ký tự) để biết cụm bị cắt ngang hay không.
    /// </summary>
    public float MeasureRange(string text, int start, int end, in TextStyle style)
    {
        if (end <= start) return 0;
        int ext = end;
        if (end < text.Length && FontFor(style).HasLigatures)
        {
            int lim = Math.Min(text.Length, end + 24);
            while (ext < lim && text[ext] != ' ' && text[ext] != '\n') ext++;
        }
        int n = Shape(text, start, ext, style, false);
        float sp = style.LetterSpacing, size = style.FontSize, w = 0, k = 0;
        Font? kf = null;
        for (int i = 0; i < n; i++)
        {
            ref var s = ref Glyphs[i];
            if (s.Cluster >= end) break;
            if (s.Font != kf) { kf = s.Font; k = size / kf.UnitsPerEm; }
            w += s.Pre * k;
            if (s.ClusterEnd > end) { w += s.Advance * k * ((end - s.Cluster) / (float)(s.ClusterEnd - s.Cluster)); break; }
            w += s.Advance * k + (s.IsMark ? 0 : sp);
        }
        return w;
    }

    /// <summary>Vị trí xa nhất (ranh giới cụm) trong [start, end) mà đoạn [start, vị trí) vẫn rộng không quá maxWidth.</summary>
    public int FitIndex(string text, int start, int end, float maxWidth, in TextStyle style)
    {
        if (end <= start) return end;
        int n = Shape(text, start, end, style, false);
        float sp = style.LetterSpacing, size = style.FontSize, w = 0;
        for (int i = 0; i < n;)
        {
            int j = i + 1;
            while (j < n && Glyphs[j].IsMark) j++;
            float a = 0;
            for (int q = i; q < j; q++)
            {
                ref var s = ref Glyphs[q];
                float k = size / s.Font.UnitsPerEm;
                a += s.Advance * k + (s.IsMark ? 0 : sp) + s.Pre * k;
            }
            if (w + a > maxWidth) return Glyphs[i].Cluster;
            w += a; i = j;
        }
        return end;
    }

    /// <summary>
    /// Vị trí (UTF-16, theo ranh giới grapheme) gần toạ độ x (tính từ mép trái dòng) nhất trong [start, end).
    /// Có bidi: toạ độ là toạ độ hiển thị, kết quả là vị trí logic.
    /// </summary>
    public int IndexAtX(string text, int start, int end, float x, in TextStyle style)
    {
        if (end <= start) return end;
        int n = Shape(text, start, end, style, true);
        int best = end;
        float bestD = float.MaxValue, cur = 0;
        for (int i = 0; i < n;)
        {
            Group(i, n, style, ref cur, out int j, out float left, out float right, out int c0, out int c1);
            bool rtl = (Glyphs[i].Level & 1) == 1;
            for (int p = c0; ; )
            {
                float d = MathF.Abs(x - EdgeX(left, right, c0, c1, p, rtl));
                if (d <= bestD) { bestD = d; best = p; }
                if (p >= c1) break;
                p = Math.Min(p + Math.Max(1, StringInfo.GetNextTextElementLength(text, p)), c1);
            }
            i = j;
        }
        return best;
    }

    /// <summary>Toạ độ x hiển thị của vị trí caret <paramref name="pos"/> trong dòng [start, end) (đúng cả với ligature và bidi).</summary>
    public float CaretX(string text, int start, int end, int pos, in TextStyle style)
    {
        if (end <= start) return 0;
        int n = Shape(text, start, end, style, true);
        float cur = 0, lastX = 0;
        int lastEnd = -1;
        for (int i = 0; i < n;)
        {
            Group(i, n, style, ref cur, out int j, out float left, out float right, out int c0, out int c1);
            bool rtl = (Glyphs[i].Level & 1) == 1;
            if (pos >= c0 && pos < c1) return EdgeX(left, right, c0, c1, pos, rtl);
            if (c1 > lastEnd) { lastEnd = c1; lastX = EdgeX(left, right, c0, c1, c1, rtl); }
            i = j;
        }
        return lastX;
    }

    static float EdgeX(float left, float right, int c0, int c1, int p, bool rtl)
    {
        float t = c1 == c0 ? 0 : (p - c0) / (float)(c1 - c0);
        return rtl ? right - t * (right - left) : left + t * (right - left);
    }

    /// <summary>Nhóm glyph (chữ gốc + dấu) bắt đầu ở i: hộp hiển thị [left, right] và khoảng UTF-16 [c0, c1].</summary>
    void Group(int i, int n, in TextStyle style, ref float cur, out int j, out float left, out float right, out int c0, out int c1)
    {
        j = i + 1;
        while (j < n && Glyphs[j].IsMark) j++;
        float k0 = style.FontSize / Glyphs[i].Font.UnitsPerEm;
        left = cur + Glyphs[i].Pre * k0;
        float w = 0;
        for (int q = i; q < j; q++) w += Glyphs[q].Advance * (style.FontSize / Glyphs[q].Font.UnitsPerEm) + (Glyphs[q].IsMark ? 0 : style.LetterSpacing);
        right = left + w; cur = right;
        c0 = Glyphs[i].Cluster; c1 = Glyphs[j - 1].ClusterEnd;
    }
}
