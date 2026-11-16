namespace GotX.Footprint;

static class Stats
{
    /// <summary>Per-metric median over the runs (each metric independently, so the result may not match any single run).</summary>
    public static Dictionary<string, double> Median(List<Dictionary<string, double>> runs)
    {
        var r = new Dictionary<string, double>();
        foreach (string k in runs.SelectMany(x => x.Keys).Distinct())
        {
            var v = runs.Where(x => x.ContainsKey(k)).Select(x => x[k]).Order().ToArray();
            r[k] = v.Length % 2 == 1 ? v[v.Length / 2] : (v[v.Length / 2 - 1] + v[v.Length / 2]) / 2;
        }
        return r;
    }
}
