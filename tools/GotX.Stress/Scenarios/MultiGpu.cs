namespace GotX.Stress;

static class MultiWindowScenario
{
    public static Dictionary<string, double>? Run(GotX.Text.TextSystem text)
    {
        var rig = new Rig(text);
        var wins = new[]
        {
            rig.Add(a => a.New(new MixedPage()), new Size(900, 650)),
            rig.Add(a => a.New(new MixedPage(12, 100)), new Size(700, 500), 1.25f),
            rig.Add(a => a.New(new ChurnPage(200)), new Size(800, 600), 1.5f),
            rig.Add(a => a.New(new HugeTree(20)), new Size(600, 700)),
        };
        foreach (var w in wins) rig.Settle(w);
        for (int i = 0; i < 6; i++) foreach (var w in wins) rig.Frame(w);
        var m = new Meter();
        m.Begin();
        var rng = new Random(99);
        var churn = (ChurnPage)wins[2].Window.Root!;
        var mixed = (MixedPage)wins[0].Window.Root!;
        // One round = a state change in several windows, one input event to a random window, then every window draws (frames counts window-frames).
        for (int round = 0; round < 250; round++)
        {
            var target = wins[rng.Next(wins.Length)];
            var ev = InputScenario.Next(rng, target.Size);
            bool first = true;
            foreach (var w in wins)
            {
                var win = w;
                m.Add(rig.Frame(win, () =>
                {
                    if (!first) return;
                    first = false;
                    for (int i = 0; i < 5; i++) churn.Leaves[rng.Next(churn.Leaves.Length)].Bump();
                    mixed.Leaves[rng.Next(mixed.Leaves.Length)].Bump();
                    target.Window.DispatchInput(ev);
                }));
            }
        }
        return m.Finish();
    }
}

static class GpuLossScenario
{
    /// <summary>
    /// Skipped: the only device-loss seam (D3D11Renderer.InjectHr/InjectCount) is internal to GotX.Platform.Windows and that assembly's
    /// InternalsVisibleTo lists only GotX.Tests and GotX.Perf. Needs InternalsVisibleTo Include="GotX.Stress" (Core change request).
    /// </summary>
    public static Dictionary<string, double>? Run(GotX.Text.TextSystem text)
    {
        Console.WriteLine("  gpu-loss: SKIPPED - D3D11Renderer / device-loss inject seam are internal; GotX.Platform.Windows has no InternalsVisibleTo for GotX.Stress");
        return null;
    }
}
