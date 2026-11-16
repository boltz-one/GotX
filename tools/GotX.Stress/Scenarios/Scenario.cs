using GotX.Text;
using ShadcnShots;

namespace GotX.Stress;

/// <summary>A named stress scenario. <see cref="Run"/> returns its metric map, or null when it cannot run on this machine (reported as skipped).</summary>
public sealed record Scenario(string Name, string What, Func<TextSystem, Dictionary<string, double>?> Run, bool Slow = false);

public static class Scenarios
{
    public static readonly Size Desktop = new(1280, 800);

    /// <summary>All default scenarios in run order (soak is separate: it only runs with --soak).</summary>
    public static readonly Scenario[] All =
    [
        new("tree-10k", "~10k-node tree: wheel scroll + hover", t => TreeScenarios.Tree(t, 100)),
        new("tree-100k", "~90k-node tree: wheel scroll + hover (10x fewer events)", t => TreeScenarios.Tree(t, 1000), Slow: true),
        new("vlist-100k", "VirtualList 100k rows: wheel, click, PageDown/Down/End/Home", t => TreeScenarios.VList(t, 100_000)),
        new("vlist-1m", "VirtualList 1M rows: same script", t => TreeScenarios.VList(t, 1_000_000)),
        new("churn", "400 views, random Notify (1-8/frame + bursts of all)", ChurnScenario.Run),
        new("input", "4000 seeded mouse/click/scroll/key/text events", InputScenario.Run),
        new("resize-dpi", "resize sweep 320..1600 wide + DPI 1/1.25/1.5/2", ResizeScenario.Run),
        new("text", "20k-char lines, no-space words, emoji, RTL, Vietnamese combining; re-wrap per frame", TextScenario.Run),
        new("multiwindow", "4 windows on one App, notify + input + step all", MultiWindowScenario.Run),
        new("gpu-loss", "D3D11/WARP device loss under load", GpuLossScenario.Run),
    ];

    /// <summary>Geist (deterministic) plus whatever system fallbacks exist for emoji / Arabic / Hebrew; the list is printed so runs are comparable.</summary>
    public static TextSystem Fonts()
    {
        var text = GeistFonts.Create();
        string dir = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + "\\Fonts\\";
        var added = new List<string>();
        foreach (string f in new[] { "seguiemj.ttf", "segoeui.ttf", "arial.ttf" })
            if (File.Exists(dir + f) && text.TryAddFallbackFont(dir + f)) added.Add(f);
        Console.WriteLine("fonts: Geist + fallbacks [" + string.Join(", ", added) + "]");
        return text;
    }

    /// <summary>Settles the window, runs an unmeasured warm-up, then starts a fresh <see cref="Meter"/>.</summary>
    public static Meter Begin(Rig rig, Win win, int warm = 12, int settleMax = 200)
    {
        rig.Settle(win, settleMax);
        for (int i = 0; i < warm; i++) rig.Frame(win);
        var m = new Meter();
        m.Begin();
        return m;
    }
}
