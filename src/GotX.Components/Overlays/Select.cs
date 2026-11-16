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
    // Bảng class của style lúc tạo (danh sách bật ra vẽ trong overlay, ngoài phạm vi style của view cha).
    // Bảng ghim lúc tạo nếu được tạo trong ComponentStyles.Use(...); không thì theo style chung của app (đổi được lúc chạy).
    readonly ComponentStyleSheet? _pinned = ComponentStyles.Scoped;
    ComponentStyleSheet _sheet => _pinned ?? ComponentStyles.S;

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

    /// <summary>Class thêm cho nút (ví dụ <c>"w-full"</c> như SelectTrigger className của shadcn).</summary>
    public string TriggerClass { get; set; } = "";

    /// <summary>Class thêm cho danh sách bật ra (SelectContent className).</summary>
    public string ContentClass { get; set; } = "";
    public bool Disabled { get; set; }

    /// <summary>Tên truy cập (aria-label); không đặt thì dùng <see cref="Placeholder"/>.</summary>
    public string? AriaLabel { get; set; }

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
        var options = new OverlayOptions { Anchor = _anchor, Placement = Placement.BottomStart, MatchAnchorWidth = true };
        // Base UI (alignItemWithTrigger): danh sách mở đè lên nút sao cho mục đang chọn nằm đúng chỗ nút, chữ thẳng hàng.
        if (_sheet.SelectAlignItem is { } align && _index >= 0 && _window.AnchorBounds(_anchor!) is { } t)
        {
            options.Anchor = null;
            options.Offset = 0;
            options.AnchorRect = new Bounds(t.X + align.X, t.Y - align.Y - _index * align.Height - (align.Height - t.Height) / 2, t.Width, 0);
        }
        _popup = _window.OpenOverlay(_list, options);
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
        var trigger = w.Div().Class(_sheet.SelectTrigger);
        if (TriggerClass.Length > 0) trigger.Class(TriggerClass);
        return trigger
            .Disabled(Disabled)
            .Key(TriggerKey).Anchor(_anchor).Focusable().Role(SemanticRole.ComboBox).AriaState(SemanticState.Expanded, IsOpen)
            .AriaLabel(AriaLabel ?? Placeholder).AriaValue(Value)
            .OnClick(this, static (s, _) => { if (s.IsOpen) s.Close(); else s.Open(); })
            .OnKey(this, static (s, e) => s.TriggerKeyDown(e))
            .Child(w.Div().Class(_index >= 0 ? "truncate" : "truncate text-muted-foreground").Child(Value ?? Placeholder))
            .Child(w.Div().Class(_sheet.SelectChevron).Child(Icon.ChevronDown(w)));
    }

    /// <summary>Danh sách lựa chọn, vẽ trong lớp overlay; chính nó giữ focus khi mở.</summary>
    sealed class ListView(Select owner) : View
    {
        Indexed<Select>[]? _refs;

        public override Div Render(Window w)
        {
            var items = owner._items;
            var refs = Indexed<Select>.Ensure(ref _refs, owner, items.Count);
            var sheet = owner._sheet;
            var list = w.Div().Key(owner._listKey ??= "gotx-select-list-" + owner.Id.Value).Class(sheet.SelectList);
            if (owner.ContentClass.Length > 0) list.Class(owner.ContentClass);
            list.Role(SemanticRole.List)
                .Focusable().AutoFocus()
                .OnKey(owner, static (s, e) => s.ListKeyDown(e));
            for (int i = 0; i < items.Count; i++)
            {
                string key = owner.ItemKey(i);
                var row = w.Div().Key(key).Anchor(key).Class(sheet.SelectRow[i == owner._active ? 1 : 0])
                    .Role(SemanticRole.ListItem).AriaState(SemanticState.Selected, i == owner._index)
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
