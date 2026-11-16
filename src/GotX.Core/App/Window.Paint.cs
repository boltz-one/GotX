using System.Runtime.InteropServices;
using GotX.Text;
using GotX.Widgets;

namespace GotX;

public sealed partial class Window
{
    int CurrentView => _viewStack.Count > 0 ? _viewStack[^1] : -1;

    void MarkAnimating() { if (CurrentView > 0) _animating.Add(CurrentView); }

    /// <summary>Khoá state của phần tử: (view, .Key() hoặc thứ tự phần tử có state trong view).</summary>
    long ElementKey(ref Node n)
    {
        int local = n.Key != 0 ? n.Key
            : _keyCounters.Count > 0 ? ++CollectionsMarshal.AsSpan(_keyCounters)[^1]
            : ++_rootKeyCounter;
        return ((long)(uint)CurrentView << 32) | (uint)local;
    }

    static bool Within(Bounds inner, Bounds outer) =>
        inner.X >= outer.X && inner.Y >= outer.Y && inner.Right <= outer.Right && inner.Bottom <= outer.Bottom;

    static Rgba Fade(Rgba c, float opacity) => opacity >= 1 ? c : c with { A = c.A * opacity };

    static Corners Grow(in Corners c, float d) => new(
        c.TopLeft > 0 ? MathF.Max(0, c.TopLeft + d) : 0, c.TopRight > 0 ? MathF.Max(0, c.TopRight + d) : 0,
        c.BottomRight > 0 ? MathF.Max(0, c.BottomRight + d) : 0, c.BottomLeft > 0 ? MathF.Max(0, c.BottomLeft + d) : 0);

    void Add(Primitive p)
    {
        p.ClipShape = _shape;
        _scene.Add(p);
    }

