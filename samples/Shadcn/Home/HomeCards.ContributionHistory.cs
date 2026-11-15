using GotX;
using GotX.Components;

static partial class HomeCards
{
    static readonly (string Month, int Amount)[] Contributions = [("Dec", 800), ("Jan", 1100), ("Feb", 900), ("Mar", 1300), ("Apr", 750)];

    // Mỗi cột một màu chart-N như data-[index=N]:bg-chart-N của bản gốc; chuỗi dựng sẵn, không nối class mỗi frame.
    static readonly string[] ContributionBars =
    [
        "min-h-2 rounded-lg bg-chart-1", "min-h-2 rounded-lg bg-chart-2", "min-h-2 rounded-lg bg-chart-3",
        "min-h-2 rounded-lg bg-chart-4", "min-h-2 rounded-lg bg-chart-5",
    ];

    static readonly string[] BarKeys = ["contribution-bar-0", "contribution-bar-1", "contribution-bar-2", "contribution-bar-3", "contribution-bar-4"];

    // Bản gốc đặt height theo % của cột cao 200px rồi để flex co lại chừa chỗ cho nhãn; ở đây tính thẳng ra px:
    // 200 − nhãn 16 − gap 8 = 176px cho cột cao nhất.
    const float ContributionPlot = 176, ContributionMax = 1300;

    /// <summary>contribution-history.tsx: biểu đồ cột 5 tháng (dựng bằng div như bản gốc), hai ô tóm tắt, nút xem báo cáo.</summary>
    public static Div ContributionHistory(Window w)
    {
        var chart = w.Div().Class("flex h-[200px] w-full items-end gap-3").Key("contribution-chart");
        for (int i = 0; i < Contributions.Length; i++)
        {
            var bar = w.Div().Class(ContributionBars[i]).Key(BarKeys[i]);
            bar.Style.Height = Length.Px(MathF.Round(Contributions[i].Amount / ContributionMax * ContributionPlot));
            chart.Child(w.Div().Class("flex h-full flex-1 flex-col justify-end gap-2")
                .Child(bar)
                .Child(w.Div().Class("text-center text-xs text-muted-foreground").Child(Contributions[i].Month)));
        }

        return HomeUi.Card(w).Key("contribution-card")
            .Child(HomeUi.Header(w)
                .Child(Ui.CardTitle(w, "Contribution History"))
                .Child(Ui.CardDescription(w, "Last 6 months of activity")))
            .Child(HomeUi.Content(w).Child(chart))
            .Child(HomeUi.Content(w).Child(w.Div().Class("grid w-full grid-cols-1 gap-3 xl:grid-cols-2")
                .Child(Summary(w, "Upcoming", "May 2024", "Scheduled"))
                // Ô thứ hai chỉ hiện từ xl như bản gốc (hidden … xl:flex).
                .Child(Summary(w, "Savings Plan", "Accelerated", "Recurring").Class("hidden xl:flex"))))
            .Child(HomeUi.Footer(w).Child(Ui.Button(w, "View Full Report").Class("w-full").Key("view-full-report")));
    }

    static Div Summary(Window w, string label, string value, string note) =>
        HomeUi.MutedStack(w).Child(w.Div().Class("flex flex-col gap-1")
            .Child(w.Div().Class(HomeUi.Caps).Child(label))
            .Child(w.Div().Class("text-base font-semibold").Child(value))
            .Child(w.Div().Class("text-sm text-muted-foreground").Child(note)));
}
