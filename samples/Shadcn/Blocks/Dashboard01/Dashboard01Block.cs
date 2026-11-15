using GotX;
using GotX.Components;

/// <summary>
/// Block dashboard-01 của shadcn/ui – "A dashboard with sidebar, charts and data table": sidebar kiểu inset (ẩn hẳn khi
/// bấm trigger – <c>collapsible="offcanvas"</c>), header, bốn thẻ số liệu, biểu đồ vùng có chọn khoảng thời gian, bảng
/// dữ liệu có tab. Biểu đồ và bảng là view riêng nên thao tác trên chúng không vẽ lại cả trang.
/// Bỏ Ctrl+B (Core chưa có phím tắt toàn cục): ẩn/hiện sidebar bằng nút trigger (chuột hoặc Tab + Enter/Space).
/// </summary>
public sealed class Dashboard01Block : View
{
    public static readonly SitePage Page = new("dashboard-01", "Dashboard 01", static app => app.New(new Dashboard01Block()), FullBleed: true);

    public const string TriggerKey = "dashboard-trigger", SidebarKey = "dashboard-sidebar", ContentKey = "dashboard-content";
    public const string QuickCreateKey = "dashboard-quick-create", InboxKey = "dashboard-inbox", BrandKey = "dashboard-brand", GitHubKey = "dashboard-github";

    const string SidebarRing = " focus-visible:ring-2 focus-visible:ring-sidebar-ring";
    const string BrandClass = "flex h-8 w-full items-center gap-2 rounded-md p-1.5 text-left cursor-pointer transition-colors " +
        "hover:bg-sidebar-accent hover:text-sidebar-accent-foreground" + SidebarRing;
    const string QuickCreateClass = "flex h-8 min-w-0 flex-1 items-center gap-2 rounded-md bg-primary px-2 text-sm text-primary-foreground cursor-pointer " +
        "transition-colors hover:bg-primary/90 active:bg-primary/90" + SidebarRing;

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
        _nav = App.New(new SidebarNav { Groups = Dashboard01Data.Groups });
        _nav.SetActive(0, 0);
        _userMenu = App.New(new DropdownMenu { Items = Dashboard01Data.UserMenu, Trigger = UserTrigger, Placement = Placement.RightEnd });
        _chart = App.New(new DashboardChart());
        _table = App.New(new DashboardTable());
        App.Subscribe<SidebarNav.ItemSelected>(_nav, e => LastAction = e.Title);
        App.Subscribe<SidebarNav.MenuSelected>(_nav, e => LastAction = e.Choice.Text + ": " + e.Target.Title);
        App.Subscribe<MenuItemSelected>(_userMenu, e => LastAction = e.Item.Text);
    }

    // Lớp bọc nút mở của DropdownMenu co theo nội dung, nên nút cần bề rộng cố định: w-56 = 15rem (khung inset) trừ p-2 hai bên.
    Div UserTrigger(Window w) =>
        w.Div().Class(SidebarNav.LargeButtonClass(false, _userMenu.IsOpen)).Class("w-56")
            .Child(w.Div().Class("flex size-8 shrink-0 items-center justify-center rounded-lg bg-muted text-sm").Child(Dashboard01Data.UserInitials))
            .Child(w.Div().Class("flex min-w-0 flex-1 flex-col text-left text-sm leading-tight")
                .Child(w.Div().Class("truncate font-medium").Child(Dashboard01Data.UserName))
                .Child(w.Div().Class("truncate text-xs text-muted-foreground").Child(Dashboard01Data.UserEmail)))
            .Child(Icon.MoreVertical(w));

    Div Sidebar(Window w) =>
        SidebarNav.Frame(w, variant: SidebarVariant.Inset).Key(SidebarKey)
            .Child(SidebarNav.Header(w)
                // Bản gốc là thẻ <a href="#">: một nút focus được, bấm không làm gì.
                .Child(w.Div().Key(BrandKey).Focusable().Class(BrandClass)
                    .Child(Icon.Circle(w).Class("size-5"))
                    .Child(w.Div().Class("truncate text-base font-semibold").Child("Acme Inc."))))
            // Hàng "Quick Create" + hộp thư: trong bản gốc nằm đầu nhóm NavMain; đặt ngoài SidebarNav vì nó không phải nút menu.
            .Child(w.Div().Class("flex shrink-0 items-center gap-2 px-2 pt-2")
                .Child(w.Div().Key(QuickCreateKey).Focusable().Class(QuickCreateClass)
                    .OnClick(this, static (b, _) => b.LastAction = "Quick Create")
                    .Child(DashboardIcons.CirclePlus(w)).Child(w.Div().Class("truncate").Child("Quick Create")))
                .Child(Ui.Button(w, ButtonVariant.Outline, ButtonSize.Icon).Key(InboxKey)
                    .OnClick(this, static (b, _) => b.LastAction = "Inbox").Child(Icon.Mail(w))))
            .Child(SidebarNav.Content(w, _nav))
            .Child(SidebarNav.Footer(w).Child(w.View(_userMenu)));

    Div Header(Window w) =>
        w.Div().Class("flex h-12 shrink-0 items-center gap-2 border-b border-border")
            .Child(w.Div().Class("flex w-full items-center gap-1 px-4 lg:gap-2 lg:px-6")
                .Child(SidebarNav.Trigger(w).Class("-ml-1").Key(TriggerKey).OnClick(this, static (b, _) => b.SidebarOpen = !b.SidebarOpen))
                .Child(Ui.Separator(w, SeparatorOrientation.Vertical).Class("mx-2 h-4 self-center"))
                .Child(w.Div().Class("text-base font-medium").Child("Documents"))
                .Child(w.Div().Class("flex-1"))
                .Child(Ui.Button(w, "GitHub", ButtonVariant.Ghost, ButtonSize.Sm).Class("hidden sm:flex").Key(GitHubKey)
                    .OnClick(this, static (b, _) => b.LastAction = "GitHub")));

    public override Div Render(Window w)
    {
        // Các khối trong vùng cuộn đều shrink-0: cột flex cao cố định sẽ ép chúng co lại thay vì cho cuộn.
        var content = w.Div().Key(ContentKey).Class("flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto py-4 md:gap-6 md:py-6")
            .Child(DashboardCards.Render(w))
            .Child(w.Div().Class("shrink-0 px-4 lg:px-6").Child(w.View(_chart)))
            .Child(w.View(_table).Class("shrink-0"));
        var root = SidebarNav.Wrapper(w, SidebarVariant.Inset);
        if (_open) root.Child(Sidebar(w));
        // Sidebar ẩn thì thẻ nội dung cần lề trái (m-2 thay cho m-2 ml-0) để không dính mép cửa sổ.
        return root.Child(SidebarNav.Inset(w, SidebarVariant.Inset).Class(_open ? "" : "ml-2").Child(Header(w)).Child(content));
    }
}
