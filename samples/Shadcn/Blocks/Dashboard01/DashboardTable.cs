using GotX;
using GotX.Components;

/// <summary>
/// Phần bảng của dashboard-01 (data-table.tsx): thanh tab (Select khi vùng main hẹp hơn 56rem), nút "Customize Columns" /
/// "Add Section", bảng Outline 68 hàng của data.json (kéo tay nắm, chọn hàng, link mở drawer chi tiết, badge, ô nhập
/// Target / Limit, Select người duyệt, menu hàng), phân trang 10 hàng. Ba tab còn lại là khung viền gạch aspect-video.
/// Như bản gốc: không sắp xếp theo cột (tiêu đề cột là chữ thường), bảng tự giãn cột theo nội dung (auto table layout)
/// và cuộn ngang khi hẹp hơn tổng bề rộng cột.
/// </summary>
public sealed partial class DashboardTable : View
{
    // has-[>svg]:px-2.5 của Button size="sm" (Core chưa có has-*): nút có icon con hẹp hơn 2px mỗi bên.
    const string HasSvg = "px-2.5";

    public const string AddKey = "add-section", ColumnsKey = "customize-columns", TabsKey = "tabs-list", ViewSelectKey = "view-selector";
    public const string SelectAllKey = "select-all", PlaceholderKey = "tab-placeholder", NextPageKey = "next-page", PreviousPageKey = "previous-page";
    public const string FirstPageKey = "first-page", LastPageKey = "last-page";

    /// <summary>Cột ẩn được (accessor + getCanHide): id hiện trong menu Customize Columns (<c>capitalize</c>).</summary>
    static readonly string[] Hideable = ["type", "status", "target", "limit", "reviewer"];
    static readonly string[] HideableLabels = ["Type", "Status", "Target", "Limit", "Reviewer"];

    readonly MenuItem[] _columnItems = new MenuItem[Hideable.Length];
    DropdownMenu _columns = null!;
    Select _view = null!;
    Drawer _drawer = null!;
    Toaster _toaster = null!;
    List<DashboardSection> _sections = [];
    readonly HashSet<int> _selected = [];
    Indexed<DashboardTable>[]? _tabRefs;
    int _tab, _page, _pageSize = 10;
    DashboardSection? _viewing;
    Window? _window;

    public DropdownMenu ColumnsMenu => _columns;
    public Drawer Drawer => _drawer;
    public IReadOnlyList<DashboardSection> Sections => _sections;

    /// <summary>Tab đang mở: 0 Outline, 1 Past Performance, 2 Key Personnel, 3 Focus Documents.</summary>
    public int Tab
    {
        get => _tab;
        set { if (_tab == value) return; _tab = value; if (_view.SelectedIndex != value) _view.SelectedIndex = value; Notify(); }
    }

    /// <summary>Hàng đang mở trong drawer chi tiết (null khi chưa mở lần nào).</summary>
    public DashboardSection? Viewing => _viewing;

    /// <summary>Thao tác gần nhất trên menu hàng, dạng "Edit: Cover page" (bản gốc không làm gì với chúng).</summary>
    public string LastAction { get; private set; } = "";

    /// <summary>Trang đang xem (1-based) và số trang.</summary>
    public int Page => _page + 1;
    public int PageCount => Math.Max(1, (_sections.Count + _pageSize - 1) / _pageSize);

    /// <summary>Số hàng đang được chọn (trên mọi trang) và hàng có id cho trước có được chọn không.</summary>
    public int SelectedCount => _selected.Count;
    public bool IsSelected(int id) => _selected.Contains(id);

    /// <summary>Menu "⋯" và ô nhập Target của hàng có id cho trước.</summary>
    public DropdownMenu RowMenu(int id) => _controls[id].Menu;
    public Input TargetInput(int id) => _controls[id].Target;

    /// <summary>Key của ô tiêu đề cột (<c>head-header</c>, <c>head-reviewer</c>…).</summary>
    public static string HeadKey(string column) => "head-" + column;

    public bool IsColumnVisible(string id) => Array.IndexOf(Hideable, id) is var i && (i < 0 || _columnItems[i].Checked);

    protected override void OnCreated()
    {
        for (int i = 0; i < Hideable.Length; i++) _columnItems[i] = MenuItem.Check(HideableLabels[i], true, key: "column-" + Hideable[i]);
        _columns = App.New(new DropdownMenu { Items = _columnItems, Trigger = ColumnsTrigger, Placement = Placement.BottomEnd, MenuClass = "w-56" });
        App.Subscribe<MenuItemToggled>(_columns, _ => Notify());
        _view = App.New(new Select { Items = [.. Dashboard01Data.Tabs.Select(static t => t.Title)], Placeholder = "Select a view", SelectedIndex = 0, TriggerClass = "h-8" });
        App.Subscribe<Select.ValueChanged>(_view, e => Tab = e.Index);
        _drawer = App.New(new Drawer { Side = PanelSide.Right, Body = DrawerBody, Footer = DrawerFooter });
        _toaster = App.New(new Toaster());
        _sections = Dashboard01Data.BuildSections();
        foreach (var s in _sections) CreateControls(s);
        _pageSizeSelect = CreatePageSizeSelect();
        CreateDrawerForm();
    }

