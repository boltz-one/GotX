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
    static readonly Size Wide = new(1440, 2400), Medium = new(1024, 2400), Narrow = new(640, 3200);

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

    public static void Register(TestSuite suite)
    {
        suite.Add("shadcn/home/lint", t =>
        {
            var report = UiDoctor.Run(app => app.New(new HomeHost()), new DoctorOptions
            {
                Sizes = [Wide, Medium, Narrow], Scales = [1, 1.5f], OutDir = t.Options.OutDir, Name = "shadcn-home",
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
            Open(t, new Size(640, 1200));
            for (int page = 0; page < 7; page++)
            {
                t.Window.ScrollTo("site-page-home", 0, page * 1100);
                t.Frames(4);
                t.Save($"home-640-page{page}");
            }
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
            Open(t, Wide); // xl: 4 cột, cột thứ năm thành hàng thẻ bên dưới
            t.Assert.Aligned(Edge.Top, 1, "ui-elements-card", "contribution-card", "milestone-card", "qr-card");
            t.Assert.Aligned(Edge.Top, 1, "distribute-card", "analytics-card", "notifications-card", "power-card");
            t.Assert.True(t.Box("distribute-card").Y > t.Box("payments-card").Bottom, "hàng thẻ thứ năm nằm dưới bốn cột");
            t.Assert.Visible("nav-analytics"); t.Assert.Hidden("nav-compact-analytics");
            t.Assert.True(t.Box("nav-documents").Y < t.Box("nav-analytics").Y, "từ xl: Planning đứng trên Overview như bản gốc");

            Open(t, Medium); // lg: 3 cột
            t.Assert.Aligned(Edge.Top, 1, "ui-elements-card", "contribution-card", "milestone-card");
            t.Assert.True(t.Box("qr-card").Y > t.Box("access-card").Bottom, "cột thứ tư xuống hàng dưới ở 1024");
            t.Assert.Visible("nav-compact-analytics"); t.Assert.Hidden("nav-analytics");

            Open(t, Narrow); // một cột
            t.Assert.Aligned(Edge.Left, 1, "ui-elements-card", "contribution-card", "milestone-card");
            t.Assert.Near(t.Box("ui-elements-card").Width, 640 - 32, 1, "thẻ rộng hết cột ở 640");
        });

        suite.Add("shadcn/home/static-cards", t =>
        {
            Open(t);
            foreach (string text in new[] { "$420,000", "65% achieved", "$1,211.29", "Pending Setup", "Realty Income", "Distribute Track", "418.2K Visitors", "3.4 kW", "Scan to connect your mobile device" })
                t.Assert.TextVisible(text);
            // Cột biểu đồ tỉ lệ với số liệu: Mar (1300) cao nhất = 176px, Dec (800) = 108px.
            t.Assert.Near(t.Box("contribution-bar-3").Height, 176, 0.5f, "cột Mar");
            t.Assert.Near(t.Box("contribution-bar-0").Height, 108, 0.5f, "cột Dec");
            // QR: ô (0,0) đen, ô (1,1) trắng – mỗi ô 160/21 px.
            var qr = t.Box("qr-code");
            t.Assert.Pixel(new Point(qr.X + 3, qr.Y + 3), Rgba.FromHex(0x000000));
            t.Assert.Pixel(new Point(qr.X + 11, qr.Y + 11), Rgba.FromHex(0xffffff));
            foreach (string key in new[] { "get-started", "view-components", "announcement", "view-full-report", "create-release", "view-analytics", "dismiss-dividend", "footer-github" })
                PressOn(t, key);
        });

        RegisterCards(suite);
        RegisterChatAndPerf(suite);
    }
}
