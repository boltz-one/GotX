using System.Text;

namespace GotX.Components;

/// <summary>Các control con của <see cref="DataTable{T}"/> (ô lọc, menu cột, menu số hàng, menu thao tác hàng), key và chữ dựng sẵn.</summary>
public sealed partial class DataTable<T>
{
    readonly List<(MenuItem Item, DataColumn<T> Column)> _columnItems = new();
    Input? _filterInput;
    DropdownMenu? _columnsMenu, _sizeMenu;
    ContextMenu? _rowMenu;
    IReadOnlyList<int> _pageSizes = [10, 20, 30, 40, 50];
    Indexed<DataTable<T>>[]? _rowRefs, _columnRefs;
    Window? _window;
    string[] _rowKeys = [], _checkKeys = [], _actionKeys = [], _headKeys = [], _pagerKeys = [];
    string? _grid, _pageText, _selectionText, _sizeText;
    int _menuRow = -1, _sizeShown;
    bool _gridSelectable, _gridActions;

    /// <summary>Các lựa chọn "Rows per page".</summary>
    public IReadOnlyList<int> PageSizes { get => _pageSizes; set { _pageSizes = value; if (_sizeMenu is not null) _sizeMenu.Items = SizeItems(); } }

    public string FilterPlaceholder { get; set; } = "Filter...";
    public string ColumnsLabel { get; set; } = "Columns";
    public string RowsPerPageLabel { get; set; } = "Rows per page";
    public string EmptyText { get; set; } = "No results.";

    /// <summary>Hiện hàng công cụ (ô lọc + nút cột). Tắt đi để tự đặt <see cref="FilterInput"/> / <see cref="ColumnsMenu"/> ở chỗ khác.</summary>
    public bool ShowToolbar { get; set; } = true;

    /// <summary>Hiện hàng phân trang dưới bảng.</summary>
    public bool ShowPagination { get; set; } = true;

    /// <summary>Nội dung thêm ở bên phải hàng công cụ, trước nút cột (nút "Add"…).</summary>
    public Func<Window, Div>? ToolbarExtra { get; set; }

    /// <summary>Class thêm cho hàng tiêu đề (dashboard-01 dùng <c>bg-muted</c>).</summary>
    public string HeaderRowClass { get; set; } = "";

    /// <summary>Class khung bao quanh bảng.</summary>
    public string FrameClass { get; set; } = "overflow-hidden rounded-md border border-border";

    /// <summary>Ô lọc – để focus, gõ trong kiểm tra, hoặc tự đặt vào hàng công cụ riêng.</summary>
    public Input FilterInput => _filterInput ?? throw new InvalidOperationException("Component chưa được tạo qua App.New(...).");

    /// <summary>Menu ẩn/hiện cột (nút "Columns").</summary>
    public DropdownMenu ColumnsMenu => _columnsMenu ?? throw new InvalidOperationException("Component chưa được tạo qua App.New(...).");

    /// <summary>Menu chọn số hàng mỗi trang.</summary>
    public DropdownMenu PageSizeMenu => _sizeMenu ?? throw new InvalidOperationException("Component chưa được tạo qua App.New(...).");

    /// <summary>Menu thao tác dùng chung cho mọi hàng – đọc trạng thái mở, dòng đang tô sáng.</summary>
    public ContextMenu RowMenu => _rowMenu ?? throw new InvalidOperationException("Component chưa được tạo qua App.New(...).");

    public string RowKey(int index) => Name(ref _rowKeys, index, "row");
    public string RowCheckKey(int index) => Name(ref _checkKeys, index, "check");
    public string RowActionKey(int index) => Name(ref _actionKeys, index, "action");
    public string SelectAllKey => Name(ref _pagerKeys, 0, "ctl");
    public string FirstPageKey => Name(ref _pagerKeys, 1, "ctl");
    public string PreviousPageKey => Name(ref _pagerKeys, 2, "ctl");
    public string NextPageKey => Name(ref _pagerKeys, 3, "ctl");
    public string LastPageKey => Name(ref _pagerKeys, 4, "ctl");
    public string EmptyKey => Name(ref _pagerKeys, 5, "ctl");

    /// <summary>Key ô tiêu đề của cột <paramref name="columnId"/> (cột sắp xếp được: chính là nút sắp xếp).</summary>
    public string HeaderKey(string columnId)
    {
        for (int c = 0; c < _columns.Count; c++) if (_columns[c].Id == columnId) return Name(ref _headKeys, c, "head");
        throw new ArgumentException($"Không có cột \"{columnId}\".", nameof(columnId));
    }

