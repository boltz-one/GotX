namespace GotX;

/// <summary>Trạng thái một vùng cuộn: vị trí hiện tại, vị trí tối đa và kích thước khung nhìn (logical px).</summary>
public readonly record struct ScrollInfo(Point Offset, Point Max, Size Viewport)
{
    /// <summary>Khoảng còn lại tới đáy.</summary>
    public float RemainingY => Max.Y - Offset.Y;
    public float RemainingX => Max.X - Offset.X;
}

internal delegate void ScrollInvoker(Delegate handler, object state, in ScrollInfo info);

internal static class ScrollInvoker<T> where T : class
{
    public static readonly ScrollInvoker Instance = static (Delegate h, object s, in ScrollInfo i) => ((Action<T, ScrollInfo>)h)((T)s, i);
}

/// <summary>Cuộn bằng code và theo dõi vị trí cuộn. Vùng cuộn được gọi tên bằng <c>.Key("tên")</c> của nó.</summary>
public sealed partial class Window
{
    // Yêu cầu cuộn chờ tới lần vẽ kế của vùng đó: lúc ấy mới biết kích thước nội dung mới (vd. vừa thêm tin nhắn).
    readonly Dictionary<int, (float? X, float? Y, bool Animate)> _pendingScroll = new();
    bool _callbackFired; // một handler (hover, scroll) đã chạy trong lúc vẽ: cần Flush để Notify của nó có hiệu lực

    ScrollState? FindScroll(string key)
    {
        if (!_tree.TryKeyId(key, out int id)) return null;
        foreach (var kv in _scrolls)
            if ((int)kv.Key == id) return kv.Value;
        return null;
    }

    /// <summary>Vị trí cuộn ở frame gần nhất của vùng có <c>.Key(key)</c>; null nếu vùng đó chưa từng được vẽ.</summary>
    public ScrollInfo? GetScroll(string key) =>
        FindScroll(key) is { } st ? new ScrollInfo(new Point(st.X, st.Y), new Point(st.MaxX, st.MaxY), st.Viewport) : null;

    /// <summary>
    /// Cuộn vùng có <c>.Key(key)</c> tới vị trí cho trước (null = giữ nguyên trục đó). Giá trị được kẹp vào phạm vi
    /// hợp lệ ở lần vẽ kế, nên <c>float.PositiveInfinity</c> nghĩa là "tới cuối" kể cả khi nội dung vừa dài thêm.
    /// Gọi được trước khi vùng cuộn xuất hiện lần đầu.
    /// </summary>
    public void ScrollTo(string key, float? x, float? y, bool animate = true)
    {
        int id = _tree.KeyId(key);
        _pendingScroll[id] = (x, y, animate);
        foreach (var kv in _scrolls)
            if ((int)kv.Key == id) MarkViewDirty((int)(kv.Key >> 32), request: false, DirtyCause.Scroll);
        RequestFrame();
    }

    public void ScrollToEnd(string key, bool animate = true) => ScrollTo(key, null, float.PositiveInfinity, animate);

    int _scrollScope; // vùng cuộn đang bao ngoài trong lúc vẽ (1-based trong danh sách hitbox, 0 = không có)

    /// <summary>
    /// Cuộn các vùng cuộn bao ngoài sao cho phần tử có <c>.Anchor(name)</c> nằm trong tầm nhìn (dùng cho mục đang
    /// được chọn bằng bàn phím trong danh sách dài). Trả về false nếu phần tử chưa được vẽ – khi đó yêu cầu được nhớ
    /// và thực hiện sau frame kế tiếp (gọi được ngay sau khi mở overlay hoặc trong Render).
    /// </summary>
    public bool ScrollIntoView(string anchor)
    {
        if (!_drawing)
            for (int i = _hitboxes.Count - 1; i >= 0; i--)
                if (_hitboxes[i].Anchor == anchor) { ScrollIntoView(i); return true; }
        // Chưa được vẽ (overlay vừa mở, hoặc đang trong Render): nhớ lại và thực hiện ở cuối frame kế tiếp.
        _pendingScrollAnchor = anchor;
        RequestFrame();
        return false;
    }

