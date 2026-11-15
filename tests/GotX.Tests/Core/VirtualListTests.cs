using GotX;
using GotX.Components;
using GotX.Test;

/// <summary>VirtualList chiều cao cố định trên 100.000 hàng thật: số node mỗi frame, cuộn tới cuối, bàn phím, key ổn định khi dữ liệu đổi.</summary>
static class VirtualListTests
{
    public sealed record Row(int Id, string Name);

    public static List<Row> MakeRows(int n)
    {
        var rows = new List<Row>(n);
        for (int i = 0; i < n; i++) rows.Add(new Row(i, "Row " + i));
        return rows;
    }

    public static string KeyOf(int id) => "r" + id;

    sealed class Harness
    {
        public List<Row> Rows = MakeRows(100_000);
        public VirtualList List = null!;
        public int Activated;
    }

    static Harness Open(UiTest t)
    {
        var h = new Harness();
        t.Open(app =>
        {
            h.List = app.New(new VirtualList
            {
                Count = h.Rows.Count, ItemHeight = 32, Class = "h-80",
                ItemKey = i => KeyOf(h.Rows[i].Id),
                RenderItem = (w, i, active) => w.Div().Class(active ? "flex items-center bg-muted px-3 text-sm" : "flex items-center px-3 text-sm").Child(h.Rows[i].Name),
            });
            app.Subscribe<VirtualList.Activated>(h.List, _ => h.Activated++);
            return app.New(new FragmentView(w => w.Div().Class("size-full bg-background p-4 text-foreground").Child(w.View(h.List))));
        }, new Size(600, 500));
        t.Settle();
        return h;
    }

    static int Nodes(UiTest t) { t.Frames(); return t.Tree.Nodes.Count; }
    static float Offset(UiTest t, Harness h) => t.Window.GetScroll(h.List.ScrollKey)!.Value.Offset.Y;

