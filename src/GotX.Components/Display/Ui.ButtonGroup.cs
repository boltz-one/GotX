namespace GotX.Components;

public static partial class Ui
{
    /// <summary>
    /// Nhóm nút dính liền. shadcn bo góc phần tử đầu/cuối bằng selector <c>[&amp;&gt;[data-slot]~[data-slot]]</c> mà GotX
    /// không có, nên hàm nhận sẵn danh sách con và tự gỡ bo góc/viền trùng theo vị trí. Class gắn thêm không có
    /// variant trạng thái nên không làm mất <c>hover:</c> của nút.
    /// </summary>
    public static Div ButtonGroup(Window w, SeparatorOrientation orientation, params ReadOnlySpan<Div> items)
    {
        bool h = orientation == SeparatorOrientation.Horizontal;
        var group = w.Div().Class(h ? "flex items-stretch" : "flex flex-col items-stretch");
        for (int i = 0; i < items.Length; i++)
        {
            bool first = i == 0, last = i == items.Length - 1;
            string cls = (first, last) switch
            {
                (true, true) => "",
                (true, false) => h ? "rounded-r-none" : "rounded-b-none",
                (false, true) => h ? "rounded-l-none border-l-0" : "rounded-t-none border-t-0",
                _ => h ? "rounded-none border-l-0" : "rounded-none border-t-0",
            };
            group.Child(cls.Length == 0 ? items[i] : items[i].Class(cls));
        }
        return group;
    }

    /// <summary>Nhóm nút ngang (trường hợp thường gặp).</summary>
    public static Div ButtonGroup(Window w, params ReadOnlySpan<Div> items) => ButtonGroup(w, SeparatorOrientation.Horizontal, items);

    /// <summary>Ô chữ tĩnh trong nhóm nút (tiền tố, nhãn).</summary>
    public static Div ButtonGroupText(Window w, string text) =>
        w.Div().Class("flex items-center gap-2 rounded-lg border border-border bg-muted px-2.5 text-sm font-medium").Child(text);

    /// <summary>
    /// Vạch ngăn giữa các nút có nền liền (variant Default/Secondary, vốn không có viền). Mặc định dọc – dùng trong nhóm ngang.
    /// </summary>
    public static Div ButtonGroupSeparator(Window w, SeparatorOrientation orientation = SeparatorOrientation.Vertical) =>
        w.Div().Class(orientation == SeparatorOrientation.Vertical
            ? "relative shrink-0 self-stretch bg-input w-px my-px"
            : "relative shrink-0 self-stretch bg-input h-px mx-px");
}
