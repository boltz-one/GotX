using GotX;
using GotX.Components;

/// <summary>Trang gallery nhóm Chat: một đoạn hội thoại mẫu trong MessageScroller, rồi từng component riêng lẻ.</summary>
public sealed class ChatPage : View
{
    MessageScroller _thread = null!;
    CodeBlock _snippet = null!, _errorLog = null!;
    Trace _traceOpen = null!, _traceClosed = null!;
    Questionnaire _survey = null!;
    readonly List<Attachment> _files = new();
    Attachment _upload = null!, _failed = null!, _idle = null!, _vertical = null!, _photo = null!, _photoCard = null!;
    string _removed = "";

    protected override void OnCreated()
    {
        // Mỗi View chỉ được nhúng một chỗ, nên hội thoại có code/trace riêng, khác với các mục bên dưới.
        var threadCode = App.New(new CodeBlock { Title = "fib.cs", Language = "C#", Code = ChatSamples.Fib, Highlighted = new HashSet<int> { 4 } });
        var threadTrace = App.New(new Trace { Title = "Đã suy nghĩ 8 giây", Steps = ChatSamples.Steps });
        _thread = App.New(new MessageScroller { Class = "h-[380px] rounded-xl border border-border bg-background" });
        _thread.SetItems(ChatSamples.Thread(threadCode, threadTrace));

        _snippet = App.New(new CodeBlock { Language = "Python", Code = ChatSamples.Py, StartLine = 41, Small = true });
        _errorLog = App.New(new CodeBlock { Title = "build.log", Variant = CodeBlockVariant.Destructive, Small = true, LineNumbers = false, Code = ChatSamples.Log });
        _traceOpen = App.New(new Trace { Title = "Đã suy nghĩ 8 giây", Steps = ChatSamples.Steps, Open = true });
        _traceClosed = App.New(new Trace { Title = "Đang tìm trong mã nguồn", Steps = ChatSamples.RunningSteps, Open = true });
        _survey = App.New(new Questionnaire { Questions = ChatSamples.Questions });

        foreach (var (name, bytes) in new[] { ("bao-cao-quy-3.pdf", 2_450_000L), ("du-lieu.csv", 18_400L) })
        {
            var a = App.New(new Attachment { Name = name, Bytes = bytes });
            App.Subscribe<Attachment.RemoveRequested>(a, e => { _files.Remove(a); _removed = e.Name; Notify(); });
            _files.Add(a);
        }
        _upload = App.New(new Attachment { Name = "anh-chup-man-hinh.png", State = AttachmentState.Uploading, Size = AttachmentSize.Sm, Preview = DemoImages.Landscape });
        _photo = App.New(new Attachment { Name = "phong-canh.png", Bytes = 1_240_000, Preview = DemoImages.Landscape });
        _photoCard = App.New(new Attachment { Name = "chan-dung.png", Bytes = 384_000, Preview = DemoImages.Portrait, Orientation = AttachmentOrientation.Vertical });
        _failed = App.New(new Attachment { Name = "video-demo.mp4", State = AttachmentState.Error, Size = AttachmentSize.Sm });
        _idle = App.New(new Attachment { Name = "ghi-chu.txt", Bytes = 912, State = AttachmentState.Idle, Size = AttachmentSize.Xs });
        _vertical = App.New(new Attachment { Name = "thiet-ke.fig", Bytes = 5_800_000, Orientation = AttachmentOrientation.Vertical });
    }