    public static void Register(TestSuite suite)
    {
        suite.Add("virtual-list/100k-rows-only-visible-rows-become-nodes", t =>
        {
            var h = Open(t);
            int nodes = Nodes(t);
            t.Assert.True(nodes > 20 && nodes < 400, $"100.000 hàng: {nodes} node mỗi frame (20..400)");
            t.Assert.Exists(KeyOf(0));
            t.Assert.True(h.List.EndRendered < 60, $"chỉ dựng {h.List.EndRendered} hàng đầu");
            t.Assert.NotExists(KeyOf(99_999));
            t.Assert.Equal(h.List.ContentHeight, 100_000d * 32, "tổng chiều cao = số hàng × chiều cao hàng");
            t.Assert.Equal(t.Window.GetScroll(h.List.ScrollKey)!.Value.Max.Y + t.Window.GetScroll(h.List.ScrollKey)!.Value.Viewport.Height, 3_200_000f, "vùng cuộn của Core thấy đủ chiều cao");
            t.Assert.NoLint();
        });

        suite.Add("virtual-list/scroll-to-end-keeps-nodes-and-frame-budget", t =>
        {
            var h = Open(t);
            var point = t.Get(h.List.ScrollKey).Screen;
            var centre = new Point(point.X + 100, point.Y + 100);
            int maxNodes = 0;
            var report = t.Perf.Measure(frames: 60, raster: false, each: i =>
                { t.Window.DispatchInput(new InputEvent(InputKind.Scroll, centre, Delta: new Point(0, 40_000))); maxNodes = Math.Max(maxNodes, t.Tree.Nodes.Count); });
            t.Assert.True(report.Total.Median < 120, $"cuộn 100k hàng: trung vị Draw {report.Total.Median:0.0} ms < 120 ms ({report})");
            t.Assert.True(report.AllocatedBytes.Median < 16_384, $"cuộn: {report.AllocatedBytes.Median:0} B/frame (chỉ chuỗi key của hàng mới) < 16 KB");
            t.Assert.True(maxNodes < 400, $"giữa lúc cuộn: tối đa {maxNodes} node < 400");
            t.Settle();
            t.Scroll(centre, 10_000_000f);
            t.Assert.Exists(KeyOf(99_999));
            t.Assert.Equal(h.List.LastVisible, 99_999, "lăn chuột tới cuối: thấy hàng cuối");
            t.Scroll(centre, -10_000_000f);
            t.Assert.Exists(KeyOf(0));
            h.List.ScrollToIndex(99_999, VirtualList.ScrollAlign.End);
            t.Settle();
            t.Assert.Exists(KeyOf(99_999));
            t.Assert.True(Nodes(t) < 400, "ở cuối danh sách vẫn dưới 400 node");
            var last = t.Get(KeyOf(99_999)).Screen;
            var view = t.Get(h.List.ScrollKey).Screen;
            t.Assert.True(MathF.Abs(last.Bottom - view.Bottom) < 3, $"hàng cuối nằm sát đáy khung ({last.Bottom} / {view.Bottom})");
            t.Assert.Text(KeyOf(99_999), "Row 99999");
            t.Perf.ExpectIdle();
        });

        suite.Add("virtual-list/scroll-to-index-aligns-row", t =>
        {
            var h = Open(t);
            h.List.ScrollToIndex(50_000, VirtualList.ScrollAlign.Start);
            t.Settle();
            var view = t.Get(h.List.ScrollKey).Screen;
            t.Assert.True(MathF.Abs(t.Get(KeyOf(50_000)).Screen.Y - view.Y) < 2, "Start: hàng 50000 ở đầu khung");
            h.List.ScrollToIndex(50_005, VirtualList.ScrollAlign.Nearest);
            t.Settle();
            t.Assert.Visible(KeyOf(50_005));
            float before = Offset(t, h);
            h.List.ScrollToIndex(50_005, VirtualList.ScrollAlign.Nearest);
            t.Settle();
            t.Assert.Near(Offset(t, h), before, 0.5f, "Nearest: hàng đã trong khung thì không cuộn");
            t.Assert.True(h.List.EndRendered - h.List.FirstRendered < 60, "chỉ dựng cửa sổ quanh hàng 50000");
        });

        suite.Add("virtual-list/keyboard-navigation", t =>
        {
            var h = Open(t);
            t.Press(Key.Tab);
            t.Assert.Focused(h.List.FocusKey);
            t.Press(Key.Down); t.Press(Key.Down); t.Press(Key.Down);
            t.Assert.Equal(h.List.ActiveIndex, 2, "↓ ×3 từ chưa chọn → hàng 2");
            t.Press(Key.Up);
            t.Assert.Equal(h.List.ActiveIndex, 1, "↑");
            for (int i = 0; i < 12; i++) t.Press(Key.Down);
            t.Assert.Equal(h.List.ActiveIndex, 13, "↓ thêm 12 lần");
            var view = t.Get(h.List.ScrollKey).Screen;
            t.Assert.True(t.Get(KeyOf(13)).Screen.Bottom <= view.Bottom + 0.5f, "hàng đang chọn được cuộn vào khung");
            t.Press(Key.PageDown);
            t.Assert.True(h.List.ActiveIndex > 13 + 5, $"PageDown nhảy cả trang ({h.List.ActiveIndex})");
            t.Press(Key.End);
            t.Assert.Equal(h.List.ActiveIndex, 99_999, "End");
            t.Settle();
            t.Assert.Visible(KeyOf(99_999));
            t.Press(Key.PageUp);
            t.Assert.True(h.List.ActiveIndex < 99_999 - 5, "PageUp");
            t.Press(Key.Home);
            t.Assert.Equal(h.List.ActiveIndex, 0, "Home");
            t.Settle();
            t.Assert.Near(Offset(t, h), 0f, 0.5f, "Home cuộn về đầu");
            t.Assert.Visible(KeyOf(0));
            t.Press(Key.Enter);
            t.Assert.Equal(h.Activated, 1, "Enter phát Activated");
            t.Assert.True(nodesBounded(t), "node vẫn bị chặn sau khi điều hướng");
            t.Assert.NoLint();
        });

        suite.Add("virtual-list/data-mutation-keeps-keys-stable", t =>
        {
            var h = Open(t);
            h.List.ActiveIndex = 5;
            string activeKey = KeyOf(h.Rows[5].Id);
            h.List.ScrollToIndex(10, VirtualList.ScrollAlign.Start);
            t.Settle();
            var rowBefore = t.Get(KeyOf(12));
            string textBefore = t.Tree.TextOf(rowBefore);
            // Chèn 3 hàng mới ở đầu: hàng cũ đổi chỉ số nhưng giữ key; hàng đang chọn theo key.
            h.Rows.InsertRange(0, [new Row(-1, "New A"), new Row(-2, "New B"), new Row(-3, "New C")]);
            h.List.Count = h.Rows.Count;
            h.List.Changed();
            t.Settle();
            t.Assert.Exists(KeyOf(12));
            t.Assert.Equal(t.Tree.TextOf(t.Get(KeyOf(12))), textBefore, "key của hàng 12 vẫn trỏ về cùng dữ liệu");
            t.Assert.Equal(h.List.ActiveIndex, 8, "hàng đang chọn đi theo key (5 → 8)");
            t.Assert.Equal(KeyOf(h.Rows[h.List.ActiveIndex].Id), activeKey, "key hàng đang chọn không đổi");
            // Xoá hàng đang chọn: không còn hàng chọn; xoá nhiều hàng: số hàng và tổng chiều cao theo kịp.
            h.Rows.RemoveRange(0, 50_000);
            h.List.Count = h.Rows.Count;
            h.List.Changed();
            t.Settle();
            t.Assert.Equal(h.List.ActiveIndex, -1, "hàng đang chọn bị xoá → -1");
            t.Assert.Equal(h.List.ContentHeight, h.Rows.Count * 32d, "tổng chiều cao theo số hàng mới");
            t.Assert.True(Nodes(t) < 400, "sau khi xoá vẫn bị chặn node");
        });
    }

    static bool nodesBounded(UiTest t) => Nodes(t) < 400;
}
