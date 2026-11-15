using GotX;
using GotX.Components;

/// <summary>View thử nghiệm cho ChatChecks: bong bóng hai phía, message, tệp đính kèm, code, trace, bộ câu hỏi.</summary>
sealed class ChatProbe : View
{
    public Window? Window;
    public Attachment File = null!, Photo = null!;
    public CodeBlock Code = null!, Mono = null!, Wide = null!;
    public int PhotoRemoves;
    public Trace Trace = null!;
    public Questionnaire Survey = null!;
    public string Removed = "", Steps = "";
    public int RemoveEvents, TraceEvents;
    public IReadOnlyList<IReadOnlyList<int>>? Submitted;
    readonly List<Attachment> _files = new();

    protected override void OnCreated()
    {
        File = App.New(new Attachment { Name = "bao-cao.pdf", Bytes = 2_450_000 });
        _files.Add(File);
        _files.Add(App.New(new Attachment { Name = "du-lieu.csv", Bytes = 18_400 }));
        App.Subscribe<Attachment.RemoveRequested>(File, e => { RemoveEvents++; Removed = e.Name; _files.Remove(File); Notify(); });
        Code = App.New(new CodeBlock { Title = "fib.cs", Language = "C#", Code = ChatSamples.Fib });
        Photo = App.New(new Attachment { Name = "anh.png", Bytes = 1_240_000, Preview = DemoImages.Landscape });
        App.Subscribe<Attachment.RemoveRequested>(Photo, _ => PhotoRemoves++);
        Mono = App.New(new CodeBlock { Language = "txt", LineNumbers = false, Code = "iiiiiiiiiiiiiiiiiiii\nWWWWWWWWWWWWWWWWWWWW" });
        Wide = App.New(new CodeBlock { Language = "txt", LineNumbers = false, Code = new string('x', 160) });
        Trace = App.New(new Trace { Title = "Đã suy nghĩ 8 giây", Steps = ChatSamples.Steps });
        App.Subscribe<Trace.OpenChanged>(Trace, _ => TraceEvents++);
        Survey = App.New(new Questionnaire { Questions = ChatSamples.Questions });
        App.Subscribe<Questionnaire.StepChanged>(Survey, e => Steps += e.Step);
        App.Subscribe<Questionnaire.Submitted>(Survey, e => Submitted = e.Answers);
    }

    public override Div Render(Window w)
    {
        Window = w;
        var files = Ui.AttachmentGroup(w, "probe-files");
        foreach (var f in _files) files.Child(w.View(f));
        var left = w.Div().Class("flex flex-col gap-4")
            .Child(Ui.BubbleGroup(w).Key("probe-bubble-column")
                .Child(Ui.Bubble(w, "Xin chào", BubbleVariant.Default, ChatAlign.End).Key("probe-user-bubble"))
                .Child(Ui.Bubble(w, "Chào bạn, mình giúp gì được?", BubbleVariant.Muted).Key("probe-bot-bubble")))
            .Child(Ui.Message(w, ChatAlign.End,
                Ui.MessageContent(w, ChatAlign.End).Key("probe-message-content").Child(Ui.Bubble(w, "Tin của tôi", BubbleVariant.Default, ChatAlign.End)),
                Ui.MessageAvatar(w, DemoImages.Portrait, "NV").Key("probe-message-avatar")))
            .Child(files)
            .Child(w.View(Code))
            .Child(w.Div().Key("probe-photo").Child(w.View(Photo)))
            .Child(w.View(Mono))
            .Child(w.Div().Class("w-[380px]").Key("probe-wide").Child(w.View(Wide)));
        var right = w.Div().Class("flex flex-col gap-4")
            .Child(w.View(Trace))
            .Child(w.Div().Class("h-6 bg-muted").Key("probe-after-trace"))
            .Child(w.View(Survey));
        return w.Div().Class("size-full grid grid-cols-2 gap-6 p-6 bg-background text-foreground").Child(left).Child(right);
    }
}

/// <summary>MessageScroller cao 300px với đoạn hội thoại dài, ghi lại các AtEndChanged.</summary>
sealed class ScrollerProbe : View
{
    public Window? Window;
    public MessageScroller Scroller = null!;
    public string AtEndLog = "";

    protected override void OnCreated()
    {
        Scroller = App.New(new MessageScroller { Class = "h-[300px] border border-border rounded-xl" });
        var items = new List<Func<Window, Div>>();
        for (int i = 1; i <= 12; i++)
            items.Add(i % 2 == 0 ? ChatSamples.Reply($"Trả lời số {i}: nội dung đủ dài để chiếm một dòng.") : ChatSamples.User($"Câu hỏi số {i}"));
        Scroller.SetItems(items);
        App.Subscribe<MessageScroller.AtEndChanged>(Scroller, e => AtEndLog += (AtEndLog.Length > 0 ? "," : "") + e.AtEnd);
    }

    public override Div Render(Window w)
    {
        Window = w;
        return w.Div().Class("size-full flex flex-col p-8 bg-background text-foreground").Child(w.View(Scroller));
    }
}
