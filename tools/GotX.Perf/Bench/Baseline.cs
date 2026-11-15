using System.Text.Json;

namespace GotX.Perf;

/// <summary>metric name → value, per "target/scenario" key. Stored in <c>perf-baseline.json</c> next to the tool.</summary>
public sealed class Baseline
{
    public Dictionary<string, Dictionary<string, double>> Entries = new(StringComparer.Ordinal);

    /// <summary>Allowed drift per metric: relative factor plus an absolute floor (so near-zero numbers don't flap).</summary>
    public static readonly Dictionary<string, (double Rel, double Abs)> Tolerance = new()
    {
        ["drawMed"] = (0.15, 0.04), ["drawP95"] = (0.25, 0.15), ["drawTotal"] = (0.15, 0.5), ["rasterMed"] = (0.20, 0.10),
        ["allocMed"] = (0.10, 512), ["rendPerFrame"] = (0.05, 0.2),
        ["firstFrame"] = (0.25, 5), ["coldTotal"] = (0.25, 8), ["fonts"] = (0.30, 6), ["create"] = (0.30, 6),
    };

    public static Baseline Load(string path)
    {
        var b = new Baseline();
        if (!File.Exists(path)) return b;
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var e in doc.RootElement.GetProperty("entries").EnumerateObject())
        {
            var m = new Dictionary<string, double>();
            foreach (var p in e.Value.EnumerateObject()) m[p.Name] = p.Value.GetDouble();
            b.Entries[e.Name] = m;
        }
        return b;
    }

    public void Save(string path)
    {
        using var fs = File.Create(path);
        using var w = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });
        w.WriteStartObject();
        w.WriteNumber("version", 1);
        w.WriteString("note", "median over repeated fresh-process runs; regenerate with: GotX.Perf bench --update-baseline");
        w.WriteStartObject("entries");
        foreach (var (key, metrics) in Entries.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            w.WriteStartObject(key);
            foreach (var (name, v) in metrics.OrderBy(m => m.Key, StringComparer.Ordinal)) w.WriteNumber(name, Math.Round(v, 4));
            w.WriteEndObject();
        }
        w.WriteEndObject();
        w.WriteEndObject();
    }

    /// <summary>Metrics of <paramref name="cur"/> worse than this baseline entry beyond tolerance (only metrics both have).</summary>
    public static List<(string Metric, double Base, double Cur)> Regressions(Dictionary<string, double> baseline, Dictionary<string, double> cur, double scale = 1) =>
        Drifts(baseline, cur, worse: true, scale);

    public static List<(string Metric, double Base, double Cur)> Improvements(Dictionary<string, double> baseline, Dictionary<string, double> cur, double scale = 1) =>
        Drifts(baseline, cur, worse: false, scale);

    /// <summary>Số đo thời gian: gộp các lần lặp bằng MIN (nhiễu của máy dùng chung chỉ làm tăng thời gian); còn lại (cấp phát, số view render) lấy trung vị.</summary>
    public static readonly HashSet<string> TimeMetrics = ["drawMed", "drawP95", "drawTotal", "rasterMed", "firstFrame", "coldTotal", "fonts", "create"];

    static List<(string, double, double)> Drifts(Dictionary<string, double> baseline, Dictionary<string, double> cur, bool worse, double scale)
    {
        var list = new List<(string, double, double)>();
        foreach (var (metric, (rel, abs)) in Tolerance)
        {
            if (!baseline.TryGetValue(metric, out double b) || !cur.TryGetValue(metric, out double c)) continue;
            if (TimeMetrics.Contains(metric)) b *= scale; // máy chậm / bận hơn lúc ghi baseline thì mốc thời gian được nới theo
            bool drift = worse ? c > b * (1 + rel) + abs : c < b * (1 - rel) - abs;
            if (drift) list.Add((metric, b, c));
        }
        return list;
    }
}
