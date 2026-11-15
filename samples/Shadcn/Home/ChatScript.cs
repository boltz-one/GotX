using GotX;
using GotX.Components;

/// <summary>
/// Kịch bản của thẻ chat (message-scroller-demo.tsx): bản gốc không gọi model mà phát lại các lượt hỏi–đáp soạn sẵn,
/// mỗi lần bấm gửi là một lượt. Tách khỏi <see cref="ChatCard"/> để file view chỉ còn state và bố cục.
/// </summary>
static class ChatScript
{
    public sealed record Turn(string User, string Assistant);

    public static readonly Turn[] Turns =
    [
        new("I'm building a chat for our app and the scroll behavior is driving me nuts. Every time the AI streams a reply, the whole thread jumps around.",
            "That's the classic streaming scroll problem. Wrap your message list in `MessageScroller` and turn on `autoScroll` — the viewport pins to the bottom as tokens arrive, so users always see the latest text land in place.\n\nThe important part: it only auto-scrolls while the reader is already at the bottom. The moment they scroll up to read something earlier, auto-scroll backs off and their position is preserved. You get smooth streaming without fighting the user's intent."),
        new("Okay, but when someone sends a new message the view still feels jarring — like the whole conversation reloads from the top.",
            "MessageScrollerItem fixes that with turn anchoring. Set `scrollAnchor` on the turn that should settle near the top instead of blindly snapping to the document bottom.\n\nIt also leaves a small peek of the previous exchange visible above the anchor, so context isn't lost. The reply starts in view without that disorienting jump you get from a plain overflow container."),
        new("And if they've scrolled up to re-read an older answer? I don't want to yank them back down.",
            "You won't. Auto-scroll only runs when the viewport is already pinned to the bottom, so scrolling up is a deliberate opt-out — their place in the thread stays put even as new tokens keep arriving below.\n\nWhen there is content they haven't seen yet, `MessageScrollerButton` appears at the bottom of the viewport. One tap jumps them back to the newest message and re-engages auto-scroll. Same pattern as Slack or iMessage: quiet when you're caught up, helpful when you're not."),
        new("Last one — does this work with assistive tech?",
            "`MessageScrollerContent` sets `role=\"log\"` and `aria-relevant=\"additions\"` by default, so screen readers announce new messages as they stream in.\n\nThe scroll button is a real `<button>` with an sr-only label, and it's removed from the tab order when you're already at the bottom — no ghost focus stops."),
    ];

    /// <summary>Câu trả lời đang "stream": lộ dần từng từ như transport giả của bản gốc (20 ms mỗi mẩu).</summary>
    public sealed class Stream(string full)
    {
        int _length;

        public string Visible { get; private set; } = "";
        public bool Done => _length >= full.Length;

        /// <summary>Lộ thêm một từ; trả về false khi đã hết.</summary>
        public bool Advance()
        {
            if (Done) return false;
            int next = full.IndexOf(' ', Math.Min(_length + 1, full.Length));
            _length = next < 0 ? full.Length : next;
            Visible = full[.._length];
            return true;
        }
    }

    /// <summary>Tin của người dùng: bong bóng muted bên phải (userVariant = "muted").</summary>
    public static Func<Window, Div> UserMessage(string text) => w =>
        Ui.Message(w, ChatAlign.End, Ui.MessageContent(w, ChatAlign.End).Child(Ui.Bubble(w, text, BubbleVariant.Muted, ChatAlign.End)));

    /// <summary>Tin của trợ lý: bong bóng ghost, mỗi đoạn (cách nhau một dòng trống) là một khối như <c>space-y-2</c>.</summary>
    public static Func<Window, Div> AssistantMessage(Stream reply) => w =>
    {
        var content = Ui.BubbleContent(w, BubbleVariant.Ghost).Class("flex flex-col gap-2");
        foreach (string paragraph in reply.Visible.Split("\n\n", StringSplitOptions.RemoveEmptyEntries))
            content.Child(w.Div().Child(paragraph));
        return Ui.Message(w, ChatAlign.Start, Ui.MessageContent(w).Child(Ui.Bubble(w, BubbleVariant.Ghost).Child(content)));
    };
}
