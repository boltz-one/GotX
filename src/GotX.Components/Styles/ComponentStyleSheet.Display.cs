namespace GotX.Components;

// Giá trị mặc định: chép nguyên văn chuỗi class các component Display/Navigation vẫn dùng (đổi một ký tự là đổi ảnh).
public sealed partial record ComponentStyleSheet
{
    // ── Button ── chỉ số theo ButtonVariant / ButtonSize.
    public string ButtonBase { get; init; } =
        "inline-flex shrink-0 items-center justify-center rounded-lg border border-transparent text-sm font-medium " +
        "whitespace-nowrap cursor-pointer transition-all focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 " +
        "active:translate-y-px disabled:opacity-50";

    public string[] ButtonVariants { get; init; } =
    [
        "bg-primary text-primary-foreground hover:bg-primary/80",
        "border-border bg-background hover:bg-muted hover:text-foreground dark:border-input dark:bg-input/30 dark:hover:bg-input/50",
        "bg-secondary text-secondary-foreground hover:bg-secondary/80",
        "hover:bg-muted hover:text-foreground dark:hover:bg-muted/50",
        "bg-destructive/10 text-destructive hover:bg-destructive/20 focus-visible:border-destructive/40 focus-visible:ring-destructive/20 " +
            "dark:bg-destructive/20 dark:hover:bg-destructive/30 dark:focus-visible:ring-destructive/40",
        "text-primary hover:underline",
    ];

    public string[] ButtonSizes { get; init; } =
    [
        "h-8 gap-1.5 px-2.5", "h-6 gap-1 px-2 text-xs rounded-md", "h-7 gap-1 px-2.5 text-[0.8rem] rounded-md", "h-9 gap-1.5 px-2.5",
        "size-8", "size-6 rounded-md", "size-7 rounded-md", "size-9",
    ];

    // ── Badge ── chỉ số theo BadgeVariant.
    public string BadgeBase { get; init; } =
        "inline-flex h-5 shrink-0 items-center justify-center gap-1 overflow-hidden rounded-4xl border border-transparent " +
        "px-2 py-0.5 text-xs font-medium whitespace-nowrap transition-all " +
        "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50";

    public string[] BadgeVariants { get; init; } =
    [
        "bg-primary text-primary-foreground",
        "bg-secondary text-secondary-foreground",
        "bg-destructive/10 text-destructive focus-visible:ring-destructive/20 dark:bg-destructive/20 dark:focus-visible:ring-destructive/40",
        "border-border text-foreground",
        "hover:bg-muted hover:text-muted-foreground dark:hover:bg-muted/50",
        "text-primary hover:underline",
    ];

    // ── Card ── chỉ số theo CardSize.
    public string[] CardRoot { get; init; } =
    [
        "flex flex-col gap-4 overflow-hidden rounded-xl bg-card py-4 text-sm text-card-foreground ring-1 ring-foreground/10",
        "flex flex-col gap-3 overflow-hidden rounded-xl bg-card py-3 text-sm text-card-foreground ring-1 ring-foreground/10",
    ];
    /// <summary>Class thêm vào khung khi có footer (style mặc định: footer tự có đệm nên khung bỏ đệm đáy).</summary>
    public string CardWithFooter { get; init; } = "pb-0";
    public string[] CardHeader { get; init; } = ["grid items-start gap-1 rounded-t-xl px-4", "grid items-start gap-1 rounded-t-xl px-3"];
    public string[] CardHeaderWithAction { get; init; } =
    [
        "grid grid-cols-[1fr_auto] items-start gap-x-2 gap-y-1 rounded-t-xl px-4",
        "grid grid-cols-[1fr_auto] items-start gap-x-2 gap-y-1 rounded-t-xl px-3",
    ];
    public string[] CardTitle { get; init; } = ["text-base leading-snug font-medium", "text-sm leading-snug font-medium"];
    public string CardDescription { get; init; } = "text-sm text-muted-foreground";
    public string CardAction { get; init; } = "row-span-2 self-start flex justify-end";
    public string[] CardContent { get; init; } = ["px-4", "px-3"];
    public string[] CardFooter { get; init; } =
    [
        "flex items-center rounded-b-xl border-t border-border bg-muted/50 p-4",
        "flex items-center rounded-b-xl border-t border-border bg-muted/50 p-3",
    ];

