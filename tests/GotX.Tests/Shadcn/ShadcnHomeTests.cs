using GotX;
using GotX.Components;
using GotX.Test;

/// <summary>
/// Test trang chủ của ví dụ samples/Shadcn: lint ở ba bề rộng × sáng/tối, hành vi từng thẻ có tương tác, hiệu năng.
/// </summary>
static partial class ShadcnHomeTests
{
    /// <summary>Bọc trang như vùng nội dung của SiteShell (cuộn dọc, nền/chữ theo theme) và giữ lại view trang để đọc state.</summary>
    sealed class HomeHost : View
    {
        public HomePage Page { get; private set; } = null!;
        protected override void OnCreated() => Page = App.New(new HomePage());
        public override Div Render(Window w) =>
            w.Div().Class("size-full overflow-y-auto bg-background text-foreground").Key("site-page-home").Child(w.View(Page));
    }

    // Cửa sổ cao để cả lưới thẻ nằm trong khung nhìn: click theo toạ độ và luật tương phản chỉ làm được với phần đang hiện.
    // Cột thẻ thứ năm (Distribute Track, Analytics, Notifications, Power Usage) chỉ hiện từ 1900px như bản gốc: Full.
    static readonly Size Full = new(1920, 2400), Wide = new(1440, 2400), Medium = new(1024, 2400), Narrow = new(640, 1600);

    static HomePage Open(UiTest t, Size? size = null, bool dark = false)
    {
        var host = t.Open(new HomeHost(), size ?? Wide, dark: dark);
        t.Settle();
        return host.Page;
    }

    /// <summary>Focus phần tử bằng bàn phím rồi nhấn <paramref name="key"/> – chứng minh nó là điểm dừng Tab và phím kích hoạt được.</summary>
    static void PressOn(UiTest t, string key, Key press = Key.Enter)
    {
        t.Window.FocusKey(key);
        t.Frames();
        t.Assert.Focused(key);
        t.Press(press);
    }

    static void TypeInto(UiTest t, string key, string text) { t.Click(key); t.Type(text); }

    /// <summary>
    /// Hai chỗ cố ý như bản gốc: dải mờ đáy khối demo phủ lên nút Update Security (nên tương phản đo trên ảnh thấp), và ảnh
    /// chụp dưới md rộng 140vw bị cắt ở mép phải.
    /// </summary>
    static LintOptions HomeLint() => new LintOptions().Suppress("low-contrast", "update-security").Suppress("clipped", "home-mobile");

