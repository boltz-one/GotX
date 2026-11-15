namespace GotX.Components;

// Phần biểu mẫu, menu, tooltip và hộp thoại của new-york-v4 (xem NewYorkV4Style.cs).
static partial class NewYorkV4Style
{
    const string Box = "flex size-4 shrink-0 items-center justify-center rounded-[4px] border shadow-xs transition-shadow ";
    const string Radio = "relative flex size-4 shrink-0 items-center justify-center rounded-full border border-input shadow-xs dark:bg-input/30";
    const string Track = "relative inline-flex shrink-0 items-center rounded-full border shadow-xs transition-all ";
    const string Thumb = "block rounded-full bg-background transition-transform ";
    const string MenuRow = "flex items-center gap-2 rounded-sm px-2 py-1.5 text-sm whitespace-nowrap ";
    const string SelectRow = "flex items-center justify-between gap-2 rounded-sm py-1.5 pl-2 pr-2 text-sm cursor-pointer";
    const string Popup = "min-w-[8rem] rounded-md border border-border bg-popover text-popover-foreground shadow-md";
    const string InputField = "rounded-md border bg-transparent text-sm shadow-xs cursor-text transition-colors dark:bg-input/30 ";
    const string Error = "border-destructive ring-3 ring-destructive/20 dark:ring-destructive/40";

