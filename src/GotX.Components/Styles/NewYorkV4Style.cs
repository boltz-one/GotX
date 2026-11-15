namespace GotX.Components;

/// <summary>
/// Style new-york-v4 của shadcn (<c>apps/v4/registry/new-york-v4/ui/*.tsx</c>) – style của các block. Chép class của bản
/// gốc; thay <c>border</c> trơn bằng <c>border border-border</c> (viền GotX mặc định lấy màu chữ), bỏ những gì Core chưa có
/// (has-*, data-*, aria-*, [&amp;_svg]…) và thay bằng tham số / chuỗi riêng như bảng mặc định.
/// </summary>
static partial class NewYorkV4Style
{
    const string Focus = "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50";

    public static ComponentStyleSheet Create(ComponentStyleSheet d) => More(Forms(d with
    {
        Style = ComponentStyle.NewYorkV4,

        // Nút v4 không có viền (chỉ outline có): không thêm border-transparent kẻo rộng hơn bản gốc 2px.
        ButtonBase = "inline-flex shrink-0 items-center justify-center gap-2 rounded-md text-sm font-medium whitespace-nowrap " +
            "cursor-pointer transition-all " + Focus + " disabled:opacity-50",
        ButtonVariants =
        [
            "bg-primary text-primary-foreground hover:bg-primary/90",
            "border border-border bg-background shadow-xs hover:bg-accent hover:text-accent-foreground dark:border-input dark:bg-input/30 dark:hover:bg-input/50",
            "bg-secondary text-secondary-foreground hover:bg-secondary/80",
            "hover:bg-accent hover:text-accent-foreground dark:hover:bg-accent/50",
            "bg-destructive text-white hover:bg-destructive/90 focus-visible:ring-destructive/20 dark:bg-destructive/60 dark:focus-visible:ring-destructive/40",
            "text-primary underline-offset-4 hover:underline",
        ],
        ButtonSizes =
        [
            "h-9 px-4 py-2", "h-6 gap-1 rounded-md px-2 text-xs", "h-8 gap-1.5 rounded-md px-3", "h-10 rounded-md px-6",
            "size-9", "size-6 rounded-md", "size-8", "size-10",
        ],

        BadgeBase = "inline-flex shrink-0 items-center justify-center gap-1 overflow-hidden rounded-full border border-transparent px-2 py-0.5 " +
            "text-xs font-medium whitespace-nowrap transition-colors " + Focus,
        BadgeVariants =
        [
            "bg-primary text-primary-foreground",
            "bg-secondary text-secondary-foreground",
            "bg-destructive text-white focus-visible:ring-destructive/20 dark:bg-destructive/60 dark:focus-visible:ring-destructive/40",
            "border-border text-foreground",
            "",
            "text-primary underline-offset-4",
        ],

        // v4 không có Card size: cỡ Sm dùng chung bảng (gap-6 py-6 px-6).
        CardRoot =
        [
            "flex flex-col gap-6 rounded-xl border border-border bg-card py-6 text-card-foreground shadow-sm",
            "flex flex-col gap-6 rounded-xl border border-border bg-card py-6 text-card-foreground shadow-sm",
        ],
        CardWithFooter = "",
        CardHeader = ["grid items-start gap-2 px-6", "grid items-start gap-2 px-6"],
        CardHeaderWithAction = ["grid grid-cols-[1fr_auto] items-start gap-2 px-6", "grid grid-cols-[1fr_auto] items-start gap-2 px-6"],
        CardTitle = ["leading-none font-semibold", "leading-none font-semibold"],
        CardContent = ["px-6", "px-6"],
        CardFooter = ["flex items-center px-6", "flex items-center px-6"],

        ItemBase = "flex w-full flex-wrap items-center rounded-md border text-sm transition-colors duration-100",
        ItemVariants = ["border-transparent bg-transparent", "border-border", "border-transparent bg-muted/50"],
        ItemSizes = ["gap-4 p-4", "gap-2.5 px-4 py-3", "gap-2.5 px-4 py-3"],
        ItemGroup = ["flex w-full flex-col", "flex w-full flex-col", "flex w-full flex-col"],
        ItemTitle = "flex items-center gap-2 text-sm leading-snug font-medium",
        ItemDescription =
        [
            "line-clamp-2 text-sm leading-normal font-normal text-balance text-muted-foreground",
            "line-clamp-2 text-sm leading-normal font-normal text-balance text-muted-foreground",
            "line-clamp-2 text-sm leading-normal font-normal text-balance text-muted-foreground",
        ],

        Empty = "flex w-full min-w-0 flex-1 flex-col items-center justify-center gap-6 rounded-lg border-dashed p-6 text-center text-balance md:p-12",
        EmptyHeader = "flex max-w-sm flex-col items-center gap-2 text-center",
        EmptyMedia =
        [
            "mb-2 flex shrink-0 items-center justify-center",
            "mb-2 flex size-10 shrink-0 items-center justify-center rounded-lg bg-muted text-foreground",
        ],
        EmptyTitle = "text-lg font-medium tracking-tight",
        EmptyContent = "flex w-full max-w-sm min-w-0 flex-col items-center gap-4 text-sm text-balance",

        ProgressTrack = "relative flex h-2 w-full items-center overflow-hidden rounded-full bg-primary/20",
        ButtonGroupText = "flex items-center gap-2 rounded-md border border-border bg-muted px-4 text-sm font-medium shadow-xs",

        Breadcrumb = "flex flex-wrap items-center gap-1.5 text-sm text-muted-foreground sm:gap-2.5",
        BreadcrumbItem = "inline-flex items-center gap-1.5",
        BreadcrumbSeparatorIcon = "size-3.5",
    }));
}
