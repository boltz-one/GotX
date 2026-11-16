using GotX;
using GotX.Components;

static partial class HomeCards
{
    sealed record Holding(string Name, string Shares, int[] Quarters)
    {
        // Chiều cao 4 cột mini tính một lần: % của ô cao 32px theo quý cao nhất của chính mã đó (min-h-1).
        public readonly float[] Heights = Array.ConvertAll(Quarters, q => MathF.Max(4, q * 32f / Quarters.Max()));
    }

    static readonly Holding[] Holdings =
    [
        new("Vanguard", "450 Shares", [380, 420, 390, 652]),
        new("S&P 500 VOO", "112 Shares", [180, 210, 320, 218]),
        new("Apple AAPL", "85 Shares", [60, 70, 120, 90]),
        new("Realty Income", "320 Shares", [240, 260, 280, 360]),
    ];

    /// <summary>Nút "×" nền muted ở góc thẻ (Button ghost icon-sm + bg-muted).</summary>
    internal static Div Dismiss(Window w, string key) =>
        Ui.Button(w, ButtonVariant.Ghost, ButtonSize.IconSm).Class("bg-muted").Key(key).AriaLabel("Dismiss").Child(HugeIcons.Cancel01(w));

    /// <summary>dividend-income.tsx: danh sách mã cổ phiếu, mỗi dòng có biểu đồ cột mini theo quý (ẩn dưới md như bản gốc).</summary>
    public static Div DividendIncome(Window w)
    {
        var list = Ui.ItemGroup(w);
        foreach (var holding in Holdings)
        {
            var bars = w.Div().Class("hidden h-8 w-24 shrink-0 items-end gap-1 md:flex");
            foreach (float h in holding.Heights)
            {
                var bar = w.Div().Class("min-h-1 flex-1 rounded-t-sm bg-chart-2");
                bar.Style.Height = Length.Px(h);
                bars.Child(bar);
            }
            list.Child(HomeUi.MutedItem(w)
                .Child(Ui.ItemContent(w)
                    .Child(Ui.ItemTitle(w, holding.Name))
                    .Child(Ui.ItemDescription(w, holding.Shares)))
                .Child(bars));
        }

        return HomeUi.Card(w).Key("dividend-card")
            .Child(HomeUi.Header(w, hasAction: true)
                .Child(Ui.CardTitle(w, "Q2 Dividend Income"))
                .Child(Ui.CardAction(w).Child(Dismiss(w, "dismiss-dividend")))
                .Child(Ui.CardDescription(w, "Quarterly dividend payouts across your portfolio holdings.")))
            .Child(HomeUi.Content(w).Child(list));
    }
}