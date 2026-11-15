using GotX;
using GotX.Components;

/// <summary>
/// Thẻ "Total Visitors" của dashboard-01 (chart-area-interactive.tsx): biểu đồ vùng + chọn khoảng thời gian bằng
/// ToggleGroup (thẻ rộng ≥ 767px) hoặc Select (hẹp hơn). Là view riêng để đổi khoảng thời gian chỉ vẽ lại thẻ này.
/// Như Radix ToggleGroup type="single": bấm lại nút đang bật thì bỏ chọn (timeRange = "" → lọc như 90 ngày, Select hiện
/// placeholder). Tab vào nhóm dừng ở nút đang bật (hoặc nút đầu), ←/→ chuyển focus vòng quanh, Enter/Space bật.
/// Container query <c>@[767px]/card</c> / <c>@[540px]/card</c>: thẻ rộng = cửa sổ − 296px (sidebar + lề) − 48px (lg:px-6),
/// nên ToggleGroup từ cửa sổ 1111px, mô tả dài từ 868px.
/// </summary>
public sealed class DashboardChart : View
{
    public const string CardKey = "chart-card";
    static readonly string[] ToggleKeys = ["toggle-90d", "toggle-30d", "toggle-7d"];

    DashboardAreaChart _plot = null!;
    Select _select = null!;
    Indexed<DashboardChart>[]? _refs;
    string _range = "90d";
    int _focus = -1;

    public DashboardAreaChart Plot => _plot;
    public Select Select => _select;

    /// <summary>timeRange của bản gốc: "90d", "30d", "7d" hoặc "" (không nút nào bật).</summary>
    public string Range
    {
        get => _range;
        set
        {
            if (_range == value) return;
            _range = value;
            _plot.Range = value;
            int index = Array.IndexOf(DashboardChartData.RangeValues, value);
            if (_select.SelectedIndex != index) _select.SelectedIndex = index;
            Notify();
        }
    }

    protected override void OnCreated()
    {
        _plot = App.New(new DashboardAreaChart());
        _select = App.New(new Select { Items = DashboardChartData.RangeLabels, Placeholder = "Last 3 months", SelectedIndex = 0, TriggerClass = "h-8 w-full", ContentClass = "rounded-xl" });
        App.Subscribe<Select.ValueChanged>(_select, e => Range = e.Index >= 0 ? DashboardChartData.RangeValues[e.Index] : "");
    }

    void Toggle(int index)
    {
        Range = _range == DashboardChartData.RangeValues[index] ? "" : DashboardChartData.RangeValues[index];
        _focus = index;
        // Bấm chuột focus chính nút (button của Radix), kể cả khi nó chưa là điểm dừng Tab; không hiện vòng focus.
        _window?.FocusKey(ToggleKeys[index], visible: false);
    }

    bool ToggleKey(int index, KeyEvent e)
    {
        int n = ToggleKeys.Length;
        int next = e.Key switch { Key.Right or Key.Down => (index + 1) % n, Key.Left or Key.Up => (index + n - 1) % n, Key.Home => 0, Key.End => n - 1, _ => -1 };
        if (next < 0) return false;
        _focus = next;
        Notify();
        _window?.FocusKey(ToggleKeys[next]); // nút đích thành điểm dừng Tab ở frame sau; FocusKey chờ tới khi nó có mặt
        return true;
    }

    Window? _window;

    Div ToggleGroup(Window w)
    {
        var refs = Indexed<DashboardChart>.Ensure(ref _refs, this, ToggleKeys.Length);
        int on = Array.IndexOf(DashboardChartData.RangeValues, _range);
        // Roving tabindex: chỉ một nút là điểm dừng Tab – nút vừa được focus (chuột / mũi tên), ban đầu là nút đang bật.
        int stop = _focus >= 0 ? _focus : Math.Max(on, 0);
        var group = w.Div().Key("toggle-group").Class(V4.ToggleGroupClass + " hidden min-[1111px]:flex");
        for (int i = 0; i < ToggleKeys.Length; i++)
            group.Child(w.Div().Key(ToggleKeys[i]).Focusable(i == stop).Class(V4.ToggleItem(i, ToggleKeys.Length, i == on)).Class("px-4")
                .OnClick(refs[i], static (r, _) => r.Owner.Toggle(r.Index))
                .OnKey(refs[i], static (r, e) => r.Owner.ToggleKey(r.Index, e))
                .Child(DashboardChartData.RangeLabels[i]));
        return group;
    }

    public override Div Render(Window w)
    {
        _window = w;
        return Ui.Card(w).Key(CardKey)
            .Child(Ui.CardHeader(w, hasAction: true)
                .Child(Ui.CardTitle(w, "Total Visitors"))
                .Child(Ui.CardAction(w)
                    .Child(ToggleGroup(w))
                    .Child(w.View(_select).Class("grid w-40 min-[1111px]:hidden")))
                .Child(w.Div().Class("col-start-1 text-sm text-muted-foreground")
                    .Child(w.Div().Class("hidden min-[868px]:block").Child("Total for the last 3 months"))
                    .Child(w.Div().Class("min-[868px]:hidden").Child("Last 3 months"))))
            .Child(w.Div().Class("px-2 pt-4 sm:px-6 sm:pt-6").Child(w.View(_plot)));
    }
}
