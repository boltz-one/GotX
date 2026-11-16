namespace GotX.Stress;

static class TreeScenarios
{
    /// <summary>Huge tree: hover zigzag, wheel down, wheel up (deterministic; no RNG needed).</summary>
    public static Dictionary<string, double>? Tree(GotX.Text.TextSystem text, int blocks)
    {
        if (int.TryParse(Environment.GetEnvironmentVariable("STRESS_BLOCKS"), out int ov)) blocks = ov; // probe other sizes
        var rig = new Rig(text);
        var win = rig.Add(a => a.New(new HugeTree(blocks)), Scenarios.Desktop);
        var m = Scenarios.Begin(rig, win, blocks >= 500 ? 1 : 4, blocks >= 500 ? 4 : 200); // 0.45 s per frame at 100k nodes: short settle
        var p = rig.ScrollTarget(win);
        int k = blocks >= 500 ? 10 : 1; // frames cost ~1 s at 100k nodes: divide the event counts by 10
        for (int i = 0; i < 60 / k; i++)
            m.Add(rig.Send(win, new InputEvent(InputKind.MouseMove, new Point(40 + (i * 97) % 1100, 30 + (i * 53) % 700))));
        m.Add(rig.Send(win, new InputEvent(InputKind.MouseMove, p)));
        for (int i = 0; i < 150 / k; i++) m.Add(rig.Send(win, new InputEvent(InputKind.Scroll, p, Delta: new Point(0, 96 * k))));
        for (int i = 0; i < 50 / k; i++) m.Add(rig.Send(win, new InputEvent(InputKind.Scroll, p, Delta: new Point(0, -96 * k))));
        for (int i = 0; i < 20 / k; i++) m.Add(rig.Frame(win));
        return m.Finish();
    }

    /// <summary>VirtualList (fixed 640 px height: without a definite height it renders every row): wheel, click-to-focus, PageDown / Down / End / Home.</summary>
    public static Dictionary<string, double>? VList(GotX.Text.TextSystem text, int rows)
    {
        var rig = new Rig(text);
        var win = rig.Add(a => a.New(new VListPage(rows)), Scenarios.Desktop);
        rig.Settle(win); // unmeasured scroll out and back: sizes the damage / hash arrays for this list before the measured window
        for (int i = 0; i < 3; i++) rig.Send(win, new InputEvent(InputKind.Scroll, new Point(400, 300), Delta: new Point(0, 120)));
        for (int i = 0; i < 3; i++) rig.Send(win, new InputEvent(InputKind.Scroll, new Point(400, 300), Delta: new Point(0, -120)));
        var m = Scenarios.Begin(rig, win, 6);
        var p = new Point(400, 300);
        for (int i = 0; i < 10; i++) m.Add(rig.Frame(win));
        m.Add(rig.Send(win, new InputEvent(InputKind.MouseMove, p)));
        m.Add(rig.Send(win, new InputEvent(InputKind.MouseDown, p)));
        m.Add(rig.Send(win, new InputEvent(InputKind.MouseUp, p)));
        for (int i = 0; i < 40; i++) m.Add(rig.Send(win, new InputEvent(InputKind.Scroll, p, Delta: new Point(0, 120))));
        foreach (var k in new[] { Key.PageDown, Key.PageDown, Key.Down, Key.End, Key.Home }) m.Add(rig.Send(win, new InputEvent(InputKind.KeyDown, Key: k)));
        for (int i = 0; i < 10; i++) m.Add(rig.Frame(win));
        var r = m.Finish();
        var list = ((VListPage)win.Window.Root!).List;
        r["renderedRows"] = list.EndRendered - list.FirstRendered;
        return r;
    }
}