    /// <summary>
    /// Vẽ một node và cây con của nó. Thứ tự: trạng thái chuột → transition/animation → biến đổi riêng →
    /// bóng, nền, viền → nội dung → vùng cắt + cuộn → các con → đường kẻ, thanh cuộn → outline.
    /// Các bước lớn nằm trong Window.PaintParts.cs.
    /// </summary>
    void Paint(int id, in TextStyle parentText, Bounds clip, Xf xf)
    {
        ref var n = ref _tree[id];
        if (n.Style.Display == Display.None) return;
        var lb = n.Bounds; // toạ độ layout
        var s = n.Style;   // bản sao: variant trạng thái/transition chỉ sửa bản sao, không đổi arena

        // Phần tử focus được / nhận phím cần định danh giữ qua các frame (cùng không gian khoá với cuộn, transition).
        long focusKey = (n.Focusable || n.KeyHandler is not null) && !n.Disabled ? ElementKey(ref n) : 0;
        if (focusKey != 0 && n.AutoFocus) TouchAutoFocus(focusKey);
        bool inside = ApplyMouseState(ref n, ref s, lb, clip, xf, focusKey);

        // ---- Transition / animation ----
        long key = s.Transition != TransitionProps.None || s.ScrollX || s.ScrollY ? (focusKey != 0 ? focusKey : ElementKey(ref n)) : 0;
        var fromShadow = s.Shadow;
        float shadowMix = 1;
        if (s.Transition != TransitionProps.None) ApplyTransition(key, ref s, parentText, lb, ref fromShadow, ref shadowMix);
        float animTy = 0;
        if (s.Animation != AnimationKind.None)
        {
            Keyframes.Apply(s.Animation, _now, ref s.Opacity, ref s.ScaleX, ref s.ScaleY, ref animTy, ref s.Rotate);
            MarkAnimating();
        }

        var ts = parentText.Refine(s, RemSize);
        float op = ts.Opacity;

        // ---- Biến đổi riêng theo CSS: translate → rotate → scale → skew quanh transform-origin ----
        float tx = s.TranslateX.Resolve(lb.Width, RemSize) ?? 0;
        float ty = (s.TranslateY.Resolve(lb.Height, RemSize) ?? 0) + animTy * lb.Height;
        var nodeXf = xf;
        if (s.ScaleX != 1 || s.ScaleY != 1 || tx != 0 || ty != 0 || s.Rotate != 0 || s.SkewX != 0 || s.SkewY != 0)
        {
            var origin = new Point(lb.X + (s.OriginX.Resolve(lb.Width, RemSize) ?? lb.Width / 2),
                                   lb.Y + (s.OriginY.Resolve(lb.Height, RemSize) ?? lb.Height / 2));
            nodeXf = xf.Then(Xf.Element(origin, tx, ty, s.Rotate, s.ScaleX, s.ScaleY, s.SkewX, s.SkewY));
        }
        var deviceClip = clip.Scale(_scale);
        var bw = s.BorderStyle == BorderStyle.None ? default : s.Border;

        PaintBackdrop(s, ts, lb, bw, op, fromShadow, shadowMix, deviceClip, nodeXf);

        if (n.Kind == NodeKind.Text) PaintText(n.Text!, lb, ts, deviceClip, nodeXf, clip);
        else if (n.Kind == NodeKind.Input) PaintInput(n.Input!, lb, ts, deviceClip, nodeXf);
        else if (n.Vector is { } vector) PaintVector(vector, s, ts, lb, bw, deviceClip, nodeXf);
        else if (n.Image is { } image) PaintImage(image, s, lb, bw, op, deviceClip, nodeXf);

        if (n.Click is { } click && !n.Disabled)
            _hitboxes.Add(new Hitbox { Local = lb, Xf = nodeXf, Clip = clip, Handler = click, State = n.ClickState, Invoke = n.ClickInvoke, ViewId = CurrentView });
        if (n.AuxClick is { } aux && !n.Disabled)
            _hitboxes.Add(new Hitbox { Local = lb, Xf = nodeXf, Clip = clip, Handler = aux, State = n.AuxState, Invoke = n.AuxInvoke, ViewId = CurrentView, Aux = true });
        if (n.DragHandler is { } drag && !n.Disabled)
            _hitboxes.Add(new Hitbox { Local = lb, Xf = nodeXf, Clip = clip, Handler = drag, State = n.DragState, DragInvoke = n.DragInvoke, ViewId = CurrentView });
        if (n.Anchor is { } anchorName)
            _hitboxes.Add(new Hitbox { Local = lb, Xf = nodeXf, Clip = clip, Anchor = anchorName, ViewId = CurrentView, ScrollParent = _scrollScope });
        if (n.FocusInput is { } focusInput && !n.Disabled)
            _hitboxes.Add(new Hitbox { Local = lb, Xf = nodeXf, Clip = clip, Input = focusInput, ViewId = CurrentView, KeyParent = _keyScope, ScrollParent = _scrollScope });
        int outerKeyScope = _keyScope, outerScrollScope = _scrollScope;
        if (focusKey != 0)
        {
            _hitboxes.Add(new Hitbox
            {
                Local = lb, Xf = nodeXf, Clip = clip, ViewId = CurrentView, IsKey = true, Focusable = n.Focusable, FocusKey = focusKey,
                KeyHandler = n.KeyHandler, KeyState = n.KeyState, KeyInvoke = n.KeyInvoke, KeyParent = _keyScope,
                Handler = n.Click, State = n.ClickState, Invoke = n.ClickInvoke, ScrollParent = _scrollScope,
            });
            _keyScope = _hitboxes.Count; // 1-based: các phần tử con nổi phím lên mục này
        }
        if (_inspect is not null) InspectBegin(id, ref n, s, ts, lb, clip, nodeXf);
        if (_sem is not null) SemBegin(ref n, focusKey, lb, clip, nodeXf);

        // ---- Vùng cắt + cuộn cho các con ----
        var childClip = clip;
        var childXf = nodeXf;
        int savedShape = s.Overflow == Overflow.Hidden ? PushClip(s, lb, bw, clip, nodeXf, ref childClip) : -1;
        ScrollState? scroll = null;
        if (s.ScrollX || s.ScrollY)
        {
            scroll = UpdateScroll(key, n.ContentSize, lb, s.ScrollX, s.ScrollY);
            NotifyScroll(ref n, scroll);
            _hitboxes.Add(new Hitbox { Local = lb, Xf = nodeXf, Clip = clip, ScrollKey = key, ViewId = CurrentView, ScrollParent = _scrollScope });
            _scrollScope = _hitboxes.Count;
            // Lượng cuộn được làm tròn theo pixel thiết bị → chữ không bị rung, phát lại chính xác
            float px = nodeXf.S * _scale;
            childXf = nodeXf.Then(Xf.Translation(-MathF.Round(scroll.X * px) / px, -MathF.Round(scroll.Y * px) / px));
            NoteScroller(key, scroll, nodeXf, childClip, px);
        }

        bool groupHovered = n.IsGroup && inside;
        if (groupHovered) _groupHover++;
        if (n.Kind == NodeKind.View) PaintViewContent(id, ts, childClip, childXf);
        else PaintChildren(id, ts, childClip, childXf);
        if (groupHovered) _groupHover--;

        if (s.DivideX > 0 || s.DivideY > 0) PaintDividers(id, s, Fade(s.DivideColor ?? s.BorderColorValue ?? ts.Color, op), childClip.Scale(_scale), childXf);
        if (scroll is not null && ScrollbarVisible(scroll, s.ScrollbarMode, inside)) PaintScrollbars(scroll, s, lb, bw, nodeXf, childClip.Scale(_scale), op);
        if (savedShape >= 0) _shape = savedShape;

        if (s.OutlineWidth > 0 && s.OutlineStyle != BorderStyle.None) PaintOutline(s, ts, lb, op, deviceClip, nodeXf);
        _keyScope = outerKeyScope;
        _scrollScope = outerScrollScope;
        if (_inspect is not null) InspectEnd();
        if (_sem is not null) SemEnd();
    }

