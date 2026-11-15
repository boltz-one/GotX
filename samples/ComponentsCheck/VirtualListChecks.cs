using GotX;
using GotX.Components;

/// <summary>Kiểm tra VirtualList trên 100.000 hàng thật: chỉ hàng nhìn thấy có node, phím End/Home/↓ cuộn và tô hàng đang chọn, thanh cuộn của vùng cuộn Core.</summary>
static class VirtualListChecks
{
    public static void Run(CheckHarness h)
    {
        const int Rows = 100_000;
        VirtualList list = null!;
        var w = h.Open(app =>
        {
            list = app.New(new VirtualList
            {
                Count = Rows, ItemHeight = 32, Class = "h-72 w-80 border border-border",
                ItemKey = static i => "vl-" + i,
                RenderItem = static (win, i, active) => win.Div().Class(active ? "flex items-center bg-primary px-3 text-sm text-primary-foreground" : "flex items-center px-3 text-sm").Child("Item " + i),
            });
            return app.New(new FragmentView(win => win.Div().Class("size-full bg-background p-6 text-foreground").Child(win.View(list))));
        });
        h.Check($"virtual-list: 100.000 hàng, chỉ dựng cửa sổ đầu ({list.FirstRendered}..{list.EndRendered})",
            h.Exists(w, "vl-0") && !h.Exists(w, "vl-60") && list.EndRendered < 40);
        h.Check($"virtual-list: tổng chiều cao {list.ContentHeight} = hàng × 32", list.ContentHeight == Rows * 32.0);
        h.Press(w, Key.Tab);
        h.Check("virtual-list: Tab focus khung danh sách", w.IsFocused(list.FocusKey));
        h.Press(w, Key.Down);
        h.Press(w, Key.Down);
        var row1 = h.Box(w, "vl-1");
        var fill = h.Pixel(w, new Point(row1.X + row1.Width - 6, row1.Y + 4));
        h.Check($"virtual-list: ↓↓ chọn hàng 1 và tô nền primary ({list.ActiveIndex}, {fill})", list.ActiveIndex == 1 && fill.R < 90 && fill.B < 90);
        h.Press(w, Key.End);
        h.Wait(w, 0.5);
        h.Check($"virtual-list: End tới hàng cuối, hàng cuối trong khung ({list.ActiveIndex})",
            list.ActiveIndex == Rows - 1 && h.Exists(w, "vl-99999") && !h.Exists(w, "vl-0") && list.EndRendered - list.FirstRendered < 40);
        var view = h.Box(w, list.ScrollKey);
        float offset = w.GetScroll(list.ScrollKey)!.Value.Offset.Y; // FindBounds trả toạ độ nội dung, chưa trừ cuộn
        var last = h.Box(w, "vl-99999");
        h.Check($"virtual-list: hàng cuối nằm trong khung cuộn ({last.Bottom - offset} ≤ {view.Bottom})", last.Bottom - offset <= view.Bottom + 0.5f);
        h.Press(w, Key.Home);
        h.Wait(w, 0.5);
        h.Check("virtual-list: Home về hàng đầu", list.ActiveIndex == 0 && h.Exists(w, "vl-0") && w.GetScroll(list.ScrollKey)!.Value.Offset.Y < 0.5f);
        list.ScrollToIndex(70_000, VirtualList.ScrollAlign.Start);
        h.Wait(w, 0.3);
        h.Check("virtual-list: ScrollToIndex(70000) đưa hàng đó lên đầu khung",
            h.Exists(w, "vl-70000") && Math.Abs(h.Box(w, "vl-70000").Y - w.GetScroll(list.ScrollKey)!.Value.Offset.Y - h.Box(w, list.ScrollKey).Y) < 2);
    }
}
