using GotX.Raster;

namespace GotX.Test;

/// <summary>Kiểm tra cache view: frame rảnh, số view render lại sau một thao tác, và tính đúng của việc phát lại.</summary>
public sealed partial class UiPerf
{
    /// <summary>Số sự kiện ViewRender đếm được ở lần <see cref="ExpectRenders"/> / <see cref="CountRenders"/> gần nhất.</summary>
    public int LastRenderCount { get; private set; }

    /// <summary>
    /// Sau khi đứng yên, vẽ lại (một frame tại chỗ + <paramref name="frames"/> frame cách nhau 16 ms) không được gọi
    /// Render() của view nào: mọi thứ phải phát lại từ cache. Hỏng nghĩa là có thứ đang phá cache mỗi frame –
    /// view tự Notify trong Render, hẹn giờ Notify liên tục, bounds/kiểu chữ truyền xuống đổi từng frame…
    /// Lỗi liệt kê kiểu view đã render và view bị đánh dấu bẩn (từ Trace).
    /// </summary>
    public bool ExpectIdle(int frames = 3)
    {
        if (!ExpectSettles()) return false;
        int mark = _t.Trace.Mark();
        _t.Draw();
        int rendered = _t.Window.LastFrame.RenderedViews;
        for (int i = 0; i < frames; i++)
        {
            _t.Step();
            rendered += _t.Window.LastFrame.RenderedViews;
        }
        if (rendered == 0 && !_t.Window.FramePending) return true;
        var f = _t.Fail($"Idle: {frames + 1} frame rảnh vẫn render {rendered} lượt view (mong đợi 0, phát lại toàn bộ); FramePending={_t.Window.FramePending}");
        AddRenderDetails(f, mark);
        return false;
    }

    /// <summary>
    /// Chạy <paramref name="action"/> (thường là một thao tác input), vẽ cho tới khi hết yêu cầu vẽ lại, và trả về số
    /// lượt Render() của view đã xảy ra. Frame soi cây của harness không được tính.
    /// </summary>
    public int CountRenders(Action action)
    {
        _t.App.Flush();
        if (_t.Window.FramePending) _t.Step(); // việc còn dở từ trước không được tính cho thao tác này
        int mark = _t.Trace.Mark();
        action();
        _t.App.Flush();
        for (int guard = 0; _t.Window.FramePending && guard < 8; guard++) _t.Step();
        _lastMark = mark;
        return LastRenderCount = _t.Trace.Count(TraceKind.ViewRender, mark);
    }

    int _lastMark;

    /// <summary>
    /// Một thao tác chỉ được làm render lại tối đa <paramref name="max"/> lượt view (và ít nhất <paramref name="min"/>).
    /// Ví dụ <c>ExpectRenders(() =&gt; t.Click("inc"), max: 2)</c>: bấm nút đếm chỉ render view đếm và view cha của nó.
    /// </summary>
    public bool ExpectRenders(Action action, int max, int min = 0)
    {
        int count = CountRenders(action);
        if (count <= max && count >= min) return true;
        var f = _t.Fail($"Renders: thao tác làm render {count} lượt view, mong đợi {(min > 0 ? $"{min}.." : "≤ ")}{max}");
        AddRenderDetails(f, _lastMark);
        return false;
    }

    /// <summary>
    /// Ảnh dựng từ frame CÓ cache phải giống hệt ảnh dựng lại từ đầu (cache tắt) ở cùng thời điểm. Khác nhau nghĩa là
    /// việc phát lại đang vẽ nội dung cũ hoặc đặt sai chỗ – lỗi cache mà nhìn code không thấy. Lỗi chỉ ra phần tử chứa vùng khác.
    /// </summary>
    public bool ExpectReplayMatchesFresh()
    {
        var cached = new Canvas();
        cached.Resize(_t.PixelWidth, _t.PixelHeight);
        SoftwareRenderer.Render(_t.Draw(), cached);
        var fresh = _t.Render();
        var diff = ImageDiff.Compare(cached, Pixels.ToRgba(fresh), 0);
        if (diff.DiffPixels == 0) return true;
        var f = _t.Fail($"ReplayMatchesFresh: frame phát lại từ cache khác frame dựng mới {diff.DiffPixels} pixel trong {diff.Regions.Count} vùng");
        int shown = 0;
        foreach (var region in diff.Regions)
        {
            if (++shown > 6) break;
            var logical = region.Area.Scale(1 / _t.Scale);
            var owner = _t.Tree.DeepestContaining(logical);
            f.Details.Add($"vùng {shown}: {Fmt.B(logical)} {region.Pixels} px → {(owner is null ? "?" : _t.Tree.Describe(owner))}");
        }
        if (_t.Options.OutDir is not null)
        {
            string path = _t.ArtifactPath("replay.diff.png");
            PngWriter.Write(path, diff.Image);
            f.Artifacts.Add(path);
        }
        return false;
    }
}
