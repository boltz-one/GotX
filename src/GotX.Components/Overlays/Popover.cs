namespace GotX.Components;

/// <summary>
/// Nội dung tuỳ ý (form nhỏ, cài đặt nhanh…) nổi cạnh nút mở theo <see cref="Placement"/>, tự lật/trượt khi sát mép.
/// Bấm nút mở để bật/tắt, bấm ra ngoài hoặc Escape để đóng. Không modal: trang phía sau vẫn tương tác được.
/// Nút mở focus được (Enter/Space bật/tắt); khi mở, tấm nhận focus và Tab đi tiếp vào nội dung; đóng thì focus về nút.
/// <code>
/// _pop = App.New(new Popover { TriggerText = "Kích thước", Title = "Kích thước", Body = w =&gt; … });
/// w.View(_pop)
/// </code>
/// </summary>
public sealed class Popover : View
{
    readonly PopupSlot _slot = new();
    FragmentView _content = null!;
    Window? _window;
    string? _anchor, _triggerKey, _contentKey;
    static readonly string DefaultTrigger = Ui.ButtonClasses(ButtonVariant.Outline) + " " + FocusRing.Ring;

    /// <summary>Popover vừa mở / vừa đóng.</summary>
    public readonly record struct OpenChanged(bool Open);

    /// <summary>Chữ của nút mở mặc định (nút viền) khi không đặt <see cref="Trigger"/>.</summary>
    public string TriggerText { get; set; } = "Mở";

    /// <summary>
    /// Hình dáng nút mở tự vẽ. Phần tử bọc ngoài mới là nút (nhận click, focus, Enter/Space), nên nội dung ở đây chỉ để
    /// nhìn: dùng <c>w.Div().Class(Ui.ButtonClasses(…))</c> thay vì một nút tự focus được, kẻo có hai điểm dừng Tab.
    /// </summary>
    public Func<Window, Div>? Trigger { get; set; }

    public string Title { get; set; } = "";
    public string Description { get; set; } = "";

    /// <summary>Nội dung dưới tiêu đề.</summary>
    public Func<Window, Div>? Body { get; set; }

    public Placement Placement { get; set; } = Placement.Bottom;
    public float Offset { get; set; } = 4;

    /// <summary>Class độ rộng/thêm cho tấm nổi; mặc định <c>w-72</c> như shadcn.</summary>
    public string PanelClass { get; set; } = "w-72";

    public bool IsOpen => _slot.IsOpen;

    /// <summary>Overlay đang mở (null khi đóng) – đọc vị trí thực tế.</summary>
    public Overlay? Overlay => _slot.Current;

    /// <summary>Key của nút mở – phần tử focus được, nhận lại focus khi tấm đóng.</summary>
    public string TriggerKey => _triggerKey ??= "gotx-popover-trigger-" + Id.Value;

    /// <summary>Key của tấm nổi – phần tử giữ focus ngay sau khi mở.</summary>
    public string ContentKey => _contentKey ??= "gotx-popover-content-" + Id.Value;

    /// <summary>Tên anchor của nút mở (duy nhất theo instance).</summary>
    public string AnchorName => _anchor ??= "gotx-popover-" + Id.Value;

    /// <summary>Tạo view nội dung dùng lại cho mọi lần mở.</summary>
    protected override void OnCreated() => _content = App.New(new FragmentView(RenderContent));

    /// <summary>Mở tấm nổi (cần đã render ít nhất một lần để biết cửa sổ).</summary>
    public void Open()
    {
        if (IsOpen || _window is null) return;
        _slot.Open(_window, _content, new OverlayOptions { Anchor = AnchorName, Placement = Placement, Offset = Offset },
            () => { Notify(); Emit(new OpenChanged(_slot.IsOpen)); });
    }

    /// <summary>Đóng tấm nổi.</summary>
    public void Close() => _slot.Close();

    /// <summary>Vẽ nút mở; tấm nổi được vẽ riêng trong lớp overlay.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        var trigger = Trigger is { } t
            ? w.Div().Class("flex rounded-lg " + FocusRing.RingOnly).Child(t(w))
            : w.Div().Class(DefaultTrigger).Child(TriggerText);
        // Lớp "flex" ngoài cùng giữ nút co theo nội dung dù view được đặt trong khối dọc.
        return w.Div().Class("flex").Child(trigger.Key(TriggerKey).Anchor(AnchorName).Focusable()
            .OnClick(this, static (p, _) => { if (p.IsOpen) p.Close(); else p.Open(); }));
    }

    Div RenderContent(Window w)
    {
        var panel = w.Div().Class("flex flex-col gap-2.5 rounded-lg bg-popover p-2.5 text-sm text-popover-foreground shadow-md ring-1 ring-foreground/10")
            .TakeFocus(ContentKey);
        if (PanelClass.Length > 0) panel.Class(PanelClass);
        if (Title.Length > 0 || Description.Length > 0)
        {
            var header = w.Div().Class("flex flex-col gap-0.5 text-sm");
            if (Title.Length > 0) header.Child(w.Div().Class("font-medium").Child(Title));
            if (Description.Length > 0) header.Child(w.Div().Class("text-muted-foreground").Child(Description));
            panel.Child(header);
        }
        if (Body is { } body) panel.Child(body(w));
        return panel;
    }
}
