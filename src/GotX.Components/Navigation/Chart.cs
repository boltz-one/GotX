namespace GotX.Components;

/// <summary>Dạng biểu đồ của <see cref="Chart"/>.</summary>
public enum ChartKind : byte { Bar, Line, Area }

/// <summary>
/// Biểu đồ cột, đường hoặc vùng trên cùng một khung (trục Y, lưới, nhãn, chú giải). Đường và vùng là hình vector
/// (<see cref="VectorPath"/>) vẽ bằng màu chữ của từng dãy (<see cref="ChartSeries.StrokeClass"/>).
/// View giữ các hình đã dựng và chỉ dựng lại khi dữ liệu hoặc kích thước vùng vẽ đổi – không tạo hình mới mỗi frame,
/// vì mỗi VectorPath chiếm một mask riêng trong atlas. Không có tooltip / điểm đánh dấu khi rê chuột như Recharts.
/// </summary>
public sealed class Chart : View
{
    AfterFrame _measure = null!;
    IReadOnlyList<string> _categories = [];
    IReadOnlyList<ChartSeries> _series = [];
    VectorPath[] _lines = [], _areas = [];
    Size _builtFor, _size = new(480, 192);
    Window? _window;
    ChartKind _kind = ChartKind.Line;
    bool _smooth = true, _dirty = true;
    int _ticks = 4;
    float _stroke = 2;
    string? _anchor;

    public ChartKind Kind { get => _kind; set { _kind = value; Invalidate(); } }

    /// <summary>Nhãn các nhóm trên trục X.</summary>
    public IReadOnlyList<string> Categories { get => _categories; set { _categories = value; Invalidate(); } }

    /// <summary>Các dãy số. Gán danh sách mới để biểu đồ dựng lại (không sửa tại chỗ danh sách đã gán).</summary>
    public IReadOnlyList<ChartSeries> Series { get => _series; set { _series = value; Invalidate(); } }

    /// <summary>Số khoảng chia trục Y.</summary>
    public int Ticks { get => _ticks; set { _ticks = Math.Max(1, value); Invalidate(); } }

    /// <summary>Đường cong mượt đi qua mọi điểm và không vọt quá giá trị (nội suy đơn điệu); tắt thì nối thẳng.</summary>
    public bool Smooth { get => _smooth; set { _smooth = value; Invalidate(); } }

    /// <summary>Độ dày nét (px).</summary>
    public float StrokeWidth { get => _stroke; set { _stroke = value; Invalidate(); } }

    /// <summary>Class chiều cao vùng vẽ.</summary>
    public string PlotClass { get; set; } = "h-48";

    /// <summary>Với dạng cột: tiền tố key của từng cột (xem <see cref="Ui.BarChart"/>).</summary>
    public string? KeyPrefix { get; set; }

    /// <summary>Tên anchor của lớp đường – <c>window.AnchorBounds</c> cho hộp của nó trên màn hình (điểm thứ c nằm ở (c + 0.5) / số nhóm bề ngang).</summary>
    public string PlotAnchor => _anchor ??= "gotx-chart-" + Id.Value;

    /// <summary>Số lần đã dựng lại hình vector – để kiểm tra rằng vẽ lại nhiều frame không dựng lại hình.</summary>
    public int BuildCount { get; private set; }

    /// <summary>Tạo mốc đo vùng vẽ sau mỗi frame có dựng biểu đồ.</summary>
    protected override void OnCreated() => _measure = AfterFrame.Create(App, Measure);

    void Invalidate() { _dirty = true; Notify(); }

    /// <summary>Vẽ khung và lớp đường/vùng; hình vector lấy từ bộ nhớ của view.</summary>
    public override Div Render(Window w)
    {
        if (_kind == ChartKind.Bar) return Ui.BarChart(w, _categories, _series, _ticks, PlotClass, KeyPrefix);
        float top = Ui.ChartTop(_series, _ticks);

        // viewBox của hình = đúng số pixel vùng vẽ, để nét dày đúng StrokeWidth px dù biểu đồ rộng hẹp thế nào. Số đo của
        // frame này chỉ có sau khi layout xong (trong Render, AnchorBounds còn là của frame trước – sai khi vùng vẽ vừa đổi cỡ),
        // nên đo ngay sau frame; frame đầu dựng tạm theo cỡ ước lượng, đo xong mà khác thì dựng lại đúng một lần.
        _window = w;
        _measure.Request();
        if (_dirty || _size != _builtFor) Rebuild(_size, top);

        // Lớp đường lùi vào bằng đệm ngang của cột/nhãn (inset-x-2) để điểm dữ liệu thẳng với nhãn trục X.
        var marks = w.Div().Class("absolute inset-y-0 inset-x-2").Anchor(PlotAnchor);
        if (_kind == ChartKind.Area)
            for (int s = 0; s < _areas.Length; s++)
                marks.Child(w.Path(_areas[s]).Class("absolute inset-0 object-fill opacity-40").Class(_series[s].StrokeClass));
        for (int s = 0; s < _lines.Length; s++)
            marks.Child(w.Path(_lines[s]).Class("absolute inset-0 object-fill").Class(_series[s].StrokeClass));
        return Ui.ChartFrame(w, _categories, _series, top, _ticks, PlotClass, marks);
    }

