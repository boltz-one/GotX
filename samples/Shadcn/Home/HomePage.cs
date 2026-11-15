using GotX;

/// <summary>
/// Trang chủ của shadcn/ui (apps/v4/app/(app)/(root)): hero, lưới thẻ demo chủ đề tài chính, chân trang. Thẻ có state là
/// view con (chỉ thẻ đó vẽ lại khi gõ/bấm), thẻ tĩnh là hàm trong <see cref="HomeCards"/>.
/// </summary>
public sealed class HomePage : View
{
    public static readonly SitePage Page = new("home", "Home", static app => app.New(new HomePage()));

    // Lưới theo cột như bản gốc (mỗi cột là một chồng thẻ cao thấp khác nhau). Bản gốc ẩn bớt cột khi hẹp và thay cả lưới
    // bằng ảnh chụp dưới md; ở đây mọi thẻ luôn hiện: 1 cột → md 2 → lg 3 → xl 4, cột thứ năm (bản gốc chỉ hiện từ 1900px)
    // trải thành một hàng thẻ bên dưới.
    const string Grid = "grid w-full max-w-[1600px] grid-cols-1 gap-6 md:grid-cols-2 md:gap-8 lg:grid-cols-3 lg:gap-6 xl:grid-cols-4";
    const string Column = "flex min-w-0 flex-col gap-6 md:gap-8 lg:gap-6";
    const string LastColumn = "flex min-w-0 flex-col gap-6 md:col-span-2 md:grid md:grid-cols-2 md:gap-8 lg:gap-6 xl:col-span-4 xl:grid-cols-4";

    public UiElementsCard Elements { get; private set; } = null!;
    public SidebarNavCard Sidebar { get; private set; } = null!;
    public NewMilestoneCard Milestone { get; private set; } = null!;
    public PayoutThresholdCard Payout { get; private set; } = null!;
    public AccountAccessCard Access { get; private set; } = null!;
    public ChatCard Chat { get; private set; } = null!;
    public PaymentsCard Payments { get; private set; } = null!;
    public NotificationsCard Notifications { get; private set; } = null!;

    protected override void OnCreated()
    {
        Elements = App.New(new UiElementsCard());
        Sidebar = App.New(new SidebarNavCard());
        Milestone = App.New(new NewMilestoneCard());
        Payout = App.New(new PayoutThresholdCard());
        Access = App.New(new AccountAccessCard());
        Chat = App.New(new ChatCard());
        Payments = App.New(new PaymentsCard());
        Notifications = App.New(new NotificationsCard());
    }

    public override Div Render(Window w) =>
        w.Div().Class("flex flex-col").Key("home")
            .Child(HomeHero.Hero(w))
            // Nền muted (sáng) / background (tối) như khối demo gốc; hai dải gradient mờ trên/dưới bị bỏ (Core chưa có gradient).
            .Child(w.Div().Class("flex justify-center bg-muted p-4 md:p-12 lg:p-6 dark:bg-background").Key("home-demo")
                .Child(w.Div().Class(Grid).Key("home-grid")
                    .Child(w.Div().Class(Column)
                        .Child(w.View(Elements))
                        .Child(w.View(Sidebar))
                        .Child(HomeCards.SavingsTargets(w)))
                    .Child(w.Div().Class(Column)
                        .Child(HomeCards.ContributionHistory(w))
                        .Child(HomeCards.ClaimableBalance(w))
                        .Child(HomeCards.DividendIncome(w)))
                    .Child(w.Div().Class(Column)
                        .Child(w.View(Milestone))
                        .Child(w.View(Payout))
                        .Child(w.View(Access)))
                    .Child(w.Div().Class(Column)
                        .Child(HomeCards.QrConnect(w))
                        .Child(w.View(Chat))
                        .Child(w.View(Payments)))
                    // Mỗi thẻ bọc trong một ô block để ô lưới không kéo thẻ cao bằng thẻ cao nhất của hàng.
                    .Child(w.Div().Class(LastColumn)
                        .Child(w.Div().Child(HomeCards.DistributeTrack(w)))
                        .Child(w.Div().Child(HomeCards.Analytics(w)))
                        .Child(w.Div().Child(w.View(Notifications)))
                        .Child(w.Div().Child(HomeCards.PowerUsage(w))))))
            .Child(HomeHero.Footer(w));
}
