using GotX;
using GotX.Test;

/// <summary>Hàng flex-wrap mà mọi phần tử vừa một dòng (flex-1 có basis 0) phải cao đúng bằng khi flex-nowrap, kể cả khi phần tử giữa chứa chữ xuống dòng.</summary>
static class FlexWrapMeasureTests
{
    const string Desc = "Một mô tả khá dài cho mục này, đủ để phải xuống hai dòng trong thẻ hẹp và bị cắt bằng line-clamp-2 nếu còn dài hơn nữa nhiều.";

    static View Item(string wrapClass) => new Fx(w => w.Div().Class("size-full bg-white").Child(
        w.Div().Key("item").Class($"flex w-[290px] {wrapClass} items-center gap-4 rounded-md border p-4")
            .Child(w.Div().Class("size-10 shrink-0 bg-slate-200"))
            .Child(w.Div().Class("flex flex-1 flex-col gap-1")
                .Child(w.Div().Class("text-sm font-medium").Child("Tiêu đề"))
                .Child(w.Div().Key("desc").Class("line-clamp-2 text-sm").Child(Desc)))
            .Child(w.Div().Class("size-4 shrink-0 bg-slate-400"))));

    public static void Register(TestSuite suite)
    {
        suite.Add("core/flex-wrap/single-line-row-measures-like-nowrap", t =>
        {
            t.Open(_ => Item("flex-nowrap"));
            float nowrap = t.Get("item").Layout.Height;
            t.Open(_ => Item("flex-wrap"));
            float wrapped = t.Get("item").Layout.Height;
            t.Assert.True(nowrap > 70 && nowrap < 120, $"nowrap: {nowrap}");
            t.Assert.Near(wrapped, nowrap, 0.5f, "flex-wrap: cùng chiều cao với flex-nowrap (một dòng)");
            t.Assert.Equal(t.Get("desc").Layout.Height, 40f, "mô tả đúng hai dòng (line-clamp-2)");
        });

        suite.Add("core/flex-wrap/really-wrapping-row-still-wraps", t =>
        {
            t.Open(_ => new Fx(w => w.Div().Class("size-full bg-white").Child(
                w.Div().Key("row").Class("flex w-[100px] flex-wrap gap-2")
                    .Child(w.Div().Class("w-[60px] h-6 bg-slate-300")).Child(w.Div().Class("w-[60px] h-6 bg-slate-300")).Child(w.Div().Class("w-[30px] h-6 bg-slate-300")))));
            t.Assert.Equal(t.Get("row").Layout.Height, 2 * 24 + 8f, "hai dòng + gap");
        });
    }
}
