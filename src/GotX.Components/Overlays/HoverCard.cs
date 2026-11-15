namespace GotX.Components;

/// <summary>
/// Thẻ xem trước nội dung phong phú (hồ sơ, liên kết) hiện khi rê chuột vào phần tử (hoặc Tab tới nó) sau
/// <see cref="OpenDelay"/>; giữ nguyên khi chuột đi tiếp vào thẻ, ẩn sau <see cref="CloseDelay"/> khi chuột rời cả hai.
/// Lướt chuột ngang qua không làm thẻ bật lên, và rời đi rồi quay lại ngay thì thẻ không chớp tắt.
/// </summary>
public sealed class HoverCard : View
{
    readonly PopupSlot _slot = new();
    readonly Delay _open = new(), _close = new();
    FragmentView _content = null!;
    AfterFrame _afterFrame = null!;
    Window? _window;
    string? _anchor;
    bool _inTrigger, _inCard, _focused;

    /// <summary>Thẻ vừa hiện / vừa ẩn.</summary>
    public readonly record struct OpenChanged(bool Open);

    /// <summary>Phần tử kích hoạt (thường là liên kết hay avatar). Muốn mở bằng Tab thì nó (hoặc con của nó) phải focus được.</summary>
    public Func<Window, Div>? Trigger { get; set; }

    /// <summary>Nội dung thẻ.</summary>
    public Func<Window, Div>? Body { get; set; }

    public Placement Placement { get; set; } = Placement.Bottom;

    /// <summary>Class độ rộng/thêm của thẻ; mặc định <c>w-64</c> như shadcn.</summary>
    public string PanelClass { get; set; } = "w-64";

    /// <summary>Số giây chuột phải nằm trên phần tử trước khi thẻ hiện (Radix: 700 ms).</summary>
    public double OpenDelay { get; set; } = 0.7;

    /// <summary>Số giây chờ sau khi chuột rời cả phần tử lẫn thẻ rồi mới ẩn (Radix: 300 ms).</summary>
    public double CloseDelay { get; set; } = 0.3;

    public bool IsOpen => _slot.IsOpen;

    /// <summary>Overlay đang hiện (null khi ẩn) – đọc vị trí thực tế.</summary>
    public Overlay? Overlay => _slot.Current;

    /// <summary>Tên anchor của phần tử kích hoạt (duy nhất theo instance).</summary>
    public string AnchorName => _anchor ??= "gotx-hovercard-" + Id.Value;

    /// <summary>
    /// Tạo view nội dung, và quyết định ẩn/hiện SAU khi cả lượt hover đã xử lý xong: khi chuột đi từ phần tử sang thẻ
    /// trong một lần di, "ra khỏi phần tử" đến trước "vào thẻ". Notify xếp một effect chạy trong App.Flush (sau mọi
    /// callback hover) nên Observe là điểm quyết định đúng lúc – với CloseDelay = 0 thẻ vẫn không chớp tắt.
    /// </summary>
    protected override void OnCreated()
    {
        _content = App.New(new FragmentView(RenderCard));
        App.Observe(this, Evaluate);
        _afterFrame = AfterFrame.Create(App, Evaluate);
    }

    void Evaluate()
    {
        if (_inTrigger || _inCard || _focused)
        {
            _close.Cancel();
            if (!IsOpen && !_open.Pending) _open.Start(App, OpenDelay, Show);
        }
        else
        {
            _open.Cancel();
            if (IsOpen && !_close.Pending) _close.Start(App, CloseDelay, Hide);
        }
    }

    /// <summary>Hiện thẻ ngay, bỏ qua độ trễ (cần đã render ít nhất một lần).</summary>
    public void Show()
    {
        _open.Cancel();
        if (IsOpen || _window is null) return;
        _slot.Open(_window, _content, new OverlayOptions { Anchor = AnchorName, Placement = Placement, Offset = 0 }, OnSlotChanged);
    }

    /// <summary>Ẩn thẻ ngay và huỷ mọi hẹn giờ đang chờ.</summary>
    public void Hide()
    {
        _open.Cancel();
        _close.Cancel();
        _slot.Close();
    }

    void OnSlotChanged()
    {
        // Thẻ biến mất (kể cả do Escape) thì vùng hover của nó cũng mất mà không phát "ra" – tự xoá cờ và hẹn giờ.
        if (!_slot.IsOpen) { _inCard = false; _close.Cancel(); }
        Emit(new OpenChanged(_slot.IsOpen));
    }


    void SetHover(bool trigger, bool inside)
    {
        if (trigger) _inTrigger = inside; else _inCard = inside;
        Notify();
    }

    /// <summary>Vẽ phần tử kích hoạt; thẻ được vẽ riêng trong lớp overlay.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        // Đổi focus làm view này vẽ lại: so focus với anchor ngay ở đây, còn việc hẹn mở/đóng thì để sau frame.
        bool focused = FocusRing.Within(w, AnchorName);
        if (focused != _focused) { _focused = focused; _afterFrame.Request(); }
        return w.Div().Class("flex").Anchor(AnchorName)
            .OnHover(this, static (c, inside) => c.SetHover(true, inside))
            .Child(Trigger is { } t ? t(w) : w.Div().Child("@gotx"));
    }

    Div RenderCard(Window w)
    {
        var card = w.Div().Class("rounded-lg bg-popover p-2.5 text-sm text-popover-foreground shadow-md ring-1 ring-foreground/10");
        if (PanelClass.Length > 0) card.Class(PanelClass);
        if (Body is { } body) card.Child(body(w));
        // Lớp bọc trong suốt p-1 thay cho khoảng hở 4px: vùng hover của thẻ nối liền với phần tử, chuột đi qua không bị ngắt.
        return w.Div().Class("p-1").OnHover(this, static (c, inside) => c.SetHover(false, inside)).Child(card);
    }
}
