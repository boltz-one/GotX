using System.Text.Json;

namespace GotX.Stress;

/// <summary>
/// stress-baseline.json + --check. A metric fails when value &gt; baseline * rel + abs. Only "lower is better" metrics are checked;
/// wall-clock tails are noisy on a shared machine, so their tolerances are generous. Counts and shapes (views, nodes) are tight.
/// </summary>
static class Baseline
{
    public static readonly Dictionary<string, (double Rel, double Abs)> Tolerance = new()
    {
        ["p50"] = (1.8, 1.0), ["p95"] = (2.5, 8.0), ["p99"] = (3.0, 12.0), ["max"] = (3.0, 40.0),
        ["layoutMs"] = (1.8, 0.5), ["paintMs"] = (1.8, 0.5), ["rasterMs"] = (1.8, 1.0),
        ["allocPerFrame"] = (1.3, 1024), ["gen0"] = (1.5, 4), ["gen1"] = (2, 3), ["gen2"] = (2, 2),
        ["wsGrowthMB"] = (1.5, 128), ["privGrowthMB"] = (1.5, 128), ["heapGrowthMB"] = (1.5, 128),
        ["views"] = (1.2, 1), ["nodes"] = (1.1, 50), ["prims"] = (1.2, 50),
    };

    public static string Path { get; set; } = "stress-baseline.json";

    public static Dictionary<string, Dictionary<string, double>> Load()
    {
        var r = new Dictionary<string, Dictionary<string, double>>();
        if (!File.Exists(Path)) return r;
        using var doc = JsonDocument.Parse(File.ReadAllText(Path));
        foreach (var e in doc.RootElement.GetProperty("entries").EnumerateObject())
            r[e.Name] = e.Value.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetDouble());
        return r;
    }

    public static void Update(Dictionary<string, Dictionary<string, double>> measured, int repeat)
    {
        var all = Load();
        foreach (var (k, v) in measured) all[k] = v.Where(x => Tolerance.ContainsKey(x.Key)).ToDictionary(x => x.Key, x => Math.Round(x.Value, 3));
        using var fs = File.Create(Path);
        using var w = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });
        w.WriteStartObject();
        w.WriteString("note", $"Median of {repeat} in-process run(s), Release, headless software renderer, Geist fonts, fixed seeds. Machine-specific; written by GotX.Stress --update-baseline. --check fails when value > baseline * rel + abs (Baseline.Tolerance).");
        w.WriteStartObject("entries");
        foreach (var (k, v) in all.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            w.WriteStartObject(k);
            foreach (var (m, x) in v.OrderBy(x => x.Key, StringComparer.Ordinal)) w.WriteNumber(m, x);
            w.WriteEndObject();
        }
        w.WriteEndObject(); w.WriteEndObject();
        Console.WriteLine("baseline updated: " + System.IO.Path.GetFullPath(Path));
    }

    /// <summary>Prints regressions; returns the failure count.</summary>
    public static int Check(Dictionary<string, Dictionary<string, double>> measured)
    {
        var all = Load();
        int bad = 0;
        foreach (var (scn, m) in measured)
        {
            if (!all.TryGetValue(scn, out var b)) { Console.WriteLine($"  [new ] {scn}: no baseline"); continue; }
            foreach (var (k, (rel, abs)) in Tolerance)
            {
                if (!b.TryGetValue(k, out double bv) || !m.TryGetValue(k, out double v)) continue;
                if (v > bv * rel + abs) { bad++; Console.WriteLine($"  [FAIL] {scn}.{k}: {v:0.###} > {bv:0.###} * {rel} + {abs}"); }
            }
        }
        Console.WriteLine(bad == 0 ? "check: OK" : $"check: {bad} regression(s)");
        return bad;
    }

    public static Dictionary<string, double> Median(List<Dictionary<string, double>> runs) =>
        runs[0].Keys.ToDictionary(k => k, k => { var v = runs.Select(r => r.GetValueOrDefault(k)).OrderBy(x => x).ToArray(); return v[v.Length / 2]; });
}
