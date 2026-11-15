using GotX;
using GotX.Components;

static partial class HomeCards
{
    // Đường xu hướng của bản gốc (viewBox 100×86, kéo giãn theo thẻ, nét non-scaling 1.5px). Core kéo giãn cả độ dày nét,
    // nên toạ độ được đổi sẵn sang khung 330×116 (cỡ thật của ô ở cửa sổ 1440) để nét gần đúng 1.5px sau khi object-fill.
    const float TrendW = 330, TrendH = 116;
    static readonly (float X, float Y)[] Trend = [(0, 52), (18, 40), (36, 46), (54, 70), (72, 50), (100, 49)];
    static readonly VectorPath TrendLine = BuildTrend(area: false);
    static readonly VectorPath TrendArea = BuildTrend(area: true);

    static VectorPath BuildTrend(bool area)
    {
        var d = new System.Text.StringBuilder();
        foreach (var (x, y) in Trend)
            d.Append(d.Length == 0 ? 'M' : 'L').Append(FormattableString.Invariant($"{x * TrendW / 100:0.##} {y * TrendH / 86:0.##}"));
        if (!area) return VectorPath.Stroke(d.ToString(), 1.5f, TrendW, TrendH);
        d.Append(FormattableString.Invariant($"V{TrendH}H0Z"));
        return VectorPath.Fill(d.ToString(), TrendW, TrendH);
    }

    /// <summary>analytics-card.tsx: thẻ cỡ sm, số khách + badge tăng trưởng, đồ thị vùng chạm đáy thẻ (pb-0).</summary>
    public static Div Analytics(Window w) =>
        Ui.Card(w, CardSize.Sm).Class("w-full gap-4 rounded-[24px] py-4 pb-0 shadow-sm ring-foreground/5 dark:ring-foreground/10").Key("analytics-card")
            .Child(Ui.CardHeader(w, CardSize.Sm, hasAction: true).Class("px-4")
                .Child(Ui.CardTitle(w, "Analytics", CardSize.Sm))
                .Child(Ui.CardAction(w).Child(Ui.Button(w, "View Analytics", ButtonVariant.Outline, ButtonSize.Sm).Key("view-analytics")))
                .Child(w.Div().Class("flex items-center gap-1.5 text-sm text-muted-foreground")
                    .Child("418.2K Visitors")
                    .Child(Ui.Badge(w, "+10%"))))
            // Hai lớp cùng một màu chữ (text-chart-1): vùng tô mờ 28% và đường nét đè lên trên.
            .Child(w.Div().Class("relative h-[116px] w-full text-chart-1").Key("analytics-chart")
                .Child(w.Path(TrendArea).Class("absolute inset-0 size-full object-fill opacity-30"))
                .Child(w.Path(TrendLine).Class("absolute inset-0 size-full object-fill")));
}
