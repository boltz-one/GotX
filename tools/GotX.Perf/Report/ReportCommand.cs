using System.Text;

namespace GotX.Perf;

/// <summary><c>report &lt;log&gt;</c>: compact text summary of one perf log.</summary>
public static class ReportCommand
{
    public static int Run(string[] args)
    {
        if (args.Length < 1) { Console.Error.WriteLine("usage: report <log.jsonl> [--top N] [--phases]"); return 2; }
        int top = ArgInt(args, "--top", 8);
        var data = LogData.Read(args[0]);
        Console.Write(Render(data, top, args.Contains("--phases")));
        return 0;
    }

    public static string Render(LogData data, int top = 8, bool phases = false)
    {
        var sb = new StringBuilder();
        var all = data.Frames;
        var s = Summary.Of(all);
        double wall = all.Count > 0 ? all[^1].Wall - all[0].Wall : 0;
        sb.AppendLine($"log {data.Path}: {all.Count} frames, {all.Select(f => f.Win).Distinct().Count()} window(s), {wall:0.0} s wall" +
            (data.BadLines > 0 ? $", {data.BadLines} unreadable lines" : ""));
        if (data.Starts.Count > 0) sb.AppendLine("startup: " + string.Join(", ", data.Starts.Select(x => $"{x.What} {Stat.Fmt(x.Ms)} ms")));
        if (all.Count == 0) return sb.ToString();

        sb.AppendLine();
        sb.AppendLine($"{"ms",-14} {"min",7} {"median",7} {"p95",7} {"p99",7} {"max",7}");
        Row(sb, "draw", s.Draw); Row(sb, " layout", s.Layout); Row(sb, " paint", s.Paint); Row(sb, " render views", s.RenderViews);
        Row(sb, " text wrap", s.Wrap); Row(sb, " glyph raster", s.Glyph);
        if (s.Present.Count > 0) Row(sb, "present total", s.Present);
        if (s.Build.Count > 0) Row(sb, " build", s.Build);
        if (s.Inst.Count > 0) { Row(sb, "  instances", s.Inst); Row(sb, "  map+copy", s.Map); }
        if (s.Submit.Count > 0) Row(sb, " submit", s.Submit);
        if (s.Flip.Count > 0) Row(sb, " vsync flip", s.Flip);
        if (s.Gpu.Count > 0) Row(sb, " gpu", s.Gpu);
        if (s.GpuFrames > 0)
            sb.AppendLine($"gpu counters/frame (median over {s.GpuFrames}): draw calls {s.Draws:0}, pipeline switches {s.Pipe:0}, texture switches {s.TexSw:0}, " +
                $"instance bytes {Stat.Bytes(s.InstB)}, atlas hit {100 * s.AtlasHit:0.#}%, cpu encode {Stat.Fmt(s.Enc)} ms");
        if (s.Interval.Count > 0) Row(sb, "frame interval", s.Interval);
        if (s.Latency.Count > 0) Row(sb, "input->frame", s.Latency);

        sb.AppendLine();
        sb.AppendLine($"slow frames (>16.6 ms): {s.Slow} ({100.0 * s.Slow / all.Count:0.#}%), slow input latency (>50 ms): {s.SlowLatency}");
        sb.AppendLine($"alloc/frame: median {Stat.Bytes(s.Alloc.Median)}, p95 {Stat.Bytes(s.Alloc.P95)}, max {Stat.Bytes(s.Alloc.Max)}; total {Stat.Bytes(s.TotalAlloc)}; " +
            $"zero-alloc frames {100.0 * s.ZeroAllocFrames / all.Count:0}%");
        sb.AppendLine($"GC: gen0 {s.Gc0}, gen1 {s.Gc1}, gen2 {s.Gc2}; working set {Stat.Fmt(all.Where(f => f.WsMb > 0).Select(f => f.WsMb).DefaultIfEmpty(0).Max())} MB");
        sb.AppendLine($"views per frame: rendered {s.RendPerFrame:0.0}, replayed {s.ReplPerFrame:0.0}; wrap cache hit {100 * s.WrapHitRate:0.#}% ({s.WrapMiss} misses), " +
            $"layout memo hit {100 * s.MeasMemoRate:0.#}%");
        sb.AppendLine($"glyphs rasterised {s.GlyphN} (total {Stat.Fmt(all.Sum(f => f.Glyph))} ms); GPU upload {Stat.Bytes(s.UpBytes)}; atlas {Stat.Fmt(all.Max(f => f.AtlasKb))} KB max");

        AppendTopViews(sb, all, top);
        AppendCauses(sb, all);
        AppendSlow(sb, all, top);
        if (phases) AppendPhases(sb, all);
        if (data.Warns.Count > 0)
        {
            sb.AppendLine().AppendLine($"warnings ({data.Warns.Count}):");
            foreach (var g in data.Warns.GroupBy(w => (w.Kind, w.Msg)).Take(top * 2))
                sb.AppendLine($"  {g.Key.Kind}{(g.Count() > 1 ? $" x{g.Count()}" : "")}: {g.Key.Msg}");
        }
        return sb.ToString();
    }

