using GotX;
using GotX.Test;

/// <summary>Track fr tường minh (1fr = minmax(auto, 1fr)) không hẹp hơn min-content của phần tử; grid-cols-N (minmax(0,1fr)) thì co được.</summary>
static class GridFrMinTests
{
    static View Grid(string classes, string textClasses = "whitespace-nowrap text-3xl", string text = "$1,250.00") => new Fx(w => w.Div().Class("size-full bg-white").Child(
        w.Div().Key("grid").Class(classes)
            .Child(w.Div().Key("a").Class(textClasses).Child(text))
            .Child(w.Div().Key("b").Class("w-[73px] h-5"))));

    public static void Register(TestSuite suite)
    {
        suite.Add("core/grid/explicit-fr-keeps-min-content", t =>
        {
            t.Open(_ => Grid("grid w-[190px] grid-cols-[1fr_auto] gap-2"));
            float textW = t.Window.Text.Measure("$1,250.00", TextStyle.Default with { FontSize = 30, LineHeight = 36 });
            var a = t.Get("a").Layout; var b = t.Get("b").Layout;
            t.Assert.Near(a.Width, textW, 1.2f, "cột 1 = bề rộng chữ (min-content), không bị ép còn 190 − 8 − 73 = 109");
            t.Assert.Near(b.X, a.Right + 8, 0.6f, "cột auto đứng sau cột 1 + gap (tràn phải)");
            t.Assert.Equal(b.Width, 73f, "phần tử cột auto giữ 73");
        });

        suite.Add("core/grid/explicit-fr-still-fills-when-content-fits", t =>
        {
            t.Open(_ => Grid("grid w-[400px] grid-cols-[1fr_auto] gap-2"));
            t.Assert.Near(t.Get("a").Layout.Width, 400 - 8 - 73, 0.6f, "đủ chỗ: cột fr nhận phần dư");
        });

        suite.Add("core/grid/grid-cols-n-and-minmax-zero-can-shrink", t =>
        {
            t.Open(_ => Grid("grid w-[212px] grid-cols-2 gap-2"));
            t.Assert.Near(t.Get("a").Layout.Width, 102, 0.6f, "grid-cols-2 = minmax(0,1fr): chia đều dù chữ rộng hơn");
            t.Open(_ => Grid("grid w-[212px] grid-cols-[minmax(0,1fr)_auto] gap-2"));
            t.Assert.Near(t.Get("a").Layout.Width, 212 - 8 - 73, 0.6f, "minmax(0,1fr) không có min tự động");
        });

        suite.Add("core/grid/fr-min-content-of-wrappable-text-is-longest-word", t =>
        {
            t.Open(_ => Grid("grid w-[212px] grid-cols-[1fr_auto] gap-2", "text-sm", "Steady performance increase"));
            t.Assert.Near(t.Get("a").Layout.Width, 131, 0.6f, "chữ xuống dòng được: min-content nhỏ nên cột vẫn nhận 212 − 8 − 73");
        });
    }
}