    public static void Register(TestSuite suite)
    {
        suite.Add("shadcn/home/lint", t =>
        {
            var report = UiDoctor.Run(app => app.New(new HomeHost()), new DoctorOptions
            {
                Sizes = [Wide, Medium, Narrow], Scales = [1, 1.5f], OutDir = t.Options.OutDir, Name = "shadcn-home", Lint = HomeLint(),
            });
            if (t.Options.Verbose) t.Notes.Add(report.ToText(200).TrimEnd());
            foreach (var issue in report.Issues)
                if (issue.Severity >= Severity.Warning) t.FailIssue(issue);
        });

        // Ảnh từng cấu hình để xem bằng mắt (ghi vào --out); bề rộng 640 dài hơn cửa sổ nên chụp thêm từng đoạn cuộn.
        suite.Add("shadcn/home/render", t =>
        {
            foreach (var (name, size) in new[] { ("1440", Wide), ("1024", Medium), ("640", Narrow) })
                foreach (bool dark in new[] { false, true })
                {
                    Open(t, size, dark);
                    t.Assert.TextVisible("The Foundation for your Design System");
                    t.Save($"home-{name}-{(dark ? "dark" : "light")}");
                }
            Open(t, Full);
            t.Save("home-1920-light");
            // Các trạng thái mở: hộp thoại, danh sách chọn, menu, chat sau một lượt hỏi–đáp (dark).
            var home = Open(t, new Size(1440, 1800), dark: true);
            t.Click("chat-send");
            for (int i = 0; i < 60 && home.Chat.IsBusy; i++) t.Advance(0.25);
            t.Click(home.Payout.Currency.TriggerKey); t.Settle();
            t.Save("home-states-select-chat");
            t.Press(Key.Escape); t.Click(home.Elements.Actions.TriggerKey); t.Settle();
            t.Save("home-states-menu");
            t.Press(Key.Escape); t.Click("ui-alert-open"); t.Settle();
            t.Save("home-states-dialog");
        });

        suite.Add("shadcn/home/layout-reflows", t =>
        {
            Open(t, Full); // từ 1900px: 5 cột, đệm 48px
            t.Assert.Aligned(Edge.Top, 1, "ui-elements-card", "contribution-card", "milestone-card", "qr-card", "distribute-card");

            Open(t, Wide); // từ 1400px: 4 cột rộng 330px, cột thứ năm ẩn
            t.Assert.Aligned(Edge.Top, 1, "ui-elements-card", "contribution-card", "milestone-card", "qr-card");
            t.Assert.Near(t.Box("ui-elements-card").Width, 330, 0.5f, "(1440 − 2·24 − 3·24) / 4");
            t.Assert.Hidden("distribute-card");
            t.Assert.True(t.Box("nav-documents").Y < t.Box("nav-analytics").Y, "từ xl: Planning đứng trên Overview (col/row-start)");

            Open(t, Medium); // lg: 3 cột – cột New Milestone/Payout/Account ẩn tới 1400px
            t.Assert.Aligned(Edge.Top, 1, "ui-elements-card", "contribution-card", "qr-card");
            t.Assert.Hidden("milestone-card");
            t.Assert.True(t.Box("nav-analytics").Y < t.Box("nav-help-center").Y, "dưới xl: Overview, Planning / Support, Account");

            Open(t, Narrow); // dưới md: ảnh chụp thay cho khối demo
            t.Assert.Hidden("home-demo"); t.Assert.Visible("home-mobile");
            t.Assert.Near(t.Box("home-mobile").Height, 896 * 2764 / 2560f, 1, "ảnh rộng 140vw theo tỉ lệ 2560×2764");
        });

        suite.Add("shadcn/home/static-cards", t =>
        {
            Open(t, Full);
            foreach (string text in new[] { "$420,000", "65% achieved", "$1,211.29", "Pending Setup", "Realty Income", "Distribute Track", "418.2K Visitors", "3.4 kW", "Scan to connect your mobile device" })
                t.Assert.TextVisible(text);
            // Cột biểu đồ cao amount/1300 × 200px; riêng cột cao nhất bị flex co còn 200 − nhãn 16 − gap 8 = 176px.
            t.Assert.Near(t.Box("contribution-bar-3").Height, 176, 0.5f, "cột Mar");
            t.Assert.Near(t.Box("contribution-bar-0").Height, 800 / 1300f * 200, 0.5f, "cột Dec");
            // QR: ô (0,0) đen, ô (1,1) trắng – mỗi ô 160/21 px.
            var qr = t.Box("qr-code");
            t.Assert.Pixel(new Point(qr.X + 3, qr.Y + 3), Rgba.FromHex(0x000000));
            t.Assert.Pixel(new Point(qr.X + 11, qr.Y + 11), Rgba.FromHex(0xffffff));
            foreach (string key in new[] { "get-started", "view-components", "announcement", "view-full-report", "create-release", "view-analytics", "dismiss-dividend", "footer-github" })
                PressOn(t, key);
        });

        // Mọi class của trang (cả style rhea, overlay đang mở, mọi breakpoint) đều được Core hiểu: không có token bị bỏ qua.
        suite.Add("shadcn/home/classes", t =>
        {
            var unknown = new SortedSet<string>(StringComparer.Ordinal);
            void Collect() { foreach (var n in t.Tree.Nodes) if (n.Classes is { } c) foreach (var u in Tailwind.Get(c).Unknown) unknown.Add(u); }
            foreach (var size in new[] { Full, Wide, Medium, Narrow })
                foreach (bool dark in new[] { false, true }) { Open(t, size, dark); Collect(); }
            var home = Open(t);
            t.Click(home.Payout.Currency.TriggerKey); t.Settle(); Collect();
            t.Press(Key.Escape); t.Click(home.Elements.Actions.TriggerKey); t.Settle(); Collect();
            t.Press(Key.Escape); t.Click("ui-alert-open"); t.Settle(); Collect();
            t.Press(Key.Escape); t.Hover("chat-reset"); t.Settle(); Collect();
            t.Assert.True(unknown.Count == 0, "class chưa hỗ trợ: " + string.Join(", ", unknown));
        });

        RegisterCards(suite);
        RegisterChatAndPerf(suite);
    }
}
