using System.Text.Json;
using GotX.Stress;

const string Usage = """
    GotX.Stress - headless deterministic stress scenarios (software renderer, virtual clock, fixed seeds)
      [--only a,b,..] [--repeat N] [--check] [--update-baseline] [--baseline file] [--out file.json]
      [--soak <minutes>]      soak mode (off by default): reports leak slope of working set / private bytes / GC heap
      --list                  scenario names
    Scenarios: tree-10k tree-100k vlist-100k vlist-1m churn input resize-dpi text multiwindow gpu-loss
    """;

if (args.Contains("--help") || args.Contains("-h")) { Console.WriteLine(Usage); return 0; }
if (args.Contains("--list")) { foreach (var s in Scenarios.All) Console.WriteLine($"{s.Name,-14}{s.What}"); return 0; }

string? Arg(string name) { int i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
string baseline = Arg("--baseline") ?? DefaultBaseline();
Baseline.Path = baseline;
bool update = args.Contains("--update-baseline"), check = args.Contains("--check");
int repeat = int.TryParse(Arg("--repeat"), out int rp) ? Math.Max(1, rp) : (update ? 3 : 1);
var only = Arg("--only")?.Split(',', StringSplitOptions.RemoveEmptyEntries);
double soakMin = double.TryParse(Arg("--soak"), System.Globalization.CultureInfo.InvariantCulture, out double sm) ? sm : 0;

if (only is not null)
    foreach (var o in only.Where(o => Scenarios.All.All(s => s.Name != o))) { Console.Error.WriteLine("unknown scenario " + o); return 2; }

Console.WriteLine($"GotX.Stress  {Environment.OSVersion}  {Environment.ProcessorCount} cpu  {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}  " +
    $"{(System.Diagnostics.Debugger.IsAttached ? "debugger" : "")}{(IsDebugBuild() ? " DEBUG BUILD - numbers meaningless" : "")}");
var text = Scenarios.Fonts();
Console.Write("warm-up (JIT, caches; not measured) ... ");
Scenarios.All.First(s => s.Name == "input").Run(text);
Console.WriteLine("done");
var results = new Dictionary<string, Dictionary<string, double>>();
foreach (var sc in Scenarios.All.Where(s => only is null || only.Contains(s.Name)))
{
    var runs = new List<Dictionary<string, double>>();
    int reps = sc.Slow ? 1 : repeat; // minutes per run: measured once
    for (int r = 0; r < reps; r++)
    {
        Console.Write($"run {sc.Name} ({r + 1}/{reps}) ... ");
        var m = sc.Run(text);
        if (m is null) break;
        Console.WriteLine($"p50 {m["p50"]:0.00} p95 {m["p95"]:0.00} p99 {m["p99"]:0.00} max {m["max"]:0.0} ms");
        runs.Add(m);
    }
    if (runs.Count > 0) results[sc.Name] = Baseline.Median(runs);
}

if (results.Count > 0) { Report.Table(results); Report.HotSpots(results); }

if (soakMin > 0)
{
    var soak = Soak.Run(text, soakMin);
    results["soak"] = soak;
}

string? outPath = Arg("--out");
if (outPath is not null)
{
    File.WriteAllText(outPath, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("wrote " + outPath);
}

var gated = results.Where(r => r.Key != "soak").ToDictionary(r => r.Key, r => r.Value);
if (update) Baseline.Update(gated, repeat);
if (check) return Baseline.Check(gated) == 0 ? 0 : 1;
return 0;

static bool IsDebugBuild() =>
#if DEBUG
    true;
#else
    false;
#endif

// Source-tree baseline: found by walking up from the working directory (then from the exe) to the repo's tools/GotX.Stress folder.
static string DefaultBaseline()
{
    foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        for (var d = new DirectoryInfo(start); d is not null; d = d.Parent)
        {
            string dir = Path.Combine(d.FullName, "tools", "GotX.Stress");
            if (Directory.Exists(dir)) return Path.Combine(dir, "stress-baseline.json");
        }
    return "stress-baseline.json";
}
