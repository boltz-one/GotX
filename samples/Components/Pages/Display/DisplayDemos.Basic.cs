using GotX;
using GotX.Components;

/// <summary>Các mục demo của trang Display (badge, avatar, nhóm nút, alert).</summary>
static partial class DisplayDemos
{
    /// <summary>Mọi variant của badge, kèm badge có icon.</summary>
    public static Div Badges(Window w) =>
        Demo.Section(w, "Badge", "Default, Secondary, Destructive, Outline, Ghost, Link").Child(Demo.Row(w)
            .Child(Ui.Badge(w, "Mặc định").Key("badge-default"))
            .Child(Ui.Badge(w, "Phụ", BadgeVariant.Secondary).Key("badge-secondary"))
            .Child(Ui.Badge(w, "Lỗi", BadgeVariant.Destructive).Key("badge-destructive"))
            .Child(Ui.Badge(w, "Viền", BadgeVariant.Outline).Key("badge-outline"))
            .Child(Ui.Badge(w, "Ghost", BadgeVariant.Ghost))
            .Child(Ui.Badge(w, BadgeVariant.Outline).Class("pr-1.5").Child("Mở").Child(Icon.ExternalLink(w).Class("size-3")))
            .Child(Ui.Badge(w, "Liên kết", BadgeVariant.Link))
            .Child(Ui.Badge(w, BadgeVariant.Secondary).Class("pl-1.5").Child(Icon.Check(w).Class("size-3")).Child("Đã xác minh"))
            .Child(Ui.Badge(w, "8").Class("min-w-5 rounded-full px-1")));

    /// <summary>Ba kích thước avatar, chấm trạng thái, nhóm avatar có ô đếm.</summary>
    public static Div Avatars(Window w) =>
        Demo.Section(w, "Avatar", "Ảnh cắt tròn (object-cover) hoặc chữ viết tắt khi không có ảnh; 3 kích thước, badge trạng thái, nhóm chồng nhau").Child(Demo.Row(w)
            .Child(Ui.Avatar(w, DemoImages.Portrait, "AN", AvatarSize.Sm))
            .Child(Ui.Avatar(w, DemoImages.Portrait, "AN").Key("avatar-image"))
            .Child(Ui.Avatar(w, DemoImages.Landscape, "PC", AvatarSize.Lg).Child(Ui.AvatarBadge(w, AvatarSize.Lg).Class("bg-green-600")))
            .Child(Ui.Avatar(w, null, "NA").Key("avatar-fallback"))
            .Child(Ui.Separator(w, SeparatorOrientation.Vertical))
            .Child(Ui.Avatar(w, "NA", AvatarSize.Sm).Key("avatar-sm"))
            .Child(Ui.Avatar(w, "TB").Key("avatar-default"))
            .Child(Ui.Avatar(w, "LC", AvatarSize.Lg).Key("avatar-lg"))
            .Child(Ui.Avatar(w, "PD").Child(Ui.AvatarBadge(w).Class("bg-green-600").Key("avatar-badge")).Key("avatar-with-badge"))
            .Child(Ui.Avatar(w, "VE", AvatarSize.Lg).Child(Ui.AvatarBadge(w, AvatarSize.Lg)))
            .Child(Ui.AvatarGroup(w,
                Ui.Avatar(w, DemoImages.Portrait, "AN").Key("group-a"), Ui.Avatar(w, "BT").Key("group-b"), Ui.Avatar(w, DemoImages.Landscape, "CH"),
                Ui.AvatarGroupCount(w, "+3")))
            .Child(Ui.AvatarGroup(w,
                Ui.Avatar(w, "A", AvatarSize.Sm), Ui.Avatar(w, "B", AvatarSize.Sm), Ui.AvatarGroupCount(w, "+5", AvatarSize.Sm))));

    /// <summary>Nhóm nút ngang/dọc, có ô chữ và vạch ngăn.</summary>
    public static Div ButtonGroups(Window w) =>
        Demo.Section(w, "Button Group", "Nút dính liền: ngang, dọc, kèm ô chữ và vạch ngăn").Child(Demo.Row(w).Class("items-start")
            .Child(Ui.ButtonGroup(w,
                Ui.Button(w, "Lưu trữ", ButtonVariant.Outline).Key("bg-first"),
                Ui.Button(w, "Báo cáo", ButtonVariant.Outline).Key("bg-mid"),
                Ui.Button(w, "Xoá", ButtonVariant.Outline).Key("bg-last")).Key("bg-row"))
            .Child(Ui.ButtonGroup(w,
                Ui.ButtonGroupText(w, "https://"),
                Ui.Button(w, "gotx.dev", ButtonVariant.Outline)))
            .Child(Ui.ButtonGroup(w,
                Ui.Button(w, "Sao chép", ButtonVariant.Secondary),
                Ui.ButtonGroupSeparator(w).Key("bg-sep"),
                Ui.Button(w, ButtonVariant.Secondary, ButtonSize.Icon).Child(Icon.ChevronDown(w))))
            .Child(Ui.ButtonGroup(w, SeparatorOrientation.Vertical,
                Ui.Button(w, ButtonVariant.Outline, ButtonSize.Icon).Child(Icon.ChevronUp(w)).Key("bgv-first"),
                Ui.Button(w, ButtonVariant.Outline, ButtonSize.Icon).Child(Icon.ChevronDown(w)).Key("bgv-last"))));

    /// <summary>Alert hai variant, có/không icon, có hành động.</summary>
    public static Div Alerts(Window w) =>
        Demo.Section(w, "Alert", "Default và Destructive; icon trải hai hàng; hành động góc phải").Child(w.Div().Class("flex flex-col gap-3")
            .Child(Ui.Alert(w, hasIcon: true).Key("alert-icon")
                .Child(Ui.AlertIcon(w, Icon.CircleCheck(w)).Key("alert-icon-box"))
                .Child(Ui.AlertTitle(w, "Đã lưu thay đổi").Key("alert-title"))
                .Child(Ui.AlertDescription(w, "Hồ sơ của bạn đã được cập nhật và đồng bộ.").Key("alert-desc")))
            .Child(Ui.Alert(w, AlertVariant.Destructive, hasIcon: true).Key("alert-destructive")
                .Child(Ui.AlertIcon(w, Icon.AlertCircle(w)))
                .Child(Ui.AlertTitle(w, "Thanh toán thất bại"))
                .Child(Ui.AlertDescription(w, "Thẻ của bạn bị từ chối. Hãy kiểm tra thông tin và thử lại.", AlertVariant.Destructive)))
            .Child(Ui.Alert(w, hasAction: true).Key("alert-action")
                .Child(Ui.AlertTitle(w, "Có bản cập nhật mới"))
                .Child(Ui.AlertDescription(w, "Phiên bản 2.4 đã sẵn sàng."))
                .Child(Ui.AlertAction(w).Key("alert-action-box").Child(Ui.Button(w, "Cập nhật", size: ButtonSize.Xs)))));
}
