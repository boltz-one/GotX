namespace GotX.Components;

public enum ButtonVariant : byte { Default, Outline, Secondary, Ghost, Destructive, Link }
public enum ButtonSize : byte { Default, Xs, Sm, Lg, Icon, IconXs, IconSm, IconLg }

/// <summary>
/// Component không có state: mỗi hàm trả về một <see cref="Div"/> đã có class, người gọi nối thêm
/// <c>.Child(...)</c>, <c>.OnClick(...)</c>, <c>.Class(...)</c> (class viết sau thắng). Class lấy từ bảng của style đang
/// hiệu lực (<see cref="ComponentStyles"/>), ghép sẵn mọi tổ hợp variant × size: mỗi frame chỉ tra mảng, không nối chuỗi.
/// </summary>
public static partial class Ui
{
    /// <summary>Chuỗi class của nút – dùng khi muốn một phần tử khác trông như nút.</summary>
    public static string ButtonClasses(ButtonVariant variant = ButtonVariant.Default, ButtonSize size = ButtonSize.Default)
    {
        var s = ComponentStyles.S;
        return s.ButtonTable[(int)variant * s.ButtonSizes.Length + (int)size];
    }

    /// <summary>
    /// Nút rỗng: tự thêm nội dung (icon + chữ) bằng <c>.Child(...)</c>. Nút là điểm dừng Tab; Enter/Space gọi
    /// <c>OnClick</c>. <c>.Disabled()</c> bỏ nó khỏi thứ tự Tab. Trong danh sách động nên đặt thêm <c>.Key(...)</c>.
    /// </summary>
    public static Div Button(Window w, ButtonVariant variant = ButtonVariant.Default, ButtonSize size = ButtonSize.Default) =>
        w.Div().Class(ButtonClasses(variant, size)).Focusable();

    public static Div Button(Window w, string label, ButtonVariant variant = ButtonVariant.Default, ButtonSize size = ButtonSize.Default) =>
        Button(w, variant, size).Child(label);
}