using GotX;
using GotX.Raster;
using GotX.Text;

namespace ShadcnShots;

/// <summary>A captured state: the clipped region (window px), its pixels and layout items (region-relative), overlay count.</summary>
public sealed record StateShot(Canvas Image, Bounds Region, List<LayoutItem> Items, int Overlays);

public sealed class StateException(string message) : Exception(message);

/// <summary>
/// Replays a <see cref="StateScenario"/> on GotX with real input events (mouse move / down / up, Tab key presses, text input),
/// lets transitions and delays run out on the virtual clock, then clips the region around the target(s) and any overlay —
/// the same rule as states.mjs (reference side).
/// </summary>
public static class StatePlayer
{
    public static StateShot Play(StateScenario sc, SitePage page, TextSystem text, bool dark)
    {
        var s = new HeadlessSession(page, text, dark, new Size(sc.Width, sc.Height));
        if (!page.FullBleed)
        {
            // Flowing page: like the reference (viewport = full page height, nothing scrolls) the window holds the whole content,
            // which may be taller than the reference page. Regions are cut around each side's own targets, so this is consistent.
            s.Size = new Size(sc.Width, MathF.Max(sc.Height, MathF.Ceiling(ShotRenderer.ContentHeight(s.Inspect()))));
            s.Settle();
        }
        var rects = new List<Bounds>();
        foreach (var step in sc.Steps)
        {
            var snap = s.Inspect();
            var node = Resolve(snap, step.GotX) ?? throw new StateException($"target {step.Target} not found ({Describe(step.GotX)})");
            rects.Add(node.Screen);
            var p = Center(node.Visible);
            switch (step.Do)
            {
                case "hover": s.Send(new InputEvent(InputKind.MouseMove, p)); break;
                case "press":
                    s.Send(new InputEvent(InputKind.MouseMove, p));
                    s.Send(new InputEvent(InputKind.MouseDown, p));
                    break;
                case "click":
                case "type":
                    s.Send(new InputEvent(InputKind.MouseMove, p));
                    s.Send(new InputEvent(InputKind.MouseDown, p));
                    s.Send(new InputEvent(InputKind.MouseUp, p));
                    if (step.Do == "type") s.Send(new InputEvent(InputKind.TextInput, Text: step.Text ?? ""));
                    break;
                case "focus-key": TabTo(s, node.Screen, step.Target); break;
                default: throw new StateException($"unknown action {step.Do}");
            }
            s.Quiesce(step.Do == "hover" ? 1.2 : 0.4); // tooltip delay / menu mount, as states.mjs waits
        }
        var final = s.Inspect();
        var region = RegionOf(rects, s.Window.Overlays, (int)s.Size.Width, (int)s.Size.Height);
        if (region.Width <= 0 || region.Height <= 0) throw new StateException("target region lies outside the window");
        var image = ShotRenderer.RasterizeRegion(final, region);
        return new StateShot(image, region, ItemsIn(LayoutExport.FromSnapshot(final, text), region), s.Window.Overlays.Count);
    }

    /// <summary>Tab presses (KeyDown Tab, as a keyboard would) until the focused element is the target (or inside / around it).</summary>
    static void TabTo(HeadlessSession s, Bounds target, string name)
    {
        for (int i = 0; i < 200; i++)
        {
            s.Send(new InputEvent(InputKind.KeyDown, Key: Key.Tab));
            if (s.Window.FocusedBounds is { } f && (Inflate(target, 1).Contains(Center(f)) || f.Contains(Center(target)))) return;
        }
        throw new StateException($"target {name} never received keyboard focus (200 Tab presses)");
    }

    public static UiNode? Resolve(UiSnapshot snap, GotXTarget t)
    {
        if (t.Key is not null)
        {
            var node = snap.Find(t.Key);
            if (node is null) return null;
            int end = SubtreeEnd(snap, node.Index);
            if (t.Pick is { } pick) return NthInteractive(snap, node.Index + 1, end, pick);
            if (t.Next is { } next) return NthInteractive(snap, end, snap.Nodes.Count, next);
            return node;
        }
        if (t.Text is null) return null;
        string want = LayoutItem.Normalize(t.Text);
        int seen = 0;
        foreach (var n in snap.Nodes)
        {
            if (n.Kind != "text" || LayoutItem.Normalize(n.Text) != want || n.Visible.Width <= 0) continue;
            if (seen++ < t.Index) continue;
            for (var a = n; ; a = snap.Nodes[a.Parent])
            {
                if (Interactive(a)) return a;
                if (a.Parent < 0) return n;
            }
        }
        return null;
    }

    static bool Interactive(UiNode n) => (n.Clickable || n.Focusable || n.Kind == "input") && n.Visible.Width > 0;

    static UiNode? NthInteractive(UiSnapshot snap, int from, int to, int nth)
    {
        for (int i = from; i < to; i++)
            if (Interactive(snap.Nodes[i]) && nth-- == 0) return snap.Nodes[i];
        return null;
    }

    /// <summary>Index one past the last descendant (nodes are in pre-order).</summary>
    static int SubtreeEnd(UiSnapshot snap, int index)
    {
        var n = snap.Nodes[index];
        return n.Children.Count == 0 ? index + 1 : SubtreeEnd(snap, n.Children[^1]);
    }

    static Bounds RegionOf(List<Bounds> targets, IReadOnlyList<Overlay> overlays, int vw, int vh)
    {
        var modal = overlays.Where(o => o.Options.Modal).Select(o => o.ContentBounds).ToList();
        var boxes = modal.Count > 0 ? modal : targets.Concat(overlays.Select(o => o.ContentBounds)).ToList();
        float x0 = boxes.Min(b => b.X), y0 = boxes.Min(b => b.Y), x1 = boxes.Max(b => b.Right), y1 = boxes.Max(b => b.Bottom);
        int ix0 = Math.Max(0, (int)MathF.Floor(x0 - Scenarios.Inflate)), iy0 = Math.Max(0, (int)MathF.Floor(y0 - Scenarios.Inflate));
        int ix1 = Math.Min(vw, (int)MathF.Ceiling(x1 + Scenarios.Inflate)), iy1 = Math.Min(vh, (int)MathF.Ceiling(y1 + Scenarios.Inflate));
        return new Bounds(ix0, iy0, ix1 - ix0, iy1 - iy0);
    }


    static List<LayoutItem> ItemsIn(List<LayoutItem> items, Bounds r)
    {
        var list = new List<LayoutItem>();
        foreach (var i in items)
        {
            if (i.Rect.Right <= r.X || i.Rect.X >= r.Right || i.Rect.Bottom <= r.Y || i.Rect.Y >= r.Bottom) continue;
            i.Rect = new Bounds(i.Rect.X - r.X, i.Rect.Y - r.Y, i.Rect.Width, i.Rect.Height);
            if (i.Own) i.TextRect = new Bounds(i.TextRect.X - r.X, i.TextRect.Y - r.Y, i.TextRect.Width, i.TextRect.Height);
            list.Add(i);
        }
        return list;
    }

    static Point Center(Bounds b) => new(b.X + b.Width / 2, b.Y + b.Height / 2);
    static Bounds Inflate(Bounds b, float d) => new(b.X - d, b.Y - d, b.Width + 2 * d, b.Height + 2 * d);
    static string Describe(GotXTarget t) => t.Key is not null ? $"key {t.Key}" + (t.Pick is { } p ? $" pick {p}" : t.Next is { } n ? $" next {n}" : "") : $"text \"{t.Text}\" #{t.Index}";
}
