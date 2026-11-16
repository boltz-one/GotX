using System.Diagnostics;
using GotX.Raster;
using GotX.Text;

namespace GotX.Stress;

/// <summary>One headless window: own canvas, size and DPI scale.</summary>
public sealed class Win(Window window)
{
    public Window Window { get; } = window;
    public readonly Canvas Canvas = new();
    /// <summary>Retained-canvas damage, as Win32Window does (the real software path rasterises only dirty tiles).</summary>
    public readonly SceneDamage Damage = new() { AcceptShift = Window.ScrollBlitDefault };
    public Size Size;
    public float Scale = 1;
}

/// <summary>Cost of one frame (input/notify work + Draw + Flush + software raster), wall time measured by the rig.</summary>
public readonly record struct Sample(double Total, double Layout, double Paint, double Raster, long Alloc, int Rendered, int Replayed, int Nodes, int Prims, double Cover = 1, int Kind = 0);

/// <summary>
/// Shared App + TextSystem + any number of headless windows on a virtual clock that only moves when a frame is stepped (deterministic).
/// The CPU renderer rasterises like Win32Window: damage-limited into a retained canvas (STRESS_FULL_RASTER=1 forces whole-frame raster for A/B).
/// </summary>
public sealed class Rig
{
    public const double FrameSeconds = 0.016;
    /// <summary>STRESS_TRACE=1 prints every frame slower than 500 ms (for diagnosing a stuck scenario).</summary>
    static readonly bool FullRaster = Environment.GetEnvironmentVariable("STRESS_FULL_RASTER") == "1";
    /// <summary>When set, replaces the software raster (e.g. the GPU renderer in gpu-loss); its time is counted as raster.</summary>
    public Action<Scene, Win>? Present { get; set; }
    static readonly long AllocTrace = long.TryParse(Environment.GetEnvironmentVariable("STRESS_ALLOC_TRACE"), out long at) ? at : long.MaxValue; // print frames allocating more than N bytes
    public static readonly bool Trace = Environment.GetEnvironmentVariable("STRESS_TRACE") == "1";
    double _now = 1000;
    public App App { get; }
    public TextSystem Text { get; }
    public List<Win> Wins { get; } = [];

    public Rig(TextSystem text)
    {
        Text = text;
        App = new App { Clock = () => _now, Text = text };
    }

    public Win Add(Func<App, View> root, Size size, float scale = 1)
    {
        var w = new Window(App, Text) { Clock = () => _now };
        w.Root = root(App);
        var win = new Win(w) { Size = size, Scale = scale };
        Wins.Add(win);
        return win;
    }

    /// <summary>Runs <paramref name="pre"/> (input, Notify…) then one frame of <paramref name="win"/>, timing the whole thing.</summary>
    public Sample Frame(Win win, Action? pre = null)
    {
        long a0 = GC.GetAllocatedBytesForCurrentThread();
        long t0 = Stopwatch.GetTimestamp();
        _now += FrameSeconds;
        App.RunTimers();
        pre?.Invoke();
        App.Flush();
        var scene = win.Window.Draw(win.Size, win.Scale);
        App.Flush();
        long t1 = Stopwatch.GetTimestamp();
        if (Present is null) win.Canvas.Resize((int)(win.Size.Width * win.Scale), (int)(win.Size.Height * win.Scale));
        double cover = 1; int kind = 1; // kind: 0 empty, 1 full, 2 partial
        if (Present is not null) Present(scene, win);
        else if (FullRaster) SoftwareRenderer.Render(scene, win.Canvas);
        else
        {
            var dr = win.Damage.Update(scene, win.Canvas.Width, win.Canvas.Height);
            kind = dr.Empty ? 0 : dr.Full ? 1 : 2;
            cover = SoftwareRenderer.RenderDamaged(scene, win.Canvas, dr) / Math.Max(1.0, (double)win.Canvas.Width * win.Canvas.Height);
            if (Verify.On) Verify.Check(scene, win.Canvas, dr, "win");
        }
        long t2 = Stopwatch.GetTimestamp();
        double ms = 1000.0 / Stopwatch.Frequency;
        var f = win.Window.LastFrame;
        if (Trace && (t2 - t0) * ms > 500) Console.WriteLine($"    slow frame {(t2 - t0) * ms:0} ms (layout {win.Window.LastFrame.LayoutMs:0} paint {win.Window.LastFrame.PaintMs:0} raster {(t2 - t1) * ms:0}, pre+flush+draw {(t1 - t0) * ms:0})");
        long alloc = GC.GetAllocatedBytesForCurrentThread() - a0;
        if (alloc > AllocTrace) Console.WriteLine($"    alloc frame {alloc / 1024} KB (kind {kind}, {f.Primitives} prims, {f.RenderedViews} rendered, partial {win.Window.LastFramePartial})");
        return new Sample((t2 - t0) * ms, f.LayoutMs, f.PaintMs, (t2 - t1) * ms, alloc,
            f.RenderedViews, f.ReplayedViews, f.Nodes, f.Primitives, cover, kind);
    }

    /// <summary>Delivers one input event then draws (what a platform does per message batch).</summary>
    public Sample Send(Win win, in InputEvent e) { var ev = e; return Frame(win, () => win.Window.DispatchInput(ev)); }

    /// <summary>Uncounted frames until the window is quiet (warm-up / settle).</summary>
    public void Settle(Win win, int max = 200)
    {
        for (int i = 0; i < max; i++)
        {
            Frame(win);
            if (i >= 2 && !win.Window.FramePending && !win.Window.WantsAnimationFrame) return;
        }
    }

    public Point Center(Win win, int hitbox) { var b = win.Window.HitboxBounds(hitbox); return new Point(b.X + b.Width / 2, b.Y + b.Height / 2); }

    /// <summary>Centre of the largest scrollable hitbox, or the window centre.</summary>
    public Point ScrollTarget(Win win)
    {
        int best = -1; float area = 0;
        for (int i = 0; i < win.Window.HitboxCount; i++)
        {
            if (!win.Window.HitboxIsScroll(i)) continue;
            var b = win.Window.HitboxBounds(i);
            if (b.Width * b.Height > area) { area = b.Width * b.Height; best = i; }
        }
        return best >= 0 ? Center(win, best) : new Point(win.Size.Width / 2, win.Size.Height / 2);
    }
}
