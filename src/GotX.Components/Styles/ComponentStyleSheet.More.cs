namespace GotX.Components;

// Giá trị mặc định của Avatar, Skeleton, Toggle/ToggleGroup, Sheet/Drawer, Dialog (chép nguyên văn từ bản trước khi có style).
public sealed partial record ComponentStyleSheet
{
    // ── Avatar ── chỉ số theo AvatarSize (Default, Sm, Lg).
    public string[] AvatarRoot { get; init; } =
        ["relative flex size-8 shrink-0 rounded-full", "relative flex size-6 shrink-0 rounded-full", "relative flex size-10 shrink-0 rounded-full"];
    public string[] AvatarFallback { get; init; } =
    [
        "flex size-full items-center justify-center rounded-full bg-muted text-sm text-muted-foreground",
        "flex size-full items-center justify-center rounded-full bg-muted text-xs text-muted-foreground",
        "flex size-full items-center justify-center rounded-full bg-muted text-sm text-muted-foreground",
    ];
    /// <summary>Lớp viền phủ trên avatar (after:border của style mặc định); "" = không có.</summary>
    public string AvatarRing { get; init; } = "absolute inset-0 rounded-full border border-border";
    public string AvatarImage { get; init; } = "size-full rounded-full object-cover";

    // ── Skeleton ──
    public string Skeleton { get; init; } = "animate-pulse rounded-md bg-muted";

    // ── Toggle / ToggleGroup ── bảng ghép [(variant * 3 + size) * 2 + bật] * 4 + vị trí (đứng riêng, đầu, giữa, cuối).
    public string ToggleBase { get; init; } =
        "inline-flex shrink-0 items-center justify-center gap-1 text-sm font-medium whitespace-nowrap cursor-pointer " +
        "transition-all hover:bg-muted hover:text-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 " +
        "disabled:opacity-50";
    public string[] ToggleVariants { get; init; } = ["bg-transparent", "border-input bg-transparent"];
    public string[] ToggleSizes { get; init; } = ["h-8 min-w-8 px-2.5", "h-7 min-w-7 px-2.5 text-[0.8rem]", "h-9 min-w-9 px-2.5"];
    /// <summary>Viền của Outline theo vị trí (nút sau bỏ viền trái để dính nhau).</summary>
    public string[] ToggleOutlineBorders { get; init; } = ["border", "border", "border-y border-r", "border-y border-r"];
    /// <summary>Bo góc theo vị trí, [cỡ Default/Lg, cỡ Sm].</summary>
    public string[][] ToggleRadii { get; init; } = [["rounded-lg", "rounded-l-lg", "", "rounded-r-lg"], ["rounded-md", "rounded-l-md", "", "rounded-r-md"]];
    public string TogglePressed { get; init; } = "bg-muted";
    public string ToggleGroup { get; init; } = "inline-flex items-center rounded-lg";
    /// <summary>Class thêm cho nút trong ToggleGroup ("" = như Toggle đứng riêng).</summary>
    public string ToggleGroupItem { get; init; } = "";

    // ── Sheet / Drawer (SidePanel) ── tấm theo PanelSide (Right, Left, Top, Bottom).
    public string[] SheetPanel { get; init; } =
    [
        "relative flex flex-col gap-4 h-full w-96 max-w-full bg-popover text-sm text-popover-foreground shadow-lg border-l border-border",
        "relative flex flex-col gap-4 h-full w-96 max-w-full bg-popover text-sm text-popover-foreground shadow-lg border-r border-border",
        "relative flex flex-col gap-4 w-full bg-popover text-sm text-popover-foreground shadow-lg border-b border-border",
        "relative flex flex-col gap-4 w-full bg-popover text-sm text-popover-foreground shadow-lg border-t border-border",
    ];
    public string SheetHeader { get; init; } = "flex flex-col gap-0.5 p-4";
    public string SheetFooter { get; init; } = "flex flex-col gap-2 p-4";
    public string SheetClosePosition { get; init; } = "absolute top-3 right-3";
    public string PanelTitle { get; init; } = "text-base font-medium text-foreground";
    public string PanelDescription { get; init; } = "text-sm text-muted-foreground";

    // ── Dialog ──
    public string DialogPanel { get; init; } =
        "relative flex flex-col gap-4 w-96 max-w-full rounded-xl bg-popover p-4 text-sm text-popover-foreground ring-1 ring-foreground/10 shadow-lg";
    public string DialogHeader { get; init; } = "flex flex-col gap-2";
    public string DialogTitle { get; init; } = "text-base font-medium leading-none";
    public string DialogDescription { get; init; } = "text-sm text-muted-foreground";
    public string DialogFooter { get; init; } = "flex justify-end gap-2";
    public string DialogClosePosition { get; init; } = "absolute top-2 right-2";
    /// <summary>Class của nút × ở góc Dialog/Sheet; "" = nút ghost icon-sm của bảng nút (mặc định).</summary>
    public string OverlayClose { get; init; } = "";

    // ── Accordion ──
    public string AccordionTrigger { get; init; } =
        "flex items-start justify-between gap-4 rounded-lg border border-transparent py-2.5 text-left text-sm font-medium cursor-pointer " +
        "transition-all hover:underline focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50";
    /// <summary>[đóng, mở].</summary>
    public string[] AccordionChevron { get; init; } =
        ["text-muted-foreground transition-transform duration-200", "text-muted-foreground transition-transform duration-200 rotate-180"];
    public string AccordionItemDivider { get; init; } = "border-b border-border";
    public string AccordionContent { get; init; } = "pb-2.5 text-sm";

    // ── Popover ──
    public string PopoverPanel { get; init; } =
        "flex flex-col gap-2.5 rounded-lg bg-popover p-2.5 text-sm text-popover-foreground shadow-md ring-1 ring-foreground/10";
    public string PopoverHeader { get; init; } = "flex flex-col gap-0.5 text-sm";
    public string PopoverTitle { get; init; } = "font-medium";
    public string PopoverDescription { get; init; } = "text-muted-foreground";

    string[] BuildToggles()
    {
        var table = new string[2 * 3 * 2 * 4];
        for (int v = 0; v < 2; v++)
            for (int s = 0; s < 3; s++)
                for (int p = 0; p < 2; p++)
                    for (int pos = 0; pos < 4; pos++)
                    {
                        string border = v == 1 ? ToggleOutlineBorders[pos] : "";
                        string radius = ToggleRadii[s == 1 ? 1 : 0][pos];
                        string pressed = p == 1 ? TogglePressed : "";
                        table[((v * 3 + s) * 2 + p) * 4 + pos] = string.Join(' ',
                            new[] { ToggleBase, ToggleVariants[v], ToggleSizes[s], border, radius, pressed }.Where(x => x.Length > 0));
                    }
        return table;
    }

    internal string[] ToggleTable { get; private set; } = [];
}
