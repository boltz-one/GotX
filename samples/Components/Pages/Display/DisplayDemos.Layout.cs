using GotX;
using GotX.Components;

/// <summary>Các mục demo của trang Display (card, item, empty, các phần tử nhỏ).</summary>
static partial class DisplayDemos
{
    /// <summary>Card đầy đủ các phần, card có action, card cỡ nhỏ.</summary>
    public static Div Cards(Window w) =>
        Demo.Section(w, "Card", "Header / Title / Description / Action / Content / Footer; cỡ Default và Sm").Child(Demo.Row(w).Class("items-start")
            .Child(Ui.Card(w, hasFooter: true).Class("w-80").Key("card")
                .Child(Ui.CardHeader(w, hasAction: true).Key("card-header")
                    .Child(Ui.CardTitle(w, "Đăng nhập").Key("card-title"))
                    .Child(Ui.CardAction(w).Key("card-action").Child(Ui.Button(w, "Đăng ký", ButtonVariant.Link)))
                    .Child(Ui.CardDescription(w, "Nhập email để đăng nhập vào tài khoản.").Key("card-desc")))
                .Child(Ui.CardContent(w).Key("card-content").Child(w.Div().Class("flex flex-col gap-2")
                    .Child(Ui.Skeleton(w).Class("h-8"))
                    .Child(Ui.Skeleton(w).Class("h-8"))))
                .Child(Ui.CardFooter(w).Key("card-footer").Child(Ui.Button(w, "Đăng nhập").Class("w-full"))))
            .Child(Ui.Card(w, CardSize.Sm).Class("w-64").Key("card-sm")
                .Child(Ui.CardHeader(w, CardSize.Sm).Key("card-sm-header")
                    .Child(Ui.CardTitle(w, "Thông báo", CardSize.Sm))
                    .Child(Ui.CardDescription(w, "Bạn có 3 tin nhắn chưa đọc.")))
                .Child(Ui.CardContent(w, CardSize.Sm).Child(w.Div().Class("text-sm").Child("Card cỡ nhỏ: đệm 12px, tiêu đề nhỏ hơn."))))
            .Child(Ui.Card(w).Class("w-56")
                .Child(Ui.CardHeader(w).Child(Ui.CardTitle(w, "Doanh thu")).Child(Ui.CardDescription(w, "Tháng này")))
                .Child(Ui.CardContent(w).Child(w.Div().Class("text-2xl font-semibold").Child("45.231.000 ₫")))));

    /// <summary>Item ba variant, media dạng icon/ảnh, nhóm item có vạch ngăn.</summary>
    public static Div Items(Window w) =>
        Demo.Section(w, "Item", "Default / Outline / Muted; media Icon và Image; cỡ Xs; nhóm có vạch ngăn").Child(w.Div().Class("flex flex-col gap-3 max-w-xl")
            .Child(Ui.Item(w, ItemVariant.Outline).Key("item-outline")
                .Child(Ui.ItemMedia(w, ItemMediaVariant.Icon).Key("item-media").Child(Icon.Check(w)))
                .Child(Ui.ItemContent(w).Key("item-content")
                    .Child(Ui.ItemTitle(w, "Đã xác minh hồ sơ"))
                    .Child(Ui.ItemDescription(w, "Tài khoản của bạn đã được xác minh.")))
                .Child(Ui.ItemActions(w).Key("item-actions").Child(Ui.Button(w, "Xem", ButtonVariant.Outline, ButtonSize.Sm))))
            .Child(Ui.Item(w, ItemVariant.Muted).Key("item-muted")
                .Child(Ui.ItemMedia(w, ItemMediaVariant.Image, alignTop: true).Child(Ui.Avatar(w, DemoImages.Portrait, "MH", AvatarSize.Lg)))
                .Child(Ui.ItemContent(w)
                    .Child(Ui.ItemTitle(w, "Minh Hà"))
                    .Child(Ui.ItemDescription(w, "Đã bình luận về bài viết của bạn: “Giao diện rất gọn, chạy mượt trên máy cũ.”")))
                .Child(Ui.ItemActions(w).Child(Ui.Button(w, ButtonVariant.Ghost, ButtonSize.IconSm).Child(Icon.MoreHorizontal(w)))))
            .Child(Ui.ItemGroup(w, ItemSize.Xs)
                .Child(Ui.Item(w, size: ItemSize.Xs)
                    .Child(Ui.ItemContent(w, ItemSize.Xs).Child(Ui.ItemTitle(w, "Thông báo email"))
                        .Child(Ui.ItemDescription(w, "Cỡ Xs: chữ mô tả nhỏ.", ItemSize.Xs)))
                    .Child(Ui.ItemActions(w).Child(Ui.Badge(w, "Bật"))))
                .Child(Ui.ItemSeparator(w))
                .Child(Ui.Item(w, size: ItemSize.Xs)
                    .Child(Ui.ItemContent(w, ItemSize.Xs).Child(Ui.ItemTitle(w, "Thông báo đẩy"))
                        .Child(Ui.ItemDescription(w, "Gửi tới thiết bị di động.", ItemSize.Xs)))
                    .Child(Ui.ItemActions(w).Child(Ui.Badge(w, "Tắt", BadgeVariant.Outline))))));

