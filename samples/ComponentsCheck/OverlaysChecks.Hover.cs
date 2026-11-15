using GotX;
using GotX.Components;

/// <summary>Kiểm tra Overlays phần rê chuột có độ trễ (tooltip, hover card) – thời gian chạy theo đồng hồ ảo của harness.</summary>
static partial class OverlaysChecks
{
    static void Hovering(CheckHarness h)
    {
        Tooltip tip = null!, slow = null!;
        HoverCard card = null!;
        var w = Host(h, app =>
        {
            tip = app.New(new Tooltip { Text = "Thêm vào thư viện", Trigger = static w => Ui.Button(w, "Rê chuột", ButtonVariant.Outline).Key("tip-btn").Focusable() });
            slow = app.New(new Tooltip { Text = "Chậm", OpenDelay = 0.5, Trigger = static w => Ui.Button(w, "Chờ", ButtonVariant.Outline) });
            card = app.New(new HoverCard
            {
                Trigger = static w => Ui.Button(w, "@gotx", ButtonVariant.Link).Key("card-link").Focusable(),
                Body = static w => w.Div().Class("flex flex-col gap-1").Child("GotX").Child(w.Div().Class("text-muted-foreground").Child("Framework UI cho .NET")),
            });
        }, w => w.Div().Class("flex gap-24").Child(w.View(tip)).Child(w.View(slow)).Child(w.View(card)));
        var away = new Point(h.Size.Width - 20, h.Size.Height - 20);

        var t = Anchor(w, tip.AnchorName);
        h.Move(w, CheckHarness.Center(t));
        var tb = tip.Overlay?.ContentBounds ?? default;
        h.Check($"tooltip: OpenDelay = 0 thì rê chuột vào là hiện ngay phía trên ({tb.Bottom:F0} ≤ {t.Y:F0})", tip.IsOpen && tb.Bottom <= t.Y && tb.Height > 0);
        h.Save(w, "overlay-tooltip.png");
        h.Move(w, new Point(t.X + 5, t.Bottom + 120));
        h.Check("tooltip: rời chuột thì ẩn", !tip.IsOpen && w.Overlays.Count == 0);

        var s = CheckHarness.Center(Anchor(w, slow.AnchorName));
        h.Move(w, s);
        h.Wait(w, 0.3);
        h.Check("tooltip có trễ: chưa đủ 0.5 giây thì chưa hiện", !slow.IsOpen);
        h.Wait(w, 0.3);
        h.Check("tooltip có trễ: đủ thời gian thì hiện", slow.IsOpen);
        h.Move(w, away);
        h.Check("tooltip có trễ: rời chuột thì ẩn ngay", !slow.IsOpen);
        h.Move(w, s);
        h.Wait(w, 0.2);
        h.Move(w, away);
        h.Wait(w, 0.6);
        h.Check("tooltip có trễ: rời đi trước hạn thì huỷ hẹn giờ, không hiện", !slow.IsOpen && w.App.PendingTimers == 0);

        h.Press(w, Key.Tab);
        h.Check("tooltip: Tab tới nút thì hiện (focus bàn phím)", w.IsFocused("tip-btn") && w.FocusVisible && tip.IsOpen);
        h.Press(w, Key.Escape);
        h.Check("tooltip: Escape ẩn dù nút còn focus", !tip.IsOpen);
        h.Press(w, Key.Tab, Modifiers.Shift);
        h.Press(w, Key.Tab);
        h.Check("tooltip: focus rời đi rồi quay lại thì hiện lại", tip.IsOpen);
        h.Press(w, Key.Tab);
        h.Check("tooltip: Tab đi tiếp thì ẩn", !tip.IsOpen && !w.IsFocused("tip-btn"));
        w.Blur();
        h.Frames(w);

        var a = Anchor(w, card.AnchorName);
        h.Move(w, CheckHarness.Center(a));
        h.Wait(w, 0.5);
        h.Check("hover-card: chưa đủ 0.7 giây thì chưa hiện", !card.IsOpen);
        h.Wait(w, 0.3);
        h.Check("hover-card: đủ độ trễ mở thì hiện thẻ", card.IsOpen);
        var cb = card.Overlay!.ContentBounds;
        h.Save(w, "overlay-hover-card.png");
        h.Move(w, CheckHarness.Center(cb));
        h.Wait(w, 0.5);
        h.Check("hover-card: đi tiếp vào thẻ thì thẻ vẫn mở", card.IsOpen);
        h.Move(w, away);
        h.Wait(w, 0.15);
        h.Check("hover-card: rời cả hai nhưng chưa đủ 0.3 giây thì còn mở", card.IsOpen);
        h.Wait(w, 0.25);
        h.Check("hover-card: đủ độ trễ đóng thì ẩn", !card.IsOpen && w.Overlays.Count == 0);

        h.Move(w, CheckHarness.Center(a));
        h.Wait(w, 0.8);
        h.Move(w, away);
        h.Wait(w, 0.1);
        h.Move(w, CheckHarness.Center(a));
        h.Wait(w, 0.6);
        h.Check("hover-card: rời rồi quay lại trong lúc chờ đóng thì huỷ lệnh đóng", card.IsOpen);
        h.Move(w, away);
        h.Wait(w, 0.5);
        h.Move(w, CheckHarness.Center(a));
        h.Wait(w, 0.3);
        h.Move(w, away);
        h.Wait(w, 1);
        h.Check("hover-card: lướt qua chưa đủ lâu thì không hiện, không còn hẹn giờ treo", !card.IsOpen && w.App.PendingTimers == 0);

        w.FocusKey("card-link");
        h.Frames(w);
        h.Wait(w, 0.5);
        bool early = card.IsOpen;
        h.Wait(w, 0.4);
        h.Check("hover-card: focus bàn phím vào liên kết cũng mở thẻ sau độ trễ", !early && card.IsOpen);
        h.Press(w, Key.Tab);
        h.Wait(w, 0.5);
        h.Check("hover-card: focus rời đi thì thẻ ẩn sau độ trễ đóng", !card.IsOpen);
    }
}
