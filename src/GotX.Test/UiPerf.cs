using System.Diagnostics;
using GotX.Raster;

namespace GotX.Test;

/// <summary>Thống kê một đại lượng qua nhiều frame.</summary>
public readonly record struct Stat(double Min, double Median, double P95, double Max)
{
    public static Stat Of(List<double> values)
    {
        if (values.Count == 0) return default;
        var v = new List<double>(values);
        v.Sort();
        return new Stat(v[0], v[v.Count / 2], v[Math.Min(v.Count - 1, (int)Math.Ceiling(v.Count * 0.95) - 1)], v[^1]);
    }

    public override string ToString() => $"{Fmt.N(Min)}/{Fmt.N(Median)}/{Fmt.N(P95)}";
}

/// <summary>Kết quả đo <see cref="UiPerf.Measure"/>. Thời gian tính bằng mili giây, định dạng min/trung vị/p95.</summary>
public sealed class PerfReport
{
    public int Frames { get; init; }
    public Stat Layout { get; init; }
    public Stat Paint { get; init; }
    /// <summary>Toàn bộ Window.Draw (dựng cây + layout + paint + dọn state).</summary>
    public Stat Total { get; init; }
    /// <summary>Raster CPU của Scene ra canvas (đo riêng; app thật dùng GPU thì không tốn phần này).</summary>
    public Stat Raster { get; init; }
    public Stat AllocatedBytes { get; init; }
    public int RenderedViews { get; init; }
    public int ReplayedViews { get; init; }
    public int Nodes { get; init; }
    public int Primitives { get; init; }

    public override string ToString() =>
        $"{Frames} frame: draw {Total} ms (layout {Layout}, paint {Paint}), raster {Raster} ms, " +
        $"cấp phát {Fmt.Bytes((long)AllocatedBytes.Median)}/frame (max {Fmt.Bytes((long)AllocatedBytes.Max)}), " +
        $"render {RenderedViews} / phát lại {ReplayedViews} view, {Primitives} primitive";
}

/// <summary>
/// Đo và kiểm tra hiệu năng dựng giao diện: thời gian frame, cấp phát, và quan trọng nhất là cache view có giữ được
/// không (frame rảnh phải phát lại toàn bộ; một thao tác chỉ được render lại đúng những view liên quan).
/// </summary>
public sealed partial class UiPerf(UiTest test)
{
    readonly UiTest _t = test;

    /// <summary>
    /// Chạy <paramref name="frames"/> frame (mỗi frame đẩy đồng hồ 16 ms, chạy hẹn giờ, gọi <paramref name="each"/> nếu có)
    /// sau <paramref name="warmup"/> frame làm nóng. Trong lúc đo, Trace được gỡ ra vì bản thân việc ghi nhật ký có cấp phát.
    /// </summary>
    public PerfReport Measure(int frames = 60, int warmup = 5, bool raster = true, Action<int>? each = null)
    {
        var w = _t.Window;
        var trace = w.Trace;
        w.Trace = null;
        var canvas = new Canvas();
        canvas.Resize(_t.PixelWidth, _t.PixelHeight);
        List<double> layout = new(), paint = new(), total = new(), rasterMs = new(), alloc = new();
        int rendered = 0, replayed = 0;
        FrameStats last = default;
        try
        {
            for (int i = -warmup; i < frames; i++)
            {
                each?.Invoke(Math.Max(i, 0));
                _t.App.Flush();
                _t.AdvanceClock(UiTest.FrameSeconds);
                _t.App.RunTimers();
                var scene = _t.Draw();
                last = w.LastFrame;
                long t0 = Stopwatch.GetTimestamp();
                if (raster) SoftwareRenderer.Render(scene, canvas);
                double ms = (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
                if (i < 0) continue;
                layout.Add(last.LayoutMs); paint.Add(last.PaintMs); total.Add(last.TotalMs); alloc.Add(last.AllocatedBytes);
                if (raster) rasterMs.Add(ms);
                rendered += last.RenderedViews; replayed += last.ReplayedViews;
            }
        }
        finally { w.Trace = trace; }
        return new PerfReport
        {
            Frames = frames, Layout = Stat.Of(layout), Paint = Stat.Of(paint), Total = Stat.Of(total), Raster = Stat.Of(rasterMs),
            AllocatedBytes = Stat.Of(alloc), RenderedViews = rendered, ReplayedViews = replayed, Nodes = last.Nodes, Primitives = last.Primitives,
        };
    }

    /// <summary>
    /// Trung vị thời gian Window.Draw không vượt <paramref name="milliseconds"/>. Dùng trung vị (không phải max) vì máy
    /// chạy test có nhiễu; đặt ngân sách rộng rãi – mục đích là bắt thoái hoá cỡ bậc độ lớn, không phải đo chính xác.
    /// </summary>
    public bool ExpectFrameBudget(double milliseconds, int frames = 60, Action<int>? each = null)
    {
        var report = Measure(frames, raster: false, each: each);
        if (report.Total.Median <= milliseconds) return true;
        _t.Fail($"FrameBudget: trung vị Draw {Fmt.N(report.Total.Median)} ms > ngân sách {Fmt.N(milliseconds)} ms").Details.Add(report.ToString());
        return false;
    }

    /// <summary>Ở trạng thái ổn định (không input), trung vị cấp phát mỗi frame không vượt <paramref name="maxBytesPerFrame"/>.</summary>
    public bool ExpectAllocations(long maxBytesPerFrame, int frames = 60, Action<int>? each = null)
    {
        var report = Measure(frames, raster: false, each: each);
        if (report.AllocatedBytes.Median <= maxBytesPerFrame) return true;
        _t.Fail($"Allocations: trung vị {Fmt.Bytes((long)report.AllocatedBytes.Median)}/frame > ngân sách {Fmt.Bytes(maxBytesPerFrame)}").Details.Add(report.ToString());
        return false;
    }

    /// <summary>Giao diện tự đứng yên trong <paramref name="maxFrames"/> frame (không có animation hay Notify lặp vô hạn).</summary>
    public bool ExpectSettles(int maxFrames = 300)
    {
        int mark = _t.Trace.Mark();
        if (_t.Settle(maxFrames)) return true;
        var f = _t.Fail($"Settles: sau {maxFrames} frame vẫn chưa đứng yên (FramePending={_t.Window.FramePending}, WantsAnimationFrame={_t.Window.WantsAnimationFrame})");
        AddRenderDetails(f, mark);
        return false;
    }

    /// <summary>Ghi vào lỗi: view nào đã render, view nào bị đánh dấu bẩn kể từ mốc.</summary>
    internal void AddRenderDetails(Failure f, int mark)
    {
        var rendered = _t.Trace.Names(TraceKind.ViewRender, mark);
        var dirtied = _t.Trace.Names(TraceKind.ViewDirty, mark);
        f.Details.Add("view đã render: " + (rendered.Count == 0 ? "(không)" : string.Join(", ", rendered)));
        f.Details.Add("view bị đánh dấu bẩn: " + (dirtied.Count == 0 ? "(không có ViewDirty – render do animation/hover/cache bị xoá)" : string.Join(", ", dirtied)));
    }
}
