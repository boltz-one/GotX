using GotX;

/// <summary>Viền từng cạnh/gạch/chấm, outline, divide-y, xoay, skew, origin, cắt theo bo góc, animate-spin.</summary>
public sealed class EffectsView : View
{
    int _presses;
    string _label = "Đã nhấn 0 lần";

    void Press() { _presses++; _label = $"Đã nhấn {_presses} lần"; Notify(); }

    const string Card = "relative overflow-hidden flex flex-col gap-3 p-5 rounded-xl border shadow-md bg-white border-slate-200 dark:bg-slate-900 dark:border-slate-800";

    static Div Callout(Window w, string cls, string title, string body) =>
        w.Div().Class(cls)
            .Child(w.Div().Class("text-sm font-semibold").Child(title))
            .Child(w.Div().Class("text-xs opacity-80").Child(body));

    static Div Row(Window w, string name, string value) =>
        w.Div().Class("flex justify-between py-2.5 text-sm")
            .Child(w.Div().Class("text-slate-600 dark:text-slate-400").Child(name))
            .Child(w.Div().Class("font-semibold").Child(value));

    static Div Sticker(Window w, string cls, string text) =>
        w.Div().Class(cls).Child(text);

    public override Div Render(Window w) =>
        w.Div().Class("flex flex-col gap-3")
            .Child(w.Div().Class("text-lg font-semibold").Child("Hiệu ứng: xoay, skew, viền, outline, vùng cắt"))
            .Child(
                w.Div().Class("grid grid-cols-1 md:grid-cols-3 gap-5")
                    // 1. Callout: border-l-4 + màu riêng cạnh trái
                    .Child(w.Div().Class(Card)
                        .Child(w.Div().Class("text-sm font-semibold text-slate-500").Child("border-l-4 border-l-*"))
                        .Child(Callout(w, "pl-3 py-2 pr-2 rounded-r-md border-l-4 border-l-sky-500 bg-sky-50 text-sky-900 dark:bg-sky-950 dark:text-sky-100",
                            "Thông tin", "Viền trái dày, màu riêng, các cạnh khác không có viền."))
                        .Child(Callout(w, "pl-3 py-2 pr-2 rounded-r-md border-l-4 border-l-amber-500 bg-amber-50 text-amber-900 dark:bg-amber-950 dark:text-amber-100",
                            "Cảnh báo", "Góc bo chỉ ở bên phải: rounded-r-md."))
                        .Child(Callout(w, "pl-3 py-2 pr-2 rounded-md border border-rose-200 border-l-4 border-l-rose-500 bg-rose-50 text-rose-900 dark:bg-rose-950 dark:text-rose-100 dark:border-rose-900",
                            "Lỗi", "Bốn cạnh: 1px hồng nhạt + trái 4px hồng đậm, góc nối chéo.")))
                    // 2. divide-y + spinner (border-t màu khác + animate-spin)
                    .Child(w.Div().Class(Card)
                        .Child(w.Div().Class("text-sm font-semibold text-slate-500").Child("divide-y · animate-spin"))
                        .Child(w.Div().Class("divide-y divide-slate-200 dark:divide-slate-800")
                            .Child(Row(w, "Khung hình", "60 fps"))
                            .Child(Row(w, "Cấp phát / frame", "0 B"))
                            .Child(Row(w, "Lệnh vẽ GPU", "1 DrawInstanced"))
                            .Child(Row(w, "Package ngoài", "0")))
                        .Child(w.Div().Class("flex items-center gap-3 pt-1")
                            .Child(w.Div().Class("size-6 rounded-full border-[3px] border-slate-200 border-t-indigo-600 animate-spin dark:border-slate-700 dark:border-t-indigo-400"))
                            .Child(w.Div().Class("text-sm text-slate-500").Child("Đang đồng bộ…"))))
                    // 3. Xoay: sticker, ruy băng, phím bấm 3D
                    .Child(w.Div().Class(Card)
                        .Child(w.Div().Class("absolute top-4 -right-9 w-32 py-1 rotate-45 text-center text-xs font-bold text-white bg-rose-500 shadow-md").Child("MỚI"))
                        .Child(w.Div().Class("text-sm font-semibold text-slate-500").Child("rotate-* · transition"))
                        .Child(w.Div().Class("flex gap-3 py-2")
                            .Child(Sticker(w, StickerA, "GotX"))
                            .Child(Sticker(w, StickerB, ".NET"))
                            .Child(Sticker(w, StickerC, "AOT")))
                        .Child(w.Div().Class(KeyCap).Child("Nhấn thử").OnClick(this, static (v, _) => v.Press()))
                        .Child(w.Div().Class("text-xs text-slate-500").Child(_label)))
                    // 4. Viền gạch: vùng thả tệp
                    .Child(w.Div().Class(Card)
                        .Child(w.Div().Class("text-sm font-semibold text-slate-500").Child("border-dashed · border-dotted"))
                        .Child(w.Div().Class(DropZone)
                            .Child(w.Div().Class("size-10 rounded-full flex items-center justify-center text-lg bg-indigo-50 text-indigo-600 dark:bg-indigo-950 dark:text-indigo-300").Child("↑"))
                            .Child(w.Div().Class("text-sm font-medium").Child("Kéo thả tệp vào đây"))
                            .Child(w.Div().Class("text-xs text-slate-500").Child("PNG, JPG tối đa 10MB")))
                        .Child(w.Div().Class("border-t-2 border-dotted border-slate-300 dark:border-slate-700"))
                        .Child(w.Div().Class("text-xs text-slate-500").Child("Đường kẻ chấm: border-t-2 border-dotted")))
                    // 5. Cắt theo bo góc + skew + origin
                    .Child(w.Div().Class("rounded-xl overflow-hidden border shadow-md bg-white border-slate-200 dark:bg-slate-900 dark:border-slate-800")
                        .Child(w.Div().Class("h-20 bg-indigo-500 relative")
                            .Child(w.Div().Class("absolute -left-6 top-6 w-40 h-10 bg-indigo-400 -skew-y-6"))
                            .Child(w.Div().Class("absolute right-4 -bottom-6 size-16 rounded-full overflow-hidden ring-4 ring-white dark:ring-slate-900")
                                .Child(w.Div().Class("h-1/2 bg-amber-400"))
                                .Child(w.Div().Class("h-1/2 bg-rose-500"))))
                        .Child(w.Div().Class("flex flex-col gap-3 p-5 pt-8")
                            .Child(w.Div().Class("text-sm font-semibold text-slate-500").Child("overflow-hidden theo bo góc · skew · origin"))
                            .Child(w.Div().Class("flex gap-2")
                                .Child(w.Div().Class(Tag + " bg-emerald-500").Child(w.Div().Class("skew-x-12").Child("-skew-x-12")))
                                .Child(w.Div().Class(Tag + " bg-sky-500").Child(w.Div().Class("skew-x-12").Child("GotX"))))
                            .Child(w.Div().Class("flex justify-start pl-6")
                                .Child(w.Div().Class(SwingTag).Child("origin-top-left")))))
                    // 6. Outline: hover/active, offset, nét đứt
                    .Child(w.Div().Class(Card)
                        .Child(w.Div().Class("text-sm font-semibold text-slate-500").Child("outline-* · outline-offset-*"))
                        .Child(w.Div().Class("flex flex-col gap-3 pt-1")
                            .Child(w.Div().Class(OutlineBtn).Child("Rê chuột: outline-2 offset-2"))
                            .Child(w.Div().Class("px-4 py-2.5 rounded-lg text-sm text-center bg-amber-50 text-amber-900 outline-2 outline-dashed outline-offset-4 outline-amber-400 " +
                                                 "dark:bg-amber-950 dark:text-amber-100").Child("outline-dashed outline-offset-4"))
                            .Child(w.Div().Class("self-center size-12 rounded-full bg-rose-500 outline-4 outline-rose-200 outline-offset-0 animate-pulse dark:outline-rose-900")))));