    public override Div Render(Window w)
    {
        var thread = Demo.Section(w, "Hội thoại", "MessageScroller + Message + Bubble + Marker + CodeBlock + Trace. \"Thêm tin\" nối tin mới; đang ở đáy thì tự cuộn theo.")
            .Child(w.View(_thread))
            .Child(Demo.Row(w).Child(Ui.Button(w, "Thêm tin", ButtonVariant.Outline, ButtonSize.Sm)
                .OnClick(this, static (p, _) => p._thread.Append(ChatSamples.Reply($"Tin mới số {p._thread.Items.Count + 1}.")))));

        var bubbles = Ui.BubbleGroup(w);
        foreach (var v in Enum.GetValues<BubbleVariant>())
            bubbles.Child(Ui.Bubble(w, $"Bong bóng {v}", v, v is BubbleVariant.Default or BubbleVariant.Tinted ? ChatAlign.End : ChatAlign.Start));
        bubbles.Child(Ui.Bubble(w, BubbleVariant.Secondary).Class("mb-3")
            .Child(Ui.BubbleContent(w, BubbleVariant.Secondary).Child("Có reaction ở mép dưới"))
            .Child(Ui.BubbleReactions(w).Child(Icon.ThumbsUp(w).Class("size-3")).Child("2")));
        var bubble = Demo.Section(w, "Bubble", "7 variant; Default và Tinted canh phải (tin của người dùng).").Child(bubbles);

        var message = Demo.Section(w, "Message", "Avatar, header, nội dung, footer với nút hành động.")
            .Child(Ui.MessageGroup(w)
                .Child(Ui.Message(w, ChatAlign.Start, Ui.MessageContent(w)
                        .Child(Ui.MessageHeader(w).Child("Trợ lý · 09:41"))
                        .Child(Ui.Bubble(w, "Mình đã đọc xong hai tệp bạn gửi.", BubbleVariant.Muted))
                        .Child(ChatSamples.Actions(w)),
                    Ui.MessageAvatar(w, "AI", hasFooter: true)))
                .Child(Ui.Message(w, ChatAlign.End, Ui.MessageContent(w, ChatAlign.End)
                        .Child(Ui.MessageHeader(w).Child("Bạn"))
                        .Child(Ui.Bubble(w, "Tóm tắt giúp mình nhé.", BubbleVariant.Default, ChatAlign.End)),
                    Ui.MessageAvatar(w, DemoImages.Portrait, "NV"))));

        var marker = Demo.Section(w, "Marker")
            .Child(w.Div().Class("flex flex-col gap-4")
                .Child(Ui.Marker(w, "Đã chuyển sang chế độ suy luận", MarkerVariant.Default, Icon.Info(w)))
                .Child(Ui.Marker(w, "Hôm nay", MarkerVariant.Separator))
                .Child(Ui.Marker(w, "Phiên làm việc mới", MarkerVariant.Border)));

        var group = Ui.AttachmentGroup(w, "chat-page-attachments");
        foreach (var a in _files) group.Child(w.View(a));
        var attachment = Demo.Section(w, "Attachment", _removed.Length > 0 ? $"Đã gỡ: {_removed}" : "Bấm × để gỡ tệp khỏi hàng đầu tiên.")
            .Child(group)
            .Child(Demo.Row(w).Child(w.View(_upload)).Child(w.View(_failed)))
            .Child(Demo.Row(w).Child(w.View(_photo)).Child(w.View(_idle)))
            .Child(Demo.Row(w).Class("items-start").Child(w.View(_vertical)).Child(w.View(_photoCard)));

        var trace = Demo.Section(w, "Trace", "Bấm dòng tiêu đề để mở/đóng.").Child(w.View(_traceOpen)).Child(w.View(_traceClosed));
        var code = Demo.Section(w, "Code block", "Chữ mono, không tô màu theo token; dòng chú thích được làm mờ. Nút sao chép hiện dấu tích 2 giây; Tab vào vùng code rồi ←/→ để cuộn.")
            .Child(w.View(_snippet)).Child(w.View(_errorLog));
        var survey = Demo.Section(w, "Questionnaire", "Tab vào lựa chọn rồi dùng ↑/↓ để đổi, Space để chọn, Enter để sang câu sau.").Child(w.View(_survey));

        return Demo.Page(w)
            .Child(thread)
            .Child(Columns(w, Stack(w).Child(bubble).Child(marker), Stack(w).Child(message).Child(trace)))
            .Child(Columns(w, Stack(w).Child(attachment).Child(code), Stack(w).Child(survey)));
    }

    static Div Stack(Window w) => w.Div().Class("flex min-w-0 flex-col gap-4");

    static Div Columns(Window w, Div left, Div right) => w.Div().Class("grid grid-cols-2 gap-4").Child(left).Child(right);
}
