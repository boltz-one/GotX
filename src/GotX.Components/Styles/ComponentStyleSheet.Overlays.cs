namespace GotX.Components;

// Giá trị mặc định của menu, tooltip, hộp thoại và bong bóng chat (chép nguyên văn từ bản trước khi có style).
public sealed partial record ComponentStyleSheet
{
    const string MenuRowBase = "flex items-center gap-1.5 rounded-md px-1.5 py-1 text-sm whitespace-nowrap ";

    // ── Menu (DropdownMenu, ContextMenu, Menubar) ──
    public string MenuPanel { get; init; } =
        "flex flex-col min-w-32 rounded-lg bg-popover p-1 text-popover-foreground shadow-md ring-1 ring-foreground/10";
    /// <summary>[thường, thường đang sáng, nguy hiểm, nguy hiểm đang sáng, bị tắt].</summary>
    public string[] MenuRow { get; init; } =
    [
        MenuRowBase + "cursor-pointer",
        MenuRowBase + "cursor-pointer bg-accent text-accent-foreground",
        MenuRowBase + "cursor-pointer text-destructive",
        MenuRowBase + "cursor-pointer text-destructive bg-destructive/10 dark:bg-destructive/20",
        MenuRowBase + "opacity-50",
    ];
    /// <summary>[thường, đang sáng].</summary>
    public string[] MenuShortcut { get; init; } =
        ["pl-4 text-xs tracking-widest text-muted-foreground", "pl-4 text-xs tracking-widest text-accent-foreground"];
    public string MenuSeparator { get; init; } = "-mx-1 my-1 h-px bg-border";
    public string MenuLabel { get; init; } = "px-1.5 py-1 text-xs font-medium text-muted-foreground";
    /// <summary>Class thêm cho icon đầu dòng menu ("" = giữ nguyên; v4: text-muted-foreground).</summary> tự vẽ (DropdownMenu.Trigger): vòng focus theo bo góc của nút.</summary>
    /// <summary>Class thêm cho icon đầu dòng menu ("" = giữ nguyên; v4: text-muted-foreground).</summary>
    public string MenuIcon { get; init; } = "";
    /// <summary>Lớp bọc trong suốt quanh nút mở tự vẽ (DropdownMenu.Trigger): vòng focus theo bo góc của nút.</summary>
    public string MenuTriggerWrapper { get; init; } = "flex rounded-lg " + FocusRing.RingOnly;

    // ── Tooltip ──
    public string TooltipPanel { get; init; } =
        "relative flex max-w-xs items-center gap-1.5 rounded-md bg-foreground px-3 py-1.5 text-xs text-background";
    /// <summary>Mũi tên, chỉ số = (int)placement thực tế / 3.</summary>
    public string[] TooltipArrow { get; init; } =
    [
        "absolute left-1/2 -top-1.5 size-2.5 -translate-x-1/2 rotate-45 rounded-[2px] bg-foreground",
        "absolute left-1/2 -bottom-1.5 size-2.5 -translate-x-1/2 rotate-45 rounded-[2px] bg-foreground",
        "absolute top-1/2 -left-1.5 size-2.5 -translate-y-1/2 rotate-45 rounded-[2px] bg-foreground",
        "absolute top-1/2 -right-1.5 size-2.5 -translate-y-1/2 rotate-45 rounded-[2px] bg-foreground",
    ];
    public float TooltipOffset { get; init; } = 10;

    // ── AlertDialog ── chỉ số theo AlertDialogSize.
    public string[] AlertDialogPanel { get; init; } =
    [
        "flex flex-col gap-4 w-96 max-w-full rounded-xl bg-popover p-4 text-popover-foreground ring-1 ring-foreground/10 shadow-lg",
        "flex flex-col gap-4 w-80 max-w-full rounded-xl bg-popover p-4 text-popover-foreground ring-1 ring-foreground/10 shadow-lg",
    ];
    public string[] AlertDialogHeader { get; init; } = ["flex flex-col gap-1.5", "flex flex-col items-center gap-1.5 text-center"];
    public string AlertDialogMedia { get; init; } = "mb-2 flex size-10 items-center justify-center rounded-md bg-muted";
    public string AlertDialogTitle { get; init; } = "text-base font-medium";
    public string AlertDialogDescription { get; init; } = "text-sm text-muted-foreground";
    public string[] AlertDialogFooter { get; init; } =
    [
        "-mx-4 -mb-4 flex justify-end gap-2 rounded-b-xl border-t border-border bg-muted/50 p-4",
        "-mx-4 -mb-4 grid grid-cols-2 gap-2 rounded-b-xl border-t border-border bg-muted/50 p-4",
    ];
    /// <summary>Lớp phủ sau hộp thoại modal (BackdropClass của overlay).</summary>
    public string ModalBackdrop { get; init; } = "bg-black/50";

    // ── Bubble ── bảng ghép [variant * 2 + phía End].
    public string BubbleContentBase { get; init; } =
        "max-w-full min-w-0 overflow-hidden rounded-xl border border-transparent px-3 py-2 text-sm leading-relaxed transition-colors";
    public string[] BubbleContentVariants { get; init; } =
    [
        "bg-primary text-primary-foreground",
        "bg-secondary text-secondary-foreground",
        "bg-muted text-foreground",
        // tinted dùng oklch(from var(--primary)...) – GotX không có oklch nên xấp xỉ bằng primary pha mờ
        "bg-primary/10 text-foreground dark:bg-primary/15",
        "border-border bg-background text-foreground",
        "rounded-none bg-transparent px-0 py-0 text-foreground",
        "bg-destructive/10 text-destructive dark:bg-destructive/20",
    ];
}
