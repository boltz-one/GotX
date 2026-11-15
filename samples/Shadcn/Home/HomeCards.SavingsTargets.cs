using GotX;
using GotX.Components;

/// <summary>Các thẻ tĩnh (không có state) của trang chủ: mỗi thẻ một hàm trả về <see cref="Div"/>, mỗi hàm một file.</summary>
static partial class HomeCards
{
    /// <summary>ItemDescription dùng làm nhãn chữ hoa nhỏ ("RETIREMENT", "UPCOMING").</summary>
    static Div Caption(Window w, string text) =>
        w.Div().Class("line-clamp-2 text-left text-xs font-medium tracking-wider text-muted-foreground uppercase").Child(text);

    /// <summary>Item muted xếp dọc (<c>flex-col items-stretch</c>); không dùng ItemContent (flex-1 sẽ chia theo chiều cao).</summary>
    static Div MutedColumn(Window w) => HomeUi.MutedItem(w).Class("flex-col flex-nowrap items-stretch");

    /// <summary>savings-targets.tsx: hai mục tiêu tiết kiệm kèm thanh tiến độ.</summary>
    public static Div SavingsTargets(Window w) =>
        HomeUi.Card(w).Key("savings-card")
            .Child(HomeUi.Header(w)
                .Child(Ui.CardTitle(w, "Savings Targets"))
                .Child(Ui.CardDescription(w, "Active milestones for 2024 across your portfolio. Monitor how close you are to each savings goal.")))
            .Child(HomeUi.Content(w).Child(Ui.ItemGroup(w).Class("gap-3")
                .Child(SavingsTarget(w, "Retirement", "$420,000", 65, "65% achieved", "$273,000"))
                .Child(SavingsTarget(w, "Real Estate", "$85,000", 32, "32% achieved", "$27,200"))))
            // CardFooter là hàng flex: vừa một dòng (xl) thì mô tả co theo chữ, nằm trái; xuống dòng (lg) thì nó rộng hết
            // hàng và text-center căn giữa từng dòng.
            .Child(HomeUi.Footer(w)
                .Child(Ui.CardDescription(w, "You have not met your targets for this year.").Class("text-center")));

    static Div SavingsTarget(Window w, string label, string amount, float percent, string achieved, string saved) =>
        MutedColumn(w)
            .Child(w.Div().Class("flex flex-col gap-3")
                .Child(Caption(w, label))
                .Child(w.Div().Class("text-3xl font-semibold tabular-nums").Child(amount))
                .Child(Ui.Progress(w, percent)))
            .Child(w.Div().Class("flex items-center justify-between gap-2")
                .Child(w.Div().Class("text-sm text-muted-foreground").Child(achieved))
                .Child(w.Div().Class("text-sm font-medium tabular-nums").Child(saved)));
}