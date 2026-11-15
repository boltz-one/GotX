// Phần màu, viền, bo góc, ring, bóng đổ và chữ của Styled.g.cs.
static partial class StyledWriter
{
    static void Visual()
    {
        // ---- Màu, viền, bo góc ----
        Util("Bg", "s.Background = c;", "background-color", "Rgba c", "c");
        Util("TextColor", "s.TextColorValue = c;", "color (kế thừa xuống con)", "Rgba c", "c");
        Util("BorderColor", "s.BorderColorValue = c; s.BorderTopColor = s.BorderRightColor = s.BorderBottomColor = s.BorderLeftColor = null;", "border-color (cả 4 cạnh)", "Rgba c", "c");
        Util("Border", "s.Border = new Edges(px, px, px, px);", "border-width (px, cả 4 cạnh)", "float px", "px");
        foreach (var n in new[] { 0, 1, 2, 4, 8 })
            Util("Border" + n, $"s.Border = new Edges({n}, {n}, {n}, {n});", $"border-width: {n}px");
        // Từng cạnh: BorderT(2), BorderX(1), BorderLColor(Tw.Indigo500)…
        foreach (var (side, set, color) in new[] {
            ("T", "s.Border.Top = px;", "s.BorderTopColor = c;"),
            ("R", "s.Border.Right = px;", "s.BorderRightColor = c;"),
            ("B", "s.Border.Bottom = px;", "s.BorderBottomColor = c;"),
            ("L", "s.Border.Left = px;", "s.BorderLeftColor = c;"),
            ("X", "s.Border.Left = px; s.Border.Right = px;", "s.BorderLeftColor = c; s.BorderRightColor = c;"),
            ("Y", "s.Border.Top = px; s.Border.Bottom = px;", "s.BorderTopColor = c; s.BorderBottomColor = c;") })
        {
            Util("Border" + side, set, $"border-{side.ToLowerInvariant()} (px)", "float px", "px");
            Util("Border" + side + "Color", color, $"border-{side.ToLowerInvariant()}-color", "Rgba c", "c");
        }
        Util("DivideX", "s.DivideX = px;", "divide-x (px)", "float px", "px");
        Util("DivideY", "s.DivideY = px;", "divide-y (px)", "float px", "px");
        Util("DivideColor", "s.DivideColor = c;", "divide-color", "Rgba c", "c");
        Util("Rotate", "s.Rotate = deg;", "rotate (độ)", "float deg", "deg");
        foreach (var n in new[] { 0, 1, 2, 3, 6, 12, 45, 90, 180 }) Util($"Rotate{n}", $"s.Rotate = {n};", $"rotate-{n}");
        Util("Rounded", "s.CornerRadii = Corners.All(px);", "border-radius (px)", "float px", "px");
        foreach (var (n, px) in new[] { ("None", 0), ("Xs", 2), ("Sm", 4), ("Md", 6), ("Lg", 8), ("Xl", 12), ("2xl", 16), ("3xl", 24), ("4xl", 32), ("Full", 9999) })
            Util("Rounded" + n, $"s.CornerRadii = Corners.All({px});", $"border-radius: {px}px");
        Util("RoundedDefault", "s.CornerRadii = Corners.All(4);", "border-radius: 4px (rounded)");
        Util("Ring", "s.RingWidth = px;", "ring width (px)", "float px", "px");
        foreach (var n in new[] { 0, 1, 2, 3, 4, 8 })
            Util("Ring" + n, $"s.RingWidth = {n};", $"ring-{n}");
        Util("RingColor", "s.RingColor = c;", "ring color", "Rgba c", "c");
        Util("RingOffset", "s.RingOffsetWidth = px;", "ring offset (px)", "float px", "px");
        foreach (var n in new[] { 1, 2, 4, 8 })
            Util("RingOffset" + n, $"s.RingOffsetWidth = {n};", $"ring-offset-{n}");
        Util("RingOffsetColor", "s.RingOffsetColor = c;", "ring offset color", "Rgba c", "c");
        Util("ShadowColor", "s.ShadowColor = c;", "shadow color", "Rgba c", "c");

        // ---- Bóng đổ (preset Tailwind v4, giá trị nằm trong Theme) ----
        foreach (var (n, p) in new[] { ("None", "None"), ("2xs", "TwoXs"), ("Xs", "Xs"), ("Sm", "Sm"), ("Default", "Sm"),
            ("Md", "Md"), ("Lg", "Lg"), ("Xl", "Xl"), ("2xl", "TwoXl") })
            Util("Shadow" + n, $"s.Shadow = ShadowPreset.{p};", $"shadow-{n.ToLowerInvariant()}");

        // ---- Chữ ----
        foreach (var (n, size, lh) in new[] { ("Xs", 0.75f, 1f), ("Sm", 0.875f, 1.25f), ("Base", 1f, 1.5f), ("Lg", 1.125f, 1.75f),
            ("Xl", 1.25f, 1.75f), ("2xl", 1.5f, 2f), ("3xl", 1.875f, 2.25f), ("4xl", 2.25f, 2.5f), ("5xl", 3f, 3f), ("6xl", 3.75f, 3.75f),
            ("7xl", 4.5f, 4.5f), ("8xl", 6f, 6f), ("9xl", 8f, 8f) })
            Util("Text" + n, $"s.FontSizeValue = Length.Rems({F(size)}); s.LineHeightValue = Length.Rems({F(lh)});", $"text-{n.ToLowerInvariant()}");
        foreach (var n in new[] { "Left", "Center", "Right" })
            Util("Text" + n, $"s.TextAlignValue = TextAlign.{n};", $"text-align: {n.ToLowerInvariant()}");
        Util("TextSize", "s.FontSizeValue = v;", "font-size", "Length v", "v");
        Util("Leading", "s.LineHeightValue = v;", "line-height", "Length v", "v");
        foreach (var n in new[] { "Thin", "ExtraLight", "Light", "Normal", "Medium", "Semibold", "Bold", "ExtraBold", "Black" })
            Util("Font" + n, $"s.FontWeightValue = FontWeight.{n};", $"font-weight: {n}");
    }
}
