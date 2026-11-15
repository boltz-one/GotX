namespace GotX;

public static partial class Tailwind
{
    /// <summary>
    /// calc() tối thiểu cho giá trị tuỳ ý: tổng/hiệu các số hạng N%, Npx, Nrem, Nvw, Nvh, N (px), ví dụ
    /// <c>calc(100%-2rem)</c>, <c>calc(100vw_-_var)</c> không hỗ trợ. Cũng nhận một số hạng đơn lẻ có vw/vh (<c>80vh</c>).
    /// rem trong calc được quy ra px theo 16px (giá trị gốc của trình duyệt).
    /// </summary>
    static bool TryCalc(string s, out float frac, out float px, out float vw, out float vh)
    {
        frac = px = vw = vh = 0;
        if (s.StartsWith("calc(") && s.EndsWith(')')) s = s[5..^1];
        else if (!s.EndsWith("vw") && !s.EndsWith("vh")) return false;
        s = s.Replace("_", "").Replace(" ", "");
        if (s.Length == 0) return false;
        int start = 0;
        for (int i = 1; i <= s.Length; i++)
        {
            // dấu +/- tách số hạng (không phải dấu của số mũ 1e-3, không phải dấu ở đầu)
            if (i < s.Length && !((s[i] == '+' || s[i] == '-') && s[i - 1] != 'e' && s[i - 1] != '+' && s[i - 1] != '-')) continue;
            if (!AddTerm(s[start..i], ref frac, ref px, ref vw, ref vh)) return false;
            start = i;
        }
        return true;
    }

    static bool AddTerm(string term, ref float frac, ref float px, ref float vw, ref float vh)
    {
        if (term.StartsWith('+')) term = term[1..];
        if (term.EndsWith('%') && TryNumber(term[..^1], out float p)) { frac += p / 100; return true; }
        if (term.EndsWith("vw") && TryNumber(term[..^2], out float w)) { vw += w / 100; return true; }
        if (term.EndsWith("vh") && TryNumber(term[..^2], out float h)) { vh += h / 100; return true; }
        if (term.EndsWith("rem") && TryNumber(term[..^3], out float r)) { px += r * 16; return true; }
        if (term.EndsWith("px") && TryNumber(term[..^2], out float x)) { px += x; return true; }
        if (TryNumber(term, out float n)) { px += n; return true; }
        return false;
    }

    /// <summary>Giá trị kích thước có vw/vh: <c>w-[100vw]</c>, <c>max-h-[calc(100vh-4rem)]</c> – quy ra px lúc dựng cây.</summary>
    static bool ViewportSizeOp(string v, Prop p, Action<Prop, byte, float, Length, Rgba> emit)
    {
        if (!IsArbitrary(v) || !TryCalc(v[1..^1], out float frac, out float off, out float vw, out float vh)) return false;
        if ((vw != 0) == (vh != 0)) return false; // cần đúng một loại đơn vị viewport
        var l = frac != 0 ? Length.Fraction(frac) with { Offset = off } : Length.Px(off);
        emit(p, vw != 0 ? ViewportW : ViewportH, vw != 0 ? vw : vh, l, default);
        return true;
    }

    /// <summary>Cộng phần viewport (px) vào độ dài của op vw/vh.</summary>
    static Length AddViewport(Length l, float add) =>
        l.Kind == LengthKind.Fraction ? l with { Offset = l.Offset + add } : Length.Px((l.Kind == LengthKind.Px ? l.Value : 0) + add);
}
