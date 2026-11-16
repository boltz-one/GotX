using GotX;
using GotX.Components;

/// <summary>
/// Block sidebar-07 của shadcn/ui – "A sidebar that collapses to icons": TeamSwitcher ở đầu, NavMain gập được,
/// NavProjects có menu từng hàng, NavUser ở chân; vùng inset có trigger, breadcrumb và nội dung giữ chỗ.
/// Bỏ SidebarRail (dải kéo ở mép) và Ctrl+B: thu gọn bằng nút trigger (chuột hoặc Tab + Enter/Space).
/// </summary>
public sealed class Sidebar07Block : View
{
    public static readonly SitePage Page = new("sidebar-07", "Sidebar 07", static app => app.New(new Sidebar07Block()), FullBleed: true, Style: ComponentStyle.NewYorkV4);

    public const string TriggerKey = "sidebar-07-trigger", SidebarKey = "sidebar-07-sidebar", ContentKey = "sidebar-07-content";
    public const string TeamNameKey = "sidebar-07-team-name", UserNameKey = "sidebar-07-user-name";

    SidebarNav _nav = null!;
    DropdownMenu _teamMenu = null!, _userMenu = null!;
    bool _collapsed;
    int _team;

    public SidebarNav Nav => _nav;
    public DropdownMenu TeamMenu => _teamMenu;
    public DropdownMenu UserMenu => _userMenu;

    /// <summary>Tên team đang hiện ở đầu sidebar.</summary>
    public string ActiveTeam => Sidebar07Data.Teams[_team].Name;

    /// <summary>Lựa chọn gần nhất trong các menu (bản gốc không làm gì với chúng; giữ lại để quan sát được).</summary>
    public string LastAction { get; private set; } = "";

    /// <summary>Sidebar thu gọn còn cột icon (collapsible="icon").</summary>
    public bool Collapsed
    {
        get => _collapsed;
        set
        {
            if (_collapsed == value) return;
            _collapsed = value;
            _nav.Collapsed = value;
            // Hai nút mở menu được vẽ trong view của DropdownMenu: phải báo chúng vẽ lại theo dạng thu gọn.
            _teamMenu.Notify();
            _userMenu.Notify();
            Notify();
        }
    }

    protected override void OnCreated()
    {
        // Mục con là liên kết href="#": bấm không tô (Selectable = false), như bản gốc.
        _nav = App.New(new SidebarNav { Groups = Sidebar07Data.Groups, Selectable = false, KeyPrefix = "nav" });
        // side="right": menu luôn mở sang phải nút, nên vẫn dùng được khi sidebar chỉ còn cột icon.
        _teamMenu = App.New(new DropdownMenu { AriaLabel = "Switch team", Items = Sidebar07Data.TeamMenu, Trigger = TeamTrigger, Placement = Placement.RightStart });
        _userMenu = App.New(new DropdownMenu { AriaLabel = "User menu", Items = Sidebar07Data.UserMenu, Trigger = UserTrigger, Placement = Placement.RightEnd });
        App.Subscribe<MenuItemSelected>(_teamMenu, e =>
        {
            int team = Array.IndexOf(Sidebar07Data.TeamMenu, e.Item) - 1;
            if ((uint)team >= (uint)Sidebar07Data.Teams.Length) { LastAction = e.Item.Text; return; }
            _team = team;
            _teamMenu.Notify();
        });
        App.Subscribe<MenuItemSelected>(_userMenu, e => LastAction = e.Item.Text);
        App.Subscribe<SidebarNav.MenuSelected>(_nav, e => LastAction = e.Choice.Text + ": " + e.Target.Title);
    }

    // Lớp bọc nút mở của DropdownMenu co theo nội dung, nên nút cần bề rộng cố định: w-60 = 16rem trừ p-2 hai bên.
    Div LargeButton(Window w, bool open) =>
        w.Div().Class(SidebarNav.LargeButtonClass(_collapsed, open)).Class(_collapsed ? "" : "w-full");

    static Div TwoLines(Window w, string key, string title, string subtitle) =>
        w.Div().Class("flex min-w-0 flex-1 flex-col text-left text-sm leading-tight")
            .Child(w.Div().Key(key).Class("truncate font-medium").Child(title))
            .Child(w.Div().Class("truncate text-xs").Child(subtitle));

    Div TeamTrigger(Window w)
    {
        var team = Sidebar07Data.Teams[_team];
        var button = LargeButton(w, _teamMenu.IsOpen).Child(SidebarBlockParts.LogoTile(w, team.Logo));
        return _collapsed ? button : button.Child(TwoLines(w, TeamNameKey, team.Name, team.Plan)).Child(Icon.ChevronsUpDown(w));
    }

    Div UserTrigger(Window w)
    {
        // Avatar bo góc vuông (h-8 w-8 rounded-lg) chỉ có chữ viết tắt: bản gốc cũng rơi về "CN" khi không tải được ảnh.
        var button = LargeButton(w, _userMenu.IsOpen)
            .Child(w.Div().Class("flex size-8 shrink-0 items-center justify-center rounded-lg bg-muted text-sm")
                .Child(Sidebar07Data.UserInitials));
        return _collapsed
            ? button
            : button.Child(TwoLines(w, UserNameKey, Sidebar07Data.UserName, Sidebar07Data.UserEmail)).Child(Icon.ChevronsUpDown(w));
    }

    public override Div Render(Window w)
    {
        var sidebar = SidebarNav.Frame(w, _collapsed).Key(SidebarKey)
            .Child(SidebarNav.Header(w, _collapsed).Child(w.View(_teamMenu)))
            .Child(SidebarNav.Content(w, _nav))
            .Child(SidebarNav.Footer(w, _collapsed).Child(w.View(_userMenu)));
        // Header thấp lại khi sidebar thu gọn (group-has-data-[collapsible=icon]/sidebar-wrapper:h-12).
        var header = w.Div().Class(_collapsed ? "flex h-12 shrink-0 items-center gap-2" : "flex h-16 shrink-0 items-center gap-2")
            .Child(w.Div().Class("flex items-center gap-2 px-4")
                .Child(SidebarBlockParts.Trigger(w).Class("-ml-1").Key(TriggerKey).OnClick(this, static (b, _) => b.Collapsed = !b.Collapsed))
                .Child(SidebarBlockParts.HeaderSeparator(w))
                .Child(SidebarBlockParts.Breadcrumb(w)));
        return SidebarNav.Wrapper(w)
            .Child(sidebar)
            .Child(SidebarNav.Inset(w).Child(header).Child(SidebarBlockParts.Placeholder(w, ContentKey, padTop: false)));
    }
}
