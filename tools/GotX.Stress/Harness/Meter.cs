using System.Diagnostics;

namespace GotX.Stress;

/// <summary>Collects frame samples of one scenario run plus GC / memory deltas; <see cref="Finish"/> turns them into a flat metric map.</summary>
public sealed class Meter
{
    readonly List<Sample> _s = new(4096);
    readonly Dictionary<string, double> _extra = new();
    int _g0, _g1, _g2;
    long _ws0, _priv0, _heap0, _peakWs;

    public int Count => _s.Count;

    /// <summary>Forces a full GC (not measured) so growth and GC counts start from a clean heap.</summary>
    public void Begin()
    {
        FullGc();
        (_ws0, _priv0) = Mem();
        _heap0 = GC.GetTotalMemory(false);
        _g0 = GC.CollectionCount(0); _g1 = GC.CollectionCount(1); _g2 = GC.CollectionCount(2);
        _peakWs = _ws0;
        _s.Clear(); _extra.Clear();
    }

    public static void FullGc()
    {
        GC.Collect(2, GCCollectionMode.Forced, true); GC.WaitForPendingFinalizers(); GC.Collect(2, GCCollectionMode.Forced, true);
    }

    public void Add(in Sample s) { _s.Add(s); if ((_s.Count & 63) == 0) _peakWs = Math.Max(_peakWs, Mem().ws); }
    public void Set(string key, double value) => _extra[key] = value;

    public static (long ws, long priv) Mem()
    {
        using var p = Process.GetCurrentProcess();
        return (p.WorkingSet64, p.PrivateMemorySize64);
    }

    static double Pct(double[] sorted, double q)
    {
        if (sorted.Length == 0) return 0;
        int i = (int)Math.Ceiling(q * sorted.Length) - 1;
        return sorted[Math.Clamp(i, 0, sorted.Length - 1)];
    }

    public Dictionary<string, double> Finish()
    {
        var (ws, priv) = Mem();
        long heap = GC.GetTotalMemory(false);
        double n = Math.Max(1, _s.Count), mb = 1024 * 1024.0;
        var t = _s.Select(x => x.Total).OrderBy(x => x).ToArray();
        var m = new Dictionary<string, double>
        {
            ["frames"] = _s.Count,
            ["p50"] = Pct(t, 0.50), ["p95"] = Pct(t, 0.95), ["p99"] = Pct(t, 0.99), ["max"] = t.Length == 0 ? 0 : t[^1],
            ["layoutMs"] = _s.Sum(x => x.Layout) / n, ["paintMs"] = _s.Sum(x => x.Paint) / n, ["rasterMs"] = _s.Sum(x => x.Raster) / n,
            ["otherMs"] = _s.Sum(x => Math.Max(0, x.Total - x.Layout - x.Paint - x.Raster)) / n,
            ["allocPerFrame"] = _s.Sum(x => (double)x.Alloc) / n,
            ["gen0"] = GC.CollectionCount(0) - _g0, ["gen1"] = GC.CollectionCount(1) - _g1, ["gen2"] = GC.CollectionCount(2) - _g2,
            ["wsGrowthMB"] = (ws - _ws0) / mb, ["wsPeakMB"] = (Math.Max(_peakWs, ws) - _ws0) / mb, ["privGrowthMB"] = (priv - _priv0) / mb,
            ["heapGrowthMB"] = (heap - _heap0) / mb,
            ["views"] = _s.Sum(x => (double)x.Rendered) / n, ["replayed"] = _s.Sum(x => (double)x.Replayed) / n,
            ["nodes"] = _s.Count == 0 ? 0 : _s.Max(x => x.Nodes), ["prims"] = _s.Sum(x => (double)x.Prims) / n,
        };
        foreach (var (k, v) in _extra) m[k] = v;
        return m;
    }

    /// <summary>Least-squares slope of y over x (units of y per unit of x).</summary>
    public static double Slope(IReadOnlyList<(double x, double y)> pts)
    {
        if (pts.Count < 2) return 0;
        double mx = pts.Average(p => p.x), my = pts.Average(p => p.y), num = 0, den = 0;
        foreach (var (x, y) in pts) { num += (x - mx) * (y - my); den += (x - mx) * (x - mx); }
        return den == 0 ? 0 : num / den;
    }
}
