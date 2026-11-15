using System.Diagnostics;
using System.Runtime.InteropServices;
using GotX.Text;
using GotX.Widgets;

namespace GotX;

public sealed partial class Window
{
    // ---- Được LayoutEngine gọi ----

    /// <summary>Min-content của view từ frame trước, nếu view không bẩn và nhận cùng kiểu chữ.</summary>
    internal bool TryCachedMinContent(View v, in TextStyle text, out float width)
    {
        width = 0;
        if (!CacheEnabled || _dirty.Contains(v.Id.Value) || !_prevCache.TryGetValue(v.Id.Value, out var c) || c.Text != text) return false;
        width = c.MinContent;
        return !float.IsNaN(width);
    }

    internal bool TryCachedMeasure(View v, float availW, float availH, in TextStyle text, out Size size)
    {
        size = default;
        return CacheEnabled && !_dirty.Contains(v.Id.Value)
            && _prevCache.TryGetValue(v.Id.Value, out var c)
            && c.Text == text && c.Measures.TryGet(availW, availH, out size);
    }

    internal bool CanReplay(View v, Bounds b, in TextStyle text) =>
        CacheEnabled && !_dirty.Contains(v.Id.Value)
        && _prevCache.TryGetValue(v.Id.Value, out var c)
        && c.Bounds == b && c.Text == text;

    internal void RenderView(int id)
    {
        ref var n = ref _tree[id];
        if (n.ViewRendered) return;
        n.ViewRendered = true;
        if (Trace is not null) Emit(TraceKind.ViewRender, n.View!.GetType().Name, viewId: n.View.Id.Value);
        long t0 = Log is null ? 0 : Stopwatch.GetTimestamp();
        int child = n.View!.Render(this).Id;
        if (Log is not null) NoteRender(n.View, Stopwatch.GetTimestamp() - t0);
        _tree.Append(id, child);
        _rendered++;
    }

    // ---- Notify → bẩn view và các view cha ----

    internal void MarkViewDirty(int id) => MarkViewDirty(id, request: true, DirtyCause.Notify);

    void MarkViewDirty(int id, bool request, DirtyCause cause)
    {
        if (!_cache.ContainsKey(id)) return; // không phải view đang hiển thị
        if (request && Trace is not null && !_dirty.Contains(id))
            Emit(TraceKind.ViewDirty, App.Find(id)?.GetType().Name ?? "?", viewId: id);
        if (Log is not null && !_dirty.Contains(id)) NoteDirty(id, cause);
        for (int v = id; v > 0 && _dirty.Add(v);)
            v = _cache.TryGetValue(v, out var c) ? c.ParentView : -1;
        if (request) RequestFrame();
    }

    /// <summary>Có yêu cầu vẽ lại chưa được đáp ứng (một view bị Notify, focus đổi… kể từ lần Draw gần nhất).</summary>
    public bool FramePending => _framePending;
    bool _framePending, _drawing;

    void RequestFrame()
    {
        _needsFrame = true;
        _framePending = true;
        FrameRequested?.Invoke();
    }
}
