using System.Diagnostics;
using GotX.Text;

namespace GotX;

/// <summary>Nguồn gây bẩn view (ghi vào nhật ký hiệu năng).</summary>
enum DirtyCause : byte { Notify, Hover, Focus, Scroll, Animation }

/// <summary>Vì sao toàn bộ cache view bị xoá / frame phải dựng lại.</summary>
[Flags]
enum ClearFlags : byte { None = 0, Scale = 1, CacheOff = 2, Breakpoint = 4, Atlas = 8, Dark = 16, Invalidate = 32, Resize = 64 }

/// <summary>
/// Ghi nhật ký hiệu năng (<see cref="PerfLog"/>) cho Window: mỗi frame một dòng JSON. Mọi thứ ở đây chỉ chạy khi
/// <see cref="Log"/> khác null; khi tắt, các điểm móc trong Draw/RenderView/MarkViewDirty chỉ là một phép so sánh null.
/// </summary>
public sealed partial class Window
{
    static int s_nextWindowId;
    static readonly string[] s_inputNames = Enum.GetNames<InputKind>();
    static readonly string[] s_dirtyNames = Enum.GetNames<DirtyCause>();
    const int MaxRenderNames = 48, MaxDirtyNames = 12;

    readonly int _windowId = ++s_nextWindowId;
    PerfLog? _log;
    // Trạng thái của frame đang ghi
    readonly string[] _rvName = new string[MaxRenderNames];
    readonly long[] _rvTicks = new long[MaxRenderNames];
    int _rvCount, _rvTotal;
    long _rvSumTicks;
    readonly string[] _dirtyName = new string[MaxDirtyNames];
    int _dirtyNameCount, _dirtyViews, _causeMask;
    ClearFlags _clearFlags;
    readonly int[] _inputCounts = new int[s_inputNames.Length];
    long _firstInputTicks, _prevDrawTicks;
    Size _lastSize;
    PerfCounters.Snapshot _counters0;
    int _gc0, _gc1, _gc2;
    bool _lastFrameLogged;
    readonly int[] _rankScratch = new int[MaxRenderNames];

    /// <summary>
    /// Nhật ký hiệu năng của cửa sổ này. Mặc định lấy <see cref="PerfLog.Shared"/> (bật bằng biến môi trường <c>GOTX_LOG</c>);
    /// gán null để tắt, gán một <see cref="PerfLog"/> để ghi riêng.
    /// </summary>
    public PerfLog? Log
    {
        get => _log;
        set => _log = value;
    }

    void NoteRender(View v, long ticks)
    {
        _rvTotal++;
        _rvSumTicks += ticks;
        if (_rvCount < MaxRenderNames) { _rvName[_rvCount] = v.GetType().Name; _rvTicks[_rvCount++] = ticks; }
    }

    void NoteDirty(int id, DirtyCause cause, bool first)
    {
        _causeMask |= 1 << (int)cause; // mọi nguyên nhân đều được ghi, kể cả khi view đã bẩn vì nguyên nhân khác
        if (!first) return;
        _dirtyViews++;
        if (_dirtyNameCount >= MaxDirtyNames) return;
        string name = App.Find(id)?.GetType().Name ?? "?";
        for (int i = 0; i < _dirtyNameCount; i++) if ((object)_dirtyName[i] == name) return;
        _dirtyName[_dirtyNameCount++] = name;
    }

    void NoteInput(in InputEvent e)
    {
        if (_firstInputTicks == 0) _firstInputTicks = Stopwatch.GetTimestamp();
        _inputCounts[(int)e.Kind]++;
    }

    void NoteClear(ClearFlags flags)
    {
        if (_log is not null) _clearFlags |= flags;
    }

    /// <summary>Đầu Draw: chụp bộ đếm/GC để cuối frame ra hiệu.</summary>
    void BeginFrameLog()
    {
        _rvCount = _rvTotal = 0; _rvSumTicks = 0;
        _counters0 = PerfCounters.Snapshot.Now();
        _gc0 = GC.CollectionCount(0); _gc1 = GC.CollectionCount(1); _gc2 = GC.CollectionCount(2);
    }

