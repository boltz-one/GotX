using GotX;
using GotX.Components;

/// <summary>
/// Style của trang chủ: khối demo dùng <c>@/styles/base-rhea/ui/*</c> (bảng <see cref="ComponentStyleSheet.Rhea"/>) trong
/// lớp <c>theme-neutral</c>; còn menu, danh sách chọn và tooltip được Radix/Base UI đưa ra portal ngoài lớp đó nên lấy màu
/// theme chung của site (chữ đen thay vì neutral-950, nền mục sáng #404040 ở dark mode, tooltip đen/trắng).
/// Mọi view của trang bọc thân Render và OnCreated trong <see cref="Use"/>; component có state nhớ bảng lúc được tạo.
/// </summary>
static class HomeStyle
{
    const string SitePopup = "bg-popover text-black shadow-lg ring-1 ring-foreground/5 dark:text-foreground dark:ring-foreground/10";
    const string MenuRow = "flex min-h-7 items-center gap-2 rounded-xl px-2 py-1.5 text-sm whitespace-nowrap ";
    const string SelectRow = "flex min-h-7 items-center justify-between gap-2 rounded-xl py-1.5 pl-2 pr-2 text-sm cursor-pointer";
    const string SiteAccent = " bg-accent text-accent-foreground dark:bg-[#404040]";

    public static readonly ComponentStyleSheet Sheet = (ComponentStyleSheet.Rhea with
    {
        MenuPanel = "flex flex-col min-w-32 rounded-2xl p-1 " + SitePopup,
        MenuRow =
        [
            MenuRow + "cursor-pointer",
            MenuRow + "cursor-pointer" + SiteAccent,
            MenuRow + "cursor-pointer text-destructive",
            MenuRow + "cursor-pointer text-destructive bg-destructive/10 dark:bg-destructive/20",
            MenuRow + "opacity-50",
        ],
        SelectList = "flex flex-col p-1 max-h-72 overflow-y-auto rounded-2xl " + SitePopup,
        SelectRow = [SelectRow, SelectRow + SiteAccent],
        TooltipPanel = "relative flex max-w-xs items-center gap-1.5 rounded-xl bg-black px-3 py-1.5 text-xs text-white dark:bg-foreground dark:text-background",
        TooltipArrow =
        [
            "absolute left-1/2 -top-1.5 size-2.5 -translate-x-1/2 rotate-45 rounded-[2px] bg-black dark:bg-foreground",
            "absolute left-1/2 -bottom-1.5 size-2.5 -translate-x-1/2 rotate-45 rounded-[2px] bg-black dark:bg-foreground",
            "absolute top-1/2 -left-1.5 size-2.5 -translate-y-1/2 rotate-45 rounded-[2px] bg-black dark:bg-foreground",
            "absolute top-1/2 -right-1.5 size-2.5 -translate-y-1/2 rotate-45 rounded-[2px] bg-black dark:bg-foreground",
        ],
        TooltipOffset = 10,
    }).Compile();

    /// <summary>Phạm vi style của trang: <c>using var _ = HomeStyle.Use();</c>.</summary>
    public static ComponentStyles.Scope Use() => ComponentStyles.Use(Sheet);

    /// <summary>Class nút của style trang – để dựng sẵn (static readonly) class cho phần tử trông như nút.</summary>
    public static string ButtonClasses(ButtonVariant variant, ButtonSize size = ButtonSize.Default)
    {
        using var _ = Use();
        return Ui.ButtonClasses(variant, size);
    }
}
