namespace GotX.Components;

/// <summary>Bảng class của <see cref="SidebarNav"/> và các hàm tĩnh dựng khung sidebar theo shadcn (ui/sidebar.tsx).</summary>
public sealed partial class SidebarNav
{
    const string Ring = "focus-visible:ring-2 focus-visible:ring-sidebar-ring ";
    const string Accent = "hover:bg-sidebar-accent hover:text-sidebar-accent-foreground active:bg-sidebar-accent active:text-sidebar-accent-foreground ";
    const string ActiveState = "bg-sidebar-accent text-sidebar-accent-foreground ";

    const string ContentExpanded = "relative flex min-h-0 flex-1 flex-col gap-2 overflow-y-auto";
    const string ContentCollapsed = "relative flex min-h-0 flex-1 flex-col gap-2 overflow-hidden";
    const string GroupClass = "relative flex w-full min-w-0 shrink-0 flex-col p-2";
    // Khung thu gọn rộng 3rem gồm cả viền phải 1px, nên chỉ còn 47px cho nút 32px + p-2 hai bên: bớt 1px lề phải
    // (bản gốc để nút tràn 1px ra ngoài thẻ li).
    const string GroupCollapsedClass = "relative flex w-full min-w-0 shrink-0 flex-col py-2 pl-2 pr-[7px]";
    const string GroupLabelClass = "flex h-8 shrink-0 items-center rounded-md px-2 text-xs font-medium text-sidebar-foreground/70";
    const string MenuClass = "flex w-full min-w-0 flex-col gap-1";
    const string LabelClass = "min-w-0 flex-1 truncate";
    const string SubListClass = "mx-3.5 flex min-w-0 translate-x-px flex-col gap-1 border-l border-sidebar-border px-2.5 py-0.5";
    const string SubBase = "flex h-7 min-w-0 -translate-x-px items-center gap-2 overflow-hidden rounded-md px-2 text-sm cursor-pointer " + Accent + Ring;
    const string SubClass = SubBase + "text-sidebar-foreground";
    const string SubActiveClass = SubBase + ActiveState;
    const string BadgeClass = "absolute top-1.5 right-1 flex h-5 min-w-5 items-center justify-center rounded-md px-1 text-xs font-medium text-sidebar-foreground";
    const string ChevronClosed = "flex shrink-0 transition-transform duration-200 rotate-0";
    const string ChevronOpen = "flex shrink-0 transition-transform duration-200 rotate-90";
    const string ActionBase = "absolute top-1.5 right-1 flex size-5 items-center justify-center rounded-md text-sidebar-foreground cursor-pointer " +
        "hover:bg-sidebar-accent hover:text-sidebar-accent-foreground " + Ring;
    const string ActionClass = ActionBase;
    // showOnHover: hiện khi rê vào hàng (group-hover) hoặc khi chính nó được focus bằng bàn phím.
    const string ActionHoverClass = ActionBase + "opacity-0 group-hover:opacity-100 focus-visible:opacity-100";

    const string ButtonBase = "flex items-center gap-2 overflow-hidden rounded-md p-2 text-left text-sm cursor-pointer " + Accent + Ring;

    // Chỉ số = thu gọn·16 + đang chọn·8 + có nút phụ·4 + đậm·2 + mờ: tra bảng, không nối chuỗi mỗi frame.
    static readonly string[] ButtonClasses = BuildButtonClasses();
    static readonly string[] LargeClasses = BuildLargeClasses();

    static string[] BuildButtonClasses()
    {
        var table = new string[32];
        for (int i = 0; i < table.Length; i++)
            table[i] = ButtonBase
                + ((i & 8) != 0 ? ActiveState + "font-medium " : (i & 2) != 0 ? "font-medium " : "")
                + ((i & 1) != 0 && (i & 8) == 0 ? "text-sidebar-foreground/70 " : "")
                + ((i & 16) != 0 ? "size-8" : (i & 4) != 0 ? "h-8 w-full pr-8" : "h-8 w-full");
        return table;
    }

    static string ButtonClass(bool collapsed, bool active, bool action, SidebarNavItem item) =>
        ButtonClasses[(collapsed ? 16 : 0) + (active ? 8 : 0) + (action ? 4 : 0) + (item.Emphasis ? 2 : 0) + (item.Muted ? 1 : 0)];

    static string[] BuildLargeClasses()
    {
        const string large = "flex items-center gap-2 overflow-hidden rounded-md text-left text-sm cursor-pointer " + Accent + Ring;
        string[] size = ["h-12 w-full p-2", "size-8"];
        var table = new string[4];
        for (int i = 0; i < 4; i++) table[i] = large + ((i & 1) != 0 ? ActiveState : "") + size[i >> 1];
        return table;
    }