    // Chuỗi key tạo lần đầu cần tới rồi giữ lại: không nối chuỗi mỗi lần vẽ, và không tạo sẵn cho hàng chưa từng hiện.
    string Name(ref string[] cache, int index, string kind)
    {
        if (index >= cache.Length) Array.Resize(ref cache, Math.Max(index + 1, Math.Max(8, cache.Length * 2)));
        return cache[index] ??= $"gotx-datatable-{Id.Value}-{kind}-{index}";
    }

    string PageText => _pageText ??= $"Page {_page + 1} of {PageCount}";
    string SelectionText => _selectionText ??= $"{_selectedCount} of {_count} row(s) selected.";

    protected override void OnCreated()
    {
        _filterInput = App.New(new Input { Placeholder = FilterPlaceholder, Text = _filter });
        App.Subscribe<TextEntry.TextChanged>(_filterInput, e => Filter = e.Text);

        _columnsMenu = App.New(new DropdownMenu { Trigger = ColumnsTrigger, Placement = Placement.BottomEnd });
        RebuildColumnsMenu();
        App.Subscribe<MenuItemToggled>(_columnsMenu, e =>
        {
            foreach (var (item, column) in _columnItems)
                if (ReferenceEquals(item, e.Item)) { SetColumnVisible(column.Id, e.Checked); return; }
        });

        _sizeMenu = App.New(new DropdownMenu { Trigger = SizeTrigger, Items = SizeItems(), MatchTriggerWidth = true });
        App.Subscribe<MenuItemSelected>(_sizeMenu, e => PageSize = _pageSizes[e.Index]);

        // Một menu dùng chung cho mọi hàng thay vì một DropdownMenu mỗi hàng (mỗi cái là một entity + một view).
        _rowMenu = App.New(new ContextMenu { Area = static w => w.Div().Class("absolute size-0") });
        App.Subscribe<MenuItemSelected>(_rowMenu, e =>
        {
            if ((uint)_menuRow < (uint)_items.Count) Emit(new RowAction(_menuRow, _items[_menuRow], e.Item));
        });
    }

    MenuItem[] SizeItems()
    {
        var items = new MenuItem[_pageSizes.Count];
        for (int i = 0; i < items.Length; i++) items[i] = MenuItem.Action(_pageSizes[i].ToString());
        return items;
    }

    void RebuildColumnsMenu()
    {
        _columnItems.Clear();
        var items = new List<MenuItem>();
        foreach (var column in _columns)
        {
            if (!column.CanHide) continue;
            var item = MenuItem.Check(column.Header, column.Visible);
            items.Add(item);
            _columnItems.Add((item, column));
        }
        if (_columnsMenu is not null) _columnsMenu.Items = items;
    }

    Div ColumnsTrigger(Window w) =>
        w.Div().Class(DataTableParts.ColumnsTrigger).Child(DataTableParts.ColumnsIcon(w)).Child(ColumnsLabel).Child(Icon.ChevronDown(w));

    Div SizeTrigger(Window w)
    {
        if (_sizeText is null || _sizeShown != _pageSize) _sizeText = (_sizeShown = _pageSize).ToString();
        return w.Div().Class(DataTableParts.SizeTrigger).Child(_sizeText).Child(w.Div().Class("text-muted-foreground").Child(Icon.ChevronDown(w)));
    }

    /// <summary>Mở menu thao tác của hàng <paramref name="index"/> ngay dưới nút "⋯" của nó (tự trượt vào trong cửa sổ khi sát mép).</summary>
    public void OpenRowMenu(int index)
    {
        if (RowActions is not { } actions || _rowMenu is null || _window?.AnchorBounds(RowActionKey(index)) is not { } at) return;
        _rowMenu.Items = actions;
        _menuRow = index;
        _rowMenu.OpenAt(new Point(at.X, at.Bottom + 4));
    }

    /// <summary>Mẫu cột của lưới, dùng chung cho hàng tiêu đề và mọi hàng dữ liệu; dựng lại khi tập cột hiện đổi.</summary>
    string Grid()
    {
        if (_grid is not null && _gridSelectable == Selectable && _gridActions == RowActions is not null) return _grid;
        _gridSelectable = Selectable;
        _gridActions = RowActions is not null;
        var sb = new StringBuilder("grid-cols-[");
        if (Selectable) sb.Append("40px_");
        foreach (var column in _columns) if (column.Visible) sb.Append(column.Width).Append('_');
        if (RowActions is not null) sb.Append("48px_");
        if (sb[^1] == '_') sb.Length--; else sb.Append("1fr");
        return _grid = sb.Append(']').ToString();
    }
}
