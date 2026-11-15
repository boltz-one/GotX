namespace GotX.Components;

/// <summary>
/// Menu thả xuống từ một nút: nhãn nhóm, đường kẻ, dòng có phím tắt, dòng checkbox, dòng bị tắt, dòng nguy hiểm, menu con.
/// Bấm dòng → <see cref="MenuItemSelected"/> rồi đóng; bấm ra ngoài / Escape / bấm lại nút để đóng.
/// Bàn phím: nút mở focus được (Enter/Space/↓ mở và tô dòng đầu, ↑ tô dòng cuối); trong menu ↑/↓/Home/End di chuyển,
/// Enter/Space chọn, → mở menu con, ← đóng nó; đóng xong focus trở về nút mở.
/// <code>
/// _menu = App.New(new DropdownMenu { TriggerText = "Tài khoản", Items = [MenuItem.Action("Hồ sơ"), …] });
/// App.Subscribe&lt;MenuItemSelected&gt;(_menu, e =&gt; …);
/// </code>
/// </summary>
public sealed class DropdownMenu : MenuOwner
{
    static readonly string DefaultTrigger = Ui.ButtonClasses(ButtonVariant.Outline) + " " + FocusRing.Ring;

    readonly PopupSlot _slot = new();
    MenuPanel _panel = null!;
    Window? _window;
    string? _anchor, _triggerKey;
    IReadOnlyList<MenuItem> _items = [];

    /// <summary>Menu vừa mở / vừa đóng.</summary>
    public readonly record struct OpenChanged(bool Open);

    public IReadOnlyList<MenuItem> Items
    {
        get => _items;
        set { _items = value; _panel?.Notify(); }
    }

    /// <summary>Chữ của nút mở mặc định (nút viền) khi không đặt <see cref="Trigger"/>.</summary>
    public string TriggerText { get; set; } = "Mở menu";

    /// <summary>
    /// Hình dáng nút mở tự vẽ. Phần tử bọc ngoài mới là nút (nhận click, focus, phím), nên nội dung ở đây chỉ để nhìn:
    /// dùng <c>w.Div().Class(Ui.ButtonClasses(…))</c> thay vì một nút tự focus được, kẻo có hai điểm dừng Tab.
    /// </summary>
    public Func<Window, Div>? Trigger { get; set; }

    /// <summary>
    /// Class của phần tử bọc <see cref="Trigger"/> – chính là nút (nhận focus, hover, vòng focus). Mặc định là lớp bọc
    /// trong suốt bo góc; đặt class của nút (<c>Ui.ButtonClasses(…)</c>) để lớp bọc là nút thật và Trigger chỉ còn icon.
    /// </summary>
    public string? TriggerClass { get; set; }

    /// <summary>Class thêm cho nút mở khi menu đang mở (<c>aria-expanded:bg-muted</c> của nút shadcn).</summary>
    public string TriggerOpenClass { get; set; } = "";

    public Placement Placement { get; set; } = Placement.BottomStart;

    /// <summary>Menu rộng tối thiểu bằng nút mở (như w-(--anchor-width) của shadcn).</summary>
    public bool MatchTriggerWidth { get; set; }

    public bool IsOpen => _slot.IsOpen;

    /// <summary>Overlay đang mở (null khi đóng) – đọc vị trí thực tế.</summary>
    public Overlay? Overlay => _slot.Current;

    /// <summary>Tấm danh sách – đọc dòng đang tô sáng (<see cref="MenuPanel.ActiveIndex"/>), menu con đang mở.</summary>
    public MenuPanel Panel => _panel;

    /// <summary>Tên anchor của nút mở (duy nhất theo instance).</summary>
    public string AnchorName => _anchor ??= "gotx-dropdown-" + Id.Value;

    /// <summary>Key của nút mở – phần tử nhận lại focus khi menu đóng.</summary>
    public string TriggerKey => _triggerKey ??= "gotx-dropdown-trigger-" + Id.Value;

    /// <summary>Tạo panel danh sách dùng lại cho mọi lần mở.</summary>
    protected override void OnCreated() => _panel = NewPanel(0);

    private protected override IReadOnlyList<MenuItem> ItemsOf(int menu) => _items;

    /// <summary>Mở menu (cần đã render ít nhất một lần); chưa dòng nào được tô sáng, như khi mở bằng chuột.</summary>
    public void Open() => Open(MenuHighlight.None);

    void Open(MenuHighlight highlight)
    {
        if (IsOpen || _window is null) return;
        OpenPanel(_slot, _window, _panel, new OverlayOptions
        {
            Anchor = AnchorName, Placement = Placement, MatchAnchorWidth = MatchTriggerWidth,
        }, highlight, () => { Notify(); Emit(new OpenChanged(_slot.IsOpen)); });
    }

    /// <summary>Đóng menu.</summary>
    public override void Close() => ClosePanel(_slot, _panel);

    bool TriggerKeyDown(KeyEvent e)
    {
        if (!OpensMenu(e.Key, out var highlight)) return false;
        if (IsOpen) Close(); else Open(highlight);
        return true;
    }

    /// <summary>Vẽ nút mở; danh sách được vẽ riêng trong lớp overlay.</summary>
    public override Div Render(Window w)
    {
        using var _ = ComponentStyles.Use(Sheet); // nút mở tự vẽ (Trigger) theo style lúc tạo menu
        _window = w;
        var trigger = Trigger is { } t
            ? w.Div().Class(TriggerClass ?? Sheet.MenuTriggerWrapper).Child(t(w))
            : w.Div().Class(DefaultTrigger).Child(TriggerText).Child(Icon.ChevronDown(w));
        if (IsOpen && TriggerOpenClass.Length > 0) trigger.Class(TriggerOpenClass);
        // Lớp "flex" ngoài cùng giữ nút co theo nội dung dù view được đặt trong khối dọc.
        return w.Div().Class("flex").Child(trigger.Key(TriggerKey).Anchor(AnchorName).Focusable()
            .OnClick(this, static (m, _) => { if (m.IsOpen) m.Close(); else m.Open(); })
            .OnKey(this, static (m, e) => m.TriggerKeyDown(e)));
    }
}
