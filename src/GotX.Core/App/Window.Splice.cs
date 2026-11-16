using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GotX;

public sealed partial class Window
{
    /// <summary>
    /// Thay đoạn của view <paramref name="vid"/> trong scene / hitbox / hover / thứ tự view / bảng cache đang giữ bằng đoạn vừa dựng
    /// trong bộ đệm tạm. Chỉ số cục bộ (vùng cắt hình, gradient, hitbox bao ngoài, các Start/End) được đổi sang chỉ số toàn cục;
    /// phần đuôi (và Start/End của tổ tiên) được dời theo độ lệch. false (chưa đụng gì) nếu không ghép an toàn được.
    /// </summary>
    bool MergeSegment(int vid, ViewCache old, Scene sc)
    {
        int n = sc.Count, m = _sHit.Count, k = _sHov.Count, q = _sOrd.Count;
        int dP = n - (old.PrimEnd - old.PrimStart), dH = m - (old.HitEnd - old.HitStart);
        int dV = k - (old.HoverEnd - old.HoverStart), dO = q - (old.OrderEnd - old.OrderStart);
        // Sàn hover (modal) nằm sát đoạn mà số vùng hover đổi: không phân biệt được trước/sau → dựng đầy đủ.
        if (dV != 0 && _hoverFloor >= old.HoverStart && _hoverFloor <= old.HoverEnd) return Fail("hover-floor");

        int outer = old.OuterShape, sOff = outer != 0 ? 1 : 0, gradBase = old.GradStart;
        int MapShape(int s) => s <= 0 ? s : sOff == 1 && s == 1 ? outer : old.ShapeStart + s - sOff;
        int MapScope(int x, int outerScope) => x == 0 ? outerScope : x + old.HitStart;

        bool track = SnapshotOldSegment(old);
        // ---- primitive ----
        for (int i = 0; i < n; i++)
        {
            ref var p = ref sc.Item(i);
            p.ClipShape = MapShape(p.ClipShape);
            if (p.Gradient != 0 && p.Kind != PrimitiveKind.Shadow) p.Gradient += gradBase;
        }
        int shapes = sc.ShapeCount - sOff;
        if (shapes > 0) _scene.OverwriteShapes(old.ShapeStart, sc, sOff, shapes);
        if (sc.GradientCount > 0) _scene.OverwriteGradients(old.GradStart, sc, 0, sc.GradientCount);

        // (vùng cắt / gradient đã ghi đè và chỉ số đã ánh xạ ở trên; hash mới so với hash cũ ở sau khi thay đoạn)
        // ---- hitbox ----
        var hits = CollectionsMarshal.AsSpan(_sHit);
        for (int i = 0; i < hits.Length; i++)
        {
            ref var hb = ref hits[i];
            if (hb.IsKey || hb.Input is not null) hb.KeyParent = MapScope(hb.KeyParent, old.KeyScope);
            if (hb.ScrollParent != 0 || hb.IsKey || hb.Input is not null || hb.Anchor is not null || hb.ScrollKey != 0)
                hb.ScrollParent = MapScope(hb.ScrollParent, old.ScrollScope);
        }

        // ---- thay đoạn ----
        for (int i = old.OrderStart; i < old.OrderEnd; i++)
        {
            int gone = _viewOrder[i];
            _cache.Remove(gone);
            if (!_sCache.ContainsKey(gone)) _snaps.Remove(gone); // view không còn (danh sách ảo thay dòng khi cuộn): không để bảng snap phình mãi
        }
        _scene.ReplaceRange(old.PrimStart, old.PrimEnd, sc, 0, n);
        if (track) RecordSegmentDamage(old, n);
        else Damage.MarkFull();
        _hitboxes.RemoveRange(old.HitStart, old.HitEnd - old.HitStart); _hitboxes.InsertRange(old.HitStart, _sHit);
        _hovers.RemoveRange(old.HoverStart, old.HoverEnd - old.HoverStart); _hovers.InsertRange(old.HoverStart, _sHov);
        _viewOrder.RemoveRange(old.OrderStart, old.OrderEnd - old.OrderStart); _viewOrder.InsertRange(old.OrderStart, _sOrd);
        foreach (var kv in _sCache)
        {
            var c = kv.Value;
            c.PrimStart += old.PrimStart; c.PrimEnd += old.PrimStart;
            c.HitStart += old.HitStart; c.HitEnd += old.HitStart;
            c.HoverStart += old.HoverStart; c.HoverEnd += old.HoverStart;
            c.OrderStart += old.OrderStart; c.OrderEnd += old.OrderStart;
            c.ShapeStart += old.ShapeStart - sOff; c.ShapeEnd += old.ShapeStart - sOff;
            c.GradStart += gradBase; c.GradEnd += gradBase;
            c.OuterShape = MapShape(c.OuterShape);
            c.KeyScope = MapScope(c.KeyScope, old.KeyScope);
            c.ScrollScope = MapScope(c.ScrollScope, old.ScrollScope);
            _cache[kv.Key] = c;
        }

        // ---- dời phần còn lại theo độ lệch ----
        if (dP != 0 || dH != 0 || dV != 0 || dO != 0)
        {
            for (int p = old.ParentView, guard = 0; p > 0 && guard < 512; guard++)
            {
                ref var a = ref CollectionsMarshal.GetValueRefOrNullRef(_cache, p);
                if (Unsafe.IsNullRef(ref a)) break;
                a.PrimEnd += dP; a.HitEnd += dH; a.HoverEnd += dV; a.OrderEnd += dO;
                p = a.ParentView;
            }
            for (int i = old.OrderStart + q; i < _viewOrder.Count; i++)
            {
                ref var e = ref CollectionsMarshal.GetValueRefOrNullRef(_cache, _viewOrder[i]);
                if (Unsafe.IsNullRef(ref e)) continue;
                e.PrimStart += dP; e.PrimEnd += dP; e.HitStart += dH; e.HitEnd += dH;
                e.HoverStart += dV; e.HoverEnd += dV; e.OrderStart += dO; e.OrderEnd += dO;
                if (e.KeyScope > old.HitEnd) e.KeyScope += dH;
                if (e.ScrollScope > old.HitEnd) e.ScrollScope += dH;
            }
            if (dH != 0)
            {
                var all = CollectionsMarshal.AsSpan(_hitboxes);
                for (int i = old.HitStart + m; i < all.Length; i++)
                {
                    if (all[i].KeyParent > old.HitEnd) all[i].KeyParent += dH;
                    if (all[i].ScrollParent > old.HitEnd) all[i].ScrollParent += dH;
                }
                if (_pressed >= old.HitEnd) _pressed += dH;
                else if (_pressed >= old.HitStart) _pressed = -1;
            }
            if (_hoverFloor > old.HoverEnd) _hoverFloor += dV;
        }
        return true;
    }
}
