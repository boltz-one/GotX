namespace GotX.Components;

/// <summary>
/// Chú thích ngắn hiện khi rê chuột vào phần tử hoặc khi phần tử (hay một nút bên trong nó) được Tab tới,
/// mặc định phía trên, có mũi tên chỉ vào phần tử. Ẩn khi chuột rời / focus rời / Escape.
/// Mặc định hiện ngay (<see cref="OpenDelay"/> = 0, như TooltipProvider của shadcn đặt <c>delay = 0</c>).
/// <code>
/// _tip = App.New(new Tooltip { Text = "Thêm vào thư viện", Trigger = w =&gt; Ui.Button(w, "Rê chuột") });
/// </code>
/// </summary>
public sealed class Tooltip : View
{
    readonly PopupSlot _slot = new();
    readonly Delay _open = new();
    FragmentView _content = null!;
    AfterFrame _afterFrame = null!;
    Window? _window;
    string? _anchor;
    bool _hovered, _focused;

    /// <summary>Tooltip vừa hiện / vừa ẩn.</summary>
    public readonly record struct OpenChanged(bool Open);

    public string Text { get; set; } = "";

    /// <summary>Phần tử được chú thích. Muốn tooltip hiện khi Tab tới thì phần tử này (hoặc con của nó) phải focus được.</summary>
    public Func<Window, Div>? Trigger { get; set; }

    public Placement Placement { get; set; } = Placement.Top;

    /// <summary>Số giây chờ sau khi chuột vào rồi mới hiện; chuột rời trước đó thì không hiện. Focus bàn phím luôn hiện ngay.</summary>
    public double OpenDelay { get; set; }

    public bool IsOpen => _slot.IsOpen;

    /// <summary>Overlay đang hiện (null khi ẩn) – đọc vị trí thực tế.</summary>
    public Overlay? Overlay => _slot.Current;

    /// <summary>Tên anchor của phần tử được chú thích (duy nhất theo instance).</summary>
    public string AnchorName => _anchor ??= "gotx-tooltip-" + Id.Value;

    /// <summary>Tạo view nội dung dùng lại cho mọi lần hiện.</summary>
    protected override void OnCreated()
    {
        _content = App.New(new FragmentView(RenderTip));
        _afterFrame = AfterFrame.Create(App, Sync);
    }

    /// <summary>Hiện tooltip ngay (cần đã render ít nhất một lần).</summary>
    public void Show()
    {
        _open.Cancel();
        if (IsOpen || _window is null || Text.Length == 0) return;
        // Không đóng khi bấm ra ngoài theo kiểu dropdown: tooltip chỉ theo chuột / focus.
        _slot.Open(_window, _content, new OverlayOptions
        {
            Anchor = AnchorName, Placement = Placement, Offset = 10, DismissOnOutsideClick = false,
        }, () => Emit(new OpenChanged(_slot.IsOpen)));
    }

    /// <summary>Ẩn tooltip và huỷ lần hiện đang chờ.</summary>
    public void Hide()
    {
        _open.Cancel();
        _slot.Close();
    }


    void Sync()
    {
        if (!_hovered && !_focused) Hide();
        else if (!IsOpen && (_focused || !_open.Pending)) _open.Start(App, _focused ? 0 : OpenDelay, Show);
    }

    /// <summary>Vẽ phần tử được chú thích; tooltip được vẽ riêng trong lớp overlay.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        // Core không phát sự kiện focus, nhưng đổi focus làm view chứa phần tử vẽ lại: so focus với anchor ngay ở đây.
        // Hiện/ẩn (mở, đóng overlay) thì để sau frame, không làm giữa lúc cây đang dựng.
        bool focused = FocusRing.Within(w, AnchorName);
        if (focused != _focused) { _focused = focused; _afterFrame.Request(); }
        return w.Div().Class("flex").Anchor(AnchorName)
            .OnHover(this, static (t, inside) => { t._hovered = inside; t.Sync(); })
            .Child(Trigger is { } t ? t(w) : w.Div().Child(Text));
    }

    Div RenderTip(Window w)
    {
        // Mũi tên: ô vuông xoay 45° cùng màu nền, đặt ở cạnh hướng về phần tử theo placement thực tế (sau khi lật).
        int side = (int)(_slot.Current?.ActualPlacement ?? Placement) / 3;
        string arrow = side switch
        {
            0 => "absolute left-1/2 -top-1.5 size-2.5 -translate-x-1/2 rotate-45 rounded-[2px] bg-foreground",
            1 => "absolute left-1/2 -bottom-1.5 size-2.5 -translate-x-1/2 rotate-45 rounded-[2px] bg-foreground",
            2 => "absolute top-1/2 -left-1.5 size-2.5 -translate-y-1/2 rotate-45 rounded-[2px] bg-foreground",
            _ => "absolute top-1/2 -right-1.5 size-2.5 -translate-y-1/2 rotate-45 rounded-[2px] bg-foreground",
        };
        return w.Div().Class("relative flex max-w-xs items-center gap-1.5 rounded-md bg-foreground px-3 py-1.5 text-xs text-background")
            .Child(w.Div().Class(arrow))
            .Child(Text);
    }
}
