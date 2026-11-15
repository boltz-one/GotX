using GotX;
using GotX.Components;

/// <summary>
/// View thử nghiệm nhỏ cho nhóm Display: mỗi component đặt ở vị trí cố định, đủ gọn để nằm trong cửa sổ 900×700
/// nên đo được cả hộp layout lẫn màu pixel.
/// </summary>
sealed class DisplayProbe : View
{
    public override Div Render(Window w) =>
        w.Div().Class("size-full flex flex-col items-start gap-4 p-6 bg-background text-foreground")
            .Child(w.Div().Class("flex items-center gap-3")
                .Child(Ui.Badge(w, "Mặc định").Key("p-badge-default"))
                .Child(Ui.Badge(w, "Phụ", BadgeVariant.Secondary).Key("p-badge-secondary"))
                .Child(Ui.Badge(w, "Viền", BadgeVariant.Outline).Key("p-badge-outline"))
                .Child(Ui.Badge(w, "Lỗi", BadgeVariant.Destructive).Key("p-badge-destructive"))
                .Child(Ui.Kbd(w, "K").Key("p-kbd"))
                .Child(Ui.Spinner(w).Key("p-spinner"))
                .Child(Ui.Spinner(w, 24).Key("p-spinner-24"))
                .Child(Ui.Skeleton(w).Class("h-6 w-24").Key("p-skeleton")))
            .Child(w.Div().Class("flex items-center gap-3")
                .Child(Ui.Avatar(w, "S", AvatarSize.Sm).Key("p-avatar-sm"))
                .Child(Ui.Avatar(w, "D").Key("p-avatar").Child(Ui.AvatarBadge(w).Key("p-avatar-badge")))
                .Child(Ui.Avatar(w, "L", AvatarSize.Lg).Key("p-avatar-lg"))
                .Child(Ui.AvatarGroup(w, Ui.Avatar(w, "A").Key("p-group-a"), Ui.Avatar(w, "B").Key("p-group-b"),
                    Ui.AvatarGroupCount(w, "+2").Key("p-group-count"))))
            .Child(w.Div().Class("flex items-start gap-3")
                .Child(Ui.ButtonGroup(w,
                    Ui.Button(w, "Một", ButtonVariant.Outline).Key("p-bg-1"),
                    Ui.Button(w, "Hai", ButtonVariant.Outline).Key("p-bg-2"),
                    Ui.Button(w, "Ba", ButtonVariant.Outline).Key("p-bg-3")))
                .Child(Ui.ButtonGroup(w, SeparatorOrientation.Vertical,
                    Ui.Button(w, ButtonVariant.Outline, ButtonSize.Icon).Child(Icon.ChevronUp(w).Key("p-chev")).Key("p-bgv-1"),
                    Ui.Button(w, ButtonVariant.Outline, ButtonSize.Icon).Child(Icon.ChevronDown(w)).Key("p-bgv-2")))
                .Child(w.Div().Class("flex h-10 items-center gap-2").Key("p-sep-row")
                    .Child("a").Child(Ui.Separator(w, SeparatorOrientation.Vertical).Key("p-sep-v")).Child("b"))
                .Child(Ui.AspectRatio(w, 160, 16 / 9f).Class("bg-muted").Key("p-ratio")))
            .Child(w.Div().Class("w-[420px]").Key("p-sep-box").Child(Ui.Separator(w).Key("p-sep-h")))
            .Child(w.Div().Class("flex items-start gap-3")
                .Child(Ui.Card(w, hasFooter: true).Class("w-72").Key("p-card")
                    .Child(Ui.CardHeader(w, hasAction: true).Key("p-card-header")
                        .Child(Ui.CardTitle(w, "Tiêu đề").Key("p-card-title"))
                        .Child(Ui.CardAction(w).Key("p-card-action").Child(Ui.Badge(w, "Mới")))
                        .Child(Ui.CardDescription(w, "Mô tả").Key("p-card-desc")))
                    .Child(Ui.CardContent(w).Key("p-card-content").Child("Nội dung"))
                    .Child(Ui.CardFooter(w).Key("p-card-footer").Child("Chân")))
                .Child(Ui.Card(w, CardSize.Sm).Class("w-48").Key("p-card-sm")
                    .Child(Ui.CardHeader(w, CardSize.Sm).Key("p-card-sm-header").Child(Ui.CardTitle(w, "Nhỏ", CardSize.Sm)))))
            .Child(w.Div().Class("w-[560px] flex flex-col gap-2")
                .Child(Ui.Alert(w, hasIcon: true, hasAction: true).Key("p-alert")
                    .Child(Ui.AlertIcon(w, Icon.Check(w)).Key("p-alert-icon"))
                    .Child(Ui.AlertTitle(w, "Tiêu đề").Key("p-alert-title"))
                    .Child(Ui.AlertDescription(w, "Mô tả").Key("p-alert-desc"))
                    .Child(Ui.AlertAction(w).Key("p-alert-action").Child(Ui.Button(w, "OK", size: ButtonSize.Xs))))
                .Child(Ui.Item(w, ItemVariant.Outline).Key("p-item")
                    .Child(Ui.ItemMedia(w, ItemMediaVariant.Image).Key("p-item-media").Child(Ui.Avatar(w, "M")))
                    .Child(Ui.ItemContent(w).Key("p-item-content").Child(Ui.ItemTitle(w, "Tên")).Child(Ui.ItemDescription(w, "Mô tả")))
                    .Child(Ui.ItemActions(w).Key("p-item-actions").Child(Ui.Button(w, "Xem", ButtonVariant.Outline, ButtonSize.Sm))))
                .Child(Ui.Item(w, ItemVariant.Muted, ItemSize.Xs).Key("p-item-xs").Child(Ui.ItemContent(w, ItemSize.Xs).Child(Ui.ItemTitle(w, "Gọn")))))
            .Child(Ui.Empty(w).Class("w-[400px] border border-border").Key("p-empty")
                .Child(Ui.EmptyHeader(w).Child(Ui.EmptyMedia(w, EmptyMediaVariant.Icon).Key("p-empty-media").Child(Icon.Search(w)))
                    .Child(Ui.EmptyTitle(w, "Trống").Key("p-empty-title"))));
}
