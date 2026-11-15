using System.Collections.Concurrent;
using System.Globalization;

namespace GotX;

public static partial class Tailwind
{
    /// <summary>Utility có giá trị thuộc nhóm bố cục, kích thước, biến đổi, chuyển động (tiền tố dài kiểm tra trước).</summary>
    static bool LayoutUtility(string u, bool neg, UtilityOp t, List<UtilityOp> ops)
    {
        void Emit(Prop p, byte e = 0, float f = 0, Length l = default, Rgba c = default) => AddOp(ops, t, p, e, f, l, c);

        if (Prefix(u, "items-", out var v)) return Enum<Align>(v, "stretch:Stretch start:Start center:Center end:End baseline:Baseline", x => Emit(Prop.Items, x));
        if (Prefix(u, "self-", out v))
        {
            if (v == "auto") { Emit(Prop.Self, 255); return true; }
            return Enum<Align>(v, "stretch:Stretch start:Start center:Center end:End baseline:Baseline", x => Emit(Prop.Self, x));
        }
        if (Prefix(u, "justify-", out v))
            return Enum<Justify>(v, "start:Start normal:Start center:Center end:End between:SpaceBetween around:SpaceAround evenly:SpaceEvenly", x => Emit(Prop.Justify, x));

        if (Prefix(u, "grid-cols-", out v) || Prefix(u, "grid-rows-", out _))
        {
            bool cols = u.StartsWith("grid-cols-");
            if (!cols) v = u["grid-rows-".Length..];
            GridTemplate? tpl = null;
            if (v == "none") tpl = null;
            else if (IsArbitrary(v)) { if (!GridTemplate.TryParse(v[1..^1], out var parsed)) return false; tpl = parsed; }
            else if (int.TryParse(v, out int count) && count is > 0 and <= 64) tpl = GridTemplate.Repeat(count);
            else return false;
            var o = t; o.Prop = cols ? Prop.GridCols : Prop.GridRows; o.Ref = tpl; ops.Add(o);
            return true;
        }
        if (Prefix(u, "col-span-", out v)) return int.TryParse(v, out int cs) && cs > 0 && Done(() => Emit(Prop.ColSpan, f: cs));
        if (Prefix(u, "row-span-", out v)) return int.TryParse(v, out int rs) && rs > 0 && Done(() => Emit(Prop.RowSpan, f: rs));
        if (Prefix(u, "translate-x-", out v)) return TrySize(v, neg, out var tx) && Done(() => Emit(Prop.Translate, 1, l: tx));
        if (Prefix(u, "translate-y-", out v)) return TrySize(v, neg, out var ty) && Done(() => Emit(Prop.Translate, 2, l: ty));
        if (Prefix(u, "scale-", out v))
        {
            byte axes = 3;
            if (v.StartsWith("x-")) { axes = 1; v = v[2..]; }
            else if (v.StartsWith("y-")) { axes = 2; v = v[2..]; }
            float sc;
            if (IsArbitrary(v)) { if (!TryPercentOrNumber(v[1..^1], out sc)) return false; }
            else if (TryNumber(v, out sc)) sc /= 100f;
            else return false;
            Emit(Prop.Scale, axes, neg ? -sc : sc);
            return true;
        }
        if (Prefix(u, "skew-", out v))
        {
            // skew-12 (cả hai trục, v4), skew-x-12, skew-y-[10deg]
            byte axis = 3;
            if (v.StartsWith("x-")) { axis = 1; v = v[2..]; }
            else if (v.StartsWith("y-")) { axis = 2; v = v[2..]; }
            float deg;
            if (IsArbitrary(v)) { if (!TryAngle(v[1..^1], out deg)) return false; }
            else if (!TryNumber(v, out deg)) return false;
            Emit(Prop.Skew, axis, neg ? -deg : deg);
            return true;
        }
        if (Prefix(u, "origin-", out v)) return TryOrigin(v, out var ox, out var oy) && Done(() => { Emit(Prop.Origin, 1, l: ox); Emit(Prop.Origin, 2, l: oy); });
        if (Prefix(u, "outline-offset-", out v))
        {
            if (!TryWidth(v, out float off)) return false;
            Emit(Prop.OutlineOffset, f: neg ? -off : off);
            return true;
        }
        if (Prefix(u, "outline-", out v))
        {
            if (TryWidth(v, out float ow)) { Emit(Prop.OutlineWidth, f: ow); Emit(Prop.OutlineStyle, (byte)BorderStyle.Solid); return true; }
            return TryColor(v, out var oc) && Done(() => Emit(Prop.OutlineColor, c: oc));
        }
        if (Prefix(u, "duration-", out v)) return TryMs(v, out float ms) && Done(() => Emit(Prop.Duration, f: ms));
        if (Prefix(u, "delay-", out v)) return TryMs(v, out float dl) && Done(() => Emit(Prop.Delay, f: dl));

        if (Prefix(u, "min-w-", out v)) return SizeOp(v, neg, Prop.MinW, ScreenW, Emit);
        if (Prefix(u, "min-h-", out v)) return SizeOp(v, neg, Prop.MinH, ScreenH, Emit);
        if (Prefix(u, "max-w-", out v)) return v == "none" ? Done(() => Emit(Prop.MaxW, l: Length.Auto)) : SizeOp(v, neg, Prop.MaxW, ScreenW, Emit);
        if (Prefix(u, "max-h-", out v)) return v == "none" ? Done(() => Emit(Prop.MaxH, l: Length.Auto)) : SizeOp(v, neg, Prop.MaxH, ScreenH, Emit);
        if (Prefix(u, "size-", out v)) return SizeOp(v, neg, Prop.Width, 0, Emit) && SizeOp(v, neg, Prop.Height, 0, Emit);
        if (Prefix(u, "basis-", out v)) return SizeOp(v, neg, Prop.Basis, 0, Emit);
        if (u == "w-fit") return !neg && Done(() => { Emit(Prop.Width, l: Length.Auto); Emit(Prop.ShrinkWrap, 1); });
        if (Prefix(u, "w-", out v)) return SizeOp(v, neg, Prop.Width, ScreenW, Emit);
        if (Prefix(u, "h-", out v)) return SizeOp(v, neg, Prop.Height, ScreenH, Emit);

        if (Prefix(u, "gap-x-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Gap, 1, l: l));
        if (Prefix(u, "gap-y-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Gap, 2, l: l));
        if (Prefix(u, "gap-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Gap, 3, l: l));
        // space-x/y dùng margin giữa các con trong CSS; ở đây xấp xỉ bằng gap
        if (Prefix(u, "space-x-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Gap, 1, l: l));
        if (Prefix(u, "space-y-", out v)) return SpaceOp(v, neg, l => Emit(Prop.Gap, 2, l: l));

        if (Prefix(u, "inset-x-", out v)) return InsetOp(v, neg, Left | Right, Emit);
        if (Prefix(u, "inset-y-", out v)) return InsetOp(v, neg, Top | Bottom, Emit);
        if (Prefix(u, "inset-", out v)) return InsetOp(v, neg, AllSides, Emit);
        if (Prefix(u, "top-", out v)) return InsetOp(v, neg, Top, Emit);
        if (Prefix(u, "right-", out v)) return InsetOp(v, neg, Right, Emit);
        if (Prefix(u, "bottom-", out v)) return InsetOp(v, neg, Bottom, Emit);
        if (Prefix(u, "left-", out v)) return InsetOp(v, neg, Left, Emit);

        return false;
    }

    /// <summary>Góc CSS: 45deg, 0.25turn, 1.2rad, hoặc số (độ).</summary>
    static bool TryAngle(string s, out float deg)
    {
        if (s.EndsWith("deg")) return TryNumber(s[..^3], out deg);
        if (s.EndsWith("turn") && TryNumber(s[..^4], out deg)) { deg *= 360; return true; }
        if (s.EndsWith("rad") && TryNumber(s[..^3], out deg)) { deg *= 180 / MathF.PI; return true; }
        return TryNumber(s, out deg);
    }

    /// <summary>origin-center, origin-top-left… hoặc origin-[25%_75%], origin-[10px_20px].</summary>
    static bool TryOrigin(string v, out Length x, out Length y)
    {
        var half = Length.Fraction(0.5f);
        (x, y) = v switch
        {
            "center" => (half, half), "top" => (half, Length.Fraction(0)), "bottom" => (half, Length.Full),
            "left" => (Length.Fraction(0), half), "right" => (Length.Full, half),
            "top-left" => (Length.Fraction(0), Length.Fraction(0)), "top-right" => (Length.Full, Length.Fraction(0)),
            "bottom-left" => (Length.Fraction(0), Length.Full), "bottom-right" => (Length.Full, Length.Full),
            _ => (Length.Auto, Length.Auto),
        };
        if (!x.IsAuto) return true;
        if (!IsArbitrary(v)) return false;
        var parts = v[1..^1].Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !TryLength(parts[0], out x) || !TryLength(parts[1], out y)) return false;
        return true;
    }
}
