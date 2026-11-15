using GotX;
using GotX.Components;
using GotX.Test;

/// <summary>Test block sidebar-03, đường đi qua SiteShell thật của hai block sidebar, và vài hàm phụ dùng chung.</summary>
static partial class ShadcnSidebarTests
{
    static void Focus(UiTest t, string key) { t.Window.FocusKey(key); t.Frames(); }

    /// <summary>Pixel trong mép trái của nút: nền sidebar-accent khi đang chọn, nền sidebar khi không (giao diện sáng).</summary>
    static void Highlighted(UiTest t, string key, bool on)
    {
        var box = t.Box(key);
        t.Assert.Pixel(new Point(box.X + 4, box.Y + box.Height / 2), Rgba.FromHex(on ? 0xf5f5f5u : 0xfafafau), tolerance: 1);
    }

    static Sidebar03Block Open03(UiTest t, Size? size = null, bool dark = false)
    {
        Sidebar03Block block = null!;
        t.Open(app => app.New(new Slot(block = app.New(new Sidebar03Block()))), size ?? Desktop, dark: dark);
        return block;
    }

    static void RegisterSidebar03(TestSuite suite)
    {
        Func<App, View> root03 = app => app.New(new Slot(app.New(new Sidebar03Block())));
        suite.Add("shadcn/sidebar-03/lint", t => Lint(t, "sidebar-03", root03));
        suite.Add("shadcn/sidebar-03/lint-hidden", t => Lint(t, "sidebar-03-hidden", root03, p => p.Click(Sidebar03Block.TriggerKey)));

        suite.Add("shadcn/sidebar-03/toggle-by-click-and-keyboard", t =>
        {
            var b = Open03(t);
            t.Assert.Size(Sidebar03Block.SidebarKey, 256, 900);
            t.Assert.TextVisible("Documentation");
            t.Assert.TextVisible("v1.0.0");
            t.Assert.Position(Sidebar03Block.ContentKey, 256, 64);
            t.Click(Sidebar03Block.TriggerKey);                            // offcanvas: sidebar biến mất hẳn
            t.Assert.True(!b.Open, "bấm trigger thì ẩn sidebar");
            t.Assert.NotExists(Sidebar03Block.SidebarKey);
            t.Assert.True(t.Tree.FindText("Getting Started") is null, "ẩn sidebar: các nút biến mất");
            t.Assert.Size(Sidebar03Block.ContentKey, 1440, 836);
            t.Assert.NoLint();
            Focus(t, Sidebar03Block.TriggerKey);
            t.Press(Key.Enter);
            t.Assert.Size(Sidebar03Block.SidebarKey, 256, 900);
            t.Assert.Focused(Sidebar03Block.TriggerKey);
            t.Press(Key.Space);
            t.Assert.NotExists(Sidebar03Block.SidebarKey);
            t.Press(Key.Space);
            // Thứ tự Tab: nút cuối của sidebar → trigger → link breadcrumb.
            Focus(t, b.Nav.SubItemKey(0, 4, 0));
            t.Press(Key.Tab);
            t.Assert.Focused(Sidebar03Block.TriggerKey);
            t.Press(Key.Tab);
            t.Assert.Focused("crumb-parent");
        });

        suite.Add("shadcn/sidebar-03/submenus-and-select", t =>
        {
            var nav = Open03(t, new Size(1024, 768)).Nav;
            string fetching = nav.SubItemKey(0, 1, 1), routing = nav.SubItemKey(0, 1, 0);
            t.Assert.Equal(nav.Active, (0, 1, 1), "mục đang chọn ban đầu (Data Fetching)");
            Highlighted(t, fetching, true);
            // Danh sách con luôn mở và thụt vào sau đường kẻ trái.
            t.Assert.Exists(nav.SubItemKey(0, 0, 1));
            t.Assert.True(t.Box(routing).X > t.Box(nav.ItemKey(0, 1)).X + 20, "mục con thụt vào so với nút cha");
            t.Click(routing);
            t.Assert.Equal(nav.Active, (0, 1, 0), "mục đang chọn sau khi bấm Routing");
            Highlighted(t, fetching, false);
            t.Click(nav.ItemKey(0, 0));                                    // nút cha không gập: bấm là chọn
            t.Assert.Equal(nav.Active, (0, 0, -1), "mục đang chọn sau khi bấm Getting Started");
            t.Assert.Exists(nav.SubItemKey(0, 0, 0));
            t.Press(Key.Tab);
            t.Press(Key.Enter);
            t.Assert.Equal(nav.Active, (0, 0, 0), "Tab + Enter chọn Installation");
            // Sidebar tự cuộn (danh sách dài hơn cửa sổ), vùng inset đứng yên.
            t.Scroll(nav.ContentKey, 300);
            t.Advance(0.6);
            t.Assert.True(t.Window.GetScroll(nav.ContentKey) is { Offset.Y: > 100 }, "lăn chuột trên sidebar cuộn danh sách");
            t.Assert.True(t.Window.GetScroll(Sidebar03Block.ContentKey) is { Offset.Y: 0 }, "vùng inset không cuộn theo");
            t.Assert.NoLint();
        });

        suite.Add("shadcn/sidebar-03/perf", t =>
        {
            var b = Open03(t);
            t.Assert.True(t.Settle(), "block đứng yên sau khi mở");
            t.Perf.ExpectIdle();
            // Ẩn sidebar vẽ lại vỏ + block; hiện lại thêm nav và view menu ẩn của nó; chọn một mục: nav + hai view cha.
            t.Perf.ExpectRenders(() => t.Click(Sidebar03Block.TriggerKey), max: 2);
            t.Perf.ExpectRenders(() => t.Click(Sidebar03Block.TriggerKey), max: 4);
            t.Perf.ExpectRenders(() => t.Click(b.Nav.SubItemKey(0, 2, 0)), max: 3);
            t.Perf.ExpectIdle();
            t.Perf.ExpectReplayMatchesFresh();
        });

        suite.Add("shadcn/sidebar/through-site-shell", t =>
        {
            var shell = t.Open(new SiteShell(), Desktop);
            t.Click("nav-blocks");
            t.Click("block-sidebar-07");
            t.Assert.Equal(shell.Current, "sidebar-07", "trang đang mở");
            t.Assert.Text(Sidebar07Block.TeamNameKey, "Acme Inc");
            t.Assert.Near(t.Box(Sidebar07Block.SidebarKey).Width, 256, 0.5f, "bề rộng sidebar trong shell");
            t.Click(Sidebar07Block.TriggerKey);
            t.Assert.Near(t.Box(Sidebar07Block.SidebarKey).Width, 48, 0.5f, "bề rộng sidebar sau khi thu gọn trong shell");
            t.Assert.NoLint();
            t.Click("block-sidebar-03");
            t.Assert.Equal(shell.Current, "sidebar-03", "trang đang mở");
            t.Assert.TextVisible("Documentation");
            t.Click(Sidebar03Block.TriggerKey);
            t.Assert.NotExists(Sidebar03Block.SidebarKey);
            t.Assert.NoLint();
            // Quay lại sidebar-07: view được shell giữ nên vẫn đang thu gọn.
            t.Click("block-sidebar-07");
            t.Assert.Near(t.Box(Sidebar07Block.SidebarKey).Width, 48, 0.5f, "trạng thái thu gọn được giữ khi đổi trang");
        });
    }
}
