using System.Diagnostics;

namespace GotX.Perf;

/// <summary>
/// <c>run-target &lt;target&gt; --log &lt;file&gt; [--cold] [--no-raster] [--scenarios a,b] [--alloc-types] [--trace]</c>: the in-process half of the bench (one fresh process per
/// target and repeat, so JIT, caches and the glyph atlas start cold). Writes only the perf log; the parent reads it back.
/// </summary>
public static class ChildRun
{
    public static int Run(string[] args)
    {
        string name = args[0];
        string logPath = Arg(args, "--log") ?? throw new ArgumentException("--log required");
        bool cold = args.Contains("--cold"), raster = !args.Contains("--no-raster");
        var target = Targets.All().FirstOrDefault(t => t.Name == name) ?? throw new ArgumentException($"unknown target '{name}'");

        using var log = new PerfLog(logPath, args.Contains("--trace") ? PerfLogLevel.Trace : PerfLogLevel.Frames);
        PerfLog.Shared = log;
        double Ms(long since) => (Stopwatch.GetTimestamp() - since) * 1000.0 / Stopwatch.Frequency;
        long t0 = Stopwatch.GetTimestamp();
        log.Startup("process-to-main", (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds);
        var fonts = Targets.Fonts();
        log.Startup("fonts", Ms(t0));
        long t1 = Stopwatch.GetTimestamp();
        var session = new PerfSession(fonts, target.Root, log, target.Size, raster, args.Contains("--gpu"));
        log.Startup("create", Ms(t1));
        if (cold)
        {
            Scenarios.Cold(session);
            log.Startup("cold-total", Ms(t0));
            log.Startup("cold-alloc", GC.GetTotalAllocatedBytes());
            return 0;
        }

        log.Startup("calibrate", Calibrate());

        // Warm-up (not measured): settle, then enough idle frames for the JIT and every cache to reach steady state.
        session.Mark("warmup");
        session.Settle();
        session.Steps(20);
        var wanted = Arg(args, "--scenarios")?.Split(',') ?? Scenarios.Names;
        using var alloc = args.Contains("--alloc-types") ? new AllocListener() : null;
        alloc?.Drain();
        foreach (string scenario in Scenarios.Names)
        {
            if (!wanted.Contains(scenario)) continue;
            Scenarios.Run(session, scenario);
            if (alloc is not null) Console.WriteLine(scenario + ":" + Environment.NewLine + alloc.Drain());
        }
        log.Flush();
        return 0;
    }

    /// <summary>
    /// Fixed CPU workload (integer hashing, float math, dictionary traffic), best of 7: the bench divides baseline times by the ratio of this
    /// number to the one stored in the baseline, so a slower / busier machine does not read as a regression.
    /// </summary>
    static double Calibrate()
    {
        double best = double.MaxValue;
        for (int rep = 0; rep < 7; rep++)
        {
            long t = Stopwatch.GetTimestamp();
            ulong h = 14695981039346656037UL;
            float f = 0;
            var dict = new Dictionary<int, int>();
            for (int i = 0; i < 300_000; i++)
            {
                h = (h ^ (ulong)i) * 1099511628211UL;
                f += MathF.Sqrt(i);
                dict[i & 1023] = i;
            }
            GC.KeepAlive(dict);
            if (h == 0 || f < 0) Console.Write(' '); // keep the loop observable
            best = Math.Min(best, (Stopwatch.GetTimestamp() - t) * 1000.0 / Stopwatch.Frequency);
        }
        return best;
    }

    static string? Arg(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
