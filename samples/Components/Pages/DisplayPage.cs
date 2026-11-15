using GotX;
using GotX.Components;

/// <summary>Trang gallery nhóm Display: mỗi mục một component với các variant/kích thước của nó.</summary>
public sealed class DisplayPage : View
{
    /// <summary>Dựng trang; nội dung từng mục nằm ở <c>Pages/Display/*.cs</c> để mỗi file dưới 200 dòng.</summary>
    public override Div Render(Window w) => Demo.Page(w).Class("min-h-screen")
        .Child(DisplayDemos.Buttons(w))
        .Child(DisplayDemos.Icons(w))
        .Child(DisplayDemos.Badges(w))
        .Child(DisplayDemos.Avatars(w))
        .Child(DisplayDemos.ButtonGroups(w))
        .Child(DisplayDemos.Alerts(w))
        .Child(DisplayDemos.Cards(w))
        .Child(DisplayDemos.Items(w))
        .Child(DisplayDemos.Empties(w))
        .Child(DisplayDemos.Small(w));
}
