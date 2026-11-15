using GotX;
using GotX.Test;

/// <summary>Hàng flex: flex-basis auto = max-content, sau đó co theo shrink (tỉ lệ theo basis); không lấy bề rộng đã xuống dòng ở chỗ còn lại làm basis.</summary>
static class FlexBasisTests
{
    public static void Register(TestSuite suite)
    {
        suite.Add("core/flex-basis/auto-is-max-content-then-shrinks-proportionally", t =>
        {
            const string a = "Một đoạn chữ khá dài ở phần tử thứ nhất của hàng", b = "Ngắn hơn một chút";
            t.Open(_ => new Fx(w => w.Div().Class("size-full bg-white").Child(
                w.Div().Class("flex w-[200px] gap-0 text-sm")
                    .Child(w.Div().Key("a").Child(a)).Child(w.Div().Key("b").Child(b)))));
            var ts = t.Get("a").TextStyle;
            float wa = t.Window.Text.Measure(a, ts), wb = t.Window.Text.Measure(b, ts);
            float expectA = wa - (wa + wb - 200) * wa / (wa + wb);
            t.Assert.Near(t.Get("a").Layout.Width, expectA, 1.2f, "co theo tỉ lệ basis (max-content), không phải chia theo chữ đã xuống dòng");
            t.Assert.Near(t.Get("a").Layout.Width + t.Get("b").Layout.Width, 200, 0.6f, "đầy hàng");
        });
        suite.Add("core/flex-basis/fits-without-shrinking", t =>
        {
            t.Open(_ => new Fx(w => w.Div().Class("size-full bg-white").Child(
                w.Div().Class("flex w-[400px] text-sm").Child(w.Div().Key("a").Child("ngắn")).Child(w.Div().Key("b").Child("cũng ngắn")))));
            t.Assert.True(t.Get("a").Layout.Width < 40 && t.Get("b").Layout.Width < 70, "vừa chỗ: giữ bề rộng tự nhiên");
        });
    }
}
