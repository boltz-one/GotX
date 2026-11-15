using GotX;
using GotX.Components;

/// <summary>
/// Thẻ "Total Visitors" của dashboard-01 (chart-area-interactive.tsx): biểu đồ vùng + nhóm nút chọn khoảng thời gian.
/// Là view riêng để đổi khoảng thời gian chỉ vẽ lại thẻ này, không phải cả dashboard.
/// Khác bản gốc: có trục Y và chú giải (khung của <see cref="Chart"/>), không có tooltip khi rê chuột, vùng tô một màu
/// thay cho gradient; nhóm nút luôn hiện (bản gốc đổi sang Select khi thẻ hẹp hơn 767px – cần container query).
/// </summary>
public sealed class DashboardChart : View
{
    public const string CardKey = "dashboard-chart", AxisKey = "dashboard-chart-axis";

    ToggleGroup _toggle = null!;
    Chart _chart = null!;
    int _range;

    public Chart Chart => _chart;
    public ToggleGroup Toggle => _toggle;

    /// <summary>Số điểm dữ liệu đang vẽ (91 / 30 / 7).</summary>
    public int Points => DashboardChartData.Ranges[_range].Points;

    /// <summary>Khoảng thời gian đang chọn: 0 = 3 tháng, 1 = 30 ngày, 2 = 7 ngày.</summary>
    public int Range
    {
        get => _range;
        set
        {
            if (_range == value) return;
            _range = value;
            var data = DashboardChartData.Ranges[value];
            _chart.Categories = data.Categories;
            _chart.Series = data.Series;
            if (!_toggle.IsPressed(value)) _toggle.SetPressed(value, true);
            Notify();
        }
    }

    protected override void OnCreated()
    {
        var data = DashboardChartData.Ranges[0];
        _chart = App.New(new Chart
        {
            Kind = ChartKind.Area, Categories = data.Categories, Series = data.Series, PlotClass = "h-[250px]", StrokeWidth = 1.5f,
        });
        _toggle = App.New(new ToggleGroup { Items = DashboardChartData.RangeLabels, Variant = ToggleVariant.Outline });
        _toggle.SetPressed(0, true);
        App.Subscribe<ToggleGroup.ValueChanged>(_toggle, e =>
        {
            // ToggleGroup một-lựa-chọn cho phép bấm lại để tắt; biểu đồ luôn cần một khoảng nên bật lại nút đang chọn.
            if (e.Selected.Count == 0) _toggle.SetPressed(_range, true);
            else Range = e.Selected[0];
        });
    }

    public override Div Render(Window w)
    {
        var axis = w.Div().Key(AxisKey).Class("flex justify-between pl-14 pr-2 text-xs text-muted-foreground");
        foreach (string label in DashboardChartData.Ranges[_range].Axis) axis.Child(w.Div().Child(label));
        return Ui.Card(w).Key(CardKey).Class("shadow-xs")
            .Child(Ui.CardHeader(w, hasAction: true)
                .Child(Ui.CardTitle(w, "Total Visitors"))
                .Child(Ui.CardAction(w).Child(w.View(_toggle)))
                .Child(Ui.CardDescription(w, "Total for the last 3 months")))
            .Child(Ui.CardContent(w).Class("flex flex-col gap-1 pt-2").Child(w.View(_chart)).Child(axis));
    }
}

/// <summary>Bốn thẻ số liệu đầu trang (section-cards.tsx). Không có state nên chỉ là hàm dựng.</summary>
public static class DashboardCards
{
    /// <summary>
    /// Lưới thẻ: 1 cột → 2 cột (md) → 4 cột (xl). Bản gốc chia theo bề rộng vùng nội dung (<c>@xl/main</c>, <c>@5xl/main</c>);
    /// Core chưa có container query nên dùng breakpoint cửa sổ cho ra cùng số cột ở 1440 và 1024. Nền gradient của thẻ
    /// (<c>from-primary/5 to-card</c>) thay bằng nền card đặc.
    /// </summary>
    public static Div Render(Window w)
    {
        var grid = w.Div().Key("dashboard-cards").Class("grid shrink-0 grid-cols-1 gap-4 px-4 md:grid-cols-2 lg:px-6 xl:grid-cols-4");
        foreach (var stat in Dashboard01Data.Stats)
        {
            Func<Window, Div> trend = stat.Up ? DashboardIcons.TrendingUp : DashboardIcons.TrendingDown;
            grid.Child(Ui.Card(w).Class("shadow-xs")
                .Child(Ui.CardHeader(w, hasAction: true)
                    .Child(Ui.CardDescription(w, stat.Label))
                    .Child(Ui.CardAction(w).Child(Ui.Badge(w, BadgeVariant.Outline).Child(trend(w).Class("size-3")).Child(stat.Trend)))
                    .Child(w.Div().Class("text-2xl font-semibold").Child(stat.Value)))
                .Child(w.Div().Class("flex flex-col items-start gap-1.5 px-4 text-sm")
                    .Child(w.Div().Class("flex items-center gap-2 font-medium").Child(w.Div().Class("truncate").Child(stat.Headline)).Child(trend(w)))
                    .Child(w.Div().Class("text-muted-foreground").Child(stat.Note))));
        }
        return grid;
    }
}
