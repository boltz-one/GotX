using GotX;
using GotX.Components;

/// <summary>Kiểm tra hành vi Overlays (phần menu, điều hướng, lọc, toast).</summary>
static partial class OverlaysChecks
{
    static MenuItem[] SampleMenu() =>
    [
        MenuItem.Label("Tài khoản"),
        MenuItem.Action("Hồ sơ", "Ctrl+Shift+P", key: "mi-profile"),
        MenuItem.Separator(),
        MenuItem.Check("Thanh trạng thái", false, key: "mi-status"),
        MenuItem.Action("API", disabled: true, key: "mi-api"),
        MenuItem.Action("Đăng xuất", destructive: true, key: "mi-logout"),
    ];

    static void Menus(CheckHarness h)
    {
        DropdownMenu menu = null!;
        var picked = new List<string>();
        var toggled = new List<bool>();
        var w = Host(h, app =>
        {
            menu = app.New(new DropdownMenu { TriggerText = "Tài khoản", Items = SampleMenu() });
            app.Subscribe<MenuItemSelected>(menu, e => picked.Add(e.Item.Text));
            app.Subscribe<MenuItemToggled>(menu, e => toggled.Add(e.Checked));
        }, w => w.View(menu));

        var trigger = Anchor(w, menu.AnchorName);
        h.Click(w, CheckHarness.Center(trigger));
        var list = menu.Overlay?.ContentBounds ?? default;
        h.Check("dropdown: bấm nút thì mở danh sách ngay dưới, mép trái thẳng nút",
            menu.IsOpen && Near(list.Y, trigger.Bottom + 4) && Near(list.X, trigger.X));
        h.Save(w, "overlay-dropdown-menu.png");
        h.Click(w, "mi-status");
        h.Check("dropdown: bấm dòng checkbox thì đảo trạng thái, menu vẫn mở",
            menu.IsOpen && menu.Items[3].Checked && toggled is [true] && picked.Count == 0);
        h.Click(w, "mi-status");
        h.Check("dropdown: bấm lần nữa thì bỏ chọn", !menu.Items[3].Checked && toggled is [true, false]);
        h.Click(w, "mi-api");
        h.Check("dropdown: dòng bị tắt không phát sự kiện, không đóng", menu.IsOpen && picked.Count == 0);
        h.Click(w, "mi-profile");
        h.Check("dropdown: bấm dòng thì phát MenuItemSelected rồi đóng", !menu.IsOpen && picked is ["Hồ sơ"] && w.Overlays.Count == 0);
        h.Click(w, CheckHarness.Center(trigger));
        h.Press(w, Key.Escape);
        h.Check("dropdown: Escape đóng", !menu.IsOpen);
        h.Click(w, CheckHarness.Center(trigger));
        h.Click(w, CheckHarness.Center(trigger));
        h.Check("dropdown: bấm lại nút thì đóng", !menu.IsOpen);
        h.Click(w, CheckHarness.Center(trigger));
        h.Click(w, new Point(h.Size.Width - 20, h.Size.Height - 20));
        h.Check("dropdown: bấm ra ngoài thì đóng", !menu.IsOpen && picked.Count == 1);
    }

    static void ContextMenus(CheckHarness h)
    {
        ContextMenu ctx = null!;
        var picked = new List<string>();
        var w = Host(h, app =>
        {
            ctx = app.New(new ContextMenu { Items = SampleMenu() });
            app.Subscribe<MenuItemSelected>(ctx, e => picked.Add(e.Item.Text));
        }, w => w.Div().Key("ctx-area").Child(w.View(ctx)));

        var area = h.Box(w, "ctx-area");
        var at = new Point(area.X + 57, area.Y + 41);
        h.Click(w, at);
        h.Check("context-menu: bấm chuột trái không mở", !ctx.IsOpen);
        h.Click(w, at, MouseButton.Right);
        var b = ctx.Overlay?.ContentBounds ?? default;
        h.Check($"context-menu: chuột phải mở menu đúng tại điểm bấm ({b.X:F0},{b.Y:F0} so với {at.X:F0},{at.Y:F0})",
            ctx.IsOpen && Near(b.X, at.X) && Near(b.Y, at.Y));
        h.Save(w, "overlay-context-menu.png");
        var at2 = new Point(area.X + 150, area.Y + 20);
        h.Click(w, at2, MouseButton.Right);
        b = ctx.Overlay?.ContentBounds ?? default;
        h.Check("context-menu: chuột phải chỗ khác thì menu dời tới đó", ctx.IsOpen && w.Overlays.Count == 1 && Near(b.X, at2.X) && Near(b.Y, at2.Y));
        h.Click(w, "mi-logout");
        h.Check("context-menu: bấm dòng thì phát sự kiện rồi đóng", !ctx.IsOpen && picked is ["Đăng xuất"]);
    }

