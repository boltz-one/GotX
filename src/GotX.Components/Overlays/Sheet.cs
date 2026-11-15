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
    // Tấm theo cạnh (ComponentStyleSheet.SheetPanel): viền chỉ ở cạnh hướng vào trong cửa sổ, như data-[side=…]:border-*.

    public bool ShowCloseButton { get; set; } = true;

    /// <summary>Vẽ tấm (được gọi trong lớp overlay).</summary>
    public override Div Render(Window w)
    {
        var s = StyleSheet;
        using var _ = ComponentStyles.Use(s); // Body/Footer và nút × theo cùng style
        var panel = w.Div().Class(s.SheetPanel[(int)Side]).TakeFocus(ContentKey);
        if (PanelClass.Length > 0) panel.Class(PanelClass);
        if (Header(w, s.SheetHeader) is { } header) panel.Child(header);
        BodyAndFooter(w, panel, Horizontal ? "px-4" : "flex-1 px-4", s.SheetFooter);
        if (ShowCloseButton)
            panel.Child(OverlayParts.CloseButton(w, CloseKey, s.SheetClosePosition).OnClick(this, static (p, _) => p.Close()));
        return panel;
    }
}
