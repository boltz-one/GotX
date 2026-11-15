namespace GotX.Components;

public static partial class Ui
{
    /// <summary>
    /// Khung giữ tỉ lệ. GotX chưa có <c>aspect-*</c>, nên tính chiều cao từ độ rộng px: <paramref name="ratio"/> = rộng / cao
    /// (16/9f…). Nội dung bên trong nên dùng <c>size-full</c>; phần tràn bị cắt.
    /// </summary>
    public static Div AspectRatio(Window w, float width, float ratio)
    {
        var box = w.Div().Class("relative shrink-0 overflow-hidden");
        box.Style.Width = Length.Px(width);
        box.Style.Height = Length.Px(ratio > 0 ? width / ratio : width);
        return box;
    }
}