    static void Menubars(CheckHarness h)
    {
        Menubar bar = null!;
        var picked = new List<(int Menu, string Text)>();
        var w = Host(h, app =>
        {
            bar = app.New(new Menubar
            {
                Menus =
                [
                    new("Tệp", [MenuItem.Action("Tab mới", "Ctrl+T", key: "mb-newtab"), MenuItem.Action("In…")]),
                    new("Sửa", [MenuItem.Action("Hoàn tác", "Ctrl+Z", key: "mb-undo"), MenuItem.Action("Làm lại")]),
                ],
            });
            app.Subscribe<MenuItemSelected>(bar, e => picked.Add((e.Menu, e.Item.Text)));
        }, w => w.View(bar));

        var file = Anchor(w, bar.AnchorName(0));
        var edit = Anchor(w, bar.AnchorName(1));
        h.Click(w, CheckHarness.Center(file));
        h.Check("menubar: bấm mục thì mở menu của nó dưới mục", bar.OpenIndex == 0 && Near(bar.Overlay!.ContentBounds.X, file.X));
        h.Move(w, CheckHarness.Center(edit));
        h.Check("menubar: đang mở thì rê sang mục khác là chuyển menu",
            bar.OpenIndex == 1 && w.Overlays.Count == 1 && Near(bar.Overlay!.ContentBounds.X, edit.X));
        h.Save(w, "overlay-menubar.png");
        h.Click(w, "mb-undo");
        h.Check("menubar: bấm dòng thì phát sự kiện kèm chỉ số menu rồi đóng", bar.OpenIndex == -1 && picked is [(1, "Hoàn tác")]);
        h.Move(w, CheckHarness.Center(file));
        h.Check("menubar: không có menu mở thì rê qua không mở", bar.OpenIndex == -1);
    }

    static void Navigation(CheckHarness h)
    {
        NavigationMenu nav = null!;
        var picked = new List<(int, int, string)>();
        var w = Host(h, app =>
        {
            nav = app.New(new NavigationMenu
            {
                Items =
                [
                    new("Bắt đầu", [new("Giới thiệu", "Tổng quan.", "nav-intro"), new("Cài đặt", "Thêm vào dự án.", "nav-install")]),
                    new("Component", [new("Tooltip", "Chú thích.", "nav-tooltip")]),
                    new("Tài liệu"),
                ],
            });
            app.Subscribe<NavigationMenu.LinkSelected>(nav, e => picked.Add((e.Item, e.Link, e.Title)));
        }, w => w.View(nav));

        h.Click(w, CheckHarness.Center(Anchor(w, nav.AnchorName(0))));
        h.Check("navigation-menu: bấm mục thì mở tấm liên kết", nav.OpenIndex == 0 && h.Exists(w, "nav-install"));
        h.Save(w, "overlay-navigation-menu.png");
        h.Move(w, CheckHarness.Center(Anchor(w, nav.AnchorName(1))));
        h.Check("navigation-menu: rê sang mục khác thì chuyển tấm", nav.OpenIndex == 1 && h.Exists(w, "nav-tooltip") && !h.Exists(w, "nav-install"));
        h.Click(w, "nav-tooltip");
        h.Check("navigation-menu: bấm liên kết thì phát LinkSelected rồi đóng", nav.OpenIndex == -1 && picked is [(1, 0, "Tooltip")]);
        h.Click(w, CheckHarness.Center(Anchor(w, nav.AnchorName(2))));
        h.Check("navigation-menu: mục liên kết thẳng phát LinkSelected với Link = -1", picked.Count == 2 && picked[1] == (2, -1, "Tài liệu") && w.Overlays.Count == 0);
    }
}
