using GotX;
using GotX.Components;
using GotX.Test;

/// <summary>
/// Test hai block sidebar-07 / sidebar-03 của samples/Shadcn: lint (sáng/tối × hai cỡ cửa sổ × bốn scale), hành vi bằng
/// chuột và bàn phím, hiệu năng. File này chứa phần dùng chung và sidebar-07; sidebar-03 ở ShadcnSidebarTests.Sidebar03.cs.
/// </summary>
static partial class ShadcnSidebarTests
{
    /// <summary>
    /// Bọc block như ô trang FullBleed của SiteShell (cột flex, ô <c>flex-1 min-h-0 overflow-hidden</c>). Khác shell ở một
    /// điểm: nút view có <c>h-full</c> – thiếu nó thì block chỉ cao bằng nội dung chứ không lấp đầy ô.
    /// </summary>
    sealed class Slot(View block) : View
    {
        // Như SiteShell với SitePage.Style: block của shadcn chạy với style new-york-v4.
        protected override void OnCreated() => ComponentTheme.Use(App, ComponentStyle.NewYorkV4);

        public override Div Render(Window w) =>
            w.Div().Class("size-full flex flex-col bg-background text-foreground")
                .Child(w.Div().Key("slot").Class("flex-1 min-h-0 overflow-hidden").Child(w.View(block).Class("h-full")));
    }

    static readonly Size[] Sizes = [new(1440, 900), new(1024, 768)];
    static readonly Size Desktop = new(1440, 900);

    /// <summary>Soi block ở mọi cấu hình; <paramref name="prepare"/> đưa nó về trạng thái cần soi (thu gọn, mở menu…).</summary>
    static void Lint(UiTest t, string name, Func<App, View> root, Action<UiTest>? prepare = null)
    {
        var report = UiDoctor.Run(root, new DoctorOptions { Sizes = Sizes, OutDir = t.Options.OutDir, Name = name, Prepare = prepare });
        int bad = report.Count(Severity.Warning) + report.Count(Severity.Error);
        t.Assert.True(bad == 0, $"{name}: {bad} vấn đề lint trên {report.Configs} cấu hình\n{report.ToText(30)}");
    }

    static Sidebar07Block Open07(UiTest t, Size? size = null, bool dark = false)
    {
        Sidebar07Block block = null!;
        t.Open(app => app.New(new Slot(block = app.New(new Sidebar07Block()))), size ?? Desktop, dark: dark);
        return block;
    }

