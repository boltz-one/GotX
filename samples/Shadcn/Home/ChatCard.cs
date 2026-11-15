using GotX;
using GotX.Components;

/// <summary>
/// Thẻ chat (MessageScrollerDemo): ô soạn chỉ đọc hiện tin kế tiếp trong kịch bản, bấm gửi thì tin đó vào danh sách,
/// một giây sau câu trả lời lộ dần từng từ. Danh sách là view con <see cref="MessageScroller"/> nên lúc stream chỉ view đó
/// vẽ lại, không kéo theo cả trang.
/// </summary>
public sealed class ChatCard : View
{
    /// <summary>Người dùng vừa gửi tin thứ <paramref name="Turn"/> của kịch bản.</summary>
    public readonly record struct MessageSent(int Turn, string Text);

    /// <summary>Câu trả lời của lượt <paramref name="Turn"/> đã hiện xong.</summary>
    public readonly record struct ReplyFinished(int Turn);

    const double ThinkSeconds = 1.0, TokenSeconds = 0.02; // .sleep(1000) và delayMs: 20 của bản gốc

    static readonly string AttachTrigger = Ui.ButtonClasses(ButtonVariant.Outline, ButtonSize.IconSm);

    MessageScroller _scroller = null!;
    DropdownMenu _attach = null!;
    Tooltip _reset = null!;
    ChatScript.Stream? _stream;
    IDisposable? _timer;
    int _turn;
    bool _busy;

    /// <summary>Số lượt đã gửi.</summary>
    public int Turn => _turn;
    /// <summary>Đang chờ hoặc đang stream câu trả lời: nút gửi và nút reset bị khoá.</summary>
    public bool IsBusy => _busy;
    /// <summary>Tin sẽ được gửi ở lần bấm kế; null khi kịch bản đã hết.</summary>
    public string? NextMessage => _turn < ChatScript.Turns.Length ? ChatScript.Turns[_turn].User : null;
    public MessageScroller Scroller => _scroller;
    public DropdownMenu AttachMenu => _attach;
    public Tooltip ResetTooltip => _reset;

    protected override void OnCreated()
    {
        _scroller = App.New(new MessageScroller { Class = "h-full" });
        _attach = App.New(new DropdownMenu
        {
            Placement = Placement.TopStart,
            Trigger = static w => w.Div().Class(AttachTrigger).Child(Icon.Plus(w)),
            Items =
            [
                new MenuItem { Text = "Add Photos & Files", Icon = Icon.Paperclip, Key = "chat-attach-files" }, MenuItem.Separator(),
                new MenuItem { Text = "Create Image", Icon = Icon.Image },
                new MenuItem { Text = "Deep Research", Icon = HomeIcons.Telescope },
                new MenuItem { Text = "Web Search", Icon = Icon.Globe },
            ],
        });
        // Nút reset nằm trong view Tooltip: hàm dựng tạo một lần ở đây, đọc _busy mỗi lần Tooltip vẽ lại.
        _reset = App.New(new Tooltip
        {
            Text = "Reset",
            Trigger = w => Ui.Button(w, ButtonVariant.Outline, ButtonSize.Icon).Key("chat-reset").Disabled(_busy)
                .OnClick(this, static (c, _) => c.Reset()).Child(HomeIcons.RotateCw(w)),
        });
    }

    /// <summary>Gửi tin kế tiếp của kịch bản (không làm gì khi đang bận hoặc đã hết tin).</summary>
    public void Send()
    {
        if (_busy || NextMessage is null) return;
        var turn = ChatScript.Turns[_turn++];
        _scroller.Append(ChatScript.UserMessage(turn.User));
        SetBusy(true);
        Emit(new MessageSent(_turn, turn.User));
        _timer = App.SetTimeout(ThinkSeconds, () =>
        {
            var stream = _stream = new ChatScript.Stream(turn.Assistant);
            stream.Advance();
            _scroller.Append(ChatScript.AssistantMessage(stream));
            _timer = App.SetInterval(TokenSeconds, Tick);
        });
    }

    void Tick()
    {
        if (_stream is { } stream && stream.Advance())
        {
            // Tin dài thêm: chỉ danh sách vẽ lại; đang ở đáy thì bám theo, đã cuộn lên thì để yên như bản gốc.
            if (_scroller.IsAtEnd) _scroller.ScrollToEnd(); else _scroller.Notify();
            return;
        }
        _timer?.Dispose();
        _timer = null;
        _stream = null;
        SetBusy(false);
        Emit(new ReplyFinished(_turn));
    }

    /// <summary>Xoá cuộc trò chuyện, quay về tin đầu của kịch bản (bị khoá khi đang stream, như bản gốc).</summary>
    public void Reset()
    {
        if (_busy) return;
        _turn = 0;
        _scroller.SetItems([]);
        Notify();
    }

    void SetBusy(bool busy)
    {
        _busy = busy;
        Notify();
        _reset.Notify();
    }

    public override Div Render(Window w)
    {
        var body = Ui.CardContent(w).Class("flex min-h-0 flex-1 flex-col overflow-hidden p-0");
        if (_scroller.Items.Count == 0)
            body.Child(Ui.Empty(w).Class("p-12").Key("chat-empty")
                .Child(Ui.EmptyHeader(w)
                    .Child(Ui.EmptyMedia(w, EmptyMediaVariant.Icon).Class("size-10 rounded-xl").Child(Icon.MessageSquare(w).Class("size-5")))
                    .Child(Ui.EmptyTitle(w, "Morning, shadcn!").Class("text-lg"))
                    .Child(Ui.EmptyDescription(w, "What are we working on today? Press send to start a new conversation"))));
        else
            body.Child(w.View(_scroller).Class("min-h-0 flex-1"));

        // Core chưa có line-clamp-2: ô soạn hiện đủ tin kế tiếp (cao tối thiểu h-14 như bản gốc) thay vì cắt ở dòng hai.
        string? next = NextMessage;
        var draft = w.Div().Key("chat-draft").Class(next is null ? "text-muted-foreground" : _busy ? "opacity-60" : "")
            .Child(next ?? "No messages queued. Reset the conversation.");

        return HomeUi.Card(w).Class("h-140 gap-0").Key("chat-card")
            .Child(HomeUi.Header(w, hasAction: true).Class("gap-y-1 border-b border-border pb-5")
                .Child(Ui.CardTitle(w, "New Chat"))
                .Child(Ui.CardAction(w).Child(w.View(_reset)))
                .Child(Ui.CardDescription(w, "How can I help you today?")))
            .Child(body)
            .Child(HomeUi.Footer(w).Child(Ui.InputGroup(w, stacked: true).Key("chat-composer")
                .Child(w.Div().Class("min-h-14 w-full px-3 py-2.5 text-sm").Child(draft))
                .Child(Ui.InputGroupAddon(w, InputGroupAlign.BlockEnd).Class("pt-1")
                    .Child(w.View(_attach))
                    .Child(w.Div().Class("flex-1"))
                    .Child(Ui.Button(w, ButtonVariant.Default, ButtonSize.IconSm).Key("chat-send").Disabled(next is null || _busy)
                        .OnClick(this, static (c, _) => c.Send()).Child(Icon.ArrowUp(w))))));
    }
}
