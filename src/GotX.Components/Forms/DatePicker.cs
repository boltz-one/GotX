using System.Globalization;

namespace GotX.Components;

/// <summary>
/// Ô chọn ngày (trang "Date Picker" của shadcn/ui: nút viền + <see cref="Calendar"/> trong Popover). Nút hiện ngày đã
/// định dạng hoặc chữ gợi ý kèm icon lịch; bấm (hoặc Enter/Space/↓ khi focus) mở lịch ngay dưới nút, chọn một ngày thì
/// đóng lại và focus trở về nút; Escape hoặc bấm ra ngoài đóng mà không đổi giá trị.
/// Khác bản gốc: bấm lại đúng ngày đang chọn không đóng lịch (Calendar không phát sự kiện khi giá trị không đổi);
/// không có chọn khoảng ngày, dropdown tháng/năm hay ô nhập ngày bằng chữ.
/// <code>
/// _date = App.New(new DatePicker { Placeholder = "Pick a date" });
/// App.Subscribe&lt;DatePicker.ValueChanged&gt;(_date, e =&gt; …);
/// </code>
/// </summary>
public sealed class DatePicker : View
{
    // Nút outline của shadcn: w-[240px] justify-start text-left font-normal; chữ mờ khi chưa có ngày (data-[empty=true]).
    static readonly string Filled = Ui.ButtonClasses(ButtonVariant.Outline) + " justify-start gap-2 px-2.5 font-normal";
    static readonly string Empty = Filled + " text-muted-foreground";

    Overlay? _popup;
    Calendar _calendar = null!;
    FragmentView _content = null!;
    Window? _window;
    DateOnly? _value;
    string _label = "";
    string? _anchor, _triggerKey, _contentKey;

    /// <summary>Ngày được chọn vừa đổi (null = vừa bị xoá bằng code).</summary>
    public readonly record struct ValueChanged(DateOnly? Value);

    /// <summary>Lịch vừa mở / vừa đóng.</summary>
    public readonly record struct OpenChanged(bool Open);

    /// <summary>Ngày đang chọn; null = chưa chọn (nút hiện <see cref="Placeholder"/>).</summary>
    public DateOnly? Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value;
            _label = value is { } d ? Format(d) : "";
            if (_calendar is not null) _calendar.Selected = value;
            Notify();
            Emit(new ValueChanged(value));
        }
    }

    /// <summary>Chữ trên nút khi chưa chọn ngày.</summary>
    public string Placeholder { get; set; } = "Pick a date";

    /// <summary>Cách viết ngày trên nút; mặc định kiểu "November 15, 2026" (gần với định dạng PPP của date-fns).</summary>
    public Func<DateOnly, string> Format { get; set; } = static d => d.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);

    /// <summary>Class bề rộng của nút (mặc định 240px như bản gốc).</summary>
    public string WidthClass { get; set; } = "w-[240px]";

    /// <summary>Vô hiệu hoá: nút mờ, không mở lịch, bỏ khỏi thứ tự Tab.</summary>
    public bool Disabled { get; set { field = value; Notify(); } }

    /// <summary>Lịch bên trong – đặt <c>Today</c>, đọc tháng đang hiện.</summary>
    public Calendar Calendar => _calendar;

    public bool IsOpen => _popup is { IsOpen: true };

    /// <summary>Overlay đang mở (null khi đóng) – đọc vị trí thực tế của tấm lịch.</summary>
    public Overlay? Overlay => IsOpen ? _popup : null;

    /// <summary>Key của nút – phần tử focus được, nhận lại focus khi lịch đóng.</summary>
    public string TriggerKey => _triggerKey ??= "gotx-datepicker-trigger-" + Id.Value;

    /// <summary>Key của tấm lịch – phần tử giữ focus ngay sau khi mở (Tab đi tiếp vào lịch).</summary>
    public string ContentKey => _contentKey ??= "gotx-datepicker-content-" + Id.Value;

    protected override void OnCreated()
    {
        _calendar = App.New(new Calendar { Selected = _value });
        _content = App.New(new FragmentView(RenderContent));
        // Lịch là nguồn sự kiện duy nhất cho thao tác chọn: bấm ô ngày hay Enter trên ô đang focus đều đi qua đây.
        App.Subscribe<Calendar.SelectedChanged>(_calendar, e => { Value = e.Date; Close(); });
    }

    /// <summary>Mở lịch ở tháng của ngày đang chọn (cần đã render ít nhất một lần để biết cửa sổ).</summary>
    public void Open()
    {
        if (IsOpen || Disabled || _window is null) return;
        if (_value is { } d) _calendar.Month = d;
        var popup = _popup = _window.OpenOverlay(_content, new OverlayOptions { Anchor = _anchor, Placement = Placement.BottomStart, Offset = 4 });
        popup.Closed += () => { if (ReferenceEquals(_popup, popup)) _popup = null; Notify(); Emit(new OpenChanged(false)); };
        Notify();
        Emit(new OpenChanged(true));
    }

    /// <summary>Đóng lịch; Core trả focus về nút.</summary>
    public void Close() => _popup?.Close();

    bool TriggerKeyDown(KeyEvent e)
    {
        if (e.Key != Key.Down) return false; // Enter/Space đi qua OnClick
        Open();
        return true;
    }

    /// <summary>Vẽ nút; tấm lịch được vẽ riêng trong lớp overlay.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        _anchor ??= "gotx-datepicker-" + Id.Value;
        bool empty = _value is null;
        var button = w.Div().Class(empty ? Empty : Filled).Class(WidthClass)
            .Disabled(Disabled)
            .Key(TriggerKey).Anchor(_anchor).Focusable()
            .OnClick(this, static (p, _) => { if (p.IsOpen) p.Close(); else p.Open(); })
            .OnKey(this, static (p, e) => p.TriggerKeyDown(e))
            .Child(Icon.Calendar(w))
            .Child(w.Div().Class("truncate").Child(empty ? Placeholder : _label));
        // Lớp "flex" ngoài cùng giữ nút co theo bề rộng của nó dù view được đặt trong khối dọc.
        return w.Div().Class("flex").Child(button);
    }

    // PopoverContent className="w-auto p-0": tấm chỉ bọc lịch, bo góc và đổ bóng như popover.
    Div RenderContent(Window w) =>
        w.Div().Class("flex overflow-hidden rounded-lg bg-popover text-popover-foreground shadow-md ring-1 ring-foreground/10")
            .Key(ContentKey).Focusable().AutoFocus()
            .Child(w.View(_calendar));
}
