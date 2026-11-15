namespace GotX.Components;

/// <summary>Trục cuộn của <see cref="Ui.ScrollArea"/>.</summary>
public enum ScrollAxis : byte { Vertical, Horizontal, Both }

/// <summary>
/// Vùng cuộn. Core đã tự vẽ thanh cuộn nổi và cuộn mượt cho <c>overflow-*-auto</c>, nên không cần dựng lại
/// viewport/scrollbar/thumb như Radix – chỉ còn lớp vỏ. Chưa kéo được thanh cuộn (Core chưa hỗ trợ), chỉ lăn chuột.
/// </summary>
public static partial class Ui
{
    // Thanh cuộn như ScrollArea của shadcn (Radix type="hover"): chỉ hiện khi con trỏ ở trên vùng cuộn, ẩn 600ms sau khi rời;
    // rãnh w-2.5 (viền trong 1px + đệm 1px) với con trượt bg-border bo tròn.
    const string ScrollbarClasses = " scrollbar-hover scrollbar-w-2.5 scrollbar-thumb-border";
    static readonly string[] ScrollAxisClasses =
        ["relative overflow-y-auto" + ScrollbarClasses, "relative overflow-x-auto" + ScrollbarClasses, "relative overflow-auto" + ScrollbarClasses];

    /// <summary>
    /// Vùng cuộn có <paramref name="key"/> bắt buộc: vị trí cuộn được giữ qua các frame theo key. Người gọi đặt kích thước
    /// (<c>.Class("h-72 w-48 rounded-md border border-border")</c>) và nối nội dung.
    /// </summary>
    public static Div ScrollArea(Window w, string key, ScrollAxis axis = ScrollAxis.Vertical) =>
        w.Div().Key(key).Class(ScrollAxisClasses[(int)axis]);
}
