using GotX;
using GotX.Components;

/// <summary>
/// Thẻ "Payments" (payments.tsx): breadcrumb có menu thả ở mục giữa, và ba dòng liên kết tới các thiết lập chuyển tiền.
/// Bấm một dòng phát <see cref="LinkOpened"/>; chọn mục menu phát <c>MenuItemSelected</c> từ <see cref="AccountMenu"/>.
/// </summary>
public sealed class PaymentsCard : View
{
    /// <summary>Người dùng bấm dòng thứ <paramref name="Index"/>.</summary>
    public readonly record struct LinkOpened(int Index, string Title);

    static readonly (string Key, string Title, string Description, Func<Window, Div> Icon)[] Links =
    [
        ("payments-limit", "Change transfer limit", "Adjust how much you can send from your balance.", Icon.Settings),
        ("payments-scheduled", "Scheduled transfers", "Set up a transfer to send at a later date.", Icon.Calendar),
        ("payments-recurring", "Recurring card payments", "Manage your repeated card transactions.", Icon.Refresh),
    ];

    static readonly string MenuTrigger = Ui.ButtonClasses(ButtonVariant.Ghost, ButtonSize.IconSm);

    DropdownMenu _menu = null!;
    Indexed<PaymentsCard>[]? _refs;

    public DropdownMenu AccountMenu => _menu;

    protected override void OnCreated() => _menu = App.New(new DropdownMenu
    {
        Trigger = static w => w.Div().Class(MenuTrigger).Child(HomeIcons.CircleEllipsis(w)),
        Items = [MenuItem.Action("Profile", key: "payments-menu-profile"), MenuItem.Action("Statements"), MenuItem.Action("Documents")],
    });

    void Open(int index) => Emit(new LinkOpened(index, Links[index].Title));

    public override Div Render(Window w)
    {
        var refs = Indexed<PaymentsCard>.Ensure(ref _refs, this, Links.Length);
        var list = Ui.ItemGroup(w);
        for (int i = 0; i < Links.Length; i++)
            list.Child(HomeUi.LinkItem(w, Links[i].Key).OnClick(refs[i], static (r, _) => r.Owner.Open(r.Index))
                .Child(HomeUi.Media(w, Links[i].Icon(w)))
                .Child(Ui.ItemContent(w).Class("min-w-0")
                    .Child(Ui.ItemTitle(w, Links[i].Title))
                    .Child(Ui.ItemDescription(w, Links[i].Description)))
                .Child(Icon.ChevronRight(w).Class("text-muted-foreground")));

        return HomeUi.Card(w).Key("payments-card")
            .Child(HomeUi.Header(w).Child(Ui.Breadcrumb(w).Class("sm:gap-2.5")
                .Child(Ui.BreadcrumbItem(w).Child(Ui.BreadcrumbLink(w, "Home").Key("payments-home")))
                .Child(Ui.BreadcrumbSeparator(w))
                .Child(Ui.BreadcrumbItem(w).Child(w.View(_menu)))
                .Child(Ui.BreadcrumbSeparator(w))
                .Child(Ui.BreadcrumbItem(w).Child(Ui.BreadcrumbPage(w, "Payments")))))
            .Child(HomeUi.Content(w).Child(list));
    }
}
