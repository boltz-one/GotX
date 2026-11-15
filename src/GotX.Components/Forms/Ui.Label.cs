namespace GotX.Components;

public static partial class Ui
{
    const string LabelClass = "flex items-center gap-2 text-sm leading-none font-medium";
    // Nhãn bấm được cao 14px (leading-none): thêm đệm 1px trên dưới và bù bằng margin âm, vùng bấm đủ 16px mà bố cục không đổi.
    const string ClickableLabelClass = LabelClass + " -my-px py-px cursor-pointer";

    /// <summary>Nhãn tĩnh (label.tsx). Muốn bấm nhãn thì focus ô nhập, dùng <see cref="Label(Window, string, TextEntry)"/>.</summary>
    public static Div Label(Window w, string text) => w.Div().Class(LabelClass).Child(text);

    /// <summary>Nhãn gắn với một ô nhập như <c>htmlFor</c>: bấm vào nhãn thì ô nhận focus.</summary>
    public static Div Label(Window w, string text, TextEntry control) =>
        w.Div().Class(ClickableLabelClass).OnClick(control, static (c, _) => c.Focus()).Child(text);

    /// <summary>Nhãn rỗng để tự ghép nội dung (icon + chữ).</summary>
    public static Div Label(Window w) => w.Div().Class(LabelClass);
}
