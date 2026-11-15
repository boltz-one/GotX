namespace GotX.Components;

/// <summary>Mảnh ghép chung của các tấm modal (Dialog, Sheet, Drawer): nút đóng và việc nhận focus khi mở.</summary>
internal static class OverlayParts
{
    /// <summary>Nút × ở góc tấm (<paramref name="position"/> là class đặt vị trí): focus được bằng Tab, có vòng focus.</summary>
    public static Div CloseButton(Window w, string key, string position) =>
        Ui.Button(w, ButtonVariant.Ghost, ButtonSize.IconSm).Class(position).Class(FocusRing.Ring)
            .Key(key).Focusable().Child(Icon.X(w));

    /// <summary>
    /// Tấm nhận focus ngay khi mở: phím (Tab, Escape) thuộc về lớp vừa mở chứ không còn ở nút phía sau, và Tab đầu tiên
    /// đi tới phần tử focus được đầu tiên bên trong. Core trả focus về nơi cũ khi lớp đóng.
    /// </summary>
    public static Div TakeFocus(this Div panel, string key) => panel.Key(key).Focusable().AutoFocus();
}
