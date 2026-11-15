namespace GotX.Components;

/// <summary>Chọn hàng của <see cref="DataTable{T}"/>: trạng thái theo chỉ số trong Items nên không đổi khi sắp xếp, lọc hay đổi trang.</summary>
public sealed partial class DataTable<T>
{
    public int SelectedCount => _selectedCount;
    public bool IsSelected(int index) => _selected[index];

    /// <summary>Chỉ số (trong Items) các hàng đang chọn, tăng dần.</summary>
    public IReadOnlyList<int> SelectedIndices
    {
        get
        {
            var list = new List<int>(_selectedCount);
            for (int i = 0; i < _selected.Length; i++) if (_selected[i]) list.Add(i);
            return list;
        }
    }

    public void SetSelected(int index, bool selected)
    {
        if (_selected[index] == selected) return;
        _selected[index] = selected;
        SelectionDone(selected ? 1 : -1);
    }

    /// <summary>Chọn / bỏ chọn mọi hàng của trang đang hiện (hành vi của ô đánh dấu ở tiêu đề, như toggleAllPageRowsSelected).</summary>
    public void SetPageSelected(bool selected)
    {
        Refresh();
        int delta = 0;
        for (int p = _page * _pageSize, end = Math.Min(_count, p + _pageSize); p < end; p++)
        {
            if (_selected[_order[p]] == selected) continue;
            _selected[_order[p]] = selected;
            delta += selected ? 1 : -1;
        }
        if (delta != 0) SelectionDone(delta);
    }

    void SelectionDone(int delta)
    {
        _selectedCount += delta;
        _selectionText = null;
        Notify();
        Emit(new SelectionChanged(_selectedCount));
    }

    /// <summary>0 = trang hiện tại chưa chọn hàng nào, 1 = đã chọn hết, 2 = chọn một phần.</summary>
    int PageSelection()
    {
        int on = 0, start = _page * _pageSize, end = Math.Min(_count, start + _pageSize);
        for (int p = start; p < end; p++) if (_selected[_order[p]]) on++;
        return on == 0 ? 0 : on == end - start ? 1 : 2;
    }
}
