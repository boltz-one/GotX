using System.Runtime.InteropServices;

namespace GotX;

/// <summary>
/// Focus bàn phím trên phần tử thường (<c>.Focusable()</c>) và đường đi của phím. Phần tử được dựng lại mỗi frame
/// nên focus được nhớ bằng khoá của phần tử (view + <c>.Key()</c> hoặc thứ tự trong view), còn hình học và handler
/// lấy từ danh sách hitbox của frame gần nhất. Thứ tự Tab là thứ tự vẽ; lớp modal chặn Tab ở bên trong nó.
/// </summary>
public sealed partial class Window
{
    readonly Dictionary<long, FocusMark> _autoFocus = new();
    long _focusKey, _pendingAutoFocus;
    int _pendingFocusName; // FocusKey("key") gọi trước khi phần tử được vẽ
    int _keyScope;         // mục bàn phím đang bao ngoài trong lúc vẽ (1-based, 0 = không có)
    bool _focusVisible, _keyboardMode, _pendingFocusVisible = true;

    /// <summary>Đang có phần tử thường (không phải ô nhập) giữ focus.</summary>
    public bool HasElementFocus => _focusKey != 0;

    /// <summary>Focus hiện tại có nên hiện vòng focus (đến từ bàn phím).</summary>
    public bool FocusVisible => _focusVisible && _focusKey != 0;

    /// <summary>Phần tử có <c>.Key(key)</c> đang giữ focus.</summary>
    public bool IsFocused(string key) => _focusKey != 0 && _tree.TryKeyId(key, out int id) && (int)_focusKey == id;

    /// <summary>Hộp trên màn hình (logical px) của phần tử đang focus, hoặc null.</summary>
    public Bounds? FocusedBounds
    {
        get
        {
            if (!_drawing) return FocusedHitbox() is >= 0 and var i ? HitboxBounds(i) : null;
            // Gọi trong Render: danh sách frame này chưa dựng xong → trả vị trí ở frame trước.
            foreach (var hb in _prevHitboxes)
                if (_focusKey != 0 ? hb.IsKey && hb.FocusKey == _focusKey : Focused is { } input && ReferenceEquals(hb.Input, input) && !hb.IsKey)
                    return hb.Xf.ApplyAabb(hb.Local).Intersect(hb.Clip);
            return null;
        }
    }

    /// <summary>Đưa focus tới phần tử <c>.Focusable().Key(key)</c>. Nếu nó chưa được vẽ thì focus ở frame kế tiếp.</summary>
    /// <param name="visible">false = focus "im lặng": không bật <c>focus-visible:</c> (ví dụ focus panel menu vừa mở bằng chuột).</param>
    public void FocusKey(string key, bool visible = true)
    {
        int id = _tree.KeyId(key);
        int at = _drawing ? -1 : FindByName(id); // đang dựng frame: hitbox chưa đủ, để cuối frame xử lý
        if (at >= 0) { Focus(null); SetElementFocus(_hitboxes[at].FocusKey, visible); ScrollIntoView(at); }
        else { _pendingFocusName = id; _pendingFocusVisible = visible; RequestFrame(); }
    }

    /// <summary>Bỏ focus (cả ô nhập lẫn phần tử thường).</summary>
    public void Blur() => Focus(null);

    void SetElementFocus(long key, bool visible)
    {
        if (_focusKey == key && _focusVisible == visible) return;
        if (_focusKey != 0) MarkViewDirty((int)(_focusKey >> 32), true, DirtyCause.Focus);
        _focusKey = key;
        _focusVisible = visible && key != 0;
        if (key == 0) return;
        if (Focused is not null) FocusInputOnly(null);
        MarkViewDirty((int)(key >> 32), true, DirtyCause.Focus);
        if (Trace is not null && FindKey(key) is >= 0 and var i) Emit(TraceKind.Focus, visible ? "phần tử (bàn phím)" : "phần tử", HitboxBounds(i), _hitboxes[i].ViewId);
        RequestFrame();
    }

    int FindKey(long key)
    {
        var span = CollectionsMarshal.AsSpan(_hitboxes);
        for (int i = span.Length - 1; i >= 0; i--)
            if (span[i].IsKey && span[i].FocusKey == key) return i;
        return -1;
    }

    int FindByName(int keyId)
    {
        var span = CollectionsMarshal.AsSpan(_hitboxes);
        for (int i = span.Length - 1; i >= 0; i--)
        {
            if (span[i].Blocker) break; // không đưa focus xuống dưới lớp modal đang mở
            if (span[i].IsKey && span[i].Focusable && (int)span[i].FocusKey == keyId) return i;
        }
        return -1;
    }

