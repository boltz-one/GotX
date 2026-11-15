using GotX;
using GotX.Components;

/// <summary>Kiểm tra hành vi của <see cref="SidebarNav"/>: mở/đóng mục con, chọn, nút phụ + menu, thu gọn, bàn phím, pixel.</summary>
static class DataSidebarChecks
{
    static readonly MenuItem[] RowMenu = [MenuItem.Action("View"), MenuItem.Action("Share"), MenuItem.Separator(), MenuItem.Action("Delete")];

    static readonly SidebarNavGroup[] Groups =
    [
        new("Platform",
        [
            new("Playground", Icon.Terminal) { DefaultOpen = true, SubItems = ["History", "Starred"] },
            new("Models", Icon.Globe) { SubItems = ["Genesis", "Explorer"] },
            new("Inbox", Icon.Mail) { Badge = "12" },
        ]),
        new("Projects", [new("Design", Icon.Folder) { Menu = RowMenu, ActionOnHover = true }, new("Report", Icon.File) { Action = Icon.Plus }])
            { HideWhenCollapsed = true },
        new(null, [new("Help")]) { PushToBottom = true },
    ];

    public static void Run(CheckHarness h)
    {
        SidebarNav nav = null!;
        bool collapsed = false;
        var log = new List<string>();
        var w = h.Open(app =>
        {
            nav = app.New(new SidebarNav { Groups = Groups });
            app.Subscribe<SidebarNav.OpenChanged>(nav, e => log.Add($"open {e.Group}/{e.Item} {e.Open}"));
            app.Subscribe<SidebarNav.ItemSelected>(nav, e => log.Add($"select {e.Group}/{e.Item}/{e.Sub} {e.Title}"));
            app.Subscribe<SidebarNav.ActionInvoked>(nav, e => log.Add($"action {e.Group}/{e.Item} {e.Target.Title} {e.Anchor == nav.ActionKey(e.Group, e.Item)}"));
            app.Subscribe<SidebarNav.MenuSelected>(nav, e => log.Add($"menu {e.Group}/{e.Item} {e.Choice.Text}"));
            return app.New(new FragmentView(w => SidebarNav.Wrapper(w).Class("h-[600px] w-[700px] bg-background text-foreground")
                .Child(SidebarNav.Frame(w, collapsed).Key("frame").Child(SidebarNav.Content(w, nav)))
                .Child(SidebarNav.Inset(w))));
        });
        string Last() => log.Count > 0 ? log[^1] : "(chưa có)";
        void SetCollapsed(bool value) { collapsed = value; nav.Collapsed = value; w.Root!.Notify(); h.Frames(w); }

        // --- mở / đóng mục con
        string models = nav.ItemKey(0, 1), inbox = nav.ItemKey(0, 2);
        h.Check("sidenav: DefaultOpen hiện mục con, nút khác thì chưa", h.Exists(w, nav.SubItemKey(0, 0, 0)) && !h.Exists(w, nav.SubItemKey(0, 1, 0)));
        float inboxY = h.Box(w, inbox).Y;
        h.Click(w, models);
        h.Check($"sidenav: bấm nút gập được thì mở, phát OpenChanged, không chọn ({Last()})",
            nav.IsOpen(0, 1) && Last() == "open 0/1 True" && nav.Active.Item == -1 && h.Exists(w, nav.SubItemKey(0, 1, 1)));
        h.Check($"sidenav: mục con đẩy nút sau xuống ({inboxY} → {h.Box(w, inbox).Y}), focus vẫn ở nút vừa bấm",
            h.Box(w, inbox).Y > inboxY + 50 && w.IsFocused(models));
        h.Press(w, Key.Left);
        bool closedByLeft = !nav.IsOpen(0, 1) && !h.Exists(w, nav.SubItemKey(0, 1, 0));
        h.Press(w, Key.Right);
        bool openedByRight = nav.IsOpen(0, 1);
        h.Press(w, Key.Enter);
        bool closedByEnter = !nav.IsOpen(0, 1);
        h.Press(w, Key.Space);
        h.Check($"sidenav: ← đóng ({closedByLeft}), → mở ({openedByRight}), Enter đóng ({closedByEnter}), Space mở lại; focus không rời nút",
            closedByLeft && openedByRight && closedByEnter && nav.IsOpen(0, 1) && w.IsFocused(models));
        nav.Toggle(0, 0);
        h.Frames(w);
        h.Check("sidenav: đóng nhóm phía trên bằng code, focus vẫn bám nút Models (key ổn định)", !nav.IsOpen(0, 0) && w.IsFocused(models));

        // --- chọn nút / mục con
        h.Press(w, Key.Tab);
        h.Check("sidenav: Tab từ nút cha sang mục con đầu tiên", w.IsFocused(nav.SubItemKey(0, 1, 0)));
        h.Press(w, Key.Enter);
        h.Check($"sidenav: Enter trên mục con chọn nó ({Last()})", nav.Active == (0, 1, 0) && Last() == "select 0/1/0 Genesis");
        h.Press(w, Key.Left);
        h.Check("sidenav: ← trên mục con đưa focus về nút cha", w.IsFocused(models));
        h.Click(w, inbox);
        h.Check($"sidenav: bấm nút thường thì chọn ({Last()})", nav.Active == (0, 2, -1) && Last() == "select 0/2/-1 Inbox");
        var ib = h.Box(w, inbox);
        var on = h.Pixel(w, new Point(ib.X + 4, ib.Y + 16));
        var off = h.Pixel(w, new Point(ib.X + 4, ib.Y - 2));
        h.Check($"sidenav: nút đang chọn có nền sidebar-accent {on}, ngoài nút là nền sidebar {off}", on == (245, 245, 245) && off == (250, 250, 250));
        var help = h.Box(w, nav.ItemKey(2, 0));
        h.Check($"sidenav: nhóm PushToBottom nằm sát đáy khung (đáy nút {help.Bottom}, khung {h.Box(w, "frame").Bottom})",
            MathF.Abs(h.Box(w, "frame").Bottom - 8 - help.Bottom) <= 1);

        // --- nút phụ và menu của nó
        h.Click(w, nav.ActionKey(1, 1));
        h.Check($"sidenav: nút phụ không có Menu phát ActionInvoked kèm anchor ({Last()})", Last() == "action 1/1 Report True" && !nav.IsMenuOpen);
        string action = nav.ActionKey(1, 0);
        h.Click(w, action);
        var ab = h.Box(w, action);
        var mb = nav.ActionMenu.Overlay?.ContentBounds ?? default;
        h.Check($"sidenav: nút phụ có Menu mở menu bên phải nút (nút tới x={ab.Right}, menu từ x={mb.X}, y={mb.Y} so với {ab.Y})",
            nav.IsMenuOpen && mb.X >= ab.Right && MathF.Abs(mb.Y - ab.Y) <= 1);
        h.Press(w, Key.Down);
        h.Press(w, Key.Down);
        h.Press(w, Key.Enter);
        h.Check($"sidenav: ↓ ↓ Enter chọn dòng hai, menu đóng, focus về nút phụ ({Last()})",
            Last() == "menu 1/0 Share" && !nav.IsMenuOpen && w.IsFocused(action));
        h.Press(w, Key.Enter);
        bool reopened = nav.IsMenuOpen;
        h.Press(w, Key.Escape);
        h.Check($"sidenav: Enter mở lại menu ({reopened}), Escape đóng và trả focus", reopened && !nav.IsMenuOpen && w.IsFocused(action));
        h.Click(w, action);
        h.Click(w, action);
        h.Check("sidenav: bấm nút phụ lần hai đóng menu chứ không mở lại", !nav.IsMenuOpen);
        h.Save(w, "sidenav-expanded.png");

        // --- thu gọn còn icon
        SetCollapsed(true);
        var item = h.Box(w, models);
        h.Check($"sidenav: thu gọn → khung {h.Box(w, "frame").Width}px, nút {item.Width}×{item.Height}",
            h.Box(w, "frame").Width == 48 && item.Width == 32 && item.Height == 32);
        h.Check("sidenav: thu gọn ẩn mục con, nút phụ và nhóm HideWhenCollapsed; trạng thái mở vẫn được giữ",
            !h.Exists(w, nav.SubItemKey(0, 1, 0)) && !h.Exists(w, action) && !h.Exists(w, nav.ItemKey(1, 0)) && nav.IsOpen(0, 1));
        h.Click(w, models);
        h.Check($"sidenav: thu gọn thì bấm nút gập được là chọn nó ({Last()})", nav.Active == (0, 1, -1) && nav.IsOpen(0, 1));
        h.Save(w, "sidenav-collapsed.png");
        SetCollapsed(false);
        h.Check("sidenav: mở rộng lại → khung 256px, mục con và nút phụ trở lại",
            h.Box(w, "frame").Width == 256 && h.Exists(w, nav.SubItemKey(0, 1, 0)) && h.Exists(w, action));
        InsetVariant(h);
    }

