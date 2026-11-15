namespace GotX.Components;

/// <summary>
/// Bảng dữ liệu (trang "Data Table" của shadcn/ui – công thức TanStack Table – và bảng của block dashboard-01): cột có
/// kiểu, bấm tiêu đề để sắp xếp, chọn hàng bằng ô đánh dấu (ô ở tiêu đề chọn cả trang, hiện gạch ngang khi mới chọn một
/// phần), lọc chữ theo một cột, phân trang, ẩn/hiện cột bằng menu, menu thao tác từng hàng, dòng "No results.".
/// Hàng được nhận diện bằng chỉ số trong <see cref="Items"/>: key của hàng/ô đánh dấu/nút menu đi theo dữ liệu nên focus và
/// lựa chọn không nhảy sang hàng khác khi sắp xếp, lọc hay đổi trang. Thứ tự hiển thị chỉ tính lại khi dữ liệu, bộ lọc
/// hoặc cách sắp xếp đổi. Không có kéo thả để đổi thứ tự hàng/cột, ghim cột, đổi bề rộng cột bằng chuột.
/// <code>
/// _table = App.New(new DataTable&lt;Payment&gt; { Columns = cols, Items = payments, Selectable = true, FilterColumn = "email",
///     RowActions = [MenuItem.Action("Copy payment ID"), MenuItem.Separator(), MenuItem.Action("View customer")] });
/// App.Subscribe&lt;DataTable&lt;Payment&gt;.RowAction&gt;(_table, e =&gt; …);
/// </code>
/// </summary>
public sealed partial class DataTable<T> : View
{
    IReadOnlyList<T> _items = [];
    IReadOnlyList<DataColumn<T>> _columns = [];
    bool[] _selected = [];
    int[] _order = [];
    int _count, _selectedCount, _page, _pageSize = 10, _sortColumn = -1;
    SortDirection _sortDirection;
    string _filter = "";
    bool _stale = true;

    /// <summary>Số hàng được chọn vừa đổi.</summary>
    public readonly record struct SelectionChanged(int Count);

    /// <summary>Cách sắp xếp vừa đổi; <see cref="ColumnId"/> null khi bỏ sắp xếp.</summary>
    public readonly record struct SortChanged(string? ColumnId, SortDirection Direction);

    /// <summary>Trang hiện tại (từ 1) hoặc tổng số trang vừa đổi.</summary>
    public readonly record struct PageChanged(int Page, int PageCount);

    /// <summary>Chữ lọc vừa đổi.</summary>
    public readonly record struct FilterChanged(string Text);

    /// <summary>Một cột vừa được ẩn/hiện.</summary>
    public readonly record struct ColumnVisibilityChanged(string ColumnId, bool Visible);

    /// <summary>Một dòng trong menu thao tác của hàng <see cref="Index"/> (chỉ số trong Items) vừa được chọn.</summary>
    public readonly record struct RowAction(int Index, T Item, MenuItem Action);

    /// <summary>Dữ liệu. Gán danh sách mới thì lựa chọn bị xoá và bảng về trang 1 (không sửa tại chỗ danh sách đã gán).</summary>
    public IReadOnlyList<T> Items
    {
        get => _items;
        set
        {
            _items = value;
            _selected = new bool[value.Count];
            _selectedCount = 0;
            _page = 0;
            Invalidate();
        }
    }

    /// <summary>Các cột theo thứ tự hiển thị.</summary>
    public IReadOnlyList<DataColumn<T>> Columns
    {
        get => _columns;
        set { _columns = value; _sortColumn = -1; _sortDirection = SortDirection.None; _grid = null; RebuildColumnsMenu(); Invalidate(); }
    }

    /// <summary>Có cột ô đánh dấu để chọn hàng.</summary>
    public bool Selectable { get; set; }

    /// <summary>Id cột dùng để lọc chữ (cột phải có <c>Text</c>); null = không có ô lọc.</summary>
    public string? FilterColumn { get; set; }

    /// <summary>Các dòng của menu "⋯" ở cuối mỗi hàng; null = không có cột thao tác. Chọn một dòng phát <see cref="RowAction"/>.</summary>
    public IReadOnlyList<MenuItem>? RowActions { get; set; }

    /// <summary>Số hàng mỗi trang.</summary>
    public int PageSize
    {
        get => _pageSize;
        set
        {
            value = Math.Max(1, value);
            if (_pageSize == value) return;
            _pageSize = value;
            _page = 0;
            _sizeMenu?.Notify();
            PagingChanged();
        }
    }

