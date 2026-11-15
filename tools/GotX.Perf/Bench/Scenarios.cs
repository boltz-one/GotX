namespace GotX.Perf;

/// <summary>
/// The scripted interactions run against every target, in this order (state carries over, deterministically).
/// Each scenario writes a "mark" record first so <c>report --phases</c>/<c>compare</c> and the bench can cut the log by scenario.
/// </summary>
public static class Scenarios
{
    /// <summary>Scenario names in run order (cold is run separately, in a fresh process).</summary>
    public static readonly string[] Names = ["idle", "hover", "scroll", "type", "click", "resize", "dark"];

    public static void Run(PerfSession s, string name)
    {
        s.Mark(name);
        switch (name)
        {
            case "idle": s.Steps(60); break;
            case "hover": Hover(s); break;
            case "scroll": Scroll(s); break;
            case "type": Type(s); break;
            case "click": Click(s); break;
            case "resize": Resize(s); break;
            case "dark": Dark(s); break;
            default: throw new ArgumentException("unknown scenario " + name);
        }
        s.Mark("end");
    }

    /// <summary>Cold start: everything from creating the view to the first settled state (the caller times fonts/process).</summary>
    public static void Cold(PerfSession s)
    {
        s.Mark("cold");
        s.Settle();
    }

    // Pointer zigzag over the viewport: one MouseMove per frame, so every interactive element gets enter/leave once.
    static void Hover(PerfSession s)
    {
        var size = s.Size;
        int n = 0;
        for (float y = 20; y < size.Height && n < 80; y += 56)
            for (float x = 20; x < size.Width && n < 80; x += 160, n++)
                s.Send(new InputEvent(InputKind.MouseMove, new Point((n / 7) % 2 == 0 ? x : size.Width - x, y)));
        s.Send(new InputEvent(InputKind.MouseLeave));
        s.Steps(4);
    }

    // Mouse wheel over the largest scroll area: 40 notches (one per frame) down, then 20 back up, then let the smooth scroll finish.
    static void Scroll(PerfSession s)
    {
        int best = -1;
        float area = 0;
        for (int i = 0; i < s.Window.HitboxCount; i++)
        {
            if (!s.Window.HitboxIsScroll(i)) continue;
            var b = s.Window.HitboxBounds(i);
            if (b.Width * b.Height > area) { area = b.Width * b.Height; best = i; }
        }
        if (best < 0) { s.Steps(2); return; }
        var p = s.Center(best);
        s.Send(new InputEvent(InputKind.MouseMove, p));
        for (int i = 0; i < 40; i++) s.Send(new InputEvent(InputKind.Scroll, p, Delta: new Point(0, 64)));
        for (int i = 0; i < 20; i++) s.Send(new InputEvent(InputKind.Scroll, p, Delta: new Point(0, -64)));
        s.Settle();
    }

    // Click the first visible text input and type into it, then delete.
    static void Type(PerfSession s)
    {
        int input = -1;
        for (int i = 0; i < s.Window.HitboxCount && input < 0; i++)
            if (s.Window.HitboxIsInput(i) && s.Window.HitboxBounds(i) is { Width: > 8, Height: > 8 }) input = i;
        if (input < 0) { s.Steps(2); return; }
        var p = s.Center(input);
        s.Send(new InputEvent(InputKind.MouseMove, p));
        s.Send(new InputEvent(InputKind.MouseDown, p));
        s.Send(new InputEvent(InputKind.MouseUp, p));
        foreach (char c in "Hello GotX, xin chào")
            s.Send(new InputEvent(InputKind.TextInput, Text: c.ToString()));
        for (int i = 0; i < 8; i++) s.Send(new InputEvent(InputKind.KeyDown, Key: Key.Backspace));
        s.Send(new InputEvent(InputKind.KeyDown, Key: Key.Escape));
        s.Settle();
    }

    // Click up to 24 clickable things (evenly spread over the page's hitboxes), each followed by Escape: exercises overlays
    // (menus, selects, dialogs, tooltips), toggles and navigation wherever the page has them.
    static void Click(PerfSession s)
    {
        var points = new List<Point>();
        var w = s.Window;
        var clickable = new List<int>();
        for (int i = 0; i < w.HitboxCount; i++)
        {
            var b = w.HitboxBounds(i);
            if (w.HitboxIsClick(i) && b.Width >= 12 && b.Height >= 12 && b.Right > 0 && b.Bottom > 0 && b.X < s.Size.Width && b.Y < s.Size.Height) clickable.Add(i);
        }
        int take = Math.Min(24, clickable.Count);
        for (int k = 0; k < take; k++) points.Add(s.Center(clickable[k * clickable.Count / take]));
        foreach (var p in points)
        {
            s.Send(new InputEvent(InputKind.MouseMove, p));
            s.Send(new InputEvent(InputKind.MouseDown, p));
            s.Send(new InputEvent(InputKind.MouseUp, p));
            s.Steps(3);
            s.Send(new InputEvent(InputKind.KeyDown, Key: Key.Escape));
            s.Steps(2);
        }
        s.Send(new InputEvent(InputKind.MouseLeave));
        s.Settle();
    }

    // Breakpoint jumps (3 frames each) and a continuous window drag (one frame per width).
    static void Resize(PerfSession s)
    {
        var orig = s.Size;
        foreach (float w in new[] { 1000f, 900f, 800f, 700f, 600f, 560f, 700f, 900f })
        {
            s.Size = new Size(w, orig.Height);
            s.Steps(3);
        }
        for (float w = orig.Width; w >= 600; w -= 20) { s.Size = new Size(w, orig.Height); s.Step(); }
        for (float w = 600; w <= orig.Width; w += 20) { s.Size = new Size(w, orig.Height); s.Step(); }
        s.Size = orig;
        s.Settle();
    }

    static void Dark(PerfSession s)
    {
        s.Window.DarkMode = true;
        s.Steps(4);
        s.Window.DarkMode = false;
        s.Steps(4);
    }
}
