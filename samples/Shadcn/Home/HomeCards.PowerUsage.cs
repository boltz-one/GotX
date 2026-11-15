using GotX;
using GotX.Components;

static partial class HomeCards
{
    static readonly (string Hour, float Usage)[] Power =
        [("6a", 1.2f), ("8a", 2.8f), ("10a", 3.1f), ("12p", 2.4f), ("2p", 3.4f), ("4p", 2.9f), ("6p", 3.8f), ("8p", 3.2f)];

    // Cột cao nhất = 140 − nhãn 16 − gap 6 (xem ghi chú ở ContributionHistory).
    const float PowerPlot = 118, PowerMax = 3.8f;

    /// <summary>power-usage.tsx: biểu đồ cột theo giờ và hai số liệu công suất.</summary>
    public static Div PowerUsage(Window w)
    {
        var chart = w.Div().Class("flex h-[140px] w-full items-end gap-2").Key("power-chart");
        foreach (var (hour, usage) in Power)
        {
            var bar = w.Div().Class("min-h-2 rounded-t bg-chart-2");
            bar.Style.Height = Length.Px(MathF.Round(usage / PowerMax * PowerPlot));
            chart.Child(w.Div().Class("flex h-full flex-1 flex-col justify-end gap-1.5")
                .Child(bar)
                .Child(w.Div().Class("text-center text-xs text-muted-foreground").Child(hour)));
        }

        return HomeUi.Card(w).Key("power-card")
            .Child(HomeUi.Header(w)
                .Child(Ui.CardTitle(w, "Power Usage"))
                .Child(Ui.CardDescription(w, "Whole Home")))
            .Child(HomeUi.Content(w).Class("flex flex-col gap-4")
                .Child(chart)
                .Child(Ui.Separator(w))
                .Child(w.Div().Class("grid grid-cols-2 gap-4")
                    .Child(PowerStat(w, "Currently Using", "3.4 kW"))
                    .Child(PowerStat(w, "Solar Gen", "+1.2 kW"))));
    }

    static Div PowerStat(Window w, string label, string value) =>
        w.Div().Class("flex flex-col gap-0.5")
            .Child(w.Div().Class("text-sm text-muted-foreground").Child(label))
            .Child(w.Div().Class("text-lg font-semibold").Child(value));
}
