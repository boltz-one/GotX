using GotX;
using GotX.Components;

/// <summary>
/// Khung site: thanh đầu trang (logo, điều hướng Home / Blocks, đổi theme), hàng chọn block khi đang ở Blocks,
/// và vùng nội dung của trang đang mở. View của mỗi trang được tạo khi mở lần đầu rồi giữ lại (giữ state, vị trí cuộn).
/// </summary>
public sealed class SiteShell : View
{
    readonly Dictionary<string, View> _views = new();
    Indexed<SiteShell>[]? _blockRefs;
    string _current = HomePage.Page.Id;

    /// <summary>Id trang đang mở (Home hoặc id của một block).</summary>
    public string Current => _current;

    public void Open(string id)
    {
        if (_current == id) return;
        _current = id;
        Notify();
    }

    bool OnHome => _current == HomePage.Page.Id;

    SitePage CurrentPage
    {
        get
        {
            foreach (var b in BlockCatalog.All) if (b.Id == _current) return b;
            return HomePage.Page;
        }
    }

    View ViewOf(SitePage page)
    {
        if (!_views.TryGetValue(page.Id, out var view)) _views[page.Id] = view = page.Create(App);
        return view;
    }

    void ToggleDark()
    {
        foreach (var w in App.Windows) w.DarkMode = !w.DarkMode;
        Notify();
    }

    const string NavOn = "px-2.5 py-1.5 rounded-md text-sm font-medium cursor-pointer bg-accent text-accent-foreground focus-visible:ring-[3px] focus-visible:ring-ring/50";
    const string NavOff = "px-2.5 py-1.5 rounded-md text-sm font-medium cursor-pointer text-muted-foreground hover:bg-accent hover:text-accent-foreground focus-visible:ring-[3px] focus-visible:ring-ring/50";

    public override Div Render(Window w)
    {
        var header = w.Div().Class("flex h-14 shrink-0 items-center gap-2 border-b border-border px-6").Key("site-header")
            .Child(w.Div().Class("mr-4 text-sm font-semibold").Child("shadcn/ui · GotX"))
            .Child(w.Div().Key("nav-home").Focusable().Class(OnHome ? NavOn : NavOff).Child("Home")
                .OnClick(this, static (s, _) => s.Open(HomePage.Page.Id)))
            .Child(w.Div().Key("nav-blocks").Focusable().Class(OnHome ? NavOff : NavOn).Child("Blocks")
                .OnClick(this, static (s, _) => { if (s.OnHome && BlockCatalog.All.Length > 0) s.Open(BlockCatalog.All[0].Id); }))
            .Child(w.Div().Class("flex-1"))
            .Child(Ui.Button(w, w.DarkMode ? "Light" : "Dark", ButtonVariant.Ghost, ButtonSize.Sm).Key("toggle-theme")
                .OnClick(this, static (s, _) => s.ToggleDark()));

        var root = w.Div().Class("size-full flex flex-col bg-background text-foreground").Child(header);
        if (!OnHome)
        {
            var blocks = BlockCatalog.All;
            var refs = Indexed<SiteShell>.Ensure(ref _blockRefs, this, blocks.Length);
            var bar = w.Div().Class("flex h-11 shrink-0 items-center gap-1 border-b border-border px-6").Key("block-bar");
            for (int i = 0; i < blocks.Length; i++)
                bar.Child(w.Div().Key("block-" + blocks[i].Id).Focusable().Class(blocks[i].Id == _current ? NavOn : NavOff).Child(blocks[i].Title)
                    .OnClick(refs[i], static (r, _) => r.Owner.Open(BlockCatalog.All[r.Index].Id)));
            root.Child(bar);
        }
        var page = CurrentPage;
        return root.Child(w.Div().Key("site-page-" + page.Id)
            .Class(page.FullBleed ? "flex-1 min-h-0 overflow-hidden" : "flex-1 min-h-0 overflow-y-auto")
            // Trang full-bleed (dashboard, sidebar, login) phải nhận đúng chiều cao vùng nội dung: node view mặc định cao theo
            // nội dung, nên size-full của gốc block sẽ quy về chiều cao nội dung thay vì khung nhìn.
            .Child(page.FullBleed ? w.View(ViewOf(page)).Class("size-full") : w.View(ViewOf(page))));
    }
}