namespace GotX;

/// <summary>Trạng thái focus giữ qua các frame: dấu AutoFocus, yêu cầu focus đang chờ, và việc dọn chúng cuối mỗi frame.</summary>
public sealed partial class Window
{
    void TouchAutoFocus(long key)
    {
        if (!_autoFocus.TryGetValue(key, out var mark))
        {
            _autoFocus[key] = mark = new FocusMark();
            _pendingAutoFocus = key;
        }
        mark.Frame = _frame;
    }

    /// <summary>Cuối frame: thực hiện AutoFocus / FocusKey đang chờ, bỏ focus trên phần tử đã biến mất.</summary>
    void ResolveFocus()
    {
        if (_autoFocus.Count > 0)
            foreach (var kv in _autoFocus) // phần tử không còn được vẽ (kể cả khi view chứa nó bị gỡ khỏi cây) → lần sau lại tự focus
            {
                int view = (int)(kv.Key >> 32);
                if (kv.Value.Frame != _frame && (!_cache.ContainsKey(view) || _paintedViews.Contains(view))) _autoFocus.Remove(kv.Key);
            }
        if (_pendingAutoFocus != 0)
        {
            long key = _pendingAutoFocus;
            _pendingAutoFocus = 0;
            if (FindKey(key) >= 0) { if (Focused is not null) FocusInputOnly(null); SetElementFocus(key, _keyboardMode); }
        }
        if (_pendingFocusName != 0 && FindByName(_pendingFocusName) is >= 0 and var at)
        {
            _pendingFocusName = 0;
            if (Focused is not null) FocusInputOnly(null);
            SetElementFocus(_hitboxes[at].FocusKey, _pendingFocusVisible);
            ScrollIntoView(at);
        }
        if (_focusKey != 0 && FindKey(_focusKey) < 0) SetElementFocus(0, false);
    }

    /// <summary>View <paramref name="view"/> nằm trong cây của view <paramref name="ancestor"/> (theo frame gần nhất).</summary>
    bool ViewWithin(int view, int ancestor)
    {
        for (int guard = 0; view > 0 && guard < 256; guard++)
        {
            if (view == ancestor) return true;
            view = _cache.TryGetValue(view, out var c) ? c.ParentView : -1;
        }
        return false;
    }
}