    /// <summary>Hitbox của thứ đang focus (phần tử thường hoặc ô nhập), -1 nếu không có.</summary>
    int FocusedHitbox()
    {
        if (_focusKey != 0) return FindKey(_focusKey);
        if (Focused is not { } input) return -1;
        var span = CollectionsMarshal.AsSpan(_hitboxes);
        for (int i = span.Length - 1; i >= 0; i--)
            if (ReferenceEquals(span[i].Input, input) && !span[i].IsKey) return i;
        return -1;
    }

    /// <summary>Chỉ số hitbox đầu tiên còn nhận được input: ngay sau lớp modal trên cùng.</summary>
    int InputFloor()
    {
        var span = CollectionsMarshal.AsSpan(_hitboxes);
        for (int i = span.Length - 1; i >= 0; i--)
            if (span[i].Blocker) return i + 1;
        return 0;
    }

    /// <summary>Nhấn chuột: focus phần tử focus được trên cùng tại điểm đó (không hiện vòng focus), không có thì bỏ focus.</summary>
    void FocusFromPointer(Point p)
    {
        var span = CollectionsMarshal.AsSpan(_hitboxes);
        for (int i = span.Length - 1; i >= 0; i--)
        {
            if (span[i].Blocker) break;
            if (span[i].IsKey && span[i].Focusable && span[i].Contains(p)) { SetElementFocus(span[i].FocusKey, false); return; }
        }
        SetElementFocus(0, false);
    }

    /// <summary>
    /// Đường đi của một phím: ô nhập đang focus dùng trước; phần tử đang focus nhận qua OnKey, rồi Enter/Space kích hoạt
    /// OnClick của nó; chưa ai xử lý thì nổi lên các phần tử cha có OnKey; cuối cùng Tab chuyển focus, Escape bỏ focus.
    /// </summary>
    void DispatchKey(in InputEvent e)
    {
        var key = new KeyEvent(e.Key, e.Modifiers);
        int at = FocusedHitbox();
        bool handled = false;
        if (Focused is { } input)
        {
            if (e.Key is not (Key.Tab or Key.Escape)) handled = input.HandleKey(e.Key, e.Modifiers);
            if (!handled && at >= 0) handled = Bubble(_hitboxes[at].KeyParent, key);
        }
        else if (at >= 0)
        {
            var hb = _hitboxes[at];
            if (hb is { KeyInvoke: { } invoke, KeyHandler: { } handler, KeyState: { } state }) handled = invoke(handler, state, key);
            if (!handled && e.Key is Key.Enter or Key.Space && hb is { Invoke: { } click, Handler: { } h, State: { } st })
            {
                var b = HitboxBounds(at);
                if (Trace is not null) Emit(TraceKind.Click, st.GetType().Name + " (phím)", b, hb.ViewId);
                click(h, st, new MouseEvent(new Point(b.X + b.Width / 2, b.Y + b.Height / 2), MouseButton.Left));
                handled = true;
            }
            if (!handled) handled = Bubble(hb.KeyParent, key);
        }
        if (handled) return;
        if (e.Key == Key.Tab) MoveFocus(backward: (e.Modifiers & Modifiers.Shift) != 0);
        else if (e.Key == Key.Escape) Focus(null);
    }

    bool Bubble(int oneBased, in KeyEvent key)
    {
        // Handler có thể đổi state nhưng danh sách hitbox chỉ đổi ở frame sau, nên duyệt theo chỉ số là an toàn.
        for (int guard = 0; oneBased > 0 && oneBased <= _hitboxes.Count && guard < 256; guard++)
        {
            var hb = _hitboxes[oneBased - 1];
            if (hb is { KeyInvoke: { } invoke, KeyHandler: { } handler, KeyState: { } state } && invoke(handler, state, key)) return true;
            oneBased = hb.KeyParent;
        }
        return false;
    }

    /// <summary>Tab / Shift+Tab: tới điểm dừng kế tiếp theo thứ tự vẽ, quay vòng ở hai đầu, không ra khỏi lớp modal trên cùng.</summary>
    public void MoveFocus(bool backward)
    {
        int floor = InputFloor(), current = FocusedHitbox(), count = _hitboxes.Count;
        if (count == floor) return;
        int span = count - floor;
        int start = current >= floor ? current - floor : (backward ? 0 : span - 1);
        for (int step = 1; step <= span; step++)
        {
            int i = floor + ((start + (backward ? -step : step)) % span + span) % span;
            var hb = _hitboxes[i];
            if (hb.IsKey && hb.Focusable) { SetElementFocus(hb.FocusKey, true); ScrollIntoView(i); return; }
            if (hb.Input is { } input && !hb.IsKey && !ReferenceEquals(input, Focused)) { Focus(input); ScrollIntoView(i); return; }
        }
    }
}
