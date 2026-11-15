namespace GotX;

/// <summary>
/// Thuộc tính kế thừa từ cha xuống con (chữ + opacity tích luỹ). Là record struct để so sánh làm khoá cache view.
/// </summary>
public record struct TextStyle
{
    public Rgba Color;
    public float FontSize;
    public float LineHeight;
    public FontWeight Weight;
    public TextAlign Align;
    public bool NoWrap;
    public bool Ellipsis;
    public float Opacity;
    public FontFamily Family;
    public bool Italic;
    public TextDecoration Decoration;
    public TextCase Case;
    /// <summary>Khoảng cách thêm giữa các ký tự, theo em (nhân với cỡ chữ).</summary>
    public float Tracking;
    /// <summary>line-clamp-N: số dòng tối đa (0 = không giới hạn), dòng cuối có "…" nếu còn chữ bị cắt.</summary>
    public byte LineClamp;
    public TextWrapStyle WrapStyle;
    /// <summary>tabular-nums: chữ số cùng bề rộng (thay glyph theo OpenType feature tnum).</summary>
    public bool TabularNums;
    /// <summary>underline-offset-* (px tính từ đường gốc); null = vị trí mặc định.</summary>
    public float? UnderlineOffset;
    public Rgba? DecorationColor;          // null = currentColor
    public float DecorationThickness;      // px; 0 = auto (theo cỡ chữ)

    /// <summary>Khoảng cách thêm sau mỗi ký tự (logical px).</summary>
    public readonly float LetterSpacing => Tracking * FontSize;

    public static TextStyle Default => new()
    { Color = Rgba.Black, FontSize = 16, LineHeight = 24, Weight = FontWeight.Normal, Opacity = 1 };

    public readonly TextStyle Refine(in Style s, float rem)
    {
        var t = this;
        if (s.TextColorValue is { } c) t.Color = c;
        if (s.FontWeightValue is { } w) t.Weight = w;
        if (s.TextAlignValue is { } a) t.Align = a;
        if (s.NoWrapValue is { } nw) t.NoWrap = nw;
        if (s.EllipsisValue is { } el) t.Ellipsis = el;
        if (s.FontFamilyValue is { } ff) t.Family = ff;
        if (s.ItalicValue is { } it) t.Italic = it;
        if (s.DecorationValue is { } d) t.Decoration = d;
        if (s.CaseValue is { } cs) t.Case = cs;
        if (s.TrackingValue is { } tr) t.Tracking = tr;
        // line-clamp ngầm có ellipsis (như -webkit-line-clamp): dòng cuối kết thúc bằng "…" thay vì tràn ra ngoài
        if (s.LineClampValue is { } lc) { t.LineClamp = lc; if (lc > 0) t.Ellipsis = true; }
        if (s.WrapStyleValue is { } ws) t.WrapStyle = ws;
        if (s.TabularNumsValue is { } tn) t.TabularNums = tn;
        if (s.UnderlineOffsetValue is { } uo) t.UnderlineOffset = float.IsNaN(uo) ? null : uo;
        if (s.DecorationColorValue is { } dc) t.DecorationColor = dc;
        if (s.DecorationThicknessValue is { } dt) t.DecorationThickness = dt;
        if (s.FontSizeValue is { } fs)
        {
            t.FontSize = fs.Resolve(FontSize, rem) ?? FontSize;
            if (s.LineHeightValue is null) t.LineHeight = MathF.Round(t.FontSize * 1.5f);
        }
        if (s.LineHeightValue is { } lh) t.LineHeight = lh.Resolve(t.FontSize, rem) ?? LineHeight;
        // tracking-[2px] là độ dài tuyệt đối: quy về em theo cỡ chữ của chính phần tử này
        if (s.TrackingPxValue is { } tpx && t.FontSize > 0) t.Tracking = tpx / t.FontSize;
        t.Opacity *= s.Opacity;
        return t;
    }
}
