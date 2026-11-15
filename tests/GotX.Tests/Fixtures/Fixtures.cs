using GotX;
using GotX.Test;

/// <summary>View dựng từ một lambda – để mỗi test tự mô tả giao diện của nó ngay tại chỗ.</summary>
sealed class Fx(Func<Window, Div> render) : View
{
    public override Div Render(Window w) => render(w);

    /// <summary>Khung chung của mọi fixture: nền trắng, chữ đậm màu, lề 24 px, các khối xếp dọc cách nhau 16 px.</summary>
    public static Div Page(Window w) => w.Div().Class("size-full p-6 flex flex-col items-start gap-4 bg-white text-slate-900");

    /// <summary>Hộp màu kích thước cố định, có khoá.</summary>
    public static Div Box(Window w, string key, string classes = "w-16 h-8 bg-slate-300") => w.Div().Key(key).Class(classes);
}

/// <summary>Bộ đếm: view con có state, bấm nút thì chỉ nó (và view cha) phải render lại.</summary>
sealed class CounterView : View
{
    public int Count, Renders;
    public string KeyPrefix = "c";

    public override Div Render(Window w)
    {
        Renders++;
        return w.Div().Class("flex items-center gap-3")
            .Child(w.Div().Key(KeyPrefix + "-inc").Focusable()
                .Class("px-3 h-8 flex items-center rounded-md bg-slate-900 text-white text-sm cursor-pointer hover:bg-slate-700")
                .OnClick(this, static (v, _) => { v.Count++; v.Notify(); }).Child("Tăng"))
            .Child(w.Div().Key(KeyPrefix + "-value").Class("text-sm").Child($"Giá trị: {Count}"));
    }
}

/// <summary>Khối tĩnh: không bao giờ Notify, nên sau frame đầu phải luôn được phát lại từ cache.</summary>
sealed class StaticPanel : View
{
    public int Renders;

    public override Div Render(Window w)
    {
        Renders++;
        var list = w.Div().Class("flex flex-col gap-1");
        for (int i = 0; i < 20; i++) list.Child(w.Div().Class("h-6 px-2 flex items-center rounded bg-slate-100 text-xs").Child("Dòng tĩnh"));
        return list;
    }
}

/// <summary>Gốc gồm một bộ đếm và một khối tĩnh – mẫu chuẩn để đo "một thao tác render lại bao nhiêu view".</summary>
sealed class CounterHost : View
{
    public CounterView Counter = null!;
    public StaticPanel Panel = null!;
    public int Renders;

    protected override void OnCreated()
    {
        Counter = App.New(new CounterView());
        Panel = App.New(new StaticPanel());
    }

    public override Div Render(Window w)
    {
        Renders++;
        return Fx.Page(w).Child(w.View(Counter)).Child(w.View(Panel));
    }
}

/// <summary>Lỗi cố ý: tự Notify ngay trong Render → không frame nào được phát lại, giao diện không bao giờ đứng yên.</summary>
sealed class RestlessView : View
{
    public int Renders;

    public override Div Render(Window w)
    {
        Renders++;
        Notify();
        return Fx.Page(w).Child(w.Div().Key("label").Class("text-sm").Child("Không bao giờ yên"));
    }
}

/// <summary>Giao diện sạch có đủ thứ thường gặp: thanh công cụ, thẻ, danh sách cuộn, chữ cắt bằng ellipsis. Lint phải im lặng.</summary>
sealed class CleanView : View
{
    public int Saves;

    public override Div Render(Window w)
    {
        Div Button(string key, string label, string tone) => w.Div().Key(key).Focusable()
            .Class("h-8 px-3 flex items-center rounded-md text-sm cursor-pointer " + tone)
            .OnClick(this, static (v, _) => { v.Saves++; v.Notify(); }).Child(label);

        var list = w.Div().Key("list").Class("w-64 h-32 overflow-y-auto rounded-md border border-slate-300");
        for (int i = 0; i < 12; i++)
            list.Child(w.Div().Class("h-8 px-3 flex items-center text-sm border-b border-slate-200").Child("Mục " + (i + 1)));

        return Fx.Page(w)
            .Child(w.Div().Key("toolbar").Class("flex items-center gap-2")
                .Child(Button("save", "Lưu", "bg-slate-900 text-white"))
                .Child(Button("cancel", "Huỷ", "bg-slate-200 text-slate-900"))
                .Child(w.Div().Key("status").Class("text-sm text-slate-700").Child($"Đã lưu {Saves} lần")))
            .Child(w.Div().Key("card").Class("w-64 p-4 flex flex-col gap-2 rounded-lg border border-slate-300 bg-slate-50")
                .Child(w.Div().Class("text-base font-semibold").Child("Tiêu đề thẻ"))
                .Child(w.Div().Key("long").Class("truncate text-sm text-slate-700").Child("Một dòng mô tả rất dài sẽ được cắt bằng dấu ba chấm ở cuối")))
            .Child(list);
    }
}
