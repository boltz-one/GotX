namespace GotX.Components;

/// <summary>
/// Ngăn kéo: tấm modal trượt từ đáy (mặc định) hoặc một cạnh, bo góc phía trong và có thanh kéo nhỏ.
/// Khác <see cref="Sheet"/> ở hình thức (kiểu bottom-sheet trên di động). Chưa có cử chỉ vuốt để đóng:
/// Core chưa có quán tính/snap cho kéo, nên đóng bằng nút, Escape hoặc bấm ra ngoài. Nhận focus khi mở như Sheet.
/// </summary>
public sealed class Drawer : SidePanel
{
    /// <summary>Tạo drawer mặc định trượt từ đáy.</summary>
    public Drawer() => Side = PanelSide.Bottom;

    static readonly string[] PanelClasses =
    [
        "flex flex-row h-full w-96 max-w-full bg-popover text-sm text-popover-foreground rounded-l-xl border-l border-border",
        "flex flex-row h-full w-96 max-w-full bg-popover text-sm text-popover-foreground rounded-r-xl border-r border-border",
        "flex flex-col w-full bg-popover text-sm text-popover-foreground rounded-b-xl border-b border-border",
        "flex flex-col w-full bg-popover text-sm text-popover-foreground rounded-t-xl border-t border-border",
    ];

    /// <summary>Vẽ tấm (được gọi trong lớp overlay).</summary>
    public override Div Render(Window w)
    {
        var panel = w.Div().Class(PanelClasses[(int)Side]).TakeFocus(ContentKey);
        if (PanelClass.Length > 0) panel.Class(PanelClass);

        var content = w.Div().Class(Horizontal ? "flex flex-col pb-4" : "flex flex-1 flex-col pb-4");
        if (Header(w, Horizontal ? "flex flex-col gap-0.5 p-4 pb-0 text-center" : "flex flex-col gap-0.5 p-4 pb-0") is { } header)
            content.Child(header);
        BodyAndFooter(w, content, Horizontal ? "px-4 pt-4" : "flex-1 px-4 pt-4", "flex flex-col gap-2 px-4 pt-4");

        var handle = Horizontal
            ? w.Div().Class("flex justify-center py-4").Child(w.Div().Class("h-1.5 w-[100px] rounded-full bg-muted"))
            : w.Div().Class("flex items-center px-2").Child(w.Div().Class("h-[100px] w-1.5 rounded-full bg-muted"));

        // Thanh kéo luôn nằm ở cạnh hướng vào trong cửa sổ; Core chưa có *-reverse nên đổi thứ tự con.
        if (Side is PanelSide.Bottom or PanelSide.Right) panel.Child(handle).Child(content);
        else panel.Child(content).Child(handle);
        return panel;
    }
}
