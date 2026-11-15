using GotX;
using GotX.Components;

/// <summary>
/// Kiểm tra phần mới của nhóm Chat trên <see cref="ChatProbe"/>: bàn phím (nút gỡ tệp, trace, bộ câu hỏi, cuộn code),
/// ảnh xem trước / ảnh avatar và font mono của khối code.
/// </summary>
static class ChatKeyboardChecks
{
    public static void Run(CheckHarness h)
    {
        var saved = h.Size;
        h.Size = new Size(900, 1000); // probe cao hơn cửa sổ mặc định; pixel phải nằm trong canvas mới đo được
        try
        {
            Media(h);
            Code(h);
            TraceAndSurvey(h);
        }
        finally { h.Size = saved; }
    }

    static ChatProbe Open(CheckHarness h) => (ChatProbe)h.Open(app => app.New(new ChatProbe())).Root!;

    static void Media(CheckHarness h)
    {
        var probe = Open(h);
        var w = probe.Window!;
        var avatar = h.Pixel(w, CheckHarness.Center(h.Box(w, "probe-message-avatar")));
        h.Check($"message avatar: có ảnh thì vẽ ảnh thay chữ viết tắt {avatar}", avatar.R > 235 && avatar.B is > 200 and < 245);

        // Thẻ ngang cỡ Default: viền 1 + px-2.5, ô media size-10 → tâm ô cách mép trái 31px.
        var card = h.Box(w, "probe-photo");
        var preview = h.Pixel(w, new Point(card.X + 31, card.Y + card.Height / 2));
        h.Check($"attachment: Preview lấp ô media bằng ảnh (bầu trời {preview})", preview.B > preview.R + 30 && preview.B > 180);

        string remove = "gotx-attachment-remove-" + probe.Photo.Id.Value;
        var edge = new Point(card.X - 1, card.Y + card.Height / 2);
        h.Check("attachment: chưa focus thì thẻ không có vòng", !FormsKeyboardChecks.RingAt(h, w, edge));
        w.FocusKey(remove);
        h.Frames(w);
        h.Check("attachment: nút gỡ focus được; thẻ hiện vòng mảnh (focus-within:ring-1), nút có vòng focus riêng",
            w.IsFocused(remove) && FormsKeyboardChecks.RingAt(h, w, edge) && FormsKeyboardChecks.Ring(h, w, h.Box(w, remove)));
        h.Press(w, Key.Enter);
        h.Press(w, Key.Space);
        h.Check($"attachment: Enter và Space trên nút gỡ đều phát RemoveRequested ({probe.PhotoRemoves})", probe.PhotoRemoves == 2);
    }

    /// <summary>Bề rộng phần mực (pixel tối) trong dải ngang [y0, y1) của hộp.</summary>
    static int InkWidth(GotX.Raster.Canvas c, Bounds box, float y0, float y1)
    {
        int min = int.MaxValue, max = -1;
        for (int y = (int)y0; y < (int)y1; y++)
            for (int x = (int)box.X; x < (int)box.Right; x++)
            {
                uint v = c.Pixels[y * c.Width + x];
                if ((((v >> 16) & 255) + ((v >> 8) & 255) + (v & 255)) / 3 >= 140) continue;
                min = Math.Min(min, x); max = Math.Max(max, x);
            }
        return max < 0 ? 0 : max - min + 1;
    }

    static void Code(CheckHarness h)
    {
        var probe = Open(h);
        var w = probe.Window!;

        // Hai dòng 20 ký tự "i" và "W": font mono thì rộng như nhau, font thường thì dòng W rộng gấp mấy lần.
        var body = h.Box(w, "gotx-code-body-" + probe.Mono.Id.Value);
        var canvas = h.Render(w);
        float mid = body.Y + 8 + (body.Height - 8 - 12) / 2; // pt-2 … pb-3, hai dòng chia đôi phần giữa
        int narrow = InkWidth(canvas, body, body.Y + 8, mid), wide = InkWidth(canvas, body, mid, body.Bottom - 12);
        h.Check($"code-block: chữ mono – 20 chữ i rộng {narrow}px, 20 chữ W rộng {wide}px", narrow > 100 && Math.Abs(wide - narrow) <= 10);

        string key = "gotx-code-body-" + probe.Wide.Id.Value;
        var block = h.Box(w, "probe-wide");
        float X() => w.GetScroll(key)!.Value.Offset.X;
        float max = w.GetScroll(key)!.Value.Max.X;
        h.Check($"code-block: dòng 160 ký tự tràn ngang (cuộn được {max:F0}px), chưa focus thì không có vòng", max > 200 && !FormsKeyboardChecks.Ring(h, w, block));
        w.FocusKey(key);
        h.Frames(w);
        h.Check("code-block: vùng code focus được, cả khối hiện vòng focus", w.IsFocused(key) && FormsKeyboardChecks.Ring(h, w, block));
        h.Press(w, Key.Right);
        h.Wait(w, 0.5);
        float once = X();
        h.Press(w, Key.Right);
        h.Wait(w, 0.5);
        h.Check($"code-block: → cuộn sang phải từng bước 40px ({once:F0} → {X():F0})", MathF.Abs(once - 40) < 1 && MathF.Abs(X() - 80) < 1);
        h.Press(w, Key.Left);
        h.Wait(w, 0.5);
        h.Check($"code-block: ← cuộn lại ({X():F0})", MathF.Abs(X() - 40) < 1);
        h.Press(w, Key.End);
        h.Wait(w, 0.8);
        float end = X();
        h.Press(w, Key.Home);
        h.Wait(w, 0.8);
        h.Check($"code-block: End cuộn tới cuối dòng, Home về đầu ({end:F0} / {max:F0} → {X():F0})", MathF.Abs(end - max) < 1 && X() < 1);

        // Khối không tràn ngang thì không giữ phím: mũi tên nổi lên cha thay vì bị nuốt.
        string still = "gotx-code-body-" + probe.Mono.Id.Value;
        w.FocusKey(still);
        h.Frames(w);
        h.Press(w, Key.Right);
        h.Check("code-block: không tràn thì → không cuộn gì", w.IsFocused(still) && w.GetScroll(still)!.Value.Offset.X == 0);
    }

