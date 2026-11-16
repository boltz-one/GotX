using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using GotX.Text;
using GotX.Widgets;

namespace GotX;

/// <summary>
/// Một cửa sổ: dựng cây element → layout → paint ra Scene. Không phụ thuộc OS.
///
/// Cache view: mỗi view nhớ kích thước đo được, bounds,
/// và đoạn primitive/hitbox của frame trước. Nếu view không bị Notify và nhận cùng bounds,
/// kết quả được chép lại thay vì gọi Render(); nếu chỉ bị dời chỗ (cuộn, translate) thì
/// được chép kèm phép tịnh tiến. Notify một view đánh dấu bẩn view đó và mọi view cha.
/// </summary>
public sealed partial class Window
{
    readonly ElementTree _tree = new();
    readonly LayoutEngine _layout;
    readonly List<TextLine> _lines = new();
    readonly HashSet<int> _dirty = new();
    readonly HashSet<int> _animating = new();
    readonly List<int> _viewStack = new(), _keyCounters = new();
    readonly List<bool> _innerClip = new();
    readonly Dictionary<long, ScrollState> _scrolls = new();
    readonly Dictionary<long, TransitionState> _transitions = new();
    readonly HashSet<int> _paintedViews = new(); // view được vẽ thật (không phát lại) trong frame này
    int _rootKeyCounter;
    int _frame, _atlasGeneration;
    object? _pressedState; // phần tử nhận MouseDown: click chỉ tính khi MouseUp trúng đúng phần tử đó
    int _pressedView;
    int _shape; // vùng cắt theo hình đang có hiệu lực (1-based trong scene hiện tại, 0 = không)

    // Bộ đệm đôi: frame hiện tại / frame trước
    Scene _scene, _prevScene;
    List<Hitbox> _hitboxes = new(), _prevHitboxes = new();
    List<HoverRegion> _hovers = new(), _prevHovers = new();
    List<int> _viewOrder = new(), _prevViewOrder = new();
    Dictionary<int, ViewCache> _cache = new(), _prevCache = new();

    Point _mouse = new(float.NegativeInfinity, float.NegativeInfinity);
    bool _mouseDown, _dark;
    int _groupHover, _lastBucket = -1;
    int _pressed = -1;
    TextInput? _dragInput;
    float _scale = 1;
    double _now;
    bool _needsFrame;
    int _rendered, _replayed;
    Rgba _clearColor = Rgba.White;

    public Window(App app, TextSystem text)
    {
        App = app;
        _log = PerfLog.Shared;
        Text = text;
        _layout = new LayoutEngine(_tree, text, this);
        _scene = new Scene(text.Atlas);
        _prevScene = new Scene(text.Atlas);
        _scene.Damage = _prevScene.Damage = Damage;
        app.AddWindow(this);
    }

    public App App { get; }
    public TextSystem Text { get; }
    public View? Root { get; set; }
    public float RemSize { get; set; } = 16;
    public TextStyle BaseTextStyle { get; set; } = TextStyle.Default;
    public Rgba Background { get => _clearColor; set => _clearColor = value; }
    public bool CacheEnabled { get; set; } = true;

    /// <summary>Đồng hồ (giây) cho transition/animation. Test có thể thay bằng đồng hồ giả.</summary>
    public Func<double> Clock { get; set; } = static () => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

    /// <summary>Còn transition/animation/cuộn mượt đang chạy: platform nên hẹn frame tiếp (~16ms).</summary>
    public bool WantsAnimationFrame { get; private set; }

    /// <summary>Bật variant dark:. Đổi giá trị sẽ dựng lại toàn bộ.</summary>
    public bool DarkMode
    {
        get => _dark;
        set { if (_dark == value) return; _dark = value; _cache.Clear(); NoteClear(ClearFlags.Dark); RequestFrame(); }
    }

