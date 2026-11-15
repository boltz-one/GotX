using GotX;

/// <summary>
/// Hình học của AreaChart Recharts trong chart-area-interactive.tsx, tính theo đúng quy tắc của Recharts 2 / d3 để đường
/// cong, lưới và nhãn trùng bản gốc: lề 5px mỗi phía, trục X cao 30px (không vẽ trục Y), trục Y ẩn có domain
/// [0, auto] với 5 vạch "đẹp" (getNiceTickValues), trục X kiểu point (điểm đầu/cuối chạm mép vùng vẽ), đường cong
/// <c>type="natural"</c> (d3.curveNatural), hai vùng xếp chồng (mobile dưới, desktop trên), nhãn trục X thưa theo
/// <c>interval="preserveEnd"</c> + <c>minTickGap=32</c>.
/// </summary>
sealed class DashboardChartGeometry
{
    public const float Margin = 5, AxisHeight = 30, TickY = 14; // nhãn: tickSize 6 + tickMargin 8 dưới đáy vùng vẽ

    public float Left, Top, Width, Height;             // vùng vẽ
    public float[] GridY = [];                          // 5 đường lưới ngang (y tuyệt đối trong khung)
    public Point[] MobileLine = [], TotalLine = [];     // đường cong đã lấy mẫu
    public Point[] MobileArea = [], DesktopArea = [];   // đa giác vùng tô
    public (float Top, float Bottom) MobileBox, DesktopBox; // hộp bao theo trục y (cho gradient objectBoundingBox)
    public List<(int Index, float X)> Ticks = [];       // nhãn được hiện: chỉ số điểm + toạ độ tâm
    public Point[] MobilePoints = [], TotalPoints = []; // điểm dữ liệu (cho tooltip / activeDot)
    public int Start;                                   // chỉ số dữ liệu của điểm đầu

    public static DashboardChartGeometry Build(Size size, int start, Func<string, float> measure)
    {
        var g = new DashboardChartGeometry
        {
            Left = Margin, Top = Margin, Width = size.Width - 2 * Margin, Height = size.Height - 2 * Margin - AxisHeight,
        };
        int n = DashboardChartData.Desktop.Length - start;
        float max = 0;
        for (int i = start; i < start + n; i++) max = Math.Max(max, DashboardChartData.Desktop[i] + DashboardChartData.Mobile[i]);
        float top = NiceTop(max, 5);
        g.GridY = new float[5];
        for (int t = 0; t < 5; t++) g.GridY[t] = g.Top + g.Height * (1 - t / 4f);

        var mobile = new Point[n];
        var total = new Point[n];
        for (int i = 0; i < n; i++)
        {
            float x = g.Left + (n == 1 ? g.Width / 2 : g.Width * i / (n - 1));
            float m = DashboardChartData.Mobile[start + i], d = DashboardChartData.Desktop[start + i];
            mobile[i] = new Point(x, g.Top + g.Height * (1 - m / top));
            total[i] = new Point(x, g.Top + g.Height * (1 - (m + d) / top));
        }
        g.MobilePoints = mobile;
        g.TotalPoints = total;
        g.Start = start;
        g.MobileLine = Natural(mobile);
        g.TotalLine = Natural(total);
        float baseY = g.Top + g.Height;
        g.MobileArea = [.. g.MobileLine, new Point(g.MobileLine[^1].X, baseY), new Point(g.MobileLine[0].X, baseY)];
        var reversed = (Point[])g.MobileLine.Clone();
        Array.Reverse(reversed);
        g.DesktopArea = [.. g.TotalLine, .. reversed];
        g.MobileBox = (MinY(g.MobileLine), baseY);
        g.DesktopBox = (MinY(g.TotalLine), MaxY(g.MobileLine));
        g.Ticks = PreserveEnd(g, start, n, measure);
        return g;
    }

    static float MinY(Point[] p) { float v = float.MaxValue; foreach (var q in p) v = Math.Min(v, q.Y); return v; }
    static float MaxY(Point[] p) { float v = float.MinValue; foreach (var q in p) v = Math.Max(v, q.Y); return v; }

