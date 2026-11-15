using GotX;
using GotX.Test;

/// <summary>Con flex không kéo giãn theo trục ngang: bề rộng fit-content = min(max-content, còn lại) (không dùng min-content: ước lượng của GotX cao hơn Chrome ở khối lồng nhau); chữ xuống dòng thì box nhận cả bề rộng còn lại.</summary>
static class FitContentTests
{
    public static void Register(TestSuite suite)
    {
        suite.Add("core/fit-content/row-in-items-start-column-is-clamped-and-text-wraps", t =>
        {
            t.Open(_ => new Fx(w => w.Div().Class("size-full bg-white").Child(
                w.Div().Key("col").Class("flex w-[180px] flex-col items-start").Child(
                    w.Div().Key("row").Class("flex gap-2").Child(w.Div().Key("txt").Class("text-sm font-medium").Child("Steady performance increase"))
                        .Child(w.Div().Class("size-4 shrink-0"))))));
            var row = t.Get("row").Layout; var txt = t.Get("txt").Layout;
            t.Assert.Near(row.Width, 180, 0.5f, "hàng bị kẹp về bề rộng còn lại, không tràn ra 202");
            t.Assert.True(txt.Height > 30, $"chữ xuống 2 dòng (cao {txt.Height})");
            t.Assert.True(txt.Right + 8 + 16 <= row.Right + 0.6f, "chữ + icon nằm gọn trong hàng");
        });

        suite.Add("core/fit-content/short-content-keeps-max-content-width", t =>
        {
            t.Open(_ => new Fx(w => w.Div().Class("size-full bg-white").Child(
                w.Div().Class("flex w-[300px] flex-col items-center").Child(w.Div().Key("t").Class("text-sm").Child("Ngắn")))));
            var b = t.Get("t").Layout;
            t.Assert.True(b.Width < 60, $"chữ ngắn: box = bề rộng chữ ({b.Width})");
            t.Assert.Near(b.X + b.Width / 2, 150, 1f, "vẫn căn giữa");
        });

        suite.Add("core/fit-content/wrapped-text-box-takes-full-available-width", t =>
        {
            const string text = "Một đoạn văn khá dài sẽ phải xuống dòng nhiều lần bên trong thẻ hẹp này để kiểm tra.";
            foreach (var align in new[] { "items-center", "items-start", "items-end" })
            {
                t.Open(_ => new Fx(w => w.Div().Class("size-full bg-white").Child(
                    w.Div().Class("flex w-[200px] flex-col " + align).Child(w.Div().Key("p").Class("text-center text-sm").Child(text)))));
                var b = t.Get("p").Layout;
                t.Assert.Near(b.Width, 200, 0.5f, $"{align}: chữ xuống dòng → box = cả bề rộng 200, không co theo dòng rộng nhất");
                t.Assert.True(b.Height > 40, $"{align}: nhiều dòng (cao {b.Height})");
            }
        });
    }
}
