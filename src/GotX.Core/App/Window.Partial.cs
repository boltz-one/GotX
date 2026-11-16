using System.Runtime.InteropServices;
using GotX.Text;

namespace GotX;

/// <summary>
/// Khung một phần: khi chỉ vài view bị Notify trực tiếp (gõ phím, nháy con trỏ, hover, animation) mà bố cục bên ngoài không đổi,
/// chỉ các view đó được Render + layout + vẽ lại; đoạn primitive/hitbox/hover cũ của chúng được thay TẠI CHỖ trong scene đang giữ
/// (<see cref="MergeSegment"/>), phần còn lại của frame không bị chép, không bị đo, không bị Render.
/// Điều kiện (mọi thứ khác → khung đầy đủ như trước, không mất tính đúng):
/// – view tạo ranh giới layout: mọi kết quả đo (kích thước khả dụng → kích thước) và min-content khớp với lần đo cha đã thấy,
///   nên layout của cha / anh em không đổi (relayout boundary);
/// – số vùng cắt hình / gradient, cờ cắt trong của view không đổi (chỉ số của phần còn lại không phải dời).
/// </summary>
public sealed partial class Window
{
    readonly HashSet<int> _self = new();               // view bị Notify / hover / focus / cuộn trực tiếp (không tính tổ tiên)
    readonly Dictionary<int, ViewSnap> _snaps = new();
    readonly List<int> _tops = new();
    View? _lastRoot;
    float _lastRem;
    TextStyle _lastBase;
    int _overlayStamp, _lastOverlayStamp;
    Scene? _scratch;
    readonly List<Hitbox> _sHit = new();
    readonly List<HoverRegion> _sHov = new();
    readonly List<int> _sOrd = new();
    readonly Dictionary<int, ViewCache> _sCache = new();

    /// <summary>Tắt bằng <c>GOTX_PARTIAL=0</c> hoặc gán false: mọi frame dựng đầy đủ (view không đổi vẫn phát lại từ cache).</summary>
    public static bool PartialDefault { get; set; } = Environment.GetEnvironmentVariable("GOTX_PARTIAL") != "0";
    public bool PartialFrames { get; set; } = PartialDefault;
    /// <summary>Frame vừa rồi chỉ vẽ lại các view bẩn (không dựng cả cây).</summary>
    public bool LastFramePartial { get; private set; }
    public int PartialFrameCount { get; private set; }
    public int PartialFallbackCount { get; private set; }
    /// <summary>Lý do khung một phần gần nhất phải lùi về khung đầy đủ (chẩn đoán, test).</summary>
    public string? PartialFallbackReason { get; private set; }
    bool _fellBack;
    int _preView, _preChild = -1; // kết quả Render của view vừa thử vẽ một phần mà thất bại (tái dùng ở khung đầy đủ)
    bool Fail(string why) { PartialFallbackReason = why; if (s_debug) Console.Error.WriteLine("partial-fallback: " + why); return false; }
    static readonly bool s_debug = Environment.GetEnvironmentVariable("GOTX_PARTIAL_DEBUG") == "1";

    int ParentOf(int view) => _cache.TryGetValue(view, out var c) ? c.ParentView : -1;

    bool CanPartial(Size size, float scale, int bucket, GlyphAtlas atlas) =>
        PartialFrames && CacheEnabled && _frame > 0 && _cache.Count > 0 && _inspect is null && _sem is null
        && scale == _scale && bucket == _lastBucket && atlas.Generation == _atlasGeneration && size == _lastSize
        && ReferenceEquals(Root, _lastRoot) && RemSize == _lastRem && BaseTextStyle == _lastBase && _overlayStamp == _lastOverlayStamp;

    /// <summary>Vẽ lại các view bẩn cao nhất. false = phải dựng khung đầy đủ (scene vẫn nhất quán nếu đã thay một phần).</summary>
    bool TryPartial()
    {
        _tops.Clear();
        foreach (int v in _self)
        {
            if (!_cache.ContainsKey(v) || !_snaps.TryGetValue(v, out var snap) || App.Find(v) is not global::GotX.View) return Fail("no-cache");
            if (snap.Style.ScrollX || snap.Style.ScrollY) return Fail("scroller");
            bool top = true;
            for (int p = ParentOf(v), guard = 0; p > 0 && guard < 512; p = ParentOf(p), guard++)
                if (_self.Contains(p)) { top = false; break; }
            if (top) _tops.Add(v);
        }
        foreach (int v in _tops)
            if (!RepaintView(v)) return false;
        return true;
    }

