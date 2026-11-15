namespace GotX.Components;

// Avatar, Skeleton, Toggle/ToggleGroup, Sheet và Dialog của new-york-v4 (xem NewYorkV4Style.cs).
static partial class NewYorkV4Style
{
    const string SidePanel = "relative flex flex-col gap-4 bg-background text-sm text-foreground shadow-lg border-border ";
    const string Fallback = "flex size-full items-center justify-center rounded-full bg-muted text-muted-foreground ";

    static ComponentStyleSheet More(ComponentStyleSheet d) => d with
    {
        AvatarRoot =
        [
            "relative flex size-8 shrink-0 overflow-hidden rounded-full",
            "relative flex size-6 shrink-0 overflow-hidden rounded-full",
            "relative flex size-10 shrink-0 overflow-hidden rounded-full",
        ],
        AvatarFallback = [Fallback + "text-sm", Fallback + "text-xs", Fallback + "text-sm"],
        AvatarRing = "",
        AvatarImage = "aspect-square size-full object-cover",

        Skeleton = "animate-pulse rounded-md bg-accent",

        ToggleBase = "inline-flex shrink-0 items-center justify-center gap-2 text-sm font-medium whitespace-nowrap cursor-pointer " +
            "transition-colors hover:bg-muted hover:text-muted-foreground " + Focus + " disabled:opacity-50",
        ToggleVariants = ["bg-transparent", "border-input bg-transparent shadow-xs hover:bg-accent hover:text-accent-foreground"],
        ToggleSizes = ["h-9 min-w-9 px-2", "h-8 min-w-8 px-1.5", "h-10 min-w-10 px-2.5"],
        ToggleRadii = [["rounded-md", "rounded-l-md", "", "rounded-r-md"], ["rounded-md", "rounded-l-md", "", "rounded-r-md"]],
        TogglePressed = "bg-accent text-accent-foreground",
        ToggleGroup = "inline-flex items-center rounded-md",
        ToggleGroupItem = "px-3",

        // w-3/4 sm:max-w-sm ở cạnh trái/phải; nền background, viền border ở cạnh hướng vào trong.
        SheetPanel =
        [
            SidePanel + "h-full w-3/4 sm:max-w-sm border-l", SidePanel + "h-full w-3/4 sm:max-w-sm border-r",
            SidePanel + "w-full border-b", SidePanel + "w-full border-t",
        ],
        SheetHeader = "flex flex-col gap-1.5 p-4",
        SheetFooter = "flex flex-col gap-2 p-4", // mt-auto: thân tấm đã flex-1 nên chân tự dính đáy
        SheetClosePosition = "absolute top-4 right-4",
        PanelTitle = "font-semibold text-foreground",

        DialogPanel = "relative flex flex-col gap-4 w-full max-w-[calc(100%-2rem)] sm:max-w-lg rounded-lg border border-border bg-background p-6 shadow-lg",
        DialogHeader = "flex flex-col gap-2 text-center sm:text-left",
        DialogTitle = "text-lg leading-none font-semibold",
        // flex-col-reverse chưa có: cột thường dưới sm, hàng phải từ sm.
        DialogFooter = "flex flex-col gap-2 sm:flex-row sm:justify-end",
        DialogClosePosition = "absolute top-4 right-4",
        OverlayClose = "flex size-4 items-center justify-center rounded-xs opacity-70 cursor-pointer transition-opacity hover:opacity-100",

        // Accordion: py-4 rounded-md, chevron size-4 lệch xuống 2px, nội dung pt-0 pb-4.
        AccordionTrigger = "flex items-start justify-between gap-4 rounded-md border border-transparent py-4 text-left text-sm font-medium " +
            "cursor-pointer transition-all hover:underline " + Focus,
        AccordionChevron =
        [
            "translate-y-0.5 text-muted-foreground transition-transform duration-200",
            "translate-y-0.5 rotate-180 text-muted-foreground transition-transform duration-200",
        ],
        AccordionContent = "pt-0 pb-4 text-sm",

        PopoverPanel = "flex flex-col gap-2.5 rounded-md border border-border bg-popover p-4 text-popover-foreground shadow-md",
        PopoverHeader = "flex flex-col gap-1 text-sm",
    };
}