    static void TraceAndSurvey(CheckHarness h)
    {
        var probe = Open(h);
        var w = probe.Window!;
        string trigger = "gotx-trace-trigger-" + probe.Trace.Id.Value;
        w.FocusKey(trigger);
        h.Frames(w);
        h.Check("trace: dòng tiêu đề focus được và có vòng focus", w.IsFocused(trigger) && FormsKeyboardChecks.Ring(h, w, h.Box(w, trigger)));
        h.Press(w, Key.Enter);
        h.Check("trace: Enter mở danh sách bước", probe.Trace.Open && w.IsFocused(trigger));
        h.Press(w, Key.Space);
        h.Check($"trace: Space đóng lại ({probe.TraceEvents})", !probe.Trace.Open && probe.TraceEvents == 2);

        var q = probe.Survey;
        string id = "gotx-questionnaire-" + q.Id.Value;
        string Choice(int i) => $"{id}-choice-{i}";
        h.Press(w, Key.Tab);
        h.Check("questionnaire: Tab từ trace vào lựa chọn đầu, chưa chọn gì", w.IsFocused(Choice(0)) && q.Answers[0].Count == 0);
        h.Check("questionnaire: lựa chọn đang focus có vòng focus", FormsKeyboardChecks.Ring(h, w, h.Box(w, Choice(0))));
        h.Press(w, Key.Down);
        h.Check("questionnaire (chọn một): ↓ chuyển focus và chọn luôn", w.IsFocused(Choice(1)) && q.Answers[0].SequenceEqual([1]));
        h.Press(w, Key.End);
        h.Press(w, Key.Down);
        h.Check("questionnaire: End tới lựa chọn cuối, ↓ quay vòng về đầu", w.IsFocused(Choice(0)) && q.Answers[0].SequenceEqual([0]));
        h.Press(w, Key.Tab);
        h.Check("questionnaire (chọn một): Tab rời nhóm trong một bước, bỏ qua nút Quay lại đang khoá", w.IsFocused(id + "-skip"));
        h.Press(w, Key.Tab, Modifiers.Shift);
        h.Check("questionnaire: Shift+Tab vào lại thì focus về lựa chọn đang chọn (không phải lựa chọn cuối)", w.IsFocused(Choice(0)));
        h.Press(w, Key.Enter);
        h.Check($"questionnaire: Enter sang câu 2, focus ở lựa chọn đầu của câu mới ({probe.Steps})",
            q.Step == 1 && probe.Steps == "1" && w.IsFocused(Choice(0)) && q.Answers[0].SequenceEqual([0]) && q.Answers[1].Count == 0);

        h.Press(w, Key.Down);
        h.Check("questionnaire (chọn nhiều): ↓ chỉ chuyển focus", w.IsFocused(Choice(1)) && q.Answers[1].Count == 0);
        h.Press(w, Key.Space);
        h.Press(w, Key.Down);
        h.Press(w, Key.Space);
        h.Check("questionnaire (chọn nhiều): Space bật từng lựa chọn", q.Answers[1].SequenceEqual([1, 2]));
        h.Press(w, Key.Space);
        h.Press(w, Key.Tab, Modifiers.Shift);
        h.Check("questionnaire (chọn nhiều): Space lần nữa bỏ chọn; mỗi checkbox là một điểm dừng Tab",
            q.Answers[1].SequenceEqual([1]) && w.IsFocused(Choice(1)));
        h.Press(w, Key.Enter);
        h.Check("questionnaire: đã có câu trả lời thì Enter không đổi lựa chọn, chỉ sang câu sau", q.Step == 2 && q.Answers[1].SequenceEqual([1]) && w.IsFocused(Choice(0)));
        h.Press(w, Key.Enter);
        var sent = probe.Submitted;
        h.Check("questionnaire: Enter ở câu cuối chưa trả lời thì chọn lựa chọn đang focus rồi gửi",
            q.IsSubmitted && sent is { Count: 3 } && sent[0].SequenceEqual([0]) && sent[1].SequenceEqual([1]) && sent[2].SequenceEqual([0]));
        h.Check("questionnaire: gửi xong các lựa chọn biến mất nên focus được bỏ", !w.HasElementFocus);
    }
}
