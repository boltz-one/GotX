using GotX;
using GotX.Components;

/// <summary>Dữ liệu mẫu của block sidebar-07 (chép từ app-sidebar.tsx, nav-*.tsx, team-switcher.tsx của shadcn/ui).</summary>
static class Sidebar07Data
{
    public readonly record struct Team(string Name, Func<Window, Div> Logo, string Plan);

    public const string UserName = "shadcn", UserEmail = "m@example.com", UserInitials = "CN";

    public static readonly Team[] Teams =
    [
        new("Acme Inc", SidebarIcons.GalleryVerticalEnd, "Enterprise"),
        new("Acme Corp.", SidebarIcons.AudioWaveform, "Startup"),
        new("Evil Corp.", SidebarIcons.Command, "Free"),
    ];

    /// <summary>Menu "⋯" của từng project; một danh sách dùng chung cho mọi hàng.</summary>
    public static readonly MenuItem[] ProjectMenu =
    [
        new() { Text = "View Project", Icon = static w => Icon.Folder(w).Class("text-muted-foreground"), Key = "sidebar-07-project-view" },
        new() { Text = "Share Project", Icon = static w => SidebarIcons.Forward(w).Class("text-muted-foreground"), Key = "sidebar-07-project-share" },
        MenuItem.Separator(),
        new() { Text = "Delete Project", Icon = static w => SidebarIcons.Trash2(w).Class("text-muted-foreground"), Key = "sidebar-07-project-delete" },
    ];

    public static readonly SidebarNavGroup[] Groups =
    [
        new("Platform",
        [
            new("Playground", SidebarIcons.SquareTerminal) { DefaultOpen = true, SubItems = ["History", "Starred", "Settings"] },
            new("Models", SidebarIcons.Bot) { SubItems = ["Genesis", "Explorer", "Quantum"] },
            new("Documentation", SidebarIcons.BookOpen) { SubItems = ["Introduction", "Get Started", "Tutorials", "Changelog"] },
            new("Settings", SidebarIcons.Settings2) { SubItems = ["General", "Team", "Billing", "Limits"] },
        ]),
        new("Projects",
        [
            new("Design Engineering", SidebarIcons.Frame) { Menu = ProjectMenu, ActionOnHover = true },
            new("Sales & Marketing", SidebarIcons.PieChart) { Menu = ProjectMenu, ActionOnHover = true },
            new("Travel", SidebarIcons.Map) { Menu = ProjectMenu, ActionOnHover = true },
            new("More", static w => Icon.MoreHorizontal(w).Class("text-sidebar-foreground/70")) { Muted = true },
        ]) { HideWhenCollapsed = true },
    ];

    /// <summary>Menu chọn team: nhãn, ba team (⌘1..⌘3 – bản gốc ghi cứng ký hiệu Mac), "Add team". Chỉ số dòng của team thứ i là i + 1.</summary>
    public static readonly MenuItem[] TeamMenu = BuildTeamMenu();

    static MenuItem[] BuildTeamMenu()
    {
        var items = new List<MenuItem> { MenuItem.Label("Teams") };
        for (int i = 0; i < Teams.Length; i++)
        {
            var logo = Teams[i].Logo;
            items.Add(new MenuItem
            {
                // Bản gốc ghi ⌘1..3; Segoe UI không có glyph ⌘ (U+2318) nên dùng cách viết của Windows.
                Text = Teams[i].Name, Shortcut = "⌘" + (i + 1), Key = "sidebar-07-team-" + i,
                Icon = w => Tile(w).Child(logo(w).Class("size-3.5")),
            });
        }
        items.Add(MenuItem.Separator());
        items.Add(new MenuItem { Text = "Add team", Key = "sidebar-07-team-add", Icon = static w => Tile(w).Child(Icon.Plus(w)) });
        return [.. items];
    }

    static Div Tile(Window w) => w.Div().Class("flex size-6 shrink-0 items-center justify-center rounded-md border border-border");

    /// <summary>Menu tài khoản. Hai nhãn đầu thay cho dòng avatar + tên + email (MenuItem chưa nhận nội dung tự vẽ).</summary>
    public static readonly MenuItem[] UserMenu =
    [
        MenuItem.Label(UserName), MenuItem.Label(UserEmail),
        MenuItem.Separator(),
        new() { Text = "Upgrade to Pro", Icon = SidebarIcons.Sparkles, Key = "sidebar-07-user-upgrade" },
        MenuItem.Separator(),
        new() { Text = "Account", Icon = SidebarIcons.BadgeCheck, Key = "sidebar-07-user-account" },
        new() { Text = "Billing", Icon = SidebarIcons.CreditCard, Key = "sidebar-07-user-billing" },
        new() { Text = "Notifications", Icon = Icon.Bell, Key = "sidebar-07-user-notifications" },
        MenuItem.Separator(),
        new() { Text = "Log out", Icon = Icon.LogOut, Key = "sidebar-07-user-logout" },
    ];
}
