using GotX;
using GotX.Components;

/// <summary>Kiểm tra Navigation phần bàn phím: focus đang ở đâu và state đổi thế nào sau mỗi phím.</summary>
static partial class NavigationChecks
{
    static void KeyboardChecks(CheckHarness h)
    {
        TabsKeys(h);
        ListKeys(h);
        LayoutKeys(h);
    }

    static void TabsKeys(CheckHarness h)
    {
        Tabs tabs = null!, vertical = null!;
        var w = Host(h, app =>
        {
            tabs = app.New(new Tabs { Items = ["Một", "Hai", "Ba"], Content = static (w, i) => w.Div().Child("Nội dung") });
            tabs.SelectedIndex = 1;
            vertical = app.New(new Tabs { Items = ["A", "B", "C"], Orientation = TabsOrientation.Vertical, Variant = TabsVariant.Line, Content = static (w, i) => w.Div().Child("Dọc") });
            return w => w.Div().Class("flex flex-col gap-4 w-96 p-4 bg-background text-foreground")
                .Child(w.View(tabs)).Child(w.Div().Class("flex").Child(w.Div().Key("tabs-after").Class("px-2").Focusable().Child("Sau"))).Child(w.View(vertical));
        });
        h.Press(w, Key.Tab);
        h.Check("tabs: Tab vào thanh thì focus tới tab ĐANG CHỌN (cả thanh là một điểm dừng)", w.IsFocused(tabs.TriggerKey(1)) && w.FocusVisible);
        h.Press(w, Key.Right);
        bool third = tabs.SelectedIndex == 2 && w.IsFocused(tabs.TriggerKey(2));
        h.Press(w, Key.Right);
        h.Check("tabs: → chuyển focus và chọn luôn tab; quay vòng ở cuối", third && tabs.SelectedIndex == 0 && w.IsFocused(tabs.TriggerKey(0)));
        h.Save(w, "nav-tabs-focus.png");
        h.Press(w, Key.Left);
        bool wrapped = tabs.SelectedIndex == 2;
        h.Press(w, Key.Home);
        bool home = tabs.SelectedIndex == 0;
        h.Press(w, Key.End);
        h.Check("tabs: ← quay vòng; Home/End về tab đầu/cuối", wrapped && home && tabs.SelectedIndex == 2 && w.IsFocused(tabs.TriggerKey(2)));
        h.Press(w, Key.Down);
        h.Check("tabs ngang: ↑/↓ không đổi tab", tabs.SelectedIndex == 2);
        h.Press(w, Key.Home);
        h.Press(w, Key.Tab);
        h.Check("tabs: Tab rời cả thanh trong một lần nhấn", w.IsFocused("tabs-after"));
        h.Press(w, Key.Tab, Modifiers.Shift);
        h.Check("tabs: Shift+Tab quay lại thì về tab đang chọn", w.IsFocused(tabs.TriggerKey(0)));

        var a = h.Box(w, vertical.TriggerKey(0));
        var b = h.Box(w, vertical.TriggerKey(1));
        h.Check("tabs dọc: các nút xếp thành cột bên trái nội dung", Near(a.X, b.X) && b.Y >= a.Bottom - 0.5f && h.Box(w, vertical.ContentKey).X >= a.Right);
        w.FocusKey(vertical.TriggerKey(0));
        h.Frames(w);
        h.Press(w, Key.Right);
        bool ignored = vertical.SelectedIndex == 0;
        h.Press(w, Key.Down); h.Press(w, Key.Down); h.Press(w, Key.Down);
        bool looped = vertical.SelectedIndex == 0;
        h.Press(w, Key.Up);
        h.Check("tabs dọc: ↑/↓ đổi tab (quay vòng), ←/→ không", ignored && looped && vertical.SelectedIndex == 2 && w.IsFocused(vertical.TriggerKey(2)));
    }

