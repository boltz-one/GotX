using System.Buffers;

namespace GotX.Text;

/// <summary>
/// UAX#9 rút gọn cho hiển thị: một đoạn một cấp nhúng (không có LRE/RLE/isolate), quy tắc W1–W7, N1–N2, I1–I2 và L2.
/// Đủ cho chữ Hebrew/Ả Rập (và số) nằm trong đoạn LTR. Chưa có: ký tự điều khiển hướng tường minh, ghép chữ Ả Rập
/// (init/medi/fina – xem docs/system-architecture.md), bracket pairs (N0).
/// </summary>
internal static class Bidi
{
    public enum T : byte { L, R, AL, EN, AN, ES, ET, CS, NSM, ON, WS, B }

    public static T Type(char c)
    {
        if (c < 0x590)
        {
            if (c is >= '0' and <= '9') return T.EN;
            if (c is '+' or '-') return T.ES;
            if (c is '#' or '$' or '%' or '¢' or '£' or '¤' or '¥' or '°' or '±') return T.ET;
            if (c is ',' or '.' or '/' or ':' or ' ') return T.CS;
            if (c is ' ' or '\t') return T.WS;
            if (c is '\n' or '\r') return T.B;
            if (c is >= '̀' and <= 'ͯ') return T.NSM;
            return char.IsLetter(c) ? T.L : T.ON;
        }
        if (c <= 0x5FF) return c is (>= '֑' and <= 'ֽ') or 'ֿ' or 'ׁ' or 'ׂ' or 'ׄ' or 'ׅ' or 'ׇ' ? T.NSM : T.R;
        if (c <= 0x6FF)
        {
            if (c is >= '٠' and <= '٩' or '٫' or '٬') return T.AN;
            if (c is >= '۰' and <= '۹') return T.EN;
            if (c is >= '؀' and <= '؅') return T.AN;
            if (c is '،') return T.CS;
            if (c is '٪' or '؉' or '؊') return T.ET;
            if (c is (>= 'ؐ' and <= 'ؚ') or (>= 'ً' and <= 'ٟ') or 'ٰ' or (>= 'ۖ' and <= 'ۜ')
                or (>= '۟' and <= 'ۤ') or 'ۧ' or 'ۨ' or (>= '۪' and <= 'ۭ')) return T.NSM;
            return T.AL;
        }
        if (c <= 0x7BF) return T.AL;
        if (c <= 0x85F) return T.R;
        if (c <= 0x8FF) return T.AL;
        if (c is >= ' ' and <= ' ') return T.WS;
        if (c == '‎') return T.L;
        if (c == '‏') return T.R;
        if (c is >= 'יִ' and <= 'ﭏ') return T.R;
        if (c is >= 'ﭐ' and <= '﷿' or >= 'ﹰ' and <= '﻿') return T.AL;
        return char.IsLetter(c) ? T.L : T.ON;
    }

    /// <summary>Có ký tự nào thuộc loại R/AL/AN (cần xử lý bidi) không. Vòng lặp rẻ: chữ Latin thoát ngay vì c &lt; 0x590.</summary>
    public static bool HasRtl(string s, int start, int end)
    {
        for (int i = start; i < end; i++)
        {
            char c = s[i];
            if (c >= 0x590 && (c <= 0x8FF || c is '‏' || (c >= 0xFB1D && c <= 0xFDFF) || (c >= 0xFE70 && c <= 0xFEFF))) return true;
        }
        return false;
    }

