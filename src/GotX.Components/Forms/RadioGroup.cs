namespace GotX.Components;

/// <summary>
/// Nhóm nút chọn một (radio-group.tsx): chọn mục này thì bỏ mục kia. Bàn phím như Radix/Base UI: cả nhóm là một
/// điểm dừng Tab (focus vào mục đang chọn, chưa chọn thì mục đầu), mũi tên chuyển cả focus lẫn lựa chọn và quay vòng,
/// Space chọn mục đang focus. Khác bản gốc: Enter cũng chọn (Core coi Enter như Space trên phần tử focus).
/// </summary>
public sealed class RadioGroup : View
{
    IReadOnlyList<string> _items = [];
    int _index = -1;
    Indexed<RadioGroup>[]? _refs;
    RovingFocus? _roving;
    Window? _window;

    /// <summary>Mục được chọn vừa đổi.</summary>
    public readonly record struct ValueChanged(int Index, string Value);

    const string Ring = "relative flex size-4 shrink-0 items-center justify-center rounded-full border border-input dark:bg-input/30";
    const string RingOn = "relative flex size-4 shrink-0 items-center justify-center rounded-full border border-primary bg-primary";
    // [đã chọn * 2 + đang focus bằng phím]: vòng focus vẽ quanh nút tròn, dù phần tử focus là cả hàng (nút + nhãn).
    static readonly string[] Rings = [Ring, Ring + " border-ring ring-3 ring-ring/50", RingOn, RingOn + " ring-3 ring-ring/50"];
    const string Row = "flex items-center gap-2 cursor-pointer disabled:opacity-50";

    /// <summary>Nhãn các lựa chọn.</summary>
    public IReadOnlyList<string> Items
    {
        get => _items;
        set { _items = value; if (_index >= value.Count) _index = -1; Notify(); }
    }

    /// <summary>Chỉ số đang chọn, -1 = chưa chọn.</summary>
    public int SelectedIndex
    {
        get => _index;
        set
        {
            if (value < -1 || value >= _items.Count) throw new ArgumentOutOfRangeException(nameof(value));
            if (_index == value) return;
            _index = value;
            Notify();
            if (value >= 0) Emit(new ValueChanged(value, _items[value]));
        }
    }

    /// <summary>Nhãn đang chọn, null nếu chưa chọn.</summary>
    public string? Value => _index >= 0 ? _items[_index] : null;

    /// <summary>Vô hiệu hoá cả nhóm.</summary>
    public bool Disabled { get; set; }

    /// <summary>Xếp các mục theo hàng ngang thay vì cột.</summary>
    public bool Horizontal { get; set; }

    void Pick(int index) { if (!Disabled) SelectedIndex = index; }

    bool OnItemKey(int index, KeyEvent e)
    {
        if (_window is not { } w || _roving is not { } roving || Disabled) return false;
        if (e.Key == Key.Tab) { roving.LeaveOnTab(w, _items.Count, e.Shift); return false; }
        int next = RovingFocus.Step(e.Key, index, _items.Count);
        if (next < 0) return false;
        SelectedIndex = next;
        roving.Focus(w, next);
        return true;
    }

    public override Div Render(Window w)
    {
        _window = w;
        var roving = _roving ??= new RovingFocus("gotx-radio-" + Id.Value);
        roving.Sync(w, _items.Count, Math.Max(_index, 0));
        var refs = Indexed<RadioGroup>.Ensure(ref _refs, this, _items.Count);
        var group = w.Div().Class(Horizontal ? "flex flex-wrap items-center gap-4" : "flex flex-col gap-3");
        for (int i = 0; i < _items.Count; i++)
        {
            bool on = i == _index;
            var ring = w.Div().Class(Rings[(on ? 2 : 0) + (roving.ShowsRing(w, i) ? 1 : 0)]);
            if (on) ring.Child(w.Div().Class("size-2 rounded-full bg-primary-foreground"));
            group.Child(w.Div().Class(Row).Disabled(Disabled)
                .Key(roving.Key(i)).Focusable()
                .OnKey(refs[i], static (r, e) => r.Owner.OnItemKey(r.Index, e))
                .OnClick(refs[i], static (r, _) => r.Owner.Pick(r.Index))
                .Child(ring)
                .Child(w.Div().Class("text-sm font-medium leading-none").Child(_items[i])));
        }
        return group;
    }
}