    /// <summary>Kiểu inset (dashboard): khung không viền lùi vào p-2, vùng nội dung là thẻ bo góc cách mép 8px.</summary>
    static void InsetVariant(CheckHarness h)
    {
        SidebarNav nav = null!;
        var w = h.Open(app =>
        {
            nav = app.New(new SidebarNav { Groups = Groups });
            return app.New(new FragmentView(w => SidebarNav.Wrapper(w, SidebarVariant.Inset).Class("h-[600px] w-[700px]")
                .Child(SidebarNav.Frame(w, variant: SidebarVariant.Inset).Key("frame")
                    .Child(SidebarNav.Header(w).Child(w.Div().Key("brand").Focusable().Class(SidebarNav.LargeButtonClass()).Child("Acme Inc.")))
                    .Child(SidebarNav.Content(w, nav))
                    .Child(SidebarNav.Separator(w))
                    .Child(SidebarNav.Footer(w).Child(SidebarNav.Trigger(w).Key("trigger"))))
                .Child(SidebarNav.Inset(w, SidebarVariant.Inset).Key("inset"))));
        });
        var frame = h.Box(w, "frame");
        var inset = h.Box(w, "inset");
        var brand = h.Box(w, "brand");
        h.Check($"sidenav inset: khung 256px, nút lg {brand.Width}×{brand.Height} lùi vào 16px", frame.Width == 256 && brand.X == 16 && brand.Width == 224 && brand.Height == 48);
        h.Check($"sidenav inset: thẻ nội dung cách mép trên/phải/dưới 8px và sát khung ({inset})",
            inset.X == 256 && inset.Y == 8 && inset.Right == 692 && inset.Bottom == 592);
        var card = h.Pixel(w, new Point(400, 300));
        var around = h.Pixel(w, new Point(696, 300));
        h.Check($"sidenav inset: thẻ nền background {card} trên nền sidebar {around}", card == (255, 255, 255) && around == (250, 250, 250));
        h.Click(w, "trigger");
        h.Check("sidenav inset: nút trigger nhận focus khi bấm", w.IsFocused("trigger"));
        h.Save(w, "sidenav-inset.png");
    }
}
