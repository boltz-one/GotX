using GotX;
using GotX.Components;

/// <summary>Kiểm tra hành vi các component nhóm Chat.</summary>
static class ChatChecks
{
    public static void Run(CheckHarness h)
    {
        Bubbles(h);
        Attachments(h);
        Code(h);
        Traces(h);
        Survey(h);
        Scroller(h);
        ChatKeyboardChecks.Run(h);
    }

    static void Bubbles(CheckHarness h)
    {
        var w = h.Open(app => app.New(new ChatProbe()));
        var user = h.Box(w, "probe-user-bubble");
        var bot = h.Box(w, "probe-bot-bubble");
        var row = h.Box(w, "probe-bubble-column");
        h.Check($"bubble: tin người dùng sát mép phải ({user.Right:F0} / {row.Right:F0})", MathF.Abs(user.Right - row.Right) < 1);
        h.Check($"bubble: tin trợ lý sát mép trái ({bot.X:F0} / {row.X:F0})", MathF.Abs(bot.X - row.X) < 1);
        h.Check($"bubble: co theo nội dung, không giãn hết hàng ({user.Width:F0})", user.Width < row.Width * 0.8f);
        var msg = h.Box(w, "probe-message-content");
        var avatar = h.Box(w, "probe-message-avatar");
        h.Check("message: phía End đặt avatar bên phải nội dung", avatar.X >= msg.Right);
    }

    static void Attachments(CheckHarness h)
    {
        var probe = (ChatProbe)h.Open(app => app.New(new ChatProbe())).Root!;
        var w = probe.Window!;
        h.Click(w, "gotx-attachment-remove-" + probe.File.Id.Value);
        h.Check($"attachment: bấm × phát RemoveRequested với tên tệp ({probe.Removed})", probe.Removed == "bao-cao.pdf" && probe.RemoveEvents == 1);
        h.Check("attachment: danh sách gỡ thẻ sau sự kiện", !h.Exists(w, "gotx-attachment-remove-" + probe.File.Id.Value));
        h.Check("attachment: định dạng dung lượng", Attachment.FormatBytes(2_450_000) == "2.3 MB" && Attachment.FormatBytes(512) == "512 B");
    }

    static void Code(CheckHarness h)
    {
        var probe = (ChatProbe)h.Open(app => app.New(new ChatProbe())).Root!;
        var w = probe.Window!;
        w.App.Clipboard.SetText("cũ");
        h.Click(w, "gotx-code-copy-" + probe.Code.Id.Value);
        h.Check("code-block: sao chép đúng nguyên văn code vào App.Clipboard", w.App.Clipboard.GetText() == ChatSamples.Fib && probe.Code.IsCopied);
        h.Check("code-block: tách dòng, bỏ dòng trống cuối", probe.Code.Lines.Count == 8 && probe.Code.Lines[3] == "    long a = 0, b = 1;");
        h.Wait(w, CodeBlock.CopiedSeconds - 0.5);
        h.Check("code-block: 1.5s after copy the check mark is still shown", probe.Code.IsCopied);
        h.Wait(w, 0.7);
        h.Check("code-block: after 2s the timer restores the copy icon", !probe.Code.IsCopied && w.App.PendingTimers == 0);
    }

    static void Traces(CheckHarness h)
    {
        var probe = (ChatProbe)h.Open(app => app.New(new ChatProbe())).Root!;
        var w = probe.Window!;
        string content = "gotx-trace-content-" + probe.Trace.Id.Value, trigger = "gotx-trace-trigger-" + probe.Trace.Id.Value;
        h.Check("trace: mặc định đóng, không có danh sách bước", !probe.Trace.Open && !h.Exists(w, content));
        float before = h.Box(w, "probe-after-trace").Y;
        h.Click(w, trigger);
        h.Check("trace: bấm tiêu đề thì mở và hiện các bước", probe.Trace.Open && h.Exists(w, content) && h.Box(w, content).Height > 60);
        h.Check("trace: mở ra đẩy phần bên dưới xuống", h.Box(w, "probe-after-trace").Y > before + 60);
        h.Click(w, trigger);
        h.Check($"trace: bấm lại thì đóng (OpenChanged × {probe.TraceEvents})", !probe.Trace.Open && !h.Exists(w, content) && probe.TraceEvents == 2);
        h.Check("trace: trạng thái gộp là lỗi khi có bước lỗi", probe.Trace.Summary == TraceStatus.Error);
    }

