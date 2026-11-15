namespace GotX.Components;

/// <summary>Kiểu khung của <see cref="Ui.Item(Window, ItemVariant, ItemSize)"/>.</summary>
public enum ItemVariant : byte { Default, Outline, Muted }

/// <summary>Mật độ của item (Default và Sm giống nhau như bản shadcn gốc; Xs gọn hơn, chữ mô tả nhỏ).</summary>
public enum ItemSize : byte { Default, Sm, Xs }

/// <summary>Kiểu ô media đầu item: Default trong suốt, Icon cho icon 16px, Image là ô vuông bo góc (avatar/ảnh thu nhỏ).</summary>
public enum ItemMediaVariant : byte { Default, Icon, Image }

public static partial class Ui
{
    const string ItemBase = "flex w-full flex-wrap items-center rounded-lg border text-sm transition-colors duration-100";

    static readonly string[] ItemVariantClasses = ["border-transparent", "border-border", "border-transparent bg-muted/50"];
    static readonly string[] ItemSizeClasses = ["gap-2.5 px-3 py-2.5", "gap-2.5 px-3 py-2.5", "gap-2 px-2.5 py-2"];

    // Ghép sẵn variant × size; group-data-[size=...] của shadcn được thay bằng tham số size ở từng phần con.
    static readonly string[] ItemClassTable = BuildItemClasses();

    static string[] BuildItemClasses()
    {
        var table = new string[ItemVariantClasses.Length * ItemSizeClasses.Length];
        for (int v = 0; v < ItemVariantClasses.Length; v++)
            for (int s = 0; s < ItemSizeClasses.Length; s++)
                table[v * ItemSizeClasses.Length + s] = $"{ItemBase} {ItemVariantClasses[v]} {ItemSizeClasses[s]}";
        return table;
    }

    static readonly string[] ItemImageClasses =
    [
        "flex shrink-0 items-center justify-center gap-2 size-10 overflow-hidden rounded-sm",
        "flex shrink-0 items-center justify-center gap-2 size-8 overflow-hidden rounded-sm",
        "flex shrink-0 items-center justify-center gap-2 size-6 overflow-hidden rounded-sm",
    ];

    /// <summary>
    /// Một dòng nội dung: Media → Content (flex-1) → Actions xếp ngang; Header/Footer chiếm trọn một hàng.
    /// Muốn bấm được thì nối <c>.OnClick(...)</c> và <c>.Class("cursor-pointer hover:bg-muted")</c>.
    /// </summary>
    public static Div Item(Window w, ItemVariant variant = ItemVariant.Default, ItemSize size = ItemSize.Default) =>
        w.Div().Class(ItemClassTable[(int)variant * ItemSizeClasses.Length + (int)size]);

    /// <summary>Danh sách item xếp dọc.</summary>
    public static Div ItemGroup(Window w, ItemSize size = ItemSize.Default) =>
        w.Div().Class(size switch { ItemSize.Sm => "flex w-full flex-col gap-2.5", ItemSize.Xs => "flex w-full flex-col gap-2", _ => "flex w-full flex-col gap-4" });

    /// <summary>Đường kẻ giữa các item trong nhóm.</summary>
    public static Div ItemSeparator(Window w) => Separator(w).Class("my-2");

    /// <summary>
    /// Ô media. <paramref name="alignTop"/> = true khi item có mô tả: media bám đỉnh thay vì căn giữa
    /// (thay cho <c>group-has-data-[slot=item-description]</c>).
    /// </summary>
    public static Div ItemMedia(Window w, ItemMediaVariant variant = ItemMediaVariant.Default, ItemSize size = ItemSize.Default, bool alignTop = false)
    {
        var media = w.Div().Class(variant == ItemMediaVariant.Image
            ? ItemImageClasses[(int)size]
            : "flex shrink-0 items-center justify-center gap-2");
        return alignTop ? media.Class("self-start translate-y-0.5") : media;
    }

    /// <summary>Cột tiêu đề + mô tả, chiếm phần rộng còn lại.</summary>
    public static Div ItemContent(Window w, ItemSize size = ItemSize.Default) =>
        w.Div().Class(size == ItemSize.Xs ? "flex flex-1 flex-col gap-0" : "flex flex-1 flex-col gap-1");

    /// <summary>Tiêu đề (một dòng; GotX chưa có line-clamp nên dùng truncate).</summary>
    public static Div ItemTitle(Window w, string text) =>
        w.Div().Class("flex items-center gap-2 text-sm leading-snug font-medium").Child(w.Div().Class("truncate").Child(text));

    /// <summary>Mô tả (chữ tự xuống dòng; không giới hạn 2 dòng vì GotX chưa có line-clamp).</summary>
    public static Div ItemDescription(Window w, string text, ItemSize size = ItemSize.Default) =>
        w.Div().Class(size == ItemSize.Xs
            ? "text-left text-xs leading-normal font-normal text-muted-foreground"
            : "text-left text-sm leading-normal font-normal text-muted-foreground").Child(text);

    /// <summary>Nhóm nút ở cuối item.</summary>
    public static Div ItemActions(Window w) => w.Div().Class("flex items-center gap-2");

    /// <summary>Hàng đầu item (trọn chiều ngang, hai đầu dàn ra).</summary>
    public static Div ItemHeader(Window w) => w.Div().Class("flex basis-full items-center justify-between gap-2");

    /// <summary>Hàng cuối item (trọn chiều ngang, hai đầu dàn ra).</summary>
    public static Div ItemFooter(Window w) => w.Div().Class("flex basis-full items-center justify-between gap-2");
}