    /// <summary>
    /// Class của SidebarMenuButton <c>size="lg"</c> (hàng chọn team / tài khoản): cao 48px, thu gọn còn ô 32px;
    /// <paramref name="open"/> giữ nền sáng khi menu của nó đang mở. Rộng <c>w-full</c>: khi đặt làm
    /// <c>DropdownMenu.Trigger</c> (lớp bọc co theo nội dung) hãy nối thêm bề rộng cố định, vd. <c>.Class("w-60")</c>.
    /// </summary>
    public static string LargeButtonClass(bool collapsed = false, bool open = false) => LargeClasses[(collapsed ? 2 : 0) + (open ? 1 : 0)];

    /// <summary>Hàng ngoài cùng (SidebarProvider): sidebar và vùng nội dung đứng cạnh nhau, lấp đầy cha.</summary>
    public static Div Wrapper(Window w, SidebarVariant variant = SidebarVariant.Sidebar) =>
        w.Div().Class(variant == SidebarVariant.Inset ? "flex size-full bg-sidebar" : "flex size-full");

    /// <summary>
    /// Cột sidebar: w-64 (16rem), thu gọn w-12 (3rem). Kiểu Sidebar có viền phải; kiểu Inset không viền và lùi vào p-2
    /// (bề rộng phần trong còn 15rem). Chế độ offcanvas của shadcn = không vẽ khung này.
    /// </summary>
    public static Div Frame(Window w, bool collapsed = false, SidebarVariant variant = SidebarVariant.Sidebar) =>
        w.Div().Class(FrameClasses[(variant == SidebarVariant.Inset ? 2 : 0) + (collapsed ? 1 : 0)]);

    const string FrameBase = "flex h-full shrink-0 flex-col overflow-hidden bg-sidebar text-sidebar-foreground ";
    static readonly string[] FrameClasses =
    [
        FrameBase + "w-64 border-r border-sidebar-border", FrameBase + "w-12 border-r border-sidebar-border",
        FrameBase + "w-64 p-2", FrameBase + "w-16 p-2",
    ];

    // Như GroupCollapsedClass: khi thu gọn bớt 1px lề phải để ô 32px không bị viền phải của khung ép còn 31px.
    const string EdgeClass = "flex shrink-0 flex-col gap-2 p-2", EdgeCollapsedClass = "flex shrink-0 flex-col gap-2 py-2 pl-2 pr-[7px]";

    /// <summary>SidebarHeader: phần đầu khung (logo, chọn team).</summary>
    public static Div Header(Window w, bool collapsed = false) => w.Div().Class(collapsed ? EdgeCollapsedClass : EdgeClass);

    /// <summary>SidebarFooter: phần chân khung (tài khoản).</summary>
    public static Div Footer(Window w, bool collapsed = false) => w.Div().Class(collapsed ? EdgeCollapsedClass : EdgeClass);

    /// <summary>SidebarContent: nhúng <paramref name="nav"/> vào khung, chiếm phần còn lại và tự cuộn.</summary>
    public static Div Content(Window w, SidebarNav nav) => w.View(nav).Class("flex min-h-0 flex-1 flex-col");

    /// <summary>SidebarSeparator: đường kẻ ngang giữa các phần.</summary>
    public static Div Separator(Window w) => w.Div().Class("mx-2 h-px shrink-0 bg-sidebar-border");

    /// <summary>SidebarInset: vùng nội dung cạnh sidebar. Kiểu Inset là thẻ bo góc có bóng, cách mép 0.5rem.</summary>
    public static Div Inset(Window w, SidebarVariant variant = SidebarVariant.Sidebar) =>
        w.Div().Class(variant == SidebarVariant.Inset
            ? "relative m-2 ml-0 flex min-w-0 flex-1 flex-col overflow-hidden rounded-xl bg-background shadow-sm"
            : "relative flex min-w-0 flex-1 flex-col bg-background");

    /// <summary>
    /// SidebarTrigger: nút ghost 28px có icon panel-left, là điểm dừng Tab (Enter/Space = bấm) – thay cho Ctrl+B của
    /// shadcn vì Core chưa có phím tắt toàn cục. Nơi dùng nối <c>.Key(...).OnClick(...)</c>.
    /// </summary>
    public static Div Trigger(Window w) => Ui.Button(w, ButtonVariant.Ghost, ButtonSize.IconSm).Child(Icon.PanelLeft(w));

    /// <summary>Icon thay thế khi thu gọn mà nút không có icon: chữ cái đầu trong ô viền 16px.</summary>
    static Div InitialIcon(Window w, string initial) =>
        w.Div().Class("flex size-4 shrink-0 items-center justify-center rounded-[4px] border border-sidebar-foreground/50 text-[9px] font-semibold leading-none")
            .Child(initial);
}
