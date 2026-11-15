using GotX;
using GotX.Test;

/// <summary>items-baseline / self-baseline: dòng chữ đầu tiên của các phần tử trong hàng flex thẳng hàng; hàng cao đúng bằng phần trên + phần dưới đường cơ sở lớn nhất.</summary>
static class BaselineAlignTests
{
    static float BaselineY(UiTest t, string key)
    {
        var n = t.Get(key);
        var ts = n.TextStyle;
        var font = t.Window.Text.FontFor(ts);
        float k = ts.FontSize / font.UnitsPerEm, ascent = font.Ascender * k, descent = -font.Descender * k;
        return n.Layout.Y + (ts.LineHeight - (ascent + descent)) / 2 + ascent;
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("core/baseline/items-baseline-aligns-first-line", t =>
        {
            t.Open(_ => new Fx(w => w.Div().Class("size-full bg-white").Child(
                w.Div().Key("row").Class("flex w-[300px] items-baseline justify-between")
                    .Child(w.Div().Key("label").Class("text-sm").Child("Minimum Payout Amount"))
                    .Child(w.Div().Key("number").Class("text-2xl font-semibold").Child("$2500.00")))));
            t.Assert.Near(BaselineY(t, "label"), BaselineY(t, "number"), 0.6f, "hai dòng chữ chung một đường cơ sở");
            var label = t.Get("label").Layout; var number = t.Get("number").Layout; var row = t.Get("row").Layout;
            t.Assert.True(label.Y > number.Y, "nhãn nhỏ bị đẩy xuống so với số lớn");
            t.Assert.Near(row.Height, MathF.Max(label.Bottom, number.Bottom) - MathF.Min(label.Y, number.Y), 0.6f, "hàng cao = phần trên + phần dưới đường cơ sở lớn nhất");
        });

        suite.Add("core/baseline/self-baseline-and-nested-first-line", t =>
        {
            t.Open(_ => new Fx(w => w.Div().Class("size-full bg-white").Child(
                w.Div().Class("flex w-[300px] items-center gap-2")
                    .Child(w.Div().Key("a").Class("self-baseline text-xs").Child("small"))
                    .Child(w.Div().Class("self-baseline flex flex-col p-2").Child(w.Div().Key("b").Class("text-3xl").Child("Big"))))));
            t.Assert.Near(BaselineY(t, "a"), BaselineY(t, "b"), 0.6f, "self-baseline: chữ lồng trong hộp có padding vẫn thẳng hàng theo dòng đầu");
        });

        suite.Add("core/baseline/default-start-unchanged", t =>
        {
            t.Open(_ => new Fx(w => w.Div().Class("size-full bg-white").Child(
                w.Div().Key("row").Class("flex w-[300px] items-start")
                    .Child(w.Div().Key("label").Class("text-sm").Child("x")).Child(w.Div().Key("number").Class("text-2xl").Child("y")))));
            t.Assert.Equal(t.Get("label").Layout.Y, t.Get("number").Layout.Y, "items-start: đỉnh bằng nhau");
        });
    }
}
