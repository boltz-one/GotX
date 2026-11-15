// Phần spacing và kích thước của Styled.g.cs: mỗi utility × thang spacing (và phân số, auto với kích thước).
static partial class StyledWriter
{
    static void Spacing()
    {
        // ---- Thang spacing Tailwind: n → n × 0.25rem ----
        var scale = new List<(string suffix, string expr)> { ("0", "Length.Px(0)"), ("Px", "Length.Px(1)") };
        foreach (var n in new float[] { 0.5f, 1, 1.5f, 2, 2.5f, 3, 3.5f, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 64, 72, 80, 96 })
            scale.Add((n.ToString(inv).Replace('.', '_'), $"Length.Rems({F(n * 0.25f)})"));

        var fractions = new (string, string)[] {
            ("Full", "Length.Full"), ("1_2", "Length.Fraction(0.5f)"), ("1_3", "Length.Fraction(1f / 3f)"),
            ("2_3", "Length.Fraction(2f / 3f)"), ("1_4", "Length.Fraction(0.25f)"), ("3_4", "Length.Fraction(0.75f)") };

        var spacing = new (string name, string body, string doc)[] {
            ("P", "s.Padding = EdgesLength.All(v);", "padding"),
            ("Px", "s.Padding.Left = v; s.Padding.Right = v;", "padding-left/right"),
            ("Py", "s.Padding.Top = v; s.Padding.Bottom = v;", "padding-top/bottom"),
            ("Pt", "s.Padding.Top = v;", "padding-top"), ("Pr", "s.Padding.Right = v;", "padding-right"),
            ("Pb", "s.Padding.Bottom = v;", "padding-bottom"), ("Pl", "s.Padding.Left = v;", "padding-left"),
            ("M", "s.Margin = EdgesLength.All(v);", "margin"),
            ("Mx", "s.Margin.Left = v; s.Margin.Right = v;", "margin-left/right"),
            ("My", "s.Margin.Top = v; s.Margin.Bottom = v;", "margin-top/bottom"),
            ("Mt", "s.Margin.Top = v;", "margin-top"), ("Mr", "s.Margin.Right = v;", "margin-right"),
            ("Mb", "s.Margin.Bottom = v;", "margin-bottom"), ("Ml", "s.Margin.Left = v;", "margin-left"),
            ("Gap", "s.ColumnGap = v; s.RowGap = v;", "gap"),
            ("GapX", "s.ColumnGap = v;", "column-gap"),
            ("GapY", "s.RowGap = v;", "row-gap"),
        };
        var sizing = new (string name, string body, string doc, bool auto)[] {
            ("W", "s.Width = v;", "width", true), ("H", "s.Height = v;", "height", true),
            ("Size", "s.Width = v; s.Height = v;", "width + height", true),
            ("MinW", "s.MinWidth = v;", "min-width", false), ("MinH", "s.MinHeight = v;", "min-height", false),
            ("MaxW", "s.MaxWidth = v;", "max-width", false), ("MaxH", "s.MaxHeight = v;", "max-height", false),
            ("Inset", "s.Inset = EdgesLength.All(v);", "inset", true),
            ("InsetX", "s.Inset.Left = v; s.Inset.Right = v;", "left + right", true),
            ("InsetY", "s.Inset.Top = v; s.Inset.Bottom = v;", "top + bottom", true),
            ("Top", "s.Inset.Top = v;", "top", true), ("Right", "s.Inset.Right = v;", "right", true),
            ("Bottom", "s.Inset.Bottom = v;", "bottom", true), ("Left", "s.Inset.Left = v;", "left", true),
        };

        foreach (var (name, body, doc) in spacing)
        {
            Util(name, body, doc, "Length v", "v");
            foreach (var (suf, expr) in scale)
                Util(name + suf, $"var v = {expr}; {body}", $"{doc}: {suf.Replace('_', '.')}");
        }
        foreach (var (name, body, doc, auto) in sizing)
        {
            Util(name, body, doc, "Length v", "v");
            foreach (var (suf, expr) in scale)
                Util(name + suf, $"var v = {expr}; {body}", $"{doc}: {suf.Replace('_', '.')}");
            foreach (var (suf, expr) in fractions)
                Util(name + suf, $"var v = {expr}; {body}", $"{doc}: {suf.Replace('_', '/')}");
            if (auto) Util(name + "Auto", $"var v = Length.Auto; {body}", $"{doc}: auto");
        }
    }
}