    /// <summary>Trạng thái rỗng có icon, tiêu đề, mô tả và nút.</summary>
    public static Div Empties(Window w) =>
        Demo.Section(w, "Empty", "Khối \"chưa có dữ liệu\" căn giữa, viền nét đứt").Child(
            Ui.Empty(w).Class("border border-border").Key("empty")
                .Child(Ui.EmptyHeader(w)
                    .Child(Ui.EmptyMedia(w, EmptyMediaVariant.Icon).Key("empty-media").Child(Icon.Folder(w)))
                    .Child(Ui.EmptyTitle(w, "Chưa có dự án").Key("empty-title"))
                    .Child(Ui.EmptyDescription(w, "Bạn chưa tạo dự án nào. Bắt đầu bằng cách tạo dự án đầu tiên.")))
                .Child(Ui.EmptyContent(w).Child(w.Div().Class("flex gap-2")
                    .Child(Ui.Button(w, "Tạo dự án"))
                    .Child(Ui.Button(w, "Nhập dự án", ButtonVariant.Outline)))));

    /// <summary>Kbd, separator, skeleton, spinner, aspect ratio.</summary>
    public static Div Small(Window w) =>
        Demo.Section(w, "Kbd · Separator · Skeleton · Spinner · Aspect Ratio").Child(w.Div().Class("flex flex-col gap-4")
            .Child(Demo.Row(w)
                .Child(Ui.KbdGroup(w).Child(Ui.Kbd(w, "Ctrl").Key("kbd")).Child(Ui.Kbd(w, "K")))
                .Child(Ui.Kbd(w).Child(Icon.CornerDownLeft(w).Class("size-3")))
                .Child(Ui.Kbd(w, "Shift"))
                .Child(Ui.Kbd(w, "Enter"))
                .Child(w.Div().Class("flex h-5 items-center gap-3 text-sm").Key("sep-row")
                    .Child("Blog").Child(Ui.Separator(w, SeparatorOrientation.Vertical).Key("sep-v")).Child("Tài liệu")
                    .Child(Ui.Separator(w, SeparatorOrientation.Vertical)).Child("Mã nguồn")))
            .Child(Ui.Separator(w).Key("sep-h"))
            .Child(Demo.Row(w)
                .Child(Ui.Skeleton(w).Class("size-12 rounded-full").Key("skeleton"))
                .Child(w.Div().Class("flex flex-col gap-2").Child(Ui.Skeleton(w).Class("h-4 w-60")).Child(Ui.Skeleton(w).Class("h-4 w-48")))
                .Child(Ui.Spinner(w).Key("spinner"))
                .Child(Ui.Spinner(w, 24).Class("text-muted-foreground"))
                .Child(Ui.Badge(w, BadgeVariant.Secondary).Child(Ui.Spinner(w, 12)).Child("Đang đồng bộ"))
                .Child(Ui.Button(w, ButtonVariant.Outline).Disabled().Child(Ui.Spinner(w)).Child("Đang tải")))
            .Child(Demo.Row(w).Class("items-start")
                .Child(Ui.AspectRatio(w, 320, 16 / 9f).Class("rounded-lg bg-muted flex items-center justify-center").Key("ratio-16-9")
                    .Child(w.Div().Class("text-sm text-muted-foreground").Child("16 : 9")))
                .Child(Ui.AspectRatio(w, 120, 1).Class("rounded-lg bg-muted flex items-center justify-center").Key("ratio-1-1")
                    .Child(w.Div().Class("text-sm text-muted-foreground").Child("1 : 1")))));
}
