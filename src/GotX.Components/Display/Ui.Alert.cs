namespace GotX.Components;

/// <summary>Kiểu của <see cref="Ui.Alert(Window, AlertVariant, bool, bool)"/>.</summary>
public enum AlertVariant : byte { Default, Destructive }

public static partial class Ui
{
    const string AlertBase = "relative grid w-full gap-0.5 rounded-lg border border-border px-2.5 py-2 text-left text-sm";

    // Chỉ số = variant * 4 + (icon ? 2 : 0) + (action ? 1 : 0). Thay cho has-[>svg]: / has-data-[slot=alert-action]:
    // mà GotX không có: người gọi khai báo alert có icon/action hay không.
    static readonly string[] AlertClassTable = BuildAlertClasses();

    static string[] BuildAlertClasses()
    {
        string[] variants = ["bg-card text-card-foreground", "bg-card text-destructive"];
        var table = new string[8];
        for (int v = 0; v < 2; v++)
            for (int k = 0; k < 4; k++)
                table[v * 4 + k] = AlertBase + " " + variants[v]
                    + ((k & 2) != 0 ? " grid-cols-[auto_1fr] gap-x-2" : "")
                    + ((k & 1) != 0 ? " pr-18" : "");
        return table;
    }

    /// <summary>
    /// Khung thông báo. Thêm con theo thứ tự <see cref="AlertIcon"/> (nếu <paramref name="hasIcon"/>) → Title →
    /// Description; <see cref="AlertAction"/> thêm cuối cùng (nếu <paramref name="hasAction"/>, chừa lề phải cho nó).
    /// </summary>
    public static Div Alert(Window w, AlertVariant variant = AlertVariant.Default, bool hasIcon = false, bool hasAction = false) =>
        w.Div().Class(AlertClassTable[(int)variant * 4 + (hasIcon ? 2 : 0) + (hasAction ? 1 : 0)]).Role(SemanticRole.Alert);

    /// <summary>Ô icon cột trái, trải hai hàng (tiêu đề + mô tả); icon lấy màu chữ của alert.</summary>
    public static Div AlertIcon(Window w, Div icon) => w.Div().Class("row-span-2 translate-y-0.5").Child(icon);

    /// <summary>Tiêu đề alert.</summary>
    public static Div AlertTitle(Window w, string text) => w.Div().Class("font-medium").Child(text);

    /// <summary>
    /// Mô tả. Variant Destructive đổi chữ sang đỏ mờ – thay cho <c>*:data-[slot=alert-description]:</c> của shadcn.
    /// </summary>
    public static Div AlertDescription(Window w, string text, AlertVariant variant = AlertVariant.Default) =>
        w.Div().Class(variant == AlertVariant.Destructive ? "text-sm text-destructive/90" : "text-sm text-muted-foreground").Child(text);

    /// <summary>Hành động ở góc trên phải (absolute, nằm ngoài lưới).</summary>
    public static Div AlertAction(Window w) => w.Div().Class("absolute top-2 right-2");
}
