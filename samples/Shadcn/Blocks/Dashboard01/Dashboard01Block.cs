using GotX;
using GotX.Components;

/// <summary>
/// Block dashboard-01 của shadcn/ui – "A dashboard with sidebar, charts and data table": sidebar kiểu inset rộng 18rem
/// (ẩn hẳn khi bấm trigger – <c>collapsible="offcanvas"</c>), header, bốn thẻ số liệu, biểu đồ vùng có chọn khoảng thời
/// gian, bảng dữ liệu có tab. Như bản gốc, cả cột nội dung (thẻ inset) cuộn cùng trang còn sidebar đứng yên.
/// Biểu đồ và bảng là view riêng nên thao tác trên chúng không vẽ lại cả trang.
/// Bỏ Ctrl+B (Core chưa có phím tắt toàn cục): ẩn/hiện sidebar bằng nút trigger (chuột hoặc Tab + Enter/Space).
/// </summary>
public sealed class Dashboard01Block : View
{
    public static readonly SitePage Page = new("dashboard-01", "Dashboard 01", static app => app.New(new Dashboard01Block()), FullBleed: true, Style: ComponentStyle.NewYorkV4);

    // Key theo data-slot / chữ của bản gốc.
    public const string TriggerKey = "sidebar-trigger", SidebarKey = "sidebar", ContentKey = "sidebar-inset";
    public const string QuickCreateKey = "quick-create", InboxKey = "inbox", BrandKey = "acme-inc", GitHubKey = "github";

    const string BrandClass =
        "flex h-8 w-full items-center gap-2 overflow-hidden rounded-md p-1.5 text-left text-sm cursor-pointer transition-colors " +
        "hover:bg-sidebar-accent hover:text-sidebar-accent-foreground active:bg-sidebar-accent active:text-sidebar-accent-foreground " +
        "focus-visible:ring-2 focus-visible:ring-sidebar-ring";
    const string QuickCreateClass =
        "flex h-8 min-w-8 flex-1 items-center gap-2 overflow-hidden rounded-md bg-primary p-2 text-left text-sm text-primary-foreground " +
        "cursor-pointer transition-colors duration-200 ease-linear hover:bg-primary/90 hover:text-primary-foreground active:bg-primary/90 " +
        "active:text-primary-foreground focus-visible:ring-2 focus-visible:ring-sidebar-ring";
    // SidebarMenuButton size="lg" + data-[state=open]:bg-sidebar-accent.
    const string UserButton =
        "flex h-12 w-[256px] items-center gap-2 overflow-hidden rounded-md p-2 text-left text-sm cursor-pointer " +
        "hover:bg-sidebar-accent hover:text-sidebar-accent-foreground active:bg-sidebar-accent active:text-sidebar-accent-foreground ";

    SidebarNav _nav = null!;
    DropdownMenu _userMenu = null!;
    DashboardChart _chart = null!;
    DashboardTable _table = null!;
    bool _open = true;

    public SidebarNav Nav => _nav;
    public DropdownMenu UserMenu => _userMenu;
    public DashboardChart Chart => _chart;
    public DashboardTable Table => _table;

    /// <summary>Lựa chọn gần nhất ở sidebar / menu (bản gốc không làm gì với chúng; giữ lại để quan sát được).</summary>
    public string LastAction { get; private set; } = "";

    /// <summary>Sidebar đang hiện (offcanvas: đóng thì biến mất hẳn, vùng nội dung chiếm hết bề ngang).</summary>
    public bool SidebarOpen
    {
        get => _open;
        set { if (_open == value) return; _open = value; Notify(); }
    }

    protected override void OnCreated()
    {
        // Bản sao: hàng Quick Create gắn với instance này (bấm ghi LastAction), không sửa mảng tĩnh dùng chung.
        var groups = (SidebarNavGroup[])Dashboard01Data.Groups.Clone();
        groups[0] =groups[0] with { Lead = QuickCreateRow };
        _nav = App.New(new SidebarNav { Groups = groups, Selectable = false, KeyPrefix = "nav" });
        _userMenu = App.New(new DropdownMenu
        {
            Items = Dashboard01Data.UserMenu, Trigger = UserTrigger, Placement = Placement.RightEnd, MatchTriggerWidth = true, MenuClass = "min-w-56 rounded-lg",
        });
        _chart = App.New(new DashboardChart());
        _table = App.New(new DashboardTable());
        App.Subscribe<SidebarNav.ItemSelected>(_nav, e => LastAction = e.Title);
        App.Subscribe<SidebarNav.MenuSelected>(_nav, e => LastAction = e.Choice.Text + ": " + e.Target.Title);
        App.Subscribe<MenuItemSelected>(_userMenu, e => LastAction = e.Item.Text);
    }

