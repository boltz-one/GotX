using GotX;
using GotX.Components;

/// <summary>
/// Block sidebar-03 của shadcn/ui – "A sidebar with submenus": logo + "Documentation / v1.0.0" ở đầu, các nút cha
/// chữ đậm kèm danh sách con luôn mở (SidebarMenuSub), vùng inset có trigger, breadcrumb và nội dung giữ chỗ.
/// Chế độ thu gọn của bản gốc là offcanvas: nút trigger ẩn / hiện cả sidebar (không có hiệu ứng trượt, bỏ SidebarRail).
/// </summary>
public sealed class Sidebar03Block : View
{
    public static readonly SitePage Page = new("sidebar-03", "Sidebar 03", static app => app.New(new Sidebar03Block()), FullBleed: true, Style: ComponentStyle.NewYorkV4);

    public const string TriggerKey = "sidebar-03-trigger", SidebarKey = "sidebar-03-sidebar", ContentKey = "sidebar-03-content";
    public const string BrandKey = "sidebar-03-brand";

    SidebarNav _nav = null!;
    bool _open = true;

    public SidebarNav Nav => _nav;

    /// <summary>Sidebar đang hiện (offcanvas: đóng thì biến mất hẳn, vùng inset chiếm hết bề ngang).</summary>
    public bool Open
    {
        get => _open;
        set { if (_open == value) return; _open = value; Notify(); }
    }

    protected override void OnCreated()
    {
        // Chỉ "Data Fetching" mang isActive; các mục khác là liên kết href="#": bấm không đổi mục tô (Selectable = false).
        _nav = App.New(new SidebarNav { Groups = Sidebar03Data.Groups, Selectable = false, KeyPrefix = "nav" });
        _nav.SetActive(Sidebar03Data.ActiveGroup, Sidebar03Data.ActiveItem, Sidebar03Data.ActiveSub);
    }

    Div Sidebar(Window w) =>
        SidebarNav.Frame(w).Key(SidebarKey)
            .Child(SidebarNav.Header(w)
                // Bản gốc là thẻ <a href="#">: một nút focus được, bấm không làm gì.
                .Child(w.Div().Key(BrandKey).Focusable().Class(SidebarNav.LargeButtonClass())
                    .Child(SidebarBlockParts.LogoTile(w, SidebarIcons.GalleryVerticalEnd))
                    .Child(w.Div().Class("flex flex-col gap-0.5 leading-none")
                        .Child(w.Div().Class("font-medium").Child("Documentation"))
                        .Child(w.Div().Child("v1.0.0")))))
            .Child(SidebarNav.Content(w, _nav));

    public override Div Render(Window w)
    {
        var header = w.Div().Class("flex h-16 shrink-0 items-center gap-2 border-b border-border")
            .Child(w.Div().Class("flex items-center gap-2 px-3")
                .Child(SidebarBlockParts.Trigger(w).Key(TriggerKey).OnClick(this, static (b, _) => b.Open = !b.Open))
                .Child(SidebarBlockParts.HeaderSeparator(w))
                .Child(SidebarBlockParts.Breadcrumb(w)));
        var root = SidebarNav.Wrapper(w);
        if (_open) root.Child(Sidebar(w));
        return root.Child(SidebarNav.Inset(w).Child(header).Child(SidebarBlockParts.Placeholder(w, ContentKey, padTop: true)));
    }
}