    /// <summary>
    /// Bỏ toàn bộ kết quả cache của các view: frame kế mọi view đều Render lại. Dùng khi một thiết lập toàn cục mà các
    /// view đọc lúc Render (style component, ngôn ngữ, bảng màu) vừa đổi – thứ mà Notify của từng view không bắt được.
    /// </summary>
    public void InvalidateAll()
    {
        _cache.Clear();
        NoteClear(ClearFlags.Invalidate);
        RequestFrame();
    }

    /// <summary>Con trỏ chuột cần hiển thị tại vị trí hiện tại (cursor-pointer, cursor-text…).</summary>
    public CursorKind CursorAtMouse
    {
        get
        {
            var span = CollectionsMarshal.AsSpan(_hovers);
            for (int i = span.Length - 1; i >= 0; i--)
                if (span[i].Inside && span[i].Cursor != CursorKind.Default) return span[i].Cursor;
            return CursorKind.Default;
        }
    }

    /// <summary>Phát ra khi cần vẽ lại (một view bị Notify, hover đổi…). Platform nối vào RequestFrame.</summary>
    public event Action? FrameRequested;

    /// <summary>Scene của frame vừa vẽ (cùng đối tượng Draw trả về; khung một phần sửa nó tại chỗ).</summary>
    public Scene LastScene => _scene;

    public long LastFrameAllocatedBytes { get; private set; }
    public int LastFrameRenderedViews { get; private set; }
    public int LastFrameReplayedViews { get; private set; }

    public TextInput? Focused { get; private set; }

    /// <summary>Vị trí con trỏ của ô đang focus (logical px) – để platform đặt cửa sổ IME.</summary>
    public Bounds? ImeCaretRect => Focused?.CaretRect;

    // ================= Frame =================

    public Scene Draw(Size logicalSize, float scale)
    {
        long before = GC.GetAllocatedBytesForCurrentThread();
        long t0 = Stopwatch.GetTimestamp();
        _framePending = false;
        _drawing = true;
        _layoutTicks = _paintTicks = 0;
        _keyScope = _scrollScope = 0;
        _now = Clock();
        var log = _log;
        if (log is not null) BeginFrameLog();
        var variants = new VariantContext(_dark, logicalSize.Width, logicalSize.Height, RemSize);
        int bucket = variants.BreakpointBucket;
        // Atlas glyph vượt ngân sách → xoá và raster lại; chỉ số glyph cũ hết hiệu lực nên không phát lại được.
        var atlas = Text.Atlas;
        if (atlas.OverBudget)
        {
            log?.Warn("atlas-reset", $"{atlas.UsedBytes >> 10} KB > {atlas.MaxBytes >> 10} KB", _frame + 1);
            atlas.Reset();
        }
        Text.FrameTick++;
        LastFramePartial = _fellBack = false;
        if (CanPartial(logicalSize, scale, bucket, atlas))
        {
            _tree.Frame = ++_frame;
            _paintedViews.Clear();
            _scene.ClearColor = _clearColor;
            _rendered = _replayed = 0;
            if (TryPartial()) { LastFramePartial = true; PartialFrameCount++; }
            else { PartialFallbackCount++; _fellBack = true; }
        }
        else _fellBack = false;
        if (!LastFramePartial) DrawFull(logicalSize, scale, variants, bucket, atlas, log);
        FinishFrame(logicalSize, t0, before, log);
        return _scene;
    }

