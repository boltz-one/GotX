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
        w.Div().Class(ComponentStyles.S.Empty);

    /// <summary>Nhóm media + tiêu đề + mô tả.</summary>
    public static Div EmptyHeader(Window w) => w.Div().Class(ComponentStyles.S.EmptyHeader);

    /// <summary>Khung chứa icon/avatar phía trên tiêu đề.</summary>
    public static Div EmptyMedia(Window w, EmptyMediaVariant variant = EmptyMediaVariant.Default) =>
        w.Div().Class(ComponentStyles.S.EmptyMedia[(int)variant]);

    /// <summary>Tiêu đề.</summary>
    public static Div EmptyTitle(Window w, string text) => w.Div().Class(ComponentStyles.S.EmptyTitle).Child(text);

    /// <summary>Mô tả.</summary>
    public static Div EmptyDescription(Window w, string text) => w.Div().Class(ComponentStyles.S.EmptyDescription).Child(text);

    /// <summary>Vùng hành động (nút, ô nhập) dưới phần đầu.</summary>
    public static Div EmptyContent(Window w) => w.Div().Class(ComponentStyles.S.EmptyContent);
}
