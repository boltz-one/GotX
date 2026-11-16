using System.Text;

namespace GotX.Text;

/// <summary>
/// Ghép chữ Việt dạng tổ hợp (e + U+0302 + U+0323) thành dạng dựng sẵn (ệ).
/// Cần vì <c>string.Normalize</c> không hoạt động ở chế độ InvariantGlobalization, và renderer
/// chưa có GPOS để đặt dấu tổ hợp. Một số bộ gõ/ứng dụng gửi dạng tổ hợp.
/// </summary>
public static class Vietnamese
{
    const string LowerBases = "aăâeêioôơuưy", UpperBases = "AĂÂEÊIOÔƠUƯY";
    static readonly string[] Lower = ["àằầèềìòồờùừỳ", "áắấéếíóốớúứý", "ãẵẫẽễĩõỗỡũữỹ", "ảẳẩẻểỉỏổởủửỷ", "ạặậẹệịọộợụựỵ"];
    static readonly string[] Upper = ["ÀẰẦÈỀÌÒỒỜÙỪỲ", "ÁẮẤÉẾÍÓỐỚÚỨÝ", "ÃẴẪẼỄĨÕỖỠŨỮỸ", "ẢẲẨẺỂỈỎỔỞỦỬỶ", "ẠẶẬẸỆỊỌỘỢỤỰỴ"];

    static int ToneIndex(char m) => m switch
    {
        '\u0300' => 0, '\u0301' => 1, '\u0303' => 2, '\u0309' => 3, '\u0323' => 4, _ => -1,
    };

    static bool IsMark(char c) => ToneIndex(c) >= 0 || c is '\u0302' or '\u0306' or '\u031B';

    static char ApplyModifier(char b, char m) => (b, m) switch
    {
        ('a', '\u0306') => 'ă', ('a', '\u0302') => 'â', ('e', '\u0302') => 'ê',
        ('o', '\u0302') => 'ô', ('o', '\u031B') => 'ơ', ('u', '\u031B') => 'ư',
        ('A', '\u0306') => 'Ă', ('A', '\u0302') => 'Â', ('E', '\u0302') => 'Ê',
        ('O', '\u0302') => 'Ô', ('O', '\u031B') => 'Ơ', ('U', '\u031B') => 'Ư',
        _ => '\0',
    };

    public static string Compose(string s)
    {
        int first = -1;
        for (int i = 0; i < s.Length; i++) if (IsMark(s[i])) { first = i; break; }
        if (first <= 0) return s;

        var sb = new StringBuilder(s.Length);
        sb.Append(s, 0, first - 1);
        int p = first - 1;
        while (p < s.Length)
        {
            char c = s[p];
            int q = p + 1;
            while (q < s.Length && IsMark(s[q])) q++;
            if (q == p + 1) { sb.Append(c); p++; continue; }

            char b = ComposeRun(s, p, q);
            if (b != '\0') sb.Append(b); else sb.Append(s, p, q - p);
            p = q;
        }
        return sb.ToString();
    }

    /// <summary>
    /// Ghép s[p] (chữ cái gốc) với các dấu s[p+1, q) thành một ký tự dựng sẵn; '\0' nếu không ghép được.
    /// Dùng riêng khi định hình chữ (TextSystem.Shape): chỉ ghép khi font có glyph dựng sẵn, nếu không thì đặt dấu bằng GPOS.
    /// </summary>
    internal static char ComposeRun(string s, int p, int q)
    {
        char b = s[p];
        int tone = -1;
        for (int k = p + 1; k < q; k++)
        {
            int t = ToneIndex(s[k]);
            if (t >= 0) { if (tone >= 0) return '\0'; tone = t; }
            else { char nb = ApplyModifier(b, s[k]); if (nb == '\0') return '\0'; b = nb; }
        }
        if (tone < 0) return b;
        int li = LowerBases.IndexOf(b), ui = UpperBases.IndexOf(b);
        if (li >= 0) return Lower[tone][li];
        return ui >= 0 ? Upper[tone][ui] : '\0';
    }

    internal static bool IsComposingMark(char c) => IsMark(c);
}