    static void WriteFlags(PerfLog log, string key, int mask, string[] names)
    {
        log.BeginArray(key);
        for (int i = 0; i < names.Length; i++) if ((mask & (1 << i)) != 0) log.ItemStr(names[i]);
        log.EndArray();
    }

    static readonly string[] s_clearNames = ["scale", "cache-off", "breakpoint", "atlas", "dark", "invalidate", "resize"];

    void WriteFrameLog(PerfLog log, in FrameStats f, long startTicks, long endTicks, Size size)
    {
        _lastFrameLogged = false;
        double ms = 1000.0 / Stopwatch.Frequency;
        double lat = _firstInputTicks == 0 ? 0 : (endTicks - _firstInputTicks) * ms;
        bool slow = f.TotalMs > log.SlowFrameMs || lat > log.SlowLatencyMs;
        double dt = _prevDrawTicks == 0 ? 0 : (startTicks - _prevDrawTicks) * ms;
        _prevDrawTicks = startTicks;
        if (_clearFlags != ClearFlags.None)
        {
            var parts = new List<string>(4);
            for (int i = 0; i < s_clearNames.Length; i++) if (((int)_clearFlags & (1 << i)) != 0) parts.Add(s_clearNames[i]);
            // Resize chỉ đổi bounds (view không phát lại được) chứ không xoá cache: không cảnh báo.
            if ((_clearFlags & ~ClearFlags.Resize) != ClearFlags.None) log.Warn("cache-cleared", string.Join(',', parts), _frame);
        }
        if (log.Level == PerfLogLevel.Off || (log.Level == PerfLogLevel.Slow && !slow)) { ResetFrameLog(); return; }

        var c = PerfCounters.Snapshot.Now() - _counters0;
        bool trace = log.Level == PerfLogLevel.Trace;
        log.Begin("frame");
        log.Int("n", _frame); log.Int("win", _windowId);
        log.Dbl("dt", dt); log.Dbl("draw", f.TotalMs); log.Dbl("layout", f.LayoutMs); log.Dbl("paint", f.PaintMs);
        log.Dbl("rv", _rvSumTicks * ms);
        log.Dbl("wrap", PerfCounters.TicksToMs(c.WrapTicks)); log.Int("wrapHit", c.WrapHit); log.Int("wrapMiss", c.WrapMiss);
        log.Dbl("glyph", PerfCounters.TicksToMs(c.GlyphRasterTicks)); log.Int("glyphN", c.GlyphRaster);
        if (c.ClassMiss > 0) { log.Int("clsMiss", c.ClassMiss); log.Dbl("cls", PerfCounters.TicksToMs(c.ClassMissTicks)); }
        log.Int("meas", c.Measure); log.Int("memo", c.MeasureMemoHit);
        log.Int("rend", f.RenderedViews); log.Int("repl", f.ReplayedViews);
        if (_rvCount > 0)
        {
            // Hạng theo thời gian render; Frames ghi top 5, Trace ghi hết (tối đa MaxRenderNames).
            int take = trace ? _rvCount : Math.Min(5, _rvCount);
            var rank = _rankScratch;
            for (int i = 0; i < _rvCount; i++) rank[i] = i;
            for (int i = 0; i < take; i++)
            {
                int best = i;
                for (int j = i + 1; j < _rvCount; j++) if (_rvTicks[rank[j]] > _rvTicks[rank[best]]) best = j;
                (rank[i], rank[best]) = (rank[best], rank[i]);
            }
            log.BeginArray("views");
            for (int i = 0; i < take; i++) { log.ItemStr(_rvName[rank[i]]); log.ItemInt((long)(_rvTicks[rank[i]] * ms * 1000)); } // tên, µs
            log.EndArray();
        }
        if (_causeMask != 0 || _dirtyNameCount > 0)
        {
            WriteFlags(log, "cause", _causeMask, s_dirtyNames);
            if (_dirtyNameCount > 0)
            {
                log.BeginArray("dirty");
                for (int i = 0; i < _dirtyNameCount; i++) log.ItemStr(_dirtyName[i]);
                log.EndArray();
            }
        }
        if (_clearFlags != ClearFlags.None) WriteFlags(log, "clr", (int)_clearFlags, s_clearNames);
        log.Int("nodes", f.Nodes); log.Int("prim", f.Primitives); log.Int("clips", f.ClipShapes); log.Int("grad", _scene.GradientCount);
        log.Int("hit", f.Hitboxes); log.Int("hov", f.HoverRegions);
        log.Int("alloc", f.AllocatedBytes);
        log.BeginArray("gc"); log.ItemInt(GC.CollectionCount(0) - _gc0); log.ItemInt(GC.CollectionCount(1) - _gc1); log.ItemInt(GC.CollectionCount(2) - _gc2); log.EndArray();
        if (_frame % 60 == 1)
        {
            log.Dbl("wsMB", Environment.WorkingSet / 1048576.0);
            log.Dbl("heapMB", GC.GetTotalMemory(false) / 1048576.0);
            log.Dbl("atlasKB", Text.Atlas.UsedBytes / 1024.0);
        }
        bool anyInput = false;
        for (int i = 0; i < _inputCounts.Length; i++) if (_inputCounts[i] > 0) { anyInput = true; break; }
        if (anyInput)
        {
            log.BeginArray("in");
            for (int i = 0; i < _inputCounts.Length; i++) if (_inputCounts[i] > 0) { log.ItemStr(s_inputNames[i]); log.ItemInt(_inputCounts[i]); }
            log.EndArray();
            log.Dbl("lat", lat);
        }
        if (f.Animating) log.Int("anim", 1);
        log.Int("w", (long)size.Width); log.Int("h", (long)size.Height); log.Dbl("sc", _scale);
        if (slow) log.Int("slow", 1);
        log.End();
        _lastFrameLogged = true;
        ResetFrameLog();
    }

