using GotX;

/// <summary>Lưới thẻ: grid responsive + col-span + transition khi hover (nhấc lên, bóng lớn dần).</summary>
public sealed class GalleryView : View
{
    static readonly (string Title, string Desc, string Color, string Icon)[] Items =
    [
        ("Flexbox & wrap", "Chia dòng tự động khi hết chỗ, gap theo hai trục.", "indigo", "F"),
        ("CSS Grid", "grid-cols-*, col-span-*, track fr/px/auto.", "emerald", "G"),
        ("Cuộn mượt", "overflow-y-auto, lăn chuột, thanh cuộn nổi.", "sky", "S"),
        ("Transition", "Màu, độ mờ, bóng, translate/scale với easing.", "amber", "T"),
        ("Animation", "animate-pulse, animate-ping, animate-bounce.", "rose", "A"),
        ("Cache view", "View bị dời khi cuộn được phát lại kèm tịnh tiến.", "violet", "C"),
        ("Tiếng Việt", "Unikey, EVKey, Telex của Windows, IME.", "teal", "V"),
        ("D3D11", "Một lệnh DrawInstanced cho cả khung hình.", "slate", "D"),
    ];

    // Chuỗi class được ghép sẵn một lần: chuỗi nội suy trong Render sẽ cấp phát mỗi lần render.
    static readonly string[] IconClass = Items.Select(it =>
        $"size-10 rounded-lg flex items-center justify-center font-bold bg-{it.Color}-100 text-{it.Color}-600 " +
        $"transition duration-300 group-hover:scale-110 dark:bg-{it.Color}-950 dark:text-{it.Color}-300").ToArray();

    public override Div Render(Window w)
    {
        var grid = w.Div().Class("grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4");
        for (int i = 0; i < Items.Length; i++)
        {
            var (title, desc, _, icon) = Items[i];
            grid.Child(
                w.Div().Class(i == 0 ? WideCard : Card)
                    .Child(w.Div().Class(IconClass[i]).Child(icon))
                    .Child(w.Div().Class("mt-3 font-semibold text-slate-900 dark:text-slate-100").Child(title))
                    .Child(w.Div().Class("mt-1 text-sm text-slate-500 dark:text-slate-400").Child(desc)));
        }
        return w.Div().Class("flex flex-col gap-3")
            .Child(w.Div().Class("text-lg font-semibold").Child("Thư viện thành phần"))
            .Child(grid);
    }

    const string WideCard = Card + " sm:col-span-2";

    const string Card =
        "group p-5 rounded-xl border cursor-pointer bg-white border-slate-200 shadow-xs " +
        "transition duration-200 ease-out hover:-translate-y-1 hover:shadow-lg hover:border-indigo-300 active:scale-[0.98] " +
        "dark:bg-slate-900 dark:border-slate-800 dark:hover:border-indigo-700";
}
