using System.Globalization;
using GotX;
using GotX.Components;

/// <summary>
/// Control riêng của từng hàng (ô nhập Target / Limit, Select người duyệt, menu hàng) – tạo lần đầu hàng được vẽ rồi giữ
/// lại, nên giá trị đang gõ còn nguyên khi chuyển trang – và thanh phân trang.
/// </summary>
public sealed partial class DashboardTable
{
    // Input của ô: h-8 w-16 nền/viền trong suốt, chữ căn phải; rê chuột nền input/30, focus viền ring + nền background.
    const string CellInput =
        "h-8 w-16 min-w-0 rounded-md border border-transparent bg-transparent px-3 py-1 text-right text-sm cursor-text transition-shadow " +
        "hover:bg-input/30 focus-visible:border-ring focus-visible:bg-background focus-visible:ring-3 focus-visible:ring-ring/50 " +
        "dark:bg-transparent dark:hover:bg-input/30 dark:focus-visible:bg-input/30";
    // Nút mở menu hàng: ghost size-8 muted, data-[state=open]:bg-muted.
    const string MenuButton = "flex size-8 items-center justify-center rounded-md text-muted-foreground cursor-pointer transition-all " +
        "hover:bg-accent hover:text-accent-foreground dark:hover:bg-accent/50";

    sealed class RowControls(Input target, Input limit, Select? reviewer, DropdownMenu menu)
    {
        public readonly Input Target = target, Limit = limit;
        public readonly Select? Reviewer = reviewer;
        public readonly DropdownMenu Menu = menu;
    }

    readonly Dictionary<int, RowControls> _controls = [];
    static readonly string[] PageSizes = ["10", "20", "30", "40", "50"];
    static readonly string[] Reviewers = ["Eddie Lake", "Jamik Tashpulatov"];
    Select _pageSizeSelect = null!;

    RowControls Controls(DashboardSection s) => _controls[s.Id];

    /// <summary>Tạo control của một hàng (lúc OnCreated cho cả 68 hàng, và khi thêm hàng) – không tạo entity trong Render.</summary>
    void CreateControls(DashboardSection s)
    {
        var target = CellEntry(s, s.Target, "Target", static (s, v) => s.Target = v);
        var limit = CellEntry(s, s.Limit, "Limit", static (s, v) => s.Limit = v);
        Select? reviewer = null;
        if (!s.Assigned)
        {
            reviewer = App.New(new Select { Items = Reviewers, Placeholder = Dashboard01Data.Unassigned, TriggerClass = "h-8 w-full" });
            App.Subscribe<Select.ValueChanged>(reviewer, e => s.Reviewer = e.Value);
        }
        var menu = App.New(new DropdownMenu { Items = Dashboard01Data.RowMenu, Placement = Placement.BottomEnd, MenuClass = "w-32", AriaLabel = "Open menu" });
        menu.Trigger = w => w.Div().Class(menu.IsOpen ? MenuButton + " bg-muted" : MenuButton).Child(DashboardIcons.DotsVertical(w));
        App.Subscribe<DropdownMenu.OpenChanged>(menu, _ => menu.Notify());
        App.Subscribe<MenuItemSelected>(menu, e => LastAction = e.Item.Text + ": " + s.Header);
        _controls[s.Id] = new RowControls(target, limit, reviewer, menu);
    }

    Input CellEntry(DashboardSection s, string value, string label, Action<DashboardSection, string> store)
    {
        var input = App.New(new Input { Text = value, AriaLabel = label });
        input.BoxClassOverride = static _ => CellInput;
        App.Subscribe<TextEntry.TextChanged>(input, e => store(s, e.Text));
        // Enter = submit của <form> quanh ô: toast.promise "Saving …" rồi "Done" sau 1 giây.
        App.Subscribe<TextEntry.Submitted>(input, _ => Save(s));
        return input;
    }

