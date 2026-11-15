namespace GotX.Components;

/// <summary>Kiểu hiển thị của <see cref="Ui.Badge(Window, BadgeVariant)"/>.</summary>
public enum BadgeVariant : byte { Default, Secondary, Destructive, Outline, Ghost, Link }

public static partial class Ui
{
    // Bỏ w-fit (GotX coi như auto) và các variant aria-*/[a]: vì Core chưa có. Vòng focus chỉ hiện khi người gọi
    // biến badge thành phần tử bấm được bằng <c>.Focusable()</c> (badge dạng liên kết).

    /// <summary>Chuỗi class của badge – dùng khi muốn một phần tử khác trông như badge.</summary>
    public static string BadgeClasses(BadgeVariant variant = BadgeVariant.Default) => ComponentStyles.S.BadgeTable[(int)variant];

    /// <summary>Badge rỗng: tự thêm icon/chữ bằng <c>.Child(...)</c>.</summary>
    public static Div Badge(Window w, BadgeVariant variant = BadgeVariant.Default) => w.Div().Class(BadgeClasses(variant));

    /// <summary>Badge có sẵn nhãn chữ.</summary>
    public static Div Badge(Window w, string label, BadgeVariant variant = BadgeVariant.Default) => Badge(w, variant).Child(label);
}