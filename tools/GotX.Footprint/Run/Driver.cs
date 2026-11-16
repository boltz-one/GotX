using System.Diagnostics;

namespace GotX.Footprint;

/// <summary>Scripted input for one scenario, posted to the host window. Idle scenarios only wait. Cycle scenarios (dialog, resize) run a fixed count so a leak is judged over the same N cycles.</summary>
sealed class Driver(nint hwnd, string scenario, Action tick)
{
    public const int Cycles = 200, WarmCycles = 15;
    int _step;

    public bool IsCycle => scenario is "dialog" or "resize";

    public void Setup()
    {
        var (w, h) = Native.ClientSize(hwnd);
        if (scenario == "input") { Native.Click(hwnd, w / 2, h / 2); Pause(200); } // focus -> caret blinks on the timer
    }

    /// <summary>Warm-up or measured phase: <paramref name="seconds"/> for continuous scenarios, a fixed cycle count for cycle ones (at least <paramref name="seconds"/> long).</summary>
    public void Run(double seconds, bool warm)
    {
        var clock = Stopwatch.StartNew();
        if (IsCycle)
        {
            int n = warm ? WarmCycles : Cycles;
            for (int i = 0; i < n; i++) Cycle();
            if (!warm) while (clock.Elapsed.TotalSeconds < seconds) Pause(50);
            return;
        }
        var (w, h) = Native.ClientSize(hwnd);
        while (clock.Elapsed.TotalSeconds < seconds)
        {
            if (scenario == "vlist") Native.Wheel(hwnd, w / 2, h / 2, -3); // always down: 100k rows are far from the end
            Pause(scenario == "vlist" ? 16 : 100);
        }
    }

    void Cycle()
    {
        if (scenario == "dialog")
        {
            var (w, h) = Native.ClientSize(hwnd);
            Native.Click(hwnd, w / 2, h / 2); // the only button, centred
            Pause(25);
            Native.Key(hwnd, 0x1B);           // Escape closes
            Pause(25);
            return;
        }
        // Resize drag: sizes sweep between ~900x600 and ~1500x900 like a user dragging the corner; each SetWindowPos returns after WM_SIZE was handled.
        int k = _step++;
        Native.Resize(hwnd, 900 + (int)(600 * Math.Abs(Math.Sin(k * 0.15))), 600 + (int)(300 * Math.Abs(Math.Sin(k * 0.11))));
        Pause(40);
    }

    public void Pause(int ms)
    {
        tick();
        Thread.Sleep(ms);
    }
}
