namespace GotX.Text;

/// <summary>
/// UAX#14 rút gọn: ngoài khoảng trắng (do Wrap xử lý), cho phép ngắt dòng giữa/cạnh chữ biểu ý (CJK, kana, ký tự full-width)
/// nhưng không ngắt trước dấu đóng/dấu câu (、。）」...) và không ngắt sau dấu mở (（「...). Chữ Latin không bao giờ có cơ hội
/// ngắt thêm (giữ nguyên hành vi cũ, không ngắt sau gạch nối); Hangul ngắt theo khoảng trắng như Chrome mặc định.
/// </summary>
internal static class LineBreak
{
    enum C : byte { Other, Id, Open, Close }

    static C Class(int cp)
    {
        if (cp < 0x2E80)
            return cp is '(' or '[' or '{' or '‘' or '“' ? C.Open
                 : cp is '!' or ')' or ',' or '.' or ':' or ';' or '?' or ']' or '}' or '%' or '’' or '”' or '…' ? C.Close : C.Other;
        switch (cp)
        {
            case 0x3001 or 0x3002 or 0xFF0C or 0xFF0E or 0xFF1A or 0xFF1B or 0xFF01 or 0xFF1F or 0x30FB or 0x30FC or 0x3005:
            case 0x3009 or 0x300B or 0x300D or 0x300F or 0x3011 or 0x3015 or 0xFF09 or 0xFF3D or 0xFF5D or 0xFF60 or 0xFF63:
            case 0x3041 or 0x3043 or 0x3045 or 0x3047 or 0x3049 or 0x3063 or 0x3083 or 0x3085 or 0x3087 or 0x308E: // kana nhỏ (NS)
            case 0x30A1 or 0x30A3 or 0x30A5 or 0x30A7 or 0x30A9 or 0x30C3 or 0x30E3 or 0x30E5 or 0x30E7 or 0x30EE:
                return C.Close;
            case 0x3008 or 0x300A or 0x300C or 0x300E or 0x3010 or 0x3014 or 0xFF08 or 0xFF3B or 0xFF5B or 0xFF5F or 0xFF62:
                return C.Open;
        }
        if (cp is >= 0x2E80 and <= 0xA4CF and not (>= 0x3099 and <= 0x309C) // CJK, kana, bopomofo, yi (không gồm Hangul 0xAC00..)
            or >= 0xF900 and <= 0xFAFF or >= 0xFF00 and <= 0xFFEF or >= 0x20000 and <= 0x3FFFD) return C.Id;
        return C.Other;
    }

    /// <summary>Có thể ngắt dòng giữa hai ký tự liền nhau (không phải khoảng trắng) này không.</summary>
    public static bool CanBreakBetween(int prev, int next)
    {
        if (prev < 0x2E80 && next < 0x2E80) return false; // đường nhanh cho Latin
        C p = Class(prev), n = Class(next);
        if (n == C.Close || p == C.Open) return false;
        return p == C.Id || n == C.Id || (p == C.Close && n == C.Open);
    }
}
