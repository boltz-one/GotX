namespace GotX.Components;

// Giá trị mặc định của các component Forms (chép nguyên văn từ bản trước khi có style).
public sealed partial record ComponentStyleSheet
{
    const string InputBase = "h-8 w-full min-w-0 rounded-lg border bg-transparent px-2.5 py-1 text-sm cursor-text transition-colors dark:bg-input/30 ";
    const string TextareaBase = "w-full rounded-lg border bg-transparent px-2.5 py-2 text-sm cursor-text transition-colors dark:bg-input/30 ";

    // ── Input / Textarea ── [thường, lỗi, trong InputGroup]; Disabled: [đứng riêng, trong InputGroup].
    public string[] InputBox { get; init; } =
    [
        InputBase + "border-input focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50",
        InputBase + "border-destructive ring-3 ring-destructive/20 dark:border-destructive/50 dark:ring-destructive/40",
        "h-8 w-full min-w-0 bg-transparent px-2.5 py-1 text-sm cursor-text",
    ];
    public string[] InputDisabled { get; init; } =
    [
        "flex h-8 w-full min-w-0 items-center rounded-lg border border-input bg-input/50 px-2.5 py-1 text-sm opacity-50 dark:bg-input/80",
        "flex h-8 w-full min-w-0 items-center px-2.5 py-1 text-sm opacity-50",
    ];
    public string[] TextareaBox { get; init; } =
    [
        TextareaBase + "border-input focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50",
        TextareaBase + "border-destructive ring-3 ring-destructive/20 dark:border-destructive/50 dark:ring-destructive/40",
        "w-full bg-transparent px-2.5 py-2 text-sm cursor-text",
    ];
    public string[] TextareaDisabled { get; init; } =
    [
        "min-h-16 w-full rounded-lg border border-input bg-input/50 px-2.5 py-2 text-sm opacity-50 dark:bg-input/80",
        "min-h-16 w-full px-2.5 py-2 text-sm opacity-50",
    ];

    // ── InputGroup ── [xếp dọc * 2 + ô đang focus]; Addon theo InputGroupAlign.
    public string[] InputGroup { get; init; } =
    [
        "flex h-8 w-full min-w-0 items-center rounded-lg border transition-colors dark:bg-input/30 border-input",
        "flex h-8 w-full min-w-0 items-center rounded-lg border transition-colors dark:bg-input/30 border-ring ring-3 ring-ring/50",
        "flex w-full min-w-0 flex-col rounded-lg border transition-colors dark:bg-input/30 border-input",
        "flex w-full min-w-0 flex-col rounded-lg border transition-colors dark:bg-input/30 border-ring ring-3 ring-ring/50",
    ];
    public string[] InputGroupAddon { get; init; } =
    [
        "flex shrink-0 items-center justify-center gap-2 pl-2 text-sm font-medium text-muted-foreground",
        "flex shrink-0 items-center justify-center gap-2 pr-2 text-sm font-medium text-muted-foreground",
        "flex w-full items-center justify-start gap-2 px-2.5 pt-2 text-sm font-medium text-muted-foreground",
        "flex w-full items-center justify-start gap-2 px-2.5 pb-2 text-sm font-medium text-muted-foreground",
    ];
    public string InputGroupText { get; init; } = "flex items-center gap-2 text-sm text-muted-foreground";
    /// <summary>Class thêm cho nút trong InputGroup ("" = nút thường cỡ Xs/IconXs).</summary>
    public string InputGroupButton { get; init; } = "";

    // ── Select (cả NativeSelect) ──
    public string SelectTrigger { get; init; } =
        "flex h-8 min-w-36 items-center justify-between gap-1.5 rounded-lg border border-input bg-transparent " +
        "pl-2.5 pr-2 text-sm whitespace-nowrap cursor-pointer transition-colors hover:bg-muted/50 " +
        "disabled:opacity-50 dark:bg-input/30 dark:hover:bg-input/50 " + FocusRing.Ring;
    public string SelectChevron { get; init; } = "text-muted-foreground";
    public string SelectList { get; init; } =
        "flex flex-col p-1 max-h-72 overflow-y-auto rounded-lg bg-popover text-popover-foreground shadow-md ring-1 ring-foreground/10";
    /// <summary>
    /// Danh sách mở đè lên nút, mục đang chọn ở đúng chỗ nút (Base UI alignItemWithTrigger); null = mở dưới nút.
    /// X = lệch ngang của danh sách, Y = khoảng từ mép trên danh sách tới mục đầu, Height = chiều cao một mục.
    /// </summary>
    public Bounds? SelectAlignItem { get; init; }
    /// <summary>[thường, đang tô sáng].</summary>
    public string[] SelectRow { get; init; } =
    [
        "flex items-center justify-between gap-2 rounded-md py-1 pl-1.5 pr-1.5 text-sm cursor-pointer",
        "flex items-center justify-between gap-2 rounded-md py-1 pl-1.5 pr-1.5 text-sm cursor-pointer bg-accent text-accent-foreground",
    ];

