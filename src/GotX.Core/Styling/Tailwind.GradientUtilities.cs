namespace GotX;

public static partial class Tailwind
{
    // Mã hoá hướng gradient vào UtilityOp.E: bit 0-1 = loại, bit 2 = oklab, bit 3-4 / 5-6 = dấu góc đích theo x / y.
    const byte GradOklab = 4;

    static readonly (string Name, float Angle, sbyte X, sbyte Y)[] GradientSides =
    [
        ("t", 0, 0, 0), ("r", 90, 0, 0), ("b", 180, 0, 0), ("l", 270, 0, 0),
        ("tr", 0, 1, -1), ("br", 0, 1, 1), ("bl", 0, -1, 1), ("tl", 0, -1, -1),
    ];

    static byte SignCode(sbyte s) => s > 0 ? (byte)1 : s < 0 ? (byte)2 : (byte)0;
    static sbyte CodeSign(int c) => c == 1 ? (sbyte)1 : c == 2 ? (sbyte)-1 : (sbyte)0;

    /// <summary>
    /// Gradient kiểu Tailwind v4: <c>bg-linear-to-r</c>, <c>bg-linear-45</c>, <c>-bg-linear-45</c>, <c>bg-linear-[25deg]</c>,
    /// <c>bg-gradient-to-r</c> (tên v3, v4 vẫn giữ), <c>bg-radial</c>, <c>bg-none</c>; điểm dừng <c>from-*</c>, <c>via-*</c>,
    /// <c>to-*</c> (màu, <c>/opacity</c>, <c>10%</c>, <c>[20px]</c>), <c>via-none</c>. Dạng có tên nội suy "in oklab",
    /// dạng góc tuỳ ý thì sRGB – đúng như CSS Tailwind sinh ra. Modifier <c>/srgb</c>, <c>/oklab</c> đổi không gian màu.
    /// </summary>
    static bool GradientUtility(string u, bool neg, UtilityOp t, List<UtilityOp> ops)
    {
        void Emit(Prop p, byte e = 0, float f = 0, Length l = default, Rgba c = default) => AddOp(ops, t, p, e, f, l, c);

        if (u == "bg-none" && !neg) { Emit(Prop.Gradient, (byte)GradientKind.None); return true; }
        if (Prefix(u, "bg-gradient-to-", out var v)) return !neg && LinearTo(v, false, Emit);
        if (u.StartsWith("bg-radial", StringComparison.Ordinal))
        {
            if (neg) return false;
            if (u == "bg-radial") { Emit(Prop.Gradient, (byte)GradientKind.Radial | GradOklab); return true; }
            return u.StartsWith("bg-radial/") && TrySpace(u[10..], out bool ok) && Done(() => Emit(Prop.Gradient, (byte)((byte)GradientKind.Radial | (ok ? GradOklab : 0))));
        }
        if (Prefix(u, "bg-linear-", out v))
        {
            if (IsArbitrary(v))
            {
                // bg-linear-[25deg]: CSS sinh "linear-gradient(25deg, …)" không kèm "in oklab" → nội suy sRGB
                if (!TryAngle(v[1..^1], out float deg)) return false;
                Emit(Prop.Gradient, (byte)GradientKind.Linear, neg ? -deg : deg);
                return true;
            }
            SplitModifier(v, out var baseV, out var mod);
            bool oklab = true;
            if (mod is not null && !TrySpace(mod, out oklab)) return false;
            if (baseV.StartsWith("to-")) return !neg && LinearTo(v[3..], true, Emit);
            if (!int.TryParse(baseV, out int angle) || angle < 0) return false;
            Emit(Prop.Gradient, (byte)((byte)GradientKind.Linear | (oklab ? GradOklab : 0)), neg ? -angle : angle);
            return true;
        }
        if (neg) return false;
        if (u == "via-none") { Emit(Prop.GradVia, 1); return true; }
        if (Prefix(u, "from-", out v)) return StopOp(v, Prop.GradFrom, 0, Emit);
        if (Prefix(u, "via-", out v)) return StopOp(v, Prop.GradVia, 1, Emit);
        if (Prefix(u, "to-", out v)) return StopOp(v, Prop.GradTo, 2, Emit);
        return false;
    }

    /// <summary>to-r, to-tr… (kèm modifier không gian màu nếu có). bg-gradient-to-* của v4 cũng là "to … in oklab".</summary>
    static bool LinearTo(string v, bool allowModifier, Action<Prop, byte, float, Length, Rgba> emit)
    {
        SplitModifier(v, out var side, out var mod);
        bool oklab = true;
        if (mod is not null && (!allowModifier || !TrySpace(mod, out oklab))) return false;
        foreach (var (name, angle, x, y) in GradientSides)
        {
            if (name != side) continue;
            byte e = (byte)((byte)GradientKind.Linear | (oklab ? GradOklab : 0) | SignCode(x) << 3 | SignCode(y) << 5);
            emit(Prop.Gradient, e, angle, default, default);
            return true;
        }
        return false;
    }

    /// <summary>Modifier nội suy: chỉ oklab (mặc định) và srgb; oklch/hsl/longer… chưa hỗ trợ (báo class lạ).</summary>
    static bool TrySpace(string mod, out bool oklab)
    {
        oklab = mod == "oklab";
        return mod is "oklab" or "srgb";
    }

    /// <summary>Một điểm dừng: màu (from-blue-500/50, from-[#fff], from-transparent) hoặc vị trí (from-10%, from-[20px]).</summary>
    static bool StopOp(string v, Prop colorProp, byte index, Action<Prop, byte, float, Length, Rgba> emit)
    {
        if (IsArbitrary(v) && !LooksLikeColor(v[1..^1]) && TryLength(v[1..^1], out var pos))
        {
            emit(Prop.GradPos, index, 0, pos, default);
            return true;
        }
        if (v.EndsWith('%') && int.TryParse(v[..^1], out int pct) && pct >= 0)
        {
            emit(Prop.GradPos, index, 0, Length.Fraction(pct / 100f), default);
            return true;
        }
        if (!TryColor(v, out var c)) return false;
        emit(colorProp, 0, 0, default, c);
        return true;
    }

    static void ApplyGradient(ref Style s, in UtilityOp o, Rgba color)
    {
        ref var g = ref s.Gradient;
        switch (o.Prop)
        {
            case Prop.Gradient:
                g.Kind = (GradientKind)(o.E & 3);
                g.Oklab = (o.E & GradOklab) != 0;
                g.Angle = o.F;
                g.CornerX = CodeSign((o.E >> 3) & 3);
                g.CornerY = CodeSign((o.E >> 5) & 3);
                break;
            case Prop.GradFrom: g.From = color; break;
            case Prop.GradVia: g.Via = o.E == 1 ? null : color; break;
            case Prop.GradTo: g.To = color; break;
            case Prop.GradPos:
                if (o.E == 0) g.FromPos = o.L; else if (o.E == 1) g.ViaPos = o.L; else g.ToPos = o.L;
                break;
        }
    }
}
