using GotX;

/// <summary>Tag tự xuống dòng (flex-wrap), khung chờ (animate-pulse), chấm thông báo (animate-ping).</summary>
public sealed class WidgetsView : View
{
    static readonly string[] Tags =
    [
        "C#", ".NET 11", "Native AOT", "Win32", "D3D11", "Metal (sắp có)", "Tailwind v4", "Flexbox", "Grid",
        "Transition", "Unikey", "Telex", "Cache view", "Zero-alloc", "SDF", "HarfBuzz? Không cần",
    ];

    public override Div Render(Window w)
    {
        var tags = w.Div().Class("flex flex-wrap gap-2");
        foreach (var t in Tags)
            tags.Child(w.Div().Class("px-2.5 py-1 rounded-md text-xs font-medium bg-slate-100 text-slate-700 ring-1 ring-slate-200 " +
                                     "transition-colors hover:bg-indigo-600 hover:text-white hover:ring-indigo-600 " +
                                     "dark:bg-slate-800 dark:text-slate-300 dark:ring-slate-700").Child(t));

        return w.Div().Class("flex flex-col gap-4 p-5 rounded-xl border shadow-md bg-white border-slate-200 dark:bg-slate-900 dark:border-slate-800")
            .Child(w.Div().Class("flex items-center gap-2")
                .Child(w.Div().Class("text-lg font-semibold").Child("Tag (flex-wrap)"))
                .Child(w.Div().Class("relative size-2.5")
                    .Child(w.Div().Class("absolute inset-0 rounded-full bg-emerald-400 animate-ping"))
                    .Child(w.Div().Class("relative size-2.5 rounded-full bg-emerald-500"))))
            .Child(tags)
            .Child(w.Div().Class("text-sm font-semibold text-slate-500").Child("Đang tải (animate-pulse)"))
            .Child(w.Div().Class("flex gap-3 animate-pulse")
                .Child(w.Div().Class("size-10 rounded-full bg-slate-200 dark:bg-slate-700"))
                .Child(w.Div().Class("flex-1 flex flex-col gap-2 pt-1")
                    .Child(w.Div().Class("h-2.5 w-3/4 rounded bg-slate-200 dark:bg-slate-700"))
                    .Child(w.Div().Class("h-2.5 w-1/2 rounded bg-slate-200 dark:bg-slate-700"))))
            .Child(w.Div().Class("flex justify-center pt-2")
                .Child(w.Div().Class("size-8 rounded-full bg-indigo-500 animate-bounce")));
    }
}
