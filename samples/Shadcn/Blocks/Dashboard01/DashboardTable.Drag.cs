using GotX;

/// <summary>
/// Kéo tay nắm để đổi thứ tự hàng (DndContext + SortableContext verticalListSortingStrategy + restrictToVerticalAxis):
/// hàng đang kéo đi theo con trỏ (opacity-80), các hàng nó lướt qua dịch một hàng để nhường chỗ (transition như dnd-kit),
/// thả thì arrayMove. Bàn phím (KeyboardSensor): Space / Enter nhấc hàng, ↑ / ↓ dời từng hàng, Space / Enter thả, Escape huỷ.
/// Chỉ trong trang hiện tại, như bản gốc (SortableContext chỉ có các hàng đang hiện).
/// </summary>
public sealed partial class DashboardTable
{
    const float RowHeight = 53;
    int _dragId = -1;
    float _dragStart, _dragDy;
    bool _keyboardDrag;

    /// <summary>Id của hàng đang được kéo; -1 khi không kéo.</summary>
    public int DraggingId => _dragId;

    void Drag(int id, DragEvent e)
    {
        switch (e.Phase)
        {
            case DragPhase.Start: _dragId = id; _dragStart = e.Position.Y; _dragDy = 0; _keyboardDrag = false; break;
            case DragPhase.Move when _dragId == id && !_keyboardDrag: _dragDy = Clamp(e.Position.Y - _dragStart); break;
            case DragPhase.End when _dragId == id && !_keyboardDrag: Drop(); return;
            default: return;
        }
        Notify();
    }

    bool DragKey(int id, KeyEvent e)
    {
        if (e.Key is Key.Space or Key.Enter)
        {
            if (_dragId == id && _keyboardDrag) Drop();
            else { _dragId = id; _dragDy = 0; _keyboardDrag = true; Notify(); }
            return true;
        }
        if (_dragId != id || !_keyboardDrag) return false;
        if (e.Key == Key.Escape) { _dragId = -1; _dragDy = 0; Notify(); return true; }
        if (e.Key is not (Key.Up or Key.Down)) return e.Key == Key.Tab;
        _dragDy = Clamp(_dragDy + (e.Key == Key.Up ? -RowHeight : RowHeight));
        Notify();
        return true;
    }

    /// <summary>Kẹp độ dời trong phạm vi các hàng của trang (restrictToVerticalAxis + danh sách hữu hạn).</summary>
    float Clamp(float dy)
    {
        var (from, count) = DragRange();
        return from < 0 ? 0 : Math.Clamp(dy, -from * RowHeight, (count - 1 - from) * RowHeight);
    }

    (int From, int Count) DragRange()
    {
        int i = 0, from = -1;
        foreach (var s in PageRows()) { if (s.Id == _dragId) from = i; i++; }
        return (from, i);
    }

    int DragTarget()
    {
        var (from, count) = DragRange();
        return from < 0 ? -1 : Math.Clamp(from + (int)MathF.Round(_dragDy / RowHeight), 0, count - 1);
    }

    void Drop()
    {
        var (from, _) = DragRange();
        int to = DragTarget();
        if (from >= 0 && to != from)
        {
            int start = _page * _pageSize;
            var item = _sections[start + from];
            _sections.RemoveAt(start + from);
            _sections.Insert(start + to, item);
        }
        _dragId = -1;
        _dragDy = 0;
        Notify();
    }

    /// <summary>Dịch hình của hàng thứ <paramref name="index"/> trong trang theo trạng thái kéo (0 khi không kéo).</summary>
    float DragOffset(int index, int id)
    {
        if (_dragId < 0) return 0;
        if (id == _dragId) return _dragDy;
        var (from, _) = DragRange();
        int to = DragTarget();
        if (from < to && index > from && index <= to) return -RowHeight;
        if (from > to && index < from && index >= to) return RowHeight;
        return 0;
    }
}
