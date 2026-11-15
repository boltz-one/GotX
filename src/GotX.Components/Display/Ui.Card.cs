namespace GotX.Components;

/// <summary>Mật độ của card: Default đệm 16px, Sm đệm 12px (style rhea: 20px / 16px).</summary>
public enum CardSize : byte { Default, Sm }

public static partial class Ui
{
    // shadcn dùng biến --card-spacing và has-data-[slot=...]; GotX không có biến CSS lẫn has-*, nên mỗi phần nhận
    // CardSize và tra bảng class của style (chỉ số = (int)size).

    /// <summary>
    /// Khung card. Có <see cref="CardFooter"/> thì đặt <paramref name="hasFooter"/> = true: ở style mặc định footer tự có
    /// đệm và nền riêng nên khung bỏ đệm đáy (thay cho <c>has-data-[slot=card-footer]:pb-0</c>).
    /// </summary>
    public static Div Card(Window w, CardSize size = CardSize.Default, bool hasFooter = false)
    {
        var s = ComponentStyles.S;
        var card = w.Div().Class(s.CardRoot[(int)size]);
        return hasFooter && s.CardWithFooter.Length > 0 ? card.Class(s.CardWithFooter) : card;
    }

    /// <summary>
    /// Phần đầu card. Có <see cref="CardAction"/> thì đặt <paramref name="hasAction"/> = true và thêm con theo thứ tự
    /// Title → Action → Description: lưới 2 cột tự xếp action vào cột phải, trải hai hàng.
    /// </summary>
    public static Div CardHeader(Window w, CardSize size = CardSize.Default, bool hasAction = false)
    {
        var s = ComponentStyles.S;
        return w.Div().Class(hasAction ? s.CardHeaderWithAction[(int)size] : s.CardHeader[(int)size]);
    }

    /// <summary>Tiêu đề card.</summary>
    public static Div CardTitle(Window w, string text, CardSize size = CardSize.Default) =>
        w.Div().Class(ComponentStyles.S.CardTitle[(int)size]).Child(text);

    /// <summary>Mô tả phụ dưới tiêu đề.</summary>
    public static Div CardDescription(Window w, string text) => w.Div().Class(ComponentStyles.S.CardDescription).Child(text);

    /// <summary>Ô hành động góc trên phải của header (nút, badge…).</summary>
    public static Div CardAction(Window w) => w.Div().Class(ComponentStyles.S.CardAction);

    /// <summary>Thân card.</summary>
    public static Div CardContent(Window w, CardSize size = CardSize.Default) => w.Div().Class(ComponentStyles.S.CardContent[(int)size]);

    /// <summary>Chân card (style mặc định: kẻ viền trên, nền muted), các con xếp ngang.</summary>
    public static Div CardFooter(Window w, CardSize size = CardSize.Default) => w.Div().Class(ComponentStyles.S.CardFooter[(int)size]);
}