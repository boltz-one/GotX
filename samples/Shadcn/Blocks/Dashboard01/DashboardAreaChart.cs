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

    // Khung đặt tuyệt đối của phần tử nền (lưới, nhãn, cột chạm): đặt thẳng vào Style thay vì chuỗi class, vì mỗi kích thước
    // mới sinh ~100 chuỗi class chưa từng gặp (biên dịch Tailwind ≈ 60 % chi phí dựng hình khi kéo giãn cửa sổ).
    /// <summary>Khung đặt tuyệt đối đã quy ra <see cref="Length"/> (làm tròn 3 chữ số như khi đi qua class <c>left-[…px]</c>) một lần lúc dựng hình, để Render lại không định dạng chuỗi.</summary>
    readonly struct Box
    {
        public readonly Length Left, Top, W, H;
        public readonly bool HasW, HasH;

        public Box(float left, float top, float w, float h)
        {
            Left = Px(left); Top = Px(top);
            HasW = !float.IsNaN(w); HasH = !float.IsNaN(h);
            W = HasW ? Px(w) : default; H = HasH ? Px(h) : default;
        }
    }

    sealed record Shape(DashboardChartBands.Strip[] Mobile, DashboardChartBands.Strip[] Desktop, VectorPath MobileStroke,
        VectorPath DesktopStroke, Box[] Grid, (string Text, Box Box)[] Ticks, DashboardChartGeometry Geometry, Box[] Columns, Length Width);

    const string GridClass = "absolute h-px bg-border/50", TickClass = "absolute w-[80px] text-center leading-4 text-muted-foreground";
    const int MaxShapes = 64;

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
        if (!_shapes.TryGetValue((_range, _size), out var shape))
        {
            if (_shapes.Count >= MaxShapes) _shapes.Clear(); // kéo giãn sinh một kích thước mới mỗi frame: không để bảng nở mãi
            _shapes[(_range, _size)] = shape = Build(w);
        }
        _shape = shape;
        var root = w.Div().Key(PlotKey).Anchor(PlotAnchor).Class("relative h-[250px] w-full text-xs");
        foreach (var box in shape.Grid) root.Child(At(w.Div().Class(GridClass), box));
        foreach (var s in shape.Mobile) root.Child(Strip(w, s, shape.Width));
        root.Child(w.Path(shape.MobileStroke).Class("absolute inset-0 object-fill text-primary"));
        foreach (var s in shape.Desktop) root.Child(Strip(w, s, shape.Width));
        root.Child(w.Path(shape.DesktopStroke).Class("absolute inset-0 object-fill text-primary"));
        foreach (var (text, box) in shape.Ticks) root.Child(At(w.Div().Class(TickClass), box).Child(text));
        Hover(w, root, shape);
        return root;
    }

    Shape Build(Window w)
    {
        BuildCount++;
        var style = TextStyle.Default with { FontSize = 12, LineHeight = 16 };
        var g = DashboardChartGeometry.Build(_size, DashboardChartData.StartIndex(_range), s => w.Text.Measure(s, style));
        float W = _size.Width, H = _size.Height;
        var grid = new Box[g.GridY.Length];
        for (int i = 0; i < g.GridY.Length; i++) grid[i] = new Box(g.Left, g.GridY[i] - 0.5f, g.Width, float.NaN);
        float y = g.Top + g.Height + DashboardChartGeometry.TickY;
        var ticks = g.Ticks.Select(t => (DashboardChartData.Labels[t.Index], new Box(t.X - 40, y, float.NaN, float.NaN))).ToArray();
        return new Shape(
            DashboardChartBands.Build(g.MobileArea, g.MobileBox, 0.8f, W),
            DashboardChartBands.Build(g.DesktopArea, g.DesktopBox, 1f, W),
            VectorPath.Polyline(g.MobileLine, W, H, 1), VectorPath.Polyline(g.TotalLine, W, H, 1), grid, ticks, g, HitColumns(g), Px(W));
    }

    /// <summary>Đặt tuyệt đối theo <see cref="Box"/> (không có W / H thì không đặt).</summary>
    static Div At(Div d, in Box b)
    {
        ref var s = ref d.Style;
        s.Position = Position.Absolute;
        s.Inset.Left = b.Left;
        s.Inset.Top = b.Top;
        if (b.HasW) s.Width = b.W;
        if (b.HasH) s.Height = b.H;
        return d;
    }

    static Length Px(float v)
    {
        float q = float.Parse(MathF.Abs(v).ToString("0.###", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
        return Length.Px(v < 0 ? -q : q);
    }

    static Div Strip(Window w, DashboardChartBands.Strip s, Length width)
    {
        var d = w.Path(s.Path).Class(s.Class);
        ref var st = ref d.Style;
        st.Inset.Top = Length.Px(s.Y);
        st.Height = Length.Px(DashboardChartBands.Band);
        st.Width = width;
        return d;
    }

    static string PxClass(string prefix, float v) =>
        (v < 0 ? "-" : "") + prefix + "-[" + MathF.Abs(v).ToString("0.###", CultureInfo.InvariantCulture) + "px]";

    /// <summary>Mốc đo chạy ngay sau frame (thay AfterFrame nội bộ của GotX.Components): Notify xếp một lượt Observe.</summary>
    sealed class MeasureMark : Entity
    {
        public void Request() => Notify();
    }
}
