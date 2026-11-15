using GotX;
using GotX.Components;

/// <summary>Các thẻ tĩnh (không có state) của trang chủ: mỗi thẻ một hàm trả về <see cref="Div"/>, mỗi hàm một file.</summary>
static partial class HomeCards
{
    /// <summary>savings-targets.tsx: hai mục tiêu tiết kiệm kèm thanh tiến độ.</summary>
    public static Div SavingsTargets(Window w) =>
        HomeUi.Card(w).Key("savings-card")
            .Child(HomeUi.Header(w)
                .Child(Ui.CardTitle(w, "Savings Targets"))
                .Child(Ui.CardDescription(w, "Active milestones for 2024 across your portfolio. Monitor how close you are to each savings goal.")))
            .Child(HomeUi.Content(w).Child(Ui.ItemGroup(w).Class("gap-3")
                .Child(SavingsTarget(w, "Retirement", "$420,000", 65, "65% achieved", "$273,000"))
                .Child(SavingsTarget(w, "Real Estate", "$85,000", 32, "32% achieved", "$27,200"))))
            .Child(HomeUi.Footer(w)
                .Child(Ui.CardDescription(w, "You have not met your targets for this year.").Class("w-full text-center")));

    static Div SavingsTarget(Window w, string label, string amount, float percent, string achieved, string saved) =>
        HomeUi.MutedStack(w)
            // Cột nội dung không dùng flex-1 của Ui.ItemContent: item đang xếp dọc, flex-1 sẽ chia theo chiều cao.
            .Child(w.Div().Class("flex flex-col gap-3")
                .Child(w.Div().Class(HomeUi.Caps).Child(label))
                .Child(w.Div().Class("text-3xl font-semibold").Child(amount))
                .Child(Ui.Progress(w, percent).Class("h-2")))
            .Child(HomeUi.Pair(w, achieved, saved));
}
