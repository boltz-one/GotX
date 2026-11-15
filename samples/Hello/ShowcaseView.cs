using GotX;

/// <summary>Trình diễn các nhóm utility: ring/offset, absolute, group-hover, active, disabled, truncate, arbitrary.</summary>
public sealed class ShowcaseView : View
{
    int _clicks;
    string _status = "Chưa bấm";

    void Clicked() { _clicks++; _status = $"Đã bấm {_clicks} lần"; Notify(); }

    const string AvatarBase = "relative size-12 rounded-full flex items-center justify-center text-white font-semibold ring-2 ring-white dark:ring-slate-900 ";
    const string DotBase = "absolute -right-0.5 -bottom-0.5 size-3.5 rounded-full ring-2 ring-white dark:ring-slate-900 ";

    static Div Avatar(Window w, string initial, string color, string status) =>
        w.Div().Class(color).Child(initial).Child(w.Div().Class(status));

    public override Div Render(Window w) =>
        w.Div().Class("flex flex-col gap-4 p-6 h-full rounded-xl border shadow-md bg-white border-slate-200 dark:bg-slate-900 dark:border-slate-800")
            .Child(w.Div().Class("text-lg font-semibold text-slate-900 dark:text-white").Child("Tailwind showcase"))
            .Child(
                w.Div().Class("flex gap-3")
                    .Child(Avatar(w, "K", AvatarBase + "bg-indigo-500", DotBase + "bg-emerald-500"))
                    .Child(Avatar(w, "H", AvatarBase + "bg-rose-500", DotBase + "bg-amber-400"))
                    .Child(Avatar(w, "N", AvatarBase + "bg-sky-500", DotBase + "bg-slate-400")))
            .Child(
                w.Div().Class("group p-4 rounded-lg border cursor-pointer border-slate-200 hover:border-indigo-300 hover:bg-indigo-50 " +
                              "dark:border-slate-700 dark:hover:bg-slate-800")
                    .Child(w.Div().Class("font-semibold text-slate-900 group-hover:text-indigo-700 dark:text-slate-100 dark:group-hover:text-indigo-300")
                        .Child("Thẻ group-hover"))
                    .Child(w.Div().Class("text-sm text-slate-500 truncate")
                        .Child("Rê chuột vào thẻ: tiêu đề đổi màu theo cha. Dòng mô tả dài này bị cắt bằng truncate…")))
            .Child(
                w.Div().Class("flex gap-2")
                    .Child(w.Div().Class("px-2.5 py-1 rounded-full text-xs font-medium bg-indigo-50 text-indigo-700 ring-1 ring-indigo-200").Child("ring-1"))
                    .Child(w.Div().Class("px-2.5 py-1 rounded-full text-xs font-medium bg-white text-slate-700 ring-2 ring-offset-2 ring-indigo-500").Child("ring-offset"))
                    .Child(w.Div().Class("px-[10px] py-1 rounded-[10px] text-[12px] bg-[#0ea5e9]/15 text-[#0369a1]").Child("[arbitrary]")))
            .Child(w.Div().Class("p-3 rounded-lg bg-white text-sm text-slate-600 shadow-[0_8px_24px_rgba(79,70,229,0.25)] dark:bg-slate-800 dark:text-slate-300")
                .Child("shadow-[0_8px_24px_rgba(79,70,229,0.25)]"))
            .Child(
                w.Div().Class("flex gap-2")
                    .Child(w.Div().Class(CounterView.Primary).Child("Bấm thử").OnClick(this, static (v, _) => v.Clicked()))
                    .Child(w.Div().Class(CounterView.Primary + " disabled:opacity-50 disabled:cursor-default disabled:shadow-none")
                        .Disabled().Child("Disabled")))
            .Child(w.Div().Class("text-xs text-slate-500 dark:text-slate-400").Child(_status));
}
