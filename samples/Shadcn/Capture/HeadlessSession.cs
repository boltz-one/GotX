using GotX;
using GotX.Components;
using GotX.Text;

namespace ShadcnShots;

/// <summary>
/// One page hosted headless (no platform window) on a virtual clock that only moves when a frame is stepped: deterministic
/// captures, transitions/timers run to their end on demand. Input goes through <see cref="Window.DispatchInput"/> like a
/// real platform (same as GotX.Test's UiTest, which is tied to its own system fonts — this one uses Geist).
/// </summary>
public sealed class HeadlessSession : IDisposable
{
    public const double FrameSeconds = 0.016;
    double _now = 1000;

    public App App { get; }
    public Window Window { get; }
    public Size Size { get; set; }

    readonly ComponentStyle _previousStyle;

    /// <summary>
    /// Activates <see cref="SitePage.Style"/> app-wide before the page view is created (like SiteShell does when it opens the page);
    /// <see cref="Dispose"/> restores the previous style so other captures / tests are not affected.
    /// </summary>
    public HeadlessSession(SitePage page, TextSystem text, bool dark, Size size)
    {
        App = new App { Clock = () => _now };
        _previousStyle = ComponentTheme.Style;
        ComponentTheme.Use(App, page.Style);
        Window = new Window(App, text) { Clock = () => _now, DarkMode = dark };
        Window.Root = App.New(new ShotHost(page));
        Size = size;
        Settle();
    }

    /// <summary>One loop tick: advance the clock, run due timers, draw, flush effects.</summary>
    public void Step()
    {
        _now += FrameSeconds;
        App.RunTimers();
        Window.Draw(Size, 1);
        App.Flush();
    }

    /// <summary>Lets <paramref name="seconds"/> of virtual time pass frame by frame (tooltip delays, transitions).</summary>
    public void Advance(double seconds)
    {
        for (int i = 0, n = Math.Max(1, (int)Math.Round(seconds / FrameSeconds)); i < n; i++) Step();
    }

    /// <summary>Draws until nothing is pending (at least two frames); throws on an endless animation.</summary>
    public void Settle(int maxFrames = 600)
    {
        App.Flush();
        for (int i = 0; i < maxFrames; i++)
        {
            if (i >= 2 && !Window.FramePending && !Window.WantsAnimationFrame) return;
            Step();
        }
        throw new InvalidOperationException($"Page did not settle within {maxFrames} frames (endless animation?).");
    }

    /// <summary>
    /// Lets up to <paramref name="maxSeconds"/> pass: frames are drawn only while something is pending, idle time with pending
    /// timers (tooltip delay, menu close) is jumped over in 50 ms steps. Ends settled.
    /// </summary>
    public void Quiesce(double maxSeconds)
    {
        double end = _now + maxSeconds;
        App.Flush();
        while (_now < end)
        {
            if (Window.FramePending || Window.WantsAnimationFrame) { Step(); continue; }
            if (App.PendingTimers == 0) break;
            _now += 0.05;
            App.RunTimers();
            App.Flush();
        }
        Settle();
    }

    public void Send(in InputEvent e)
    {
        Window.DispatchInput(e);
        App.Flush();
        Step();
        Step();
    }

    public UiSnapshot Inspect() => Window.Inspect(Size, 1);

    public void Dispose() => ComponentTheme.Use(App, _previousStyle);
}
