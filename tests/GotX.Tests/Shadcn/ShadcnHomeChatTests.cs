using GotX;
using GotX.Components;
using GotX.Test;

/// <summary>Thẻ chat và Payments, hiệu năng của trang, và trang chạy trong SiteShell thật.</summary>
static partial class ShadcnHomeTests
{
    static void RegisterChatAndPerf(TestSuite suite)
    {
        suite.Add("shadcn/home/chat-and-payments", t =>
        {
            var page = Open(t);
            var chat = page.Chat;
            t.Assert.Exists("chat-empty"); t.Assert.TextContains("chat-draft", "I'm building a chat");
            t.Hover("chat-reset"); t.Settle();
            t.Assert.True(chat.ResetTooltip.IsOpen, "tooltip Reset hiện khi rê chuột");
            t.Click("chat-send");
            t.Assert.True(chat.IsBusy && chat.Scroller.Items.Count == 1, "gửi: tin của người dùng vào danh sách, đang chờ trả lời");
            t.Assert.NotExists("chat-empty");
            t.Click("chat-send"); t.Click("chat-reset");
            t.Assert.Equal(chat.Turn, 1, "đang bận thì nút gửi và reset bị khoá");
            t.Advance(1.2);
            t.Assert.Equal(chat.Scroller.Items.Count, 2, "sau 1 giây câu trả lời bắt đầu hiện");
            int streaming = t.Perf.CountRenders(() => t.Frames(5));
            t.Assert.True(streaming <= 20, $"lúc stream chỉ danh sách tin và ba view bọc nó vẽ lại: {streaming} lượt view trong 5 frame (≤ 20)");
            for (int i = 0; i < 40 && chat.IsBusy; i++) t.Advance(0.25);
            t.Assert.True(!chat.IsBusy, "câu trả lời hiện xong"); t.Assert.TextVisible("That's the classic streaming scroll problem.");
            t.Assert.TextContains("chat-draft", "Okay, but when someone sends");
            t.Settle(); t.Assert.NoLint(HomeLint()); t.Perf.ExpectIdle();
            PressOn(t, "chat-reset");
            t.Assert.True(chat.Turn == 0 && chat.Scroller.Items.Count == 0, "Reset xoá cuộc trò chuyện"); t.Assert.Exists("chat-empty");
            t.Click(chat.AttachMenu.TriggerKey); t.Settle();
            t.Assert.True(chat.AttachMenu.IsOpen, "menu đính kèm mở"); t.Assert.Visible("chat-attach-files");
            t.Press(Key.Escape); t.Settle(); t.Assert.True(!chat.AttachMenu.IsOpen, "Escape đóng menu");

            string? picked = null, opened = null;
            t.App.Subscribe<MenuItemSelected>(page.Payments.AccountMenu, e => picked = e.Item.Text);
            t.App.Subscribe<PaymentsCard.LinkOpened>(page.Payments, e => opened = e.Title);
            PressOn(t, page.Payments.AccountMenu.TriggerKey); t.Settle();
            t.Click("payments-menu-profile"); t.Settle();
            t.Assert.True(picked == "Profile" && !page.Payments.AccountMenu.IsOpen, "menu breadcrumb chọn Profile");
            t.Click("payments-scheduled"); t.Assert.Equal(opened, "Scheduled transfers", "bấm dòng liên kết");
        });

        suite.Add("shadcn/home/perf", t =>
        {
            Open(t, new Size(1440, 900));
            t.Perf.ExpectSettles(); t.Perf.ExpectIdle(); t.Perf.ExpectReplayMatchesFresh();
            t.Scroll("site-page-home", 600); t.Settle();
            t.Perf.ExpectIdle(); t.Perf.ExpectReplayMatchesFresh();
            // Gõ vào một ô chỉ vẽ lại ô đó và các view bọc nó, không phải mọi thẻ.
            t.Click("payout-notes");
            t.Perf.ExpectRenders(() => t.Type("x"), max: 10);
            var report = t.Perf.Measure(frames: 30, raster: false);
            t.Assert.True(report.Total.Median < 30, $"trung vị Draw {Fmt.N(report.Total.Median)} ms < 30 ms");
            t.Assert.True(report.AllocatedBytes.Median == 0, $"frame rảnh không cấp phát: {Fmt.Bytes((long)report.AllocatedBytes.Median)}");
        });

        suite.Add("shadcn/home/in-shell", t =>
        {
            t.Open(new SiteShell(), new Size(1440, 900));
            t.Settle();
            t.Assert.TextVisible("The Foundation for your Design System"); t.Assert.Visible("ui-elements-card");
            t.Assert.NoLint(HomeLint());
            t.Click("toggle-theme"); t.Settle();
            t.Assert.True(t.Window.DarkMode, "đổi sang dark"); t.Assert.NoLint(HomeLint()); t.Perf.ExpectIdle();
        });
    }
}
