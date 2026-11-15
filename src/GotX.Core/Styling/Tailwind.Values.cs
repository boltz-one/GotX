using System.Collections.Concurrent;
using System.Globalization;

namespace GotX;

public static partial class Tailwind
{
    // ====================== Giá trị ======================

    /// <summary>Bậc spacing: <c>4</c> → 1rem, <c>2.5</c>, <c>px</c>, <c>[13px]</c>.</summary>
    static bool TrySpacing(string v, bool neg, out Length l)
    {
        l = default;
        if (v == "px") l = Length.Px(1);
        else if (IsArbitrary(v)) { if (!TryLength(v[1..^1], out l)) return false; }
        else if (TryNumber(v, out float n) && n >= 0) l = n == 0 ? Length.Px(0) : Length.Rems(n * Theme.SpacingRem);
        else return false;
        if (neg) l = l.Negate();
        return true;
    }

    /// <summary>Kích thước: spacing, phân số (1/2), full, auto.</summary>
    static bool TrySize(string v, bool neg, out Length l)
    {
        switch (v)
        {
            case "full": l = Length.Full; return !neg;
            case "auto": case "fit": case "min": case "max": l = Length.Auto; return !neg;
        }
        int slash = v.IndexOf('/');
        if (slash > 0 && !IsArbitrary(v) && TryNumber(v[..slash], out float a) && TryNumber(v[(slash + 1)..], out float b) && b != 0)
        {
            l = Length.Fraction(a / b);
            if (neg) l = l.Negate();
            return true;
        }
        return TrySpacing(v, neg, out l);
    }

    static bool TryWidth(string v, out float px)
    {
        px = 0;
        if (IsArbitrary(v))
        {
            if (LooksLikeColor(v[1..^1]) || !TryLength(v[1..^1], out var l) || l.Kind != LengthKind.Px || l.Value < 0) return false;
            px = l.Value;
            return true;
        }
        return TryNumber(v, out px) && px >= 0 && float.IsInteger(px);
    }

    static bool TryRadius(string v, out float px)
    {
        if (Theme.Radius.TryGetValue(v, out px)) return true;
        if (IsArbitrary(v) && TryLength(v[1..^1], out var l) && l.Resolve(float.NaN, 16) is { } r) { px = r; return true; }
        return false;
    }

    static bool TryMs(string v, out float ms)
    {
        if (IsArbitrary(v))
        {
            string x = v[1..^1];
            if (x.EndsWith("ms")) return TryNumber(x[..^2], out ms);
            if (x.EndsWith('s') && TryNumber(x[..^1], out ms)) { ms *= 1000; return true; }
            ms = 0; return false;
        }
        return TryNumber(v, out ms) && ms >= 0;
    }

    internal static bool TryLengthValue(string s, out Length l) => TryLength(s, out l);

    static bool IsArbitrary(string v) => v.Length >= 2 && v[0] == '[' && v[^1] == ']';

    static bool LooksLikeColor(string s) => s.StartsWith('#') || s.StartsWith("rgb") || s.StartsWith("color:");