    void ResetFrameLog()
    {
        _dirtyNameCount = _dirtyViews = _causeMask = 0;
        _clearFlags = ClearFlags.None;
        Array.Clear(_inputCounts);
        _firstInputTicks = 0;
    }

    /// <summary>
    /// Platform gọi sau khi renderer vẽ xong frame vừa rồi: ghi thời gian raster CPU / nộp GPU / Present của frame đó
    /// (<c>{"t":"present","n":…}</c>, người đọc ghép theo <c>n</c>).
    /// </summary>
    public void LogPresent(double totalMs, double buildMs = 0, double submitMs = 0, double presentMs = 0, double gpuMs = 0, long uploadBytes = 0, double instMs = 0, double mapMs = 0)
    {
        if (_log is not { } log || !_lastFrameLogged) return;
        log.Begin("present");
        log.Int("n", _frame); log.Int("win", _windowId);
        log.Dbl("ms", totalMs);
        if (buildMs > 0) log.Dbl("build", buildMs);
        if (submitMs > 0) log.Dbl("submit", submitMs);
        if (presentMs > 0) log.Dbl("flip", presentMs);
        if (gpuMs > 0) log.Dbl("gpu", gpuMs);
        if (instMs > 0) log.Dbl("inst", instMs);
        if (mapMs > 0) log.Dbl("map", mapMs);
        if (uploadBytes > 0) log.Int("upBytes", uploadBytes);
        var g = GpuCounters.Last; // renderer CPU để 0 → không ghi
        if (g.DrawCalls > 0)
        {
            log.Int("draws", g.DrawCalls); log.Int("pipe", g.PipelineSwitches); log.Int("texsw", g.TextureSwitches);
            log.Int("instB", g.InstanceBytes); log.Dbl("atlasHit", g.AtlasHitRate); log.Dbl("enc", g.EncodeMs);
        }
        log.End();
    }
}
