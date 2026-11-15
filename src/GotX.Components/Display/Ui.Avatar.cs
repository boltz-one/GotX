namespace GotX.Components;

/// <summary>Kích thước avatar: Sm 24px, Default 32px, Lg 40px.</summary>
public enum AvatarSize : byte { Default, Sm, Lg }

public static partial class Ui
{
    // Không có group-data-[size] nên kích thước được truyền tường minh cho từng phần (avatar, badge, count) và tra
    // bảng dựng sẵn.
    static readonly string[] AvatarRoot =
    [
        "relative flex size-8 shrink-0 rounded-full",
        "relative flex size-6 shrink-0 rounded-full",
        "relative flex size-10 shrink-0 rounded-full",
    ];

    static readonly string[] AvatarFallbackClasses =
    [
        "flex size-full items-center justify-center rounded-full bg-muted text-sm text-muted-foreground",
        "flex size-full items-center justify-center rounded-full bg-muted text-xs text-muted-foreground",
        "flex size-full items-center justify-center rounded-full bg-muted text-sm text-muted-foreground",
    ];

    // after:border của shadcn → một lớp absolute phủ lên (không có mix-blend, nên dùng màu border bán trong suốt sẵn có).
    const string AvatarRing = "absolute inset-0 rounded-full border border-border";

    static readonly string[] AvatarBadgeClasses =
    [
        "absolute right-0 bottom-0 inline-flex size-2.5 items-center justify-center rounded-full bg-primary text-primary-foreground ring-2 ring-background",
        "absolute right-0 bottom-0 inline-flex size-2 items-center justify-center rounded-full bg-primary text-primary-foreground ring-2 ring-background",
        "absolute right-0 bottom-0 inline-flex size-3 items-center justify-center rounded-full bg-primary text-primary-foreground ring-2 ring-background",
    ];

    static readonly string[] AvatarCountClasses =
    [
        "relative flex size-8 shrink-0 items-center justify-center rounded-full bg-muted text-sm text-muted-foreground ring-2 ring-background",
        "relative flex size-6 shrink-0 items-center justify-center rounded-full bg-muted text-xs text-muted-foreground ring-2 ring-background",
        "relative flex size-10 shrink-0 items-center justify-center rounded-full bg-muted text-sm text-muted-foreground ring-2 ring-background",
    ];

    const string AvatarImageClass = "size-full rounded-full object-cover";

    /// <summary>
    /// Avatar tròn chỉ có chữ viết tắt (AvatarFallback). Thêm chấm trạng thái bằng
    /// <c>.Child(Ui.AvatarBadge(w, size))</c> – con thêm sau nằm trên viền.
    /// </summary>
    public static Div Avatar(Window w, string initials, AvatarSize size = AvatarSize.Default) => Avatar(w, null, initials, size);

    /// <summary>
    /// Avatar có ảnh (AvatarImage, cắt tròn, <c>object-cover</c>); <paramref name="image"/> null (chưa tải, tải lỗi)
    /// thì hiện <paramref name="initials"/> như AvatarFallback. Tạo <see cref="ImageSource"/> một lần rồi giữ lại.
    /// </summary>
    public static Div Avatar(Window w, ImageSource? image, string initials, AvatarSize size = AvatarSize.Default) =>
        w.Div().Class(AvatarRoot[(int)size])
            .Child(image is null
                ? w.Div().Class(AvatarFallbackClasses[(int)size]).Child(initials)
                : w.Image(image).Class(AvatarImageClass))
            .Child(w.Div().Class(AvatarRing));

    /// <summary>Chấm nhỏ ở góc dưới phải avatar (trạng thái online…); cùng <paramref name="size"/> với avatar.</summary>
    public static Div AvatarBadge(Window w, AvatarSize size = AvatarSize.Default) => w.Div().Class(AvatarBadgeClasses[(int)size]);

    /// <summary>Ô "+N" đứng cuối nhóm avatar.</summary>
    public static Div AvatarGroupCount(Window w, string text, AvatarSize size = AvatarSize.Default) =>
        w.Div().Class(AvatarCountClasses[(int)size]).Child(text);

    /// <summary>
    /// Nhóm avatar chồng lên nhau. GotX chưa có <c>-space-x-*</c>/<c>*:</c>, nên hàm nhận sẵn danh sách con và tự gán
    /// lề âm cho mọi phần tử trừ cái đầu, kèm vòng màu nền để tách các avatar.
    /// </summary>
    public static Div AvatarGroup(Window w, params ReadOnlySpan<Div> items)
    {
        var group = w.Div().Class("flex");
        for (int i = 0; i < items.Length; i++)
            group.Child(items[i].Class(i == 0 ? "ring-2 ring-background" : "-ml-2 ring-2 ring-background"));
        return group;
    }
}
