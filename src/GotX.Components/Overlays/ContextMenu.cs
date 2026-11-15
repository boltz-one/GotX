namespace GotX.Components;

/// <summary>
/// Menu mở bằng chuột phải trong một vùng, góc trên-trái đặt đúng tại điểm bấm (tự lật/trượt khi sát mép cửa sổ).
/// Dùng chung khối danh sách với DropdownMenu nên hỗ trợ cùng loại dòng, menu con, sự kiện và điều hướng bàn phím
/// (menu lấy focus khi mở: ↑/↓/Home/End, Enter/Space, →/←, Escape).
/// <code>
/// _ctx = App.New(new ContextMenu { Items = [MenuItem.Action("Quay lại", "Alt+←"), …] });
/// w.View(_ctx)   // vùng bấm chuột phải mặc định; đặt Area để tự vẽ vùng
/// </code>
/// </summary>
public sealed class ContextMenu : MenuOwner
{
    readonly PopupSlot _slot = new();
    MenuPanel _panel = null!;
    Window? _window;
    IReadOnlyList<MenuItem> _items = [];

    /// <summary>Menu vừa mở / vừa đóng.</summary>
    public readonly record struct OpenChanged(bool Open);

    public IReadOnlyList<MenuItem> Items
    {
        get => _items;
        set { _items = value; _panel?.Notify(); }
    }

    /// <summary>Vùng nhận chuột phải; null = khung viền đứt mặc định với <see cref="AreaText"/>.</summary>
    public Func<Window, Div>? Area { get; set; }

    public string AreaText { get; set; } = "Bấm chuột phải vào đây";

    public bool IsOpen => _slot.IsOpen;

    /// <summary>Overlay đang mở (null khi đóng) – đọc vị trí thực tế.</summary>
    public Overlay? Overlay => _slot.Current;

    /// <summary>Tấm danh sách – đọc dòng đang tô sáng, menu con đang mở.</summary>
    public MenuPanel Panel => _panel;

    /// <summary>Tạo panel danh sách dùng lại cho mọi lần mở.</summary>
    protected override void OnCreated() => _panel = NewPanel(0);

    private protected override IReadOnlyList<MenuItem> ItemsOf(int menu) => _items;

    /// <summary>Mở menu tại điểm <paramref name="at"/> (logical px trong cửa sổ).</summary>
    public void OpenAt(Point at)
    {
        if (_window is null) return;
        Close();
        // Neo vào hình chữ nhật rỗng tại điểm bấm; Offset 0 để góc menu trùng điểm bấm.
        OpenPanel(_slot, _window, _panel, new OverlayOptions
        {
            AnchorRect = new Bounds(at.X, at.Y, 0, 0), Placement = Placement.BottomStart, Offset = 0,
        }, MenuHighlight.None, () => Emit(new OpenChanged(_slot.IsOpen)));
    }

    /// <summary>Đóng menu.</summary>
    public override void Close() => ClosePanel(_slot, _panel);

    /// <summary>Vẽ vùng nhận chuột phải; menu được vẽ riêng trong lớp overlay.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        var area = Area is { } a
            ? a(w)
            : w.Div().Class("flex h-36 w-72 items-center justify-center rounded-xl border border-dashed border-border text-sm text-muted-foreground")
                .Child(AreaText);
        return area.OnAuxClick(this, static (m, e) => { if (e.Button == MouseButton.Right) m.OpenAt(e.Position); });
    }
}
