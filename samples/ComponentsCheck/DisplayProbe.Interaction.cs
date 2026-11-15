using GotX;
using GotX.Components;

/// <summary>
/// View thử cho phần mới của nhóm Display: mọi icon vẽ ở 48px (đủ lớn để soát path bằng mắt và bằng pixel), avatar có
/// ảnh / không ảnh, và một hàng nút để thử Tab, Enter/Space, vòng focus.
/// </summary>
sealed class DisplayInteractionProbe : View
{
    public int Clicks, DisabledClicks;

    // Khoá của từng ô icon, tạo một lần.
    public static readonly string[] IconKeys = Enumerable.Range(0, Icon.All.Count).Select(static i => "ic-" + i).ToArray();

    public override Div Render(Window w)
    {
        var icons = w.Div().Class("grid grid-cols-12 gap-x-2 gap-y-3");
        for (int i = 0; i < Icon.All.Count; i++)
            icons.Child(w.Div().Class("flex min-w-0 flex-col items-center gap-1")
                .Child(Icon.All[i].Draw(w).Class("size-12").Key(IconKeys[i]))
                .Child(w.Div().Class("max-w-full truncate text-[10px] text-muted-foreground").Child(Icon.All[i].Name)));

        return w.Div().Class("size-full flex flex-col items-start gap-5 p-6 bg-background text-foreground")
            .Child(icons)
            .Child(w.Div().Class("flex items-center gap-4")
                .Child(Icon.Check(w).Key("ic-default"))
                .Child(w.Div().Class("text-destructive").Child(Icon.Heart(w).Class("size-12").Key("ic-red")))
                .Child(Icon.Spinner(w).Class("size-12").Key("ic-spin"))
                .Child(Ui.Spinner(w, 48).Key("ui-spin"))
                .Child(Ui.Avatar(w, DemoImages.Portrait, "AN", AvatarSize.Lg).Key("a-img"))
                .Child(Ui.Avatar(w, null, "NA", AvatarSize.Lg).Key("a-null"))
                .Child(Ui.Avatar(w, DemoImages.Landscape, "PC", AvatarSize.Lg).Key("a-wide")))
            .Child(w.Div().Class("flex items-center gap-6")
                .Child(Ui.Button(w, "Bấm").Key("b-a").OnClick(this, static (p, _) => p.Clicks++))
                .Child(Ui.Button(w, "Khoá", ButtonVariant.Outline).Key("b-off").Disabled().OnClick(this, static (p, _) => p.DisabledClicks++))
                .Child(Ui.Button(w, "Liên kết gạch chân", ButtonVariant.Link).Key("b-link"))
                .Child(Ui.Button(w, "Xoá", ButtonVariant.Destructive).Key("b-del"))
                .Child(Ui.Button(w, ButtonVariant.Ghost, ButtonSize.Icon).Key("b-ghost").Child(Icon.Settings(w)))
                .Child(Ui.Badge(w, "Liên kết", BadgeVariant.Link).Key("badge-link")));
    }
}
