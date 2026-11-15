using GotX;
using GotX.Components;
using GotX.Test;

/// <summary>
/// Test block dashboard-01 của samples/Shadcn: lint (sáng/tối × hai cỡ cửa sổ × bốn scale), hành vi của sidebar, biểu đồ,
/// tab và bảng dữ liệu bằng chuột lẫn bàn phím, hiệu năng.
/// </summary>
static class ShadcnDashboardTests
{
    /// <summary>Bọc block như ô trang FullBleed của SiteShell (cột flex, ô <c>flex-1 min-h-0 overflow-hidden</c>, view cao hết ô).</summary>
    sealed class Slot(View block) : View
    {
        public override Div Render(Window w) =>
            w.Div().Class("size-full flex flex-col bg-background text-foreground")
                .Child(w.Div().Key("slot").Class("flex-1 min-h-0 overflow-hidden").Child(w.View(block).Class("size-full")));
    }

    static readonly Size[] Sizes = [new(1440, 900), new(1024, 768)];
    static readonly Size Desktop = new(1440, 900);

    static void Lint(UiTest t, string name, Action<UiTest>? prepare = null)
    {
        var report = UiDoctor.Run(app => app.New(new Slot(app.New(new Dashboard01Block()))),
            new DoctorOptions { Sizes = Sizes, OutDir = t.Options.OutDir, Name = name, Prepare = prepare, Lint = TabsQuirk() });
        int bad = report.Count(Severity.Warning) + report.Count(Severity.Error);
        t.Assert.True(bad == 0, $"{name}: {bad} vấn đề lint trên {report.Configs} cấu hình\n{report.ToText(30)}");
    }

    static Dashboard01Block Open(UiTest t, Size? size = null)
    {
        Dashboard01Block block = null!;
        t.Open(app => app.New(new Slot(block = app.New(new Dashboard01Block()))), size ?? Desktop);
        return block;
    }

    /// <summary>
    /// Bỏ qua đúng một lỗi có nguồn gốc trong component Tabs (Navigation/Tabs.cs): các nút tab là flex-1 nên rộng bằng nhau,
    /// nhãn dài ("Past Performance", "Focus Documents") rộng hơn nút ~12px. Block không sửa được component này.
    /// </summary>
    public static LintOptions TabsQuirk() => new LintOptions().Suppress("text-overflow", "gotx-tabs-");

    static void Focus(UiTest t, string key) { t.Window.FocusKey(key); t.Frames(); }

    public static void Register(TestSuite suite)
    {
        suite.Add("shadcn/dashboard-01/lint", t => Lint(t, "dashboard-01"));
        suite.Add("shadcn/dashboard-01/lint-sidebar-hidden", t => Lint(t, "dashboard-01-hidden", p => p.Click(Dashboard01Block.TriggerKey)));
        suite.Add("shadcn/dashboard-01/lint-other-tab", t => Lint(t, "dashboard-01-tab", p => p.ClickText("Focus Documents")));

        suite.Add("shadcn/dashboard-01/sidebar", t =>
        {
            var b = Open(t);
            t.Assert.Size(Dashboard01Block.SidebarKey, 256, 900);
            t.Assert.TextVisible("Acme Inc.");
            t.Assert.TextVisible("Quick Create");
            t.Click(Dashboard01Block.TriggerKey);                          // offcanvas: sidebar biến mất hẳn
            t.Assert.True(!b.SidebarOpen, "bấm trigger thì ẩn sidebar");
            t.Assert.NotExists(Dashboard01Block.SidebarKey);
            t.Assert.Near(t.Box(Dashboard01Block.ContentKey).Width, 1440 - 16, 0.5f, "vùng nội dung chiếm hết bề ngang (trừ lề 8px hai bên)");
            Focus(t, Dashboard01Block.TriggerKey);
            t.Press(Key.Enter);
            t.Assert.Size(Dashboard01Block.SidebarKey, 256, 900);
            t.Assert.Focused(Dashboard01Block.TriggerKey);

            t.Assert.Equal(b.Nav.Active, (0, 0, -1), "mục đang chọn ban đầu (Dashboard)");
            t.Click(b.Nav.ItemKey(0, 2));
            t.Assert.Equal(b.Nav.Active, (0, 2, -1), "bấm Analytics thì chọn nó");
            t.Assert.Equal(b.LastAction, "Analytics", "ItemSelected tới block");
            // Nhóm phụ (Settings…) bị đẩy xuống đáy vùng menu, ngay trên hàng tài khoản.
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
            t.Assert.NoLint(TabsQuirk());
        });

        suite.Add("shadcn/dashboard-01/chart-range", t =>
        {
            var chart = Open(t).Chart;
            t.Advance(0.1);                                                // Chart đo vùng vẽ bằng hẹn giờ rồi dựng lại một lần
            t.Assert.Equal(chart.Points, 91, "mặc định 3 tháng = 91 điểm");
            t.Assert.TextVisible("Apr 1");
            int builds = chart.Chart.BuildCount;
            var plotBefore = t.Window.AnchorBounds(chart.Chart.PlotAnchor)!.Value;
            var before = Sample(t, plotBefore);
            t.ClickText("Last 7 days");
            t.Advance(0.1);
            t.Assert.Equal(chart.Range, 2, "bấm nút thì đổi khoảng");
            t.Assert.Equal(chart.Points, 7, "7 ngày = 7 điểm");
            t.Assert.True(chart.Chart.BuildCount > builds, "biểu đồ dựng lại hình với dữ liệu mới");
            t.Assert.TextVisible("Jun 24");
            t.Assert.True(t.Tree.FindText("Apr 1") is null, "mốc trục X cũ biến mất");
            var plot = t.Window.AnchorBounds(chart.Chart.PlotAnchor)!.Value;
            t.Assert.True(!before.AsSpan().SequenceEqual(Sample(t, plot)), "hình vẽ trong vùng biểu đồ đổi sau khi đổi khoảng");
            // Bàn phím: nhóm nút là một điểm dừng, ← chuyển sang nút bên trái, Space bật nó.
            t.Press(Key.Left);
            t.Press(Key.Space);
            t.Advance(0.1);
            t.Assert.Equal(chart.Range, 1, "←, Space chọn 30 ngày");
            t.Assert.Equal(chart.Points, 30, "30 ngày = 30 điểm");
            t.Press(Key.Space);                                            // bấm lại nút đang bật: vẫn phải còn một khoảng
            t.Assert.True(chart.Toggle.IsPressed(1) && chart.Range == 1, "không thể bỏ chọn hết các khoảng");
            int idle = chart.Chart.BuildCount;
            t.Frames(10);
            t.Assert.Equal(chart.Chart.BuildCount, idle, "vẽ lại nhiều frame không dựng lại hình vector");
        });

        suite.Add("shadcn/dashboard-01/tabs", t =>
        {
            var table = Open(t).Table;
            t.Assert.Exists(table.Table.SelectAllKey);
            t.ClickText("Past Performance");
            t.Assert.Equal(table.Tabs.SelectedIndex, 1, "bấm tab thì đổi tab");
            t.Assert.Exists(DashboardTable.PlaceholderKey);
            t.Assert.NotExists(table.Table.SelectAllKey);
            t.Press(Key.Right);
            t.Assert.Equal(table.Tabs.SelectedIndex, 2, "→ sang tab kế");
            t.Press(Key.Home);
            t.Assert.Equal(table.Tabs.SelectedIndex, 0, "Home về tab Outline");
            t.Assert.Exists(table.Table.SelectAllKey);
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
