namespace GotX.Components;

/// <summary>Một nút trong menu sidebar. <paramref name="Icon"/> (vd. <c>Icon.Search</c>) null thì dùng ô chữ cái đầu của nhãn.</summary>
public sealed record SidebarItem(string Label, Func<Window, Div>? Icon = null)
{
    /// <summary>Chữ cái đầu, tính một lần để không cắt chuỗi mỗi frame.</summary>
    public string Initial { get; } = Label.Length > 0 ? Label[..1].ToUpperInvariant() : "";
}

/// <summary>Nhóm menu có nhãn (nhãn ẩn khi sidebar thu gọn).</summary>
public sealed record SidebarGroup(string Label, IReadOnlyList<SidebarItem> Items);

/// <summary>
/// Sidebar ứng dụng: header, các nhóm menu cuộn được, footer; thu gọn còn cột icon (3rem). Nút đang chọn được tô nền.
/// Nút menu và nút thu gọn focus được bằng Tab (vòng focus sidebar-ring), Enter/Space kích hoạt.
/// Phần còn lại của shadcn bị bỏ: chế độ sheet trên mobile, cookie lưu trạng thái, phím tắt Ctrl+B (Core chưa có phím chữ B
/// và phím tắt toàn cục), tooltip khi thu gọn, menu con/badge/action, và animation bề rộng (layout không có transition).
/// </summary>
public sealed partial class Sidebar : View
{
    IReadOnlyList<SidebarGroup> _groups = [];
    bool _collapsed;
    int _active = -1, _itemCount;
    string[] _keys = [];
    Indexed<Sidebar>[]? _refs;
    string? _key, _contentKey;

    /// <summary>Phát khi thu gọn/mở rộng.</summary>
    public readonly record struct CollapsedChanged(bool Collapsed);

    /// <summary>Phát khi chọn một nút menu: chỉ số nhóm, chỉ số trong nhóm, nhãn.</summary>
    public readonly record struct ItemSelected(int Group, int Item, string Label);

    /// <summary>Các nhóm menu.</summary>
    public IReadOnlyList<SidebarGroup> Groups
    {
        get => _groups;
        set
        {
            _groups = value;
            _itemCount = 0;
            foreach (var g in value) _itemCount += g.Items.Count;
            if (_active >= _itemCount) _active = -1;
            Notify();
        }
    }

    /// <summary>Phần đầu (logo, chọn workspace). Đọc <see cref="Collapsed"/> nếu cần đổi cách vẽ khi thu gọn.</summary>
    public Func<Window, Div>? Header { get; set; }

    /// <summary>Phần chân (tài khoản người dùng).</summary>
    public Func<Window, Div>? Footer { get; set; }

    /// <summary>Thu gọn còn cột icon.</summary>
    public bool Collapsed
    {
        get => _collapsed;
        set
        {
            if (_collapsed == value) return;
            _collapsed = value;
            Notify();
            Emit(new CollapsedChanged(value));
        }
    }

    /// <summary>Chỉ số phẳng (đếm qua mọi nhóm) của nút đang chọn; -1 = không có.</summary>
    public int ActiveIndex
    {
        get => _active;
        set
        {
            if (value < -1 || value >= _itemCount) throw new ArgumentOutOfRangeException(nameof(value));
            if (_active == value) return;
            _active = value;
            Notify();
            if (value < 0) return;
            var (g, i) = Locate(value);
            Emit(new ItemSelected(g, i, _groups[g].Items[i].Label));
        }
    }

    /// <summary>Đảo trạng thái thu gọn (hành vi của nút trigger).</summary>
    public void Toggle() => Collapsed = !Collapsed;

    /// <summary>Key của khung sidebar (đo bề rộng trong kiểm tra).</summary>
    public string Key => _key ??= "gotx-sidebar-" + Id.Value;

    /// <summary>Key nút menu thứ <paramref name="flatIndex"/> (đếm qua mọi nhóm).</summary>
    public string ItemKey(int flatIndex) => EnsureKeys()[flatIndex];

    string[] EnsureKeys()
    {
        if (_keys.Length == _itemCount) return _keys;
        _keys = new string[_itemCount];
        for (int i = 0; i < _itemCount; i++) _keys[i] = $"gotx-sidebar-{Id.Value}-item-{i}";
        return _keys;
    }

    (int Group, int Item) Locate(int flat)
    {
        for (int g = 0; g < _groups.Count; g++)
        {
            if (flat < _groups[g].Items.Count) return (g, flat);
            flat -= _groups[g].Items.Count;
        }
        throw new ArgumentOutOfRangeException(nameof(flat));
    }

    /// <summary>Vẽ khung sidebar: header, nội dung cuộn (các nhóm), footer.</summary>
    public override Div Render(Window w)
    {
        var keys = EnsureKeys();
        var refs = Indexed<Sidebar>.Ensure(ref _refs, this, _itemCount);
        var root = w.Div().Key(Key).Class(_collapsed ? RootCollapsed : RootExpanded);
        if (Header is { } header) root.Child(w.Div().Class("flex flex-col gap-2 p-2").Child(header(w)));

        var content = w.Div().Key(_contentKey ??= "gotx-sidebar-" + Id.Value + "-content")
            .Class(_collapsed ? "flex min-h-0 flex-1 flex-col overflow-hidden" : "flex min-h-0 flex-1 flex-col overflow-y-auto");
        int flat = 0;
        foreach (var group in _groups)
        {
            var g = w.Div().Class("relative flex w-full min-w-0 flex-col p-2");
            if (!_collapsed) g.Child(w.Div().Class(GroupLabelClass).Child(group.Label));
            var menu = w.Div().Class("flex w-full min-w-0 flex-col gap-0.5");
            foreach (var item in group.Items)
            {
                menu.Child(MenuButton(w, item, flat == _active)
                    .Key(keys[flat]).Focusable()
                    .OnClick(refs[flat], static (r, _) => r.Owner.ActiveIndex = r.Index));
                flat++;
            }
            content.Child(g.Child(menu));
        }
        root.Child(content);

        if (Footer is { } footer) root.Child(w.Div().Class("flex flex-col gap-2 p-2").Child(footer(w)));
        return root;
    }
}
