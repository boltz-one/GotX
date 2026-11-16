using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace GotX.Footprint;

/// <summary>JSON (all runs + medians) and Markdown tables into the out dir.</summary>
static class Output
{
    static readonly CultureInfo C = CultureInfo.InvariantCulture;

    public static string OneLine(Dictionary<string, double> m) =>
        string.Create(C, $"cpu {m["cpuPct"]:0.00}% ws {m["wsStartMB"]:0.0}/{m["wsPeakMB"]:0.0}/{m["wsEndMB"]:0.0} MB  {m["frames"]:0} frames  {m["allocPerFrame"]:0} B/frame  gc {m["gen0"]}/{m["gen1"]}/{m["gen2"]}  first {m["startupMs"]:0} ms");

    public static void Write(Options o, Dictionary<string, List<Dictionary<string, double>>> runs, Dictionary<string, Dictionary<string, double>> median)
    {
        string stem = Path.Combine(o.OutDir, "footprint-" + o.Mode);
        using (var fs = File.Create(stem + ".json"))
        using (var w = new Utf8JsonWriter(fs, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartObject();
            w.WriteStartObject("meta");
            w.WriteString("date", DateTime.Now.ToString("s", C)); w.WriteString("mode", o.Mode);
            w.WriteNumber("repeat", o.Repeat); w.WriteNumber("seconds", o.Seconds); w.WriteNumber("warmupSeconds", 3);
            w.WriteNumber("cores", Environment.ProcessorCount);
            w.WriteString("os", RuntimeInformation.OSDescription); w.WriteString("runtime", RuntimeInformation.FrameworkDescription);
            w.WriteEndObject();
            w.WriteStartObject("median");
            foreach (var (k, m) in median.OrderBy(x => x.Key, StringComparer.Ordinal)) Metrics(w, k, m);
            w.WriteEndObject();
            w.WriteStartObject("runs");
            foreach (var (k, list) in runs.OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                w.WriteStartArray(k);
                foreach (var m in list) { w.WriteStartObject(); foreach (var (n, v) in m.OrderBy(x => x.Key, StringComparer.Ordinal)) w.WriteNumber(n, Math.Round(v, 3)); w.WriteEndObject(); }
                w.WriteEndArray();
            }
            w.WriteEndObject();
            w.WriteEndObject();
        }
        string md = Markdown(o, median);
        File.WriteAllText(stem + ".md", md);
        Console.WriteLine(md);
        Console.WriteLine("written " + stem + ".json / .md");
    }

    static void Metrics(Utf8JsonWriter w, string key, Dictionary<string, double> m)
    {
        w.WriteStartObject(key);
        foreach (var (n, v) in m.OrderBy(x => x.Key, StringComparer.Ordinal)) w.WriteNumber(n, Math.Round(v, 3));
        w.WriteEndObject();
    }

    static string Markdown(Options o, Dictionary<string, Dictionary<string, double>> median)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Mode {o.Mode}, median of {o.Repeat} runs, {o.Seconds:0}s measured after 3s warm-up, {Environment.ProcessorCount} logical cores.\n");
        foreach (string renderer in o.Renderers)
        {
            var rows = median.Where(kv => kv.Key.Split('/')[1] == renderer).OrderBy(kv => Array.IndexOf(Scenes.Names, kv.Key.Split('/')[0])).ToList();
            if (rows.Count == 0) continue;
            sb.AppendLine($"### {renderer} ({o.Mode})\n");
            sb.AppendLine("| scenario | CPU % | WS start/peak/end MB | private end MB | heap MB | gen0/1/2 | B/frame | KB/s | frames / presented | threads | handles | GDI/USER | first frame ms | GPU MB | leak heap KB / priv MB |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var (k, m) in rows)
                sb.AppendLine(string.Create(C, $"| {k.Split('/')[0]} | {m["cpuPct"]:0.00} | {m["wsStartMB"]:0.0} / {m["wsPeakMB"]:0.0} / {m["wsEndMB"]:0.0} | {m["privEndMB"]:0.0} | {m["heapMB"]:0.0} | {m["gen0"]:0}/{m["gen1"]:0}/{m["gen2"]:0} | {m["allocPerFrame"]:0} | {m["allocKBps"]:0.0} | {m["frames"]:0} / {m["presented"]:0} | {m["threads"]:0} | {m["handles"]:0} | {m["gdi"]:0}/{m["user"]:0} | {m["startupMs"]:0} | {(m.TryGetValue("gpuLocalMB", out double g) ? g.ToString("0.0", C) : "n/a")} | {m["leakHeapKB"]:0} / {m["leakPrivMB"]:0.0} |"));
            sb.AppendLine();
        }
        var exe = median.Where(kv => kv.Value.ContainsKey("exeMB")).Select(kv => (kv.Key.Split('/')[0], kv.Value["exeMB"])).Distinct().ToList();
        if (exe.Count > 0) sb.AppendLine("Exe size MB: " + string.Join(", ", exe.Select(e => $"{e.Item1} {e.Item2:0.0}")));
        return sb.ToString();
    }
}
