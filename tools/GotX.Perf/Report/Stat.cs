namespace GotX.Perf;

/// <summary>min / median / p95 / p99 / max of a sample set.</summary>
public readonly record struct Stat(int Count, double Min, double Median, double P95, double P99, double Max, double Sum)
{
    public static Stat Of(IReadOnlyList<double> values)
    {
        if (values.Count == 0) return default;
        var v = values.ToArray();
        Array.Sort(v);
        double Pct(double p) => v[Math.Min(v.Length - 1, (int)Math.Ceiling(v.Length * p) - 1)];
        return new Stat(v.Length, v[0], v[v.Length / 2], Pct(0.95), Pct(0.99), v[^1], v.Sum());
    }

    public static double MedianOf(IReadOnlyList<double> values) => values.Count == 0 ? 0 : Of(values).Median;

    public override string ToString() => $"{Fmt(Min),7} {Fmt(Median),7} {Fmt(P95),7} {Fmt(P99),7} {Fmt(Max),7}";

    public static string Fmt(double v) => v >= 100 ? v.ToString("0") : v >= 10 ? v.ToString("0.0") : v.ToString("0.00");

    public static string Bytes(double b) => b >= 1 << 20 ? $"{b / (1 << 20):0.0} MB" : b >= 1024 ? $"{b / 1024:0.0} KB" : $"{b:0} B";
}
