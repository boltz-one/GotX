using GotX;
using GotX.Components;

/// <summary>Các mục demo của trang Display (nút, bảng icon).</summary>
static partial class DisplayDemos
{
    static readonly ButtonVariant[] Variants = Enum.GetValues<ButtonVariant>();

    /// <summary>Mọi variant và cỡ nút; nút có icon, nút đang tải, nút vô hiệu hoá. Tab đi qua từng nút và hiện vòng focus.</summary>
    public static Div Buttons(Window w)
    {
        var variants = Demo.Row(w);
        foreach (var v in Variants) variants.Child(Ui.Button(w, v.ToString(), v));
        return Demo.Section(w, "Button", "6 variant, 8 cỡ, có icon. Nhấn Tab để đi qua các nút: vòng focus chỉ hiện khi focus bằng bàn phím; Enter/Space bấm nút.")
            .Child(variants)
            .Child(Demo.Row(w)
                .Child(Ui.Button(w, "Xs", ButtonVariant.Outline, ButtonSize.Xs))
                .Child(Ui.Button(w, "Sm", ButtonVariant.Outline, ButtonSize.Sm))
                .Child(Ui.Button(w, "Default", ButtonVariant.Outline))
                .Child(Ui.Button(w, "Lg", ButtonVariant.Outline, ButtonSize.Lg))
                .Child(Ui.Button(w, ButtonVariant.Outline, ButtonSize.IconXs).Child(Icon.Plus(w).Class("size-3")))
                .Child(Ui.Button(w, ButtonVariant.Outline, ButtonSize.IconSm).Child(Icon.Plus(w).Class("size-3.5")))
                .Child(Ui.Button(w, ButtonVariant.Outline, ButtonSize.Icon).Child(Icon.Plus(w)))
                .Child(Ui.Button(w, ButtonVariant.Outline, ButtonSize.IconLg).Child(Icon.Plus(w))))
            .Child(Demo.Row(w)
                .Child(Ui.Button(w).Child(Icon.Mail(w)).Child("Gửi thư"))
                .Child(Ui.Button(w, ButtonVariant.Secondary).Child("Tiếp tục").Child(Icon.ArrowRight(w)))
                .Child(Ui.Button(w, ButtonVariant.Destructive).Child(Icon.Trash(w)).Child("Xoá"))
                .Child(Ui.Button(w, ButtonVariant.Ghost, ButtonSize.Icon).Child(Icon.Settings(w)))
                .Child(Ui.Button(w, ButtonVariant.Link).Child("Mở tài liệu").Child(Icon.ExternalLink(w)))
                .Child(Ui.Button(w).Disabled().Child(Ui.Spinner(w)).Child("Đang lưu"))
                .Child(Ui.Button(w, "Vô hiệu hoá", ButtonVariant.Outline).Disabled()));
    }

    /// <summary>Toàn bộ icon của <see cref="Icon"/>: mỗi ô vẽ icon 20px kèm tên hàm, để soát path bằng mắt.</summary>
    // Dựng một lần: trang có spinner nên render mỗi frame, chuỗi nội suy trong Render sẽ cấp phát mỗi frame.
    static readonly string IconsCaption = $"{Icon.All.Count} icon Lucide vẽ bằng VectorPath (viewBox 24, nét 2). Lấy màu chữ, đổi cỡ bằng size-*.";

    public static Div Icons(Window w)
    {
        var grid = w.Div().Class("grid grid-cols-8 gap-2").Key("icon-sheet");
        foreach (var (name, draw) in Icon.All)
            grid.Child(w.Div().Class("flex min-w-0 flex-col items-center gap-1.5 rounded-lg border border-border px-1 py-2.5")
                .Child(draw(w).Class("size-5"))
                .Child(w.Div().Class("max-w-full truncate text-[10px] text-muted-foreground").Child(name)));
        return Demo.Section(w, "Icon", IconsCaption)
            .Child(grid)
            .Child(Demo.Row(w).Class("gap-5")
                .Child(Icon.Star(w).Class("size-3"))
                .Child(Icon.Star(w))
                .Child(Icon.Star(w).Class("size-6"))
                .Child(Icon.Star(w).Class("size-8 text-amber-500"))
                .Child(Icon.Heart(w).Class("size-8 text-destructive"))
                .Child(Icon.CircleCheck(w).Class("size-8 text-green-600"))
                .Child(Icon.Settings(w).Class("size-8 text-muted-foreground"))
                .Child(Icon.Spinner(w).Class("size-8"))
                .Child(Icon.Spinner(w).Class("text-muted-foreground")));
    }
}