    // ── Checkbox / RadioGroup / Switch ── [đã chọn * 2 + đang focus bằng phím].
    public string[] CheckboxBox { get; init; } =
    [
        "flex size-4 shrink-0 items-center justify-center rounded-[4px] border border-input transition-colors dark:bg-input/30",
        "flex size-4 shrink-0 items-center justify-center rounded-[4px] border border-input transition-colors dark:bg-input/30 border-ring ring-3 ring-ring/50",
        "flex size-4 shrink-0 items-center justify-center rounded-[4px] border border-primary bg-primary text-primary-foreground transition-colors",
        "flex size-4 shrink-0 items-center justify-center rounded-[4px] border border-primary bg-primary text-primary-foreground transition-colors ring-3 ring-ring/50",
    ];
    public string CheckboxIcon { get; init; } = "size-3.5";
    public string[] RadioRing { get; init; } =
    [
        "relative flex size-4 shrink-0 items-center justify-center rounded-full border border-input dark:bg-input/30",
        "relative flex size-4 shrink-0 items-center justify-center rounded-full border border-input dark:bg-input/30 border-ring ring-3 ring-ring/50",
        "relative flex size-4 shrink-0 items-center justify-center rounded-full border border-primary bg-primary",
        "relative flex size-4 shrink-0 items-center justify-center rounded-full border border-primary bg-primary ring-3 ring-ring/50",
    ];
    public string RadioDot { get; init; } = "size-2 rounded-full bg-primary-foreground";
    /// <summary>[dọc, ngang].</summary>
    public string[] RadioGroup { get; init; } = ["flex flex-col gap-3", "flex flex-wrap items-center gap-4"];
    /// <summary>[(cỡ * 2 + bật) * 2 + đang focus bằng phím].</summary>
    public string[] SwitchTrack { get; init; } = BuildSwitchTracks();
    /// <summary>[cỡ * 2 + bật]: con chạy dịch đúng (rộng track − rộng con chạy − viền).</summary>
    public string[] SwitchThumb { get; init; } =
    [
        "block size-4 rounded-full bg-background transition-transform translate-x-0 dark:bg-foreground",
        "block size-4 rounded-full bg-background transition-transform translate-x-[14px] dark:bg-primary-foreground",
        "block size-3 rounded-full bg-background transition-transform translate-x-0 dark:bg-foreground",
        "block size-3 rounded-full bg-background transition-transform translate-x-[10px] dark:bg-primary-foreground",
    ];

    static string[] BuildSwitchTracks()
    {
        const string trackBase = "relative inline-flex shrink-0 items-center rounded-full border transition-all ";
        string[] sizes = ["h-[18.4px] w-8 ", "h-[14px] w-6 "];
        string[] states = ["bg-input dark:bg-input/80", "bg-primary"];
        var table = new string[8];
        for (int i = 0; i < 8; i++)
            table[i] = trackBase + ((i & 1) != 0 ? "border-ring ring-3 ring-ring/50 " : "border-transparent ") + sizes[i >> 2] + states[(i >> 1) & 1];
        return table;
    }

    // ── Field / Label ──
    public string FieldSet { get; init; } = "flex flex-col gap-4";
    /// <summary>[legend, nhãn].</summary>
    public string[] FieldLegend { get; init; } = ["mb-1.5 text-base font-medium", "mb-1.5 text-sm font-medium"];
    public string FieldGroup { get; init; } = "flex w-full flex-col gap-5";
    /// <summary>[ngang * 2 + lỗi].</summary>
    public string[] Field { get; init; } =
    [
        "flex w-full flex-col gap-2", "flex w-full flex-col gap-2 text-destructive",
        "flex w-full flex-row items-center gap-2", "flex w-full flex-row items-center gap-2 text-destructive",
    ];
    public string FieldContent { get; init; } = "flex flex-1 flex-col gap-1 leading-snug";
    public string FieldLabel { get; init; } = "flex gap-2 text-sm font-medium leading-snug";
    public string FieldTitle { get; init; } = "flex items-center gap-2 text-sm font-medium";
    public string FieldDescription { get; init; } = "text-left text-sm leading-normal font-normal text-muted-foreground";
    public string Label { get; init; } = "flex items-center gap-2 text-sm leading-none font-medium";
}
