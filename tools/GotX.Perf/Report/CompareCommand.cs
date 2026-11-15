namespace GotX.Perf;

/// <summary><c>compare &lt;a&gt; &lt;b&gt;</c>: what changed between two logs (b relative to a), overall and per scenario phase.</summary>
public static class CompareCommand
{
    public static int Run(string[] args)
    {
        if (args.Length < 2) { Console.Error.WriteLine("usage: compare <a.jsonl> <b.jsonl>"); return 2; }
        var a = LogData.Read(args[0]);
        var b = LogData.Read(args[1]);
        var sa = Summary.Of(a.Frames);
        var sb = Summary.Of(b.Frames);
        Console.WriteLine($"A {a.Path} ({sa.Frames} frames)\nB {b.Path} ({sb.Frames} frames)\n");
        Console.WriteLine($"{"metric",-22} {"A",10} {"B",10} {"change",9}");
        Line("draw median ms", sa.Draw.Median, sb.Draw.Median);
        Line("draw p95 ms", sa.Draw.P95, sb.Draw.P95);
        Line("draw p99 ms", sa.Draw.P99, sb.Draw.P99);
        Line("draw max ms", sa.Draw.Max, sb.Draw.Max);
        Line("layout median ms", sa.Layout.Median, sb.Layout.Median);
        Line("paint median ms", sa.Paint.Median, sb.Paint.Median);
        Line("render-views total ms", sa.RenderViews.Sum, sb.RenderViews.Sum);
        Line("wrap total ms", sa.Wrap.Sum, sb.Wrap.Sum);
        Line("glyph total ms", sa.Glyph.Sum, sb.Glyph.Sum);
        Line("present median ms", sa.Present.Median, sb.Present.Median);
        Line("alloc median B", sa.Alloc.Median, sb.Alloc.Median);
        Line("alloc total KB", sa.TotalAlloc / 1024.0, sb.TotalAlloc / 1024.0);
        Line("slow frames", sa.Slow, sb.Slow);
        Line("rendered views/frame", sa.RendPerFrame, sb.RendPerFrame);
        Line("gen0+1+2 GCs", sa.Gc0 + sa.Gc1 + sa.Gc2, sb.Gc0 + sb.Gc1 + sb.Gc2);
        Line("wrap misses", sa.WrapMiss, sb.WrapMiss);
        Line("glyphs rasterised", sa.GlyphN, sb.GlyphN);

        var pa = a.Frames.GroupBy(f => f.Phase).ToDictionary(g => g.Key, g => Summary.Of(g.ToList()));
        var pb = b.Frames.GroupBy(f => f.Phase).ToDictionary(g => g.Key, g => Summary.Of(g.ToList()));
        var common = pa.Keys.Where(k => pb.ContainsKey(k) && k.Length > 0).ToList();
        if (common.Count > 0)
        {
            Console.WriteLine($"\n{"phase",-28} {"draw med A",10} {"B",8} {"change",8}  {"total A",9} {"B",8} {"change",8}  {"alloc A",9} {"B",9}");
            foreach (var k in common)
                Console.WriteLine($"{k,-28} {Stat.Fmt(pa[k].Draw.Median),10} {Stat.Fmt(pb[k].Draw.Median),8} {Pct(pa[k].Draw.Median, pb[k].Draw.Median),8}  " +
                    $"{Stat.Fmt(pa[k].TotalDraw),9} {Stat.Fmt(pb[k].TotalDraw),8} {Pct(pa[k].TotalDraw, pb[k].TotalDraw),8}  {Stat.Bytes(pa[k].Alloc.Median),9} {Stat.Bytes(pb[k].Alloc.Median),9}");
        }
        return 0;
    }

    static void Line(string name, double a, double b) => Console.WriteLine($"{name,-22} {Stat.Fmt(a),10} {Stat.Fmt(b),10} {Pct(a, b),9}");

    internal static string Pct(double a, double b)
    {
        if (a == 0 && b == 0) return "=";
        if (a == 0) return "new";
        double p = (b - a) / a * 100;
        return Math.Abs(p) < 0.5 ? "=" : $"{p:+0;-0}%";
    }
}