    bool RepaintView(int vid)
    {
        var old = _cache[vid];
        var snap = _snaps[vid];
        var view = (global::GotX.View)App.Find(vid)!;
        var sc = _scratch ??= new Scene(Text.Atlas);
        sc.Clear(); _sHit.Clear(); _sHov.Clear(); _sOrd.Clear(); _sCache.Clear();

        var (liveScene, liveHit, liveHov, liveOrd, liveCache) = (_scene, _hitboxes, _hovers, _viewOrder, _cache);
        var (prevScene, prevHit, prevHov, prevOrd, prevCache) = (_prevScene, _prevHitboxes, _prevHovers, _prevViewOrder, _prevCache);
        (_prevScene, _prevHitboxes, _prevHovers, _prevViewOrder, _prevCache) = (liveScene, liveHit, liveHov, liveOrd, liveCache);
        (_scene, _hitboxes, _hovers, _viewOrder, _cache) = (sc, _sHit, _sHov, _sOrd, _sCache);
        bool ok;
        try { ok = BuildSegment(vid, old, snap, view, liveScene); }
        finally
        {
            (_scene, _hitboxes, _hovers, _viewOrder, _cache) = (liveScene, liveHit, liveHov, liveOrd, liveCache);
            (_prevScene, _prevHitboxes, _prevHovers, _prevViewOrder, _prevCache) = (prevScene, prevHit, prevHov, prevOrd, prevCache);
        }
        if (!ok || !MergeSegment(vid, old, sc)) return false;
        _preChild = -1;
        return true;
    }

    bool BuildSegment(int vid, in ViewCache old, in ViewSnap snap, global::GotX.View view, Scene live)
    {
        long tStart = System.Diagnostics.Stopwatch.GetTimestamp();
        _tree.Reset();
        int id = _tree.NewView(view).Id;
        {
            ref var n = ref _tree[id];
            n.Style = snap.Style; n.Bounds = old.Bounds; n.InheritedText = old.Text;
        }
        RenderView(id);
        _preView = vid; _preChild = _tree[id].FirstChild; // lùi về khung đầy đủ thì tái dùng kết quả Render này
        // Ranh giới layout: cha đã đo view với các cặp (rộng, cao) trong bộ nhớ đo; kích thước mới phải y hệt thì cha không phải layout lại.
        var memo = old.Measures;
        for (int i = 0; i < memo.Count; i++)
        {
            memo.At(i, out float w, out float h, out var size);
            var now = _layout.MeasureNode(id, old.Text, w, h, RemSize);
            if (now.Width != size.Width || now.Height != size.Height) return Fail("size");
        }
        if (!float.IsNaN(old.MinContent) && _layout.MinContentOf(id, old.Text, RemSize) != old.MinContent) return Fail("min-content");
        _layout.ArrangeChildren(id);
        long tPaint = System.Diagnostics.Stopwatch.GetTimestamp();
        _layoutTicks += tPaint - tStart;

        // Ngữ cảnh lúc vào view. Chỉ số hitbox bao ngoài dùng 0 làm "ở ngoài đoạn" (được ánh xạ lại khi ghép).
        for (int k = old.OrderStart; k < old.OrderEnd; k++) _animating.Remove(_prevViewOrder[k]);
        _viewStack.Clear(); _keyCounters.Clear(); _innerClip.Clear();
        if (old.ParentView > 0) _viewStack.Add(old.ParentView);
        _innerClip.Add(false);
        _keyScope = _scrollScope = 0;
        _groupHover = old.GroupHover;
        _blockHover = old.BlockHover;
        _shape = old.OuterShape != 0 ? _scene.AddClipShape(live.Shape(old.OuterShape)) : 0; // bản sao cục bộ: chỉ số 1 trong scene tạm
        _tree[id].Replay = false;
        PaintViewContent(id, snap.Refined, old.Clip, old.Xf);
        _paintTicks += System.Diagnostics.Stopwatch.GetTimestamp() - tPaint;

        var now2 = _cache[vid];
        int outerShapes = old.OuterShape != 0 ? 1 : 0;
        if (now2.InnerClip != old.InnerClip) return Fail("inner-clip");
        if (_scene.ShapeCount - outerShapes != old.ShapeEnd - old.ShapeStart) return Fail("shapes");
        return _scene.GradientCount == old.GradEnd - old.GradStart || Fail("gradients");
    }

    /// <summary>Gỡ kết quả đo / đặt chỗ cũ khỏi cây con đã Render ở lần thử một phần: chỉ giữ lại Render() (bộ nhớ đo, bounds, cờ phát lại tính lại).</summary>
    void CleanPrerendered(int id)
    {
        ref var n = ref _tree[id];
        n.Memo = default; n.HasMinContent = false; n.Replay = false; n.Bounds = default;
        for (int c = n.FirstChild; c >= 0; c = _tree[c].NextSibling) CleanPrerendered(c);
    }
}
