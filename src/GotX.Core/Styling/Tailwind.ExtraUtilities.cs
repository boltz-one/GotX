namespace GotX;

public static partial class Tailwind
{
    const byte ViewportW = 3, ViewportH = 4; // E của op kích thước có vw/vh trong calc(): quy ra px lúc dựng cây

    /// <summary>
    /// Utility bổ sung cho fidelity: line-clamp, text-wrap/balance/pretty, tabular-nums, aspect-*, vị trí grid
    /// (col-start/end, row-start/end, col-[…]), grid-flow-*, underline-offset, decoration-*, tracking-[…],
    /// translate hai trục / -translate-*-full, rounded-[a_b_c_d]. Chạy trước VisualUtility để không bị đọc nhầm thành màu.
    /// </summary>
    static bool ExtraUtility(string u, bool neg, UtilityOp t, List<UtilityOp> ops)
    {
        void Emit(Prop p, byte e = 0, float f = 0, Length l = default, Rgba c = default) => AddOp(ops, t, p, e, f, l, c);

        switch (u)
        {
            case "line-clamp-none": Emit(Prop.LineClamp, 0); return !neg;
            case "text-wrap": Emit(Prop.NoWrap, 0); Emit(Prop.TextWrap, (byte)TextWrapStyle.Auto); return !neg;
            case "text-nowrap": Emit(Prop.NoWrap, 1); return !neg;
            case "text-balance": Emit(Prop.NoWrap, 0); Emit(Prop.TextWrap, (byte)TextWrapStyle.Balance); return !neg;
            case "text-pretty": Emit(Prop.NoWrap, 0); Emit(Prop.TextWrap, (byte)TextWrapStyle.Pretty); return !neg;
            case "bg-clip-border": Emit(Prop.BgClip, (byte)BgClip.Border); return !neg;
            case "bg-clip-padding": Emit(Prop.BgClip, (byte)BgClip.Padding); return !neg;
            case "bg-clip-content": Emit(Prop.BgClip, (byte)BgClip.Content); return !neg;
            case "tabular-nums": Emit(Prop.NumericSpacing, 1); return !neg;
            case "proportional-nums": case "normal-nums": Emit(Prop.NumericSpacing, 0); return !neg;
            case "aspect-auto": Emit(Prop.Aspect, f: 0); return !neg;
            case "aspect-square": Emit(Prop.Aspect, f: 1); return !neg;
            case "aspect-video": Emit(Prop.Aspect, f: 16f / 9f); return !neg;
            case "col-auto": Emit(Prop.GridLine, 0, 0); Emit(Prop.GridLine, 1, 0); Emit(Prop.ColSpan, f: 0); return !neg;
            case "row-auto": Emit(Prop.GridLine, 2, 0); Emit(Prop.GridLine, 3, 0); Emit(Prop.RowSpan, f: 0); return !neg;
            case "grid-flow-row": Emit(Prop.GridFlow, 0); return !neg;
            case "grid-flow-col": Emit(Prop.GridFlow, 1); return !neg;
            case "grid-flow-dense": case "grid-flow-row-dense": Emit(Prop.GridFlow, 2); return !neg;
            case "grid-flow-col-dense": Emit(Prop.GridFlow, 3); return !neg;
            case "underline-offset-auto": Emit(Prop.UnderlineOffset, f: float.NaN); return !neg;
            case "decoration-auto": case "decoration-from-font": Emit(Prop.DecorationThickness, f: 0); return !neg;
            // Lớp mở rộng của GotX cho thanh cuộn (Tailwind không có): khi nào hiện, bề rộng rãnh, màu con trượt
            case "scrollbar-always": Emit(Prop.ScrollbarMode, (byte)ScrollbarMode.Always); return !neg;
            case "scrollbar-hover": Emit(Prop.ScrollbarMode, (byte)ScrollbarMode.Hover); return !neg;
            case "scrollbar-scroll": Emit(Prop.ScrollbarMode, (byte)ScrollbarMode.Scroll); return !neg;
            case "scrollbar-none": Emit(Prop.ScrollbarMode, (byte)ScrollbarMode.None); return !neg;
        }
        string v;
        if (Prefix(u, "scrollbar-w-", out v))
            return !neg && TrySpacing(v, false, out var sw) && sw.Resolve(0, 16) is { } swPx && Done(() => Emit(Prop.ScrollbarWidth, f: swPx));
        if (Prefix(u, "scrollbar-thumb-", out v))
            return !neg && TryColor(v, out var sc) && Done(() => Emit(Prop.ScrollbarColor, c: sc));
        if (Prefix(u, "line-clamp-", out v))
        {
            if (IsArbitrary(v)) v = v[1..^1];
            return !neg && int.TryParse(v, out int n) && n is > 0 and < 256 && Done(() => Emit(Prop.LineClamp, (byte)n));
        }
        if (Prefix(u, "aspect-", out v))
        {
            // aspect-[4/3], aspect-[1.5], aspect-3/2 (phân số v4)
            if (IsArbitrary(v)) v = v[1..^1].Replace("_", "");
            int slash = v.IndexOf('/');
            float ratio;
            if (slash > 0) { if (!TryNumber(v[..slash], out float a) || !TryNumber(v[(slash + 1)..], out float b) || b <= 0) return false; ratio = a / b; }
            else if (!TryNumber(v, out ratio)) return false;
            return !neg && ratio > 0 && Done(() => Emit(Prop.Aspect, f: ratio));
        }
        foreach (var (pre, line) in GridLines)
            if (Prefix(u, pre, out v))
            {
                if (v == "auto") { Emit(Prop.GridLine, line, 0); return !neg; }
                if (IsArbitrary(v)) v = v[1..^1];
                return int.TryParse(v, out int n) && n != 0 && Done(() => Emit(Prop.GridLine, line, neg ? -n : n));
            }
        if ((Prefix(u, "col-", out v) || Prefix(u, "row-", out v)) && IsArbitrary(v))
            return !neg && GridArbitrary(v[1..^1], u[0] == 'c', Emit);
        if (Prefix(u, "underline-offset-", out v))
        {
            if (!TryWidth(v, out float off)) return false;
            Emit(Prop.UnderlineOffset, f: neg ? -off : off);
            return true;
        }
        if (Prefix(u, "decoration-", out v))
        {
            if (neg) return false;
            if (TryWidth(v, out float th)) { Emit(Prop.DecorationThickness, f: th); return true; }
            return TryColor(v, out var dc) && Done(() => Emit(Prop.DecorationColor, c: dc));
        }
        if (Prefix(u, "tracking-", out v) && IsArbitrary(v))
        {
            // tracking-[0.2em] / tracking-[-0.01em] theo em; tracking-[2px] tuyệt đối
            string x = v[1..^1];
            float sign = neg ? -1 : 1;
            if (x.EndsWith("em") && !x.EndsWith("rem") && TryNumber(x[..^2], out float em)) { Emit(Prop.Tracking, f: sign * em); return true; }
            if (TryLength(x, out var tl) && tl.Kind != LengthKind.Fraction && tl.Resolve(0, 16) is { } px) { Emit(Prop.Tracking, 1, sign * px); return true; }
            return false;
        }
        if (Prefix(u, "translate-", out v) && !v.StartsWith("x-") && !v.StartsWith("y-"))
            return TranslateValue(v, neg, out var tr) && Done(() => Emit(Prop.Translate, 3, l: tr));
        if (Prefix(u, "translate-x-", out v)) return v == "full" && neg && Done(() => Emit(Prop.Translate, 1, l: Length.Fraction(-1)));
        if (Prefix(u, "translate-y-", out v)) return v == "full" && neg && Done(() => Emit(Prop.Translate, 2, l: Length.Fraction(-1)));
        if (Prefix(u, "rounded-", out v) && IsArbitrary(v) && v.Contains('_')) return !neg && RoundedCorners(v[1..^1], Emit);
        return false;
    }

