using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using GotX.Text;
using GotX.Widgets;

namespace GotX;

public sealed partial class Window
{
    void PaintViewContent(int id, in TextStyle ts, Bounds clip, Xf xf)
    {
        ref var n = ref _tree[id];
        int vid = n.View!.Id.Value;
        if (n.Replay)
        {
            var prev = _prevCache[vid];
            bool sameShape = prev.Clip == clip && prev.Xf.SameLinear(xf) && SameOuterShape(prev.OuterShape);
            bool moved = prev.Xf.Tx != xf.Tx || prev.Xf.Ty != xf.Ty;
            if (sameShape && (!moved || !prev.InnerClip)) { Replay(vid, prev, xf); return; }
            // Clip/tỉ lệ đổi, hoặc bị dời mà bên trong có vùng cắt → render + layout lại tại chỗ.
            n.Replay = false;
            RenderView(id);
            _layout.ArrangeChildren(id);
        }

        int primStart = _scene.Count, hitStart = _hitboxes.Count, hovStart = _hovers.Count, orderStart = _viewOrder.Count;
        int shapeStart = _scene.ShapeCount, outerShape = _shape, gradStart = _scene.GradientCount;
        int parent = CurrentView;
        _viewOrder.Add(vid);
        _paintedViews.Add(vid);
        _viewStack.Add(vid);
        _keyCounters.Add(0);
        _innerClip.Add(false);
        PaintChildren(id, ts, clip, xf);
        bool inner = _innerClip[^1];
        _viewStack.RemoveAt(_viewStack.Count - 1);
        _keyCounters.RemoveAt(_keyCounters.Count - 1);
        _innerClip.RemoveAt(_innerClip.Count - 1);
        if (inner && _innerClip.Count > 0) _innerClip[^1] = true;

        _cache[vid] = new ViewCache
        {
            ParentView = parent,
            Measures = n.Memo, MinContent = n.HasMinContent ? n.MinContent : float.NaN,
            Bounds = n.Bounds, Clip = clip, Xf = xf, InnerClip = inner, Text = n.InheritedText,
            OuterShape = outerShape, ShapeStart = shapeStart, ShapeEnd = _scene.ShapeCount,
            GradStart = gradStart, GradEnd = _scene.GradientCount,
            PrimStart = primStart, PrimEnd = _scene.Count,
            HitStart = hitStart, HitEnd = _hitboxes.Count,
            HoverStart = hovStart, HoverEnd = _hovers.Count,
            OrderStart = orderStart, OrderEnd = _viewOrder.Count,
        };
    }

    bool SameOuterShape(int prevShape) =>
        prevShape == 0 ? _shape == 0 : _shape != 0 && _prevScene.Shape(prevShape) == _scene.Shape(_shape);

