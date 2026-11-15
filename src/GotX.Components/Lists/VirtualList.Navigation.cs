namespace GotX.Components;

/// <summary>Cuộn tới hàng, bàn phím và đo chiều cao hàng thật của <see cref="VirtualList"/>.</summary>
public sealed partial class VirtualList
{
    const int MaxKnownHeights = 262144; // quá ngưỡng thì xoá và đo lại dần

    readonly Dictionary<string, float> _known = new();
    bool _sendTarget, _animateTarget;

    /// <summary>
    /// Cuộn để hàng <paramref name="index"/> nằm trong khung (hoặc ở đầu/cuối khung theo <paramref name="align"/>). Tính từ chiều
    /// cao đã biết nên chạy được cả khi hàng chưa được dựng; vị trí được áp ở lần dựng kế.
    /// </summary>
    public void ScrollToIndex(int index, ScrollAlign align = ScrollAlign.Nearest, bool animate = false)
    {
        if (_count == 0) return;
        index = Math.Clamp(index, 0, _count - 1);
        var table = Ensure();
        var info = _window?.GetScroll(ScrollKey);
        double viewport = info?.Viewport.Height is { } vh and > 0 ? vh : AssumedViewport;
        double current = float.IsNaN(_targetY) ? info?.Offset.Y ?? 0 : _targetY;
        double top = table.Offset(index), bottom = top + table[index];
        double y = align switch
        {
            ScrollAlign.Start => top,
            ScrollAlign.End => bottom - viewport,
            _ => top < current ? top : bottom > current + viewport ? bottom - viewport : current,
        };
        y = Math.Clamp(y, 0, Math.Max(0, table.Total - viewport));
        if (!float.IsNaN(_targetY) || MathF.Abs((float)(y - current)) >= 0.5f) { _targetY = (float)y; _sendTarget = true; _animateTarget = animate; Notify(); }
    }

    // Tìm hàng có key đã cho, đi ra hai phía từ chỉ số cũ (dữ liệu thường chỉ lệch vài hàng).
    int FindKey(string key, int near)
    {
        if (ItemKey is not { } keyOf) return -1;
        for (int d = 0; d < _count; d++)
        {
            if (near + d < _count && keyOf(near + d) == key) return near + d;
            if (d > 0 && near - d >= 0 && near - d < _count && keyOf(near - d) == key) return near - d;
            if (near + d >= _count && near - d < 0) break;
        }
        return -1;
    }

    // Gửi yêu cầu cuộn đã tính tới Window (cần Window nên làm trong Render); _targetY chỉ dùng cho đúng một lần dựng.
    void SendScroll(Window w)
    {
        if (!_sendTarget) return;
        _sendTarget = false;
        w.ScrollTo(ScrollKey, null, _targetY, _animateTarget);
    }

    bool OnKey(KeyEvent e)
    {
        if (_count == 0 || e.Control || e.Alt) return false;
        float viewport = _window?.GetScroll(ScrollKey)?.Viewport.Height is { } vh and > 0 ? vh : AssumedViewport;
        int page = Math.Max(1, (int)(viewport / (_variable ? _estimated : _itemHeight)) - 1);
        int from = _active < 0 ? -1 : _active;
        int target;
        switch (e.Key)
        {
            case Key.Down: target = from + 1; break;
            case Key.Up: target = from < 0 ? 0 : from - 1; break;
            case Key.PageDown: target = from < 0 ? page - 1 : from + page; break;
            case Key.PageUp: target = from < 0 ? 0 : from - page; break;
            case Key.Home: target = 0; break;
            case Key.End: target = _count - 1; break;
            case Key.Enter:
                if (_active < 0) return false;
                Emit(new Activated(_active));
                return true;
            default: return false;
        }
        ActiveIndex = Math.Clamp(target, 0, _count - 1);
        ScrollToIndex(_active);
        return true;
    }

    // Việc sau frame: khung vừa biết cỡ thật thì dựng lại nếu cần, và (chế độ thay đổi) đo các hàng vừa vẽ.
    void OnAfterFrame()
    {
        if (_window?.GetScroll(ScrollKey) is not { } info) return;
        CheckRange(info.Offset.Y, info.Viewport.Height);
        if (_variable) Measure(info);
    }

    void Measure(ScrollInfo info)
    {
        var table = Ensure();
        double before = table.Offset(FirstVisible);
        // Đang ở đáy theo mô hình (trước khi đo) và hàng cuối đã dựng: tổng chiều cao đổi thì bám theo đáy mới.
        bool pinEnd = _end == _count && info.Offset.Y >= table.Total - info.Viewport.Height - 2;
        bool changed = false;
        if (_known.Count > MaxKnownHeights) _known.Clear();
        for (int i = _start; i < _end; i++)
        {
            if (_renderedKeys[i - _start] is not { } key || _window!.AnchorBounds(key) is not { } box) continue;
            _known[key] = box.Height;
            changed |= table.Set(i, box.Height);
        }
        if (!changed) return;
        // Hàng phía trên khung vừa đổi cao: bù vị trí cuộn để nội dung đang đọc không trôi.
        double shift = table.Offset(FirstVisible) - before;
        if (pinEnd) { _targetY = (float)Math.Max(0, table.Total - info.Viewport.Height); _sendTarget = true; _animateTarget = false; }
        else if (info.Offset.Y > 0 && MathF.Abs((float)shift) >= 0.5f) { _targetY = (float)(info.Offset.Y + shift); _sendTarget = true; _animateTarget = false; }
        Notify();
    }
}