    static bool TranslateValue(string v, bool neg, out Length l)
    {
        if (v == "full") { l = Length.Fraction(neg ? -1 : 1); return true; }
        return TrySize(v, neg, out l) && !l.IsAuto;
    }

    static readonly (string, byte)[] GridLines = [("col-start-", 0), ("col-end-", 1), ("row-start-", 2), ("row-end-", 3)];

    /// <summary>col-[span_2], col-[2_/_4], col-[1_/_-1], col-[span_2_/_span_2] (dấu _ thay khoảng trắng).</summary>
    static bool GridArbitrary(string s, bool col, Action<Prop, byte, float, Length, Rgba> emit)
    {
        var parts = s.Split('/', StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2) return false;
        byte start = col ? (byte)0 : (byte)2;
        Prop span = col ? Prop.ColSpan : Prop.RowSpan;
        for (int i = 0; i < parts.Length; i++)
        {
            string p = parts[i].Trim('_');
            if (p.StartsWith("span_") && int.TryParse(p[5..], out int n) && n > 0) { if (i == 0) emit(span, 0, n, default, default); continue; }
            if (!int.TryParse(p, out int line) || line == 0) return false;
            emit(Prop.GridLine, (byte)(start + i), line, default, default);
        }
        return true;
    }

    /// <summary>rounded-[4px_8px] theo cú pháp border-radius: 1–4 giá trị tl, tr, br, bl.</summary>
    static bool RoundedCorners(string s, Action<Prop, byte, float, Length, Rgba> emit)
    {
        var parts = s.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 1 or > 4) return false;
        var r = new float[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            if (!TryLength(parts[i], out var l) || l.Resolve(float.NaN, 16) is not { } px) return false; else r[i] = px;
        (float tl, float tr, float br, float bl) = r.Length switch
        {
            1 => (r[0], r[0], r[0], r[0]), 2 => (r[0], r[1], r[0], r[1]), 3 => (r[0], r[1], r[2], r[1]), _ => (r[0], r[1], r[2], r[3]),
        };
        emit(Prop.Radius, TL, tl, default, default); emit(Prop.Radius, TR, tr, default, default);
        emit(Prop.Radius, BR, br, default, default); emit(Prop.Radius, BL, bl, default, default);
        return true;
    }

    /// <summary>Gán các Prop mới (chữ, aspect, grid) vào Style.</summary>
    static void ApplyText(ref Style s, in UtilityOp o, in VariantContext ctx)
    {
        switch (o.Prop)
        {
            case Prop.LineClamp: s.LineClampValue = o.E; s.Overflow = o.E > 0 ? Overflow.Hidden : Overflow.Visible; break;
            case Prop.TextWrap: s.WrapStyleValue = (TextWrapStyle)o.E; break;
            case Prop.NumericSpacing: s.TabularNumsValue = o.E != 0; break;
            case Prop.Aspect: s.AspectRatio = o.F; break;
            case Prop.GridLine:
                short line = (short)o.F;
                if (o.E == 0) s.ColStart = line; else if (o.E == 1) s.ColEnd = line; else if (o.E == 2) s.RowStart = line; else s.RowEnd = line;
                break;
            case Prop.GridFlow: s.GridFlowColumn = (o.E & 1) != 0; s.GridFlowDense = (o.E & 2) != 0; break;
            case Prop.UnderlineOffset: s.UnderlineOffsetValue = o.F; break;
            case Prop.DecorationColor: s.DecorationColorValue = Col(o, ctx); break;
            case Prop.DecorationThickness: s.DecorationThicknessValue = o.F; break;
        }
    }
}