    static void Row(StringBuilder sb, string name, Stat s) => sb.AppendLine($"{name,-14} {s}");

    static void AppendTopViews(StringBuilder sb, List<FrameRec> all, int top)
    {
        var views = new Dictionary<string, (int Count, long Us)>();
        foreach (var f in all) foreach (var (name, us) in f.Views) { views.TryGetValue(name, out var v); views[name] = (v.Count + 1, v.Us + us); }
        if (views.Count == 0) return;
        sb.AppendLine().AppendLine("top rendered views (renders, total Render() ms; frames log top 5 per frame, use GOTX_LOG_LEVEL=trace for all):");
        foreach (var (name, v) in views.OrderByDescending(x => x.Value.Us).Take(top))
            sb.AppendLine($"  {name,-34} {v.Count,6}x {v.Us / 1000.0,9:0.0} ms");
    }

    static void AppendCauses(StringBuilder sb, List<FrameRec> all)
    {
        var causes = all.SelectMany(f => f.Cause).GroupBy(c => c).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {g.Count()}");
        var clears = all.SelectMany(f => f.Clear).GroupBy(c => c).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {g.Count()}");
        string c1 = string.Join(", ", causes), c2 = string.Join(", ", clears);
        if (c1.Length > 0) sb.AppendLine().AppendLine("dirty causes (frames): " + c1);
        if (c2.Length > 0) sb.AppendLine("full-rebuild reasons (frames): " + c2);
        var dirty = all.SelectMany(f => f.Dirty).GroupBy(c => c).OrderByDescending(g => g.Count()).Take(6).Select(g => $"{g.Key} {g.Count()}");
        if (dirty.Any()) sb.AppendLine("most-dirtied views: " + string.Join(", ", dirty));
    }

    static void AppendSlow(StringBuilder sb, List<FrameRec> all, int top)
    {
        var slow = all.Where(f => f.Slow).OrderByDescending(f => f.Draw + f.Present).Take(top).ToList();
        if (slow.Count == 0) return;
        sb.AppendLine().AppendLine($"slowest frames (of {all.Count(f => f.Slow)} slow):");
        foreach (var f in slow)
            sb.AppendLine($"  #{f.N,-5} t={f.Wall,7:0.00}s draw {Stat.Fmt(f.Draw)} (layout {Stat.Fmt(f.Layout)} paint {Stat.Fmt(f.Paint)} views {Stat.Fmt(f.Rv)} wrap {Stat.Fmt(f.Wrap)} glyph {Stat.Fmt(f.Glyph)}x{f.GlyphN})" +
                (f.HasPresent ? $" present {Stat.Fmt(f.Present)}" : "") + (f.Lat > 0 ? $" lat {Stat.Fmt(f.Lat)}" : "") +
                $" rend {f.Rend}/{f.Rend + f.Repl} cause [{string.Join(',', f.Cause)}{(f.Clear.Count > 0 ? " clr:" + string.Join(',', f.Clear) : "")}]" +
                (f.Views.Count > 0 ? $" top view {f.Views[0].Name}" : "") + (f.Input.Count > 0 ? $" in {string.Join(' ', f.Input.Select(i => i.Kind + "x" + i.Count))}" : ""));
    }

    static void AppendPhases(StringBuilder sb, List<FrameRec> all)
    {
        sb.AppendLine().AppendLine($"{"phase",-28} {"frames",6} {"draw med",9} {"draw p95",9} {"alloc med",10} {"rend/fr",8}");
        foreach (var g in all.GroupBy(f => f.Phase))
        {
            var p = Summary.Of(g.ToList());
            sb.AppendLine($"{(g.Key.Length == 0 ? "(none)" : g.Key),-28} {p.Frames,6} {Stat.Fmt(p.Draw.Median),9} {Stat.Fmt(p.Draw.P95),9} {Stat.Bytes(p.Alloc.Median),10} {p.RendPerFrame,8:0.0}");
        }
    }

    internal static int ArgInt(string[] args, string name, int fallback)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length && int.TryParse(args[i + 1], out int v) ? v : fallback;
    }
}