    void DrawFull(Size logicalSize, float scale, in VariantContext variants, int bucket, GlyphAtlas atlas, PerfLog? log)
    {
        if (scale != _scale || !CacheEnabled || bucket != _lastBucket || atlas.Generation != _atlasGeneration)
        {
            _cache.Clear(); // qua breakpoint → dựng lại
            if (log is not null && _frame > 0)
                NoteClear((scale != _scale ? ClearFlags.Scale : 0) | (!CacheEnabled ? ClearFlags.CacheOff : 0)
                    | (bucket != _lastBucket ? ClearFlags.Breakpoint : 0) | (atlas.Generation != _atlasGeneration ? ClearFlags.Atlas : 0));
        }
        if (log is not null && _frame > 0 && logicalSize != _lastSize) NoteClear(ClearFlags.Resize);
        _lastSize = logicalSize;
        _atlasGeneration = atlas.Generation;
        _tree.Frame = ++_frame;
        _paintedViews.Clear();
        _scale = scale;
        _lastBucket = bucket;
        _tree.Variants = variants;
        _groupHover = 0;
        _rootKeyCounter = 0;
        _shape = 0;

        (_scene, _prevScene) = (_prevScene, _scene);
        (_hitboxes, _prevHitboxes) = (_prevHitboxes, _hitboxes);
        (_hovers, _prevHovers) = (_prevHovers, _hovers);
        (_viewOrder, _prevViewOrder) = (_prevViewOrder, _viewOrder);
        (_cache, _prevCache) = (_prevCache, _cache);
        _scene.Clear();
        _scene.ClearColor = _clearColor;
        _hitboxes.Clear(); _hovers.Clear(); _viewOrder.Clear(); _cache.Clear();
        _viewStack.Clear(); _keyCounters.Clear(); _innerClip.Clear();
        if (_preChild < 0) _tree.Reset(); // sau lần thử một phần hỏng, giữ cây để tái dùng Render của view đó
        _rendered = _replayed = 0;
        _animating.Clear();

        _blockHover = HasModalFrom(0);
        if (Root is { } root)
        {
            int rootId = _tree.NewView(root).SizeFull().Id;
            long t1 = Stopwatch.GetTimestamp();
            _layout.Run(rootId, logicalSize, BaseTextStyle, RemSize);
            long t2 = Stopwatch.GetTimestamp();
            Paint(rootId, BaseTextStyle, Bounds.Infinite, Xf.Identity);
            _layoutTicks += t2 - t1;
            _paintTicks += Stopwatch.GetTimestamp() - t2;
        }
        PaintOverlays(logicalSize);
        _preChild = -1;
        Damage.MarkFull();
        _lastRoot = Root; _lastRem = RemSize; _lastBase = BaseTextStyle; _lastOverlayStamp = _overlayStamp;
        if (_snaps.Count > _cache.Count * 2 + 256) PruneSnaps();
    }

    void FinishFrame(Size logicalSize, long t0, long before, PerfLog? log)
    {
        _drawing = false;
        PruneState();
        ResolveFocus();
        ResolvePendingScroll();
        _dirty.Clear();
        _self.Clear();
        // View còn đang chuyển động sẽ được vẽ lại ở frame kế (platform hẹn ~16ms, không vẽ dồn dập)
        foreach (int v in _animating) MarkViewDirty(v, request: false, DirtyCause.Animation);
        WantsAnimationFrame = _animating.Count > 0;
        UpdateHover(); // nội dung có thể đã dời dưới con trỏ (cuộn, phát lại)
        // Handler chạy trong lúc vẽ (OnHover, OnScroll) hoặc chính Render có thể đã Notify: xử lý ngay để frame kế được hẹn,
        // không phải chờ tới sự kiện input tiếp theo. Hàng đợi rỗng thì Flush trả về ngay.
        _hoverCallbackFired = _callbackFired = false;
        App.Flush();

        LastFrameRenderedViews = _rendered;
        LastFrameReplayedViews = _replayed;
        LastFrameAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        RecordFrame(t0);
        if (log is not null)
        {
            WriteFrameLog(log, LastFrame, t0, Stopwatch.GetTimestamp(), logicalSize);
            if (_frame == 1) log.Startup("first-frame", LastFrame.TotalMs);
        }
    }

    void PruneSnaps()
    {
        List<int>? gone = null;
        foreach (int id in _snaps.Keys) if (!_cache.ContainsKey(id)) (gone ??= new()).Add(id);
        if (gone is not null) foreach (int id in gone) _snaps.Remove(id);
    }
}
