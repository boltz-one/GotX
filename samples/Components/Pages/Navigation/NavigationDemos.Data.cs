using GotX;
using GotX.Components;

/// <summary>Bảng, biểu đồ và sidebar mẫu cho trang Navigation.</summary>
static partial class NavigationDemos
{
    const string InvoiceCols = "grid-cols-[100px_1fr_1fr_120px]";

    static readonly (string Id, string Status, string Method, string Amount)[] Invoices =
    [
        ("INV001", "Đã trả", "Thẻ tín dụng", "$250.00"),
        ("INV002", "Chờ xử lý", "PayPal", "$150.00"),
        ("INV003", "Chưa trả", "Chuyển khoản", "$350.00"),
        ("INV004", "Đã trả", "Thẻ tín dụng", "$450.00"),
    ];

    /// <summary>Bảng hoá đơn: cột số tiền căn phải, hàng thứ hai được chọn.</summary>
    public static Div Table(Window w)
    {
        var body = Ui.TableBody(w);
        for (int i = 0; i < Invoices.Length; i++)
        {
            var (id, status, method, amount) = Invoices[i];
            var row = Ui.TableRow(w, selected: i == 1).Class(InvoiceCols);
            if (i == Invoices.Length - 1) row.Class("border-b-0");
            body.Child(row
                .Child(Ui.TableCell(w, id).Class("font-medium"))
                .Child(Ui.TableCell(w, status))
                .Child(Ui.TableCell(w, method))
                .Child(Ui.TableCell(w, amount, TableAlign.Right)));
        }
        return Ui.Table(w)
            .Child(Ui.TableHeader(w).Child(Ui.TableRow(w).Class(InvoiceCols)
                .Child(Ui.TableHead(w, "Hoá đơn")).Child(Ui.TableHead(w, "Trạng thái"))
                .Child(Ui.TableHead(w, "Phương thức")).Child(Ui.TableHead(w, "Số tiền", TableAlign.Right))))
            .Child(body)
            .Child(Ui.TableFooter(w).Child(Ui.TableRow(w).Class(InvoiceCols).Class("border-b-0")
                .Child(Ui.TableCell(w, "Tổng").Class("col-span-3"))
                .Child(Ui.TableCell(w, "$1,200.00", TableAlign.Right))))
            .Child(Ui.TableCaption(w, "Danh sách hoá đơn gần đây."));
    }

    static readonly string[] Months = ["T1", "T2", "T3", "T4", "T5", "T6"];
    static readonly ChartSeries[] Visitors =
    [
        new("Máy tính", "bg-chart-2", [186, 305, 237, 73, 209, 214]),
        new("Di động", "bg-chart-1", [80, 200, 120, 190, 130, 140]),
    ];

    /// <summary>Biểu đồ lượt truy cập 6 tháng, hai dãy.</summary>
    public static Div Chart(Window w) => Ui.BarChart(w, Months, Visitors);

    /// <summary>Cùng dữ liệu ở dạng đường hoặc vùng (view giữ hình vector đã dựng).</summary>
    public static Chart NewChart(ChartKind kind) => new() { Kind = kind, Categories = Months, Series = Visitors };

    public static readonly SidebarGroup[] SidebarGroups =
    [
        new("Ứng dụng", [new("Trang chủ"), new("Hộp thư"), new("Lịch"), new("Tìm kiếm", Icon.Search)]),
        new("Quản trị", [new("Người dùng"), new("Cài đặt")]),
    ];

    /// <summary>Logo + tên workspace; khi thu gọn chỉ còn ô logo (phần chữ bị cắt bởi overflow-hidden).</summary>
    public static Div SidebarHeader(Window w) =>
        w.Div().Class("flex h-12 items-center gap-2 overflow-hidden")
            .Child(w.Div().Class("flex size-8 shrink-0 items-center justify-center rounded-lg bg-sidebar-primary text-sidebar-primary-foreground text-sm font-semibold").Child("G"))
            .Child(w.Div().Class("flex flex-col min-w-0")
                .Child(w.Div().Class("truncate text-sm font-semibold").Child("GotX Inc."))
                .Child(w.Div().Class("truncate text-xs text-sidebar-foreground/70").Child("Enterprise")));

    /// <summary>Tài khoản người dùng ở chân sidebar.</summary>
    public static Div SidebarFooter(Window w) =>
        w.Div().Class("flex h-12 items-center gap-2 overflow-hidden")
            .Child(w.Div().Class("flex size-8 shrink-0 items-center justify-center rounded-lg bg-sidebar-accent text-xs font-medium").Child("TN"))
            .Child(w.Div().Class("flex flex-col min-w-0")
                .Child(w.Div().Class("truncate text-sm font-medium").Child("Thu Nguyễn"))
                .Child(w.Div().Class("truncate text-xs text-sidebar-foreground/70").Child("thu@example.com")));

    /// <summary>Nội dung giả lập của vùng inset: lưới thẻ trống.</summary>
    public static Div InsetBody(Window w) =>
        w.Div().Class("flex flex-1 flex-col gap-4 p-4")
            .Child(w.Div().Class("grid grid-cols-3 gap-4")
                .Child(w.Div().Class("h-24 rounded-xl bg-muted/50"))
                .Child(w.Div().Class("h-24 rounded-xl bg-muted/50"))
                .Child(w.Div().Class("h-24 rounded-xl bg-muted/50")))
            .Child(w.Div().Class("flex-1 rounded-xl bg-muted/50"));
}