    /// <summary>Cấp bidi của từng ký tự UTF-16 trong text (đoạn LTR: baseLevel = 0).</summary>
    public static void Resolve(ReadOnlySpan<char> text, Span<byte> levels, int baseLevel = 0)
    {
        int n = text.Length;
        var t = ArrayPool<byte>.Shared.Rent(n);
        try
        {
            for (int i = 0; i < n; i++) t[i] = (byte)Type(text[i]);
            var sos = (byte)((baseLevel & 1) == 0 ? T.L : T.R);
            byte prev = sos, strong = sos;
            for (int i = 0; i < n; i++) // W1
            {
                if (t[i] == (byte)T.NSM) t[i] = prev;
                prev = t[i];
            }
            for (int i = 0; i < n; i++) // W2 + W3
            {
                if (t[i] is (byte)T.L or (byte)T.R or (byte)T.AL) strong = t[i];
                else if (t[i] == (byte)T.EN && strong == (byte)T.AL) t[i] = (byte)T.AN;
            }
            for (int i = 0; i < n; i++) if (t[i] == (byte)T.AL) t[i] = (byte)T.R;
            for (int i = 1; i < n - 1; i++) // W4
            {
                if (t[i] == (byte)T.ES && t[i - 1] == (byte)T.EN && t[i + 1] == (byte)T.EN) t[i] = (byte)T.EN;
                else if (t[i] == (byte)T.CS && t[i - 1] == t[i + 1] && t[i - 1] is (byte)T.EN or (byte)T.AN) t[i] = t[i - 1];
            }
            for (int i = 0; i < n; i++) // W5
            {
                if (t[i] != (byte)T.ET) continue;
                int j = i;
                while (j < n && t[j] == (byte)T.ET) j++;
                if ((i > 0 && t[i - 1] == (byte)T.EN) || (j < n && t[j] == (byte)T.EN))
                    for (int k = i; k < j; k++) t[k] = (byte)T.EN;
                i = j - 1;
            }
            strong = sos;
            for (int i = 0; i < n; i++) // W6 + W7
            {
                if (t[i] is (byte)T.ES or (byte)T.ET or (byte)T.CS) t[i] = (byte)T.ON;
                if (t[i] is (byte)T.L or (byte)T.R) strong = t[i];
                else if (t[i] == (byte)T.EN && strong == (byte)T.L) t[i] = (byte)T.L;
            }
            var embed = sos;
            for (int i = 0; i < n; i++) // N1 + N2
            {
                if (t[i] < (byte)T.ON) continue;
                int j = i;
                while (j < n && t[j] >= (byte)T.ON) j++;
                byte before = i == 0 ? sos : Dir(t[i - 1]), after = j == n ? sos : Dir(t[j]);
                byte d = before == after ? before : embed;
                for (int k = i; k < j; k++) t[k] = d;
                i = j - 1;
            }
            for (int i = 0; i < n; i++) // I1 + I2
            {
                int lv = baseLevel;
                if ((baseLevel & 1) == 0) lv += t[i] == (byte)T.R ? 1 : t[i] is (byte)T.EN or (byte)T.AN ? 2 : 0;
                else lv += t[i] is (byte)T.L or (byte)T.EN or (byte)T.AN ? 1 : 0;
                levels[i] = (byte)lv;
            }
        }
        finally { ArrayPool<byte>.Shared.Return(t); }
    }

    static byte Dir(byte t) => t == (byte)T.L ? (byte)T.L : (byte)T.R; // R, EN, AN đều tính là R (N1)

    /// <summary>L2: đảo các đoạn liên tiếp có cấp ≥ lvl, từ cấp cao nhất xuống cấp lẻ thấp nhất. order[] là thứ tự logic → hiển thị.</summary>
    public static void Reorder(Span<int> order, ReadOnlySpan<byte> levels)
    {
        int hi = 0, lowOdd = 255;
        foreach (byte l in levels) { if (l > hi) hi = l; if ((l & 1) == 1 && l < lowOdd) lowOdd = l; }
        for (int lv = hi; lv >= lowOdd; lv--)
            for (int i = 0; i < order.Length; i++)
            {
                if (levels[order[i]] < lv) continue;
                int j = i;
                while (j < order.Length && levels[order[j]] >= lv) j++;
                order[i..j].Reverse();
                i = j;
            }
    }

    /// <summary>Ký tự phản chiếu (dấu ngoặc...) khi nằm trong đoạn RTL; trả lại chính nó nếu không có.</summary>
    public static int Mirror(int cp) => cp switch
    {
        '(' => ')', ')' => '(', '[' => ']', ']' => '[', '{' => '}', '}' => '{', '<' => '>', '>' => '<',
        '«' => '»', '»' => '«',
        _ => cp,
    };
}
