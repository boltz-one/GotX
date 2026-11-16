namespace GotX.Components;

/// <summary>Horizontal: hai panel trái/phải, thanh chia dọc. Vertical: trên/dưới, thanh chia ngang.</summary>
public enum ResizableDirection : byte { Horizontal, Vertical }

/// <summary>
/// Hai panel với thanh chia kéo được. Kích thước tính theo tỉ lệ (0..1) của panel đầu như react-resizable-panels,
/// nên đổi cỡ khung không làm lệch tỉ lệ. Chỉ hỗ trợ hai panel; chưa có con trỏ col-resize (Core chỉ có pointer/text/default).
/// Thanh chia focus được: mũi tên theo hướng chia đổi kích thước mỗi lần <see cref="KeyboardStep"/>, Home/End về hai giới hạn.
/// </summary>
public sealed class Resizable : View
{
    // Thanh chia rộng 9px để dễ bắt chuột nhưng âm lề 4px mỗi bên → chỉ chiếm 1px trong layout, giống after:w-1 của shadcn.
    static readonly string[] HandleClasses = ["relative shrink-0 w-[9px] -mx-1 flex justify-center cursor-pointer", "relative shrink-0 h-[9px] -my-1 flex flex-col justify-center cursor-pointer"];
    static readonly string[] LineClasses = ["w-px h-full bg-border", "h-px w-full bg-border"];
    // Thanh chia đang giữ focus bàn phím: đường kẻ dày lên và đổi màu ring (bản gốc: focus-visible:ring-1 ring-ring).
    static readonly string[] FocusLineClasses = ["w-[3px] h-full rounded-full bg-ring", "h-[3px] w-full rounded-full bg-ring"];
    static readonly string[] GripClasses =
    [
        "absolute top-1/2 left-[2.5px] -translate-y-1/2 h-6 w-1 rounded-lg bg-border",
        "absolute left-1/2 top-[2.5px] -translate-x-1/2 w-6 h-1 rounded-lg bg-border",
    ];
    static readonly string[] RootClasses = ["flex w-full", "flex flex-col w-full"];
    // Lưới 1×1 để nội dung người dùng giãn đủ hai chiều của panel mà không cần tự thêm h-full/flex-1.
    const string PanelClass = "grid grid-cols-1 grid-rows-1 min-w-0 min-h-0 overflow-hidden";

    float _size = 0.5f, _dragStart, _dragFrom, _dragTotal;
    Window? _window;
    string? _anchor, _handleKey;

    /// <summary>Phát khi tỉ lệ panel đầu đổi (khi kéo hoặc đặt <see cref="Size"/>).</summary>
    public readonly record struct SizeChanged(float Size);

    /// <summary>Hướng chia.</summary>
    public ResizableDirection Direction { get; set; }

    /// <summary>Nội dung panel đầu (trái/trên).</summary>
    public Func<Window, Div>? First { get; set; }

    /// <summary>Nội dung panel sau (phải/dưới).</summary>
    public Func<Window, Div>? Second { get; set; }

    /// <summary>Tỉ lệ nhỏ nhất của panel đầu.</summary>
    public float FirstMinSize { get; set; } = 0.1f;

    /// <summary>Tỉ lệ nhỏ nhất của panel sau.</summary>
    public float SecondMinSize { get; set; } = 0.1f;

    /// <summary>Mỗi lần nhấn mũi tên đổi bao nhiêu (tỉ lệ 0..1); mặc định 10% như react-resizable-panels.</summary>
    public float KeyboardStep { get; set; } = 0.1f;

    /// <summary>Vẽ nắm kéo nhỏ giữa thanh chia.</summary>
    public bool WithHandle { get; set; }

    /// <summary>Class kích thước/khung của cả nhóm (cần chiều cao xác định, nhất là hướng Vertical).</summary>
    public string BoxClass { get; set; } = "h-48";

    /// <summary>Tỉ lệ (0..1) panel đầu chiếm, kẹp theo hai min size.</summary>
    public float Size
    {
        get => _size;
        set
        {
            value = Math.Clamp(value, FirstMinSize, Math.Max(FirstMinSize, 1 - SecondMinSize));
            if (_size == value) return;
            _size = value;
            Notify();
            Emit(new SizeChanged(value));
        }
    }

    /// <summary>Key thanh chia (kiểm tra headless).</summary>
    public string HandleKey => _handleKey ??= "gotx-resizable-" + Id.Value + "-handle";

    /// <summary>Tên anchor của cả nhóm – dùng <c>window.AnchorBounds</c> để đo.</summary>
    public string GroupAnchor => _anchor ??= "gotx-resizable-" + Id.Value;

    void Drag(DragEvent e)
    {
        bool horizontal = Direction == ResizableDirection.Horizontal;
        float pos = horizontal ? e.Local.X : e.Local.Y;
        if (e.Phase == DragPhase.Start)
        {
            // Local tính theo hộp thanh chia lúc bắt đầu kéo, nên độ dời = pos - _dragFrom; quy ra tỉ lệ theo cả nhóm.
            _dragStart = _size;
            _dragFrom = pos;
            var group = _window?.AnchorBounds(GroupAnchor);
            _dragTotal = group is { } b ? (horizontal ? b.Width : b.Height) : 0;
            return;
        }
        if (_dragTotal <= 0) return;
        Size = _dragStart + (pos - _dragFrom) / _dragTotal;
    }

    bool KeyDown(KeyEvent e)
    {
        bool horizontal = Direction == ResizableDirection.Horizontal;
        switch (e.Key)
        {
            case Key.Left when horizontal: case Key.Up when !horizontal: Size = _size - KeyboardStep; return true;
            case Key.Right when horizontal: case Key.Down when !horizontal: Size = _size + KeyboardStep; return true;
            case Key.Home: Size = FirstMinSize; return true;
            case Key.End: Size = 1 - SecondMinSize; return true;
        }
        return false;
    }

    /// <summary>Vẽ panel đầu (kích thước theo tỉ lệ), thanh chia, panel sau (lấp phần còn lại).</summary>
    public override Div Render(Window w)
    {
        _window = w;
        int d = (int)Direction;
        var first = w.Div().Class(PanelClass + " shrink-0");
        if (Direction == ResizableDirection.Horizontal) first.Style.Width = Length.Fraction(_size);
        else first.Style.Height = Length.Fraction(_size);
        if (First is { } a) first.Child(a(w));

        bool focused = w.FocusVisible && w.IsFocused(HandleKey);
        var handle = w.Div().Key(HandleKey).Class(HandleClasses[d]).Focusable().Role(SemanticRole.Separator).AriaLabel("Resize")
            .OnDrag(this, static (r, e) => r.Drag(e))
            .OnKey(this, static (r, e) => r.KeyDown(e))
            .Child(w.Div().Class(focused ? FocusLineClasses[d] : LineClasses[d]));
        if (WithHandle) handle.Child(w.Div().Class(GripClasses[d]));

        var second = w.Div().Class(PanelClass + " flex-1");
        if (Second is { } b) second.Child(b(w));

        return w.Div().Anchor(GroupAnchor).Class(RootClasses[d]).Class(BoxClass)
            .Child(first).Child(handle).Child(second);
    }
}
