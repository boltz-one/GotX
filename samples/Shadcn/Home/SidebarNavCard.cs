using GotX;
using GotX.Components;

/// <summary>
/// Bốn thẻ menu sidebar (sidebar-nav.tsx): Overview / Planning / Support / Account trong lưới 2 cột. Bấm một mục thì mục đó
/// không đổi mục đang sáng (isActive cố định như bản gốc), chỉ phát <see cref="ItemSelected"/>. Không dùng được component <c>Sidebar</c>: nó luôn rộng w-64, có viền phải và nền riêng,
/// trong khi ở đây mỗi nhóm là một thẻ rộng nửa cột – nên nút menu dựng lại bằng đúng class của sidebar.
/// </summary>
public sealed class SidebarNavCard : View
{
    /// <summary>Người dùng chọn một mục.</summary>
    public readonly record struct ItemSelected(int Section, int Item, string Label);

    sealed record Section(string Label, string Placement, (string Label, Func<Window, Div> Icon)[] Items)
    {
        public readonly string[] Keys = Array.ConvertAll(Items, i => "nav-" + i.Label.ToLowerInvariant().Replace(' ', '-'));
    }

    // Thứ tự DOM như bản gốc; từ xl các thẻ đổi chỗ bằng col-start/row-start (Planning, Support / Overview, Account).
    static readonly Section[] Sections =
    [
        new("Overview", "xl:col-start-1 xl:row-start-2", [("Analytics", HugeIcons.Analytics01), ("Transactions", HugeIcons.ArrowDataTransferHorizontal),
            ("Investments", HugeIcons.AnalyticsUp), ("Accounts", HugeIcons.Bank), ("Spending", HugeIcons.PieChart)]),
        new("Planning", "xl:col-start-1 xl:row-start-1", [("Documents", HugeIcons.File02), ("Budget", HugeIcons.Wallet01),
            ("Reports", HugeIcons.ChartBarLine), ("Goals", HugeIcons.Target02), ("Calendar", HugeIcons.Calendar03)]),
        new("Support", "xl:col-start-2 xl:row-start-1", [("Help Center", HugeIcons.HelpCircle), ("Docs", HugeIcons.BookOpen02),
            ("Contact Us", HugeIcons.Message01), ("Status", HugeIcons.Activity), ("Community", HugeIcons.Globe02)]),
        new("Account", "xl:col-start-2 xl:row-start-2", [("Profile", HugeIcons.User), ("Billing", HugeIcons.CreditCard),
            ("Notifications", HugeIcons.Notification03), ("Security", HugeIcons.Shield), ("Appearance", HugeIcons.PaintBoard)]),
    ];
    const int Stride = 8; // chỉ số phẳng = nhóm * Stride + mục
    // SidebarSection: Card "w-full overflow-hidden rounded-3xl py-0"; nhóm p-2, nhãn h-8 px-3 text-xs, menu gap-1. Trong
    // sidebar --radius = radius-xl nên rounded-xl = 14 × 1.4 = 19.6px. Nút có icon đầu: pl-2.5.
    const string SectionCard = "w-full overflow-hidden rounded-3xl py-0";
    const string ButtonBase =
        "flex h-8 w-full items-center gap-2 overflow-hidden rounded-[19.6px] py-2 pr-3 pl-2.5 text-left text-sm whitespace-nowrap " +
        "cursor-pointer hover:bg-sidebar-accent hover:text-sidebar-accent-foreground active:bg-sidebar-accent " +
        "active:text-sidebar-accent-foreground focus-visible:ring-3 focus-visible:ring-sidebar-ring";
    const string ButtonActive = ButtonBase + " bg-sidebar-accent font-medium text-sidebar-accent-foreground";
    // Mục sáng của từng nhóm như isActive của bản gốc: Analytics và Billing.
    readonly int[] _active = [0, -1, -1, 1];
    Indexed<SidebarNavCard>[]? _refs;

    /// <summary>Chỉ số mục đang chọn của nhóm (-1 = không có).</summary>
    public int ActiveItem(int section) => _active[section];

    // Như bản gốc: isActive cố định (Analytics, Billing); bấm một mục chỉ là điều hướng – báo ra ngoài, không đổi mục sáng.
    void Select(int flat)
    {
        int section = flat / Stride, item = flat % Stride;
        Emit(new ItemSelected(section, item, Sections[section].Items[item].Label));
    }

    Div RenderSection(Window w, int index)
    {
        var section = Sections[index];
        var refs = Indexed<SidebarNavCard>.Ensure(ref _refs, this, Sections.Length * Stride);
        var menu = w.Div().Class("flex w-full min-w-0 flex-col gap-1");
        for (int i = 0; i < section.Items.Length; i++)
            menu.Child(w.Div().Class(i == _active[index] ? ButtonActive : ButtonBase)
                .Key(section.Keys[i]).Focusable()
                .OnClick(refs[index * Stride + i], static (r, _) => r.Owner.Select(r.Index))
                .Child(section.Items[i].Icon(w))
                .Child(w.Div().Class("truncate").Child(section.Items[i].Label)));

        return Ui.Card(w).Class(SectionCard).Class(section.Placement).Child(w.Div().Class("relative flex w-full min-w-0 flex-col p-2")
            .Child(w.Div().Class("flex h-8 shrink-0 items-center rounded-[19.6px] px-3 text-xs font-medium text-sidebar-foreground/70").Child(section.Label))
            .Child(menu));
    }

    public override Div Render(Window w)
    {
        using var _ = HomeStyle.Use();
        var grid = w.Div().Class("grid w-full grid-cols-2 gap-4 xl:gap-6").Key("sidebar-nav");
        for (int i = 0; i < Sections.Length; i++) grid.Child(RenderSection(w, i));
        return grid;
    }
}