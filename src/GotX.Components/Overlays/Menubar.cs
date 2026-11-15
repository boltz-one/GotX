namespace GotX.Components;

/// <summary>Một menu của <see cref="Menubar"/>: chữ trên thanh + các dòng.</summary>
public sealed record MenubarMenu(string Title, IReadOnlyList<MenuItem> Items);

/// <summary>
/// Thanh menu kiểu ứng dụng desktop (Tệp, Sửa, Xem…): bấm một mục mở menu của nó ngay dưới; khi đang có menu mở,
/// rê chuột sang mục khác thì chuyển menu luôn, như thanh menu thật. Sự kiện mang chỉ số menu trong
/// <see cref="MenuItemSelected.Menu"/>.
/// Bàn phím: cả thanh là một điểm dừng Tab, ←/→ đi giữa các mục, ↓/Enter/Space mở menu; trong menu đang mở
/// ←/→ chuyển sang menu bên cạnh (trừ khi dòng đang chọn mở/đóng menu con).
/// </summary>
public sealed class Menubar : MenuOwner
{
    const string TriggerBase = "flex items-center rounded-sm px-1.5 py-[2px] text-sm font-medium cursor-pointer focus-visible:bg-muted ";

    readonly PopupSlot _slot = new();
    readonly List<MenuPanel> _panels = new();
    Indexed<Menubar>[]? _refs;
    string[] _anchors = [], _keys = [];
    Window? _window;
    IReadOnlyList<MenubarMenu> _menus = [];
    int _open = -1;

    public IReadOnlyList<MenubarMenu> Menus
    {
        get => _menus;
        set { if (_open >= 0) Close(); _menus = value; Notify(); }
    }

    /// <summary>Chỉ số menu đang mở, -1 khi không có.</summary>
    public int OpenIndex => _slot.IsOpen ? _open : -1;

    /// <summary>Overlay đang mở (null khi đóng) – đọc vị trí thực tế.</summary>
    public Overlay? Overlay => _slot.Current;

    /// <summary>Tấm danh sách của menu đang mở (null khi không có) – đọc dòng đang tô sáng.</summary>
    public MenuPanel? Panel => OpenIndex >= 0 ? _panels[_open] : null;

    /// <summary>Tên anchor của mục thứ <paramref name="index"/> trên thanh (duy nhất theo instance).</summary>
    public string AnchorName(int index) => RovingFocus.Names(ref _anchors, _menus.Count, "gotx-menubar-", Id)[index];

    /// <summary>Key của mục thứ <paramref name="index"/> trên thanh – phần tử focus được.</summary>
    public string TriggerKey(int index) => RovingFocus.Names(ref _keys, _menus.Count, "gotx-menubar-trigger-", Id)[index];

    private protected override IReadOnlyList<MenuItem> ItemsOf(int menu) => menu < _menus.Count ? _menus[menu].Items : [];

    /// <summary>Mở menu thứ <paramref name="index"/> (đóng menu khác đang mở).</summary>
    public void Open(int index) => Open(index, MenuHighlight.None);

    void Open(int index, MenuHighlight highlight)
    {
        if (_window is null || (uint)index >= (uint)_menus.Count || OpenIndex == index) return;
        while (_panels.Count <= index) _panels.Add(NewPanel(_panels.Count));
        Close();
        _open = index;
        OpenPanel(_slot, _window, _panels[index], new OverlayOptions { Anchor = AnchorName(index), Placement = Placement.BottomStart },
            highlight, Notify);
    }

    /// <summary>Đóng menu đang mở.</summary>
    public override void Close() => ClosePanel(_slot, (uint)_open < (uint)_panels.Count ? _panels[_open] : null);

    void Toggle(int index) { if (OpenIndex == index) Close(); else Open(index); }

    // Đang có menu mở thì rê sang mục khác là chuyển menu, không cần bấm.
    void Hover(int index) { if (OpenIndex >= 0 && OpenIndex != index) Open(index); }

    int Neighbor(int from, int direction) =>
        RovingFocus.Step(this, _menus.Count, from, direction, LoopFocus, static (_, _) => true);

    /// <summary>←/→ trong menu đang mở: sang menu bên cạnh, tô sẵn dòng đầu, focus đi theo mục mới trên thanh.</summary>
    internal override bool OnHorizontalKey(int direction)
    {
        int open = OpenIndex, next = open < 0 ? -1 : Neighbor(open, direction);
        if (next < 0 || next == open || _window is null) return false;
        Close();
        // Focus mục mới TRƯỚC khi mở để overlay nhớ nó làm nơi trả focus khi đóng.
        _window.FocusKey(TriggerKey(next));
        Open(next, MenuHighlight.First);
        return true;
    }

    bool TriggerKeyDown(int index, KeyEvent e)
    {
        if (_window is null) return false;
        if (e.Key == Key.Tab) return RovingFocus.TabOut(_window, e.Shift, TriggerKey(0), TriggerKey(_menus.Count - 1));
        if (e.Key is Key.Left or Key.Right or Key.Home or Key.End)
        {
            int next = RovingFocus.FromKey(e.Key, this, _menus.Count, index, LoopFocus, vertical: false, static (_, _) => true);
            if (next >= 0 && next != index) { Close(); _window.FocusKey(TriggerKey(next)); }
            return true;
        }
        if (!OpensMenu(e.Key, out var highlight)) return false;
        if (OpenIndex == index) Close(); else Open(index, highlight);
        return true;
    }

    /// <summary>Vẽ thanh mục; menu được vẽ riêng trong lớp overlay.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        var refs = Indexed<Menubar>.Ensure(ref _refs, this, _menus.Count);
        var bar = w.Div().Class("flex h-8 items-center gap-0.5 rounded-lg border border-border p-[3px]");
        int open = OpenIndex;
        for (int i = 0; i < _menus.Count; i++)
            bar.Child(w.Div()
                .Class(i == open ? TriggerBase + "bg-muted" : TriggerBase + "hover:bg-muted")
                .Key(TriggerKey(i)).Anchor(AnchorName(i)).Focusable()
                .OnClick(refs[i], static (r, _) => r.Owner.Toggle(r.Index))
                .OnKey(refs[i], static (r, e) => r.Owner.TriggerKeyDown(r.Index, e))
                .OnHover(refs[i], static (r, inside) => { if (inside) r.Owner.Hover(r.Index); })
                .Child(_menus[i].Title));
        return bar;
    }
}
