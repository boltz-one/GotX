using GotX;

/// <summary>
/// Trang chủ của shadcn/ui (apps/v4/app/(app)/(root)): hero, khối demo các thẻ, chân trang. Thẻ có state là view con (chỉ
/// thẻ đó vẽ lại khi gõ/bấm), thẻ tĩnh là hàm trong <see cref="HomeCards"/>. Mọi thứ vẽ theo <see cref="HomeStyle"/>.
/// </summary>
public sealed class HomePage : View
{
    public static readonly SitePage Page = new("home", "Home", static app => app.New(new HomePage()));

    // cards/index.tsx: lưới theo cột, mỗi cột là một chồng thẻ. Cột hiện theo breakpoint như bản gốc: cột 1 luôn có, cột 4
    // từ md, cột 2 từ lg, cột 3 từ 1400px, cột 5 từ 1900px; --gap 32px → lg 24px → 1900px 40px.
    const string Gap = " gap-8 lg:gap-6 min-[1900px]:gap-10";
    const string Grid = "grid w-full md:max-w-3xl md:grid-cols-2 lg:max-w-none lg:grid-cols-3 xl:max-w-[1600px] " +
        "min-[1400px]:grid-cols-4 2xl:max-w-[1900px] min-[1900px]:grid-cols-5" + Gap;
    const string Column = "min-w-0 flex-col items-start" + Gap; // hiển thị (flex/hidden) viết trước, breakpoint đè sau
    // Nền muted; dải mờ trên (from-background via-muted to-transparent cao 480px, chỉ ở chế độ sáng) gộp vào nền: phần
    // trong suốt của nó nằm trên đúng màu muted nên tương đương gradient background → muted trong 240px đầu.
    const string Demo = "relative hidden flex-col items-center overflow-hidden bg-muted bg-linear-to-b from-background to-muted to-[240px] " +
        "p-12 md:flex lg:p-6 min-[1900px]:p-12 pb-0! dark:bg-background dark:bg-none";
    // Dải mờ đáy (z-20 của bản gốc): phần tử absolute vẽ sau nên nằm trên các thẻ.
    const string BottomFade = "absolute inset-x-0 bottom-0 h-48 bg-linear-to-t from-background via-muted/80 to-transparent " +
        "lg:h-80 xl:h-64 dark:via-background/80";

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
        // Component có state (ô nhập, checkbox, menu…) nhớ style đang hiệu lực lúc được tạo.
        using var _ = HomeStyle.Use();
        Elements = App.New(new UiElementsCard());
        Sidebar = App.New(new SidebarNavCard());
        Milestone = App.New(new NewMilestoneCard());
        Payout = App.New(new PayoutThresholdCard());
        Access = App.New(new AccountAccessCard());
        Chat = App.New(new ChatCard());
        Payments = App.New(new PaymentsCard());
        Notifications = App.New(new NotificationsCard());
    }

    public override Div Render(Window w)
    {
        using var _ = HomeStyle.Use();
        return w.Div().Class("flex flex-col").Key("home")
            .Child(HomeHero.Hero(w))
            .Child(w.Div().Class(Demo).Key("home-demo")
                .Child(w.Div().Class(Grid).Key("home-grid")
                    .Child(w.Div().Class("flex " + Column)
                        .Child(w.View(Elements))
                        .Child(w.View(Sidebar))
                        .Child(HomeCards.SavingsTargets(w)))
                    .Child(w.Div().Class("hidden lg:flex " + Column)
                        .Child(HomeCards.ContributionHistory(w))
                        .Child(HomeCards.ClaimableBalance(w))
                        .Child(HomeCards.DividendIncome(w)))
                    .Child(w.Div().Class("hidden min-[1400px]:flex " + Column)
                        .Child(w.View(Milestone))
                        .Child(w.View(Payout))
                        .Child(w.View(Access)))
                    .Child(w.Div().Class("hidden md:flex " + Column)
                        .Child(HomeCards.QrConnect(w))
                        .Child(w.View(Chat))
                        .Child(w.View(Payments)))
                    .Child(w.Div().Class("hidden min-[1900px]:flex " + Column)
                        .Child(HomeCards.DistributeTrack(w))
                        .Child(HomeCards.Analytics(w))
                        .Child(w.View(Notifications))
                        .Child(HomeCards.PowerUsage(w))))
                .Child(w.Div().Class(BottomFade)))
            // Dưới md bản gốc thay cả khối demo bằng ảnh chụp sẵn (rộng 140vw, bị cắt ở mép phải).
            .Child(HomeImages.Mobile(w))
            .Child(HomeHero.Footer(w));
    }
}