    // Nút mở menu cột: Button outline sm + LayoutColumns, chữ "Customize Columns" (lg) / "Columns", ChevronDown.
    Div ColumnsTrigger(Window w) =>
        w.Div().Key(ColumnsKey).Class(Ui.ButtonClasses(ButtonVariant.Outline, ButtonSize.Sm)).Class(HasSvg)
            .Child(DashboardIcons.Draw(w, DashboardIcons.LayoutColumnsPath))
            .Child(w.Div().Class("hidden lg:block").Child("Customize Columns"))
            .Child(w.Div().Class("lg:hidden").Child("Columns"))
            .Child(DashboardIcons.Draw(w, DashboardIcons.ChevronDownPath));

    /// <summary>Nút "Add Section" (bản gốc chưa gắn hành vi; ở đây thêm một hàng trống vào cuối như minh hoạ).</summary>
    public void AddSection()
    {
        int id = _sections.Count == 0 ? 1 : _sections.Max(static s => s.Id) + 1;
        var section = new DashboardSection(id, "New section " + id, "Narrative", "In Process", "0", "0", Dashboard01Data.Unassigned);
        _sections.Add(section);
        CreateControls(section);
        Notify();
    }

    bool TabKey(int index, KeyEvent e)
    {
        int n = Dashboard01Data.Tabs.Length;
        int next = e.Key switch { Key.Right => (index + 1) % n, Key.Left => (index + n - 1) % n, Key.Home => 0, Key.End => n - 1, _ => -1 };
        if (next < 0) return false;
        Tab = next; // Radix Tabs: activationMode "automatic" – mũi tên chuyển focus là chọn luôn
        _window?.FocusKey(TabKeys[next]);
        return true;
    }

    void ClickTab(int index)
    {
        Tab = index;
        _window?.FocusKey(TabKeys[index], visible: false); // button của Radix nhận focus khi bấm
    }

    static readonly string[] TabKeys = ["tab-outline", "tab-past-performance", "tab-key-personnel", "tab-focus-documents"];

    Div TabsList(Window w)
    {
        var refs = Indexed<DashboardTable>.Ensure(ref _tabRefs, this, TabKeys.Length);
        var list = w.Div().Key(TabsKey).Class(V4.TabsListClass + " hidden min-[1192px]:flex");
        for (int i = 0; i < TabKeys.Length; i++)
        {
            var (title, _, badge) = Dashboard01Data.Tabs[i];
            var trigger = w.Div().Key(TabKeys[i]).Focusable(i == _tab).Class(i == _tab ? V4.TabTriggerActive : V4.TabTrigger)
                .OnClick(refs[i], static (r, _) => r.Owner.ClickTab(r.Index))
                .OnKey(refs[i], static (r, e) => r.Owner.TabKey(r.Index, e))
                .Child(title);
            // Badge secondary trong tab: size-5 rounded-full bg-muted-foreground/30 px-1.
            if (badge is not null)
                trigger.Child(w.Div().Class("flex size-5 shrink-0 items-center justify-center rounded-full bg-muted-foreground/30 px-1 text-xs font-medium text-secondary-foreground").Child(badge));
            list.Child(trigger);
        }
        return list;
    }

    public override Div Render(Window w)
    {
        _window = w;
        var toolbar = w.Div().Class("flex items-center justify-between px-4 lg:px-6")
            .Child(w.View(_view).Key(ViewSelectKey).Class("flex min-[1192px]:hidden"))
            .Child(TabsList(w))
            .Child(w.Div().Class("flex items-center gap-2")
                .Child(w.View(_columns))
                .Child(Ui.Button(w, ButtonVariant.Outline, ButtonSize.Sm).Class(HasSvg).Key(AddKey).OnClick(this, static (t, _) => t.AddSection())
                    .Child(DashboardIcons.Draw(w, DashboardIcons.PlusPath)).Child(w.Div().Class("hidden lg:block").Child("Add Section"))));
        var root = w.Div().Key("data-table").Class("flex w-full flex-col justify-start gap-6").Child(toolbar);
        // TabsContent của Radix có tabIndex=0 (class outline-none): Tab dừng ở vùng nội dung trước các control bên trong, không vẽ vòng.
        if (_tab == 0) return root.Child(w.Div().Key("tabs-content").Focusable().Class("relative flex flex-col gap-4 px-4 lg:px-6").Child(TableBox(w)).Child(Pagination(w)));
        return root.Child(w.Div().Key("tabs-content").Focusable().Class("flex flex-col px-4 lg:px-6")
            .Child(w.Div().Key(PlaceholderKey).Class("aspect-video w-full flex-1 rounded-lg border border-dashed border-border")));
    }
}
