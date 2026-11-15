using GotX;
using GotX.Components;

/// <summary>Trang gallery "Dữ liệu": Typography, Date Picker, Data Table và SidebarNav (menu sidebar có mục con).</summary>
public sealed class DataPage : View
{
    DatePicker _date = null!, _dateEmpty = null!;
    DataTable<DemoPayment> _table = null!;
    SidebarNav _nav = null!;
    string _log = "Chưa có thao tác.";

    public DatePicker Date => _date;
    public DatePicker EmptyDate => _dateEmpty;
    public DataTable<DemoPayment> Table => _table;
    public SidebarNav Nav => _nav;

    /// <summary>Dòng mô tả sự kiện gần nhất của bảng (để thấy event phát ra khi thao tác).</summary>
    public string Log => _log;

    void Say(string text) { _log = text; Notify(); }

    protected override void OnCreated()
    {
        // Ngày cố định để ảnh chụp trang không đổi theo ngày chạy.
        _date = App.New(new DatePicker { Value = new DateOnly(2026, 11, 15) });
        _date.Calendar.Today = new DateOnly(2026, 11, 15);
        _dateEmpty = App.New(new DatePicker());
        _dateEmpty.Calendar.Today = new DateOnly(2026, 11, 15);
        _dateEmpty.Calendar.Month = new DateOnly(2026, 11, 1);
        App.Subscribe<DatePicker.ValueChanged>(_dateEmpty, _ => Notify());

        _table = App.New(new DataTable<DemoPayment>
        {
            Columns = DataDemos.PaymentColumns(), Items = DataDemos.Payments, Selectable = true, FilterColumn = "email",
            FilterPlaceholder = "Filter emails...", RowActions = DataDemos.PaymentActions,
        });
        App.Subscribe<DataTable<DemoPayment>.SelectionChanged>(_table, e => Say($"SelectionChanged: {e.Count} hàng"));
        App.Subscribe<DataTable<DemoPayment>.SortChanged>(_table, e => Say($"SortChanged: {e.ColumnId ?? "-"} {e.Direction}"));
        App.Subscribe<DataTable<DemoPayment>.PageChanged>(_table, e => Say($"PageChanged: {e.Page}/{e.PageCount}"));
        App.Subscribe<DataTable<DemoPayment>.RowAction>(_table, e => Say($"RowAction: {e.Action.Text} → {e.Item.Id}"));

        _nav = App.New(new SidebarNav
        {
            Groups =
            [
                new("Platform",
                [
                    new("Playground", Icon.Terminal) { DefaultOpen = true, SubItems = ["History", "Starred", "Settings"] },
                    new("Models", Icon.Star) { SubItems = ["Genesis", "Explorer", "Quantum"] },
                    new("Documentation", Icon.FileText) { Badge = "12" },
                ]),
                new("Projects", [new("Design Engineering", Icon.Folder), new("Travel", Icon.Globe)]),
            ],
        });
        _nav.SetActive(0, 0, 1);
    }

    public override Div Render(Window w) => Demo.Page(w).Class("min-h-screen")
        .Child(DataDemos.Typography(w))
        .Child(Demo.Section(w, "Date Picker", "Nút + Calendar trong popover. Enter/Space/↓ mở, chọn ngày thì đóng và focus về nút, Escape đóng.")
            .Child(Demo.Row(w)
                .Child(w.View(_date))
                .Child(w.View(_dateEmpty))
                .Child(Ui.Muted(w, _dateEmpty.Value is { } d ? "Đã chọn: " + d.ToString("yyyy-MM-dd") : "Chưa chọn ngày."))))
        .Child(Demo.Section(w, "Data Table", "Sắp xếp, chọn hàng, lọc theo email, phân trang, ẩn/hiện cột, menu thao tác từng hàng.")
            .Child(w.View(_table))
            .Child(w.Div().Key("d-log").Class("text-xs text-muted-foreground").Child(_log)))
        .Child(Demo.Section(w, "Sidebar nav", "Menu sidebar có nhóm, mục con gập được, badge; →/← mở/đóng mục đang focus.")
            .Child(w.Div().Class("flex h-80 overflow-hidden rounded-lg border border-border")
                .Child(SidebarNav.Frame(w).Child(SidebarNav.Content(w, _nav)))
                .Child(SidebarNav.Inset(w).Child(w.Div().Class("p-4 text-sm text-muted-foreground").Child("Nội dung")))));
}
