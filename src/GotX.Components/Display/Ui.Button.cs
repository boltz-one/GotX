namespace GotX.Components;

public enum ButtonVariant : byte { Default, Outline, Secondary, Ghost, Destructive, Link }
public enum ButtonSize : byte { Default, Xs, Sm, Lg, Icon, IconXs, IconSm, IconLg }

/// <summary>
/// Component không có state: mỗi hàm trả về một <see cref="Div"/> đã có class, người gọi nối thêm
/// <c>.Child(...)</c>, <c>.OnClick(...)</c>, <c>.Class(...)</c> (class viết sau thắng).
/// </summary>
public static partial class Ui
{
    const string ButtonBase =
        "inline-flex shrink-0 items-center justify-center rounded-lg border border-transparent text-sm font-medium " +
        "whitespace-nowrap cursor-pointer transition-all focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 " +
        "active:translate-y-px disabled:opacity-50";

    static readonly string[] ButtonVariants =
    [
        "bg-primary text-primary-foreground hover:bg-primary/80",
        "border-border bg-background hover:bg-muted hover:text-foreground dark:border-input dark:bg-input/30 dark:hover:bg-input/50",
        "bg-secondary text-secondary-foreground hover:bg-secondary/80",
        "hover:bg-muted hover:text-foreground dark:hover:bg-muted/50",
        "bg-destructive/10 text-destructive hover:bg-destructive/20 focus-visible:border-destructive/40 focus-visible:ring-destructive/20 " +
            "dark:bg-destructive/20 dark:hover:bg-destructive/30 dark:focus-visible:ring-destructive/40",
        "text-primary hover:underline",
    ];

    static readonly string[] ButtonSizes =
    [
        "h-8 gap-1.5 px-2.5", "h-6 gap-1 px-2 text-xs rounded-md", "h-7 gap-1 px-2.5 text-[0.8rem] rounded-md", "h-9 gap-1.5 px-2.5",
        "size-8", "size-6 rounded-md", "size-7 rounded-md", "size-9",
    ];

    // Ghép sẵn mọi tổ hợp variant × size: mỗi frame chỉ tra mảng, không nối chuỗi.
    static readonly string[] ButtonClassTable = BuildButtonClasses();

    static string[] BuildButtonClasses()
    {
        var table = new string[ButtonVariants.Length * ButtonSizes.Length];
        for (int v = 0; v < ButtonVariants.Length; v++)
            for (int s = 0; s < ButtonSizes.Length; s++)
                table[v * ButtonSizes.Length + s] = $"{ButtonBase} {ButtonVariants[v]} {ButtonSizes[s]}";
        return table;
    }

    /// <summary>Chuỗi class của nút – dùng khi muốn một phần tử khác trông như nút.</summary>
    public static string ButtonClasses(ButtonVariant variant = ButtonVariant.Default, ButtonSize size = ButtonSize.Default) =>
        ButtonClassTable[(int)variant * ButtonSizes.Length + (int)size];

    /// <summary>
    /// Nút rỗng: tự thêm nội dung (icon + chữ) bằng <c>.Child(...)</c>. Nút là điểm dừng Tab; Enter/Space gọi
    /// <c>OnClick</c>. <c>.Disabled()</c> bỏ nó khỏi thứ tự Tab. Trong danh sách động nên đặt thêm <c>.Key(...)</c>.
    /// </summary>
    public static Div Button(Window w, ButtonVariant variant = ButtonVariant.Default, ButtonSize size = ButtonSize.Default) =>
        w.Div().Class(ButtonClasses(variant, size)).Focusable();

    public static Div Button(Window w, string label, ButtonVariant variant = ButtonVariant.Default, ButtonSize size = ButtonSize.Default) =>
        Button(w, variant, size).Child(label);
}
