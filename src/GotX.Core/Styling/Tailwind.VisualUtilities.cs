using System.Collections.Concurrent;
using System.Globalization;

namespace GotX;

public static partial class Tailwind
{
    /// <summary>Utility có giá trị thuộc nhóm màu, viền, bóng, chữ, khoảng cách.</summary>
    static bool VisualUtility(string u, bool neg, UtilityOp t, List<UtilityOp> ops)
    {
        void Emit(Prop p, byte e = 0, float f = 0, Length l = default, Rgba c = default) => AddOp(ops, t, p, e, f, l, c);

        string v;
        if (Prefix(u, "ring-offset-", out v))
        {
            if (TryWidth(v, out float rw)) { Emit(Prop.RingOffsetWidth, f: rw); return true; }
            return TryColor(v, out var rc) && Done(() => Emit(Prop.RingOffsetColor, c: rc));
        }
        if (Prefix(u, "ring-", out v))
        {
            if (TryWidth(v, out float rw)) { Emit(Prop.RingWidth, f: rw); return true; }
            return TryColor(v, out var rc) && Done(() => Emit(Prop.RingColor, c: rc));
        }

        foreach (var (pre, mask) in RoundedSides)
        {
            // rounded-t (không có giá trị) = bo theo bán kính mặc định, như rounded
            if (u.Length == pre.Length - 1 && pre.StartsWith(u, StringComparison.Ordinal)) { Emit(Prop.Radius, mask, Theme.Radius[""]); return true; }
            if (Prefix(u, pre, out v))
                return TryRadius(v, out float r) && Done(() => Emit(Prop.Radius, mask, r));
        }
        if (Prefix(u, "rounded-", out v))
            return TryRadius(v, out float r) && Done(() => Emit(Prop.Radius, TL | TR | BR | BL, r));

        if (Prefix(u, "border-", out v))
        {
            // border-t, border-x-2, border-l-[3px], border-b-red-500 … (s/e = trái/phải theo chiều viết LTR)
            byte sides = AllSides;
            foreach (var (name, mask) in BorderSides)
            {
                if (v == name) { Emit(Prop.BorderWidth, mask, 1); return true; }
                if (v.StartsWith(name + "-")) { sides = mask; v = v[(name.Length + 1)..]; break; }
            }
            if (TryWidth(v, out float bw)) { Emit(Prop.BorderWidth, sides, bw); return true; }
            return TryColor(v, out var bc) && Done(() => Emit(Prop.BorderColor, sides, c: bc));
        }
        if (Prefix(u, "divide-x-", out v)) return TryWidth(v, out float dx) && Done(() => Emit(Prop.DivideX, f: dx));
        if (Prefix(u, "divide-y-", out v)) return TryWidth(v, out float dy) && Done(() => Emit(Prop.DivideY, f: dy));
        if (Prefix(u, "divide-", out v)) return TryColor(v, out var dc) && Done(() => Emit(Prop.DivideColor, c: dc));
        if (Prefix(u, "rotate-", out v))
        {
            float deg;
            if (IsArbitrary(v)) { if (!TryAngle(v[1..^1], out deg)) return false; }
            else if (!TryNumber(v, out deg)) return false;
            Emit(Prop.Rotate, f: neg ? -deg : deg);
            return true;
        }

        if (Prefix(u, "shadow-", out v))
        {
            if (Theme.ShadowNames.TryGetValue(v, out var preset)) { Emit(Prop.Shadow, (byte)preset); return true; }
            if (IsArbitrary(v) && TryShadow(v[1..^1], out var sh))
            {
                var o = t; o.Prop = Prop.CustomShadow; o.S = sh; ops.Add(o);
                return true;
            }
            return TryColor(v, out var sc) && Done(() => Emit(Prop.ShadowColor, c: sc));
        }

        if (Prefix(u, "opacity-", out v))
        {
            if (IsArbitrary(v) && TryPercentOrNumber(v[1..^1], out float a)) { Emit(Prop.Opacity, f: a); return true; }
            return TryNumber(v, out float n) && Done(() => Emit(Prop.Opacity, f: n / 100f));
        }

        if (Prefix(u, "leading-", out v))
        {
            if (Theme.Leading.TryGetValue(v, out float k)) { Emit(Prop.LineHeight, l: Length.Fraction(k)); return true; }
            // leading-[1.1]: số không đơn vị là hệ số nhân cỡ chữ (như line-height của CSS), không phải pixel
            if (IsArbitrary(v) && float.TryParse(v[1..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out float factor))
            { Emit(Prop.LineHeight, l: Length.Fraction(factor)); return true; }
            return TrySpacing(v, false, out var lh) && Done(() => Emit(Prop.LineHeight, l: lh));
        }

        if (Prefix(u, "font-", out v))
        {
            if (Theme.FontWeights.TryGetValue(v, out var fw)) { Emit(Prop.Weight, f: (float)fw); return true; }
            return IsArbitrary(v) && TryNumber(v[1..^1], out float n) && Done(() => Emit(Prop.Weight, f: n));
        }

        if (Prefix(u, "text-", out v))
        {
            // text-sm, text-sm/6, text-[15px], text-[15px]/[22px] hoặc màu chữ
            SplitModifier(v, out var baseV, out var mod);
            if (Theme.FontSizes.TryGetValue(baseV, out var fs))
            {
                Emit(Prop.FontSize, l: Length.Rems(fs.size));
                if (mod is null) Emit(Prop.LineHeight, l: Length.Rems(fs.leading));
                else if (TrySpacing(mod, false, out var lh)) Emit(Prop.LineHeight, l: lh);
                else return false;
                return true;
            }
            if (IsArbitrary(baseV) && !LooksLikeColor(baseV[1..^1]) && TryLength(baseV[1..^1], out var size))
            {
                Emit(Prop.FontSize, l: size);
                if (mod is not null) { if (!TrySpacing(mod, false, out var lh)) return false; Emit(Prop.LineHeight, l: lh); }
                return true;
            }
            return TryColor(v, out var tc) && Done(() => Emit(Prop.TextColor, c: tc));
        }

        if (Prefix(u, "bg-", out v)) return TryColor(v, out var bg) && Done(() => Emit(Prop.Bg, c: bg));

        if (Prefix(u, "px-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Padding, Left | Right, l: l));
        if (Prefix(u, "py-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Padding, Top | Bottom, l: l));
        if (Prefix(u, "pt-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Padding, Top, l: l));
        if (Prefix(u, "pr-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Padding, Right, l: l));
        if (Prefix(u, "pb-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Padding, Bottom, l: l));
        if (Prefix(u, "pl-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Padding, Left, l: l));
        if (Prefix(u, "p-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Padding, AllSides, l: l));
        if (Prefix(u, "mx-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Margin, Left | Right, l: l));
        if (Prefix(u, "my-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Margin, Top | Bottom, l: l));
        if (Prefix(u, "mt-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Margin, Top, l: l));
        if (Prefix(u, "mr-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Margin, Right, l: l));
        if (Prefix(u, "mb-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Margin, Bottom, l: l));
        if (Prefix(u, "ml-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Margin, Left, l: l));
        if (Prefix(u, "m-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Margin, AllSides, l: l));

        return false;
    }

    static readonly (string, byte)[] BorderSides =
    [
        ("x", Left | Right), ("y", Top | Bottom), ("t", Top), ("r", Right), ("b", Bottom), ("l", Left), ("s", Left), ("e", Right),
    ];

    static readonly (string, byte)[] RoundedSides =
    [
        ("rounded-tl-", TL), ("rounded-tr-", TR), ("rounded-br-", BR), ("rounded-bl-", BL),
        ("rounded-t-", TL | TR), ("rounded-r-", TR | BR), ("rounded-b-", BR | BL), ("rounded-l-", TL | BL),
    ];
}