    static void ListKeys(CheckHarness h)
    {
        Accordion acc = null!;
        Collapsible col = null!;
        Pagination pg = null!;
        Sidebar sb = null!;
        var counter = new Opener();
        var w = Host(h, app =>
        {
            acc = app.New(new Accordion { Items = [new("Một", "Nội dung một"), new("Hai", "Nội dung hai"), new("Ba", "Nội dung ba")] });
            col = app.New(new Collapsible { Trigger = static w => w.Div().Child("Tiêu đề"), Content = static w => w.Div().Key("col-body").Child("Thân") });
            pg = app.New(new Pagination { PageCount = 10 });
            sb = app.New(new Sidebar { Groups = [new("Nhóm", [new("Trang chủ"), new("Hộp thư"), new("Lịch")])] });
            return w => w.Div().Class("flex gap-6 p-4 bg-background text-foreground")
                .Child(w.Div().Class("flex flex-col gap-4 w-[420px]")
                    .Child(w.View(acc)).Child(w.View(col)).Child(w.View(pg))
                    .Child(Ui.Breadcrumb(w).Child(Ui.BreadcrumbLink(w, "Trang chủ").Key("crumb").OnClick(counter, static (c, _) => c.Count++))))
                .Child(w.Div().Class("flex h-64").Child(w.View(sb)).Child(Sidebar.Inset(w).Child(sb.Trigger(w))));
        });

        h.Press(w, Key.Tab);
        h.Press(w, Key.Down);
        bool second = w.IsFocused(acc.TriggerKey(1)) && !acc.IsOpen(1);
        h.Press(w, Key.Enter);
        bool opened = acc.IsOpen(1);
        h.Press(w, Key.Space);
        h.Check("accordion: ↓ chỉ dời focus; Enter mở, Space đóng mục đang focus", second && opened && !acc.IsOpen(1) && w.IsFocused(acc.TriggerKey(1)));
        h.Press(w, Key.End);
        bool last = w.IsFocused(acc.TriggerKey(2));
        h.Press(w, Key.Down);
        bool wrapped = w.IsFocused(acc.TriggerKey(0));
        h.Press(w, Key.Up);
        h.Press(w, Key.Home);
        h.Check("accordion: Home/End và quay vòng ở hai đầu", last && wrapped && w.IsFocused(acc.TriggerKey(0)));

        w.FocusKey(col.TriggerKey);
        h.Frames(w);
        h.Press(w, Key.Enter);
        h.Check("collapsible: trigger focus được, Enter mở", col.Open && h.Exists(w, "col-body") && w.IsFocused(col.TriggerKey));

        h.Press(w, Key.Tab);
        h.Check("pagination: nút Trước bị vô hiệu ở trang 1 nên Tab bỏ qua, tới nút số 1", w.IsFocused(pg.PageKey(1)));
        h.Press(w, Key.Tab);
        h.Press(w, Key.Enter);
        h.Check("pagination: Enter trên nút số 2 → trang 2, focus vẫn ở nút đó", pg.Page == 2 && w.IsFocused(pg.PageKey(2)));
        w.FocusKey(pg.NextKey);
        h.Frames(w);
        h.Press(w, Key.Space);
        h.Check("pagination: Space trên nút Sau → trang 3", pg.Page == 3 && w.IsFocused(pg.NextKey));

        w.FocusKey("crumb");
        h.Frames(w);
        h.Press(w, Key.Enter);
        h.Check("breadcrumb: liên kết focus được, Enter kích hoạt OnClick", counter.Count == 1 && w.IsFocused("crumb") && w.FocusVisible);

        h.Press(w, Key.Tab);
        bool firstItem = w.IsFocused(sb.ItemKey(0));
        h.Press(w, Key.Tab);
        h.Press(w, Key.Enter);
        h.Check("sidebar: nút menu là điểm dừng Tab, Enter chọn", firstItem && sb.ActiveIndex == 1 && w.IsFocused(sb.ItemKey(1)));
        w.FocusKey(sb.Key + "-trigger");
        h.Frames(w);
        h.Press(w, Key.Enter);
        h.Check("sidebar: nút thu gọn focus được, Enter thu gọn", sb.Collapsed && w.IsFocused(sb.Key + "-trigger"));
    }

    static void LayoutKeys(CheckHarness h)
    {
        Carousel car = null!;
        Resizable rz = null!, rv = null!;
        var w = Host(h, app =>
        {
            car = app.New(new Carousel { Count = 3, Slide = static (w, i) => w.Div().Class("h-24 bg-muted"), ShowIndex = false });
            rz = app.New(new Resizable { First = static w => w.Div().Key("kz-a"), Second = static w => w.Div(), FirstMinSize = 0.2f, SecondMinSize = 0.25f, BoxClass = "h-24 w-[600px]" });
            rv = app.New(new Resizable { Direction = ResizableDirection.Vertical, BoxClass = "h-[200px] w-[300px]", First = static w => w.Div().Key("kv-a"), Second = static w => w.Div() });
            return w => w.Div().Class("flex flex-col gap-4 p-4 bg-background text-foreground")
                .Child(w.Div().Class("w-[400px] px-12").Child(w.View(car))).Child(w.View(rz)).Child(w.View(rv));
        });

        h.Press(w, Key.Tab);
        h.Check("carousel: nút lùi bị vô hiệu ở slide đầu nên Tab tới nút tiến", w.IsFocused(car.NextKey));
        h.Press(w, Key.Right);
        bool one = car.Index == 1 && w.IsFocused(car.NextKey);
        h.Press(w, Key.Right);
        h.Check("carousel: → tiến; tới slide cuối nút tiến bị vô hiệu thì focus chuyển sang nút lùi", one && car.Index == 2 && w.IsFocused(car.PreviousKey));
        h.Press(w, Key.Left); h.Press(w, Key.Left);
        h.Check("carousel: ← lùi về slide đầu, focus chuyển lại nút tiến", car.Index == 0 && w.IsFocused(car.NextKey));
        h.Press(w, Key.Enter);
        h.Check("carousel: Enter trên nút tiến cũng chuyển slide", car.Index == 1);

        w.FocusKey(rz.HandleKey);
        h.Frames(w);
        h.Press(w, Key.Right);
        float grown = h.Box(w, "kz-a").Width;
        h.Press(w, Key.Left); h.Press(w, Key.Left);
        h.Check($"resizable: thanh chia focus được; →/← đổi 10% mỗi lần ({grown:F0} → {h.Box(w, "kz-a").Width:F0})",
            Near(grown, 360) && Near(h.Box(w, "kz-a").Width, 240) && Near(rz.Size, 0.4f, 0.001f));
        h.Save(w, "nav-resizable-focus.png");
        h.Press(w, Key.Up);
        bool ignored = Near(rz.Size, 0.4f, 0.001f);
        h.Press(w, Key.Home);
        bool min = Near(rz.Size, 0.2f, 0.001f);
        h.Press(w, Key.End);
        h.Check("resizable: ↑/↓ không dùng cho hướng ngang; Home/End về hai giới hạn min", ignored && min && Near(rz.Size, 0.75f, 0.001f));
        w.FocusKey(rv.HandleKey);
        h.Frames(w);
        h.Press(w, Key.Down);
        h.Check($"resizable dọc: ↓ tăng panel trên 10% ({h.Box(w, "kv-a").Height:F0})", Near(h.Box(w, "kv-a").Height, 120) && Near(rv.Size, 0.6f, 0.001f));
    }
}
