using System.Diagnostics;
using GotX.Raster;

namespace GotX.Footprint;

/// <summary>One measured run: start a fresh host, warm up 3 s, snapshot, drive 10+ s while sampling from outside, snapshot, collect, close.</summary>
static class Runner
{
    const double MB = 1048576.0, Warmup = 3;

    public static int Run(string[] args)
    {
        Native.DpiAware();
        var o = new Options(args);
        Directory.CreateDirectory(o.OutDir);
        var results = new Dictionary<string, List<Dictionary<string, double>>>();
        foreach (string renderer in o.Renderers)
            foreach (string scenario in o.Scenarios)
            {
                string key = $"{scenario}/{renderer}/{o.Mode}";
                results[key] = [];
                for (int rep = 0; rep < o.Repeat; rep++)
                {
                    var m = RunOne(o, scenario, renderer, o.Shots && rep == 0);
                    Console.WriteLine($"{key} #{rep + 1}: " + (m is null ? "FAILED" : Output.OneLine(m)));
                    if (m is not null) results[key].Add(m);
                }
            }
        results = results.Where(kv => kv.Value.Count > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
        var median = results.ToDictionary(kv => kv.Key, kv => Stats.Median(kv.Value));
        foreach (var (key, m) in median) if (o.ExeMB.TryGetValue(key.Split('/')[0], out double mb)) m["exeMB"] = mb;
        Baseline.Path = o.BaselinePath;
        Output.Write(o, results, median);
        if (o.UpdateBaseline) Baseline.Update(median);
        return o.Check ? Baseline.Check(median) : 0;
    }

    static Dictionary<string, double>? RunOne(Options o, string scenario, string renderer, bool shot)
    {
        using var host = new HostProcess(o.HostExe, o.HostArgs, scenario, renderer);
        if (!host.WaitReady(30000)) { Console.Error.WriteLine("  host never presented a frame"); return null; }
        var proc = host.Proc;
        var sampler = new Sampler(proc);
        var driver = new Driver(host.Hwnd, scenario, sampler.Poll);
        Thread.Sleep(300);
        driver.Setup();
        driver.Run(Warmup, warm: true);
        var heap0 = host.Ask('h');                        // live heap after a full GC, before the window (leak baseline)
        var s0 = host.Ask('s');
        sampler.Begin();
        var cpu0 = proc.TotalProcessorTime;
        var wall = Stopwatch.StartNew();
        driver.Run(o.Seconds, warm: false);
        wall.Stop();
        proc.Refresh();
        var cpu1 = proc.TotalProcessorTime;
        var s1 = host.Ask('s');
        sampler.Poll(force: true);
        var (gdi, user) = Native.GuiObjects(proc.Handle);
        var m = new Dictionary<string, double>
        {
            ["cpuPct"] = (cpu1 - cpu0).TotalSeconds / wall.Elapsed.TotalSeconds / Environment.ProcessorCount * 100,
            ["wsStartMB"] = sampler.WsStart / MB, ["wsPeakMB"] = sampler.WsPeak / MB, ["wsEndMB"] = sampler.WsEnd / MB,
            ["privStartMB"] = sampler.PrivStart / MB, ["privPeakMB"] = sampler.PrivPeak / MB, ["privEndMB"] = sampler.PrivEnd / MB,
            ["gen0"] = s1["gc0"] - s0["gc0"], ["gen1"] = s1["gc1"] - s0["gc1"], ["gen2"] = s1["gc2"] - s0["gc2"],
            ["heapMB"] = s1["heap"] / MB,
            ["threads"] = proc.Threads.Count, ["handles"] = proc.HandleCount, ["gdi"] = gdi, ["user"] = user,
            ["frames"] = s1["frames"] - s0["frames"], ["presented"] = s1["presents"] - s0["presents"],
            ["startupMs"] = host.FirstFrameMs,
            ["allocKBps"] = (s1["alloc"] - s0["alloc"]) / 1024.0 / wall.Elapsed.TotalSeconds,
        };
        double frames = m["frames"];
        m["fps"] = frames / wall.Elapsed.TotalSeconds;
        m["allocPerFrame"] = frames > 0 ? (s1["alloc"] - s0["alloc"]) / frames : 0;
        if (s1["gpuLocal"] >= 0) { m["gpuLocalMB"] = s1["gpuLocal"] / MB; m["gpuSharedMB"] = s1["gpuShared"] / MB; }
        if (scenario == "dialog") m["cycles"] = s1["cycles"] - s0["cycles"];
        if (shot) Shoot(o, host.Hwnd, $"{scenario}-{renderer}");
        var heap1 = host.Ask('h');                        // after the run, again after a full GC: growth = leak
        m["leakHeapKB"] = (heap1["heap"] - heap0["heap"]) / 1024.0;
        proc.Refresh();
        m["leakPrivMB"] = m["privEndMB"] - m["privStartMB"];
        return m;
    }

    static void Shoot(Options o, nint hwnd, string name)
    {
        var px = Native.Capture(hwnd, out int w, out int h);
        if (px is null) return;
        var canvas = new Canvas();
        canvas.Resize(w, h);
        for (int i = 0; i < w * h; i++) canvas.Pixels[i] = px[i] | 0xFF000000u;
        PngWriter.Write(Path.Combine(o.OutDir, name + ".png"), canvas);
    }
}
