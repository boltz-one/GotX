using System.Globalization;
using GotX;
using GotX.Components;

/// <summary>
/// Rê chuột trên biểu đồ (ChartTooltip cursor={false} + ChartTooltipContent indicator="dot", activeDot mặc định của
/// Recharts): điểm gần nhất theo trục X được chọn bằng các cột chạm vô hình (mỗi điểm một cột từ trung điểm tới trung
/// điểm); hiện chấm 8px trên hai đường và hộp tooltip: ngày, rồi Mobile / Desktop với chấm màu và số. Core không báo vị
/// trí chuột khi chỉ rê, nên hộp đặt cạnh điểm (x + 10, phía trên đỉnh) thay vì bám con trỏ như Recharts.
/// </summary>
public sealed partial class DashboardAreaChart
{
    const string TooltipClass =
        "absolute grid min-w-[8rem] items-start gap-1.5 rounded-lg border border-border/50 bg-background px-2.5 py-1.5 text-xs shadow-xl";
    const string Dot = "size-2.5 shrink-0 rounded-[2px] bg-primary";

    int _hover = -1;
    Indexed<DashboardAreaChart>[]? _hitRefs;

    /// <summary>Điểm đang được rê (chỉ số trong khoảng đang vẽ); -1 khi không.</summary>
    public int HoverIndex => _hover;

    static string[] HitColumns(DashboardChartGeometry g)
    {
        int n = g.MobilePoints.Length;
        var cols = new string[n];
        for (int i = 0; i < n; i++)
        {
            float x0 = i == 0 ? g.Left : (g.MobilePoints[i - 1].X + g.MobilePoints[i].X) / 2;
            float x1 = i == n - 1 ? g.Left + g.Width : (g.MobilePoints[i].X + g.MobilePoints[i + 1].X) / 2;
            cols[i] = "absolute " + Px("left", x0) + " " + Px("w", x1 - x0) + " " + Px("top", g.Top) + " " + Px("h", g.Height);
        }
        return cols;
    }

    void SetHover(int index, bool on)
    {
        int next = on ? index : _hover == index ? -1 : _hover;
        if (next == _hover) return;
        _hover = next;
        Notify();
    }

    void Hover(Window w, Div root, Shape shape)
    {
        var refs = Indexed<DashboardAreaChart>.Ensure(ref _hitRefs, this, shape.Columns.Length);
        for (int i = 0; i < shape.Columns.Length; i++)
            root.Child(w.Div().Class(shape.Columns[i]).OnHover(refs[i], static (r, on) => r.Owner.SetHover(r.Index, on)));
        var g = shape.Geometry;
        if ((uint)_hover >= (uint)g.MobilePoints.Length) return;
        var m = g.MobilePoints[_hover];
        var t = g.TotalPoints[_hover];
        root.Child(w.Div().Class("absolute size-2 rounded-full bg-primary " + Px("left", m.X - 4) + " " + Px("top", m.Y - 4)));
        root.Child(w.Div().Class("absolute size-2 rounded-full bg-primary " + Px("left", t.X - 4) + " " + Px("top", t.Y - 4)));
        int data = g.Start + _hover;
        bool flip = t.X + 10 + 128 > g.Left + g.Width;
        string place = flip ? Px("right", g.Left + g.Width + 2 * DashboardChartGeometry.Margin - t.X + 10) : Px("left", t.X + 10);
        root.Child(w.Div().Key("chart-tooltip").Class(TooltipClass + " " + place + " " + Px("top", Math.Max(0, t.Y - 70)))
            .Child(w.Div().Class("font-medium").Child(DashboardChartData.Labels[data]))
            .Child(w.Div().Class("grid gap-1.5")
                .Child(Row(w, "Mobile", DashboardChartData.Mobile[data]))
                .Child(Row(w, "Desktop", DashboardChartData.Desktop[data]))));
    }

    static Div Row(Window w, string label, float value) =>
        w.Div().Class("flex w-full items-center gap-2")
            .Child(w.Div().Class(Dot))
            .Child(w.Div().Class("flex flex-1 items-center justify-between leading-none")
                .Child(w.Div().Class("text-muted-foreground").Child(label))
                .Child(w.Div().Class("font-mono font-medium text-foreground tabular-nums").Child(value.ToString("0", CultureInfo.InvariantCulture))));
}