    static void Survey(CheckHarness h)
    {
        var probe = (ChatProbe)h.Open(app => app.New(new ChatProbe())).Root!;
        var w = probe.Window!;
        var q = probe.Survey;
        string id = "gotx-questionnaire-" + q.Id.Value;
        h.Click(w, id + "-next");
        h.Check("questionnaire: chưa chọn thì Tiếp không đi tiếp", q.Step == 0);
        h.Click(w, id + "-choice-0");
        h.Click(w, id + "-choice-1");
        h.Check("questionnaire: chọn một – lựa chọn sau thay lựa chọn trước", q.Answers[0].SequenceEqual([1]));
        h.Click(w, id + "-next");
        h.Check("questionnaire: Tiếp sang câu 2", q.Step == 1 && probe.Steps == "1");
        h.Click(w, id + "-choice-0");
        h.Click(w, id + "-choice-2");
        h.Click(w, id + "-choice-0");
        h.Check("questionnaire: chọn nhiều – bấm lại thì bỏ chọn", q.Answers[1].SequenceEqual([2]));
        h.Click(w, id + "-prev");
        h.Check("questionnaire: Quay lại giữ câu trả lời cũ", q.Step == 0 && q.Answers[0].SequenceEqual([1]));
        h.Click(w, id + "-next");
        h.Click(w, id + "-next");
        h.Check("questionnaire: câu cuối hiện nút Gửi", q.IsLast && q.Step == 2);
        h.Click(w, id + "-skip");
        h.Check("questionnaire: Bỏ qua ở câu cuối thì gửi", q.IsSubmitted && probe.Submitted is not null);
        var a = probe.Submitted!;
        h.Check("questionnaire: Submitted mang đúng câu trả lời", a.Count == 3 && a[0].SequenceEqual([1]) && a[1].SequenceEqual([2]) && a[2].Count == 0);
        h.Check("questionnaire: hiện tóm tắt sau khi gửi", h.Exists(w, id + "-answer-1") && string.Join(",", q.AnswerLabels(1)) == "Kiểm thử");
    }

    static void Scroller(CheckHarness h)
    {
        var probe = (ScrollerProbe)h.Open(app => app.New(new ScrollerProbe())).Root!;
        var w = probe.Window!;
        var s = probe.Scroller;
        string viewKey = "gotx-message-scroller-" + s.Id.Value, jump = viewKey + "-jump";
        var view = h.Box(w, viewKey);
        float Distance() => w.AnchorBounds(viewKey + "-end")!.Value.Bottom - w.AnchorBounds(viewKey)!.Value.Bottom;
        float Max() => w.GetScroll(viewKey)!.Value.Max.Y;
        h.Check($"message-scroller: nội dung dài hơn khung (cuộn được {Max():F0}px)", Max() > 200);
        h.Check($"message-scroller: mở ra ở tin mới nhất ({Distance():F0})", Distance() <= 24 && s.IsAtEnd && !h.Exists(w, jump));

        h.Scroll(w, CheckHarness.Center(view), -500);
        h.Frames(w);
        h.Check($"message-scroller: lăn lên thì rời đáy và hiện nút ({Distance():F0})", Distance() > 200 && !s.IsAtEnd && h.Exists(w, jump));

        h.Click(w, jump);
        h.Wait(w, 0.8);
        h.Check($"message-scroller: bấm nút thì về tin mới nhất và ẩn nút ({Distance():F0})", Distance() <= 24 && s.IsAtEnd && !h.Exists(w, jump));
        h.Check($"message-scroller: AtEndChanged false→true ({probe.AtEndLog})", probe.AtEndLog == "False,True");

        // Đang ở đáy mà có tin mới → nội dung dài ra và khung nhìn tự cuộn theo, không hiện nút.
        float before = Max();
        s.Append(ChatSamples.Reply("Tin mới đến khi đang ở đáy, đủ cao để vượt ngưỡng 24px."));
        s.Append(ChatSamples.User("Thêm một tin nữa"));
        h.Wait(w, 0.8);
        h.Check($"message-scroller: tin mới làm nội dung dài ra ({before:F0} → {Max():F0})", Max() > before + 24);
        h.Check($"message-scroller: đang ở đáy thì bám theo tin mới ({Distance():F0})", Distance() <= 24 && s.IsAtEnd && !h.Exists(w, jump));

        // Đã lăn lên xem tin cũ thì tin mới không kéo khung nhìn đi
        h.Scroll(w, CheckHarness.Center(view), -500);
        h.Frames(w);
        float held = w.GetScroll(viewKey)!.Value.Offset.Y;
        s.Append(ChatSamples.User("Tin đến khi đang xem tin cũ"));
        h.Wait(w, 0.8);
        h.Check($"message-scroller: đang xem tin cũ thì tin mới không làm cuộn ({held:F0} → {w.GetScroll(viewKey)!.Value.Offset.Y:F0})",
            MathF.Abs(w.GetScroll(viewKey)!.Value.Offset.Y - held) < 1 && !s.IsAtEnd && h.Exists(w, jump));
    }
}
