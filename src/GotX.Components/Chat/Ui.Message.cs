namespace GotX.Components;

public static partial class Ui
{
    /// <summary>Cột các tin nhắn của cả cuộc trò chuyện.</summary>
    public static Div MessageGroup(Window w) => w.Div().Class("flex min-w-0 flex-col gap-2");

    /// <summary>
    /// Một hàng tin nhắn: avatar + cột nội dung. Bản gốc đảo phía bằng <c>flex-row-reverse</c>, GotX chưa có
    /// <c>*-reverse</c> nên hàm tự xếp thứ tự con theo <paramref name="align"/> – vì thế nhận sẵn các phần.
    /// </summary>
    public static Div Message(Window w, ChatAlign align, Div content, Div? avatar = null)
    {
        var row = w.Div().Class(align == ChatAlign.End
            ? "relative flex w-full min-w-0 justify-end gap-2 text-sm"
            : "relative flex w-full min-w-0 gap-2 text-sm");
        if (align == ChatAlign.End) { row.Child(content); if (avatar is { } a) row.Child(a); }
        else { if (avatar is { } a) row.Child(a); row.Child(content); }
        return row;
    }

    /// <summary>
    /// Avatar tròn chỉ có chữ viết tắt. <paramref name="hasFooter"/>: tin có hàng footer thì avatar nhích lên để
    /// thẳng với bong bóng thay vì với footer (bản gốc dùng group-has-[footer]).
    /// </summary>
    public static Div MessageAvatar(Window w, string initials, bool hasFooter = false) => MessageAvatar(w, null, initials, hasFooter);

    /// <summary>Avatar có ảnh (cắt tròn); <paramref name="image"/> null thì hiện <paramref name="initials"/>.</summary>
    public static Div MessageAvatar(Window w, ImageSource? image, string initials, bool hasFooter = false)
    {
        var avatar = w.Div().Class(hasFooter
            ? "flex size-8 shrink-0 items-center justify-center self-end overflow-hidden rounded-full bg-muted text-xs font-medium -translate-y-8"
            : "flex size-8 shrink-0 items-center justify-center self-end overflow-hidden rounded-full bg-muted text-xs font-medium");
        return image is null ? avatar.Child(initials) : avatar.Child(w.Image(image).Class("size-full rounded-full object-cover"));
    }

    /// <summary>Cột nội dung (header, bong bóng, đính kèm, footer); phía End dồn mọi phần sang phải.</summary>
    public static Div MessageContent(Window w, ChatAlign align = ChatAlign.Start) =>
        w.Div().Class(align == ChatAlign.End
            ? "flex min-w-0 flex-1 flex-col items-end gap-2.5"
            : "flex min-w-0 flex-1 flex-col items-start gap-2.5");

    /// <summary>Dòng tên người gửi / thời gian phía trên bong bóng. <paramref name="ghost"/>: bỏ lề khi bong bóng kiểu ghost.</summary>
    public static Div MessageHeader(Window w, bool ghost = false) =>
        w.Div().Class(ghost
            ? "flex min-w-0 max-w-full items-center gap-1.5 text-xs font-medium text-muted-foreground"
            : "flex min-w-0 max-w-full items-center gap-1.5 px-3 text-xs font-medium text-muted-foreground");

    /// <summary>Hàng hành động dưới tin nhắn (sao chép, thích, thử lại…) – thường chứa các nút ghost cỡ icon.</summary>
    public static Div MessageFooter(Window w, bool ghost = false) =>
        w.Div().Class(ghost
            ? "flex min-w-0 max-w-full items-center gap-0.5 text-xs font-medium text-muted-foreground"
            : "flex min-w-0 max-w-full items-center gap-0.5 px-3 text-xs font-medium text-muted-foreground");
}
