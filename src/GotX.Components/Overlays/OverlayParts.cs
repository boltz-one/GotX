namespace GotX.Components;

/// <summary>Mảnh ghép chung của các tấm modal (Dialog, Sheet, Drawer): nút đóng và việc nhận focus khi mở.</summary>
internal static class OverlayParts
{
    /// <summary>Nút × ở góc tấm (<paramref name="position"/> là class đặt vị trí): focus được bằng Tab, có vòng focus.</summary>
    public static Div CloseButton(Window w, string key, string position)
    {
        // Style có nút × riêng (v4: icon mờ 70%, không nền) thì dùng nó, không thì nút ghost icon-sm.
        string own = ComponentStyles.S.OverlayClose;
        var button = own.Length > 0 ? w.Div().Class(own) : Ui.Button(w, ButtonVariant.Ghost, ButtonSize.IconSm);
        return button.Class(position).Class(FocusRing.Ring).Key(key).Focusable().Role(SemanticRole.Button).AriaLabel("Close").Child(Icon.X(w));
    }

    /// <summary>
    /// Tấm nhận focus ngay khi mở: phím (Tab, Escape) thuộc về lớp vừa mở chứ không còn ở nút phía sau, và Tab đầu tiên
    /// đi tới phần tử focus được đầu tiên bên trong. Core trả focus về nơi cũ khi lớp đóng.
    /// </summary>
    public static Div TakeFocus(this Div panel, string key) => panel.Key(key).Focusable().AutoFocus();
}
