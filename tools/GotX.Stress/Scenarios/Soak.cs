using System.Diagnostics;

namespace GotX.Stress;

/// <summary>
/// Soak: three windows (mixed input, churn, text/resize) driven unthrottled for N minutes; every interval the heap is fully collected
/// and working set / private bytes / GC heap / text caches are sampled. Root views are replaced every 1500 frames so view create/release leaks show up.
/// The leak slope is a least-squares fit over the samples after the first quarter (warm-up).
/// </summary>
static class Soak
{
    public static Dictionary<string, double> Run(GotX.Text.TextSystem text, double minutes)
    {
        var rig = new Rig(text);
        var a = rig.Add(app => app.New(new MixedPage()), Scenarios.Desktop);
        var b = rig.Add(app => app.New(new ChurnPage(200)), new Size(900, 600), 1.25f);
        var c = rig.Add(app => app.New(new TextPage()), new Size(800, 600));
        foreach (var w in new[] { a, b, c }) rig.Settle(w);
        var rng = new Random(2024);
        var meter = new Meter();
        meter.Begin();
        double interval = Math.Clamp(minutes * 60 / 24, 2, 30); // seconds between samples (~24 samples)
        var sw = Stopwatch.StartNew();
        double next = interval, deadline = minutes * 60;
        var rows = new List<(double t, double ws, double priv, double heap, int gen2, int fonts, int pages, int prims, int nodes)>();
        long frames = 0;
        Console.WriteLine($"soak: {minutes} min, sample every {interval:0.#}s");
        while (sw.Elapsed.TotalSeconds < deadline)
        {
            frames++;
            var s = rig.Send(a, InputScenario.Next(rng, a.Size));
            var churn = (ChurnPage)b.Window.Root!;
            rig.Frame(b, () => { for (int i = 0; i < 4; i++) churn.Leaves[rng.Next(churn.Leaves.Length)].Bump(); });
            rig.Frame(c, () => c.Size = new Size(300 + (int)(frames * 29 % 900), 600));
            if (frames % 1500 == 0) Replace(rig, a, b);
            if (frames % 7 == 0) rig.Frame(a, () => { a.Scale = frames % 14 == 0 ? 1f : 1.5f; });
            meter.Add(s);
            if (sw.Elapsed.TotalSeconds >= next)
            {
                next += interval;
                Meter.FullGc();
                var (ws, priv) = Meter.Mem();
                var f = a.Window.LastFrame;
                rows.Add((sw.Elapsed.TotalMinutes, ws / 1048576.0, priv / 1048576.0, GC.GetTotalMemory(false) / 1048576.0, GC.CollectionCount(2),
                    text.ResidentFontCount, text.FallbackPageCount, f.Primitives, f.Nodes));
                var r = rows[^1];
                Console.WriteLine($"  t={r.t,5:0.0}m ws={r.ws,7:0.0}MB priv={r.priv,7:0.0}MB heap={r.heap,7:0.0}MB gen2={r.gen2} fallbackPages={r.pages} prims={r.prims} nodes={r.nodes}");
            }
        }
        var m = meter.Finish();
        var fit = rows.Skip(rows.Count / 4).ToList();
        m["soakFrames"] = frames;
        m["leakWsMBperMin"] = Meter.Slope(fit.Select(r => (r.t, r.ws)).ToList());
        m["leakPrivMBperMin"] = Meter.Slope(fit.Select(r => (r.t, r.priv)).ToList());
        m["leakHeapKBperMin"] = Meter.Slope(fit.Select(r => (r.t, r.heap * 1024)).ToList());
        m["leakFallbackPagesPerMin"] = Meter.Slope(fit.Select(r => (r.t, (double)r.pages)).ToList());
        Console.WriteLine($"leak slope (after warm-up quarter): ws {m["leakWsMBperMin"]:+0.00;-0.00} MB/min, private {m["leakPrivMBperMin"]:+0.00;-0.00} MB/min, " +
            $"GC heap {m["leakHeapKBperMin"]:+0;-0} KB/min, fallback pages {m["leakFallbackPagesPerMin"]:+0.00;-0.00}/min over {fit.Count} samples");
        Console.WriteLine("arena sizes: not exposed by Core (no public counters for scene/tree/layout arenas); GC heap after full GC is the proxy");
        return m;
    }

    static void Replace(Rig rig, Win a, Win b)
    {
        var old = a.Window.Root;
        a.Window.Root = rig.App.New(new MixedPage());
        if (old is not null) rig.App.Release(old);
        old = b.Window.Root;
        b.Window.Root = rig.App.New(new ChurnPage(200));
        if (old is not null) rig.App.Release(old);
    }
}
