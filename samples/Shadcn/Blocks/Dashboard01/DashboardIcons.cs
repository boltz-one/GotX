using System.Globalization;
using GotX;
using GotX.Components;

/// <summary>
/// Icon mà dashboard-01 cần nhưng <see cref="Icon"/> chưa có. Bản gốc dùng bộ Tabler; ở đây lấy icon Lucide cùng nghĩa
/// (cùng bộ với phần còn lại của GotX.Components). Mỗi path tạo một lần: một VectorPath chiếm một mask trong atlas.
/// </summary>
public static class DashboardIcons
{
    static readonly VectorPath DashboardPath = VectorPath.Icon("rect 3 3 7 9 1", "rect 14 3 7 5 1", "rect 14 12 7 9 1", "rect 3 16 7 5 1");
    static readonly VectorPath ListPath = VectorPath.Icon("M3 5h.01", "M3 12h.01", "M3 19h.01", "M8 5h13", "M8 12h13", "M8 19h13");
    static readonly VectorPath ChartBarPath = VectorPath.Icon("M3 3v16a2 2 0 0 0 2 2h16", "M18 17V9", "M13 17V5", "M8 17v-3");
    static readonly VectorPath UsersPath = VectorPath.Icon("M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2", "circle 9 7 4",
        "M22 21v-2a4 4 0 0 0-3-3.87", "M16 3.13a4 4 0 0 1 0 7.75");
    static readonly VectorPath HelpPath = VectorPath.Icon("circle 12 12 10", "M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3", "M12 17h.01");
    static readonly VectorPath DatabasePath = VectorPath.Icon("M3 5a9 3 0 1 0 18 0a9 3 0 1 0-18 0", "M3 5v14a9 3 0 0 0 18 0V5", "M3 12a9 3 0 0 0 18 0");
    static readonly VectorPath CirclePlusPath = VectorPath.Icon("circle 12 12 10", "M8 12h8", "M12 8v8");
    static readonly VectorPath TrendingUpPath = VectorPath.Icon("M16 7h6v6", "m22 7-8.5 8.5-5-5L2 17");
    static readonly VectorPath TrendingDownPath = VectorPath.Icon("M16 17h6v-6", "m22 17-8.5-8.5-5 5L2 7");

    static Div Draw(Window w, VectorPath path) => w.Path(path).Class("size-4 shrink-0");

    public static Div Dashboard(Window w) => Draw(w, DashboardPath);
    public static Div List(Window w) => Draw(w, ListPath);
    public static Div ChartBar(Window w) => Draw(w, ChartBarPath);
    public static Div Users(Window w) => Draw(w, UsersPath);
    public static Div Help(Window w) => Draw(w, HelpPath);
    public static Div Database(Window w) => Draw(w, DatabasePath);
    public static Div CirclePlus(Window w) => Draw(w, CirclePlusPath);
    public static Div TrendingUp(Window w) => Draw(w, TrendingUpPath);
    public static Div TrendingDown(Window w) => Draw(w, TrendingDownPath);
}

/// <summary>
/// Dữ liệu biểu đồ "Total Visitors": 91 ngày (1/4 – 30/6/2024, mốc 30/6 như <c>referenceDate</c> của bản gốc), mỗi ngày có
/// lượt desktop và mobile. Bản gốc chép cứng 91 cặp số; ở đây sinh theo công thức cố định có cùng biên độ (≈ 60–500) để
/// không phải chép bảng số – hình dạng đường cong vì thế khác bản gốc. Ba khoảng thời gian được cắt sẵn một lần.
/// </summary>
public static class DashboardChartData
{
    public static readonly string[] RangeLabels = ["Last 3 months", "Last 30 days", "Last 7 days"];
    static readonly int[] Days = [91, 30, 7];
    const int AxisLabels = 6;

    public sealed record Range(IReadOnlyList<string> Categories, IReadOnlyList<ChartSeries> Series, string[] Axis, int Points);

    public static readonly Range[] Ranges = Build();

    static Range[] Build()
    {
        var start = new DateOnly(2024, 4, 1);
        var desktop = new float[91];
        var mobile = new float[91];
        for (int d = 0; d < 91; d++)
        {
            // Hai sóng lệch pha + nhiễu tất định: trông như số liệu thật mà mọi lần chạy giống nhau.
            int noise = (d * 7919 + 104729) % 97;
            desktop[d] = 230 + 130 * MathF.Sin(d * 0.45f) + noise;
            mobile[d] = 210 + 110 * MathF.Sin(d * 0.31f + 1.3f) + (noise * 53 % 89);
        }
        var ranges = new Range[Days.Length];
        for (int r = 0; r < Days.Length; r++)
        {
            int n = Days[r], from = 91 - n;
            // Chart không xếp chồng dãy (stackId của Recharts), nên dãy "Desktop" mang sẵn tổng desktop + mobile.
            var total = new float[n];
            var lower = new float[n];
            // Nhãn trục X của Chart chia đều cho MỌI điểm (91 nhãn sẽ chồng lên nhau), nên để trống và block tự vẽ 6 mốc ngày.
            var categories = new string[n];
            for (int i = 0; i < n; i++)
            {
                lower[i] = mobile[from + i];
                total[i] = mobile[from + i] + desktop[from + i];
                categories[i] = "";
            }
            var axis = new string[AxisLabels];
            for (int a = 0; a < AxisLabels; a++)
                axis[a] = start.AddDays(from + a * (n - 1) / (AxisLabels - 1)).ToString("MMM d", CultureInfo.InvariantCulture);
            ranges[r] = new Range(categories, [new("Desktop", "bg-primary", total), new("Mobile", "bg-muted-foreground", lower)], axis, n);
        }
        return ranges;
    }
}
