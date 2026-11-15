using GotX;
using GotX.Test;

/// <summary>w-fit và inline-flex / inline-block / inline-grid: bề rộng shrink-to-fit (không bị kéo giãn theo cha block / cột flex); vẫn bị kẹp ở chỗ còn lại khi chữ dài.</summary>
static class ShrinkWrapTests
{
    static View Page(string parentClass, string childClass, string text, string childKey = "c") => new Fx(w => w.Div().Class("size-full bg-white").Child(
        w.Div().Class(parentClass).Child(w.Div().Key("plain").Child("x")).Child(w.Div().Key(childKey).Class(childClass).Child(text))));

    public static void Register(TestSuite suite)
    {
        suite.Add("core/shrink-wrap/w-fit-in-block-and-column-flex", t =>
        {
            t.Open(_ => Page("w-[200px]", "w-fit text-sm", "Ngắn"));
            t.Assert.Equal(t.Get("plain").Layout.Width, 200f, "con block thường kéo giãn theo cha");
            t.Assert.True(t.Get("c").Layout.Width < 50, $"w-fit: co theo nội dung ({t.Get("c").Layout.Width})");
            t.Open(_ => Page("flex w-[200px] flex-col", "w-fit text-sm", "Ngắn"));
            t.Assert.True(t.Get("c").Layout.Width < 50 && t.Get("plain").Layout.Width == 200, "cột flex stretch: chỉ con w-fit co lại");
        });

        suite.Add("core/shrink-wrap/inline-display-values-shrink", t =>
        {
            foreach (var cls in new[] { "inline-flex text-sm", "inline-block text-sm" })
            {
                t.Open(_ => Page("w-[200px]", cls, "Ngắn"));
                t.Assert.True(t.Get("c").Layout.Width < 50, $"{cls}: co theo nội dung ({t.Get("c").Layout.Width})");
            }
            t.Open(_ => Page("w-[200px]", "flex text-sm", "Ngắn"));
            t.Assert.Equal(t.Get("c").Layout.Width, 200f, "flex (block-level) vẫn kéo giãn");
            t.Open(_ => Page("flex w-[200px] flex-col", "inline-flex text-sm", "Ngắn"));
            t.Assert.Equal(t.Get("c").Layout.Width, 200f, "con của flex được blockify: inline-flex trong cột stretch vẫn kéo giãn");
            t.Open(_ => Page("w-[200px]", "inline-flex flex text-sm", "Ngắn"));
            t.Assert.Equal(t.Get("c").Layout.Width, 200f, "flex đặt sau inline-flex đưa về block-level");
        });

        suite.Add("core/shrink-wrap/long-text-is-clamped-to-available", t =>
        {
            const string text = "Một đoạn văn khá dài để chắc chắn phải xuống dòng trong cột hai trăm pixel này.";
            t.Open(_ => Page("w-[200px]", "w-fit text-sm", text));
            var b = t.Get("c").Layout;
            t.Assert.Near(b.Width, 200, 0.6f, "chữ dài: w-fit = chỗ còn lại");
            t.Assert.True(b.Height > 30, $"xuống nhiều dòng ({b.Height})");
        });

        suite.Add("core/shrink-wrap/explicit-width-wins", t =>
        {
            t.Open(_ => Page("w-[200px]", "w-fit w-[120px] text-sm", "Ngắn"));
            t.Assert.Equal(t.Get("c").Layout.Width, 120f, "w-[120px] đặt sau w-fit");
        });
    }
}
