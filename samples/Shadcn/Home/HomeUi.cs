using GotX;
using GotX.Components;

/// <summary>
/// Phần dùng chung của các thẻ trang chủ. Trang gốc dùng style "rhea" của shadcn (thẻ bo 24px, đệm 20px, item bo 18px);
/// GotX.Components theo style mặc định, nên các hàm ở đây gọi component rồi nối thêm class khoảng cách/bo góc của bản gốc
/// – một chỗ duy nhất để chỉnh, các thẻ không lặp lại chuỗi class.
/// </summary>
static class HomeUi
{
    /// <summary>Nhãn chữ hoa nhỏ ("RETIREMENT", "UPCOMING", "FORGOT?").</summary>
    public const string Caps = "text-xs font-medium tracking-wider text-muted-foreground uppercase";

    /// <summary>Vòng focus cho phần tử bấm được tự dựng (link, item) – cùng kiểu với component.</summary>
    public const string Ring = "focus-visible:ring-3 focus-visible:ring-ring/50";

    /// <summary>Khung thẻ: <c>[--card-spacing:--spacing(5)]</c>, bo 24px, bóng nhẹ, vòng viền mờ.</summary>
    public static Div Card(Window w) =>
        Ui.Card(w).Class("w-full gap-5 rounded-[24px] py-5 shadow-sm ring-foreground/5 dark:ring-foreground/10");

    /// <summary>Đầu thẻ; có nút góc phải thì <paramref name="hasAction"/> = true (thứ tự con: Title → Action → Description).</summary>
    public static Div Header(Window w, bool hasAction = false) =>
        Ui.CardHeader(w, hasAction: hasAction).Class(hasAction ? "gap-y-2 px-5" : "gap-2 px-5");

    public static Div Content(Window w) => Ui.CardContent(w).Class("px-5");

    // Chân thẻ của style rhea không có vạch kẻ và nền muted như Ui.CardFooter mặc định.
    public static Div Footer(Window w) => Ui.CardFooter(w).Class("border-t-0 bg-transparent px-5 py-0");

    /// <summary>Item nền muted (khối số liệu, dòng liên kết).</summary>
    public static Div MutedItem(Window w) => Ui.Item(w, ItemVariant.Muted).Class("gap-3.5 rounded-2xl px-4 py-3.5");

    /// <summary>Item nền muted xếp dọc (<c>flex-col items-stretch</c> của bản gốc).</summary>
    public static Div MutedStack(Window w) => MutedItem(w).Class("flex-col flex-nowrap items-stretch");

    /// <summary>Item nền muted bấm được như liên kết: điểm dừng Tab, sáng lên khi rê chuột.</summary>
    public static Div LinkItem(Window w, string key) =>
        MutedItem(w).Class("flex-nowrap cursor-pointer hover:bg-muted " + Ring).Key(key).Focusable();

    /// <summary>Ô icon đầu item (ItemMedia variant icon).</summary>
    public static Div Media(Window w, Div icon) => Ui.ItemMedia(w, ItemMediaVariant.Icon, alignTop: true).Child(icon);

    /// <summary>Nút "×" nền muted ở góc thẻ.</summary>
    public static Div Dismiss(Window w, string key) =>
        Ui.Button(w, ButtonVariant.Ghost, ButtonSize.IconSm).Class("bg-muted").Key(key).Child(Icon.X(w));

    /// <summary>Field và nhóm field với khoảng cách của style rhea (gap-3 / gap-6).</summary>
    public static Div Field(Window w) => Ui.Field(w).Class("gap-3");
    public static Div FieldGroup(Window w) => Ui.FieldGroup(w).Class("gap-6");

    /// <summary>Hai đầu một hàng: nhãn mờ bên trái, giá trị bên phải.</summary>
    public static Div Pair(Window w, string label, string value, string valueClass = "text-sm font-medium") =>
        w.Div().Class("flex items-center justify-between gap-2")
            .Child(w.Div().Class("text-sm text-muted-foreground").Child(label))
            .Child(w.Div().Class(valueClass).Child(value));
}
