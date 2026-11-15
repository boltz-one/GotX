using GotX;
using GotX.Components;

/// <summary>
/// Class new-york-v4 mà block tự giữ: ToggleGroup outline dựng từ bảng style hiện hành (block tự lo roving focus và cho
/// phép bỏ chọn như Radix), thanh tab có badge của dashboard (tab của bản gốc chứa Badge – Tabs component chỉ nhận nhãn
/// chữ) và hàng bảng dạng flex (bảng tự giãn cột theo auto table layout). Trạng thái chọn bằng tham số C#.
/// </summary>
static class V4
{
    // ---- ToggleGroup variant="outline" (spacing 0): ghép từ bảng style như ToggleGroup component (outline, cỡ mặc định) ----
    static ComponentStyleSheet? s_toggleSheet;
    static string[] s_toggles = [];

    /// <summary>Class nút thứ <paramref name="index"/> / <paramref name="count"/> của ToggleGroup outline theo style đang dùng.</summary>
    public static string ToggleItem(int index, int count, bool on)
    {
        var sheet = ComponentStyles.Current;
        if (!ReferenceEquals(sheet, s_toggleSheet))
        {
            // Vị trí: 1 đầu, 2 giữa, 3 cuối (0 = đứng riêng) – như ToggleStyles.Position của GotX.Components.
            var t = new string[8];
            for (int p = 0; p < 4; p++)
                for (int pressed = 0; pressed < 2; pressed++)
                    t[p * 2 + pressed] = string.Join(' ', sheet.ToggleBase, sheet.ToggleVariants[1], sheet.ToggleSizes[0],
                        sheet.ToggleOutlineBorders[p], sheet.ToggleRadii[1][p], pressed == 1 ? sheet.TogglePressed : "", sheet.ToggleGroupItem);
            s_toggles = t;
            s_toggleSheet = sheet;
        }
        int position = count == 1 ? 0 : index == 0 ? 1 : index == count - 1 ? 3 : 2;
        return s_toggles[position * 2 + (on ? 1 : 0)];
    }

    /// <summary>Khung nhóm: <c>data-[variant=outline]:shadow-xs</c> của bản gốc (spacing mặc định).</summary>
    public static string ToggleGroupClass => ComponentStyles.Current.ToggleGroup + " w-fit shadow-xs";
    // ---- Tabs ----
    public const string TabsListClass = "inline-flex h-9 w-fit items-center justify-center rounded-lg bg-muted p-[3px] text-muted-foreground";
    const string TabBase =
        "relative inline-flex h-[29px] flex-1 items-center justify-center gap-1.5 rounded-md border border-transparent px-2 py-1 text-sm " +
        "font-medium whitespace-nowrap cursor-pointer transition-all hover:text-foreground focus-visible:border-ring focus-visible:ring-3 " +
        "focus-visible:ring-ring/50 dark:hover:text-foreground "; // outline-1 của bản gốc nằm dưới vòng ring 3px: không thấy trong ảnh tham chiếu
    public const string TabTrigger = TabBase + "text-foreground/60 dark:text-muted-foreground";
    public const string TabTriggerActive = TabBase + "bg-background text-foreground shadow-sm dark:border-input dark:bg-input/30 dark:text-foreground";

    // ---- Table ----
    public const string TableRow = "flex border-b border-border transition-colors hover:bg-muted/50";
    /// <summary>Hàng được chọn: <c>data-[state=selected]:bg-muted</c> thắng <c>hover:bg-muted/50</c> (cùng độ ưu tiên, đứng sau).</summary>
    public const string TableRowSelected = "flex border-b border-border bg-muted transition-colors";
    public const string TableHead = "flex h-10 items-center px-2 text-left font-medium whitespace-nowrap text-foreground";
    public const string TableCell = "flex items-center p-2 whitespace-nowrap";

}
