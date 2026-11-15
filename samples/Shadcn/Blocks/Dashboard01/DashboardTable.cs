using GotX;
using GotX.Components;

/// <summary>
/// Phần bảng của dashboard-01 (data-table.tsx): thanh tab, nút "Customize Columns" / "Add Section", bảng Outline dựng trên
/// <see cref="DataTable{T}"/>, và tấm chi tiết mở từ link tiêu đề. Ba tab còn lại là khung giữ chỗ như bản gốc.
/// Khác bản gốc: không kéo thả đổi thứ tự hàng (cột tay nắm bị bỏ); Target/Limit là chữ căn phải thay cho ô nhập trong
/// ô; hàng chưa có reviewer hiện chữ mờ "Assign reviewer" thay cho Select (mỗi ô nhập/Select là một entity riêng cho
/// từng hàng); tab không có badge đếm (Tabs chỉ nhận nhãn chữ); thêm sắp xếp theo Header / Target / Limit.
/// </summary>
public sealed class DashboardTable : View
{
    public const string AddKey = "dashboard-add-section", PlaceholderKey = "dashboard-tab-placeholder", DoneKey = "dashboard-sheet-done";
    const string LinkClass = "truncate rounded-sm text-left text-foreground cursor-pointer hover:underline focus-visible:ring-3 focus-visible:ring-ring/50";

    Tabs _tabs = null!;
    DataTable<DashboardSection> _table = null!;
    Sheet _sheet = null!;
    List<DashboardSection> _sections = [];
    DashboardSection? _viewing;
    Window? _window;

    public Tabs Tabs => _tabs;
    public DataTable<DashboardSection> Table => _table;
    public Sheet Sheet => _sheet;

    /// <summary>Hàng đang mở trong tấm chi tiết (null khi tấm đóng lần đầu).</summary>
    public DashboardSection? Viewing => _viewing;

    /// <summary>Thao tác gần nhất trên menu hàng, dạng "Edit: Cover page" (bản gốc không làm gì với chúng).</summary>
    public string LastAction { get; private set; } = "";

    protected override void OnCreated()
    {
        // Kiểu Line thay cho kiểu thẻ của bản gốc: Tabs chia đều bề rộng cho các nút (flex-1) và Core không giữ sàn min-content
        // cho flex item, nên nhãn dài tràn khỏi nút ~12px. Với kiểu Line phần tràn rơi vào khoảng đệm giữa hai nhãn nên không
        // đè chữ; với kiểu thẻ nó đè lên nhãn bên cạnh.
        _tabs = App.New(new Tabs { Items = Dashboard01Data.Tabs, Variant = TabsVariant.Line });
        App.Subscribe<Tabs.ValueChanged>(_tabs, _ => Notify());
        _table = App.New(new DataTable<DashboardSection>
        {
            Columns = Columns(), Selectable = true, RowActions = Dashboard01Data.RowMenu, ShowToolbar = false,
            ColumnsLabel = "Customize Columns", HeaderRowClass = "bg-muted", FrameClass = "overflow-hidden rounded-lg border border-border",
        });
        SetSections(Dashboard01Data.BuildSections());
        App.Subscribe<DataTable<DashboardSection>.RowAction>(_table, e =>
        {
            LastAction = e.Action.Text + ": " + e.Item.Header;
            if (e.Action.Destructive) { _sections.RemoveAt(e.Index); SetSections([.. _sections]); }
        });
        _sheet = App.New(new Sheet { Body = SheetBody, Footer = SheetFooter });
    }

    void SetSections(List<DashboardSection> sections)
    {
        _sections = sections;
        foreach (var s in sections) s.Owner = this;
        _table.Items = sections; // danh sách mới: bảng xoá lựa chọn và tính lại số trang
    }

    /// <summary>Nút "Add Section": thêm một hàng trống vào cuối (bản gốc chỉ có nút, chưa gắn hành vi).</summary>
    public void AddSection()
    {
        var next = new List<DashboardSection>(_sections);
        int id = next.Count == 0 ? 1 : next[^1].Id + 1;
        next.Add(new(id, "New section " + id, "Narrative", "In Process", 0, 0, Dashboard01Data.Unassigned));
        SetSections(next);
    }

