namespace GotX;

public sealed partial class Window
{
    readonly HashSet<int> _keysWithState = new();
    int _keyPruneAt = 4096;

    /// <summary>Dọn state giữ qua các frame (cuộn, transition, bảng khoá) của những phần tử không còn tồn tại.</summary>
    void PruneState()
    {
        PruneElementState(_scrolls);
        PruneElementState(_transitions);
        if (_tree.KeyCount <= _keyPruneAt) return;

        // Khoá .Key() sinh động (vd. theo id dòng) tích lại dần: bỏ khoá không còn dùng và không còn state.
        _keysWithState.Clear();
        foreach (var kv in _scrolls) _keysWithState.Add((int)kv.Key);
        foreach (var kv in _transitions) _keysWithState.Add((int)kv.Key);
        _tree.PruneKeys(_keysWithState);
        _keyPruneAt = Math.Max(4096, _tree.KeyCount * 2);
    }

    /// <summary>
    /// Bỏ state của phần tử không còn tồn tại:
    /// – view chứa nó đã bị Release hoặc bị GC thu hồi;
    /// – view vừa được vẽ lại mà phần tử không còn xuất hiện.
    /// View còn sống nhưng tạm không hiển thị (đổi tab) và view được phát lại từ cache thì giữ nguyên state.
    /// </summary>
    void PruneElementState<T>(Dictionary<long, T> states) where T : ElementState
    {
        if (states.Count == 0) return;
        foreach (var kv in states) // Dictionary cho phép Remove trong lúc duyệt (.NET Core 3.0+)
        {
            int view = (int)(kv.Key >> 32);
            if (view <= 0) continue;
            bool gone = _cache.ContainsKey(view)
                ? kv.Value.Frame != _frame && _paintedViews.Contains(view)
                : !App.IsAlive(view);
            if (gone) states.Remove(kv.Key);
        }
    }

    /// <summary>App gọi khi một entity bị Release: không giữ focus/kéo chọn trên entity đã gỡ.</summary>
    internal void OnEntityReleased(Entity entity)
    {
        if (ReferenceEquals(_dragInput, entity)) _dragInput = null;
        if (ReferenceEquals(Focused, entity)) Focus(null);
        for (int i = _overlays.Count - 1; i >= 0; i--)
            if (ReferenceEquals(_overlays[i].Content, entity)) CloseOverlay(_overlays[i]);
    }
}
