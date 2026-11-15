using GotX;
using GotX.Components;
using GotX.Test;

/// <summary>
/// Test block dashboard-01 của samples/Shadcn: lint (sáng/tối × hai cỡ cửa sổ × bốn scale), hành vi của sidebar, biểu đồ,
/// tab và bảng dữ liệu bằng chuột lẫn bàn phím, hiệu năng. Hành vi đối chiếu bản gốc (Radix): mục sidebar không có
/// trạng thái active, ToggleGroup một-lựa-chọn bỏ chọn được, Tabs chuyển tab ngay khi mũi tên dời focus.
/// </summary>
static class ShadcnDashboardTests
{
    /// <summary>Bọc block như ô trang FullBleed của SiteShell (cột flex, ô <c>flex-1 min-h-0 overflow-hidden</c>, view cao hết ô).</summary>
    sealed class Slot(View block) : View
    {
        // Như SiteShell với SitePage.Style: block của shadcn chạy với style new-york-v4.
        protected override void OnCreated() => ComponentTheme.Use(App, ComponentStyle.NewYorkV4);

        public override Div Render(Window w) =>
            w.Div().Class("size-full flex flex-col bg-background text-foreground")
                .Child(w.Div().Key("slot").Class("flex-1 min-h-0 overflow-hidden").Child(w.View(block).Class("size-full")));
    }

    static readonly Size[] Sizes = [new(1440, 900), new(1024, 768)];
    static readonly Size Desktop = new(1440, 900);

    static void Lint(UiTest t, string name, Action<UiTest>? prepare = null)
    {
        var report = UiDoctor.Run(app => app.New(new Slot(app.New(new Dashboard01Block()))),
            new DoctorOptions { Sizes = Sizes, OutDir = t.Options.OutDir, Name = name, Prepare = prepare });
        int bad = report.Count(Severity.Warning) + report.Count(Severity.Error);
        t.Assert.True(bad == 0, $"{name}: {bad} vấn đề lint trên {report.Configs} cấu hình\n{report.ToText(30)}");
    }

    static Dashboard01Block Open(UiTest t, Size? size = null)
    {
        Dashboard01Block block = null!;
        t.Open(app => app.New(new Slot(block = app.New(new Dashboard01Block()))), size ?? Desktop);
        return block;
    }

    static void Focus(UiTest t, string key) { t.Window.FocusKey(key); t.Frames(); }

