namespace GotX.Components;

/// <summary>Mật độ của card: Default đệm 16px, Sm đệm 12px.</summary>
public enum CardSize : byte { Default, Sm }

public static partial class Ui
{
    // shadcn dùng biến --card-spacing và has-data-[slot=...]; GotX không có biến CSS lẫn has-*, nên mỗi phần nhận
    // CardSize và tra bảng class dựng sẵn (chỉ số = (int)size).
    static readonly string[] CardRoot =
    [
        "flex flex-col gap-4 overflow-hidden rounded-xl bg-card py-4 text-sm text-card-foreground ring-1 ring-foreground/10",
        "flex flex-col gap-3 overflow-hidden rounded-xl bg-card py-3 text-sm text-card-foreground ring-1 ring-foreground/10",
    ];
    static readonly string[] CardHeaderClasses = ["grid items-start gap-1 rounded-t-xl px-4", "grid items-start gap-1 rounded-t-xl px-3"];
    static readonly string[] CardHeaderActionClasses =
    [
        "grid grid-cols-[1fr_auto] items-start gap-x-2 gap-y-1 rounded-t-xl px-4",
        "grid grid-cols-[1fr_auto] items-start gap-x-2 gap-y-1 rounded-t-xl px-3",
    ];
    static readonly string[] CardTitleClasses = ["text-base leading-snug font-medium", "text-sm leading-snug font-medium"];
    static readonly string[] CardContentClasses = ["px-4", "px-3"];
    static readonly string[] CardFooterClasses =
    [
        "flex items-center rounded-b-xl border-t border-border bg-muted/50 p-4",
        "flex items-center rounded-b-xl border-t border-border bg-muted/50 p-3",
    ];

    /// <summary>
    /// Khung card. Có <see cref="CardFooter"/> thì đặt <paramref name="hasFooter"/> = true: footer tự có đệm và nền riêng
    /// nên khung bỏ đệm đáy (thay cho <c>has-data-[slot=card-footer]:pb-0</c>).
    /// </summary>
    public static Div Card(Window w, CardSize size = CardSize.Default, bool hasFooter = false)
    {
        var card = w.Div().Class(CardRoot[(int)size]);
        return hasFooter ? card.Class("pb-0") : card;
    }

    /// <summary>
    /// Phần đầu card. Có <see cref="CardAction"/> thì đặt <paramref name="hasAction"/> = true và thêm con theo thứ tự
    /// Title → Action → Description: lưới 2 cột tự xếp action vào cột phải, trải hai hàng.
    /// </summary>
    public static Div CardHeader(Window w, CardSize size = CardSize.Default, bool hasAction = false) =>
        w.Div().Class(hasAction ? CardHeaderActionClasses[(int)size] : CardHeaderClasses[(int)size]);

    /// <summary>Tiêu đề card.</summary>
    public static Div CardTitle(Window w, string text, CardSize size = CardSize.Default) =>
        w.Div().Class(CardTitleClasses[(int)size]).Child(text);

    /// <summary>Mô tả phụ dưới tiêu đề.</summary>
    public static Div CardDescription(Window w, string text) => w.Div().Class("text-sm text-muted-foreground").Child(text);

    /// <summary>Ô hành động góc trên phải của header (nút, badge…).</summary>
    public static Div CardAction(Window w) => w.Div().Class("row-span-2 self-start flex justify-end");

    /// <summary>Thân card.</summary>
    public static Div CardContent(Window w, CardSize size = CardSize.Default) => w.Div().Class(CardContentClasses[(int)size]);

    /// <summary>Chân card: kẻ viền trên, nền muted, các con xếp ngang.</summary>
    public static Div CardFooter(Window w, CardSize size = CardSize.Default) => w.Div().Class(CardFooterClasses[(int)size]);
}
