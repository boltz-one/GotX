using GotX;
using GotX.Components;

/// <summary>View gốc của gallery: cột điều hướng bên trái, trang đang chọn cuộn bên phải.</summary>
public sealed class GalleryView : View
{
    static readonly string[] Names = ["Hiển thị", "Biểu mẫu", "Overlay", "Điều hướng & dữ liệu", "Chat", "Dữ liệu"];
    View[] _pages = [];
    Indexed<GalleryView>[]? _refs;
    int _current;

    protected override void OnCreated() => _pages =
    [
        App.New(new DisplayPage()), App.New(new FormsPage()), App.New(new OverlaysPage()), App.New(new NavigationPage()), App.New(new ChatPage()), App.New(new DataPage()),
    ];

    void Show(int index) { _current = index; Notify(); }

    void ToggleDark()
    {
        foreach (var w in App.Windows) w.DarkMode = !w.DarkMode;
    }

    public override Div Render(Window w)
    {
        var refs = Indexed<GalleryView>.Ensure(ref _refs, this, Names.Length);
        var nav = w.Div().Class("flex flex-col gap-1 w-56 shrink-0 p-3 border-r border-border bg-sidebar text-sidebar-foreground")
            .Child(w.Div().Class("px-2 py-2 text-sm font-semibold").Child("GotX.Components"));
        for (int i = 0; i < Names.Length; i++)
            nav.Child(w.Div().Class(i == _current
                    ? "px-2 py-1.5 rounded-md text-sm cursor-pointer bg-sidebar-accent text-sidebar-accent-foreground font-medium"
                    : "px-2 py-1.5 rounded-md text-sm cursor-pointer hover:bg-sidebar-accent hover:text-sidebar-accent-foreground")
                .Child(Names[i]).OnClick(refs[i], static (r, _) => r.Owner.Show(r.Index)));
        nav.Child(w.Div().Class("flex-1"))
            .Child(Ui.Button(w, w.DarkMode ? "Chế độ sáng" : "Chế độ tối", ButtonVariant.Outline, ButtonSize.Sm)
                .OnClick(this, static (v, _) => v.ToggleDark()));

        return w.Div().Class("size-full flex bg-background text-foreground")
            .Child(nav)
            .Child(w.Div().Class("flex-1 overflow-y-auto").Key("gallery-page").Child(w.View(_pages[_current])));
    }
}