    /// <summary>Mở tấm chi tiết của một hàng (link ở cột Header; bản gốc mở Drawer bên phải).</summary>
    public void OpenDetail(DashboardSection section)
    {
        if (_window is null) return;
        _viewing = section;
        _sheet.Title = section.Header;
        _sheet.Description = "Showing total visitors for the last 6 months";
        _sheet.Open(_window);
    }

    static IReadOnlyList<DataColumn<DashboardSection>> Columns() =>
    [
        new("header", "Header")
        {
            Width = "2fr", CanHide = false, Text = static s => s.Header, Compare = DataColumn<DashboardSection>.ByText(static s => s.Header),
            Cell = static (w, s) => w.Div().Key(s.LinkKey).Focusable().Class(LinkClass).Child(s.Header).OnClick(s, static (s, _) => s.Owner?.OpenDetail(s)),
        },
        new("type", "Section Type")
        {
            Width = "150px", Text = static s => s.Type,
            Cell = static (w, s) => Ui.Badge(w, s.Type, BadgeVariant.Outline).Class("px-1.5 text-muted-foreground"),
        },
        new("status", "Status")
        {
            Width = "120px", Text = static s => s.Status,
            Cell = static (w, s) => Ui.Badge(w, BadgeVariant.Outline).Class("px-1.5 text-muted-foreground")
                .Child(s.Done ? Icon.CircleCheck(w).Class("size-3 text-green-500") : Icon.Loader(w).Class("size-3")).Child(s.Status),
        },
        new("target", "Target") { Width = "84px", Align = TableAlign.Right, Text = static s => s.TargetText, Compare = DataColumn<DashboardSection>.By(static s => s.Target) },
        new("limit", "Limit") { Width = "84px", Align = TableAlign.Right, Text = static s => s.LimitText, Compare = DataColumn<DashboardSection>.By(static s => s.Limit) },
        new("reviewer", "Reviewer")
        {
            Text = static s => s.Reviewer,
            Cell = static (w, s) => w.Div().Class(s.Assigned ? "truncate" : "truncate text-muted-foreground").Child(s.Reviewer),
        },
    ];

    static Div Detail(Window w, string label, string value) =>
        w.Div().Class("flex flex-col gap-1.5").Child(Ui.Label(w, label)).Child(w.Div().Class("text-muted-foreground").Child(value));

    Div SheetBody(Window w)
    {
        var s = _viewing;
        var body = w.Div().Class("flex flex-col gap-4 text-sm");
        if (s is null) return body;
        return body
            .Child(w.Div().Class("flex items-center gap-2 font-medium").Child("Trending up by 5.2% this month").Child(DashboardIcons.TrendingUp(w)))
            .Child(Ui.Separator(w))
            .Child(w.Div().Class("grid grid-cols-2 gap-4")
                .Child(Detail(w, "Type", s.Type)).Child(Detail(w, "Status", s.Status))
                .Child(Detail(w, "Target", s.TargetText)).Child(Detail(w, "Limit", s.LimitText)))
            .Child(Detail(w, "Reviewer", s.Reviewer));
    }

    Div SheetFooter(Window w) =>
        Ui.Button(w, "Done", ButtonVariant.Outline).Key(DoneKey).OnClick(this, static (t, _) => t._sheet.Close());

    public override Div Render(Window w)
    {
        _window = w;
        // flex-wrap: ở cửa sổ hẹp hai nút xuống dòng dưới thanh tab (bản gốc đổi thanh tab thành Select nhờ container query).
        var toolbar = w.Div().Class("flex flex-wrap items-center justify-between gap-2 px-4 lg:px-6")
            .Child(w.View(_tabs))
            .Child(w.Div().Class("flex items-center gap-2")
                .Child(w.View(_table.ColumnsMenu))
                .Child(Ui.Button(w, ButtonVariant.Outline, ButtonSize.Sm).Key(AddKey).OnClick(this, static (t, _) => t.AddSection())
                    .Child(Icon.Plus(w)).Child("Add Section")));
        var content = w.Div().Class("px-4 lg:px-6");
        if (_tabs.SelectedIndex == 0) content.Child(w.View(_table));
        else content.Child(w.Div().Key(PlaceholderKey).Class("h-64 w-full rounded-lg border border-dashed border-border"));
        return w.Div().Key("dashboard-table").Class("flex w-full shrink-0 flex-col gap-6").Child(toolbar).Child(content);
    }
}
