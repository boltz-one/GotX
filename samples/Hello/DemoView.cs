using GotX;
using GotX.Widgets;

/// <summary>
/// View gốc: thanh tiêu đề cố định + vùng trang cuộn (overflow-y-auto) chứa các view con.
/// Khi cuộn trang, view con không đổi được phát lại kèm tịnh tiến thay vì gọi Render().
/// </summary>
public sealed class DemoView : View
{
    CounterView _counter = null!;
    ShowcaseView _showcase = null!;
    GalleryView _gallery = null!;
    InboxView _inbox = null!;
    WidgetsView _widgets = null!;
    EffectsView _effects = null!;
    TextInput _note = null!;
    string _stats = "0 ký tự";

    const string Paragraph =
        "GotX dựng lại cây element mỗi frame, nhưng từng view được cache: " +
        "khi bạn gõ vào ô ghi chú, chỉ những view bị Notify mới gọi Render(), các view khác được " +
        "chép nguyên kết quả vẽ từ frame trước. Chuỗi class Tailwind được biên dịch một lần rồi cache.";

    protected override void OnCreated()
    {
        _counter = App.New(new CounterView());
        _showcase = App.New(new ShowcaseView());
        _gallery = App.New(new GalleryView());
        _inbox = App.New(new InboxView());
        _widgets = App.New(new WidgetsView());
        _effects = App.New(new EffectsView());
        _note = App.New(new TextInput { Placeholder = "Gõ tiếng Việt ở đây – Unikey, EVKey hoặc bộ gõ Telex của Windows…" });
        App.Subscribe<TextInput.TextChanged>(_note, e => { _stats = $"{e.Text.Length} ký tự"; Notify(); });
    }

    void ToggleDark()
    {
        foreach (var w in App.Windows) w.DarkMode = !w.DarkMode;
    }

    public override Div Render(Window w) =>
        w.Div().Class("size-full flex flex-col bg-slate-100 text-slate-900 dark:bg-slate-950 dark:text-slate-100")
            .Child(
                w.Div().Class("flex items-center justify-between px-6 py-4 shadow-xs bg-white dark:bg-slate-900")
                    .Child(w.Div().Class("text-xl font-bold").Child("GotX · Tailwind trên .NET"))
                    .Child(
                        w.Div().Class("flex items-center gap-2")
                            .Child(w.Div().Class("px-3 py-1 rounded-full text-xs font-semibold bg-indigo-100 text-indigo-700 dark:bg-indigo-950 dark:text-indigo-300")
                                .Child("thuần .NET"))
                            .Child(w.Div().Class("px-3 py-1.5 rounded-lg text-xs font-semibold cursor-pointer border transition-colors bg-white border-slate-300 hover:bg-slate-100 " +
                                                 "active:scale-95 dark:bg-slate-800 dark:border-slate-600 dark:hover:bg-slate-700")
                                .Child(w.DarkMode ? "☀ Sáng" : "☾ Tối").OnClick(this, static (v, _) => v.ToggleDark()))))
            .Child(
                w.Div().Class("flex-1 overflow-y-auto").Key("page")
                    .Child(
                        w.Div().Class("flex flex-col gap-6 p-6")
                            .Child(
                                w.Div().Class("flex flex-col md:flex-row gap-5")
                                    .Child(w.View(_counter).Class("md:w-64"))
                                    .Child(
                                        w.Div().Class("flex-1 flex flex-col gap-3 p-6 rounded-xl border shadow-md bg-white border-slate-200 dark:bg-slate-900 dark:border-slate-800")
                                            .Child(w.Div().Class("text-lg font-semibold").Child("Ghi chú"))
                                            .Child(w.View(_note))
                                            .Child(w.Div().Class("text-xs text-right text-slate-500").Child(_stats))
                                            .Child(w.Div().Class("mt-2 text-sm/6 text-slate-600 dark:text-slate-400").Child(Paragraph)))
                                    .Child(w.View(_showcase).Class("md:w-80")))
                            .Child(w.View(_gallery))
                            .Child(w.View(_effects))
                            .Child(
                                w.Div().Class("grid grid-cols-1 lg:grid-cols-[3fr_2fr] gap-5")
                                    .Child(w.View(_inbox))
                                    .Child(w.View(_widgets)))
                            .Child(w.Div().Class("text-xs text-center text-slate-400 dark:text-slate-600")
                                .Child("Lăn chuột để cuộn · Esc: bỏ focus · Shift+mũi tên: chọn · Ctrl+A: chọn hết"))));
}