    public static void Register(TestSuite suite)
    {
        Func<App, View> root07 = app => app.New(new Slot(app.New(new Sidebar07Block())));
        suite.Add("shadcn/sidebar-07/lint", t => Lint(t, "sidebar-07", root07));
        suite.Add("shadcn/sidebar-07/lint-collapsed", t => Lint(t, "sidebar-07-collapsed", root07, p => p.Click(Sidebar07Block.TriggerKey)));
        suite.Add("shadcn/sidebar-07/lint-menu-open", t => Lint(t, "sidebar-07-menu", root07,
            p => p.Click(Fmt.Center(p.Tree.FindView(nameof(DropdownMenu))!.Screen))));

        suite.Add("shadcn/sidebar-07/collapse-by-click-and-keyboard", t =>
        {
            var b = Open07(t);
            t.Assert.Size(Sidebar07Block.SidebarKey, 256, 900);
            t.Assert.Text(Sidebar07Block.TeamNameKey, "Acme Inc");
            t.Assert.TextVisible("Platform");
            t.Click(Sidebar07Block.TriggerKey);
            t.Assert.True(b.Collapsed, "bấm trigger thì thu gọn");
            t.Assert.Size(Sidebar07Block.SidebarKey, 48, 900);
            t.Assert.Size(b.Nav.ItemKey(0, 0), 32, 32);
            t.Assert.True(t.Tree.FindText("Platform") is null && t.Tree.FindText("Playground") is null, "thu gọn: nhãn nhóm và chữ của nút biến mất");
            t.Assert.NotExists(Sidebar07Block.TeamNameKey);
            t.Assert.NotExists(b.Nav.SubItemKey(0, 0, 0));
            t.Assert.NotExists(b.Nav.ItemKey(1, 0));                       // nhóm Projects ẩn khi thu gọn
            t.Assert.Position(Sidebar07Block.ContentKey, 48, 48);          // header inset thấp lại còn h-12
            // Bàn phím: Tab từ nút cuối của sidebar tới trigger, Enter mở rộng, Space thu gọn lại.
            Focus(t, b.UserMenu.TriggerKey);
            t.Press(Key.Tab);
            t.Assert.Focused(Sidebar07Block.TriggerKey);
            t.Press(Key.Enter);
            t.Assert.True(!b.Collapsed, "Enter trên trigger mở rộng lại");
            t.Assert.Size(Sidebar07Block.SidebarKey, 256, 900);
            t.Assert.TextVisible("Projects");
            t.Assert.Focused(Sidebar07Block.TriggerKey);
            t.Press(Key.Space);
            t.Assert.Size(Sidebar07Block.SidebarKey, 48, 900);
            t.Assert.NoLint();
        });

        suite.Add("shadcn/sidebar-07/submenu-and-select", t =>
        {
            var nav = Open07(t).Nav;
            string models = nav.ItemKey(0, 1), genesis = nav.SubItemKey(0, 1, 0);
            t.Assert.Exists(nav.SubItemKey(0, 0, 0));                      // Playground mở sẵn (isActive)
            t.Assert.NotExists(genesis);
            float below = t.Box(nav.ItemKey(0, 2)).Y;
            t.Click(models);
            t.Assert.Exists(genesis);
            t.Assert.Focused(models);
            t.Assert.True(t.Box(nav.ItemKey(0, 2)).Y > below + 90, "ba mục con của Models đẩy nút Documentation xuống");
            t.Click(models);
            t.Assert.NotExists(genesis);
            t.Assert.Focused(models);
            t.Press(Key.Right);
            t.Assert.Exists(genesis);
            t.Press(Key.Left);
            t.Assert.NotExists(genesis);
            t.Press(Key.Enter);
            t.Press(Key.Tab);
            t.Assert.Focused(genesis);
            // Mục con / dự án là liên kết href="#" của bản gốc: Enter / bấm phát ItemSelected nhưng không tô mục nào.
            string? picked = null;
            t.App.Subscribe<SidebarNav.ItemSelected>(nav, e => picked = e.Title);
            t.Press(Key.Enter);
            t.Assert.Equal(picked, "Genesis", "Enter trên Genesis kích hoạt liên kết");
            t.Assert.Equal(nav.Active, (-1, -1, -1), "không mục nào được tô sau Enter");
            Highlighted(t, genesis, false);
            t.Click(nav.ItemKey(1, 2));
            t.Assert.Equal(picked, "Travel", "bấm Travel kích hoạt liên kết");
            t.Assert.Equal(nav.Active, (-1, -1, -1), "bấm không tô");
            t.Advance(0.3);                                                // mũi tên xoay xong
            t.Assert.NoLint();
        });

        suite.Add("shadcn/sidebar-07/team-switcher", t =>
        {
            var b = Open07(t);
            string trigger = b.TeamMenu.TriggerKey;
            t.Click(trigger);
            t.Assert.True(b.TeamMenu.IsOpen, "bấm hàng team thì mở menu");
            t.Assert.TextVisible("Teams");
            t.Assert.True(b.TeamMenu.Overlay!.ContentBounds.X >= t.Box(trigger).Right, "menu mở sang phải nút (side=right)");
            t.Assert.NoLint();
            t.Click("sidebar-07-team-1");
            t.Assert.True(!b.TeamMenu.IsOpen, "chọn team thì menu đóng");
            t.Assert.Text(Sidebar07Block.TeamNameKey, "Acme Corp.");
            t.Assert.TextVisible("Startup");
            t.Assert.Focused(trigger);
            t.Press(Key.Enter);                                            // mở bằng phím: dòng đầu được tô sẵn
            t.Press(Key.Down);
            t.Press(Key.Down);
            t.Press(Key.Enter);
            t.Assert.Text(Sidebar07Block.TeamNameKey, "Evil Corp.");
            t.Assert.Equal(b.ActiveTeam, "Evil Corp.", "team đang chọn");
            t.Assert.Focused(trigger);
        });

        suite.Add("shadcn/sidebar-07/user-menu", t =>
        {
            var b = Open07(t, new Size(1024, 768), dark: true);
            string trigger = b.UserMenu.TriggerKey;
            t.Click(trigger);
            t.Assert.True(b.UserMenu.IsOpen, "bấm hàng tài khoản thì mở menu");
            t.Assert.TextVisible("Upgrade to Pro");
            t.Press(Key.Escape);
            t.Assert.True(!b.UserMenu.IsOpen, "Escape đóng menu");
            t.Assert.Focused(trigger);
            // Thu gọn còn icon: menu vẫn mở được, nằm bên phải cột icon và trọn trong cửa sổ.
            t.Click(Sidebar07Block.TriggerKey);
            t.Click(trigger);
            var menu = b.UserMenu.Overlay!.ContentBounds;
            t.Assert.Size(trigger, 32, 32);
            t.Assert.True(menu.X >= t.Box(trigger).Right && menu.Bottom <= 768, $"menu tài khoản khi thu gọn nằm bên phải nút, trong cửa sổ: {menu}");
            t.Assert.NoLint();
            t.Click("sidebar-07-user-logout");
            t.Assert.Equal(b.LastAction, "Log out", "dòng vừa chọn");
            t.Assert.Focused(trigger);
        });

        suite.Add("shadcn/sidebar-07/project-row-menu", t =>
        {
            var b = Open07(t);
            string action = b.Nav.ActionKey(1, 1);
            t.Hover(b.Nav.ItemKey(1, 1));
            t.Click(action);
            t.Assert.True(b.Nav.IsMenuOpen, "bấm ⋯ của hàng project thì mở menu");
            t.Assert.True(b.Nav.ActionMenu.Overlay!.ContentBounds.X >= t.Box(action).Right, "menu nằm bên phải nút ⋯");
            t.Assert.NoLint();
            t.Click("sidebar-07-project-share");
            t.Assert.Equal(b.LastAction, "Share Project: Sales & Marketing", "dòng vừa chọn kèm tên project");
            t.Assert.True(!b.Nav.IsMenuOpen, "chọn xong thì menu đóng");
            t.Assert.Focused(action);
            t.Press(Key.Enter);
            t.Assert.True(b.Nav.IsMenuOpen, "Enter trên nút ⋯ mở lại menu");
            t.Press(Key.Escape);
            t.Assert.Focused(action);
            t.Click(action);
            t.Click(action);
            t.Assert.True(!b.Nav.IsMenuOpen, "bấm ⋯ lần hai thì đóng");
        });

        suite.Add("shadcn/sidebar-07/perf", t =>
        {
            var b = Open07(t);
            t.Assert.True(t.Settle(), "block đứng yên sau khi mở");
            t.Perf.ExpectIdle();
            // Thu gọn đổi cả khung: vỏ, block, nav, hai nút menu và view menu ẩn của nav vẽ lại đúng một lượt (6 view).
            t.Perf.ExpectRenders(() => t.Click(Sidebar07Block.TriggerKey), max: 6);
            t.Perf.ExpectRenders(() => t.Click(Sidebar07Block.TriggerKey), max: 6);
            // Mở một nút gập được chỉ làm bẩn nav và hai view cha của nó (2 frame × 3 view); hai nút menu được phát lại.
            // Rê chuột tới trước để lượt vẽ do con trỏ rời hàng team (nó nằm dưới con trỏ sau khi mở rộng) không tính vào.
            t.Hover(b.Nav.ItemKey(0, 1));
            t.Perf.ExpectRenders(() => t.Click(b.Nav.ItemKey(0, 1)), max: 6);
            var frames = t.Perf.Measure(frames: 30, raster: false);
            t.Assert.True(frames.AllocatedBytes.Median == 0, $"frame ổn định không cấp phát (trung vị {Fmt.Bytes((long)frames.AllocatedBytes.Median)})");
            t.Assert.True(t.Settle(), "đứng yên lại sau khi mũi tên xoay xong");
            t.Perf.ExpectIdle();
            t.Perf.ExpectReplayMatchesFresh();
        });

        RegisterSidebar03(suite);
    }
}
