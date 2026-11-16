using GotX;
using GotX.Test;

/// <summary>
/// Ngân sách hiệu năng trên một trang lớn (hàng nghìn node, chữ dài, thẻ bo góc có overflow-hidden và bóng): ghim lại các điều đã tối ưu
/// (frame rảnh phát lại không cấp phát, cuộn / hover chỉ render vài view, chi phí mỗi frame không phình theo bậc độ lớn).
/// Mốc thời gian đặt RỘNG – test chạy bản Debug trên máy dùng chung; mục tiêu là bắt thoái hoá cỡ bậc độ lớn, không đo chính xác.
/// </summary>
static class PerfBudgetTests
{
    sealed class Section(int index) : View
    {
        static readonly string[] Words = ["layout", "glyph", "atlas", "Tiếng", "Việt", "replay", "cache", "button", "dialog", "tailwind"];
        readonly string[] _rows = Enumerable.Range(0, 8).Select(r =>
            string.Join(' ', Enumerable.Range(0, 14).Select(j => Words[(index * 7 + r * 3 + j) % Words.Length]))).ToArray();

        public override Div Render(Window w)
        {
            var card = w.Div().Class("flex flex-col gap-1 p-4 rounded-xl border bg-white shadow-md overflow-hidden").Key("card-" + index);
            card.Child(w.Div().Class("font-semibold text-base").Child("Section " + index));
            foreach (string row in _rows) card.Child(w.Div().Class("text-sm text-slate-600 hover:bg-slate-50").Child(row));
            card.Child(w.Div().Class("flex gap-2")
                .Child(w.Div().Class("px-2 py-1 rounded-md bg-slate-900 text-white text-xs cursor-pointer hover:opacity-90").Child("Open"))
                .Child(w.Div().Class("px-2 py-1 rounded-md border text-xs cursor-pointer hover:bg-slate-100").Child("Cancel")));
            return card;
        }
    }

    sealed class BigPage : View
    {
        View[] _sections = [];
        protected override void OnCreated() => _sections = Enumerable.Range(0, 40).Select(i => (View)App.New(new Section(i))).ToArray();

        public override Div Render(Window w)
        {
            var list = w.Div().Class("flex flex-col gap-3 p-4").Key("big-list");
            foreach (var s in _sections) list.Child(w.View(s));
            return w.Div().Class("size-full bg-slate-100 text-slate-900 overflow-y-auto").Key("big-scroll").Child(list);
        }
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("perf/budget/big-page-idle-replays-everything-without-allocating", t =>
        {
            t.Open(new BigPage(), new Size(900, 700));
            t.Settle();
            t.Perf.ExpectIdle();
            var report = t.Perf.Measure(frames: 30, raster: false);
            // Primitive của view ngoài tầm nhìn bị loại khỏi scene (Window.Cull.cs) nên đo độ lớn bằng số node.
            t.Assert.True(report.Nodes > 800, $"trang đủ lớn để phép đo có nghĩa ({report.Nodes} node, {report.Primitives} primitive)");
            t.Assert.Equal((long)report.AllocatedBytes.Max, 0L, "frame rảnh trên trang lớn không cấp phát byte nào");
            t.Perf.ExpectFrameBudget(40, frames: 30);
        });

        suite.Add("perf/budget/big-page-scroll-stays-cheap", t =>
        {
            t.Open(new BigPage(), new Size(900, 700));
            t.Settle();
            var point = new Point(450, 350);
            var report = t.Perf.Measure(frames: 40, raster: false, each: i =>
            {
                t.Window.DispatchInput(new InputEvent(InputKind.Scroll, point, Delta: new Point(0, 48)));
            });
            t.Assert.True(report.Total.Median < 120, $"cuộn trang lớn: trung vị Draw {report.Total.Median:0.0} ms < 120 ms");
            t.Assert.True(report.AllocatedBytes.Median <= 4096, $"cuộn không cấp phát đáng kể: {report.AllocatedBytes.Median:0} B/frame");
        });

        suite.Add("perf/budget/hover-renders-few-views", t =>
        {
            t.Open(new BigPage(), new Size(900, 700));
            t.Settle();
            // di chuột qua dải hàng chữ của thẻ đầu: chỉ view chứa hàng đó (và cha) render lại, không phải cả 40 section
            int renders = t.Perf.CountRenders(() => { t.Hover(new Point(100, 90)); t.Hover(new Point(100, 130)); });
            t.Assert.True(renders <= 6, $"hai lần hover trên hàng chữ render lại {renders} view (≤ 6): hover chỉ làm bẩn view chứa phần tử và cha của nó");
        });

        suite.Add("perf/budget/text-wrap-cache-keeps-resize-bounded", t =>
        {
            t.Open(new BigPage(), new Size(900, 700));
            t.Settle();
            var before = PerfCounters.Snapshot.Now();
            for (int i = 0; i < 20; i++) t.Window.Draw(new Size(900, 700), 1);
            var steady = PerfCounters.Snapshot.Now() - before;
            t.Assert.Equal(steady.WrapMiss, 0L, "20 frame cùng kích thước: không ngắt dòng lại");
            before = PerfCounters.Snapshot.Now();
            t.Window.Draw(new Size(640, 700), 1);
            var resized = PerfCounters.Snapshot.Now() - before;
            t.Assert.True(resized.WrapMiss > 0 && resized.WrapMiss < 4000, $"đổi bề rộng chỉ ngắt dòng lại chữ cần ngắt lại ({resized.WrapMiss})");
        });
    }
}
