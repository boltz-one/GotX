using GotX;

/// <summary>View bộ đếm: state trong field, Notify() khi đổi → chỉ view này (và view cha) render lại.</summary>
public sealed class CounterView : View
{
    int _count;
    string _label = "0";

    void Add(int delta)
    {
        _count += delta;
        _label = _count.ToString(); // cấp phát khi giá trị đổi, không phải mỗi frame
        Notify();
    }

    public override Div Render(Window w) =>
        w.Div().Class("flex flex-col items-center gap-4 p-6 h-full rounded-xl border shadow-md " +
                      "bg-white border-slate-200 dark:bg-slate-900 dark:border-slate-800")
            .Child(w.Div().Class("text-sm font-semibold text-indigo-600 dark:text-indigo-400").Child("Bộ đếm (view riêng)"))
            .Child(w.Div().Class("text-5xl font-bold text-slate-900 dark:text-white").Child(_label))
            .Child(w.Div().Class("text-xs text-center text-slate-500 dark:text-slate-400")
                .Child("Bấm nút: chỉ view này render lại, các view khác được phát lại từ cache."))
            .Child(
                w.Div().Class("flex gap-2 w-full")
                    .Child(w.Div().Class(Secondary).Child("− Giảm").OnClick(this, static (v, _) => v.Add(-1)))
                    .Child(w.Div().Class(Primary).Child("+ Tăng").OnClick(this, static (v, _) => v.Add(1))));

    public const string Primary =
        "flex-1 flex justify-center py-2.5 rounded-lg text-sm font-semibold cursor-pointer shadow-xs " +
        "bg-indigo-600 text-white hover:bg-indigo-700 active:bg-indigo-800";

    public const string Secondary =
        "flex-1 flex justify-center py-2.5 rounded-lg text-sm font-semibold cursor-pointer border " +
        "bg-white text-slate-700 border-slate-300 hover:bg-slate-50 hover:border-slate-400 active:bg-slate-100 " +
        "dark:bg-slate-800 dark:text-slate-200 dark:border-slate-600 dark:hover:bg-slate-700";
}
