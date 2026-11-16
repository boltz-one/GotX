using GotX;
using GotX.Components;
using GotX.Test;

/// <summary>VirtualList thiếu chiều cao xác định dựng cả danh sách; phải có đúng một cảnh báo dev (TraceKind.Warn), và không cảnh báo khi có chiều cao.</summary>
static class VirtualListBoundedTests
{
    static int Warnings(UiTest t, string classes, int rows)
    {
        t.Open(app =>
        {
            var list = app.New(new VirtualList
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

    public static void Register(TestSuite suite)
    {
        suite.Add("virtual-list/no-definite-height-warns-once", t =>
            t.Assert.Equal(Warnings(t, "", 2000), 1, "danh sách 2000 hàng trong cha cao theo nội dung: đúng một cảnh báo"));
        suite.Add("virtual-list/definite-height-does-not-warn", t =>
            t.Assert.Equal(Warnings(t, "h-80", 2000), 0, "h-80: không cảnh báo"));
    }
}
