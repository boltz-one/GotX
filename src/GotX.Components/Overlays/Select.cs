namespace GotX.Components;

/// <summary>
/// Hộp chọn một giá trị: nút hiện giá trị đang chọn, bấm thì mở danh sách neo ngay dưới nút
/// (tự lật lên trên khi sát đáy cửa sổ), rộng tối thiểu bằng nút. Bấm ra ngoài hoặc Escape để đóng.
/// Bàn phím: nút focus được, Enter/Space/↑/↓ mở; trong danh sách ↑/↓/Home/End di chuyển (không quay vòng, như Radix),
/// Enter/Space chọn; đóng xong focus trở về nút. Chưa có gõ chữ để nhảy tới mục (typeahead): Core chỉ gửi ký tự
/// cho ô nhập, phần tử thường chỉ nhận phím điều khiển.
/// </summary>
public sealed class Select : View
{
    const string Row = "flex items-center justify-between gap-2 rounded-md py-1 pl-1.5 pr-1.5 text-sm cursor-pointer";
    const string RowOn = Row + " bg-accent text-accent-foreground";

    IReadOnlyList<string> _items = [];
    string[] _keys = [];
    int _index = -1, _active = -1;
    Window? _window;
    Overlay? _popup;
    ListView _list = null!;
    string? _anchor, _triggerKey, _listKey;

    public readonly record struct ValueChanged(int Index, string Value);

    public IReadOnlyList<string> Items
    {
        get => _items;
        set { _items = value; if (_index >= value.Count) _index = -1; Notify(); _list?.Notify(); }
    }

    public string Placeholder { get; set; } = "Chọn…";
    public bool Disabled { get; set; }

    public int SelectedIndex
    {
        get => _index;
        set
        {
            if (value < -1 || value >= _items.Count) throw new ArgumentOutOfRangeException(nameof(value));
            if (_index == value) return;
            _index = value;
            Notify();
            _list?.Notify();
            if (value >= 0) Emit(new ValueChanged(value, _items[value]));
        }
    }

    public string? Value => _index >= 0 ? _items[_index] : null;
    public bool IsOpen => _popup is { IsOpen: true };

    /// <summary>Mục đang được tô sáng trong danh sách mở (bằng phím hoặc chuột); -1 khi chưa có.</summary>
    public int ActiveIndex => IsOpen ? _active : -1;

    /// <summary>Key của nút – phần tử focus được, nhận lại focus khi danh sách đóng.</summary>
    public string TriggerKey => _triggerKey ??= "gotx-select-trigger-" + Id.Value;

    /// <summary>Key của mục thứ <paramref name="index"/> trong danh sách (cũng là tên anchor để cuộn mục vào tầm nhìn).</summary>
    public string ItemKey(int index) => RovingFocus.Names(ref _keys, Math.Max(index + 1, _items.Count), "gotx-select-", Id)[index];

    protected override void OnCreated() => _list = App.New(new ListView(this));

    public void Open()
    {
        if (IsOpen || Disabled || _window is null) return;
        // Như Radix: mở ra thì mục đang chọn được tô sáng; chưa chọn gì thì chưa tô mục nào.
        _active = _index;
        _popup = _window.OpenOverlay(_list, new OverlayOptions
        {
            Anchor = _anchor, Placement = Placement.BottomStart, MatchAnchorWidth = true,
        });
        _popup.Closed += () => { _popup = null; Notify(); };
        // Danh sách dài: đưa mục đang chọn vào tầm nhìn. Danh sách chưa được vẽ nên Core giữ yêu cầu tới cuối frame kế.
        if (_active >= 0) _window.ScrollIntoView(ItemKey(_active));
        Notify();
        _list.Notify();
    }

    public void Close() => _popup?.Close();

    void Pick(int index)
    {
        if ((uint)index >= (uint)_items.Count) return;
        SelectedIndex = index;
        Close();
    }

    void SetActive(int index, bool reveal)
    {
        if (_active == index) return;
        _active = index;
        _list.Notify();
        if (reveal && index >= 0) _window?.ScrollIntoView(ItemKey(index));
    }

    bool ListKeyDown(KeyEvent e)
    {
        int next = RovingFocus.FromKey(e.Key, this, _items.Count, _active, loop: false, vertical: true, static (_, _) => true);
        if (next != RovingFocus.NotNavigation) { SetActive(next, reveal: true); return true; }
        if (e.Key is Key.Enter or Key.Space) { if (_active >= 0) Pick(_active); return true; }
        return e.Key == Key.Tab; // Tab không rời danh sách đang mở
    }

    bool TriggerKeyDown(KeyEvent e)
    {
        if (e.Key is not (Key.Up or Key.Down)) return false; // Enter/Space đi qua OnClick
        Open();
        return true;
    }

    public override Div Render(Window w)
    {
        _window = w;
        _anchor ??= "gotx-select-" + Id.Value;
        return w.Div().Class(
                "flex h-8 min-w-36 items-center justify-between gap-1.5 rounded-lg border border-input bg-transparent " +
                "pl-2.5 pr-2 text-sm whitespace-nowrap cursor-pointer transition-colors hover:bg-muted/50 " +
                "disabled:opacity-50 dark:bg-input/30 dark:hover:bg-input/50 " + FocusRing.Ring)
            .Disabled(Disabled)
            .Key(TriggerKey).Anchor(_anchor).Focusable()
            .OnClick(this, static (s, _) => { if (s.IsOpen) s.Close(); else s.Open(); })
            .OnKey(this, static (s, e) => s.TriggerKeyDown(e))
            .Child(w.Div().Class(_index >= 0 ? "truncate" : "truncate text-muted-foreground").Child(Value ?? Placeholder))
            .Child(w.Div().Class("text-muted-foreground").Child(Icon.ChevronDown(w)));
    }

    /// <summary>Danh sách lựa chọn, vẽ trong lớp overlay; chính nó giữ focus khi mở.</summary>
    sealed class ListView(Select owner) : View
    {
        Indexed<Select>[]? _refs;

        public override Div Render(Window w)
        {
            var items = owner._items;
            var refs = Indexed<Select>.Ensure(ref _refs, owner, items.Count);
            var list = w.Div().Key(owner._listKey ??= "gotx-select-list-" + owner.Id.Value).Class(
                    "flex flex-col p-1 max-h-72 overflow-y-auto rounded-lg bg-popover text-popover-foreground shadow-md ring-1 ring-foreground/10")
                .Focusable().AutoFocus()
                .OnKey(owner, static (s, e) => s.ListKeyDown(e));
            for (int i = 0; i < items.Count; i++)
            {
                string key = owner.ItemKey(i);
                var row = w.Div().Key(key).Anchor(key).Class(i == owner._active ? RowOn : Row)
                    .OnClick(refs[i], static (r, _) => r.Owner.Pick(r.Index))
                    // Rời mục thì tắt sáng (Radix trả focus về danh sách), nên chuột và phím dùng chung một dòng sáng.
                    .OnHover(refs[i], static (r, inside) => { if (inside) r.Owner.SetActive(r.Index, false); else if (r.Owner._active == r.Index) r.Owner.SetActive(-1, false); })
                    .Child(w.Div().Class("truncate").Child(items[i]));
                row.Child(i == owner._index ? Icon.Check(w) : w.Div().Class("size-4 shrink-0"));
                list.Child(row);
            }
            return list;
        }
    }
}