    public static void Register(TestSuite suite)
    {
        suite.Add("shadcn/dashboard-01/lint", t => Lint(t, "dashboard-01"));
        suite.Add("shadcn/dashboard-01/lint-sidebar-hidden", t => Lint(t, "dashboard-01-hidden", p => p.Click(Dashboard01Block.TriggerKey)));
        suite.Add("shadcn/dashboard-01/lint-other-tab", t => Lint(t, "dashboard-01-tab", p => p.ClickText("Focus Documents")));

        suite.Add("shadcn/dashboard-01/sidebar", t =>
        {
            var b = Open(t);
            t.Assert.Size(Dashboard01Block.SidebarKey, 288, 900);         // --sidebar-width: calc(var(--spacing) * 72)
            t.Assert.TextVisible("Acme Inc.");
            t.Assert.TextVisible("Quick Create");
            t.Click(Dashboard01Block.TriggerKey);                          // offcanvas: sidebar biến mất hẳn
            t.Assert.True(!b.SidebarOpen, "bấm trigger thì ẩn sidebar");
            t.Assert.NotExists(Dashboard01Block.SidebarKey);
            t.Assert.Near(t.Box(Dashboard01Block.ContentKey).Width, 1440, 0.5f, "vùng nội dung chiếm hết bề ngang");
            Focus(t, Dashboard01Block.TriggerKey);
            t.Press(Key.Enter);
            t.Assert.Size(Dashboard01Block.SidebarKey, 288, 900);
            t.Assert.Focused(Dashboard01Block.TriggerKey);

            // Bản gốc: các mục là <a href="#">, không mục nào active và bấm cũng không tô.
            t.Assert.Equal(b.Nav.Active, (-1, -1, -1), "không có mục đang chọn");
            t.Click(b.Nav.ItemKey(0, 2));
            t.Assert.Equal(b.Nav.Active, (-1, -1, -1), "bấm Analytics không tô nó");
            t.Assert.Equal(b.LastAction, "Analytics", "ItemSelected tới block");
            t.Assert.True(t.Box(b.Nav.ItemKey(2, 0)).Y > 600, $"nav phụ nằm ở đáy sidebar (y = {t.Box(b.Nav.ItemKey(2, 0)).Y})");

            Focus(t, b.Nav.ActionKey(1, 0));                               // menu "⋯" của Data Library, bằng bàn phím
            t.Press(Key.Enter);
            t.Assert.True(b.Nav.IsMenuOpen, "Enter trên nút ⋯ mở menu tài liệu");
            t.Click("dashboard-doc-open");
            t.Assert.Equal(b.LastAction, "Open: Data Library", "chọn dòng trong menu tài liệu");

            t.Click(b.UserMenu.TriggerKey);
            t.Assert.True(b.UserMenu.IsOpen, "bấm hàng tài khoản mở menu");
            t.Press(Key.Escape);
            t.Assert.True(!b.UserMenu.IsOpen, "Escape đóng menu tài khoản");
            t.Assert.Focused(b.UserMenu.TriggerKey);
            t.Click(Dashboard01Block.QuickCreateKey);
            t.Assert.Equal(b.LastAction, "Quick Create", "nút Quick Create bấm được");
            t.Assert.NoLint();
        });

        suite.Add("shadcn/dashboard-01/chart-range", t =>
        {
            var chart = Open(t).Chart;
            t.Advance(0.1);
            t.Assert.Equal(chart.Plot.Points, 91, "mặc định 3 tháng = 91 điểm (2024-04-01 … 06-30)");
            // interval="preserveEnd": nhãn cuối luôn còn, các nhãn trước bị bỏ bớt cho khỏi đè nhau (cách ≥ minTickGap 32px).
            t.Assert.Equal(chart.Plot.TickLabels.Last(), "Jun 30", "nhãn cuối của trục X");
            t.Assert.TextVisible("Jun 30");
            int builds = chart.Plot.BuildCount;
            var plot = t.Box(DashboardAreaChart.PlotKey);
            var before = Sample(t, plot);
            t.Click("toggle-7d");
            t.Advance(0.1);
            t.Assert.Equal(chart.Range, "7d", "bấm nút thì đổi khoảng");
            t.Assert.Equal(chart.Plot.Points, 8, "7 ngày = 06-23 … 06-30");
            t.Assert.True(chart.Plot.BuildCount > builds, "biểu đồ dựng lại hình với dữ liệu mới");
            t.Assert.TextVisible("Jun 24");                                // Jun 23 ở sát mép trái, nửa nhãn tràn ra ngoài → bị bỏ
            t.Assert.True(t.Tree.FindText("Jun 23") is null && t.Tree.FindText("Apr 3") is null, "mốc trục X cũ biến mất");
            t.Assert.True(!before.AsSpan().SequenceEqual(Sample(t, t.Box(DashboardAreaChart.PlotKey))), "hình vẽ đổi sau khi đổi khoảng");
            // Bàn phím (roving focus của Radix): ← chỉ dời focus, Space bật nút.
            t.Press(Key.Left);
            t.Assert.Focused("toggle-30d");
            t.Assert.Equal(chart.Range, "7d", "mũi tên không tự bật nút");
            t.Press(Key.Space);
            t.Advance(0.1);
            t.Assert.Equal(chart.Range, "30d", "Space bật 30 ngày");
            t.Assert.Equal(chart.Plot.Points, 31, "30 ngày = 05-31 … 06-30");
            t.Press(Key.Space);                                            // type="single": bấm lại nút đang bật thì bỏ chọn
            t.Assert.Equal(chart.Range, "", "bỏ chọn hết như Radix");
            t.Assert.Equal(chart.Plot.Points, 91, "timeRange rỗng lọc như 90 ngày");
            // Rê chuột: điểm gần nhất theo trục X hiện tooltip (ngày + Mobile / Desktop).
            var area = t.Box(DashboardAreaChart.PlotKey);
            t.Hover(new Point(area.Right - 6, area.Y + 100));
            t.Assert.Equal(chart.Plot.HoverIndex, 90, "điểm cuối (Jun 30) được rê");
            t.Assert.Exists("chart-tooltip");
            t.Assert.TextVisible("446");                                   // Desktop của 2024-06-30
            t.Hover(new Point(area.X - 40, area.Y - 40));
            t.Assert.NotExists("chart-tooltip");
            int idle = chart.Plot.BuildCount;
            t.Frames(10);
            t.Assert.Equal(chart.Plot.BuildCount, idle, "vẽ lại nhiều frame không dựng lại hình vector");
        });

        suite.Add("shadcn/dashboard-01/tabs", t =>
        {
            var table = Open(t).Table;
            t.Assert.Exists(DashboardTable.SelectAllKey);
            t.ClickText("Past Performance");
            t.Assert.Equal(table.Tab, 1, "bấm tab thì đổi tab");
            t.Assert.Exists(DashboardTable.PlaceholderKey);
            t.Assert.NotExists(DashboardTable.SelectAllKey);
            t.Press(Key.Right);
            t.Assert.Equal(table.Tab, 2, "→ sang tab kế (activationMode automatic)");
            t.Press(Key.Home);
            t.Assert.Equal(table.Tab, 0, "Home về tab Outline");
            t.Assert.Exists(DashboardTable.SelectAllKey);
            t.Assert.NotExists(DashboardTable.PlaceholderKey);
        });

        ShadcnDashboardTableTests.Register(suite, Open);
    }

    /// <summary>Màu trên một lưới 24×8 điểm trong <paramref name="area"/> – để so hình vẽ trước và sau một thao tác.</summary>
    static Rgba[] Sample(UiTest t, Bounds area)
    {
        var colors = new Rgba[24 * 8];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = t.PixelAt(new Point(area.X + (i % 24 + 0.5f) * area.Width / 24, area.Y + (i / 24 + 0.5f) * area.Height / 8));
        return colors;
    }
}
