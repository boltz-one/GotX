using GotX;
using GotX.Components;

/// <summary>Kiểm tra Overlays phần bàn phím và độ trễ rê chuột của Menubar / NavigationMenu.</summary>
static partial class OverlaysChecks
{
    static void MenubarKeys(CheckHarness h)
    {
        Menubar bar = null!;
        var picked = new List<(int Menu, string Text)>();
        var w = Host(h, app =>
        {
            bar = app.New(new Menubar
            {
                Menus =
                [
                    new("Tệp", [MenuItem.Action("Tab mới"), MenuItem.Sub("Chia sẻ", [MenuItem.Action("Email"), MenuItem.Action("Liên kết")]), MenuItem.Action("In…")]),
                    new("Sửa", [MenuItem.Action("Hoàn tác"), MenuItem.Action("Làm lại")]),
                    new("Xem", [MenuItem.Check("Thanh dấu trang"), MenuItem.Action("Toàn màn hình")]),
                ],
            });
            app.Subscribe<MenuItemSelected>(bar, e => picked.Add((e.Menu, e.Item.Text)));
        }, w => w.Div().Class("flex items-center gap-4").Child(w.View(bar)).Child(Ui.Button(w, "Sau").Key("after-bar").Focusable()));

        h.Press(w, Key.Tab);
        h.Press(w, Key.Right);
        bool second = w.IsFocused(bar.TriggerKey(1));
        h.Press(w, Key.End);
        bool third = w.IsFocused(bar.TriggerKey(2));
        h.Press(w, Key.Right);
        h.Check("menubar: Tab vào mục đầu; →/End chuyển focus giữa các mục, quay vòng ở cuối, chưa mở menu nào",
            second && third && w.IsFocused(bar.TriggerKey(0)) && bar.OpenIndex == -1);
        h.Press(w, Key.Tab);
        h.Check("menubar: Tab rời cả thanh trong một lần nhấn (cả thanh là một điểm dừng)", w.IsFocused("after-bar"));
        h.Press(w, Key.Tab, Modifiers.Shift);
        h.Press(w, Key.Home);
        h.Press(w, Key.Down);
        h.Check("menubar: ↓ trên mục → mở menu của nó, tô dòng đầu, focus vào menu",
            bar.OpenIndex == 0 && bar.Panel is { ActiveIndex: 0 } p0 && w.IsFocused(p0.ContentKey));
        h.Press(w, Key.Right);
        h.Check("menubar: → trong menu sang menu bên cạnh (một overlay), tô dòng đầu",
            bar.OpenIndex == 1 && w.Overlays.Count == 1 && bar.Panel is { ActiveIndex: 0 } p1 && w.IsFocused(p1.ContentKey));
        h.Save(w, "overlay-menubar-keys.png");
        h.Press(w, Key.Right); h.Press(w, Key.Right);
        bool wrapped = bar.OpenIndex == 0;
        h.Press(w, Key.Left);
        h.Check("menubar: →/← quay vòng qua các menu", wrapped && bar.OpenIndex == 2);
        h.Press(w, Key.Escape);
        h.Check("menubar: Escape đóng, focus về đúng mục của menu vừa mở", bar.OpenIndex == -1 && w.IsFocused(bar.TriggerKey(2)) && w.FocusVisible);
        h.Press(w, Key.Home); h.Press(w, Key.Enter);
        h.Press(w, Key.Down);
        h.Press(w, Key.Right);
        h.Check("menubar: → trên dòng menu con mở panel con chứ không chuyển menu",
            bar.OpenIndex == 0 && w.Overlays.Count == 2 && bar.Panel!.OpenSubmenu is { ActiveIndex: 0 });
        h.Press(w, Key.Right);
        h.Check("menubar: → trong panel con (không còn cấp sâu hơn) sang menu kế, đóng cả cây cũ", bar.OpenIndex == 1 && w.Overlays.Count == 1);
        h.Press(w, Key.Down); h.Press(w, Key.Enter);
        h.Check("menubar: Enter chọn dòng, đóng, focus về mục trên thanh", picked is [(1, "Làm lại")] && bar.OpenIndex == -1 && w.IsFocused(bar.TriggerKey(1)));
    }

