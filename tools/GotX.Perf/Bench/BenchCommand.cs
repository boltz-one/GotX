using System.Diagnostics;

namespace GotX.Perf;

/// <summary>
/// <c>bench [--repeat N] [--only a,b] [--update-baseline] [--baseline file] [--out dir] [--raster] [--gpu] [--scenarios a,b] [--verbose]</c>:
/// runs each target in fresh child processes (steady-state run with TieredCompilation off so it measures optimised code like
/// Native AOT does, plus a default-JIT cold-start run), reads the logs back, takes the median over repeats and compares with
/// <c>perf-baseline.json</c>. Exit 1 on regression beyond the per-metric noise tolerance.
/// </summary>
public static class BenchCommand
{
    public static int Run(string[] args)
    {
        int repeat = ReportCommand.ArgInt(args, "--repeat", 3);
        string[] only = (Arg(args, "--only") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        string[] scenarios = (Arg(args, "--scenarios") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        bool improved = args.Contains("--update-improved"), update = args.Contains("--update-baseline") || improved, verbose = args.Contains("--verbose"), raster = args.Contains("--raster"), gpu = args.Contains("--gpu");
        string baselinePath = Arg(args, "--baseline") ?? DefaultBaseline();
        string outDir = Path.GetFullPath(Arg(args, "--out") ?? Path.Combine(Path.GetTempPath(), "gotx-perf-" + DateTime.Now.ToString("yyyyMMdd-HHmmss")));
        Directory.CreateDirectory(outDir);

        var targets = Targets.All().Where(t => only.Length == 0 || only.Any(o => t.Name.Contains(o, StringComparison.OrdinalIgnoreCase))).ToList();
        if (targets.Count == 0) { Console.Error.WriteLine("no target matches --only"); return 2; }
        var baseline = Baseline.Load(baselinePath);
        var current = new Dictionary<string, Dictionary<string, double>>();
        var allRuns = new Dictionary<string, List<Dictionary<string, Dictionary<string, double>>>>();
        var sw = Stopwatch.StartNew();

        // One pass = `repeat` fresh-process runs of a target; results of all passes are merged (time metrics: minimum, since noise from a
        // shared machine only ever adds time; allocations / renders per frame: median).
        bool Pass(Target target)
        {
            var runs = allRuns.TryGetValue(target.Name, out var existing) ? existing : allRuns[target.Name] = new();
            int first = runs.Count;
            for (int r = first; r < first + repeat; r++)
            {
                string steady = Path.Combine(outDir, $"{target.Name}.{r}.jsonl"), cold = Path.Combine(outDir, $"{target.Name}.{r}.cold.jsonl");
                var m = new Dictionary<string, Dictionary<string, double>>();
                if (!Child(target.Name, cold, "--cold", raster, tiered: true, gpu: gpu) || !Child(target.Name, steady, scenarios.Length > 0 ? "--scenarios " + string.Join(',', scenarios) : "", raster, tiered: false, gpu: gpu)) return false;
                Extract(target.Name, LogData.Read(cold), LogData.Read(steady), m, gpu);
                runs.Add(m);
            }
            foreach (var key in runs[0].Keys)
            {
                var merged = new Dictionary<string, double>();
                foreach (var metric in runs[0][key].Keys)
                {
                    var values = runs.Where(x => x.TryGetValue(key, out var d) && d.ContainsKey(metric)).Select(x => x[key][metric]).ToList();
                    merged[metric] = Baseline.TimeMetrics.Contains(metric) || metric == "calib" ? values.Min() : Stat.MedianOf(values);
                }
                if (key == "_machine" && current.TryGetValue(key, out var prev)) merged["calib"] = Math.Min(merged["calib"], prev["calib"]);
                current[key] = merged;
            }
            Console.Error.WriteLine($"  {target.Name} done ({sw.Elapsed.TotalSeconds:0}s)");
            return true;
        }

        foreach (var target in targets) if (!Pass(target)) return 3;

        // A regression has to survive a re-measurement (up to two extra passes over just the suspect targets): the machine is not quiet.
        if (!update)
            for (int retry = 1; retry <= 2; retry++)
            {
                double scale = MachineScale(current, baseline);
                var suspects = targets.Where(t => current.Any(e => e.Key.StartsWith(t.Name + "/", StringComparison.Ordinal)
                    && baseline.Entries.TryGetValue(e.Key, out var b) && Baseline.Regressions(b, e.Value, scale).Count > 0)).ToList();
                if (suspects.Count == 0) break;
                Console.Error.WriteLine($"  re-measuring {suspects.Count} target(s) that look regressed (pass {retry}): {string.Join(", ", suspects.Select(s => s.Name))}");
                foreach (var target in suspects) if (!Pass(target)) return 3;
            }

        if (update)
        {
            foreach (var (k, v) in current)
            {
                // --gpu --update-baseline chỉ ghi các trường gpu* vào entry sẵn có: số đo thời gian của baseline giữ nguyên.
                if (gpu) { if (baseline.Entries.TryGetValue(k, out var old)) foreach (var (m, x) in v) if (m.StartsWith("gpu", StringComparison.Ordinal)) old[m] = x; }
                else if (improved && baseline.Entries.TryGetValue(k, out var cur0))
                {
                    // --update-improved: chỉ siết baseline (số đo tốt hơn thay số cũ); số đo xấu hơn không bao giờ được ghi để che hồi quy.
                    foreach (var (m, x) in v) if (!cur0.TryGetValue(m, out double was) || x < was) cur0[m] = x;
                }
                else baseline.Entries[k] = baseline.Entries.TryGetValue(k, out var prevE) ? Merge(v, prevE) : v;
            }
            baseline.Save(baselinePath);
            Console.WriteLine($"baseline updated: {baselinePath} ({current.Count} entries)");
        }
        int bad = Print(current, baseline, verbose || update);
        Console.WriteLine($"logs: {outDir}");
        return bad > 0 && !update ? 1 : 0;
    }

    static void Extract(string target, LogData cold, LogData steady, Dictionary<string, Dictionary<string, double>> into, bool gpu = false)
    {
        var c = new Dictionary<string, double>
        {
            ["fonts"] = cold.Starts.FirstOrDefault(s => s.What == "fonts")?.Ms ?? 0,
            ["create"] = cold.Starts.FirstOrDefault(s => s.What == "create")?.Ms ?? 0,
            ["firstFrame"] = cold.Starts.FirstOrDefault(s => s.What == "first-frame")?.Ms ?? 0,
            ["coldTotal"] = cold.Starts.FirstOrDefault(s => s.What == "cold-total")?.Ms ?? 0,
        };
        into[target + "/cold"] = c;
        into["_machine"] = new Dictionary<string, double> { ["calib"] = steady.Starts.FirstOrDefault(s => s.What == "calibrate")?.Ms ?? 0 };
        foreach (string scenario in Scenarios.Names)
        {
            var s = Summary.Of(steady.Frames.Where(f => f.Phase == scenario).ToList());
            if (s.Frames == 0) continue;
            var frames = steady.Frames.Where(f => f.Phase == scenario && f.HasPresent).Select(f => f.Present).ToList();
            var entry = new Dictionary<string, double>
            {
                ["frames"] = s.Frames, ["drawMed"] = s.Draw.Median, ["drawP95"] = s.Draw.P95, ["drawTotal"] = s.Draw.Sum,
                ["rasterMed"] = Stat.MedianOf(frames), ["allocMed"] = s.Alloc.Median, ["allocTotal"] = s.TotalAlloc, ["rendPerFrame"] = s.RendPerFrame,
            };
            if (gpu)
            {
                // --gpu: chỉ thêm số liệu GPU (thông tin, không nằm trong Tolerance nên không bao giờ làm hỏng cổng hồi quy); rasterMed không có nghĩa nên bỏ.
                entry.Remove("rasterMed");
                entry["gpuFrames"] = s.GpuFrames; entry["gpuDraws"] = s.Draws; entry["gpuPipe"] = s.Pipe; entry["gpuTexSw"] = s.TexSw;
                entry["gpuInstB"] = s.InstB; entry["gpuAtlasHit"] = s.AtlasHit; entry["gpuEncMs"] = s.Enc;
            }
            into[$"{target}/{scenario}"] = entry;
        }
    }

    /// <summary>Giữ lại các trường thông tin gpu* của baseline cũ khi ghi lại số đo thường.</summary>
    static Dictionary<string, double> Merge(Dictionary<string, double> fresh, Dictionary<string, double> old)
    {
        foreach (var (m, x) in old) if (m.StartsWith("gpu", StringComparison.Ordinal) && !fresh.ContainsKey(m)) fresh[m] = x;
        return fresh;
    }

    static double MachineScale(Dictionary<string, Dictionary<string, double>> cur, Baseline baseline) =>
        cur.TryGetValue("_machine", out var cm) && baseline.Entries.TryGetValue("_machine", out var bm) && cm["calib"] > 0 && bm["calib"] > 0
            ? Math.Clamp(cm["calib"] / bm["calib"], 1, 3) : 1;

    static int Print(Dictionary<string, Dictionary<string, double>> cur, Baseline baseline, bool all)
    {
        int bad = 0, improved = 0, missing = 0;
        double scale = 1;
        if (cur.TryGetValue("_machine", out var cm) && baseline.Entries.TryGetValue("_machine", out var bm) && cm["calib"] > 0 && bm["calib"] > 0)
        {
            scale = Math.Clamp(cm["calib"] / bm["calib"], 1, 3);
            Console.WriteLine($"machine speed vs baseline: x{scale:0.00} (calibration {Stat.Fmt(cm["calib"])} ms vs {Stat.Fmt(bm["calib"])} ms); baseline times are scaled by it before comparing");
        }
        Console.WriteLine($"{"target/scenario",-34} {"frames",6} {"draw med",9} {"p95",7} {"total",8} {"raster",7} {"alloc med",10} {"rend/fr",7}  vs baseline");
        bool hasGpu = cur.Any(e => e.Value.ContainsKey("gpuDraws"));
        foreach (var (key, m) in cur.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            if (key == "_machine") continue;
            string status = "";
            if (!baseline.Entries.TryGetValue(key, out var b)) { status = "new"; missing++; }
            else
            {
                var reg = Baseline.Regressions(b, m, scale);
                var imp = Baseline.Improvements(b, m, scale);
                if (reg.Count > 0) { bad++; status = "REGRESSION " + string.Join(", ", reg.Select(r => $"{r.Metric} {Stat.Fmt(r.Base)}->{Stat.Fmt(r.Cur)}")); }
                else if (imp.Count > 0) { improved++; status = "faster " + string.Join(", ", imp.Select(r => $"{r.Metric} {Stat.Fmt(r.Base)}->{Stat.Fmt(r.Cur)}")); }
                else status = "ok";
            }
            if (!all && status == "ok") continue;
            if (key.EndsWith("/cold", StringComparison.Ordinal))
                Console.WriteLine($"{key,-34} {"",6} fonts {Stat.Fmt(m["fonts"])} ms, create {Stat.Fmt(m["create"])} ms, first frame {Stat.Fmt(m["firstFrame"])} ms, cold total {Stat.Fmt(m["coldTotal"])} ms  {status}");
            else
                Console.WriteLine($"{key,-34} {m["frames"],6:0} {Stat.Fmt(m["drawMed"]),9} {Stat.Fmt(m["drawP95"]),7} {Stat.Fmt(m["drawTotal"]),8} {Stat.Fmt(m.GetValueOrDefault("rasterMed")),7} {Stat.Bytes(m["allocMed"]),10} {m["rendPerFrame"],7:0.0}  {status}"
                    + (hasGpu && m.ContainsKey("gpuDraws") ? $"  | gpu: {m["gpuDraws"]:0} draws, {m["gpuPipe"]:0} pipe, {m["gpuTexSw"]:0} texsw, {Stat.Bytes(m["gpuInstB"])} inst, atlas {100 * m["gpuAtlasHit"]:0}%, enc {Stat.Fmt(m["gpuEncMs"])} ms" : ""));
        }
        Console.WriteLine($"\n{cur.Count} entries: {bad} regressed, {improved} faster, {missing} new (not in baseline)");
        return bad;
    }

    static bool Child(string target, string log, string extra, bool raster, bool tiered, bool gpu = false)
    {
        string exe = Environment.ProcessPath!;
        var args = new List<string>();
        if (Path.GetFileNameWithoutExtension(exe).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            args.Add(typeof(BenchCommand).Assembly.Location);
        args.AddRange(["run-target", target, "--log", log]);
        foreach (var part in extra.Split(' ', StringSplitOptions.RemoveEmptyEntries)) args.Add(part);
        if (!raster) args.Add("--no-raster");
        if (gpu) args.Add("--gpu");
        var psi = new ProcessStartInfo(exe) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var a in args) psi.ArgumentList.Add(a);
        psi.Environment.Remove("GOTX_LOG");
        psi.Environment["DOTNET_TieredPGO"] = "0";
        psi.Environment["DOTNET_TieredCompilation"] = tiered ? "1" : "0";
        using var p = Process.Start(psi)!;
        string err = p.StandardError.ReadToEnd();
        p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode == 0) return true;
        Console.Error.WriteLine($"child run-target {target} failed ({p.ExitCode}):\n{err}");
        return false;
    }

    static string DefaultBaseline([System.Runtime.CompilerServices.CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source) ?? ".", "..", "perf-baseline.json"));

    static string? Arg(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