    void Measure()
    {
        if (_window?.AnchorBounds(PlotAnchor) is not { Width: > 0, Height: > 0 } b) return;
        var size = new Size(MathF.Round(b.Width), MathF.Round(b.Height));
        if (size == _size) return;
        _size = size;
        Notify();
    }

    void Rebuild(Size size, float top)
    {
        _dirty = false;
        _builtFor = size;
        BuildCount++;
        int n = _categories.Count;
        _lines = new VectorPath[n > 0 ? _series.Count : 0];
        _areas = new VectorPath[_kind == ChartKind.Area ? _lines.Length : 0];
        var pts = new Point[n];
        for (int s = 0; s < _lines.Length; s++)
        {
            var values = _series[s].Values;
            for (int c = 0; c < n; c++)
            {
                float v = c < values.Count && top > 0 ? Math.Clamp(values[c] / top, 0, 1) : 0;
                // Điểm nằm giữa "ô" của nhóm, thẳng với cột và nhãn trục X; lùi nửa nét để đường ở 0 / đỉnh không bị cắt mất.
                pts[c] = new Point(size.Width * (c + 0.5f) / n, Math.Clamp(size.Height * (1 - v), _stroke / 2, size.Height - _stroke / 2));
            }
            var curve = _smooth ? Monotone(pts, 8) : (Point[])pts.Clone();
            _lines[s] = VectorPath.Polyline(curve, size.Width, size.Height, _stroke);
            if (_areas.Length == 0) continue;
            // Vùng tô: đường cong khép xuống đường đáy.
            var poly = new Point[curve.Length + 2];
            curve.CopyTo(poly, 0);
            poly[^2] = new Point(curve[^1].X, size.Height);
            poly[^1] = new Point(curve[0].X, size.Height);
            _areas[s] = VectorPath.Polyline(poly, size.Width, size.Height, 0);
        }
    }

    /// <summary>
    /// Lấy mẫu đường cong Hermite đơn điệu (Fritsch–Carlson) qua các điểm: mượt như <c>type="monotone"</c> của Recharts
    /// mà không tạo đỉnh/đáy giả giữa hai điểm dữ liệu. <paramref name="steps"/> đoạn thẳng cho mỗi khoảng.
    /// </summary>
    static Point[] Monotone(Point[] p, int steps)
    {
        int n = p.Length;
        if (n < 3) return (Point[])p.Clone();
        var slope = new float[n - 1];
        var m = new float[n];
        for (int i = 0; i < n - 1; i++) slope[i] = (p[i + 1].Y - p[i].Y) / (p[i + 1].X - p[i].X);
        m[0] = slope[0];
        m[n - 1] = slope[n - 2];
        for (int i = 1; i < n - 1; i++) m[i] = slope[i - 1] * slope[i] <= 0 ? 0 : (slope[i - 1] + slope[i]) / 2;
        for (int i = 0; i < n - 1; i++)
        {
            if (slope[i] == 0) { m[i] = m[i + 1] = 0; continue; }
            float a = m[i] / slope[i], b = m[i + 1] / slope[i], len = a * a + b * b;
            if (len <= 9) continue;
            float k = 3 / MathF.Sqrt(len);
            m[i] = k * a * slope[i];
            m[i + 1] = k * b * slope[i];
        }
        var result = new Point[(n - 1) * steps + 1];
        for (int i = 0; i < n - 1; i++)
        {
            float h = p[i + 1].X - p[i].X;
            for (int k = 0; k < steps; k++)
            {
                float t = (float)k / steps, t2 = t * t, t3 = t2 * t;
                float y = (2 * t3 - 3 * t2 + 1) * p[i].Y + (t3 - 2 * t2 + t) * h * m[i] + (-2 * t3 + 3 * t2) * p[i + 1].Y + (t3 - t2) * h * m[i + 1];
                result[i * steps + k] = new Point(p[i].X + t * h, y);
            }
        }
        result[^1] = p[^1];
        return result;
    }
}
