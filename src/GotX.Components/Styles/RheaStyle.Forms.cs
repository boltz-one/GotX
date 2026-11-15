namespace GotX.Components;

// Phần biểu mẫu, menu, tooltip, hộp thoại và bong bóng của style rhea (xem RheaStyle.cs).
static partial class RheaStyle
{
    const string Field = "border border-transparent bg-input/50 transition-colors ";
    const string Box = "flex size-4 shrink-0 items-center justify-center rounded-[5px] border transition-shadow ";
    const string Radio = "relative flex size-4 shrink-0 items-center justify-center rounded-full border ";
    const string Track = "relative inline-flex shrink-0 items-center rounded-full border-2 transition-all ";
    const string Thumb = "block rounded-full bg-background shadow-sm transition-transform ";
    const string MenuRow = "flex min-h-7 items-center gap-2 rounded-xl px-2 py-1.5 text-sm whitespace-nowrap ";
    const string SelectRow = "flex min-h-7 items-center justify-between gap-2 rounded-xl py-1.5 pl-2 pr-2 text-sm cursor-pointer";
    const string Popup = "bg-popover text-popover-foreground shadow-lg ring-1 ring-foreground/5 dark:ring-foreground/10";

    static ComponentStyleSheet Forms(ComponentStyleSheet d) => d with
    {
        // <input> căn dòng chữ giữa hộp theo chiều dọc: flex-col justify-center.
        InputBox =
        [
            "flex h-8 w-full min-w-0 flex-col justify-center rounded-2xl " + Field + "px-2.5 py-1 text-sm cursor-text " + Focus,
            "flex h-8 w-full min-w-0 flex-col justify-center rounded-2xl " + Field + "px-2.5 py-1 text-sm cursor-text border-destructive ring-3 ring-destructive/20 dark:border-destructive/50 dark:ring-destructive/40",
            "flex h-8 w-full min-w-0 flex-col justify-center bg-transparent px-2.5 py-1 text-sm cursor-text",
        ],
        InputDisabled =
        [
            "flex h-8 w-full min-w-0 items-center rounded-2xl border border-transparent bg-input/50 px-2.5 py-1 text-sm opacity-50",
            "flex h-8 w-full min-w-0 items-center px-2.5 py-1 text-sm opacity-50",
        ],
        TextareaBox =
        [
            "w-full rounded-2xl " + Field + "px-2.5 py-2 text-sm cursor-text " + Focus,
            "w-full rounded-2xl " + Field + "px-2.5 py-2 text-sm cursor-text border-destructive ring-3 ring-destructive/20 dark:border-destructive/50 dark:ring-destructive/40",
            "w-full bg-transparent px-2.5 py-2 text-sm cursor-text",
        ],
        TextareaDisabled =
        [
            "min-h-16 w-full rounded-2xl border border-transparent bg-input/50 px-2.5 py-2 text-sm opacity-50",
            "min-h-16 w-full px-2.5 py-2 text-sm opacity-50",
        ],

        InputGroup =
        [
            "flex h-8 w-full min-w-0 items-center rounded-2xl border bg-input/50 transition-colors border-transparent",
            "flex h-8 w-full min-w-0 items-center rounded-2xl border bg-input/50 transition-colors border-ring ring-3 ring-ring/30",
            "flex w-full min-w-0 flex-col rounded-2xl border bg-input/50 transition-colors border-transparent",
            "flex w-full min-w-0 flex-col rounded-2xl border bg-input/50 transition-colors border-ring ring-3 ring-ring/30",
        ],
        InputGroupAddon =
        [
            "flex shrink-0 items-center justify-center gap-2 py-1.5 pl-2 text-sm font-medium text-muted-foreground",
            "flex shrink-0 items-center justify-center gap-2 py-1.5 pr-2 text-sm font-medium text-muted-foreground",
            "flex w-full items-center justify-start gap-2 px-2.5 pt-2 text-sm font-medium text-muted-foreground",
            "flex w-full items-center justify-start gap-2 px-2.5 pb-2 text-sm font-medium text-muted-foreground",
        ],

        SelectTrigger = "flex h-8 items-center justify-between gap-1.5 rounded-2xl " + Field +
            "px-3 py-2 text-sm whitespace-nowrap cursor-pointer disabled:opacity-50 " + Focus,
        SelectChevron = "text-muted-foreground",
        SelectList = "flex flex-col p-1 max-h-72 overflow-y-auto rounded-2xl " + Popup,
        SelectRow = [SelectRow, SelectRow + " bg-accent text-accent-foreground"],
        // Chữ mục (p-1 + pl-2) thẳng với chữ nút (viền 1 + px-3): danh sách lệch phải 1px; mục cao 32px.
        SelectAlignItem = new Bounds(1, 4, 0, 32),

        // focus-visible:border-ring thắng data-checked:border-primary (đo: viền a1a1a1 khi Tab tới ô đã chọn).
        CheckboxBox =
        [
            Box + "border-transparent bg-input/90",
            Box + "border-ring bg-input/90 ring-3 ring-ring/30",
            Box + "border-primary bg-primary text-primary-foreground",
            Box + "border-ring bg-primary text-primary-foreground ring-3 ring-ring/30",
        ],
        RadioRing =
        [
            Radio + "border-transparent bg-input/90",
            Radio + "border-ring bg-input/90 ring-3 ring-ring/30",
            Radio + "border-transparent bg-primary text-primary-foreground",
            Radio + "border-ring bg-primary text-primary-foreground ring-3 ring-ring/30",
        ],
        RadioDot = "size-2 rounded-full bg-primary-foreground dark:size-2.5",
        RadioGroup = ["flex flex-col gap-3", "flex flex-wrap items-center gap-3"],
        // h-5 w-8 (sm h-4 w-6), viền 2px; con chạy size-4 (sm size-3) dịch calc(100% - 4px).
        SwitchTrack =
        [
            Track + "border-transparent h-5 w-8 bg-input/90", Track + "border-ring ring-3 ring-ring/30 h-5 w-8 bg-input/90",
            Track + "border-primary h-5 w-8 bg-primary", Track + "border-ring ring-3 ring-ring/30 h-5 w-8 bg-primary",
            Track + "border-transparent h-4 w-6 bg-input/90", Track + "border-ring ring-3 ring-ring/30 h-4 w-6 bg-input/90",
            Track + "border-primary h-4 w-6 bg-primary", Track + "border-ring ring-3 ring-ring/30 h-4 w-6 bg-primary",
        ],
        SwitchThumb =
        [
            Thumb + "size-4 translate-x-0 dark:bg-foreground", Thumb + "size-4 translate-x-[12px] dark:bg-primary-foreground",
            Thumb + "size-3 translate-x-0 dark:bg-foreground", Thumb + "size-3 translate-x-[8px] dark:bg-primary-foreground",
        ],

        FieldSet = "flex flex-col gap-6",
        FieldLegend = ["mb-3 text-base font-medium", "mb-3 text-sm font-medium"],
        FieldGroup = "flex w-full flex-col gap-6",
        Field =
        [
            "flex w-full flex-col gap-3", "flex w-full flex-col gap-3 text-destructive",
            "flex w-full flex-row items-center gap-3", "flex w-full flex-row items-center gap-3 text-destructive",
        ],
        FieldLabel = "flex gap-2 text-sm font-medium leading-snug",
        FieldTitle = "flex items-center gap-2 text-sm leading-snug font-medium",

        MenuPanel = "flex flex-col min-w-32 rounded-2xl p-1 " + Popup,
        MenuRow =
        [
            MenuRow + "cursor-pointer",
            MenuRow + "cursor-pointer bg-accent text-accent-foreground",
            MenuRow + "cursor-pointer text-destructive",
            MenuRow + "cursor-pointer text-destructive bg-destructive/10 dark:bg-destructive/20",
            MenuRow + "opacity-50",
        ],
        // ml-auto: chữ của dòng đã flex-1 nên phím tắt tự dạt phải.
        MenuShortcut = ["pl-4 text-xs tracking-widest text-muted-foreground", "pl-4 text-xs tracking-widest text-accent-foreground"],
        MenuSeparator = "-mx-1 my-1 h-px bg-border/50",
        MenuLabel = "px-2 py-1 text-xs text-muted-foreground",

        TooltipPanel = "relative flex max-w-xs items-center gap-1.5 rounded-xl bg-foreground px-3 py-1.5 text-xs text-background",

        // max-w-xs (320px) cho cả hai cỡ; chân không có vạch kẻ/nền, nút chia đôi hàng ở cỡ sm.
        AlertDialogPanel =
        [
            "flex flex-col gap-6 w-full max-w-xs sm:max-w-md rounded-[24px] p-6 bg-popover text-popover-foreground ring-1 ring-foreground/5 dark:ring-foreground/10 shadow-xl",
            "flex flex-col gap-6 w-full max-w-xs rounded-[24px] p-6 bg-popover text-popover-foreground ring-1 ring-foreground/5 dark:ring-foreground/10 shadow-xl",
        ],
        AlertDialogHeader = ["flex flex-col gap-1.5 text-left", "flex flex-col items-center gap-1.5 text-center"],
        AlertDialogMedia = "mb-2 flex size-16 items-center justify-center rounded-full bg-muted",
        AlertDialogTitle = "text-lg font-medium",
        AlertDialogDescription = "text-sm text-balance text-muted-foreground",
        AlertDialogFooter = ["flex justify-end gap-2", "grid grid-cols-2 gap-2"],
        ModalBackdrop = "bg-black/30",

        BubbleContentBase = "max-w-full min-w-0 overflow-hidden rounded-3xl border border-transparent px-3 py-2.5 text-sm leading-relaxed transition-colors",
    };
}