    /// <summary>
    /// divide-x / divide-y: đường kẻ ở mép đầu của mọi con trong luồng trừ con đầu tiên
    /// (giống CSS: border-top / border-left cho các phần tử từ con thứ hai).
    /// </summary>
    void PaintDividers(int id, in Style s, Rgba color, Bounds deviceClip, Xf xf)
    {
        bool first = true;
        for (int c = _tree[id].FirstChild; c >= 0; c = _tree[c].NextSibling)
        {
            ref var cn = ref _tree[c];
            if (cn.Style.Display == Display.None || cn.Style.Position == Position.Absolute) continue;
            if (first) { first = false; continue; }
            var cb = cn.Bounds;
            if (s.DivideY > 0) AddRect(new Bounds(cb.X, cb.Y, cb.Width, s.DivideY), color, deviceClip, xf);
            if (s.DivideX > 0) AddRect(new Bounds(cb.X, cb.Y, s.DivideX, cb.Height), color, deviceClip, xf);
        }
    }

    /// <summary>Con trong luồng trước, absolute sau (nằm trên), như CSS.</summary>
    void PaintChildren(int id, in TextStyle ts, Bounds clip, Xf xf)
    {
        ref var n = ref _tree[id];
        bool anyAbsolute = false;
        for (int c = n.FirstChild; c >= 0; c = _tree[c].NextSibling)
        {
            if (_tree[c].Style.Position == Position.Absolute) { anyAbsolute = true; continue; }
            Paint(c, ts, clip, xf);
        }
        if (anyAbsolute)
            for (int c = n.FirstChild; c >= 0; c = _tree[c].NextSibling)
                if (_tree[c].Style.Position == Position.Absolute) Paint(c, ts, clip, xf);
    }
}
