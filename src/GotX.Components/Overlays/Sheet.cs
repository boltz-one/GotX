namespace GotX.Components;

/// <summary>
/// Tấm modal bám một cạnh cửa sổ (phải/trái: cao hết cửa sổ, rộng 24rem (tối đa bằng cửa sổ); trên/dưới: rộng hết, cao theo nội dung).
/// Dùng cho bảng chỉnh sửa, bộ lọc… cần nhiều chỗ hơn Dialog mà vẫn giữ ngữ cảnh trang phía sau.
/// <code>
/// _sheet = App.New(new Sheet { Side = PanelSide.Left, Title = "Bộ lọc", Body = w =&gt; … });
/// button.OnClick(this, static (v, _) =&gt; v._sheet.Open(v._window));
/// </code>
/// </summary>
public sealed class Sheet : SidePanel
{
    // Viền chỉ ở cạnh hướng vào trong cửa sổ, như data-[side=…]:border-* của shadcn.
    static readonly string[] PanelClasses =
    [
        "relative flex flex-col gap-4 h-full w-96 max-w-full bg-popover text-sm text-popover-foreground shadow-lg border-l border-border",
        "relative flex flex-col gap-4 h-full w-96 max-w-full bg-popover text-sm text-popover-foreground shadow-lg border-r border-border",
        "relative flex flex-col gap-4 w-full bg-popover text-sm text-popover-foreground shadow-lg border-b border-border",
        "relative flex flex-col gap-4 w-full bg-popover text-sm text-popover-foreground shadow-lg border-t border-border",
    ];

    public bool ShowCloseButton { get; set; } = true;

    /// <summary>Vẽ tấm (được gọi trong lớp overlay).</summary>
    public override Div Render(Window w)
    {
        var panel = w.Div().Class(PanelClasses[(int)Side]).TakeFocus(ContentKey);
        if (PanelClass.Length > 0) panel.Class(PanelClass);
        if (Header(w, "flex flex-col gap-0.5 p-4") is { } header) panel.Child(header);
        BodyAndFooter(w, panel, Horizontal ? "px-4" : "flex-1 px-4", "flex flex-col gap-2 p-4");
        if (ShowCloseButton)
            panel.Child(OverlayParts.CloseButton(w, CloseKey, "absolute top-3 right-3").OnClick(this, static (s, _) => s.Close()));
        return panel;
    }
}
