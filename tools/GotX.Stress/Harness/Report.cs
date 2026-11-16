namespace GotX.Stress;

/// <summary>Console tables: one row per scenario, then the ranked hot spots (scenario x phase, by mean ms per frame).</summary>
static class Report
{
    public static void Table(Dictionary<string, Dictionary<string, double>> r)
    {
        Console.WriteLine();
        Console.WriteLine($"{"scenario",-16}{"frames",7}{"p50",8}{"p95",8}{"p99",8}{"max",8}{"B/frm",9}{"g0",5}{"g1",4}{"g2",4}{"wsMB",7}{"privMB",8}{"views",7}{"nodes",8}{"prims",8}");
        foreach (var (k, m) in r)
            Console.WriteLine($"{k,-16}{m["frames"],7:0}{m["p50"],8:0.00}{m["p95"],8:0.00}{m["p99"],8:0.00}{m["max"],8:0.0}{m["allocPerFrame"],9:0}{m["gen0"],5:0}{m["gen1"],4:0}{m["gen2"],4:0}{m["wsGrowthMB"],7:0.0}{m["privGrowthMB"],8:0.0}{m["views"],7:0.0}{m["nodes"],8:0}{m["prims"],8:0}");
        Console.WriteLine("(ms/frame incl. input dispatch, layout, paint, software raster; growth = end minus start, start after a forced GC)");
    }

    public static void HotSpots(Dictionary<string, Dictionary<string, double>> r, int top = 12)
    {
        var rows = new List<(string name, string phase, double ms, double share)>();
        foreach (var (k, m) in r)
        {
            double sum = m["layoutMs"] + m["paintMs"] + m["rasterMs"] + m["otherMs"];
            foreach (var p in new[] { "layout", "paint", "raster", "other" })
                rows.Add((k, p, m[p + "Ms"], sum > 0 ? m[p + "Ms"] / sum : 0));
        }
        Console.WriteLine();
        Console.WriteLine("Hot spots (mean ms/frame by phase; other = input dispatch, flush, timers, view bookkeeping)");
        Console.WriteLine($"{"#",3} {"scenario",-16}{"phase",-8}{"ms/frame",9}{"share",7}");
        int i = 1;
        foreach (var x in rows.OrderByDescending(x => x.ms).Take(top))
            Console.WriteLine($"{i++,3} {x.name,-16}{x.phase,-8}{x.ms,9:0.000}{x.share,7:P0}");
        Console.WriteLine();
        Console.WriteLine("Tail signals (p99/p50 >= 3, max >= 50 ms, gen2 > 0, alloc >= 100 KB/frame)");
        foreach (var (k, m) in r)
        {
            var s = new List<string>();
            if (m["p50"] > 0 && m["p99"] / m["p50"] >= 3) s.Add($"tail p99/p50={m["p99"] / m["p50"]:0.0}x");
            if (m["max"] >= 50) s.Add($"max={m["max"]:0}ms");
            if (m["gen2"] > 0) s.Add($"gen2={m["gen2"]:0}");
            if (m["allocPerFrame"] >= 100_000) s.Add($"alloc={m["allocPerFrame"] / 1024:0}KB/frame");
            if (s.Count > 0) Console.WriteLine($"  {k}: {string.Join(", ", s)}");
        }
    }
}
