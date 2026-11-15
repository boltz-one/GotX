namespace GotX.Components;

/// <summary>Cạnh cửa sổ mà Sheet / Drawer trượt ra.</summary>
public enum PanelSide : byte { Right, Left, Top, Bottom }

/// <summary>
/// Phần chung của <see cref="Sheet"/> và <see cref="Drawer"/>: tấm modal bám một cạnh cửa sổ, có tiêu đề, mô tả,
/// thân và chân. Hai lớp con chỉ khác cách vẽ tấm (viền, bo góc, thanh kéo), nên vòng đời mở/đóng nằm ở đây.
/// </summary>
public abstract class SidePanel : View
{
    // Bảng ghim lúc tạo nếu được tạo trong ComponentStyles.Use(...); không thì theo style chung của app.
    readonly ComponentStyleSheet? _pinned = ComponentStyles.Scoped;
    /// <summary>Bảng class đang dùng (ghim hoặc style chung).</summary>
    private protected ComponentStyleSheet StyleSheet => _pinned ?? ComponentStyles.S;

    readonly PopupSlot _slot = new();
    string? _contentKey, _closeKey;

    /// <summary>Tấm vừa mở / vừa đóng (kể cả do Escape hay bấm ra ngoài).</summary>
    public readonly record struct OpenChanged(bool Open);

    public PanelSide Side { get; set; } = PanelSide.Right;
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";

    /// <summary>Phần thân giữa tiêu đề và chân.</summary>
    public Func<Window, Div>? Body { get; set; }

    /// <summary>Hàng nút ở chân tấm.</summary>
    public Func<Window, Div>? Footer { get; set; }

    public bool DismissOnOutsideClick { get; set; } = true;

    /// <summary>Class thêm cho tấm (ví dụ đổi độ rộng).</summary>
    public string PanelClass { get; set; } = "";

    public bool IsOpen => _slot.IsOpen;

    /// <summary>Key của tấm – phần tử giữ focus ngay sau khi mở (Tab bị Core giữ trong lớp modal).</summary>
    public string ContentKey => _contentKey ??= "gotx-panel-" + Id.Value;

    /// <summary>Key của nút × (Sheet).</summary>
    public string CloseKey => _closeKey ??= "gotx-panel-close-" + Id.Value;

    /// <summary>Vị trí tấm ở frame gần nhất (logical px); rỗng khi đóng.</summary>
    public Bounds PanelBounds => _slot.Current?.ContentBounds ?? default;

    /// <summary>Mở tấm trên lớp overlay của <paramref name="window"/>.</summary>
    public void Open(Window window)
    {
        if (IsOpen) return;
        _slot.Open(window, this, new OverlayOptions
        {
            Modal = true, DismissOnOutsideClick = DismissOnOutsideClick, LayerClass = LayerFor(Side),
        }, () => Emit(new OpenChanged(_slot.IsOpen)));
    }

    /// <summary>Đóng tấm.</summary>
    public void Close() => _slot.Close();

    /// <summary>Bố cục lớp overlay đẩy tấm về đúng cạnh: lớp là flex nên tấm tự giãn theo trục còn lại.</summary>
    static string LayerFor(PanelSide side) => side switch
    {
        PanelSide.Left => "flex justify-start",
        PanelSide.Top => "flex flex-col justify-start",
        PanelSide.Bottom => "flex flex-col justify-end",
        _ => "flex justify-end",
    };

    /// <summary>Tấm nằm ngang (trên/dưới) thay vì dọc (trái/phải).</summary>
    protected bool Horizontal => Side is PanelSide.Top or PanelSide.Bottom;

    /// <summary>Khối tiêu đề + mô tả; null khi cả hai đều rỗng.</summary>
    protected Div? Header(Window w, string cls)
    {
        if (Title.Length == 0 && Description.Length == 0) return null;
        var header = w.Div().Class(cls);
        if (Title.Length > 0) header.Child(w.Div().Class(StyleSheet.PanelTitle).Child(Title));
        if (Description.Length > 0) header.Child(w.Div().Class(StyleSheet.PanelDescription).Child(Description));
        return header;
    }

    /// <summary>Thân (giãn hết phần còn lại khi tấm dọc, để chân dính đáy) rồi chân.</summary>
    protected void BodyAndFooter(Window w, Div panel, string bodyClass, string footerClass)
    {
        if (Body is { } body) panel.Child(w.Div().Class(bodyClass).Child(body(w)));
        else if (!Horizontal) panel.Child(w.Div().Class("flex-1"));
        if (Footer is { } footer) panel.Child(w.Div().Class(footerClass).Child(footer(w)));
    }
}