    /// <summary>getTicksEnd của Recharts: duyệt từ nhãn cuối về đầu, giữ nhãn nào không đè nhãn đã giữ (cộng minTickGap).</summary>
    static List<(int, float)> PreserveEnd(DashboardChartGeometry g, int start, int n, Func<string, float> measure)
    {
        const float minTickGap = 32;
        var shown = new List<(int, float)>();
        float begin = g.Left, end = g.Left + g.Width;
        for (int i = n - 1; i >= 0; i--)
        {
            float size = measure(DashboardChartData.Labels[start + i]);
            float x = g.Left + (n == 1 ? g.Width / 2 : g.Width * i / (n - 1));
            if (i == n - 1)
            {
                float gap = x + size / 2 - end;
                if (gap > 0) x -= gap; // nhãn cuối bị đẩy vào trong mép phải
            }
            if (x - size / 2 >= begin && x + size / 2 <= end)
            {
                end = x - (size / 2 + minTickGap);
                shown.Insert(0, (start + i, x));
            }
        }
        return shown;
    }

    /// <summary>getNiceTickValues(0, max, tickCount) của recharts-scale (allowDecimals): đỉnh = số bước "đẹp" × (tickCount − 1).</summary>
    public static float NiceTop(float max, int tickCount)
    {
        if (max <= 0) return tickCount - 1;
        for (int correction = 0; ; correction++)
        {
            double rough = max / (tickCount - 1.0);
            int digits = (int)Math.Floor(Math.Log10(rough)) + 1;
            double unit = Math.Pow(10, digits), scale = digits != 1 ? 0.05 : 0.1;
            double step = (Math.Ceiling(rough / unit / scale) + correction) * scale * unit;
            int up = (int)Math.Ceiling(max / step);
            if (up + 1 <= tickCount) return (float)(step * (tickCount - 1));
        }
    }

    /// <summary>d3.curveNatural: spline bậc ba tự nhiên qua mọi điểm, mỗi đoạn Bézier lấy 12 mẫu.</summary>
    static Point[] Natural(Point[] p)
    {
        int n = p.Length - 1;
        if (n < 1) return (Point[])p.Clone();
        if (n == 1) return [p[0], p[1]];
        var xs = new float[p.Length];
        var ys = new float[p.Length];
        for (int i = 0; i <= n; i++) { xs[i] = p[i].X; ys[i] = p[i].Y; }
        var (ax, bx) = ControlPoints(xs);
        var (ay, by) = ControlPoints(ys);
        const int steps = 12;
        var result = new Point[n * steps + 1];
        for (int i = 0; i < n; i++)
            for (int k = 0; k < steps; k++)
            {
                float t = (float)k / steps, u = 1 - t;
                float b0 = u * u * u, b1 = 3 * u * u * t, b2 = 3 * u * t * t, b3 = t * t * t;
                result[i * steps + k] = new Point(
                    b0 * xs[i] + b1 * ax[i] + b2 * bx[i] + b3 * xs[i + 1],
                    b0 * ys[i] + b1 * ay[i] + b2 * by[i] + b3 * ys[i + 1]);
            }
        result[^1] = p[^1];
        return result;
    }

    /// <summary>controlPoints của d3-shape/natural.js (giải hệ ba đường chéo).</summary>
    static (float[] A, float[] B) ControlPoints(float[] x)
    {
        int n = x.Length - 1;
        var a = new float[n];
        var b = new float[n];
        var r = new float[n];
        a[0] = 0; b[0] = 2; r[0] = x[0] + 2 * x[1];
        for (int i = 1; i < n - 1; i++) { a[i] = 1; b[i] = 4; r[i] = 4 * x[i] + 2 * x[i + 1]; }
        a[n - 1] = 2; b[n - 1] = 7; r[n - 1] = 8 * x[n - 1] + x[n];
        for (int i = 1; i < n; i++) { float m = a[i] / b[i - 1]; b[i] -= m; r[i] -= m * r[i - 1]; }
        a[n - 1] = r[n - 1] / b[n - 1];
        for (int i = n - 2; i >= 0; i--) a[i] = (r[i] - a[i + 1]) / b[i];
        b[n - 1] = (x[n] + a[n - 1]) / 2;
        for (int i = 0; i < n - 1; i++) b[i] = 2 * x[i + 1] - a[i + 1];
        return (a, b);
    }
}