    /// <summary>Trang hiện tại, từ 1; bị kẹp trong [1, <see cref="PageCount"/>].</summary>
    public int Page
    {
        get => _page + 1;
        set
        {
            Refresh();
            int page = Math.Clamp(value, 1, PageCount) - 1;
            if (_page == page) return;
            _page = page;
            PagingChanged();
        }
    }

    /// <summary>Tổng số trang sau khi lọc (ít nhất 1).</summary>
    public int PageCount { get { Refresh(); return Math.Max(1, (_count + _pageSize - 1) / _pageSize); } }

    /// <summary>Số hàng còn lại sau khi lọc.</summary>
    public int RowCount { get { Refresh(); return _count; } }

    /// <summary>Chỉ số (trong Items) của hàng đứng thứ <paramref name="position"/> sau khi lọc và sắp xếp.</summary>
    public int RowAt(int position) { Refresh(); return _order[position]; }

    /// <summary>Chữ đang lọc (so không phân biệt hoa thường, khớp một phần).</summary>
    public string Filter
    {
        get => _filter;
        set
        {
            value ??= "";
            if (_filter == value) return;
            _filter = value;
            _page = 0;
            if (_filterInput is { } input && input.Text != value) input.Text = value;
            Invalidate();
            Emit(new FilterChanged(value));
        }
    }

    public string? SortColumn => _sortColumn >= 0 ? _columns[_sortColumn].Id : null;
    public SortDirection SortDirection => _sortDirection;

    /// <summary>Sắp xếp theo cột <paramref name="columnId"/>; <see cref="SortDirection.None"/> trả về thứ tự gốc.</summary>
    public void Sort(string? columnId, SortDirection direction)
    {
        int column = -1;
        for (int c = 0; c < _columns.Count && columnId is not null; c++) if (_columns[c].Id == columnId) column = c;
        if (column < 0 || _columns[column].Compare is null || direction == SortDirection.None) { column = -1; direction = SortDirection.None; }
        if (column == _sortColumn && direction == _sortDirection) return;
        _sortColumn = column;
        _sortDirection = direction;
        Invalidate();
        Emit(new SortChanged(SortColumn, direction));
    }

    // Bấm tiêu đề: tăng dần → giảm dần → tăng dần… như ví dụ của shadcn (toggleSorting(isSorted === "asc")).
    void ToggleSort(int column) =>
        Sort(_columns[column].Id, _sortColumn == column && _sortDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending);

    /// <summary>Ẩn/hiện một cột (hành vi của menu cột).</summary>
    public void SetColumnVisible(string columnId, bool visible)
    {
        foreach (var column in _columns)
        {
            if (column.Id != columnId || column.Visible == visible) continue;
            column.Visible = visible;
            _grid = null;
            foreach (var (item, target) in _columnItems) if (ReferenceEquals(target, column)) item.Checked = visible;
            _columnsMenu?.Panel.Notify();
            _virtual?.Notify();
            Notify();
            Emit(new ColumnVisibilityChanged(columnId, visible));
        }
    }

    void Invalidate() { _stale = true; _pageText = _selectionText = null; Notify(); _virtual?.Notify(); }

    void PagingChanged() { _pageText = null; Notify(); Emit(new PageChanged(_page + 1, PageCount)); }

    /// <summary>Tính lại thứ tự hiển thị (lọc rồi sắp xếp) nếu có gì đổi từ lần trước; kẹp trang hiện tại.</summary>
    void Refresh()
    {
        if (!_stale) return;
        _stale = false;
        if (_order.Length < _items.Count) _order = new int[_items.Count];
        Func<T, string>? text = null;
        if (_filter.Length > 0 && FilterColumn is { } id)
            foreach (var column in _columns) if (column.Id == id) text = column.Text;
        _count = 0;
        for (int i = 0; i < _items.Count; i++)
            if (text is null || text(_items[i]).Contains(_filter, StringComparison.OrdinalIgnoreCase)) _order[_count++] = i;
        if (_sortColumn >= 0 && _columns[_sortColumn].Compare is { } compare)
        {
            var items = _items;
            int sign = _sortDirection == SortDirection.Descending ? -1 : 1;
            // So thêm chỉ số gốc để hàng bằng nhau giữ thứ tự cũ (Array.Sort không ổn định).
            Array.Sort(_order, 0, _count, Comparer<int>.Create((a, b) =>
            {
                int c = compare(items[a], items[b]) * sign;
                return c != 0 ? c : a.CompareTo(b);
            }));
        }
        _page = Math.Clamp(_page, 0, Math.Max(1, (_count + _pageSize - 1) / _pageSize) - 1);
        SyncVirtual();
    }
}