    static void NavigationKeys(CheckHarness h)
    {
        NavigationMenu nav = null!;
        var picked = new List<string>();
        var w = Host(h, app =>
        {
            nav = app.New(new NavigationMenu
            {
                Items =
                [
                    new("Bắt đầu", [new("Giới thiệu", "Tổng quan.", "nk-intro"), new("Cài đặt", "Thêm vào dự án.", "nk-install")]),
                    new("Component", [new("Tooltip", "Chú thích.")]),
                    new("Tài liệu"),
                ],
            });
            app.Subscribe<NavigationMenu.LinkSelected>(nav, e => picked.Add(e.Title));
        }, w => w.View(nav));
        var away = new Point(h.Size.Width - 30, h.Size.Height - 30);

        h.Press(w, Key.Tab);
        h.Press(w, Key.Right);
        bool moved = w.IsFocused(nav.TriggerKey(1));
        h.Press(w, Key.Home);
        h.Press(w, Key.Left);
        h.Check("navigation-menu: ←/→/Home đi giữa các mục, không quay vòng", moved && w.IsFocused(nav.TriggerKey(0)) && nav.OpenIndex == -1);
        h.Press(w, Key.Enter);
        h.Check("navigation-menu: Enter mở tấm, focus vẫn ở mục", nav.OpenIndex == 0 && w.IsFocused(nav.TriggerKey(0)));
        h.Press(w, Key.Down);
        bool first = w.IsFocused("nk-intro");
        h.Press(w, Key.Down);
        h.Check("navigation-menu: ↓ đi vào tấm rồi sang liên kết kế", first && w.IsFocused("nk-install"));
        h.Save(w, "overlay-navigation-menu-keys.png");
        h.Press(w, Key.Up); h.Press(w, Key.Up);
        h.Check("navigation-menu: ↑ ở liên kết đầu trở về mục, tấm vẫn mở", w.IsFocused(nav.TriggerKey(0)) && nav.OpenIndex == 0);
        h.Press(w, Key.Down); h.Press(w, Key.End);
        h.Press(w, Key.Escape);
        h.Check("navigation-menu: Escape khi focus trong tấm → đóng, focus về mục", nav.OpenIndex == -1 && w.IsFocused(nav.TriggerKey(0)));
        h.Press(w, Key.Down);
        h.Press(w, Key.Enter);
        h.Check($"navigation-menu: ↓ mở và vào tấm luôn; Enter trên liên kết → LinkSelected, đóng ({string.Join(",", picked)})",
            picked is ["Giới thiệu"] && nav.OpenIndex == -1 && w.Overlays.Count == 0);
        h.Press(w, Key.End); h.Press(w, Key.Enter);
        h.Check("navigation-menu: Enter trên mục liên kết thẳng phát LinkSelected", picked is ["Giới thiệu", "Tài liệu"]);
        w.Blur();

        // Rê chuột: mở sau OpenDelay, đóng sau CloseDelay
        var t0 = CheckHarness.Center(Anchor(w, nav.AnchorName(0)));
        h.Move(w, t0);
        bool notYet = nav.OpenIndex == -1;
        h.Wait(w, 0.05);
        h.Check("navigation-menu: rê chuột lên mục → mở sau độ trễ, không phải ngay", notYet && nav.OpenIndex == 0);
        h.Click(w, t0);
        h.Check("navigation-menu: cú bấm ngay sau khi tự mở do rê chuột không đóng tấm", nav.OpenIndex == 0);
        h.Move(w, CheckHarness.Center(h.Box(w, "nk-install")));
        h.Wait(w, 0.2);
        h.Check("navigation-menu: chuột đi xuống tấm thì tấm vẫn mở", nav.OpenIndex == 0);
        h.Move(w, away);
        bool still = nav.OpenIndex == 0;
        h.Wait(w, 0.1);
        h.Check("navigation-menu: rời cả mục lẫn tấm → đóng sau độ trễ", still && nav.OpenIndex == -1 && w.App.PendingTimers == 0);
        h.Move(w, t0);
        h.Move(w, away);
        h.Wait(w, 0.2);
        h.Check("navigation-menu: lướt qua nhanh hơn độ trễ thì không mở", nav.OpenIndex == -1);
    }
}