    static bool TryNumber(string s, out float n) =>
        float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out n);

    static bool TryPercentOrNumber(string s, out float n)
    {
        if (s.EndsWith('%')) { bool ok = TryNumber(s[..^1], out n); n /= 100; return ok; }
        return TryNumber(s, out n);
    }

    /// <summary>Giá trị CSS tuỳ ý: 13px, 2rem, 50%, 12 (px).</summary>
    static bool TryLength(string s, out Length l)
    {
        l = default;
        if (s.EndsWith("px") && TryNumber(s[..^2], out float px)) { l = Length.Px(px); return true; }
        if (s.EndsWith("rem") && TryNumber(s[..^3], out float rem)) { l = Length.Rems(rem); return true; }
        if (s.EndsWith('%') && TryNumber(s[..^1], out float pct)) { l = Length.Fraction(pct / 100f); return true; }
        if (TryNumber(s, out float n)) { l = Length.Px(n); return true; }
        // calc(100%-2rem): phần trăm + phần cộng px (rem quy theo 16px); có vw/vh thì để SizeOp xử lý
        if (TryCalc(s, out float frac, out float off, out float vw, out float vh) && vw == 0 && vh == 0)
        {
            l = frac != 0 ? Length.Fraction(frac) with { Offset = off } : Length.Px(off);
            return true;
        }
        return false;
    }

    /// <summary>Tách modifier sau dấu '/' cuối cùng (ngoài ngoặc vuông): "blue-500/50" → ("blue-500", "50").</summary>
    static void SplitModifier(string v, out string baseV, out string? mod)
    {
        int depth = 0;
        for (int i = v.Length - 1; i >= 0; i--)
        {
            char c = v[i];
            if (c == ']' || c == ')') depth++;
            else if (c == '[' || c == '(') depth--;
            else if (c == '/' && depth == 0) { baseV = v[..i]; mod = v[(i + 1)..]; return; }
        }
        baseV = v; mod = null;
    }

    /// <summary>Màu: tên trong theme, [#hex], [rgb(...)], kèm độ mờ /50 hoặc /[0.35].</summary>
    /// <summary>Biến thể dark của màu vừa được TryColor đọc (nếu là token ngữ nghĩa); AddOp gắn nó vào op kế tiếp.</summary>
    [ThreadStatic] static Rgba? t_dark;

    internal static bool TryColor(string v, out Rgba color)
    {
        t_dark = null;
        SplitModifier(v, out var baseV, out var mod);
        if (!TryColorBase(baseV, out color)) return false;
        if (Theme.DarkColors.TryGetValue(baseV, out var darkColor)) t_dark = darkColor;
        if (mod is null) return true;
        float a;
        if (IsArbitrary(mod)) { if (!TryPercentOrNumber(mod[1..^1], out a)) return false; }
        else if (TryNumber(mod, out a)) a /= 100f;
        else return false;
        color = color.WithAlpha(color.A * Math.Clamp(a, 0, 1));
        if (t_dark is { } dk) t_dark = dk.WithAlpha(dk.A * Math.Clamp(a, 0, 1));
        return true;
    }

    static bool TryColorBase(string v, out Rgba c)
    {
        if (Theme.Colors.TryGetValue(v, out c)) return true;
        if (IsArbitrary(v)) return TryCssColor(v[1..^1], out c);
        return false;
    }

    internal static bool TryCssColor(string s, out Rgba c)
    {
        c = default;
        if (s.StartsWith("color:")) s = s[6..];
        if (s.StartsWith('#'))
        {
            var h = s.AsSpan(1);
            if (!uint.TryParse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint x)) return false;
            switch (h.Length)
            {
                case 3: c = new Rgba(((x >> 8) & 0xF) / 15f, ((x >> 4) & 0xF) / 15f, (x & 0xF) / 15f, 1); return true;
                case 4: c = new Rgba(((x >> 12) & 0xF) / 15f, ((x >> 8) & 0xF) / 15f, ((x >> 4) & 0xF) / 15f, (x & 0xF) / 15f); return true;
                case 6: c = Rgba.FromHex(x); return true;
                case 8: c = Rgba.FromHex(x >> 8, (x & 0xFF) / 255f); return true;
            }
            return false;
        }
        if (s.StartsWith("rgb"))
        {
            int open = s.IndexOf('('), close = s.LastIndexOf(')');
            if (open < 0 || close < open) return false;
            var parts = s[(open + 1)..close].Split((char[])[',', '_', ' ', '/'], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) return false;
            float[] v = new float[4]; v[3] = 1;
            for (int i = 0; i < Math.Min(4, parts.Length); i++)
                if (!TryPercentOrNumber(parts[i], out v[i])) return false;
            c = new Rgba(v[0] / 255f, v[1] / 255f, v[2] / 255f, v[3]);
            return true;
        }
        return false;
    }
}
