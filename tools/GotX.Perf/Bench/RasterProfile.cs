using System.Diagnostics;
using GotX.Raster;

namespace GotX.Perf;

/// <summary>
/// <c>profile-raster &lt;target&gt; [--scroll N]</c>: draws the target (optionally after N wheel notches on the largest scroll area),
/// then rasterises the final scene one primitive at a time with the CPU renderer and prints where the time goes (per kind, and the
/// most expensive primitives with bounds / clip / flags). Finds pathological primitives that a per-frame total would hide.
/// </summary>
public static class RasterProfile
{
    public static int Run(string[] args)
    {
        var target = Targets.All().FirstOrDefault(t => t.Name == args[0]) ?? throw new ArgumentException("unknown target " + args[0]);
        int notches = ReportCommand.ArgInt(args, "--scroll", 0);
        using var log = new PerfLog(Path.Combine(Path.GetTempPath(), "gotx-raster-profile.jsonl"), PerfLogLevel.Off);
        var s = new PerfSession(Targets.Fonts(), target.Root, log, target.Size, raster: false);
        s.Settle();
        if (notches > 0)
        {
            int best = -1;
            float area = 0;
            for (int i = 0; i < s.Window.HitboxCount; i++)
                if (s.Window.HitboxIsScroll(i) && s.Window.HitboxBounds(i) is var b && b.Width * b.Height > area) { area = b.Width * b.Height; best = i; }
            if (best >= 0)
            {
                var p = s.Center(best);
                s.Send(new InputEvent(InputKind.MouseMove, p));
                for (int i = 0; i < notches; i++) s.Send(new InputEvent(InputKind.Scroll, p, Delta: new Point(0, 64)));
                s.Settle();
            }
        }
        var scene = s.Window.Draw(s.Size, 1);
        var canvas = new Canvas();
        canvas.Resize((int)s.Size.Width, (int)s.Size.Height);
        long t0 = Stopwatch.GetTimestamp();
        SoftwareRenderer.Render(scene, canvas);
        double whole = Ms(t0);
        Console.WriteLine($"{target.Name}: {scene.Count} primitives, {scene.ShapeCount} clip shapes, whole scene {whole:0.0} ms");

        var times = new (double Ms, int Index)[scene.Count];
        var one = new Scene(scene.Atlas) { ClearColor = scene.ClearColor };
        foreach (var sh in scene.ClipShapes) one.AddClipShape(sh);
        foreach (var g in scene.Gradients) one.AddGradient(g);
        for (int i = 0; i < scene.Count; i++)
        {
            one.ClearPrimitives();
            one.Add(scene.Primitives[i]);
            double best = double.MaxValue;
            for (int rep = 0; rep < 3; rep++) // min of 3: the first pass pays page faults / cache misses, the machine is shared
            {
                long t = Stopwatch.GetTimestamp();
                SoftwareRenderer.RenderPrimitives(one, canvas);
                best = Math.Min(best, Ms(t));
            }
            times[i] = (best, i);
        }
        Console.WriteLine($"sum of primitives {times.Sum(x => x.Ms):0.0} ms");
        foreach (var g in times.GroupBy(x => scene.Primitives[x.Index].Kind))
            Console.WriteLine($"  {g.Key,-7} {g.Count(),6} prims  {g.Sum(x => x.Ms),8:0.0} ms  (max {g.Max(x => x.Ms):0.00})");
        Console.WriteLine("most expensive:");
        foreach (var (ms, i) in times.OrderByDescending(x => x.Ms).Take(12))
        {
            ref readonly var p = ref scene.Primitives[i];
            Console.WriteLine($"  #{i,-6} {ms,7:0.00} ms {p.Kind,-6} bounds {p.Bounds.X:0},{p.Bounds.Y:0} {p.Bounds.Width:0}x{p.Bounds.Height:0} clip {p.Clip.X:0},{p.Clip.Y:0} {p.Clip.Width:0}x{p.Clip.Height:0}" +
                $" shape {p.ClipShape} grad {p.Gradient} xf {(p.Transform.IsTransform ? "yes" : "no")} border {(p.HasBorder ? "yes" : "no")} alpha {p.Color.A:0.##} radius {p.Corners.TopLeft:0.#} blur {p.Blur:0.#}");
        }
        return 0;
    }

    static double Ms(long since) => (Stopwatch.GetTimestamp() - since) * 1000.0 / Stopwatch.Frequency;
}
