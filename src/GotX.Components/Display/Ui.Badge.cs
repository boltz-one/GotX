namespace GotX.Components;

/// <summary>Kiểu hiển thị của <see cref="Ui.Badge(Window, BadgeVariant)"/>.</summary>
public enum BadgeVariant : byte { Default, Secondary, Destructive, Outline, Ghost, Link }

public static partial class Ui
{
    // Bỏ w-fit (GotX coi như auto) và các variant aria-*/[a]: vì Core chưa có. Vòng focus chỉ hiện khi người gọi
    // biến badge thành phần tử bấm được bằng <c>.Focusable()</c> (badge dạng liên kết).
    const string BadgeBase =
        "inline-flex h-5 shrink-0 items-center justify-center gap-1 overflow-hidden rounded-4xl border border-transparent " +
        "px-2 py-0.5 text-xs font-medium whitespace-nowrap transition-all " +
        "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50";

    static readonly string[] BadgeClassTable =
    [
        BadgeBase + " bg-primary text-primary-foreground",
        BadgeBase + " bg-secondary text-secondary-foreground",
        BadgeBase + " bg-destructive/10 text-destructive focus-visible:ring-destructive/20 dark:bg-destructive/20 dark:focus-visible:ring-destructive/40",
        BadgeBase + " border-border text-foreground",
        BadgeBase + " hover:bg-muted hover:text-muted-foreground dark:hover:bg-muted/50",
        BadgeBase + " text-primary hover:underline",
    ];

    /// <summary>Chuỗi class của badge – dùng khi muốn một phần tử khác trông như badge.</summary>
    public static string BadgeClasses(BadgeVariant variant = BadgeVariant.Default) => BadgeClassTable[(int)variant];

    /// <summary>Badge rỗng: tự thêm icon/chữ bằng <c>.Child(...)</c>.</summary>
    public static Div Badge(Window w, BadgeVariant variant = BadgeVariant.Default) => w.Div().Class(BadgeClasses(variant));

    /// <summary>Badge có sẵn nhãn chữ.</summary>
    public static Div Badge(Window w, string label, BadgeVariant variant = BadgeVariant.Default) => Badge(w, variant).Child(label);
}
