namespace GotX.Components;

/// <summary>
/// Nhóm nút toggle dính nhau (toggle-group.tsx). <see cref="Multiple"/> = false: tối đa một nút bật (bấm lại nút
/// đang bật thì tắt, như Radix); true: mỗi nút bật/tắt độc lập. Bàn phím: cả nhóm là một điểm dừng Tab (trở lại nút
/// focus lần trước), ←/→ (và ↑/↓, Home/End) chuyển focus giữa các nút và quay vòng, Space/Enter bật/tắt nút đang focus.
/// </summary>
public sealed class ToggleGroup : View
{
    // Bảng ghim lúc tạo nếu được tạo trong ComponentStyles.Use(...); không thì theo style chung của app.
    readonly ComponentStyleSheet? _pinned = ComponentStyles.Scoped;
    ComponentStyleSheet _sheet => _pinned ?? ComponentStyles.S;

    IReadOnlyList<string> _items = [];
    bool[] _on = [];
    Indexed<ToggleGroup>[]? _refs;
    RovingFocus? _roving;
    Window? _window;
    int _home; // nút giữ focus lần gần nhất: nơi Tab quay lại, như roving tabindex

    /// <summary>Tập nút đang bật vừa đổi; <see cref="Selected"/> là chỉ số các nút bật, tăng dần.</summary>
    public readonly record struct ValueChanged(IReadOnlyList<int> Selected);

    /// <summary>Nhãn các nút.</summary>
    public IReadOnlyList<string> Items
    {
        get => _items;
        set
        {
            _items = value;
            Array.Resize(ref _on, value.Count);
            Notify();
        }
    }

    /// <summary>Cho phép bật nhiều nút cùng lúc.</summary>
    public bool Multiple { get; set; }

    /// <summary>Kiểu nút.</summary>
    public ToggleVariant Variant { get; set; }

    /// <summary>Cỡ nút.</summary>
    public ToggleSize Size { get; set; }

    /// <summary>Vô hiệu hoá cả nhóm.</summary>
    public bool Disabled { get; set; }

    /// <summary>Nút thứ <paramref name="index"/> có đang bật không.</summary>
    public bool IsPressed(int index) => _on[index];

    /// <summary>Chỉ số các nút đang bật, tăng dần.</summary>
    public IReadOnlyList<int> Selected
    {
        get
        {
            var list = new List<int>();
            for (int i = 0; i < _on.Length; i++) if (_on[i]) list.Add(i);
            return list;
        }
    }

    /// <summary>Bật hoặc tắt nút <paramref name="index"/>; ở chế độ một nút, bật nút này sẽ tắt các nút khác.</summary>
    public void SetPressed(int index, bool pressed)
    {
        if ((uint)index >= (uint)_on.Length) throw new ArgumentOutOfRangeException(nameof(index));
        if (_on[index] == pressed) return;
        if (pressed && !Multiple) Array.Clear(_on);
        _on[index] = pressed;
        Notify();
        Emit(new ValueChanged(Selected));
    }

    void Flip(int index) { if (!Disabled) SetPressed(index, !_on[index]); }

    bool OnItemKey(int index, KeyEvent e)
    {
        if (_window is not { } w || _roving is not { } roving) return false;
        if (e.Key == Key.Tab) { roving.LeaveOnTab(w, _items.Count, e.Shift); return false; }
        int next = RovingFocus.Step(e.Key, index, _items.Count);
        if (next < 0) return false; // Space/Enter: Core gọi OnClick của nút đang focus
        roving.Focus(w, next);
        return true;
    }

    public override Div Render(Window w)
    {
        _window = w;
        int n = _items.Count;
        var roving = _roving ??= new RovingFocus("gotx-toggle-group-" + Id.Value);
        if (roving.Sync(w, n, Math.Min(_home, n - 1)) is >= 0 and var at) _home = at;
        var refs = Indexed<ToggleGroup>.Ensure(ref _refs, this, n);
        var sheet = _sheet;
        var group = w.Div().Class(sheet.ToggleGroup).Role(SemanticRole.Group);
        for (int i = 0; i < n; i++)
        {
            var item = w.Div().Class(ToggleStyles.Get(sheet, Variant, Size, _on[i], ToggleStyles.Position(i, n)));
            if (sheet.ToggleGroupItem.Length > 0) item.Class(sheet.ToggleGroupItem);
            group.Child(item.Role(SemanticRole.Button).AriaState(SemanticState.Checked, _on[i])
                .Key(roving.Key(i)).Focusable()
                .OnKey(refs[i], static (r, e) => r.Owner.OnItemKey(r.Index, e))
                .Disabled(Disabled)
                .OnClick(refs[i], static (r, _) => r.Owner.Flip(r.Index))
                .Child(_items[i]));
        }
        return group;
    }
}
