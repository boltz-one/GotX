using System.Globalization;

namespace GotX.Components;

/// <summary>
/// Một dãy số của biểu đồ: nhãn chú giải, class màu cột (vd. <c>"bg-chart-1"</c>), giá trị theo từng nhóm.
/// <paramref name="LineClass"/> là class màu chữ cho đường/vùng (hình vector lấy màu chữ); bỏ trống thì suy từ
/// <paramref name="BarClass"/> (<c>bg-chart-1</c> → <c>text-chart-1</c>).
/// </summary>
public sealed record ChartSeries(string Label, string BarClass, IReadOnlyList<float> Values, string? LineClass = null)
{
    /// <summary>Class màu chữ dùng cho đường và vùng tô của dãy này.</summary>
    public string StrokeClass { get; } = LineClass ?? (BarClass.StartsWith("bg-", StringComparison.Ordinal) ? "text-" + BarClass[3..] : BarClass);
}

/// <summary>
/// Biểu đồ cột dọc không có state (hộp có chiều cao theo phần trăm). Biểu đồ đường và vùng cần giữ hình vector đã dựng
/// nên nằm ở view <see cref="Chart"/>. Không có tooltip khi rê chuột (Recharts). Trục Y làm tròn lên số "đẹp"
/// (1, 2, 2.5, 5 × 10^n) để vạch chia dễ đọc.
/// </summary>
public static partial class Ui
{
    /// <summary>
    /// Biểu đồ cột: mỗi nhóm (<paramref name="categories"/>) có một cột cho mỗi dãy, đường lưới ngang, nhãn trục, chú giải.
    /// <paramref name="keyPrefix"/> ≠ null thì đặt key <c>"{prefix}-{dãy}-{nhóm}"</c> cho từng cột (để đo trong kiểm tra).
    /// </summary>
    public static Div BarChart(Window w, IReadOnlyList<string> categories, IReadOnlyList<ChartSeries> series,
        int ticks = 4, string plotClass = "h-48", string? keyPrefix = null)
    {
        float top = ChartTop(series, ticks);
        var bars = w.Div().Class("absolute inset-0 flex px-2");
        for (int c = 0; c < categories.Count; c++)
        {
            var group = w.Div().Class("flex flex-1 h-full items-end justify-center gap-1");
            for (int s = 0; s < series.Count; s++)
            {
                float v = c < series[s].Values.Count ? series[s].Values[c] : 0;
                var bar = w.Div().Class("w-3 rounded-t-[4px]").Class(series[s].BarClass);
                bar.Style.Height = Length.Fraction(top > 0 ? Math.Clamp(v / top, 0, 1) : 0);
                if (keyPrefix is not null) bar.Key($"{keyPrefix}-{s}-{c}");
                group.Child(bar);
            }
            bars.Child(group);
        }
        return ChartFrame(w, categories, series, top, ticks, plotClass, bars);
    }

    /// <summary>Đỉnh trục Y chung cho mọi dãy.</summary>
    internal static float ChartTop(IReadOnlyList<ChartSeries> series, int ticks)
    {
        float max = 0;
        foreach (var s in series)
            foreach (float v in s.Values) max = Math.Max(max, v);
        return NiceMax(max, ticks);
    }

    /// <summary>Khung chung của mọi loại biểu đồ: trục Y, lưới ngang, lớp <paramref name="marks"/> (cột / đường), nhãn trục X, chú giải.</summary>
    internal static Div ChartFrame(Window w, IReadOnlyList<string> categories, IReadOnlyList<ChartSeries> series,
        float top, int ticks, string plotClass, Div marks)
    {
        var axis = w.Div().Class("flex w-10 shrink-0 flex-col justify-between items-end text-xs text-muted-foreground -my-2");
        var grid = w.Div().Class("absolute inset-0 flex flex-col justify-between");
        for (int t = ticks; t >= 0; t--)
        {
            axis.Child(w.Div().Class("h-4 flex items-center").Child((top * t / ticks).ToString("0.##", CultureInfo.InvariantCulture)));
            grid.Child(w.Div().Class(t == 0 ? "h-px bg-border" : "h-px border-t border-dashed border-border"));
        }

        var labels = w.Div().Class("flex flex-1 px-2");
        foreach (string category in categories)
            labels.Child(w.Div().Class("flex-1 text-center text-xs text-muted-foreground truncate").Child(category));

        var legend = w.Div().Class("flex items-center justify-center gap-4 pt-2 text-xs");
        foreach (var s in series)
            legend.Child(w.Div().Class("flex items-center gap-1.5")
                .Child(w.Div().Class("size-2.5 shrink-0 rounded-[2px]").Class(s.BarClass))
                .Child(s.Label));

        // Trục Y và vùng vẽ cùng chiều cao; hàng nhãn X có ô đệm bằng bề rộng trục để nhãn thẳng dưới cột.
        var plot = w.Div().Class("flex gap-2").Class(plotClass)
            .Child(axis)
            .Child(w.Div().Class("relative flex-1").Child(grid).Child(marks));
        return w.Div().Class("flex w-full flex-col gap-1.5")
            .Child(plot)
            .Child(w.Div().Class("flex gap-2").Child(w.Div().Class("w-10 shrink-0")).Child(labels))
            .Child(legend);
    }

    /// <summary>Đỉnh trục Y: số nhỏ nhất ≥ <paramref name="max"/> chia đều <paramref name="ticks"/> bước "đẹp".</summary>
    public static float NiceMax(float max, int ticks)
    {
        if (max <= 0 || ticks <= 0) return 1;
        double raw = max / ticks, mag = Math.Pow(10, Math.Floor(Math.Log10(raw)));
        foreach (double m in (ReadOnlySpan<double>)[1, 2, 2.5, 5, 10])
            if (m * mag >= raw - 1e-9) return (float)(m * mag * ticks);
        return (float)(10 * mag * ticks);
    }
}
