namespace GotX.Components;

/// <summary>Kiểu khung media của trạng thái rỗng: Default trong suốt, Icon có nền muted bo góc.</summary>
public enum EmptyMediaVariant : byte { Default, Icon }

public static partial class Ui
{
    /// <summary>
    /// Khối "chưa có dữ liệu", căn giữa mọi thứ. Như shadcn, chỉ đặt kiểu nét đứt chứ không có viền; muốn khung thì
    /// nối <c>.Class("border border-border")</c>. Bỏ <c>text-balance</c> (GotX chưa có).
    /// </summary>
    public static Div Empty(Window w) =>
        w.Div().Class("flex w-full min-w-0 flex-1 flex-col items-center justify-center gap-4 rounded-xl border-dashed p-6 text-center");

    /// <summary>Nhóm media + tiêu đề + mô tả.</summary>
    public static Div EmptyHeader(Window w) => w.Div().Class("flex max-w-sm flex-col items-center gap-2");

    /// <summary>Khung chứa icon/avatar phía trên tiêu đề.</summary>
    public static Div EmptyMedia(Window w, EmptyMediaVariant variant = EmptyMediaVariant.Default) =>
        w.Div().Class(variant == EmptyMediaVariant.Icon
            ? "mb-2 flex size-8 shrink-0 items-center justify-center rounded-lg bg-muted text-foreground"
            : "mb-2 flex shrink-0 items-center justify-center");

    /// <summary>Tiêu đề.</summary>
    public static Div EmptyTitle(Window w, string text) => w.Div().Class("text-sm font-medium tracking-tight").Child(text);

    /// <summary>Mô tả.</summary>
    public static Div EmptyDescription(Window w, string text) => w.Div().Class("text-sm leading-relaxed text-muted-foreground").Child(text);

    /// <summary>Vùng hành động (nút, ô nhập) dưới phần đầu.</summary>
    public static Div EmptyContent(Window w) => w.Div().Class("flex w-full max-w-sm min-w-0 flex-col items-center gap-2.5 text-sm");
}
