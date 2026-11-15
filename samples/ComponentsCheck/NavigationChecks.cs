using GotX;
using GotX.Components;

/// <summary>Kiểm tra hành vi các component nhóm Navigation (đo state, sự kiện, hộp layout, pixel thật).</summary>
static partial class NavigationChecks
{
    public static void Run(CheckHarness h)
    {
        AccordionChecks(h);
        TabsChecks(h);
        PaginationChecks(h);
        ResizableChecks(h);
        CarouselChecks(h);
        SidebarChecks(h);
        ChartChecks(h);
        TableChecks(h);
        KeyboardChecks(h);
        LineChartChecks(h);
    }

    /// <summary>Mở cửa sổ có một view gốc dựng từ hàm, các component con tạo trước bằng <paramref name="setup"/>.</summary>
    static Window Host(CheckHarness h, Func<App, Func<Window, Div>> setup) =>
        h.Open(app => app.New(new FragmentView(setup(app))));

    static bool Near(float a, float b, float tol = 1.5f) => MathF.Abs(a - b) <= tol;

    static void AccordionChecks(CheckHarness h)
    {
        Accordion acc = null!;
        var log = new List<string>();
        var w = Host(h, app =>
        {
            acc = app.New(new Accordion { Items = [new("Một", "Nội dung một"), new("Hai", "Nội dung hai"), new("Ba", "Nội dung ba")] });
            app.Subscribe<Accordion.OpenChanged>(acc, e => log.Add($"{e.Index}{(e.Open ? "+" : "-")}"));
            return w => w.Div().Class("w-96 p-4 bg-background text-foreground").Child(w.View(acc));
        });
        float y1Closed = h.Box(w, acc.TriggerKey(1)).Y;
        h.Click(w, acc.TriggerKey(0));
        float y1Open = h.Box(w, acc.TriggerKey(1)).Y;
        h.Check($"accordion: mở mục đầu đẩy mục sau xuống ({y1Closed} → {y1Open})", acc.IsOpen(0) && y1Open > y1Closed + 10);
        h.Click(w, acc.TriggerKey(1));
        h.Check($"accordion single: mở mục hai thì đóng mục một ({string.Join(",", log)})",
            !acc.IsOpen(0) && acc.IsOpen(1) && string.Join(",", log) == "0+,0-,1+");
        h.Click(w, acc.TriggerKey(1));
        h.Check("accordion: bấm lại mục đang mở thì đóng", !acc.IsOpen(1));

        Accordion multi = null!;
        var w2 = Host(h, app =>
        {
            multi = app.New(new Accordion { Type = AccordionType.Multiple, Items = [new("A", "a"), new("B", "b")] });
            return w => w.Div().Class("w-96 p-4").Child(w.View(multi));
        });
        h.Click(w2, multi.TriggerKey(0));
        h.Click(w2, multi.TriggerKey(1));
        h.Check("accordion multiple: hai mục cùng mở", multi.IsOpen(0) && multi.IsOpen(1));
    }

    static void TabsChecks(CheckHarness h)
    {
        Tabs tabs = null!;
        var values = new List<string>();
        var w = Host(h, app =>
        {
            tabs = app.New(new Tabs { Items = ["Một", "Hai", "Ba"], Content = static (w, i) => w.Div().Key("tab-body-" + i).Child("Nội dung") });
            app.Subscribe<Tabs.ValueChanged>(tabs, e => values.Add(e.Value));
            return w => w.Div().Class("w-96 p-4").Child(w.View(tabs));
        });
        h.Check("tabs: ban đầu hiện nội dung tab đầu", h.Exists(w, "tab-body-0") && !h.Exists(w, "tab-body-2"));
        h.Click(w, tabs.TriggerKey(2));
        h.Check($"tabs: bấm tab ba thì đổi nội dung và phát ValueChanged ({string.Join(",", values)})",
            tabs.SelectedIndex == 2 && h.Exists(w, "tab-body-2") && !h.Exists(w, "tab-body-0") && values is ["Ba"]);
        var t0 = h.Box(w, tabs.TriggerKey(0));
        var t1 = h.Box(w, tabs.TriggerKey(1));
        h.Check("tabs: các nút xếp ngang cùng hàng", Near(t0.Y, t1.Y) && t1.X >= t0.Right - 0.5f);
    }

    static void PaginationChecks(CheckHarness h)
    {
        Pagination pg = null!;
        var pages = new List<int>();
        var w = Host(h, app =>
        {
            pg = app.New(new Pagination { PageCount = 10 });
            app.Subscribe<Pagination.PageChanged>(pg, e => pages.Add(e.Page));
            return w => w.Div().Class("w-[700px] p-4").Child(w.View(pg));
        });
        h.Click(w, pg.PreviousKey);
        h.Check("pagination: trang 1 thì nút Trước vô hiệu, không phát sự kiện", pg.Page == 1 && pages.Count == 0);
        h.Click(w, pg.PageKey(2));
        h.Click(w, pg.NextKey);
        h.Check($"pagination: bấm số 2 rồi Sau → phát 2, 3 ({string.Join(",", pages)})", pg.Page == 3 && pages is [2, 3]);
        h.Click(w, pg.PageKey(10));
        h.Click(w, pg.NextKey);
        h.Check("pagination: trang cuối thì nút Sau không đi tiếp", pg.Page == 10 && pages is [2, 3, 10]);
        h.Click(w, pg.PreviousKey);
        h.Check("pagination: Trước từ trang 10 về 9", pg.Page == 9 && pages[^1] == 9);

        var slots = new List<int>();
        Pagination.Slots(slots, 5, 10, 1);
        h.Check($"pagination: trang 5/10 → 1 … 4 5 6 … 10 ({string.Join(" ", slots)})", slots is [1, 0, 4, 5, 6, 0, 10]);
        Pagination.Slots(slots, 1, 10, 1);
        h.Check($"pagination: trang 1/10 → 1 2 3 4 5 … 10 ({string.Join(" ", slots)})", slots is [1, 2, 3, 4, 5, 0, 10]);
        h.Check("pagination: ở trang 9 màn hình có nút 6 nhưng không có nút 5 (đã thay bằng …)",
            h.Exists(w, pg.PageKey(6)) && !h.Exists(w, pg.PageKey(5)));
    }
}
