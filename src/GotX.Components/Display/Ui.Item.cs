namespace GotX.Components;

/// <summary>Kiểu khung của <see cref="Ui.Item(Window, ItemVariant, ItemSize)"/>.</summary>
public enum ItemVariant : byte { Default, Outline, Muted }

/// <summary>Mật độ của item (Default và Sm giống nhau như bản shadcn gốc; Xs gọn hơn, chữ mô tả nhỏ).</summary>
public enum ItemSize : byte { Default, Sm, Xs }

/// <summary>Kiểu ô media đầu item: Default trong suốt, Icon cho icon 16px, Image là ô vuông bo góc (avatar/ảnh thu nhỏ).</summary>
public enum ItemMediaVariant : byte { Default, Icon, Image }

public static partial class Ui
{
    /// <summary>
    /// Một dòng nội dung: Media → Content (flex-1) → Actions xếp ngang; Header/Footer chiếm trọn một hàng.
    /// Muốn bấm được thì nối <c>.OnClick(...)</c> và <c>.Class("cursor-pointer hover:bg-muted")</c>.
    /// </summary>
    public static Div Item(Window w, ItemVariant variant = ItemVariant.Default, ItemSize size = ItemSize.Default) =>
        w.Div().Class(ComponentStyles.S.ItemTable[(int)variant * ComponentStyles.S.ItemSizes.Length + (int)size]);

    /// <summary>Danh sách item xếp dọc.</summary>
    public static Div ItemGroup(Window w, ItemSize size = ItemSize.Default) =>
        w.Div().Class(ComponentStyles.S.ItemGroup[(int)size]);

    /// <summary>Đường kẻ giữa các item trong nhóm.</summary>
    public static Div ItemSeparator(Window w) => Separator(w).Class("my-2");

    /// <summary>
    /// Ô media. <paramref name="alignTop"/> = true khi item có mô tả: media bám đỉnh thay vì căn giữa
    /// (thay cho <c>group-has-data-[slot=item-description]</c>).
    /// </summary>
    public static Div ItemMedia(Window w, ItemMediaVariant variant = ItemMediaVariant.Default, ItemSize size = ItemSize.Default, bool alignTop = false)
    {
        var s = ComponentStyles.S;
        var media = w.Div().Class(variant == ItemMediaVariant.Image ? s.ItemMediaImage[(int)size] : s.ItemMedia);
        return alignTop ? media.Class(s.ItemMediaTop) : media;
    }

    /// <summary>Cột tiêu đề + mô tả, chiếm phần rộng còn lại.</summary>
    public static Div ItemContent(Window w, ItemSize size = ItemSize.Default) =>
        w.Div().Class(ComponentStyles.S.ItemContent[(int)size]);

    /// <summary>Tiêu đề (một dòng; GotX chưa có line-clamp nên dùng truncate).</summary>
    public static Div ItemTitle(Window w, string text) =>
        w.Div().Class(ComponentStyles.S.ItemTitle).Child(w.Div().Class(ComponentStyles.S.ItemTitleText).Child(text));

    /// <summary>Mô tả (chữ tự xuống dòng; không giới hạn 2 dòng vì GotX chưa có line-clamp).</summary>
    public static Div ItemDescription(Window w, string text, ItemSize size = ItemSize.Default) =>
        w.Div().Class(ComponentStyles.S.ItemDescription[(int)size]).Child(text);


    /// <summary>Nhóm nút ở cuối item.</summary>
    public static Div ItemActions(Window w) => w.Div().Class("flex items-center gap-2");

    /// <summary>Hàng đầu item (trọn chiều ngang, hai đầu dàn ra).</summary>
    public static Div ItemHeader(Window w) => w.Div().Class("flex basis-full items-center justify-between gap-2");

    /// <summary>Hàng cuối item (trọn chiều ngang, hai đầu dàn ra).</summary>
    public static Div ItemFooter(Window w) => w.Div().Class("flex basis-full items-center justify-between gap-2");
}