    const string Tag = "-skew-x-12 px-3 py-1 text-xs font-bold text-white";
    const string SwingTag =
        "px-3 py-1.5 rounded-md text-xs font-semibold bg-amber-300 text-amber-950 shadow-md cursor-pointer " +
        "origin-top-left rotate-3 transition duration-300 hover:-rotate-6";
    const string DropZone =
        "flex flex-col items-center gap-1 py-5 rounded-xl border-2 border-dashed cursor-pointer transition-colors " +
        "border-slate-300 hover:border-indigo-400 hover:bg-indigo-50/50 dark:border-slate-700 dark:hover:bg-indigo-950/40";
    const string OutlineBtn =
        "px-4 py-2.5 rounded-lg text-sm font-semibold text-center cursor-pointer bg-white text-slate-800 border border-slate-300 " +
        "transition hover:outline-2 hover:outline-offset-2 hover:outline-indigo-500 active:outline-offset-0 " +
        "dark:bg-slate-800 dark:text-slate-100 dark:border-slate-600";

    const string StickerBase = "px-3 py-2 rounded-lg text-sm font-bold shadow-md cursor-pointer transition duration-300 ease-out hover:rotate-0 hover:scale-110 ";
    const string StickerA = StickerBase + "-rotate-6 bg-amber-300 text-amber-950";
    const string StickerB = StickerBase + "rotate-3 bg-sky-300 text-sky-950";
    const string StickerC = StickerBase + "-rotate-2 bg-emerald-300 text-emerald-950";

    // Phím bấm 3D: viền dưới dày màu đậm; khi nhấn, viền dưới biến mất và phím tụt xuống 4px
    const string KeyCap =
        "flex justify-center py-2.5 rounded-lg cursor-pointer text-sm font-semibold text-white bg-indigo-600 " +
        "border-b-4 border-b-indigo-800 transition duration-75 hover:bg-indigo-500 active:border-b-0 active:translate-y-1";
}
