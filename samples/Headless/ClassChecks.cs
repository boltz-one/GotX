using GotX;

/// <summary>Phần 1: bộ biên dịch class Tailwind – mỗi dòng biên dịch một chuỗi class rồi so field của <see cref="Style"/>.</summary>
static class ClassChecks
{
    public static void Run(Checker c)
    {
        var wide = Checker.Wide;
        var narrow = wide with { ViewportWidth = 600 };
        var dark = wide with { Dark = true };
        c.Check("p-4", Checker.R("p-4").Padding.Top == Length.Rems(1));
        c.Check("px-2.5", Checker.R("px-2.5").Padding.Left == Length.Rems(0.625f) && Checker.R("px-2.5").Padding.Top.IsAuto);
        c.Check("p-17 (bậc tuỳ ý v4)", Checker.R("p-17").Padding.Top == Length.Rems(4.25f));
        c.Check("-mt-2", Checker.R("-mt-2").Margin.Top == Length.Rems(-0.5f));
        c.Check("w-1/2", Checker.R("w-1/2").Width == Length.Fraction(0.5f));
        c.Check("max-w-md", Checker.R("max-w-md").MaxWidth == Length.Rems(28));
        c.Check("w-[320px]", Checker.R("w-[320px]").Width == Length.Px(320));
        c.Check("size-10", Checker.R("size-10") is { Width: var sw, Height: var sh } && sw == Length.Rems(2.5f) && sh == sw);
        c.Check("h-screen", Checker.R("h-screen").Height == Length.Px(700));
        c.Check("gap-x-3", Checker.R("gap-x-3").ColumnGap == Length.Rems(0.75f) && Checker.R("gap-x-3").RowGap.IsAuto);
        c.Check("inset-x-0", Checker.R("inset-x-0").Inset.Left == Length.Px(0) && Checker.R("inset-x-0").Inset.Top.IsAuto);
        c.Check("bg-blue-500/50", Checker.R("bg-blue-500/50").Background is var b1 && Checker.Near(b1.A, 0.5f) && Checker.Near(b1.R, 0x3b / 255f));
        c.Check("bg-[#ff000080]", Checker.Near(Checker.R("bg-[#ff000080]").Background.A, 0.5f));
        c.Check("bg-[rgb(255,0,0)]", Checker.Near(Checker.R("bg-[rgb(255,0,0)]").Background.R, 1));
        c.Check("text-sm/6", Checker.R("text-sm/6") is var t1 && t1.FontSizeValue == Length.Rems(0.875f) && t1.LineHeightValue == Length.Rems(1.5f));
        c.Check("text-white/80", Checker.Near(Checker.R("text-white/80").TextColorValue!.Value.A, 0.8f));
        c.Check("text-[15px]", Checker.R("text-[15px]").FontSizeValue == Length.Px(15));
        c.Check("leading-tight", Checker.R("leading-tight").LineHeightValue == Length.Fraction(1.25f));
        c.Check("font-[550]", Checker.R("font-[550]").FontWeightValue == (FontWeight)550);
        c.Check("rounded-t-lg", Checker.R("rounded-t-lg").CornerRadii is { TopLeft: 8, TopRight: 8, BottomLeft: 0 });
        c.Check("shadow-lg shadow-indigo-500/30", Checker.R("shadow-lg shadow-indigo-500/30") is var s1 && s1.Shadow == ShadowPreset.Lg && Checker.Near(s1.ShadowColor!.Value.A, 0.3f));
        c.Check("shadow-[0_4px_10px_rgba(0,0,0,0.2)]", Checker.R("shadow-[0_4px_10px_rgba(0,0,0,0.2)]") is var s2 && s2.Shadow == ShadowPreset.Custom && s2.CustomShadow.OffsetY == 4 && s2.CustomShadow.Blur == 10 && Checker.Near(s2.CustomShadow.Color.A, 0.2f));
        c.Check("ring-2 ring-offset-2 ring-indigo-500", Checker.R("ring-2 ring-offset-2 ring-indigo-500") is var r1 && r1.RingWidth == 2 && r1.RingOffsetWidth == 2 && r1.RingColor is not null);
        c.Check("opacity-75", Checker.Near(Checker.R("opacity-75").Opacity, 0.75f));
        c.Check("truncate", Checker.R("truncate") is var tr && tr.Overflow == Overflow.Hidden && tr.NoWrapValue == true && tr.EllipsisValue == true);
        c.Check("md:flex-row (rộng)", Checker.R("flex flex-col md:flex-row").FlexDirection == FlexDirection.Row);
        c.Check("md:flex-row (hẹp)", Checker.R("flex flex-col md:flex-row", narrow).FlexDirection == FlexDirection.Column);
        c.Check("max-md:hidden (hẹp)", Checker.R("max-md:hidden", narrow).Display == Display.None && Checker.R("max-md:hidden").Display == Display.Block);
        c.Check("dark:bg-black", Checker.R("dark:bg-black").Background.A == 0 && Checker.R("dark:bg-black", dark).Background.A == 1);
        c.Check("p-4 p-2 (sau thắng)", Checker.R("p-4 p-2").Padding.Top == Length.Rems(0.5f));
        c.Check("p-2! p-4 (important)", Checker.R("p-2! p-4").Padding.Top == Length.Rems(0.5f));
        c.Check("hover: chỉ áp khi hover", Checker.R("bg-white hover:bg-red-500").Background == Rgba.White
              && Checker.R("bg-white hover:bg-red-500", null, StateVariant.Hover).Background == Theme.Colors["red-500"]);
        c.Check("md:hover: (variant chồng)", Checker.R("md:hover:bg-red-500", narrow, StateVariant.Hover).Background.A == 0
              && Checker.R("md:hover:bg-red-500", wide, StateVariant.Hover).Background.A == 1);
        c.Check("bg-red-500! thắng hover:", Checker.R("bg-red-500! hover:bg-blue-500", null, StateVariant.Hover).Background == Theme.Colors["red-500"]);
        c.Check("self-center", Checker.R("self-center").AlignSelf == Align.Center);
        c.Check("absolute top-2 -right-1", Checker.R("absolute top-2 -right-1") is var a1 && a1.Position == Position.Absolute && a1.Inset.Right == Length.Rems(-0.25f));
        c.Check("overflow-y-auto", Checker.R("overflow-y-auto") is var o1 && o1.ScrollY && !o1.ScrollX && o1.Overflow == Overflow.Hidden);
        c.Check("flex-wrap", Checker.R("flex flex-wrap").FlexWrap);
        c.Check("grid grid-cols-3", Checker.R("grid grid-cols-3") is var g1 && g1.Display == Display.Grid && g1.GridColumns!.Count == 3);
        c.Check("grid-cols-[200px_1fr_auto]", Checker.R("grid-cols-[200px_1fr_auto]").GridColumns is { Count: 3 } g2
              && g2.Tracks[0].Kind == TrackKind.Fixed && g2.Tracks[1].Kind == TrackKind.Fr && g2.Tracks[2].Kind == TrackKind.Auto);
        c.Check("col-span-2 row-span-full", Checker.R("col-span-2 row-span-full") is { ColSpan: 2, RowSpan: -1 });
        c.Check("-translate-y-1 translate-x-1/2", Checker.R("-translate-y-1 translate-x-1/2") is var t2 && t2.TranslateY == Length.Rems(-0.25f) && t2.TranslateX == Length.Fraction(0.5f));
        c.Check("scale-95 / scale-[1.02]", Checker.Near(Checker.R("scale-95").ScaleX, 0.95f) && Checker.Near(Checker.R("scale-95").ScaleY, 0.95f) && Checker.Near(Checker.R("scale-[1.02]").ScaleX, 1.02f));
        c.Check("scale-x-150 / scale-y-50", Checker.R("scale-x-150 scale-y-50") is var sxy && Checker.Near(sxy.ScaleX, 1.5f) && Checker.Near(sxy.ScaleY, 0.5f));
        c.Check("skew-x-12 / -skew-y-6 / skew-[10deg] / skew-x-[0.1rad]", Checker.R("skew-x-12 -skew-y-6") is var sk && sk.SkewX == 12 && sk.SkewY == -6
              && Checker.R("skew-[10deg]") is { SkewX: 10, SkewY: 10 } && Checker.Near(Checker.R("skew-x-[0.1rad]").SkewX, 5.7296f));
        c.Check("origin-top-left / origin-[25%_75%]", Checker.R("origin-top-left") is var og1 && og1.OriginX == Length.Fraction(0) && og1.OriginY == Length.Fraction(0)
              && Checker.R("origin-[25%_75%]") is var og2 && og2.OriginX == Length.Fraction(0.25f) && og2.OriginY == Length.Fraction(0.75f));
        c.Check("border-dashed / border-dotted", Checker.R("border-dashed").BorderStyle == BorderStyle.Dashed && Checker.R("border-dotted").BorderStyle == BorderStyle.Dotted);
        c.Check("border-2 border-none → không chiếm chỗ", Checker.R("border-2 border-none").BorderBox == default);
        c.Check("outline-2 outline-offset-2 outline-indigo-500", Checker.R("outline-2 outline-offset-2 outline-indigo-500") is var ol
              && ol.OutlineWidth == 2 && ol.OutlineOffset == 2 && ol.OutlineColor == Theme.Colors["indigo-500"] && ol.OutlineStyle == BorderStyle.Solid);
        c.Check("outline outline-dashed / outline-none", Checker.R("outline outline-dashed") is { OutlineWidth: 1, OutlineStyle: BorderStyle.Dashed } && Checker.R("outline-2 outline-none").OutlineWidth == 0);
        c.Check("transition-colors duration-300 ease-out delay-100", Checker.R("transition-colors duration-300 ease-out delay-100") is var tr2
              && tr2.Transition == TransitionProps.Colors && tr2.DurationMs == 300 && tr2.Easing == Easing.Out && tr2.DelayMs == 100);
        c.Check("duration-[0.4s]", Checker.R("duration-[0.4s]").DurationMs == 400);
        c.Check("animate-ping", Checker.R("animate-ping").Animation == AnimationKind.Ping);
        c.Check("cubic-bezier: ease(.25,.1,.25,1) tại 0.5 ≈ 0.8024", MathF.Abs(Ease.Bezier(0.25f, 0.1f, 0.25f, 1f, 0.5f) - 0.8024f) < 0.002f);
        c.Check("border-t-2", Checker.R("border-t-2").Border == new Edges(2, 0, 0, 0));
        c.Check("border-x", Checker.R("border-x").Border == new Edges(0, 1, 0, 1));
        c.Check("border-l-4 border-l-indigo-500", Checker.R("border-l-4 border-l-indigo-500") is var bl1 && bl1.Border.Left == 4 && bl1.BorderLeftColor == Theme.Colors["indigo-500"] && bl1.BorderColorValue is null);
        c.Check("border border-slate-200 border-b-red-500", Checker.R("border border-slate-200 border-b-red-500") is var bb1
              && bb1.Border == new Edges(1, 1, 1, 1) && bb1.BorderColorValue == Theme.Colors["slate-200"] && bb1.BorderBottomColor == Theme.Colors["red-500"]);
        c.Check("border-b-red-500 rồi border-slate-200 (ghi đè cả 4 cạnh)", Checker.R("border-b-red-500 border-slate-200").BorderBottomColor is null);
        c.Check("border-blue-500 không bị hiểu là cạnh b", Checker.R("border-blue-500").BorderColorValue == Theme.Colors["blue-500"]);
        c.Check("divide-y divide-slate-200", Checker.R("divide-y divide-slate-200") is var dv && dv.DivideY == 1 && dv.DivideColor == Theme.Colors["slate-200"]);
        c.Check("rotate-45 / -rotate-12 / rotate-[0.25turn]", Checker.R("rotate-45").Rotate == 45 && Checker.R("-rotate-12").Rotate == -12 && Checker.Near(Checker.R("rotate-[0.25turn]").Rotate, 90));
        c.Check("animate-spin", Checker.R("animate-spin").Animation == AnimationKind.Spin);
        var unknown = Tailwind.Get("rotate-x-12 z-10 border-double peer-hover:bg-red-500 flex").Unknown;
        c.Check("class chưa hỗ trợ được báo", unknown.Count == 4 && !unknown.Contains("flex"));
        Console.WriteLine($"Kiểm tra class: {c.Pass} đạt, {c.Fail} lỗi. Chưa hỗ trợ: {string.Join(", ", unknown)}");
    }
}
