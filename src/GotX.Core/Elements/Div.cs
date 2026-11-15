using GotX.Widgets;

namespace GotX;

/// <summary>
/// Handle tới một node trong arena (chỉ gồm tham chiếu arena + index, nằm trên stack).
/// Mọi utility Tailwind (<c>.Flex().P4().Bg(...)</c>) ghi thẳng vào Style trong arena.
/// </summary>
public readonly struct Div : IStyled
{
    readonly ElementTree _tree;
    readonly int _id;

    internal Div(ElementTree tree, int id) { _tree = tree; _id = id; }

    public int Id => _id;
    public ref Style Style => ref _tree[_id].Style;

    public Div Child(Div child) { _tree.Append(_id, child._id); return this; }

    /// <summary>Thêm một node chữ (tự xuống dòng theo độ rộng). Nên truyền string đã cache.</summary>
    public Div Child(string text) { _tree.Append(_id, _tree.NewText(text)); return this; }

    /// <summary>Style áp dụng khi chuột nằm trên element (chỉ thuộc tính vẽ, không đổi layout).</summary>
    public Div Hover(StyleRefiner refine) { _tree[_id].Hover = refine; return this; }

    /// <summary>
    /// Handler click kèm state. Dùng <c>static</c> lambda để không capture:
    /// <c>.OnClick(this, static (view, e) =&gt; view.Increment())</c>.
    /// </summary>
    public Div OnClick<TState>(TState state, Action<TState, MouseEvent> handler) where TState : class
    {
        ref var n = ref _tree[_id];
        n.Click = handler;
        n.ClickState = state;
        n.ClickInvoke = ClickInvoker<TState>.Instance;
        return this;
    }

    /// <summary>Handler cho nút phải / nút giữa (menu ngữ cảnh…); <c>e.Button</c> cho biết nút nào. OnClick chỉ nhận nút trái.</summary>
    public Div OnAuxClick<TState>(TState state, Action<TState, MouseEvent> handler) where TState : class
    {
        ref var n = ref _tree[_id];
        n.AuxClick = handler;
        n.AuxState = state;
        n.AuxInvoke = ClickInvoker<TState>.Instance;
        return this;
    }

    /// <summary>
    /// Gọi khi chuột đi vào (true) / rời khỏi (false) phần tử – dùng cho tooltip, hover card.
    /// Chỉ phát khi trạng thái đổi do chuột di chuyển; phần tử hiện ra ngay dưới con trỏ đứng yên thì chưa phát "vào".
    /// </summary>
    public Div OnHover<TState>(TState state, Action<TState, bool> handler) where TState : class
    {
        ref var n = ref _tree[_id];
        n.HoverHandler = handler;
        n.HoverState = state;
        n.HoverInvoke = HoverInvoker<TState>.Instance;
        return this;
    }

    /// <summary>
    /// Kéo chuột (nút trái): handler nhận Start khi nhấn trên phần tử, Move khi rê (kể cả ra ngoài), End khi thả.
    /// Dùng cho slider, thanh chia, kéo thanh cuộn.
    /// </summary>
    public Div OnDrag<TState>(TState state, Action<TState, DragEvent> handler) where TState : class
    {
        ref var n = ref _tree[_id];
        n.DragHandler = handler;
        n.DragState = state;
        n.DragInvoke = DragInvoker<TState>.Instance;
        return this;
    }

    /// <summary>
    /// Gọi khi vị trí hoặc phạm vi cuộn của vùng cuộn này đổi (lăn chuột, <c>Window.ScrollTo</c>, nội dung dài thêm).
    /// Handler chạy trong lúc vẽ: chỉ nên đổi state rồi Notify.
    /// </summary>
    public Div OnScroll<TState>(TState state, Action<TState, ScrollInfo> handler) where TState : class
    {
        ref var n = ref _tree[_id];
        n.ScrollHandler = handler;
        n.ScrollState = state;
        n.ScrollInvoke = ScrollInvoker<TState>.Instance;
        return this;
    }

    /// <summary>
    /// Phần tử nhận được focus bàn phím: là điểm dừng của Tab, bấm chuột vào cũng focus. Khi đang focus,
    /// Enter / Space gọi <c>OnClick</c>; variant <c>focus:</c> và <c>focus-visible:</c> (focus do bàn phím) được bật.
    /// Trong danh sách động nên đặt <c>.Key(...)</c> để focus bám đúng phần tử.
    /// </summary>
    public Div Focusable(bool focusable = true) { _tree[_id].Focusable = focusable; return this; }

    /// <summary>Tự nhận focus ở frame đầu tiên phần tử xuất hiện (nội dung menu, hộp thoại). Cần <c>.Focusable()</c>.</summary>
    public Div AutoFocus(bool autoFocus = true) { _tree[_id].AutoFocus = autoFocus; return this; }

    /// <summary>
    /// Nhận phím khi focus nằm trên phần tử này hoặc trên một phần tử con (kể cả ô nhập không dùng tới phím đó).
    /// Trả về true nếu đã xử lý; false để phím nổi tiếp lên phần tử cha, cuối cùng tới Tab / Escape mặc định.
    /// </summary>
    public Div OnKey<TState>(TState state, Func<TState, KeyEvent, bool> handler) where TState : class
    {
        ref var n = ref _tree[_id];
        n.KeyHandler = handler;
        n.KeyState = state;
        n.KeyInvoke = KeyInvoker<TState>.Instance;
        return this;
    }

    /// <summary>Đặt tên để overlay neo vào (<c>OverlayOptions.Anchor</c>) và để tra vị trí bằng <c>Window.AnchorBounds</c>.</summary>
    public Div Anchor(string name) { _tree[_id].Anchor = name; return this; }

    /// <summary>
    /// Áp chuỗi class Tailwind: <c>.Class("flex items-center gap-2 px-4 hover:bg-indigo-700 md:px-6")</c>.
    /// Chuỗi được biên dịch một lần rồi cache; class có variant trạng thái được đánh giá lúc vẽ.
    /// </summary>
    public Div Class(string classes)
    {
        var cl = Tailwind.Get(classes);
        ref var n = ref _tree[_id];
        cl.ApplyBase(ref n.Style, _tree.Variants);
        // Gọi .Class() nhiều lần: variant trạng thái của các lần trước vẫn phải còn hiệu lực (lần sau thắng khi trùng thuộc tính).
        if (cl.HasState) n.Classes = n.Classes is { } earlier && !ReferenceEquals(earlier, cl) ? Tailwind.Combine(earlier, cl) : cl;
        if (cl.IsGroup) n.IsGroup = true;
        if (_tree.Diagnostics) n.ClassText = n.ClassText is null ? classes : n.ClassText + " " + classes;
        return this;
    }

    /// <summary>
    /// Khoá ổn định cho phần tử có state qua các frame (vị trí cuộn, transition). Không đặt thì GotX
    /// dùng thứ tự xuất hiện trong view – đủ dùng khi cấu trúc view không đổi.
    /// </summary>
    public Div Key(string key) { _tree[_id].Key = _tree.KeyId(key); return this; }

    /// <summary>Vô hiệu hoá: bật variant disabled:, bỏ qua click.</summary>
    public Div Disabled(bool disabled = true) { _tree[_id].Disabled = disabled; return this; }

    internal Div FocusTarget(TextInput input) { _tree[_id].FocusInput = input; return this; }
}
