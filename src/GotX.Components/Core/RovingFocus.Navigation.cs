namespace GotX.Components;

// Phần tĩnh của RovingFocus: tính mục kế tiếp theo phím (bỏ qua mục bị tắt), bảng tên, và Tab rời nhóm cho các
// component tự giữ key của mình (menu, danh sách, tab, accordion).
public sealed partial class RovingFocus
{
    /// <summary>Giá trị trả về của <see cref="FromKey"/> khi phím không phải phím di chuyển.</summary>
    public const int NotNavigation = int.MinValue;

    /// <summary>
    /// Mục bật kế tiếp từ <paramref name="from"/> theo hướng <paramref name="delta"/> (±1). Chưa có mục nào (-1) thì
    /// đi xuống chọn mục đầu, đi lên chọn mục cuối. Hết danh sách: quay vòng nếu <paramref name="loop"/>, không thì đứng yên.
    /// </summary>
    public static int Step<T>(T state, int count, int from, int delta, bool loop, Func<T, int, bool> enabled)
    {
        bool valid = (uint)from < (uint)count;
        int i = valid ? from : (delta > 0 ? -1 : count);
        for (int n = 0; n < count; n++)
        {
            i += delta;
            if ((uint)i >= (uint)count)
            {
                if (!loop) break;
                i = delta > 0 ? 0 : count - 1;
            }
            if (enabled(state, i)) return i;
        }
        return valid ? from : -1;
    }

    /// <summary>Mục bật đầu tiên (hoặc cuối cùng); -1 nếu không có.</summary>
    public static int Edge<T>(T state, int count, bool last, Func<T, int, bool> enabled)
    {
        for (int n = 0; n < count; n++)
        {
            int i = last ? count - 1 - n : n;
            if (enabled(state, i)) return i;
        }
        return -1;
    }

    /// <summary>
    /// Đổi phím di chuyển thành chỉ số mới: mũi tên theo trục (<paramref name="vertical"/>: ↑/↓, ngược lại ←/→),
    /// Home/End (và PageUp/PageDown với danh sách dọc) nhảy về hai đầu. Phím khác trả <see cref="NotNavigation"/>.
    /// </summary>
    public static int FromKey<T>(Key key, T state, int count, int current, bool loop, bool vertical, Func<T, int, bool> enabled)
    {
        Key prev = vertical ? GotX.Key.Up : GotX.Key.Left, next = vertical ? GotX.Key.Down : GotX.Key.Right;
        if (key == next) return Step(state, count, current, +1, loop, enabled);
        if (key == prev) return Step(state, count, current, -1, loop, enabled);
        if (key == GotX.Key.Home || (vertical && key == GotX.Key.PageUp)) return Edge(state, count, false, enabled);
        if (key == GotX.Key.End || (vertical && key == GotX.Key.PageDown)) return Edge(state, count, true, enabled);
        return NotNavigation;
    }

    /// <summary>
    /// Bảng tên (key / anchor) cho <paramref name="count"/> phần tử của một danh sách, dạng <c>"{kind}{id}-{chỉ số}"</c>
    /// với <paramref name="id"/> là Id của view (để duy nhất theo instance). Tạo một lần rồi dùng lại, không nối chuỗi mỗi frame.
    /// </summary>
    public static string[] Names(ref string[] cache, int count, string kind, EntityId id)
    {
        if (cache.Length >= count) return cache;
        var next = new string[count];
        for (int i = 0; i < count; i++) next[i] = i < cache.Length ? cache[i] : $"{kind}{id.Value}-{i}";
        return cache = next;
    }

    /// <summary>Chỉ số phần tử đang giữ focus trong <paramref name="keys"/> (xét <paramref name="count"/> phần tử đầu); -1 nếu không có.</summary>
    public static int FocusedIndex(Window w, string[] keys, int count)
    {
        for (int i = 0; i < count && i < keys.Length; i++)
            if (w.IsFocused(keys[i])) return i;
        return -1;
    }

    /// <summary>
    /// Tab trong một nhóm roving phải rời cả nhóm chứ không dừng ở từng mục: nhảy tới mục đầu/cuối nhóm rồi để Core đi tiếp
    /// một bước. Gọi từ OnKey của nhóm khi nhận Tab; luôn coi như đã xử lý.
    /// </summary>
    public static bool TabOut(Window w, bool backward, string firstKey, string lastKey)
    {
        w.FocusKey(backward ? firstKey : lastKey);
        w.MoveFocus(backward);
        return true;
    }
}
