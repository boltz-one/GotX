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
        ("payments-limit", "Change transfer limit", "Adjust how much you can send from your balance.", HugeIcons.Settings01),
        ("payments-scheduled", "Scheduled transfers", "Set up a transfer to send at a later date.", HugeIcons.Calendar03),
        ("payments-recurring", "Recurring card payments", "Manage your repeated card transactions.", HugeIcons.Refresh),
    ];

    // Nút mở menu là lớp bọc của DropdownMenu, mang class của Button ghost icon-sm.
    static readonly string MenuTrigger = HomeStyle.ButtonClasses(ButtonVariant.Ghost, ButtonSize.IconSm);

    DropdownMenu _menu = null!;
    Indexed<PaymentsCard>[]? _refs;

    public DropdownMenu AccountMenu => _menu;

    protected override void OnCreated()
    {
        using var _ = HomeStyle.Use();
        _menu = App.New(new DropdownMenu
        {
            AriaLabel = "Payment options", TriggerClass = MenuTrigger, TriggerOpenClass = "bg-muted text-foreground",
            Trigger = static w => HugeIcons.MoreHorizontalCircle01(w),
            Items = [MenuItem.Action("Profile", key: "payments-menu-profile"), MenuItem.Action("Statements"), MenuItem.Action("Documents")],
        });
    }

    void Open(int index) => Emit(new LinkOpened(index, Links[index].Title));

    public override Div Render(Window w)
    {
        using var _ = HomeStyle.Use();
        var refs = Indexed<PaymentsCard>.Ensure(ref _refs, this, Links.Length);
        var list = Ui.ItemGroup(w);
        for (int i = 0; i < Links.Length; i++)
            list.Child(HomeUi.LinkItem(w, Links[i].Key).OnClick(refs[i], static (r, _) => r.Owner.Open(r.Index))
                .Child(HomeUi.Media(w, Links[i].Icon(w)))
                .Child(Ui.ItemContent(w)
                    .Child(Ui.ItemTitle(w, Links[i].Title))
                    .Child(Ui.ItemDescription(w, Links[i].Description)))
                .Child(HugeIcons.ArrowRight01(w).Class("text-muted-foreground")));

        return HomeUi.Card(w).Key("payments-card")
            .Child(HomeUi.Header(w).Class("flex flex-col gap-3").Child(Ui.Breadcrumb(w)
                .Child(Ui.BreadcrumbItem(w).Child(Ui.BreadcrumbLink(w, "Home").Key("payments-home")))
                .Child(Ui.BreadcrumbSeparator(w))
                .Child(Ui.BreadcrumbItem(w).Key("payments-menu").Child(w.View(_menu)))
                .Child(Ui.BreadcrumbSeparator(w))
                .Child(Ui.BreadcrumbItem(w).Child(Ui.BreadcrumbPage(w, "Payments")))))
            .Child(HomeUi.Content(w).Child(list));
    }
}