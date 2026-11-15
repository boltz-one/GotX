using GotX;

/// <summary>Phần 1b: các utility thêm cho độ trung thực với shadcn (gradient, line-clamp, text-wrap, aspect, grid, breakpoint…).</summary>
static class FeatureClassChecks
{
    public static void Run(Checker c)
    {
        var wide = Checker.Wide;
        Theme.SetColor("fx-background", Rgba.White, Rgba.Black);

        // ---- Gradient ----
        var g1 = Checker.R("bg-linear-to-r from-blue-500 to-red-500/50").Gradient;
        c.Check("bg-linear-to-r from-* to-*/50", g1.Kind == GradientKind.Linear && g1.Angle == 90 && g1.Oklab && g1.CornerX == 0
            && g1.From == Theme.Colors["blue-500"] && Checker.Near(g1.To!.Value.A, 0.5f) && g1.Via is null);
        var g2 = Checker.R("bg-linear-to-tr").Gradient;
        c.Check("bg-linear-to-tr (góc đích)", g2.CornerX == 1 && g2.CornerY == -1);
        c.Check("bg-linear-45 / -bg-linear-45", Checker.R("bg-linear-45").Gradient is { Angle: 45, Oklab: true } && Checker.R("-bg-linear-45").Gradient.Angle == -45);
        c.Check("bg-linear-[25deg] (sRGB)", Checker.R("bg-linear-[25deg]").Gradient is { Kind: GradientKind.Linear, Angle: 25, Oklab: false });
        c.Check("bg-linear-to-b/srgb", Checker.R("bg-linear-to-b/srgb").Gradient is { Angle: 180, Oklab: false });
        c.Check("bg-gradient-to-l (tên v3)", Checker.R("bg-gradient-to-l").Gradient is { Kind: GradientKind.Linear, Angle: 270, Oklab: true });
        c.Check("bg-radial", Checker.R("bg-radial").Gradient.Kind == GradientKind.Radial);
        var g3 = Checker.R("bg-linear-to-b from-10% via-white via-30% to-transparent to-[90%]").Gradient;
        c.Check("from-10% via-* via-30% to-[90%]", g3.FromPos == Length.Fraction(0.1f) && g3.ViaPos == Length.Fraction(0.3f)
            && g3.ToPos == Length.Fraction(0.9f) && g3.Via == Rgba.White && g3.To == Rgba.Transparent);
        c.Check("via-none", Checker.R("via-white via-none").Gradient.Via is null);
        c.Check("from-fx-background theo dark mode", Checker.R("from-fx-background").Gradient.From == Rgba.White
            && Checker.R("from-fx-background", wide with { Dark = true }).Gradient.From == Rgba.Black);
        c.Check("bg-* màu vẫn giữ khi có gradient", Checker.R("bg-white bg-linear-to-r").Background == Rgba.White);
        c.Check("bg-none xoá gradient", Checker.R("bg-linear-to-r bg-none").Gradient.Kind == GradientKind.None);

        // ---- Chữ ----
        c.Check("line-clamp-3 / line-clamp-[8] / line-clamp-none", Checker.R("line-clamp-3") is { LineClampValue: 3, Overflow: Overflow.Hidden }
            && Checker.R("line-clamp-[8]").LineClampValue == 8 && Checker.R("line-clamp-2 line-clamp-none").LineClampValue == 0);
        c.Check("text-balance / text-pretty / text-wrap / text-nowrap", Checker.R("text-balance").WrapStyleValue == TextWrapStyle.Balance
            && Checker.R("text-pretty").WrapStyleValue == TextWrapStyle.Pretty && Checker.R("text-nowrap").NoWrapValue == true
            && Checker.R("text-nowrap text-wrap").NoWrapValue == false);
        c.Check("tabular-nums / proportional-nums", Checker.R("tabular-nums").TabularNumsValue == true && Checker.R("tabular-nums proportional-nums").TabularNumsValue == false);
        c.Check("underline-offset-4 / -underline-offset-2", Checker.R("underline-offset-4").UnderlineOffsetValue == 4 && Checker.R("-underline-offset-2").UnderlineOffsetValue == -2);
        c.Check("decoration-2 decoration-red-500", Checker.R("decoration-2 decoration-red-500") is var d1
            && d1.DecorationThicknessValue == 2 && d1.DecorationColorValue == Theme.Colors["red-500"]);
        c.Check("tracking-[0.2em] / -tracking-[0.01em] / tracking-[2px]", Checker.Near(Checker.R("tracking-[0.2em]").TrackingValue!.Value, 0.2f)
            && Checker.Near(Checker.R("-tracking-[0.01em]").TrackingValue!.Value, -0.01f) && Checker.R("tracking-[2px]").TrackingPxValue == 2);

        // ---- Layout ----
        c.Check("aspect-square / aspect-video / aspect-[4/3] / aspect-auto", Checker.R("aspect-square").AspectRatio == 1
            && Checker.Near(Checker.R("aspect-video").AspectRatio, 16f / 9f) && Checker.Near(Checker.R("aspect-[4/3]").AspectRatio, 4f / 3f)
            && Checker.R("aspect-video aspect-auto").AspectRatio == 0);
        c.Check("col-start-2 col-end-4 row-start-1 -row-end-1", Checker.R("col-start-2 col-end-4 row-start-1 -row-end-1") is
            { ColStart: 2, ColEnd: 4, RowStart: 1, RowEnd: -1 });
        c.Check("col-[span_2] / row-[1_/_3] / col-[1_/_-1]", Checker.R("col-[span_2]").ColSpan == 2
            && Checker.R("row-[1_/_3]") is { RowStart: 1, RowEnd: 3 } && Checker.R("col-[1_/_-1]") is { ColStart: 1, ColEnd: -1 });
        c.Check("grid-flow-col / grid-flow-dense", Checker.R("grid-flow-col") is { GridFlowColumn: true, GridFlowDense: false }
            && Checker.R("grid-flow-row-dense") is { GridFlowColumn: false, GridFlowDense: true });
        c.Check("min-[900px]: / max-[640px]:", Checker.R("min-[900px]:flex").Display == Display.Flex
            && Checker.R("min-[1100px]:flex").Display == Display.Block && Checker.R("max-[640px]:hidden").Display == Display.Block
            && Checker.R("max-[640px]:hidden", wide with { ViewportWidth = 600 }).Display == Display.None);
        c.Check("2xl: / max-2xl:", Checker.R("2xl:flex").Display == Display.Block && Checker.R("max-2xl:flex").Display == Display.Flex);
        c.Check("max-w-[calc(100%-2rem)]", Checker.R("max-w-[calc(100%-2rem)]").MaxWidth is { Kind: LengthKind.Fraction, Value: 1, Offset: -32 } mw
            && mw.Resolve(400, 16) == 368);
        c.Check("w-[calc(100vw-4rem)] / h-[50vh]", Checker.R("w-[calc(100vw-4rem)]").Width == Length.Px(936) && Checker.R("h-[50vh]").Height == Length.Px(350));
        c.Check("-translate-x-full / translate-1/2 / -translate-y-1/2", Checker.R("-translate-x-full").TranslateX == Length.Fraction(-1)
            && Checker.R("translate-1/2") is var t2 && t2.TranslateX == Length.Fraction(0.5f) && t2.TranslateY == Length.Fraction(0.5f)
            && Checker.R("-translate-y-1/2").TranslateY == Length.Fraction(-0.5f));
        c.Check("top-1/2 / inset-[10%]", Checker.R("top-1/2").Inset.Top == Length.Fraction(0.5f) && Checker.R("inset-[10%]").Inset.Left == Length.Fraction(0.1f));
        c.Check("rounded-[4px_8px] / rounded-[1px_2px_3px_4px]", Checker.R("rounded-[4px_8px]").CornerRadii is { TopLeft: 4, TopRight: 8, BottomRight: 4, BottomLeft: 8 }
            && Checker.R("rounded-[1px_2px_3px_4px]").CornerRadii is { TopLeft: 1, TopRight: 2, BottomRight: 3, BottomLeft: 4 });
        c.Check("shadow-2xs / shadow-xs / ring-[3px] / leading-none / outline-hidden", Checker.R("shadow-2xs").Shadow == ShadowPreset.TwoXs
            && Checker.R("shadow-xs").Shadow == ShadowPreset.Xs && Checker.R("ring-[3px]").RingWidth == 3
            && Checker.R("leading-none").LineHeightValue == Length.Fraction(1) && Checker.R("outline-2 outline-hidden").OutlineWidth == 0);

        var unknown = Tailwind.Get("bg-linear-to-r/oklch bg-radial-[at_top] has-[>svg]:px-3 group-data-[x]:p-2 @sm:flex text-shadow-sm").Unknown;
        c.Check("chưa hỗ trợ vẫn được báo (oklch, bg-radial-[…], has-*, group-data-*, @container, text-shadow)", unknown.Count == 6);
        Console.WriteLine($"Kiểm tra utility mới: tổng {c.Pass} đạt, {c.Fail} lỗi.");
    }
}
