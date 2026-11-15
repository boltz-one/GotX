using GotX;
using GotX.Components;
using GotX.Test;

/// <summary>VirtualList chiều cao thay đổi trên 100.000 hàng: đo hình học thật, tổng ước lượng, cuộn tới cuối, key ổn định khi dữ liệu đổi.</summary>
static class VirtualListVariableTests
{
    // Chiều cao do nội dung quyết định: bốn cỡ lặp theo id (class dựng sẵn, không tạo chuỗi mới mỗi hàng).
    static readonly string[] HeightClass = ["h-8", "h-12", "h-16", "h-20"];
    static readonly float[] Heights = [32, 48, 64, 80];

    sealed class Harness
    {
        public List<VirtualListTests.Row> Rows = VirtualListTests.MakeRows(100_000);
        public VirtualList List = null!;
    }

    static Harness Open(UiTest t)
    {
        var h = new Harness();
        t.Open(app =>
        {
            h.List = app.New(new VirtualList
            {
                Count = h.Rows.Count, VariableHeight = true, EstimatedHeight = 56, Class = "h-96",
                ItemKey = i => VirtualListTests.KeyOf(h.Rows[i].Id),
                RenderItem = (w, i, active) => w.Div().Class("flex items-center border-b border-border px-3 text-sm").Class(HeightClass[h.Rows[i].Id & 3]).Child(h.Rows[i].Name),
            });
            return app.New(new FragmentView(w => w.Div().Class("size-full bg-background p-4 text-foreground").Child(w.View(h.List))));
        }, new Size(600, 600));
        t.Settle();
        return h;
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("virtual-list/variable/rows-are-measured-and-stack-without-gaps", t =>
        {
            var h = Open(t);
            int nodes = t.Tree.Nodes.Count;
            t.Assert.True(nodes < 400, $"100.000 hàng cao thay đổi: {nodes} node < 400");
            for (int i = 0; i < 6; i++)
            {
                var row = t.Get(VirtualListTests.KeyOf(i)).Screen;
                t.Assert.Near(row.Height, Heights[i & 3], 0.6f, $"hàng {i} cao đúng nội dung");
                if (i > 0) t.Assert.Near(row.Y, t.Get(VirtualListTests.KeyOf(i - 1)).Screen.Bottom, 0.6f, $"hàng {i} nối liền hàng trước");
            }
            // 4 cỡ lặp: trung bình thật 56 = EstimatedHeight, nên tổng ước lượng gần tổng thật (100000 × 56).
            t.Assert.True(MathF.Abs((float)h.List.ContentHeight - 5_600_000f) < 600, $"tổng ước lượng {h.List.ContentHeight} gần 5.600.000");
            t.Assert.NoLint();
        });

        suite.Add("virtual-list/variable/scroll-to-end-lands-on-last-row", t =>
        {
            var h = Open(t);
            var area = t.Get(h.List.ScrollKey).Screen;
            var centre = new Point(area.X + 100, area.Y + 100);
            var report = t.Perf.Measure(frames: 40, raster: false, each: i =>
                t.Window.DispatchInput(new InputEvent(InputKind.Scroll, centre, Delta: new Point(0, 60_000))));
            t.Assert.True(report.Total.Median < 120, $"cuộn: trung vị Draw {report.Total.Median:0.0} ms < 120 ms");
            h.List.ScrollToIndex(99_999, VirtualList.ScrollAlign.End);
            t.Settle();
            string last = VirtualListTests.KeyOf(99_999);
            t.Assert.Exists(last);
            var box = t.Get(last).Screen;
            t.Assert.Near(box.Height, Heights[99_999 & 3], 0.6f, "hàng cuối cao đúng nội dung");
            t.Assert.Near(box.Bottom, area.Bottom, 3f, "hàng cuối sát đáy khung dù tổng chỉ là ước lượng");
            t.Assert.True(t.Tree.Nodes.Count < 400, $"ở cuối: {t.Tree.Nodes.Count} node < 400");
            t.Assert.Text(last, "Row 99999");
            t.Perf.ExpectIdle();
        });

        suite.Add("virtual-list/variable/jump-to-middle-and-data-mutation", t =>
        {
            var h = Open(t);
            h.List.ScrollToIndex(60_000, VirtualList.ScrollAlign.Start);
            t.Settle();
            string key = VirtualListTests.KeyOf(60_000);
            t.Assert.Visible(key);
            t.Assert.Near(t.Get(key).Screen.Y, t.Get(h.List.ScrollKey).Screen.Y, 2f, "hàng 60000 ở đầu khung");
            // Xoá 10 hàng đầu: key của hàng 60000 không đổi, vẫn ở đúng chỗ (chiều cao đã đo được nhớ theo key).
            h.Rows.RemoveRange(0, 10);
            h.List.Count = h.Rows.Count;
            h.List.Changed();
            t.Settle();
            t.Assert.Exists(key);
            t.Assert.Near(t.Get(key).Screen.Height, Heights[60_000 & 3], 0.6f, "hàng 60000 giữ đúng chiều cao sau khi dữ liệu đổi");
            t.Assert.Text(key, "Row 60000");
            t.Assert.True(t.Tree.Nodes.Count < 400, "dưới 400 node sau khi đổi dữ liệu");
        });
    }
}
