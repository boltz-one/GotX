using GotX;
using GotX.Components;
using GotX.Test;

/// <summary>VirtualList thiếu chiều cao xác định dựng cả danh sách; phải có đúng một cảnh báo dev (TraceKind.Warn), và không cảnh báo khi có chiều cao.</summary>
static class VirtualListBoundedTests
{
    static VirtualList? _last;

    static int Warnings(UiTest t, string classes, int rows)
    {
        t.Open(app =>
        {
            var list = _last = app.New(new VirtualList
            {
                Count = rows, ItemHeight = 32, Class = classes,
                ItemKey = i => "r" + i,
                RenderItem = (w, i, active) => w.Div().Class("px-3 text-sm").Child("Row " + i),
            });
            return app.New(new FragmentView(w => w.Div().Class("w-full bg-background p-4 text-foreground").Child(w.View(list))));
        }, new Size(600, 500));
        t.Settle();
        t.Frames(); t.Frames(); t.Frames();
        return t.Trace.Count(TraceKind.Warn, 0, "virtual-list-unbounded");
    }

    static void Fallback(UiTest t, int rows)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        t.Assert.Equal(Warnings(t, "", rows), 1, "một cảnh báo");
        t.Assert.True(_last!.EndRendered - _last.FirstRendered < 100, "dự phòng max-h-screen: chỉ dựng hàng nhìn thấy, không dựng cả " + rows);
        t.Assert.True(sw.ElapsedMilliseconds < 5000, "không dựng hết: " + sw.ElapsedMilliseconds + " ms");
        t.Scroll(new Point(300, 250), 4000);
        t.Frames();
        t.Assert.True(_last.FirstVisible > 50 && _last.EndRendered - _last.FirstRendered < 100, "cuộn trong khung dự phòng vẫn ảo hoá");
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("virtual-list/no-definite-height-100k-falls-back", t => Fallback(t, 100_000));
        suite.Add("virtual-list/no-definite-height-1m-falls-back", t => Fallback(t, 1_000_000));
        suite.Add("virtual-list/no-definite-height-warns-once", t =>
            t.Assert.Equal(Warnings(t, "", 2000), 1, "danh sách 2000 hàng trong cha cao theo nội dung: đúng một cảnh báo"));
        suite.Add("virtual-list/definite-height-does-not-warn", t =>
            t.Assert.Equal(Warnings(t, "h-80", 2000), 0, "h-80: không cảnh báo"));
    }
}
