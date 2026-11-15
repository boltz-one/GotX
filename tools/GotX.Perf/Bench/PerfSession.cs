using GotX.Raster;
using GotX.Text;

namespace GotX.Perf;

/// <summary>
/// One page hosted headless: virtual clock that only moves when a frame is stepped (deterministic), the CPU renderer
/// rasterises every frame (its cost is reported as the "present" part of the log), and the window logs through <see cref="PerfLog"/>.
/// </summary>
public sealed class PerfSession
{
    public const double FrameSeconds = 0.016;
    double _now = 1000;
    readonly Canvas _canvas = new();
    readonly bool _raster;
    readonly PerfLog _log;

    public App App { get; }
    public Window Window { get; }
    public Size Size { get; set; }

    public PerfSession(TextSystem text, Func<App, View> root, PerfLog log, Size size, bool raster = true)
    {
        _log = log;
        _raster = raster;
        App = new App { Clock = () => _now };
        Window = new Window(App, text) { Clock = () => _now, Log = log };
        Window.Root = root(App);
        Size = size;
    }

    public void Mark(string name, string? detail = null) => _log.Mark(name, detail);

    /// <summary>One loop tick: advance the clock, run timers, draw, flush effects, rasterise.</summary>
    public void Step()
    {
        _now += FrameSeconds;
        App.RunTimers();
        var scene = Window.Draw(Size, 1);
        App.Flush();
        if (!_raster) return;
        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        _canvas.Resize((int)Size.Width, (int)Size.Height);
        SoftwareRenderer.Render(scene, _canvas);
        double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        Window.LogPresent(ms, ms);
    }

    public void Steps(int n) { for (int i = 0; i < n; i++) Step(); }

    /// <summary>Delivers one input event then draws one frame (what a real platform does per message batch).</summary>
    public void Send(in InputEvent e)
    {
        Window.DispatchInput(e);
        App.Flush();
        Step();
    }

    /// <summary>Draws until nothing is pending (at least two frames); gives up silently after <paramref name="maxFrames"/>.</summary>
    public int Settle(int maxFrames = 400)
    {
        App.Flush();
        for (int i = 0; i < maxFrames; i++)
        {
            if (i >= 2 && !Window.FramePending && !Window.WantsAnimationFrame) return i;
            Step();
        }
        return maxFrames;
    }

    /// <summary>Runs <paramref name="body"/> with the log switched off (queries that must not appear as frames).</summary>
    public T Quietly<T>(Func<T> body)
    {
        var log = Window.Log;
        Window.Log = null;
        try { return body(); }
        finally { Window.Log = log; }
    }

    public Point Center(int hitbox)
    {
        var b = Window.HitboxBounds(hitbox);
        return new Point(b.X + b.Width / 2, b.Y + b.Height / 2);
    }
}