    // ── Item ── bảng ghép theo ItemVariant × ItemSize; các phần con chỉ số theo ItemSize.
    public string ItemBase { get; init; } = "flex w-full flex-wrap items-center rounded-lg border text-sm transition-colors duration-100";
    public string[] ItemVariants { get; init; } = ["border-transparent", "border-border", "border-transparent bg-muted/50"];
    public string[] ItemSizes { get; init; } = ["gap-2.5 px-3 py-2.5", "gap-2.5 px-3 py-2.5", "gap-2 px-2.5 py-2"];
    public string[] ItemGroup { get; init; } = ["flex w-full flex-col gap-4", "flex w-full flex-col gap-2.5", "flex w-full flex-col gap-2"];
    public string ItemMedia { get; init; } = "flex shrink-0 items-center justify-center gap-2";
    public string[] ItemMediaImage { get; init; } =
    [
        "flex shrink-0 items-center justify-center gap-2 size-10 overflow-hidden rounded-sm",
        "flex shrink-0 items-center justify-center gap-2 size-8 overflow-hidden rounded-sm",
        "flex shrink-0 items-center justify-center gap-2 size-6 overflow-hidden rounded-sm",
    ];
    public string ItemMediaTop { get; init; } = "self-start translate-y-0.5";
    public string[] ItemContent { get; init; } = ["flex flex-1 flex-col gap-1", "flex flex-1 flex-col gap-1", "flex flex-1 flex-col gap-0"];
    public string ItemTitle { get; init; } = "flex items-center gap-2 text-sm leading-snug font-medium";
    public string ItemTitleText { get; init; } = "truncate";
    public string[] ItemDescription { get; init; } =
    [
        "text-left text-sm leading-normal font-normal text-muted-foreground",
        "text-left text-sm leading-normal font-normal text-muted-foreground",
        "text-left text-xs leading-normal font-normal text-muted-foreground",
    ];

    // ── Empty ──
    public string Empty { get; init; } =
        "flex w-full min-w-0 flex-1 flex-col items-center justify-center gap-4 rounded-xl border-dashed p-6 text-center";
    public string EmptyHeader { get; init; } = "flex max-w-sm flex-col items-center gap-2";
    public string[] EmptyMedia { get; init; } =
    [
        "mb-2 flex shrink-0 items-center justify-center",
        "mb-2 flex size-8 shrink-0 items-center justify-center rounded-lg bg-muted text-foreground",
    ];
    public string EmptyTitle { get; init; } = "text-sm font-medium tracking-tight";
    public string EmptyDescription { get; init; } = "text-sm leading-relaxed text-muted-foreground";
    public string EmptyContent { get; init; } = "flex w-full max-w-sm min-w-0 flex-col items-center gap-2.5 text-sm";

    // ── Progress, Separator, Kbd, ButtonGroup ──
    public string ProgressTrack { get; init; } = "relative flex h-1 w-full items-center overflow-hidden rounded-full bg-muted";
    public string ProgressIndicator { get; init; } = "h-full bg-primary transition-all";
    public string Kbd { get; init; } =
        "inline-flex h-5 min-w-5 items-center justify-center gap-1 rounded-sm bg-muted px-1 font-sans text-xs font-medium text-muted-foreground";
    public string ButtonGroupText { get; init; } = "flex items-center gap-2 rounded-lg border border-border bg-muted px-2.5 text-sm font-medium";

    // ── Breadcrumb ──
    public string Breadcrumb { get; init; } = "flex flex-wrap items-center gap-1.5 text-sm text-muted-foreground";
    public string BreadcrumbItem { get; init; } = "inline-flex items-center gap-1";
    public string BreadcrumbSeparator { get; init; } = "flex items-center";
    /// <summary>Class của icon mũi tên trong dấu phân cách ("" = cỡ mặc định của icon).</summary>
    public string BreadcrumbSeparatorIcon { get; init; } = "";
}
