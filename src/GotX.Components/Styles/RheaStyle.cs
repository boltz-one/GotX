namespace GotX.Components;

/// <summary>
/// Style "rhea" của shadcn (registry/styles/style-rhea.css + registry/bases/base/ui/*.tsx): ghép class cn-* của style với
/// class cấu trúc của component. Bỏ những gì Core chưa có (has-*, data-*, aria-*, [&amp;_svg]…) và thay bằng tham số / chuỗi
/// riêng; color-mix(…) của nút secondary tính sẵn ra màu (đo trên trang thật: #e7e7e7 sáng, #2f2f2f tối).
/// </summary>
static partial class RheaStyle
{
    const string Focus = "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/30";

    public static ComponentStyleSheet Create(ComponentStyleSheet d) => Forms(d with
    {
        Style = ComponentStyle.Rhea,
        // bg-clip-padding: nền dừng ở mép trong viền trong suốt (nút hiện nhỏ hơn hộp 1px mỗi phía như bản gốc).
        ButtonBase = "inline-flex shrink-0 items-center justify-center rounded-2xl border border-transparent bg-clip-padding text-sm font-medium " +
            "whitespace-nowrap cursor-pointer transition-all " + Focus + " active:translate-y-px disabled:opacity-50",
        ButtonVariants =
        [
            "bg-primary text-primary-foreground hover:bg-primary/80",
            "border-border bg-background hover:bg-muted hover:text-foreground dark:bg-transparent dark:hover:bg-input/30",
            "bg-secondary text-secondary-foreground hover:bg-[#e7e7e7] dark:hover:bg-[#2f2f2f]",
            "hover:bg-muted hover:text-foreground dark:hover:bg-muted/50",
            "bg-destructive/10 text-destructive hover:bg-destructive/20 focus-visible:border-destructive/40 focus-visible:ring-destructive/20 " +
                "dark:bg-destructive/20 dark:hover:bg-destructive/30 dark:focus-visible:ring-destructive/40",
            "text-primary hover:underline",
        ],
        ButtonSizes = ["h-8 gap-1.5 px-3", "h-6 gap-1 px-2.5 text-xs", "h-7 gap-1 px-3", "h-9 gap-1.5 px-4", "size-8", "size-6", "size-7", "size-9"],

        BadgeBase = "inline-flex h-5 shrink-0 items-center justify-center gap-1 overflow-hidden rounded-2xl border border-transparent " +
            "px-2 py-0.5 text-xs font-medium whitespace-nowrap transition-all focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50",

        // --card-spacing 5 (sm: 4), bo min(--radius-4xl, 24px), shadow-sm, viền vòng foreground/5; footer không có vạch kẻ/nền.
        CardRoot =
        [
            "flex flex-col gap-5 overflow-hidden rounded-[24px] bg-card py-5 text-sm text-card-foreground shadow-sm ring-1 ring-foreground/5 dark:ring-foreground/10",
            "flex flex-col gap-4 overflow-hidden rounded-[24px] bg-card py-4 text-sm text-card-foreground shadow-sm ring-1 ring-foreground/5 dark:ring-foreground/10",
        ],
        CardWithFooter = "",
        CardHeader = ["grid items-start gap-1.5 rounded-t-[24px] px-5", "grid items-start gap-1.5 rounded-t-[24px] px-4"],
        CardHeaderWithAction =
        [
            "grid grid-cols-[1fr_auto] items-start gap-1.5 rounded-t-[24px] px-5",
            "grid grid-cols-[1fr_auto] items-start gap-1.5 rounded-t-[24px] px-4",
        ],
        CardTitle = ["text-base font-medium", "text-base font-medium"],
        CardContent = ["px-5", "px-4"],
        CardFooter = ["flex items-center rounded-b-[24px] px-5", "flex items-center rounded-b-[24px] px-4"],

        ItemBase = "flex w-full flex-wrap items-center rounded-2xl border text-sm transition-colors duration-100",
        ItemSizes = ["gap-3.5 px-4 py-3.5", "gap-3.5 px-3.5 py-3", "gap-2 px-2.5 py-2"],
        ItemMediaImage =
        [
            "flex shrink-0 items-center justify-center gap-2 size-10 overflow-hidden rounded-xl",
            "flex shrink-0 items-center justify-center gap-2 size-8 overflow-hidden rounded-xl",
            "flex shrink-0 items-center justify-center gap-2 size-6 overflow-hidden rounded-lg",
        ],
        ItemContent = ["flex flex-1 flex-col gap-1", "flex flex-1 flex-col gap-1", "flex flex-1 flex-col gap-0.5"],
        ItemTitle = "flex items-center gap-2 text-sm leading-snug font-medium",
        ItemTitleText = "line-clamp-1",
        ItemDescription =
        [
            "line-clamp-2 text-left text-sm font-normal text-muted-foreground",
            "line-clamp-2 text-left text-sm font-normal text-muted-foreground",
            "line-clamp-2 text-left text-sm font-normal text-muted-foreground",
        ],

        Empty = "flex w-full min-w-0 flex-1 flex-col items-center justify-center gap-4 rounded-3xl border-dashed p-12 text-center text-balance",
        EmptyMedia =
        [
            "mb-2 flex shrink-0 items-center justify-center",
            "mb-2 flex size-10 shrink-0 items-center justify-center rounded-xl bg-muted text-foreground",
        ],
        EmptyTitle = "text-lg font-medium tracking-tight",
        EmptyContent = "flex w-full max-w-sm min-w-0 flex-col items-center gap-4 text-sm text-balance",

        ProgressTrack = "relative flex h-2 w-full items-center overflow-hidden rounded-2xl bg-muted",
        Kbd = "inline-flex h-5 min-w-5 items-center justify-center gap-1 rounded-lg bg-muted px-1 font-sans text-xs font-medium text-muted-foreground",
        ButtonGroupText = "flex items-center gap-2 rounded-2xl border border-border bg-muted px-2.5 text-sm font-medium",

        Breadcrumb = "flex flex-wrap items-center gap-1.5 text-sm text-muted-foreground sm:gap-2.5",
        BreadcrumbItem = "inline-flex items-center gap-1.5",
        BreadcrumbSeparatorIcon = "size-3.5",
    });
}
