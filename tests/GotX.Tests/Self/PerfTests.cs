using GotX;
using GotX.Test;

/// <summary>Phép đo hiệu năng và các kiểm tra cache view: phải bắt được view phá cache, và im lặng với view ngoan.</summary>
static class PerfTests
{
    /// <summary>Lỗi cố ý: mỗi frame cấp phát một chuỗi dài mới và tự Notify.</summary>
    sealed class LeakyView : View
    {
        int _tick;
        public override Div Render(Window w)
        {
            Notify();
            return Fx.Page(w).Child(w.Div().Class("text-xs").Child(new string('x', 2000) + _tick++));
        }
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("perf/measure-reports-frame-stats", t =>
        {
            var host = t.Open(new CounterHost());
            var report = t.Perf.Measure(frames: 30);
            t.Assert.Equal(report.Frames, 30, "số frame đo");
            t.Assert.True(report.RenderedViews == 0 && report.ReplayedViews == 0, $"giao diện tĩnh: không view nào bẩn nên frame không render cũng không phát lại gì ({report.RenderedViews}/{report.ReplayedViews})");
            t.Assert.True(report.Primitives > 20 && report.Nodes >= 1, $"số primitive/node của frame cuối ({report.Primitives}, {report.Nodes})");
            t.Assert.Equal((long)report.AllocatedBytes.Median, 0L, "frame phát lại không cấp phát");
            t.Assert.True(report.Total.Min <= report.Total.Median && report.Total.Median <= report.Total.P95 && report.Total.P95 > 0, "min ≤ trung vị ≤ p95");
            t.Assert.True(report.Raster.Median > 0 && report.ToString().Contains("30 frame: draw"), "raster CPU đo riêng; có bản in một dòng");
            // Callback mỗi frame: ép một view render lại ở từng frame.
            report = t.Perf.Measure(frames: 10, raster: false, each: _ => host.Counter.Notify());
            t.Assert.Equal(report.RenderedViews, 10, "mỗi frame chỉ render bộ đếm (10 × 1): view cha và khối tĩnh không bị chạm");
            t.Assert.True(t.Window.Trace is not null && report.Raster.Median == 0, "đo xong thì Trace được gắn lại; raster tắt theo yêu cầu");
        });

        suite.Add("perf/idle-must-replay", t =>
        {
            t.Open(new CounterHost());
            t.Click("c-inc");
            t.Perf.ExpectIdle();
            var inner = Kit.Probe(p => { p.Open(new RestlessView()); p.Perf.ExpectIdle(); });
            var f = Kit.ExpectOneFailure(t, inner, "view tự Notify trong Render", "Settles", "vẫn chưa đứng yên", "FramePending=True");
            t.Assert.True(f is not null && f.Details.Exists(d => d.StartsWith("view đã render: RestlessView ×")) && f.Details.Exists(d => d.StartsWith("view bị đánh dấu bẩn: RestlessView ×")),
                "lỗi nêu view nào render và view nào bị đánh dấu bẩn");
        });

        suite.Add("perf/idle-detects-timer-driven-rerender", t =>
        {
            // Đứng yên theo nghĩa "không còn yêu cầu vẽ", nhưng một hẹn giờ 10 ms Notify liên tục → frame rảnh vẫn render.
            var inner = Kit.Probe(p =>
            {
                var host = p.Open(new CounterHost());
                p.App.SetInterval(0.01, () => host.Panel.Notify());
                p.Perf.ExpectIdle();
            });
            var f = Kit.ExpectOneFailure(t, inner, "hẹn giờ phá cache", "Idle:", "frame rảnh vẫn render");
            t.Assert.True(f is not null && f.Details.Exists(d => d.Contains("StaticPanel") && d.StartsWith("view đã render")), "chỉ ra StaticPanel");
        });

        suite.Add("perf/settles", t =>
        {
            t.Open(new Fx(w => Fx.Page(w).Child(w.Div().Key("fade").Class("size-8 bg-sky-500 transition duration-300 hover:bg-rose-500"))));
            t.Hover("fade");
            t.Assert.True(t.Window.WantsAnimationFrame, "transition đang chạy ngay sau khi hover");
            t.Perf.ExpectSettles();
            t.Assert.True(!t.Window.WantsAnimationFrame, "transition 300 ms kết thúc → đứng yên");
            var inner = Kit.Probe(p => { p.Open(new Fx(w => Fx.Page(w).Child(w.Div().Class("size-8 bg-sky-500 animate-spin")))); p.Perf.ExpectSettles(maxFrames: 90); });
            Kit.ExpectOneFailure(t, inner, "animation vô hạn", "sau 90 frame vẫn chưa đứng yên", "WantsAnimationFrame=True");
            t.Assert.True(!Kit.Probe(p => p.Open(new RestlessView())).Settle(50), "Settle trả false với view không bao giờ yên");
        });

        suite.Add("perf/render-count-for-interaction", t =>
        {
            var host = t.Open(new CounterHost());
            int mark = t.Trace.Mark();
            t.Perf.ExpectRenders(() => t.Click("c-inc"), max: 1, min: 1);
            t.Assert.Equal(string.Join(",", t.Trace.Names(TraceKind.ViewRender, mark)), "CounterView", "chỉ bộ đếm render lại khi bấm; view cha và khối tĩnh giữ cache");
            t.Assert.Equal(host.Counter.Count, 1, "thao tác thật sự đã chạy");
            t.Perf.ExpectRenders(() => t.Hover(new Point(500, 500)), max: 1); // rời nút: chỉ view chứa nút
            t.Assert.Equal(t.Perf.CountRenders(() => t.Frames(3)), 0, "không thao tác gì thì không render");
            var inner = Kit.Probe(p => { p.Open(new CounterHost()); p.Perf.ExpectRenders(() => p.Click("c-inc"), max: 0); });
            var f = Kit.ExpectOneFailure(t, inner, "vượt số lượt render", "thao tác làm render 1 lượt view", "≤ 0");
            t.Assert.True(f is not null && f.Details.Contains("view đã render: CounterView") && f.Details.Contains("view bị đánh dấu bẩn: CounterView"),
                $"liệt kê view render và nguyên nhân: {(f is null ? "" : string.Join(" | ", f.Details))}");
        });

        suite.Add("perf/budgets", t =>
        {
            t.Open(new CounterHost());
            t.Perf.ExpectFrameBudget(50, frames: 20);
            t.Perf.ExpectAllocations(0, frames: 20);
            var inner = Kit.Probe(p => { p.Open(new LeakyView()); p.Perf.ExpectAllocations(1024, frames: 20); p.Perf.ExpectFrameBudget(0.000001, frames: 5); });
            t.Assert.Equal(inner.Failures.Count, 2, "vượt ngân sách cấp phát + vượt ngân sách thời gian");
            if (inner.Failures.Count != 2) return;
            t.Assert.True(inner.Failures[0].Message.StartsWith("Allocations: trung vị") && inner.Failures[0].Details[0].Contains("render 20 /"), $"lỗi cấp phát kèm số liệu: {inner.Failures[0].Message} | {inner.Failures[0].Details[0]}");
            t.Assert.True(inner.Failures[1].Message.StartsWith("FrameBudget: trung vị Draw"), inner.Failures[1].Message);
        });

        suite.Add("perf/replay-matches-fresh", t =>
        {
            t.Open(new CleanView(), new Size(640, 480));
            t.Perf.ExpectReplayMatchesFresh();
            t.Scroll("list", 100);
            t.Click("save");
            t.Hover("cancel");
            t.Perf.ExpectReplayMatchesFresh();
        });
    }
}
