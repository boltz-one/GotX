using GotX;
using GotX.Components;

/// <summary>
/// Bốn thẻ menu sidebar (sidebar-nav.tsx): Overview / Planning / Support / Account trong lưới 2 cột. Bấm một mục thì mục đó
/// thành mục đang chọn của nhóm. Không dùng được component <c>Sidebar</c>: nó luôn rộng w-64, có viền phải và nền riêng,
/// trong khi ở đây mỗi nhóm là một thẻ rộng nửa cột – nên nút menu dựng lại bằng đúng class của sidebar.
/// </summary>
public sealed class SidebarNavCard : View
{
    /// <summary>Người dùng chọn một mục.</summary>
    public readonly record struct ItemSelected(int Section, int Item, string Label);

    sealed record Section(string Label, (string Label, Func<Window, Div> Icon)[] Items)
    {
        // Key dựng một lần; bản "compact" là bản sao Overview chỉ hiện dưới xl (xem Render).
        public readonly string[] Keys = Array.ConvertAll(Items, i => "nav-" + i.Label.ToLowerInvariant().Replace(' ', '-'));
        public readonly string[] CompactKeys = Array.ConvertAll(Items, i => "nav-compact-" + i.Label.ToLowerInvariant().Replace(' ', '-'));
    }

    static readonly Section[] Sections =
    [
        new("Overview", [("Analytics", HomeIcons.ChartColumn), ("Transactions", HomeIcons.ArrowLeftRight), ("Investments", HomeIcons.TrendingUp),
            ("Accounts", HomeIcons.Landmark), ("Spending", HomeIcons.PieChart)]),
        new("Planning", [("Documents", Icon.File), ("Budget", HomeIcons.Wallet), ("Reports", HomeIcons.ChartLine),
            ("Goals", HomeIcons.Target), ("Calendar", Icon.Calendar)]),
        new("Support", [("Help Center", HomeIcons.HelpCircle), ("Docs", HomeIcons.BookOpen), ("Contact Us", Icon.MessageSquare),
            ("Status", HomeIcons.Activity), ("Community", Icon.Globe)]),
        new("Account", [("Profile", Icon.User), ("Billing", HomeIcons.CreditCard), ("Notifications", Icon.Bell),
            ("Security", HomeIcons.Shield), ("Appearance", HomeIcons.Palette)]),
    ];

    const int Stride = 8; // chỉ số phẳng = nhóm * Stride + mục
    const string SectionCard = "w-full gap-0 rounded-[24px] py-0 shadow-sm ring-foreground/5 dark:ring-foreground/10";
    const string ButtonBase =
        "flex h-8 w-full items-center gap-2 overflow-hidden rounded-xl px-3 py-2 text-left text-sm cursor-pointer transition-colors " +
        "hover:bg-sidebar-accent hover:text-sidebar-accent-foreground active:bg-sidebar-accent focus-visible:ring-2 focus-visible:ring-sidebar-ring";
    const string ButtonActive = ButtonBase + " bg-sidebar-accent font-medium text-sidebar-accent-foreground";

    // Mục đang chọn của từng nhóm như isActive của bản gốc: Analytics và Billing.
    readonly int[] _active = [0, -1, -1, 1];
    Indexed<SidebarNavCard>[]? _refs;

    /// <summary>Chỉ số mục đang chọn của nhóm (-1 = không có).</summary>
    public int ActiveItem(int section) => _active[section];

    void Select(int flat)
    {
        int section = flat / Stride, item = flat % Stride;
        if (_active[section] == item) return;
        _active[section] = item;
        Notify();
        Emit(new ItemSelected(section, item, Sections[section].Items[item].Label));
    }

    Div RenderSection(Window w, int index, bool compact = false)
    {
        var section = Sections[index];
        var refs = Indexed<SidebarNavCard>.Ensure(ref _refs, this, Sections.Length * Stride);
        var menu = w.Div().Class("flex w-full min-w-0 flex-col gap-1");
        for (int i = 0; i < section.Items.Length; i++)
            menu.Child(w.Div().Class(i == _active[index] ? ButtonActive : ButtonBase)
                .Key(compact ? section.CompactKeys[i] : section.Keys[i]).Focusable()
                .OnClick(refs[index * Stride + i], static (r, _) => r.Owner.Select(r.Index))
                .Child(section.Items[i].Icon(w))
                .Child(w.Div().Class("truncate").Child(section.Items[i].Label)));

        return Ui.Card(w).Class(SectionCard).Child(w.Div().Class("relative flex w-full min-w-0 flex-col p-2")
            .Child(w.Div().Class("flex h-8 shrink-0 items-center rounded-xl px-3 text-xs font-medium text-sidebar-foreground/70").Child(section.Label))
            .Child(menu));
    }

    public override Div Render(Window w) =>
        // Bản gốc đổi chỗ các thẻ từ xl bằng col-start/row-start (Planning, Support / Overview, Account). Core chưa có
        // col-start, nên Overview được dựng ở hai vị trí và mỗi vị trí chỉ hiện ở một phía của breakpoint xl.
        w.Div().Class("grid w-full grid-cols-2 gap-4 xl:gap-6").Key("sidebar-nav")
            .Child(RenderSection(w, 0, compact: true).Class("xl:hidden"))
            .Child(RenderSection(w, 1))
            .Child(RenderSection(w, 2))
            .Child(RenderSection(w, 0).Class("hidden xl:flex"))
            .Child(RenderSection(w, 3));
}