    string? _pendingScrollAnchor;

    /// <summary>Cuối frame: thực hiện ScrollIntoView đang chờ, nếu phần tử đã xuất hiện.</summary>
    void ResolvePendingScroll()
    {
        if (_pendingScrollAnchor is not { } anchor) return;
        _pendingScrollAnchor = null;
        for (int i = _hitboxes.Count - 1; i >= 0; i--)
            if (_hitboxes[i].Anchor == anchor) { ScrollIntoView(i); return; }
    }

    /// <summary>Đưa hitbox thứ <paramref name="at"/> vào tầm nhìn: đi từ vùng cuộn trong cùng ra ngoài, mỗi vùng cuộn vừa đủ.</summary>
    void ScrollIntoView(int at)
    {
        var hb = _hitboxes[at];
        var box = hb.Xf.ApplyAabb(hb.Local);
        for (int p = hb.ScrollParent, guard = 0; p > 0 && p <= _hitboxes.Count && guard < 32; guard++)
        {
            var area = _hitboxes[p - 1];
            p = area.ScrollParent;
            if (!_scrolls.TryGetValue(area.ScrollKey, out var st)) continue;
            // Lùi vào 2px: hitbox là border box, không để phần tử (và vòng focus của nó) nằm đè lên viền vùng cuộn.
            var view = area.Xf.ApplyAabb(area.Local);
            if (view.Width > 8 && view.Height > 8) view = view.Inflate(-2);
            float scale = area.Xf.S <= 0 ? 1 : area.Xf.S;
            // Lệch âm = phần tử nằm trên/trái khung nhìn; phần tử cao hơn khung thì ưu tiên mép đầu.
            float dy = box.Y < view.Y ? box.Y - view.Y : box.Bottom > view.Bottom ? MathF.Min(box.Bottom - view.Bottom, box.Y - view.Y) : 0;
            float dx = box.X < view.X ? box.X - view.X : box.Right > view.Right ? MathF.Min(box.Right - view.Right, box.X - view.X) : 0;
            float ty = Math.Clamp(st.TargetY + dy / scale, 0, st.MaxY), tx = Math.Clamp(st.TargetX + dx / scale, 0, st.MaxX);
            if (ty == st.TargetY && tx == st.TargetX) continue;
            box = box.Offset(-(tx - st.TargetX) * scale, -(ty - st.TargetY) * scale); // vị trí sau khi vùng này cuộn xong
            st.TargetX = tx; st.TargetY = ty;
            MarkViewDirty(area.ViewId, true, DirtyCause.Scroll);
        }
    }

    /// <summary>Áp yêu cầu cuộn đang chờ (nếu có) lên state vừa được cập nhật kích thước.</summary>
    void ApplyPendingScroll(long key, ScrollState st)
    {
        if (_pendingScroll.Count == 0 || !_pendingScroll.Remove((int)key, out var request)) return;
        if (request.X is { } x) st.TargetX = Math.Clamp(x, 0, st.MaxX);
        if (request.Y is { } y) st.TargetY = Math.Clamp(y, 0, st.MaxY);
        if (!request.Animate) { st.X = st.TargetX; st.Y = st.TargetY; }
    }

    /// <summary>Gọi handler OnScroll khi vị trí hoặc phạm vi cuộn đổi so với lần báo trước.</summary>
    void NotifyScroll(ref Node n, ScrollState st)
    {
        if (n.ScrollInvoke is not { } invoke || n.ScrollHandler is not { } handler || n.ScrollState is not { } state) return;
        if (st.NotifiedX == st.X && st.NotifiedY == st.Y && st.NotifiedMaxX == st.MaxX && st.NotifiedMaxY == st.MaxY) return;
        st.NotifiedX = st.X; st.NotifiedY = st.Y; st.NotifiedMaxX = st.MaxX; st.NotifiedMaxY = st.MaxY;
        _callbackFired = true;
        invoke(handler, state, new ScrollInfo(new Point(st.X, st.Y), new Point(st.MaxX, st.MaxY), st.Viewport));
    }
}
