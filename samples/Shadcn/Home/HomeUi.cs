using GotX;
using GotX.Components;

/// <summary>
/// Phần dùng chung của các thẻ trang chủ. Kiểu dáng thuộc về style rhea (<see cref="HomeStyle"/>), nên ở đây chỉ còn các
/// lối tắt cho mẫu lặp lại trong nhiều thẻ của bản gốc.
/// </summary>
static class HomeUi
{
    /// <summary>Nhãn chữ hoa nhỏ ("RETIREMENT", "UPCOMING").</summary>
    public const string Caps = "text-xs font-medium tracking-wider text-muted-foreground uppercase";

    /// <summary>Vòng focus cho phần tử bấm được tự dựng (link, item) – như focus-visible:ring-[3px] ring-ring/50 của item.</summary>
    public const string Ring = "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50";

    /// <summary>Khung thẻ (<c>**:data-[slot=card]:w-full</c> của lưới demo).</summary>
    public static Div Card(Window w) => Ui.Card(w).Class("w-full");

    /// <summary>Đầu thẻ; có nút góc phải thì <paramref name="hasAction"/> = true (thứ tự con: Title → Action → Description).</summary>
    public static Div Header(Window w, bool hasAction = false) => Ui.CardHeader(w, hasAction: hasAction);

    public static Div Content(Window w) => Ui.CardContent(w);

    public static Div Footer(Window w) => Ui.CardFooter(w);

    /// <summary>Item nền muted (khối số liệu, dòng liên kết).</summary>
    public static Div MutedItem(Window w) => Ui.Item(w, ItemVariant.Muted);

    /// <summary>Item bấm được như liên kết (<c>a[data-slot=item]</c>): điểm dừng Tab, nền muted khi rê chuột.</summary>
    public static Div LinkItem(Window w, string key, ItemVariant variant = ItemVariant.Muted) =>
        Ui.Item(w, variant).Class("cursor-pointer hover:bg-muted " + Ring).Key(key).Focusable();

    /// <summary>Ô icon đầu item (ItemMedia variant icon) – item có mô tả thì media bám đỉnh.</summary>
    public static Div Media(Window w, Div icon, bool alignTop = true) => Ui.ItemMedia(w, ItemMediaVariant.Icon, alignTop: alignTop).Child(icon);

    /// <summary>Hai đầu một hàng: nhãn mờ bên trái, giá trị bên phải.</summary>
    public static Div Pair(Window w, string label, string value, string valueClass = "text-sm font-medium") =>
        w.Div().Class("flex items-center justify-between gap-2")
            .Child(w.Div().Class("text-sm text-muted-foreground").Child(label))
            .Child(w.Div().Class(valueClass).Child(value));
}
