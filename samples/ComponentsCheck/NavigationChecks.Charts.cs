using GotX;
using GotX.Components;

/// <summary>Kiểm tra biểu đồ đường / vùng: đo pixel thật tại các điểm dữ liệu và đếm số lần dựng lại hình vector.</summary>
static partial class NavigationChecks
{
    static void LineChartChecks(CheckHarness h)
    {
        Chart line = null!, area = null!;
        var w = Host(h, app =>
        {
            // Màu đậm (blue-600) để đo mực rõ ràng dù token chart-* của theme là thang xám. Nối thẳng (Smooth = false) để vị trí đường giữa hai điểm tính được chính xác.
            line = app.New(new Chart { Kind = ChartKind.Line, Smooth = false, Categories = ["A", "B", "C", "D"], PlotClass = "h-[200px]", Series = [new("S", "bg-blue-600", [100, 0, 50, 100])] });
            area = app.New(new Chart { Kind = ChartKind.Area, Smooth = false, Categories = ["A", "B", "C", "D"], PlotClass = "h-[200px]", Series = [new("S", "bg-blue-600", [100, 100, 50, 50])] });
            return w => w.Div().Class("flex flex-col gap-6 w-[500px] p-4 bg-background text-foreground").Child(w.View(line)).Child(w.View(area));
        });
        h.Wait(w, 0.1);
        int builds = line.BuildCount;
        h.Wait(w, 0.5);
        h.Check($"chart đường: hình vector dựng theo cỡ ước lượng rồi đúng một lần theo cỡ thật, sau đó không dựng lại ({builds})",
            builds == 2 && line.BuildCount == 2);

        var p = w.AnchorBounds(line.PlotAnchor) ?? default;
        // Điểm dữ liệu thứ c nằm giữa ô của nhóm c, cao theo giá trị / đỉnh trục (100).
        Point At(Bounds plot, float c, float value) => new(plot.X + plot.Width * (c + 0.5f) / 4, plot.Y + plot.Height * (1 - value / 100));
        int Ink(Point at)
        {
            var px = h.Pixel(w, at);
            return 765 - px.R - px.G - px.B; // nền trắng → 0
        }
        int onLow = Ink(At(p, 1, 0) with { Y = p.Bottom - 3 }), onMid = Ink(At(p, 2, 50)), onSegment = Ink(At(p, 2.5f, 75));
        int offAbove = Ink(At(p, 2, 80)), offBelow = Ink(At(p, 2, 20));
        h.Check($"chart đường: có mực tại điểm dữ liệu và giữa đoạn nối ({onLow}, {onMid}, {onSegment}), không có ở trên/dưới đường ({offAbove}, {offBelow})",
            p.Height > 190 && onLow > 100 && onMid > 100 && onSegment > 100 && offAbove < 30 && offBelow < 30);
        h.Check("chart đường: lớp đường phủ đúng vùng lưới 200px, cạnh trục Y", Near(p.Height, 200) && p.X > 50 && Near(p.Right, 500 - 16 - 8, 2));

        var q = w.AnchorBounds(area.PlotAnchor) ?? default;
        int fill = Ink(At(q, 0.5f, 50)), edge = Ink(At(q, 0.5f, 100) with { Y = q.Y + 1 }), empty = Ink(At(q, 2.5f, 80)), under = Ink(At(q, 2.5f, 25));
        h.Check($"chart vùng: phần dưới đường được tô nhạt hơn nét ({fill} < {edge}), phần trên đường để trống ({empty}), dưới đoạn thấp vẫn tô ({under})",
            fill > 60 && edge > fill + 60 && empty < 30 && under > 60);
        h.Save(w, "nav-chart-line-area.png");

        line.Series = [new("S", "bg-blue-600", [0, 100, 100, 0])];
        h.Frames(w);
        h.Check($"chart đường: đổi dữ liệu thì dựng lại đúng một lần và đường dời theo ({line.BuildCount})",
            line.BuildCount == 3 && Ink(At(p, 2, 40)) < 30 && Ink(At(p, 1.5f, 100) with { Y = p.Y + 1 }) > 100);
        line.Kind = ChartKind.Bar;
        line.KeyPrefix = "kbar";
        h.Frames(w);
        h.Check("chart: đổi Kind sang cột thì vẽ cột trên cùng khung", Near(h.Box(w, "kbar-0-1").Height, 200) && Near(h.Box(w, "kbar-0-0").Height, 0));
    }
}
