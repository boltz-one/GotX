namespace GotX.Components;

/// <summary>
/// "Roving focus" cho một nhóm phần tử (radio, toggle group, lưới ngày): cả nhóm cư xử như một điểm dừng Tab, mũi tên
/// di chuyển bên trong. Core không có <c>tabindex</c> (mọi phần tử <c>.Focusable()</c> đều là điểm dừng, theo thứ tự
/// vẽ), nên lớp này bù lại bằng hai việc: khi Tab đi vào nhóm thì chuyển focus về phần tử "nhà" (<see cref="Sync"/>),
/// khi Tab đi ra thì nhảy tới mép nhóm trước để bước Tab mặc định rời hẳn nhóm (<see cref="LeaveOnTab"/>).
/// Các phần tử vẫn focus được từng cái để bấm chuột focus đúng chỗ mà không hiện vòng focus.
/// <code>
/// _roving ??= new RovingFocus("gotx-radio-" + Id.Value);
/// _roving.Sync(w, count, home: _index);
/// item.Key(_roving.Key(i)).Focusable().OnKey(refs[i], static (r, e) =&gt; r.Owner.OnItemKey(r.Index, e));
/// </code>
/// </summary>
public sealed partial class RovingFocus(string prefix)
{
    string[] _keys = [];
    bool _inside;

    /// <summary>Khoá của phần tử thứ <paramref name="index"/>; chuỗi được tạo một lần rồi dùng lại mỗi frame.</summary>
    public string Key(int index)
    {
        if (index >= _keys.Length)
        {
            int old = _keys.Length;
            Array.Resize(ref _keys, Math.Max(index + 1, old * 2));
            for (int i = old; i < _keys.Length; i++) _keys[i] = prefix + "-" + i;
        }
        return _keys[index];
    }

    /// <summary>Chỉ số phần tử của nhóm đang giữ focus, -1 nếu focus ở ngoài.</summary>
    public int Focused(Window w, int count)
    {
        if (!w.HasElementFocus) return -1;
        for (int i = 0; i < count; i++)
            if (w.IsFocused(Key(i))) return i;
        return -1;
    }

    /// <summary>Phần tử <paramref name="index"/> đang giữ focus do bàn phím (cần vẽ vòng focus).</summary>
    public bool ShowsRing(Window w, int index) => w.FocusVisible && w.IsFocused(Key(index));

    /// <summary>
    /// Đưa focus tới phần tử <paramref name="index"/> (gọi từ handler phím). <paramref name="visible"/> = false để
    /// focus mà không hiện vòng focus (đi theo một cú bấm chuột).
    /// </summary>
    public void Focus(Window w, int index, bool visible = true) => w.FocusKey(Key(index), visible);

    /// <summary>
    /// Gọi trong <c>Render</c>. Nếu focus vừa đi vào nhóm bằng bàn phím mà không rơi vào <paramref name="home"/> thì
    /// chuyển nó về đó (Core thực hiện <c>FocusKey</c> gọi trong Render ở cuối chính frame này). Trả về chỉ số đang focus.
    /// </summary>
    public int Sync(Window w, int count, int home)
    {
        int at = Focused(w, count);
        bool entered = at >= 0 && !_inside && w.FocusVisible;
        _inside = at >= 0;
        if (entered && home >= 0 && home < count && home != at) w.FocusKey(Key(home));
        return at;
    }

    /// <summary>Chữ ký cũ, giữ để code đang gọi không phải sửa; <paramref name="app"/> không còn được dùng.</summary>
    public int Sync(Window w, App? app, int count, int home) => Sync(w, count, home);

    /// <summary>
    /// Gọi khi một phần tử của nhóm nhận phím Tab, rồi trả <c>false</c> cho Core: focus nhảy tới phần tử cuối (hoặc đầu
    /// với Shift+Tab) nên bước Tab mặc định ngay sau đó đi ra khỏi nhóm thay vì sang phần tử kế bên.
    /// </summary>
    public void LeaveOnTab(Window w, int count, bool backward)
    {
        if (count > 0) w.FocusKey(Key(backward ? 0 : count - 1));
    }

    /// <summary>
    /// Chỉ số kế tiếp theo phím điều hướng, quay vòng ở hai đầu: ←/↑ lùi, →/↓ tiến, Home/End về đầu/cuối.
    /// Trả -1 nếu <paramref name="key"/> không phải phím điều hướng.
    /// </summary>
    public static int Step(Key key, int index, int count)
    {
        if (count <= 0) return -1;
        return key switch
        {
            GotX.Key.Left or GotX.Key.Up => (index - 1 + count) % count,
            GotX.Key.Right or GotX.Key.Down => (index + 1) % count,
            GotX.Key.Home => 0,
            GotX.Key.End => count - 1,
            _ => -1,
        };
    }
}
