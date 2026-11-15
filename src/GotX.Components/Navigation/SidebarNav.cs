namespace GotX.Components;

/// <summary>
/// Phần menu của sidebar shadcn (SidebarContent → SidebarGroup → SidebarMenu): nhóm có nhãn, nút có icon / badge /
/// nút phụ kèm menu, mục con gập được (mũi tên xoay) hoặc luôn mở, thu gọn còn cột icon. Khung bao quanh là các hàm
/// tĩnh (<see cref="Frame"/>, <see cref="Header"/>, <see cref="Content"/>, <see cref="Footer"/>, <see cref="Inset"/>,
/// <see cref="Trigger"/>) để block tự xếp header / footer của mình; trạng thái thu gọn do nơi dùng giữ.
/// <code>
/// _nav = App.New(new SidebarNav { Groups = [new("Platform", [new("Playground", SidebarIcons.SquareTerminal) { SubItems = ["History"] }])] });
/// App.Subscribe&lt;SidebarNav.ItemSelected&gt;(_nav, e =&gt; …);
/// SidebarNav.Frame(w, _collapsed).Child(SidebarNav.Header(w).Child(…)).Child(SidebarNav.Content(w, _nav))
/// </code>
/// Bàn phím: mọi nút là điểm dừng Tab (vòng focus sidebar-ring), Enter/Space kích hoạt; → mở, ← đóng nút gập được;
/// ← trên mục con đưa focus về nút cha. Khác shadcn: không có tooltip khi thu gọn, không có hiệu ứng chiều cao khi mở.
/// </summary>
public sealed partial class SidebarNav : View
{
    IReadOnlyList<SidebarNavGroup> _groups = [];
    bool _collapsed;
    int _activeRow = -1, _activeSub = -1;
    Window? _window;

    /// <summary>Phát khi chọn một nút (<paramref name="Sub"/> = -1) hoặc một mục con.</summary>
    public readonly record struct ItemSelected(int Group, int Item, int Sub, string Title);

    /// <summary>Phát khi danh sách con của một nút mở / đóng.</summary>
    public readonly record struct OpenChanged(int Group, int Item, bool Open);

    /// <summary>Phát khi bấm nút phụ của một nút không có <see cref="SidebarNavItem.Menu"/>; <paramref name="Anchor"/> để neo overlay.</summary>
    public readonly record struct ActionInvoked(int Group, int Item, SidebarNavItem Target, string Anchor);

    /// <summary>Phát khi chọn một dòng trong menu của nút phụ.</summary>
    public readonly record struct MenuSelected(int Group, int Item, SidebarNavItem Target, MenuItem Choice);

    /// <summary>Các nhóm nút. Gán lại thì trạng thái mở và mục đang chọn trở về ban đầu.</summary>
    public IReadOnlyList<SidebarNavGroup> Groups
    {
        get => _groups;
        set { _groups = value; _rows = null; _activeRow = _activeSub = -1; Notify(); }
    }

    /// <summary>Thu gọn còn icon: ẩn nhãn nhóm, chữ, badge, nút phụ, danh sách con; nút thành ô 32px.</summary>
    public bool Collapsed
    {
        get => _collapsed;
        set { if (_collapsed == value) return; _collapsed = value; Notify(); }
    }

    /// <summary>Nút / mục con đang chọn; (-1, -1, -1) khi chưa có.</summary>
    public (int Group, int Item, int Sub) Active =>
        _activeRow < 0 ? (-1, -1, -1) : (EnsureRows()[_activeRow].Group, EnsureRows()[_activeRow].Item, _activeSub);

    /// <summary>Đặt mục đang chọn mà không phát sự kiện (trạng thái ban đầu). <paramref name="item"/> âm = bỏ chọn.</summary>
    public void SetActive(int group, int item, int sub = -1)
    {
        int row = item < 0 ? -1 : RowAt(group, item).Flat;
        if (row >= 0 && sub >= _rows![row].Data.SubItems.Count) throw new ArgumentOutOfRangeException(nameof(sub));
        if (row < 0 || sub < -1) sub = -1;
        if (_activeRow == row && _activeSub == sub) return;
        _activeRow = row; _activeSub = sub;
        Notify();
    }

    /// <summary>Chọn một nút hoặc mục con và phát <see cref="ItemSelected"/> (hành vi khi bấm).</summary>
    public void Select(int group, int item, int sub = -1)
    {
        SetActive(group, item, sub);
        var data = RowAt(group, item).Data;
        Emit(new ItemSelected(group, item, sub, sub < 0 ? data.Title : data.SubItems[sub]));
    }

    /// <summary>Danh sách con của nút đang mở hay không.</summary>
    public bool IsOpen(int group, int item) => RowAt(group, item).Open;

    /// <summary>Mở / đóng danh sách con; phát <see cref="OpenChanged"/> khi đổi.</summary>
    public void SetOpen(int group, int item, bool open)
    {
        var row = RowAt(group, item);
        if (row.Open == open) return;
        row.Open = open;
        Notify();
        Emit(new OpenChanged(group, item, open));
    }

    /// <summary>Đảo trạng thái mở của danh sách con.</summary>
    public void Toggle(int group, int item) => SetOpen(group, item, !IsOpen(group, item));

    /// <summary>Menu của nút phụ đang mở hay không (đọc trong kiểm tra).</summary>
    public bool IsMenuOpen => _menu is { IsOpen: true };

    /// <summary>Menu dùng chung cho mọi nút phụ – đọc dòng đang tô sáng, vị trí overlay.</summary>
    public ContextMenu ActionMenu => _menu;

    /// <summary>Bấm một nút: nút gập được thì đảo trạng thái mở (khi sidebar thu gọn thì coi như nút thường), còn lại là chọn.</summary>
    void Activate(int flat)
    {
        var row = _rows![flat];
        bool collapsible = row.Data.SubItems.Count > 0 && _groups[row.Group].SubMenus == SidebarSubMenus.Collapsible;
        if (collapsible && !_collapsed) Toggle(row.Group, row.Item);
        else Select(row.Group, row.Item);
    }

    void ActivateSub(int subFlat)
    {
        var row = _rows![_subRow[subFlat]];
        Select(row.Group, row.Item, subFlat - row.SubStart);
    }
}
