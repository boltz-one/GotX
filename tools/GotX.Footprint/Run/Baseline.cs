using System.Text;
using System.Text.Json;

namespace GotX.Footprint;

/// <summary>
/// <c>footprint-baseline.json</c> + <c>--check</c>. A metric fails when value &gt; baseline × rel + abs. Only "lower is better" cost metrics are checked;
/// CPU %, startup and allocation rates are noisy on a shared machine, so their tolerances are generous (rel ≥ 1.5, plus an absolute floor).
/// </summary>
static class Baseline
{
    public static readonly Dictionary<string, (double Rel, double Abs)> Tolerance = new()
    {
        ["cpuPct"] = (2.0, 1.0), ["fps"] = (1.5, 3), ["startupMs"] = (1.8, 150), ["allocKBps"] = (1.5, 20), ["allocPerFrame"] = (1.3, 256),
        ["wsPeakMB"] = (1.25, 8), ["wsEndMB"] = (1.25, 8), ["privPeakMB"] = (1.25, 8), ["privEndMB"] = (1.25, 8), ["heapMB"] = (1.5, 4),
        ["gen0"] = (1.5, 3), ["gen1"] = (2, 2), ["gen2"] = (2, 2),
        ["threads"] = (1.0, 3), ["handles"] = (1.2, 20), ["gdi"] = (1.0, 5), ["user"] = (1.0, 5),
        ["gpuLocalMB"] = (1.3, 16), ["exeMB"] = (1.05, 0.3), ["leakHeapKB"] = (1.5, 256), ["leakPrivMB"] = (1.0, 4),
    };

    /// <summary>Set by the runner to <c>Options.BaselinePath</c>.</summary>
    public static string Path { get; set; } = "footprint-baseline.json";

    static Dictionary<string, Dictionary<string, double>> Load()
    {
        var r = new Dictionary<string, Dictionary<string, double>>();
        if (!File.Exists(Path)) return r;
        using var doc = JsonDocument.Parse(File.ReadAllText(Path));
        foreach (var e in doc.RootElement.GetProperty("entries").EnumerateObject())
            r[e.Name] = e.Value.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetDouble());
        return r;
    }

    public static void Update(Dictionary<string, Dictionary<string, double>> median)
    {
        var all = Load();
        foreach (var (k, v) in median) all[k] = v.Where(x => Tolerance.ContainsKey(x.Key)).OrderBy(x => x.Key).ToDictionary(x => x.Key, x => Math.Round(x.Value, 3));
        using var fs = File.Create(Path);
        using var w = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true });
        w.WriteStartObject();
        w.WriteString("note", "Median of 3 runs, Release, real window. Written by `GotX.Footprint run --update-baseline`; machine-specific. --check fails when value > baseline * rel + abs (see Baseline.Tolerance).");
        w.WriteStartObject("entries");
        foreach (var (k, v) in all.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            w.WriteStartObject(k);
            foreach (var (m, x) in v.OrderBy(x => x.Key, StringComparer.Ordinal)) w.WriteNumber(m, x);
            w.WriteEndObject();
        }
        w.WriteEndObject(); w.WriteEndObject();
        Console.WriteLine("baseline updated: " + Path);
    }

    /// <summary>Merge the "median" block of a finished run's JSON into the baseline file.</summary>
    public static int FromJson(string json)
    {
        Path = new Options(["--scenarios", "hello", "--host", Environment.ProcessPath!]).BaselinePath;
        using var doc = JsonDocument.Parse(File.ReadAllText(json));
        Update(doc.RootElement.GetProperty("median").EnumerateObject()
            .ToDictionary(e => e.Name, e => e.Value.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetDouble())));
        return 0;
    }

    /// <summary>Exit code 1 when any metric of any measured entry exceeds its limit.</summary>
    public static int Check(Dictionary<string, Dictionary<string, double>> median)
    {
        var baseline = Load();
        var sb = new StringBuilder();
        int bad = 0, compared = 0;
        foreach (var (key, m) in median)
        {
            if (!baseline.TryGetValue(key, out var b)) { sb.AppendLine($"  {key}: no baseline entry (skipped)"); continue; }
            foreach (var (metric, (rel, abs)) in Tolerance)
            {
                if (!b.TryGetValue(metric, out double bv) || !m.TryGetValue(metric, out double v)) continue;
                compared++;
                double limit = bv * rel + abs;
                if (v > limit) { bad++; sb.AppendLine($"  FAIL {key} {metric}: {v:0.###} > {limit:0.###} (baseline {bv:0.###})"); }
            }
        }
        Console.Write(sb);
        Console.WriteLine(bad == 0 ? $"footprint check OK ({compared} metrics)" : $"footprint check FAILED: {bad} of {compared} metrics over limit");
        return bad == 0 ? 0 : 1;
    }
}
