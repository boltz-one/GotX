using GotX;
using GotX.Components;

/// <summary>Bốn thẻ số liệu đầu trang (section-cards.tsx). Không có state nên chỉ là hàm dựng.</summary>
public static class DashboardCards
{
    /// <summary>
    /// Lưới thẻ <c>grid-cols-1 @xl/main:grid-cols-2 @5xl/main:grid-cols-4</c>. Core chưa có container query: vùng main rộng
    /// bằng cửa sổ trừ sidebar 18rem và lề 8px (từ md), nên @xl (36rem) ⇔ cửa sổ ≥ 872px, @5xl (64rem) ⇔ ≥ 1320px.
    /// Thẻ rộng ≥ 250px ở mọi bề rộng có 2 cột trở lên → tiêu đề luôn <c>@[250px]/card:text-3xl</c>.
    /// Nền: <c>bg-gradient-to-t from-primary/5 to-card</c> trên bg-card, shadow-xs.
    /// </summary>
    public static Div Render(Window w)
    {
        var grid = w.Div().Key("section-cards").Class("grid grid-cols-1 gap-4 px-4 lg:px-6 min-[872px]:grid-cols-2 min-[1320px]:grid-cols-4");
        foreach (var stat in Dashboard01Data.Stats)
        {
            var trend = stat.Up ? DashboardIcons.TrendingUpPath : DashboardIcons.TrendingDownPath;
            grid.Child(w.Div().Class("flex flex-col gap-6 rounded-xl border border-border bg-card bg-linear-to-t from-primary/5 to-card py-6 text-card-foreground shadow-xs")
                // CardHeader có CardAction: lưới [1fr auto] hai hàng (cột 1fr không hẹp hơn min-content của tiêu đề).
                .Child(Ui.CardHeader(w, hasAction: true)
                    .Child(Ui.CardDescription(w, stat.Label))
                    // Thứ tự con như DOM của bản gốc (mô tả, tiêu đề, action); vị trí đặt tường minh như CardAction
                    // (col-start-2 row-span-2 row-start-1).
                    .Child(Ui.CardTitle(w, stat.Value).Class("col-start-1 row-start-2 text-2xl font-semibold tabular-nums min-[872px]:text-3xl"))
                    .Child(Ui.CardAction(w).Class("col-start-2 row-start-1")
                        .Child(Ui.Badge(w, BadgeVariant.Outline).Child(w.Path(trend).Class("size-3 shrink-0")).Child(stat.Trend))))
                .Child(w.Div().Class("flex flex-col items-start gap-1.5 px-6 text-sm")
                    .Child(w.Div().Class("flex gap-2 font-medium").Child(w.Div().Class("min-w-0").Child(stat.Headline)).Child(DashboardIcons.Draw(w, trend)))
                    .Child(w.Div().Class("text-muted-foreground").Child(stat.Note))));
        }
        return grid;
    }
}
