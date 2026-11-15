namespace GotX.Components;

/// <summary>Phía đặt một tin nhắn: Start = bên trái (trợ lý, người khác), End = bên phải (chính người dùng).</summary>
public enum ChatAlign : byte { Start, End }

/// <summary>Kiểu màu của bong bóng chat (theo bubble.tsx).</summary>
public enum BubbleVariant : byte { Default, Secondary, Muted, Tinted, Outline, Ghost, Destructive }

/// <summary>Cạnh mà dải reaction bám vào bong bóng.</summary>
public enum BubbleReactionSide : byte { Top, Bottom }

public static partial class Ui
{
    // shadcn tô màu phần nội dung qua selector *:data-[slot=bubble-content] của khung bên ngoài; GotX không có
    // selector con nên variant được truyền thẳng vào BubbleContent và tra bảng ghép sẵn của style (BubbleContentTable).

    /// <summary>Cột các bong bóng liên tiếp của cùng một người gửi.</summary>
    public static Div BubbleGroup(Window w) => w.Div().Class("flex min-w-0 flex-col gap-2");

    /// <summary>
    /// Khung bong bóng (chưa có nội dung): thêm <see cref="BubbleContent"/> và tuỳ chọn <see cref="BubbleReactions"/>.
    /// Vì GotX chưa có <c>w-fit</c>, bong bóng co theo nội dung nhờ <c>self-start/self-end</c>, nên phải nằm trong
    /// một cột flex (<see cref="BubbleGroup"/>, <see cref="MessageContent"/>).
    /// </summary>
    public static Div Bubble(Window w, BubbleVariant variant = BubbleVariant.Default, ChatAlign align = ChatAlign.Start) =>
        w.Div().Class(variant == BubbleVariant.Ghost
            ? (align == ChatAlign.End ? "relative flex min-w-0 flex-col gap-1 self-end max-w-full" : "relative flex min-w-0 flex-col gap-1 self-start max-w-full")
            : (align == ChatAlign.End ? "relative flex min-w-0 flex-col gap-1 self-end max-w-[80%]" : "relative flex min-w-0 flex-col gap-1 self-start max-w-[80%]"));

    /// <summary>Phần nền màu chứa chữ của bong bóng; <paramref name="variant"/> phải trùng với khung bên ngoài.</summary>
    public static Div BubbleContent(Window w, BubbleVariant variant = BubbleVariant.Default, ChatAlign align = ChatAlign.Start) =>
        w.Div().Class(ComponentStyles.S.BubbleContentTable[(int)variant * 2 + (align == ChatAlign.End ? 1 : 0)]);

    /// <summary>Lối tắt cho trường hợp phổ biến: bong bóng chỉ có một đoạn chữ.</summary>
    public static Div Bubble(Window w, string text, BubbleVariant variant = BubbleVariant.Default, ChatAlign align = ChatAlign.Start) =>
        Bubble(w, variant, align).Child(BubbleContent(w, variant, align).Child(text));

    /// <summary>
    /// Dải reaction (emoji, số đếm) nổi ở mép trên/dưới bong bóng. Là con <c>absolute</c> nên luôn vẽ trên nội dung
    /// (GotX không có z-index); vòng <c>ring-card</c> tách nó khỏi nền bong bóng như bản gốc.
    /// </summary>
    public static Div BubbleReactions(Window w, BubbleReactionSide side = BubbleReactionSide.Bottom, ChatAlign align = ChatAlign.End) =>
        w.Div().Class((side, align) switch
        {
            (BubbleReactionSide.Top, ChatAlign.Start) => ReactionsBase + " top-0 -translate-y-3/4 left-3",
            (BubbleReactionSide.Top, _) => ReactionsBase + " top-0 -translate-y-3/4 right-3",
            (_, ChatAlign.Start) => ReactionsBase + " bottom-0 translate-y-3/4 left-3",
            _ => ReactionsBase + " bottom-0 translate-y-3/4 right-3",
        });

    const string ReactionsBase =
        "absolute flex shrink-0 items-center justify-center gap-1 rounded-full bg-muted px-1.5 py-0.5 text-xs ring-2 ring-card";
}