    /// <summary>
    /// Chép kết quả vẽ của view (và mọi view con) từ frame trước. Nếu view chỉ bị dời (cuộn, translate
    /// của tổ tiên) thì tịnh tiến luôn các primitive, hitbox, vùng hover theo số pixel thiết bị nguyên.
    /// </summary>
    void Replay(int vid, in ViewCache prev, Xf xf)
    {
        int primOff = _scene.Count - prev.PrimStart;
        int hitOff = _hitboxes.Count - prev.HitStart;
        int hovOff = _hovers.Count - prev.HoverStart;
        int orderOff = _viewOrder.Count - prev.OrderStart;
        float ddx = MathF.Round((xf.Tx - prev.Xf.Tx) * _scale), ddy = MathF.Round((xf.Ty - prev.Xf.Ty) * _scale);
        float ldx = ddx / _scale, ldy = ddy / _scale;
        bool moved = ddx != 0 || ddy != 0;

        // Vùng cắt bên trong view (overflow-hidden của phần tử con) dời theo nội dung rồi xén lại bằng vùng cắt ngoài.
        var outer = prev.Clip;
        Bounds MoveClip(Bounds c) => c == outer ? c : c.Offset(ldx, ldy).Intersect(outer);

        // Vùng cắt theo hình: cái tạo bên trong view được chép (và dời theo), cái bao ngoài ánh xạ sang frame này
        int shapeBase = _scene.ShapeCount;
        _scene.AddShapes(_prevScene, prev.ShapeStart, prev.ShapeEnd, ddx, ddy);
        int prevOuter = prev.OuterShape, curOuter = _shape, prevShapeStart = prev.ShapeStart, prevShapeEnd = prev.ShapeEnd;
        int MapShape(int old) => old == 0 ? 0
            : old == prevOuter ? curOuter
            : old - 1 >= prevShapeStart && old - 1 < prevShapeEnd ? shapeBase + (old - 1 - prevShapeStart) + 1
            : curOuter;

        // Gradient luôn do chính primitive trong view tạo ra → chỉ cần dời chỉ số sang vị trí mới trong bảng.
        int gradOff = _scene.GradientCount - prev.GradStart;
        _scene.AddGradients(_prevScene, prev.GradStart, prev.GradEnd);

        int first = _scene.Count;
        _scene.AddRange(_prevScene, prev.PrimStart, prev.PrimEnd);
        if (moved) _scene.TranslateRange(first, _scene.Count, ddx, ddy, outer.Scale(_scale));
        bool remapShapes = prev.ShapeEnd > prev.ShapeStart || prevOuter != curOuter;
        if (remapShapes || (prev.GradEnd > prev.GradStart && gradOff != 0))
            for (int i = first; i < _scene.Count; i++)
            {
                ref var item = ref _scene.Item(i);
                if (remapShapes && item.ClipShape != 0) item.ClipShape = MapShape(item.ClipShape);
                if (item.Gradient != 0) item.Gradient += gradOff;
            }
        for (int i = prev.HitStart; i < prev.HitEnd; i++)
        {
            var hb = _prevHitboxes[i];
            if (moved)
            {
                hb.Xf = hb.Xf.Translate(ldx, ldy);
                hb.Clip = MoveClip(hb.Clip);
                if (hb.Input is not null && hb.ScrollKey == 0) hb.Input.Translate(ldx, ldy);
            }
            // Mục bàn phím bao ngoài: trong đoạn phát lại thì dời chỉ số, ngoài đoạn thì nối vào mục đang mở lúc này.
            if (hb.IsKey || hb.Input is not null) hb.KeyParent = hb.KeyParent > prev.HitStart ? hb.KeyParent + hitOff : _keyScope;
            if (hb.ScrollParent != 0 || hb.IsKey || hb.Input is not null || hb.Anchor is not null || hb.ScrollKey != 0)
                hb.ScrollParent = hb.ScrollParent > prev.HitStart ? hb.ScrollParent + hitOff : _scrollScope;
            _hitboxes.Add(hb);
        }
        for (int i = prev.HoverStart; i < prev.HoverEnd; i++)
        {
            var r = _prevHovers[i];
            if (moved) { r.Xf = r.Xf.Translate(ldx, ldy); r.Clip = MoveClip(r.Clip); }
            _hovers.Add(r);
        }

        int parent = CurrentView;
        for (int k = prev.OrderStart; k < prev.OrderEnd; k++)
        {
            int v = _prevViewOrder[k];
            _viewOrder.Add(v);
            var c = _prevCache[v];
            c.PrimStart += primOff; c.PrimEnd += primOff;
            c.HitStart += hitOff; c.HitEnd += hitOff;
            c.HoverStart += hovOff; c.HoverEnd += hovOff;
            c.OrderStart += orderOff; c.OrderEnd += orderOff;
            c.Xf = c.Xf.Translate(ldx, ldy);
            if (moved && v != vid) c.Clip = MoveClip(c.Clip);
            c.OuterShape = MapShape(c.OuterShape);
            c.ShapeStart += shapeBase - prevShapeStart; c.ShapeEnd += shapeBase - prevShapeStart;
            c.GradStart += gradOff; c.GradEnd += gradOff;
            if (v == vid) c.ParentView = parent;
            _cache[v] = c;
        }
        if (prev.InnerClip && _innerClip.Count > 0) _innerClip[^1] = true;
        _replayed++;
    }
}
