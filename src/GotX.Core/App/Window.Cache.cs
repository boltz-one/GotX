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
            float dx = xf.Tx - prev.Xf.Tx, dy = xf.Ty - prev.Xf.Ty;
            bool moved = dx != 0 || dy != 0;
            float ddx = MathF.Round(dx * _scale), ddy = MathF.Round(dy * _scale);
            if (prev.Culled)
            {
                // Vẫn nằm ngoài vùng cắt: giữ đoạn rỗng (không chép gì). Vào lại tầm nhìn: dựng lại bên dưới.
                if (CullViews && prev.Xf.SameLinear(xf) && Offscreen(prev.Extent.Offset(ddx, ddy), clip)) { Replay(vid, prev, xf, clip, culled: true); return; }
            }
            else
            {
                bool sameShape = prev.Xf.SameLinear(xf) && SameOuterShape(prev.OuterShape, ddx, ddy);
                if (sameShape && ((!moved && prev.Clip == clip) || ClipsAllowReplay(prev.Clips, dx, dy, clip)) && !CulledChildVisible(prev, ddx, ddy, clip))
                {
                    bool cullIt = false;
                    if (CullCandidate(prev.PrimEnd - prev.PrimStart, clip))
                    {
                        var ext = prev.ExtentKnown ? prev.Extent : ViewExtent(_prevScene, prev.PrimStart, prev.PrimEnd);
                        cullIt = Offscreen(ext.Offset(ddx, ddy), clip);
                        _replayExtent = ext; _replayExtentKnown = true;
                    }
                    Replay(vid, prev, xf, clip, cullIt);
                    return;
                }
            }
            // Tỉ lệ / hình bao đổi, hoặc bị dời / đổi vùng cắt ngoài mà vùng cắt bên trong không còn tịnh tiến được → render + layout lại tại chỗ.
            n.Replay = false;
            RenderView(id);
            _layout.ArrangeChildren(id);
        }

        int primStart = _scene.Count, hitStart = _hitboxes.Count, hovStart = _hovers.Count, orderStart = _viewOrder.Count, culledStart = _culledTotal;
        int shapeStart = _scene.ShapeCount, outerShape = _shape, gradStart = _scene.GradientCount;
        int parent = CurrentView, keyScope = _keyScope, scrollScope = _scrollScope;
        _viewOrder.Add(vid);
        _paintedViews.Add(vid);
        _viewStack.Add(vid);
        _keyCounters.Add(0);
        _clipAcc.Add(default);
        PaintChildren(id, ts, clip, xf);
        _viewStack.RemoveAt(_viewStack.Count - 1);
        _keyCounters.RemoveAt(_keyCounters.Count - 1);
        var clips = PopClipAcc();

        _snaps[vid] = new ViewSnap { Style = n.Style, Refined = ts };
        bool cand = CullCandidate(_scene.Count - primStart, clip);
        var extent = cand ? ViewExtent(primStart, _scene.Count) : default;
        bool cull = cand && Offscreen(extent, clip);
        if (cull) DiscardView(primStart, shapeStart, gradStart, orderStart);
        _cache[vid] = new ViewCache
        {
            KeyScope = keyScope, ScrollScope = scrollScope, GroupHover = _groupHover, BlockHover = _blockHover,
            ParentView = parent,
            Measures = n.Memo, MinContent = n.HasMinContent ? n.MinContent : float.NaN,
            Bounds = n.Bounds, Clip = clip, Xf = xf, Clips = clips, Text = n.InheritedText,
            OuterShape = outerShape, ShapeStart = shapeStart, ShapeEnd = _scene.ShapeCount,
            GradStart = gradStart, GradEnd = _scene.GradientCount,
            PrimStart = primStart, PrimEnd = _scene.Count,
            HitStart = hitStart, HitEnd = _hitboxes.Count,
            HoverStart = hovStart, HoverEnd = _hovers.Count,
            OrderStart = orderStart, OrderEnd = _viewOrder.Count,
            Extent = extent, ExtentKnown = cand, Culled = cull, HasCulled = _culledTotal != culledStart,
        };
    }
}
