using GotX;
using GotX.Components;

/// <summary>Kiểm tra Overlays phần bàn phím của DropdownMenu / ContextMenu: dòng đang chọn, menu con, focus trả về nút mở.</summary>
static partial class OverlaysChecks
{
    // 0 nhãn · 1 Hồ sơ · 2 kẻ · 3 checkbox · 4 API (tắt) · 5 menu con Mời · 6 Đăng xuất
    static MenuItem[] KeyMenu() =>
    [
        MenuItem.Label("Tài khoản"),
        MenuItem.Action("Hồ sơ", "Ctrl+P", key: "mk-profile"),
        MenuItem.Separator(),
        MenuItem.Check("Thanh trạng thái", false, key: "mk-status"),
        MenuItem.Action("API", disabled: true, key: "mk-api"),
        MenuItem.Sub("Mời", [MenuItem.Action("Email", key: "mk-email"), MenuItem.Action("Tin nhắn", key: "mk-sms")], key: "mk-invite"),
        MenuItem.Action("Đăng xuất", destructive: true, key: "mk-logout"),
    ];

    static void MenuKeys(CheckHarness h)
    {
        DropdownMenu menu = null!;
        ContextMenu ctx = null!;
        var picked = new List<string>();
        var w = Host(h, app =>
        {
            menu = app.New(new DropdownMenu { TriggerText = "Tài khoản", Items = KeyMenu() });
            ctx = app.New(new ContextMenu { Items = SampleMenu() });
            app.Subscribe<MenuItemSelected>(menu, e => picked.Add(e.Item.Text));
            app.Subscribe<MenuItemSelected>(ctx, e => picked.Add("ctx:" + e.Item.Text));
        }, w => w.Div().Class("flex items-start gap-8").Child(w.View(menu)).Child(w.Div().Key("mk-area").Child(w.View(ctx))));
        var panel = menu.Panel;
        var trail = new List<int>();
        void Go(Key key) { h.Press(w, key); trail.Add(panel.ActiveIndex); }

        h.Press(w, Key.Tab);
        h.Check("dropdown: nút mở nhận focus bằng Tab", w.IsFocused(menu.TriggerKey) && w.FocusVisible);
        h.Press(w, Key.Down);
        h.Check("dropdown: ↓ trên nút mở → mở, tấm giữ focus, dòng đầu được tô", menu.IsOpen && w.IsFocused(panel.ContentKey) && panel.ActiveIndex == 1);
        Go(Key.Down); Go(Key.Down); Go(Key.Down); Go(Key.Down); Go(Key.Up); Go(Key.Home); Go(Key.End);
        h.Check($"dropdown: ↑/↓ bỏ qua nhãn, đường kẻ, dòng tắt và quay vòng; Home/End ({string.Join(",", trail)})", trail is [3, 5, 6, 1, 6, 1, 6]);
        h.Press(w, Key.Tab);
        h.Check("dropdown: Tab không rời khỏi menu đang mở", menu.IsOpen && w.IsFocused(panel.ContentKey));
        menu.LoopFocus = false;
        h.Press(w, Key.Down);
        h.Check("dropdown: LoopFocus = false thì ↓ ở dòng cuối đứng yên", panel.ActiveIndex == 6);
        menu.LoopFocus = true;
        h.Press(w, Key.Home); h.Press(w, Key.Down);
        h.Press(w, Key.Space);
        h.Check("dropdown: Space trên dòng checkbox đảo trạng thái, menu vẫn mở", menu.IsOpen && menu.Items[3].Checked && panel.ActiveIndex == 3);

        // Menu con bằng phím
        h.Press(w, Key.Down);
        h.Press(w, Key.Right);
        var sub = panel.OpenSubmenu;
        h.Check("dropdown: → trên dòng menu con mở panel con, focus sang nó, dòng đầu được tô",
            sub is not null && w.Overlays.Count == 2 && w.IsFocused(sub.ContentKey) && sub.ActiveIndex == 0 && panel.ActiveIndex == 5);
        h.Save(w, "overlay-dropdown-submenu.png");
        var sb = w.Overlays[^1].ContentBounds;
        var row = h.Box(w, "mk-invite");
        h.Check($"dropdown: panel con nằm sát bên phải dòng mở ra nó ({sb.X:F0} so với {row.Right:F0})", MathF.Abs(sb.X - row.Right) <= 4 && MathF.Abs(sb.Y - row.Y) <= 4);
        h.Press(w, Key.Down);
        h.Press(w, Key.Left);
        h.Check("dropdown: ← đóng panel con, focus về tấm cha, dòng mở vẫn được tô",
            panel.OpenSubmenu is null && w.Overlays.Count == 1 && w.IsFocused(panel.ContentKey) && panel.ActiveIndex == 5);
        h.Press(w, Key.Enter);
        h.Press(w, Key.Down);
        h.Press(w, Key.Enter);
        h.Check($"dropdown: Enter mở panel con, Enter trên dòng con → chọn, đóng cả cây, focus về nút mở ({string.Join(",", picked)})",
            picked is ["Tin nhắn"] && !menu.IsOpen && w.Overlays.Count == 0 && w.IsFocused(menu.TriggerKey) && w.FocusVisible);
        h.Press(w, Key.Up);
        h.Check("dropdown: ↑ trên nút mở → mở và tô dòng cuối", menu.IsOpen && panel.ActiveIndex == 6);
        h.Press(w, Key.Up); h.Press(w, Key.Right);
        h.Press(w, Key.Escape);
        h.Check("dropdown: Escape trong panel con đóng cả cây menu, focus về nút mở", !menu.IsOpen && w.Overlays.Count == 0 && w.IsFocused(menu.TriggerKey));
        h.Press(w, Key.Enter);
        h.Press(w, Key.Enter);
        h.Check("dropdown: Enter mở rồi Enter chọn dòng đầu", picked is ["Tin nhắn", "Hồ sơ"] && !menu.IsOpen && w.IsFocused(menu.TriggerKey));

        // Chuột và phím dùng chung một dòng sáng
        h.Click(w, menu.TriggerKey);
        h.Check("dropdown: mở bằng chuột thì chưa dòng nào được tô", menu.IsOpen && panel.ActiveIndex == -1 && w.IsFocused(panel.ContentKey));
        h.Move(w, CheckHarness.Center(h.Box(w, "mk-logout")));
        bool hovered = panel.ActiveIndex == 6;
        h.Press(w, Key.Home);
        var (lit, dim) = (RowTint(h, w, "mk-profile"), RowTint(h, w, "mk-logout"));
        h.Check($"dropdown: rê chuột tô dòng dưới con trỏ; phím dời vệt sáng đi, dòng dưới chuột tắt (sáng {lit}, tắt {dim})",
            hovered && panel.ActiveIndex == 1 && lit > 8 && dim <= 2);
        h.Move(w, CheckHarness.Center(h.Box(w, "mk-invite")));
        bool notYet = panel.OpenSubmenu is null;
        h.Wait(w, 0.15);
        h.Check("dropdown: rê lên dòng menu con → mở sau độ trễ ngắn, focus vẫn ở tấm cha",
            notYet && panel.OpenSubmenu is not null && w.IsFocused(panel.ContentKey));
        h.Move(w, CheckHarness.Center(h.Box(w, "mk-sms")));
        h.Check("dropdown: con trỏ vào panel con → nó nhận focus, dòng mở ở tấm cha vẫn sáng",
            panel.OpenSubmenu is { ActiveIndex: 1 } s2 && w.IsFocused(s2.ContentKey) && panel.ActiveIndex == 5);
        h.Press(w, Key.Escape);
        h.Check("dropdown: Escape khi con trỏ ở trong panel con cũng đóng cả cây", !menu.IsOpen && w.Overlays.Count == 0);

        // ContextMenu
        var area = h.Box(w, "mk-area");
        h.Click(w, new Point(area.X + 30, area.Y + 30), MouseButton.Right);
        h.Check("context-menu: mở thì tấm nhận focus, chưa dòng nào được tô", ctx.IsOpen && w.IsFocused(ctx.Panel.ContentKey) && ctx.Panel.ActiveIndex == -1);
        h.Press(w, Key.Up);
        bool last = ctx.Panel.ActiveIndex == 5;
        h.Press(w, Key.Enter);
        h.Check("context-menu: ↑ tô dòng cuối, Enter chọn và đóng", last && picked[^1] == "ctx:Đăng xuất" && !ctx.IsOpen && w.Overlays.Count == 0);
    }

    /// <summary>Độ lệch màu nền của dòng menu so với nền tấm (0 = không được tô sáng). Đo pixel thật ở mép trái dòng.</summary>
    static int RowTint(CheckHarness h, Window w, string rowKey)
    {
        var b = h.Box(w, rowKey);
        var row = h.Pixel(w, new Point(b.X + 3, b.Y + b.Height / 2));
        var bg = h.Pixel(w, new Point(b.X - 2, b.Y + b.Height / 2)); // đệm p-1 của tấm, ngay bên trái dòng
        return Math.Abs(row.R - bg.R) + Math.Abs(row.G - bg.G) + Math.Abs(row.B - bg.B);
    }
}