    /// <summary>Hàng đầu của NavMain: nút "Quick Create" nền primary + nút hộp thư viền (size-8).</summary>
    Div QuickCreateRow(Window w) =>
        w.Div().Class("flex items-center gap-2")
            .Child(w.Div().Key(QuickCreateKey).Focusable().Class(QuickCreateClass)
                .OnClick(this, static (b, _) => b.LastAction = "Quick Create")
                .Child(DashboardIcons.Draw(w, DashboardIcons.CirclePlusFilledPath))
                .Child(w.Div().Class("truncate").Child("Quick Create")))
            .Child(Ui.Button(w, ButtonVariant.Outline, ButtonSize.IconSm).Key(InboxKey)
                .OnClick(this, static (b, _) => b.LastAction = "Inbox")
                .Child(DashboardIcons.Draw(w, DashboardIcons.MailPath)));

    // Avatar ảnh của bản gốc (/avatars/shadcn.jpg, grayscale) chưa có: hiện AvatarFallback "CN" (rounded-lg bg-muted).
    Div UserTrigger(Window w) =>
        w.Div().Class(_userMenu.IsOpen ? UserButton + "bg-sidebar-accent text-sidebar-accent-foreground" : UserButton)
            .Child(w.Div().Class("flex size-8 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-muted").Child(Dashboard01Data.UserInitials))
            .Child(w.Div().Class("grid min-w-0 flex-1 text-left text-sm leading-tight")
                .Child(w.Div().Class("truncate font-medium").Child(Dashboard01Data.UserName))
                .Child(w.Div().Class("truncate text-xs text-muted-foreground").Child(Dashboard01Data.UserEmail)))
            .Child(DashboardIcons.DotsVertical(w));

    Div Sidebar(Window w) =>
        SidebarNav.Frame(w, variant: SidebarVariant.Inset).Class("w-72").Key(SidebarKey)
            .Child(SidebarNav.Header(w)
                // Bản gốc là thẻ <a href="#">: một nút focus được, bấm không làm gì.
                .Child(w.Div().Key(BrandKey).Focusable().Class(BrandClass)
                    .Child(w.Path(DashboardIcons.InnerShadowTopPath).Class("size-5 shrink-0"))
                    .Child(w.Div().Class("truncate text-base font-semibold").Child("Acme Inc."))))
            .Child(SidebarNav.Content(w, _nav))
            .Child(SidebarNav.Footer(w).Child(w.View(_userMenu).Key("nav-user")));

    Div Header(Window w) =>
        w.Div().Class("flex h-12 shrink-0 items-center gap-2 border-b border-border")
            .Child(w.Div().Class("flex w-full items-center gap-1 px-4 lg:gap-2 lg:px-6")
                .Child(SidebarBlockParts.Trigger(w).Class("-ml-1").Key(TriggerKey)
                    .OnClick(this, static (b, _) => b.SidebarOpen = !b.SidebarOpen))
                .Child(w.Div().Class("mx-2 h-4 w-px shrink-0 bg-border"))
                .Child(w.Div().Class("text-base font-medium").Child("Documents"))
                .Child(w.Div().Class("flex-1"))
                .Child(Ui.Button(w, ButtonVariant.Ghost, ButtonSize.Sm).Class("hidden sm:flex dark:text-foreground").Key(GitHubKey)
                    .OnClick(this, static (b, _) => b.LastAction = "GitHub").Child("GitHub")));

    public override Div Render(Window w)
    {
        var content = w.Div().Class("flex flex-col gap-4 py-4 md:gap-6 md:py-6")
            .Child(DashboardCards.Render(w))
            .Child(w.Div().Class("px-4 lg:px-6").Child(w.View(_chart)))
            .Child(w.View(_table));
        // SidebarInset (inset): m-2 ml-0 rounded-xl shadow-sm; offcanvas đóng → ml-2. Nằm trong vùng cuộn của trang.
        var inset = w.Div().Class(_open ? "m-2 ml-0 flex shrink-0 flex-col rounded-xl bg-background shadow-sm" : "m-2 flex shrink-0 flex-col rounded-xl bg-background shadow-sm")
            .Child(Header(w)).Child(content);
        var root = SidebarNav.Wrapper(w, SidebarVariant.Inset);
        if (_open) root.Child(Sidebar(w));
        return root.Child(w.Div().Key(ContentKey).Class("flex min-w-0 flex-1 flex-col overflow-y-auto").Child(inset));
    }
}
