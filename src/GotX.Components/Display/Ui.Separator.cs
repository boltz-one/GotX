namespace GotX.Components;

/// <summary>Hướng của đường kẻ / nhóm nút (tên riêng để không đụng enum hướng của nhóm khác khi gộp).</summary>
public enum SeparatorOrientation : byte { Horizontal, Vertical }

public static partial class Ui
{
    const string SeparatorHorizontal = "shrink-0 bg-border h-px w-full";
    const string SeparatorVertical = "shrink-0 bg-border w-px self-stretch";

    /// <summary>
    /// Đường kẻ 1px màu border. Bản dọc không có chiều cao riêng: nó giãn theo hàng flex chứa nó nhờ
    /// <c>self-stretch</c> (thắng <c>items-center</c> của cha), giống shadcn.
    /// </summary>
    public static Div Separator(Window w, SeparatorOrientation orientation = SeparatorOrientation.Horizontal) =>
        w.Div().Class(orientation == SeparatorOrientation.Horizontal ? SeparatorHorizontal : SeparatorVertical);
}
