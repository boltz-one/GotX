namespace GotX.Components;

/// <summary>Phần trình bày của <see cref="Sidebar"/>: bảng class, nút menu, nút trigger.</summary>
public sealed partial class Sidebar
{
    // w-64 = --sidebar-width 16rem, w-12 = --sidebar-width-icon 3rem.
    const string RootBase = "flex h-full shrink-0 flex-col overflow-hidden border-r border-sidebar-border bg-sidebar text-sidebar-foreground ";
    const string RootExpanded = RootBase + "w-64";
    const string RootCollapsed = RootBase + "w-12";
    const string GroupLabelClass = "flex h-8 shrink-0 items-center rounded-md px-2 text-xs font-medium text-sidebar-foreground/70";

    const string ButtonBase =
        "flex items-center gap-2 overflow-hidden rounded-md p-2 text-left text-sm cursor-pointer transition-colors " +
        "hover:bg-sidebar-accent hover:text-sidebar-accent-foreground active:bg-sidebar-accent active:text-sidebar-accent-foreground " +
        "focus-visible:ring-2 focus-visible:ring-sidebar-ring ";
    const string ActiveState = "bg-sidebar-accent font-medium text-sidebar-accent-foreground ";

    // [thu gọn * 2 + đang chọn]: thu gọn thì nút vuông size-8 chỉ còn icon.
    static readonly string[] ButtonClasses =
    [
        ButtonBase + "w-full h-8",
        ButtonBase + ActiveState + "w-full h-8",
        ButtonBase + "size-8",
        ButtonBase + ActiveState + "size-8",
    ];

    Div MenuButton(Window w, SidebarItem item, bool active)
    {
        var button = w.Div().Class(ButtonClasses[(_collapsed ? 2 : 0) + (active ? 1 : 0)]);
        button.Child(item.Icon is { } icon ? icon(w) : InitialIcon(w, item.Initial));
        if (!_collapsed) button.Child(w.Div().Class("truncate").Child(item.Label));
        return button;
    }

    /// <summary>Icon thay thế: chữ cái đầu trong ô viền 16px, lấy màu chữ của nút.</summary>
    static Div InitialIcon(Window w, string initial) =>
        w.Div().Class("size-4 shrink-0 flex items-center justify-center rounded-[4px] border text-[9px] font-semibold leading-none")
            .Child(initial);

    /// <summary>
    /// Nút thu gọn/mở rộng (icon panel-left), đặt ở bất kỳ đâu – thường ở đầu vùng nội dung bên cạnh sidebar.
    /// </summary>
    public Div Trigger(Window w) =>
        Ui.Button(w, ButtonVariant.Ghost, ButtonSize.IconSm).Class(FocusRing.Ring)
            .Key(_triggerKey ??= Key + "-trigger").Focusable().AriaLabel("Toggle sidebar")
            .OnClick(this, static (s, _) => s.Toggle())
            .Child(Icon.PanelLeft(w));

    string? _triggerKey;

    /// <summary>Đường kẻ ngang giữa các phần của sidebar.</summary>
    public static Div Separator(Window w) => w.Div().Class("mx-2 h-px bg-sidebar-border");

    /// <summary>Vùng nội dung chính đặt cạnh sidebar (SidebarInset): nền background, giãn hết phần còn lại.</summary>
    public static Div Inset(Window w) => w.Div().Class("relative flex min-w-0 flex-1 flex-col bg-background");
}
