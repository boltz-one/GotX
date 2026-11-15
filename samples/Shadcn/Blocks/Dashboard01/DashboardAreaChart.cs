using System.Globalization;
using GotX;

/// <summary>
/// AreaChart của "Total Visitors" (ChartContainer <c>aspect-auto h-[250px] w-full</c>): lưới ngang border/50, hai vùng xếp
/// chồng tô gradient dọc (<see cref="DashboardChartBands"/>), nét 1px màu primary, nhãn trục X 12px muted. Hình của mỗi
/// (khoảng thời gian, kích thước) dựng một lần rồi giữ lại – đổi qua lại giữa các khoảng không tạo thêm VectorPath (mỗi
/// path chiếm một mask trong atlas).
/// </summary>
public sealed partial class DashboardAreaChart : View
{
    public const string PlotKey = "chart", PlotAnchor = "dashboard-chart-plot";

    sealed record Shape(DashboardChartBands.Strip[] Mobile, DashboardChartBands.Strip[] Desktop, VectorPath MobileStroke,
        VectorPath DesktopStroke, string[] Grid, (string Text, string Class)[] Ticks, DashboardChartGeometry Geometry, string[] Columns);

    readonly MeasureMark _mark = new();
    readonly Dictionary<(string, Size), Shape> _shapes = [];
    string _range = "90d";
    Size _size = new(1046, 250);
    Shape? _shape;
    Window? _window;

    /// <summary>Số lần dựng hình mới (kiểm tra rằng vẽ lại không dựng lại path).</summary>
    public int BuildCount { get; private set; }

    /// <summary>Số điểm đang vẽ (91 / 31 / 8).</summary>
    public int Points => DashboardChartData.Desktop.Length - DashboardChartData.StartIndex(_range);

    /// <summary>Nhãn trục X đang hiện, theo thứ tự.</summary>
    public IEnumerable<string> TickLabels => _shape?.Ticks.Select(static t => t.Text) ?? [];

    /// <summary>Giá trị khoảng thời gian ("90d" / "30d" / "7d" / "" như timeRange của bản gốc).</summary>
    public string Range
    {
        get => _range;
        set { if (_range == value) return; _range = value; Notify(); }
    }

    protected override void OnCreated()
    {
        App.New(_mark);
        App.Observe(_mark, Measure);
    }

    void Measure()
    {
        if (_window?.AnchorBounds(PlotAnchor) is not { Width: > 0, Height: > 0 } b) return;
        var size = new Size(MathF.Round(b.Width), MathF.Round(b.Height));
        if (size == _size) return;
        _size = size;
        Notify();
    }

    public override Div Render(Window w)
    {
        _window = w;
        _mark.Request();
        if (!_shapes.TryGetValue((_range, _size), out var shape)) _shapes[(_range, _size)] = shape = Build(w);
        _shape = shape;
        var root = w.Div().Key(PlotKey).Anchor(PlotAnchor).Class("relative h-[250px] w-full text-xs");
        foreach (string grid in shape.Grid) root.Child(w.Div().Class(grid));
        foreach (var s in shape.Mobile) root.Child(w.Path(s.Path).Class(s.Class));
        root.Child(w.Path(shape.MobileStroke).Class("absolute inset-0 object-fill text-primary"));
        foreach (var s in shape.Desktop) root.Child(w.Path(s.Path).Class(s.Class));
        root.Child(w.Path(shape.DesktopStroke).Class("absolute inset-0 object-fill text-primary"));
        foreach (var (text, cls) in shape.Ticks) root.Child(w.Div().Class(cls).Child(text));
        Hover(w, root, shape);
        return root;
    }

    Shape Build(Window w)
    {
        BuildCount++;
        var style = TextStyle.Default with { FontSize = 12, LineHeight = 16 };
        var g = DashboardChartGeometry.Build(_size, DashboardChartData.StartIndex(_range), s => w.Text.Measure(s, style));
        float W = _size.Width, H = _size.Height;
        var grid = new string[g.GridY.Length];
        for (int i = 0; i < g.GridY.Length; i++)
            grid[i] = "absolute h-px bg-border/50 " + Px("left", g.Left) + " " + Px("w", g.Width) + " " + Px("top", g.GridY[i] - 0.5f);
        float y = g.Top + g.Height + DashboardChartGeometry.TickY;
        var ticks = g.Ticks.Select(t => (DashboardChartData.Labels[t.Index],
            "absolute w-[80px] text-center leading-4 text-muted-foreground " + Px("left", t.X - 40) + " " + Px("top", y))).ToArray();
        return new Shape(
            DashboardChartBands.Build(g.MobileArea, g.MobileBox, 0.8f, W),
            DashboardChartBands.Build(g.DesktopArea, g.DesktopBox, 1f, W),
            VectorPath.Polyline(g.MobileLine, W, H, 1), VectorPath.Polyline(g.TotalLine, W, H, 1), grid, ticks, g, HitColumns(g));
    }

    static string Px(string prefix, float v) =>
        (v < 0 ? "-" : "") + prefix + "-[" + MathF.Abs(v).ToString("0.###", CultureInfo.InvariantCulture) + "px]";

    /// <summary>Mốc đo chạy ngay sau frame (thay AfterFrame nội bộ của GotX.Components): Notify xếp một lượt Observe.</summary>
    sealed class MeasureMark : Entity
    {
        public void Request() => Notify();
    }
}
