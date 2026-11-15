namespace GotX.Components;

/// <summary>Bảng hàng phẳng (key, trạng thái mở) của <see cref="SidebarNav"/>, phím mũi tên và menu của nút phụ.</summary>
public sealed partial class SidebarNav
{
    /// <summary>Một nút sau khi trải phẳng qua mọi nhóm: chuỗi key dựng một lần, trạng thái mở sống cùng bảng.</summary>
    sealed class Row
    {
        public int Flat, Group, Item, SubStart;
        public SidebarNavItem Data = null!;
        public string Key = "", ActionKey = "", ChevronKey = "";
        public string[] SubKeys = [];
        public bool Open;
    }

    Row[]? _rows;
    int[] _groupStart = [], _subRow = [];
    Indexed<SidebarNav>[]? _itemRefs, _subRefs;
    ContextMenu _menu = null!;
    string? _contentKey;
    int _menuRow = -1;
    bool _pressClosedMenu;

    /// <summary>Key của vùng cuộn chứa các nhóm.</summary>
    public string ContentKey => _contentKey ??= "gotx-sidenav-" + Id.Value;

    /// <summary>Key của nút thứ <paramref name="item"/> trong nhóm <paramref name="group"/>.</summary>
    public string ItemKey(int group, int item) => RowAt(group, item).Key;

    /// <summary>Key của mục con thứ <paramref name="sub"/>.</summary>
    public string SubItemKey(int group, int item, int sub) => RowAt(group, item).SubKeys[sub];

    /// <summary>Key (cũng là tên anchor) của nút phụ.</summary>
    public string ActionKey(int group, int item) => RowAt(group, item).ActionKey;

    Row RowAt(int group, int item)
    {
        var rows = EnsureRows();
        if ((uint)group >= (uint)_groups.Count) throw new ArgumentOutOfRangeException(nameof(group));
        if ((uint)item >= (uint)_groups[group].Items.Count) throw new ArgumentOutOfRangeException(nameof(item));
        return rows[_groupStart[group] + item];
    }

    /// <summary>Dựng bảng hàng khi <see cref="Groups"/> đổi (cần Id của view, nên dựng lười sau khi gắn vào app).</summary>
    Row[] EnsureRows()
    {
        if (_rows is not null) return _rows;
        var rows = new List<Row>();
        var subRow = new List<int>();
        _groupStart = new int[_groups.Count];
        string prefix = KeyPrefix ?? ContentKey;
        for (int g = 0; g < _groups.Count; g++)
        {
            _groupStart[g] = rows.Count;
            var items = _groups[g].Items;
            for (int i = 0; i < items.Count; i++)
            {
                var data = items[i];
                var row = new Row
                {
                    Flat = rows.Count, Group = g, Item = i, SubStart = subRow.Count, Data = data, Open = data.DefaultOpen,
                    Key = $"{prefix}-{g}-{i}", ActionKey = $"{prefix}-{g}-{i}-action", ChevronKey = $"{prefix}-{g}-{i}-chevron",
                    SubKeys = new string[data.SubItems.Count],
                };
                for (int s = 0; s < row.SubKeys.Length; s++)
                {
                    row.SubKeys[s] = $"{prefix}-{g}-{i}-{s}";
                    subRow.Add(row.Flat);
                }
                rows.Add(row);
            }
        }
        _subRow = [.. subRow];
        return _rows = [.. rows];
    }

    /// <summary>
    /// Menu của nút phụ dùng lại ContextMenu (mở tại một điểm, tự lật khi sát mép, trả focus về nút vừa bấm khi đóng).
    /// Nó được nhúng vào cây với vùng nhận chuột rỗng chỉ để biết Window.
    /// </summary>
    protected override void OnCreated()
    {
        _menu = App.New(new ContextMenu { Area = static w => w.Div().Class("absolute size-0") });
        App.Subscribe<MenuItemSelected>(_menu, e =>
        {
            if (_rows is null || (uint)_menuRow >= (uint)_rows.Length) return;
            var row = _rows[_menuRow];
            Emit(new MenuSelected(row.Group, row.Item, row.Data, e.Item));
        });
        App.Subscribe<ContextMenu.OpenChanged>(_menu, e => { if (!e.Open) MenuClosed(); });
    }

    void InvokeAction(int flat)
    {
        var row = _rows![flat];
        if (row.Data.Menu is not { } items)
        {
            Emit(new ActionInvoked(row.Group, row.Item, row.Data, row.ActionKey));
            return;
        }
        if (_window?.AnchorBounds(row.ActionKey) is not { } at) return;
        _menu.Items = items;
        _menuRow = flat;
        _menu.OpenAt(new Point(at.Right + 4, at.Y));
        Notify();
    }

    void MenuClosed()
    {
        if (!_menu.IsOpen) _menuRow = -1;
        Notify();
    }

    /// <summary>
    /// OnDrag chỉ để biết lúc NHẤN chuột xuống nút phụ: nhấn ngoài overlay vừa đóng menu của chính nút này (sự kiện đóng
    /// còn nằm trong hàng đợi nên _menuRow chưa bị xoá) thì cú click lúc thả không mở lại – bấm lần hai là đóng, như Radix.
    /// </summary>
    void ActionPressed(int flat, DragEvent e)
    {
        if (e.Phase == DragPhase.Start) _pressClosedMenu = _menuRow == flat && !_menu.IsOpen;
    }

    void ActionClicked(int flat)
    {
        bool closedByThisPress = _pressClosedMenu;
        _pressClosedMenu = false;
        if (!closedByThisPress) InvokeAction(flat);
    }

    /// <summary>
    /// Enter/Space (và ↓ khi có menu) trên nút phụ. Xử lý ở OnKey thay vì để Core đổi thành click, để phân biệt được với
    /// cú bấm chuột (xem <see cref="ActionPressed"/>).
    /// </summary>
    bool ActionKeyDown(int flat, KeyEvent e)
    {
        bool opens = e.Key is Key.Enter or Key.Space || (e.Key == Key.Down && _rows![flat].Data.Menu is not null);
        if (opens) InvokeAction(flat);
        return opens;
    }

    /// <summary>→ mở, ← đóng nút gập được (như treeview); phím khác để Core xử lý (Enter/Space = bấm).</summary>
    bool ItemKeyDown(int flat, KeyEvent e)
    {
        var row = _rows![flat];
        if (_collapsed || e.Key is not (Key.Left or Key.Right)) return false;
        SetOpen(row.Group, row.Item, e.Key == Key.Right);
        return true;
    }

    /// <summary>← trên mục con: đưa focus về nút cha (bấm ← lần nữa để đóng).</summary>
    bool SubKeyDown(int subFlat, KeyEvent e)
    {
        if (e.Key != Key.Left || _window is null) return false;
        _window.FocusKey(_rows![_subRow[subFlat]].Key);
        return true;
    }
}