    void Save(DashboardSection s)
    {
        if (_window is not { } w) return;
        int id = _toaster.Show(w, new Toast { Title = "Saving " + s.Header, Variant = ToastVariant.Loading, Duration = TimeSpan.FromSeconds(30) });
        App.SetTimeout(1, () => { _toaster.Dismiss(id); _toaster.Show(w, "Done", variant: ToastVariant.Success); });
    }

    /// <summary>Mở drawer chi tiết (bên phải) của hàng có id cho trước – link ở cột Header.</summary>
    public void OpenDetail(int id)
    {
        if (_window is null) return;
        _viewing = _sections.First(s => s.Id == id);
        _drawer.Title = _viewing.Header;
        _drawer.Description = "Showing total visitors for the last 6 months";
        _drawer.Open(_window);
    }


    void SetPage(int page)
    {
        page = Math.Clamp(page, 0, PageCount - 1);
        if (page == _page) return;
        _page = page;
        Notify();
    }

    Div Pagination(Window w)
    {
        bool prev = _page > 0, next = _page < PageCount - 1;
        string selected = _selected.Count.ToString(CultureInfo.InvariantCulture) + " of " + _sections.Count.ToString(CultureInfo.InvariantCulture) + " row(s) selected.";
        string page = "Page " + (_page + 1).ToString(CultureInfo.InvariantCulture) + " of " + PageCount.ToString(CultureInfo.InvariantCulture);
        // ml-auto lg:ml-0: dưới lg một ô đệm flex-1 đẩy nhóm nút sang phải.
        var buttons = w.Div().Class("flex items-center gap-2")
            .Child(Nav(w, "first-page", DashboardIcons.ChevronsLeftPath, prev, true, 0))
            .Child(Nav(w, "previous-page", DashboardIcons.ChevronLeftPath, prev, false, 1))
            .Child(Nav(w, "next-page", DashboardIcons.ChevronRightPath, next, false, 2))
            .Child(Nav(w, "last-page", DashboardIcons.ChevronsRightPath, next, true, 3));
        return w.Div().Key("pagination").Class("flex items-center justify-between px-4")
            .Child(w.Div().Class("hidden flex-1 text-sm text-muted-foreground lg:flex").Child(selected))
            .Child(w.Div().Class("flex w-full items-center gap-8 lg:w-auto")
                .Child(w.Div().Class("hidden items-center gap-2 lg:flex")
                    .Child(w.Div().Class("flex items-center gap-2 text-sm leading-none font-medium").Child("Rows per page"))
                    .Child(w.View(_pageSizeSelect).Key("rows-per-page").Class("grid w-20")))
                .Child(w.Div().Class("flex items-center justify-center text-sm font-medium").Child(page))
                .Child(w.Div().Class("flex-1 lg:hidden"))
                .Child(buttons));
    }

    Indexed<DashboardTable>[]? _navRefs;

    Div Nav(Window w, string key, VectorPath icon, bool enabled, bool lgOnly, int action)
    {
        var refs = Indexed<DashboardTable>.Ensure(ref _navRefs, this, 4);
        var b = Ui.Button(w, ButtonVariant.Outline, ButtonSize.IconSm).Class(lgOnly ? "hidden disabled:opacity-50 lg:flex" : "disabled:opacity-50")
            .Key(key).AriaLabel(action switch { 0 => "Go to first page", 1 => "Go to previous page", 2 => "Go to next page", _ => "Go to last page" }).Disabled(!enabled).Child(DashboardIcons.Draw(w, icon));
        return enabled ? b.OnClick(refs[action], static (r, _) => r.Owner.GoTo(r.Index)) : b;
    }

    void GoTo(int action) => SetPage(action switch { 0 => 0, 1 => _page - 1, 2 => _page + 1, _ => PageCount - 1 });

    Select CreatePageSizeSelect()
    {
        var select = App.New(new Select { Items = PageSizes, SelectedIndex = 0, TriggerClass = "h-8 w-full" });
        App.Subscribe<Select.ValueChanged>(select, e =>
        {
            _pageSize = int.Parse(e.Value, CultureInfo.InvariantCulture);
            _page = Math.Min(_page, PageCount - 1);
            Notify();
        });
        return select;
    }
}
