using GotX;
using GotX.Components;
using GotX.Test;

/// <summary>Hành vi các thẻ có control của trang chủ: UI elements, sidebar, các biểu mẫu.</summary>
static partial class ShadcnHomeTests
{
    static void RegisterCards(TestSuite suite)
    {
        suite.Add("shadcn/home/ui-elements", t =>
        {
            var card = Open(t).Elements;
            TypeInto(t, "ui-name", "Ada"); TypeInto(t, "ui-message", "Hello");
            t.Assert.Equal(card.Name.Text, "Ada", "ô Name"); t.Assert.Equal(card.Message.Text, "Hello", "ô Message");

            var radio = t.Box("ui-radio");
            t.Click(new Point(radio.Right - 16, radio.Y + radio.Height / 2));
            t.Assert.Equal(card.Fruit.SelectedIndex, 1, "bấm nút radio thứ hai");
            t.Press(Key.Left);
            t.Assert.Equal(card.Fruit.SelectedIndex, 0, "mũi tên trái chọn lại nút đầu");
            t.Click("ui-checkbox"); t.Assert.True(!card.Alerts.Checked, "checkbox tắt sau khi bấm");
            t.Press(Key.Space); t.Assert.True(card.Alerts.Checked, "Space bật lại checkbox");
            t.Click("ui-switch"); t.Assert.True(!card.Compact.Checked, "switch tắt sau khi bấm");

            int confirmed = 0, cancelled = 0;
            t.App.Subscribe<AlertDialog.Confirmed>(card.Dialog, _ => confirmed++);
            t.App.Subscribe<AlertDialog.Cancelled>(card.Dialog, _ => cancelled++);
            t.Click("ui-alert-open"); t.Settle();
            t.Assert.True(card.Dialog.IsOpen, "hộp thoại mở"); t.Assert.TextVisible("Allow accessory to connect?");
            t.Assert.NoLint(HomeLint());
            t.Press(Key.Escape); t.Settle();
            t.Assert.True(!card.Dialog.IsOpen && cancelled == 1, "Escape đóng hộp thoại và tính là huỷ");
            PressOn(t, "ui-alert-open"); t.Settle();
            t.Click(card.Dialog.ActionKey); t.Settle();
            t.Assert.True(!card.Dialog.IsOpen && confirmed == 1, "bấm Allow xác nhận và đóng");

            string? picked = null;
            t.App.Subscribe<MenuItemSelected>(card.Actions, e => picked = e.Item.Text);
            t.Click(card.Actions.TriggerKey); t.Settle();
            t.Assert.True(card.Actions.IsOpen, "menu Quick Actions mở"); t.Assert.TextVisible("Quick Actions");
            t.Assert.True(card.Actions.Overlay!.ContentBounds.Bottom <= t.Box(card.Actions.TriggerKey).Y + 1, "menu mở phía trên nút (side=top)");
            t.Click("ui-action-mute"); t.Settle();
            t.Assert.True(!card.Actions.IsOpen && picked == "Mute Conversation", "chọn mục menu rồi đóng");
        });

        suite.Add("shadcn/home/sidebar-nav", t =>
        {
            var nav = Open(t).Sidebar;
            var picked = new List<string>();
            t.App.Subscribe<SidebarNavCard.ItemSelected>(nav, e => picked.Add(e.Label));
            t.Assert.Equal(nav.ActiveItem(0), 0, "Analytics đang sáng"); t.Assert.Equal(nav.ActiveItem(3), 1, "Billing đang sáng");
            // Như bản gốc (isActive cố định): bấm / Enter chỉ điều hướng, mục sáng không đổi.
            t.Click("nav-transactions"); PressOn(t, "nav-goals");
            t.Assert.Equal(string.Join(",", picked), "Transactions,Goals", "bấm và Enter phát ItemSelected");
            t.Assert.True(nav.ActiveItem(0) == 0 && nav.ActiveItem(1) == -1, "mục sáng giữ nguyên");
            t.Perf.ExpectRenders(() => t.Click("nav-security"), max: 6);
        });

        suite.Add("shadcn/home/forms", t =>
        {
            var page = Open(t);
            NewMilestoneCard.GoalCreated? goal = null;
            t.App.Subscribe<NewMilestoneCard.GoalCreated>(page.Milestone, e => goal = e);
            TypeInto(t, "goal-name", "New Car"); TypeInto(t, "target-date", "!");
            t.Click("create-goal");
            t.Assert.Equal(goal, new NewMilestoneCard.GoalCreated("New Car", "$15,000", "Dec 2025!"), "Create Goal phát nội dung các ô");
            PressOn(t, "cancel-goal");
            t.Assert.True(page.Milestone.GoalName.Text == "" && page.Milestone.TargetDate.Text == "Dec 2025", "Cancel đưa các ô về ban đầu");

            var currency = page.Payout.Currency;
            t.Click(currency.TriggerKey); t.Settle();
            t.Assert.True(currency.IsOpen, "danh sách tiền tệ mở");
            t.Click(currency.ItemKey(2)); t.Settle();
            t.Assert.True(!currency.IsOpen && currency.Value == "GBP — British Pound", "chọn GBP bằng chuột");
            PressOn(t, currency.TriggerKey, Key.Down); t.Settle();
            t.Press(Key.Down); t.Press(Key.Enter); t.Settle();
            t.Assert.Equal(currency.Value, "JPY — Japanese Yen", "chọn JPY bằng bàn phím");
            t.Assert.Near(t.Box("payout-progress").Height, 8, 0.5f, "thanh tiến độ cao h-2");
            PayoutThresholdCard.Saved? saved = null;
            t.App.Subscribe<PayoutThresholdCard.Saved>(page.Payout, e => saved = e);
            TypeInto(t, "payout-notes", "Monthly"); t.Click("save-threshold");
            t.Assert.Equal(saved, new PayoutThresholdCard.Saved("JPY — Japanese Yen", "Monthly"), "Save Threshold");

            var links = new List<string>();
            AccountAccessCard.SecurityUpdated? updated = null;
            t.App.Subscribe<AccountAccessCard.LinkOpened>(page.Access, e => links.Add(e.Link));
            t.App.Subscribe<AccountAccessCard.SecurityUpdated>(page.Access, e => updated = e);
            TypeInto(t, "email-address", "a@b.co"); TypeInto(t, "current-password", "secret");
            t.Assert.True(t.Tree.FindText("secret") is null && page.Access.Password.Text == "secret", "ô mật khẩu che ký tự nhưng vẫn giữ nội dung");
            t.Click("update-security"); t.Click("forgot-password"); PressOn(t, "danger-zone");
            t.Assert.Equal(updated, new AccountAccessCard.SecurityUpdated("a@b.co", "secret"), "Update Security");
            t.Assert.Equal(string.Join(",", links), "forgot,danger", "liên kết Forgot? và Danger Zone");

            var notify = Open(t, Full).Notifications; // thẻ ở cột thứ năm, chỉ hiện từ 1900px
            t.Click("notify-goals"); t.Assert.True(notify.Option(2).Checked, "bấm ô Goal milestones");
            t.Click("notify-label-transactions"); t.Assert.True(!notify.Option(0).Checked, "bấm nhãn tắt Transaction alerts");
            bool[]? prefs = null;
            t.App.Subscribe<NotificationsCard.Saved>(notify, e => prefs = e.Enabled);
            t.Click("save-preferences");
            t.Assert.Equal(string.Join(",", prefs ?? []), "False,True,True,False", "Save Preferences");
        });
    }
}
