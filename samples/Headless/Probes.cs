using GotX;

/// <summary>View chỉ để kiểm tra layout: wrap, grid, absolute, cuộn, transition.</summary>
sealed class LayoutProbe : View
{
    public override Div Render(Window w)
    {
        var wrap = w.Div().Class("flex flex-wrap gap-2 w-[300px]");
        for (int i = 0; i < 5; i++) wrap.Child(w.Div().Class("w-[100px] h-[20px] bg-slate-200").Key("w" + i));
        var grid = w.Div().Class("grid grid-cols-4 gap-4 w-[400px]");
        for (int i = 0; i < 5; i++) grid.Child(w.Div().Class(i == 0 ? "col-span-2 h-8 bg-indigo-200" : "h-8 bg-indigo-100").Key("g" + i));
        var tracks = w.Div().Class("grid grid-cols-[100px_1fr] w-[400px]")
            .Child(w.Div().Class("h-4").Key("t0")).Child(w.Div().Class("h-4").Key("t1"));
        var list = w.Div().Class("h-[100px] w-[200px] overflow-y-auto bg-slate-50");
        for (int i = 0; i < 10; i++) list.Child(w.Div().Class("h-[30px] shrink-0 border-b border-slate-200").Child("Dòng"));
        return w.Div().Class("flex flex-col gap-4 p-4")
            .Child(wrap).Child(grid).Child(tracks)
            .Child(w.Div().Class("relative w-40 h-10").Key("absParent").Child(w.Div().Class("absolute inset-0").Key("abs")))
            .Child(list)
            .Child(w.Div().Class("w-40 h-10 border border-slate-300 bg-white transition-colors duration-200 ease-linear hover:bg-black").Key("fade"));
    }
}

/// <summary>View kiểm tra viền từng cạnh, xoay, hit-test khi xoay, animate-spin.</summary>
sealed class EffectProbe : View
{
    public int Clicks;
    public override Div Render(Window w) =>
        w.Div().Class("flex flex-col gap-8 p-8 bg-white")
            .Child(w.Div().Class("w-40 h-12 bg-white border-l-4 border-l-red-500").Key("callout"))
            .Child(w.Div().Class("w-[60px] h-[8px] bg-black rotate-90").Key("bar"))
            .Child(w.Div().Class("size-20 bg-slate-300 rotate-45").Key("diamond").OnClick(this, static (v, _) => v.Clicks++))
            .Child(w.Div().Class("size-20 rounded-full border-8 border-slate-100 border-t-blue-600 animate-spin").Key("spin"));
}

/// <summary>View kiểm tra kiểu viền, vùng cắt theo hình, skew, origin, outline.</summary>
sealed class ShapeProbe : View
{
    public override Div Render(Window w) =>
        w.Div().Class("grid grid-cols-3 items-start gap-12 p-12 bg-white")
            .Child(w.Div().Class("w-40 h-10 bg-white border-2 border-dashed border-black").Key("dashed"))
            .Child(w.Div().Class("w-40 h-10 bg-white border-2 border-dotted border-black").Key("dotted"))
            .Child(w.Div().Class("w-40 h-10 bg-white border-2 border-black").Key("solid"))
            .Child(w.Div().Class("size-20 rounded-full overflow-hidden").Key("roundclip").Child(w.Div().Class("size-full bg-red-500")))
            .Child(w.Div().Class("size-20 rotate-45 overflow-hidden").Key("rotclip").Child(w.Div().Class("size-full bg-red-500")))
            .Child(w.Div().Class("w-20 h-10 bg-black skew-x-[30deg]").Key("skew"))
            .Child(w.Div().Class("w-20 h-4 ml-6 bg-black origin-top-left rotate-90").Key("origin"))
            .Child(w.Div().Class("w-20 h-10 ml-4 bg-white border border-slate-200 outline-2 outline-offset-2 outline-red-500").Key("outline"));
}

/// <summary>Entity trơn dùng cho các kiểm tra hồi quy (effect, Release).</summary>
sealed class RegressionModel : Entity { }

/// <summary>Vùng cuộn có khoá + ô nhận click trái và click phải/giữa.</summary>
sealed class ScrollProbe : View
{
    public int Clicks, Aux;
    public MouseButton LastButton;

    public override Div Render(Window w)
    {
        var list = w.Div().Class("h-[100px] w-[200px] overflow-y-auto bg-slate-50").Key("keep");
        for (int i = 0; i < 10; i++) list.Child(w.Div().Class("h-[30px] shrink-0"));
        return w.Div().Class("flex flex-col gap-4 p-4")
            .Child(w.Div().Class("w-20 h-10 bg-slate-300").Key("box")
                .OnClick(this, static (v, _) => v.Clicks++)
                .OnAuxClick(this, static (v, e) => { v.Aux++; v.LastButton = e.Button; }))
            .Child(list);
    }
}

/// <summary>Sinh hàng loạt khoá mới mỗi lần render (giả lập danh sách có khoá theo id dòng).</summary>
sealed class KeySpam : View
{
    public int Count, Generation;

    public override Div Render(Window w)
    {
        var box = w.Div().Class("flex flex-wrap");
        for (int i = 0; i < Count; i++) box.Child(w.Div().Class("size-px").Key($"dyn-{Generation}-{i}"));
        return box;
    }
}

/// <summary>Hai view anh em: Notify view này thì view kia được phát lại từ cache.</summary>
sealed class HostProbe : View
{
    public ScrollProbe Scroll = null!;
    public KeySpam Spam = null!;

    protected override void OnCreated()
    {
        Scroll = App.New(new ScrollProbe());
        Spam = App.New(new KeySpam());
    }

    public override Div Render(Window w) => w.Div().Class("flex flex-col").Child(w.View(Scroll)).Child(w.View(Spam));
}

sealed class BlankView : View
{
    public override Div Render(Window w) => w.Div().Class("size-full bg-white");
}