    static ComponentStyleSheet Forms(ComponentStyleSheet d) => d with
    {
        // <input> căn dòng chữ giữa hộp theo chiều dọc: flex-col justify-center.
        InputBox =
        [
            "flex h-9 w-full min-w-0 flex-col justify-center px-3 py-1 " + InputField + "border-input " + Focus,
            "flex h-9 w-full min-w-0 flex-col justify-center px-3 py-1 " + InputField + Error,
            "flex h-9 w-full min-w-0 flex-col justify-center bg-transparent px-3 py-1 text-sm cursor-text",
        ],
        InputDisabled =
        [
            "flex h-9 w-full min-w-0 items-center rounded-md border border-input bg-transparent px-3 py-1 text-sm opacity-50 shadow-xs dark:bg-input/30",
            "flex h-9 w-full min-w-0 items-center px-3 py-1 text-sm opacity-50",
        ],
        TextareaBox =
        [
            "w-full px-3 py-2 " + InputField + "border-input " + Focus,
            "w-full px-3 py-2 " + InputField + Error,
            "w-full bg-transparent px-3 py-3 text-sm cursor-text",
        ],
        TextareaDisabled =
        [
            "min-h-16 w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm opacity-50 shadow-xs dark:bg-input/30",
            "min-h-16 w-full px-3 py-3 text-sm opacity-50",
        ],

        InputGroup =
        [
            "flex h-9 w-full min-w-0 items-center rounded-md border shadow-xs transition-colors dark:bg-input/30 border-input",
            "flex h-9 w-full min-w-0 items-center rounded-md border shadow-xs transition-colors dark:bg-input/30 border-ring ring-3 ring-ring/50",
            "flex w-full min-w-0 flex-col rounded-md border shadow-xs transition-colors dark:bg-input/30 border-input",
            "flex w-full min-w-0 flex-col rounded-md border shadow-xs transition-colors dark:bg-input/30 border-ring ring-3 ring-ring/50",
        ],
        InputGroupAddon =
        [
            "flex shrink-0 items-center justify-center gap-2 py-1.5 pl-3 text-sm font-medium text-muted-foreground",
            "flex shrink-0 items-center justify-center gap-2 py-1.5 pr-3 text-sm font-medium text-muted-foreground",
            "flex w-full items-center justify-start gap-2 px-3 pt-3 text-sm font-medium text-muted-foreground",
            "flex w-full items-center justify-start gap-2 px-3 pb-3 text-sm font-medium text-muted-foreground",
        ],

        SelectTrigger = "flex h-9 w-fit items-center justify-between gap-2 rounded-md border border-input bg-transparent px-3 py-2 text-sm " +
            "whitespace-nowrap shadow-xs cursor-pointer transition-colors disabled:opacity-50 dark:bg-input/30 dark:hover:bg-input/50 " + Focus,
        SelectChevron = "opacity-50",
        SelectList = "flex flex-col p-1 max-h-72 overflow-y-auto " + Popup,
        SelectRow = [SelectRow, SelectRow + " bg-accent text-accent-foreground"],
        // Radix position="item-aligned" (mặc định của SelectContent v4): chữ mục thẳng chữ nút; viền 1 + p-1 tới mục đầu.
        SelectAlignItem = new Bounds(0, 5, 0, 32),

        CheckboxBox =
        [
            Box + "border-input dark:bg-input/30",
            Box + "border-input dark:bg-input/30 border-ring ring-3 ring-ring/50",
            Box + "border-primary bg-primary text-primary-foreground",
            Box + "border-primary bg-primary text-primary-foreground ring-3 ring-ring/50",
        ],
        // Mục đã chọn giữ viền input, chấm tròn màu primary ở giữa (CircleIcon fill-primary).
        RadioRing = [Radio, Radio + " border-ring ring-3 ring-ring/50", Radio, Radio + " border-ring ring-3 ring-ring/50"],
        RadioDot = "size-2 rounded-full bg-primary",
        RadioGroup = ["flex flex-col gap-3", "flex flex-wrap items-center gap-3"],
        SwitchTrack =
        [
            Track + "border-transparent h-[18.4px] w-8 bg-input dark:bg-input/80", Track + "border-ring ring-3 ring-ring/50 h-[18.4px] w-8 bg-input dark:bg-input/80",
            Track + "border-transparent h-[18.4px] w-8 bg-primary", Track + "border-ring ring-3 ring-ring/50 h-[18.4px] w-8 bg-primary",
            Track + "border-transparent h-[14px] w-6 bg-input dark:bg-input/80", Track + "border-ring ring-3 ring-ring/50 h-[14px] w-6 bg-input dark:bg-input/80",
            Track + "border-transparent h-[14px] w-6 bg-primary", Track + "border-ring ring-3 ring-ring/50 h-[14px] w-6 bg-primary",
        ],
        SwitchThumb =
        [
            Thumb + "size-4 translate-x-0 dark:bg-foreground", Thumb + "size-4 translate-x-[14px] dark:bg-primary-foreground",
            Thumb + "size-3 translate-x-0 dark:bg-foreground", Thumb + "size-3 translate-x-[10px] dark:bg-primary-foreground",
        ],

        FieldSet = "flex flex-col gap-6",
        FieldLegend = ["mb-3 text-base font-medium", "mb-3 text-sm font-medium"],
        FieldGroup = "flex w-full flex-col gap-7",
        Field =
        [
            "flex w-full flex-col gap-3", "flex w-full flex-col gap-3 text-destructive",
            "flex w-full flex-row items-center gap-3", "flex w-full flex-row items-center gap-3 text-destructive",
        ],
        FieldContent = "flex flex-1 flex-col gap-1.5 leading-snug",
        FieldLabel = "flex items-center gap-2 text-sm leading-snug font-medium",
        FieldTitle = "flex items-center gap-2 text-sm leading-snug font-medium",

        MenuPanel = "flex flex-col p-1 " + Popup,
        MenuRow =
        [
            MenuRow + "cursor-pointer",
            MenuRow + "cursor-pointer bg-accent text-accent-foreground",
            MenuRow + "cursor-pointer text-destructive",
            MenuRow + "cursor-pointer text-destructive bg-destructive/10 dark:bg-destructive/20",
            MenuRow + "opacity-50",
        ],
        MenuIcon = "text-muted-foreground",
        MenuLabel = "px-2 py-1.5 text-sm font-medium",
        MenuTriggerWrapper = "flex rounded-md focus-visible:ring-3 focus-visible:ring-ring/50",

        // flex-col-reverse chưa có: cột thường dưới sm (nút Huỷ ở trên), hàng phải từ sm như bản gốc.
        AlertDialogPanel =
        [
            "flex flex-col gap-4 w-full max-w-[calc(100%-2rem)] sm:max-w-lg rounded-lg border border-border bg-background p-6 shadow-lg",
            "flex flex-col gap-4 w-full max-w-xs rounded-lg border border-border bg-background p-6 shadow-lg",
        ],
        AlertDialogHeader = ["flex flex-col items-center gap-1.5 text-center sm:items-start sm:text-left", "flex flex-col items-center gap-1.5 text-center"],
        AlertDialogMedia = "mb-2 flex size-16 items-center justify-center rounded-md bg-muted",
        AlertDialogTitle = "text-lg font-semibold",
        AlertDialogFooter = ["flex flex-col gap-2 sm:flex-row sm:justify-end", "grid grid-cols-2 gap-2"],
    };
}
