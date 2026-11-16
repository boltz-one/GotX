namespace GotX.Stress;

static class ChurnScenario
{
    public static Dictionary<string, double>? Run(GotX.Text.TextSystem text)
    {
        var rig = new Rig(text);
        var win = rig.Add(a => a.New(new ChurnPage(400)), Scenarios.Desktop);
        var page = (ChurnPage)win.Window.Root!;
        var m = Scenarios.Begin(rig, win);
        var rng = new Random(42);
        for (int f = 0; f < 600; f++)
        {
            int k = f % 100 == 99 ? page.Leaves.Length : 1 + rng.Next(8); // burst: every leaf at once
            m.Add(rig.Frame(win, () =>
            {
                if (k == page.Leaves.Length) foreach (var l in page.Leaves) l.Bump();
                else for (int i = 0; i < k; i++) page.Leaves[rng.Next(page.Leaves.Length)].Bump();
            }));
        }
        return m.Finish();
    }
}

static class InputScenario
{
    static readonly Key[] Keys = [Key.Tab, Key.Up, Key.Down, Key.Left, Key.Right, Key.Enter, Key.Space, Key.Backspace, Key.Home, Key.End, Key.PageDown, Key.PageUp, Key.Escape];

    /// <summary>One seeded random input event (shared with soak/multiwindow); returns events and frames are the caller's.</summary>
    public static InputEvent Next(Random rng, Size size)
    {
        var pos = new Point((float)(rng.NextDouble() * size.Width), (float)(rng.NextDouble() * size.Height));
        int r = rng.Next(100);
        if (r < 40) return new InputEvent(InputKind.MouseMove, pos);
        if (r < 50) return new InputEvent(InputKind.MouseDown, pos);
        if (r < 60) return new InputEvent(InputKind.MouseUp, pos);
        if (r < 80) return new InputEvent(InputKind.Scroll, pos, Delta: new Point(0, (rng.Next(2) == 0 ? 1 : -1) * rng.Next(20, 240)));
        if (r < 92) return new InputEvent(InputKind.KeyDown, Key: Keys[rng.Next(Keys.Length)], Modifiers: rng.Next(8) == 0 ? Modifiers.Shift : Modifiers.None);
        if (r < 99) return new InputEvent(InputKind.TextInput, Text: ((char)('a' + rng.Next(26))).ToString());
        return new InputEvent(InputKind.MouseLeave);
    }

    public static Dictionary<string, double>? Run(GotX.Text.TextSystem text)
    {
        var rig = new Rig(text);
        var win = rig.Add(a => a.New(new MixedPage()), Scenarios.Desktop);
        var m = Scenarios.Begin(rig, win);
        var rng = new Random(1234);
        for (int i = 0; i < 4000; i++) m.Add(rig.Send(win, Next(rng, win.Size)));
        return m.Finish();
    }
}
