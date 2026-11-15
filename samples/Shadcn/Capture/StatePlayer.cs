using GotX;
using GotX.Raster;
using GotX.Text;

namespace ShadcnShots;

/// <summary>
/// A captured state: the clipped region (window px), its pixels and layout items (region-relative), overlay count.
/// <see cref="OwnRegion"/> = the region GotX's own targets/overlays need; <see cref="Overflow"/> = it reaches > 2 px beyond
/// <see cref="Region"/> (when the crop is aligned to the reference, GotX content outside the reference frame is not compared).
/// </summary>
public sealed record StateShot(Canvas Image, Bounds Region, List<LayoutItem> Items, int Overlays, Bounds OwnRegion, bool Overflow);

/// <summary>Reference crop of a state (states.mjs JSON): its region, the anchor box (the target, or the modal content) and its
/// layout items in window coordinates.</summary>
public readonly record struct RefFrame(Bounds Region, Bounds Anchor, List<LayoutItem> Items);

public sealed class StateException(string message) : Exception(message);

/// <summary>
/// Replays a <see cref="StateScenario"/> on GotX with real input events (mouse move / down / up, Tab key presses, text input),
/// lets transitions and delays run out on the virtual clock, then clips the region around the target(s) and any overlay —
/// the same rule as states.mjs (reference side).
/// </summary>
public static class StatePlayer
{
    /// <param name="reference">When given, GotX is cropped with the SAME rect as the reference, translated by the local offset
    /// (median GotX − reference Δ of the text runs matched around the target, else of the boxes, else of the anchor box;
    /// rounded, ≤ 1 px per axis counts as 0) so inherited page offsets and sub-pixel snapping do not shift the crops.</param>
    public static StateShot Play(StateScenario sc, SitePage page, TextSystem text, bool dark, RefFrame? reference = null)
    {
        using var s = new HeadlessSession(page, text, dark, new Size(sc.Width, sc.Height));
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
        int vw = (int)s.Size.Width, vh = (int)s.Size.Height;
        var own = RegionOf(rects, s.Window.Overlays, vw, vh);
        var region = own;
        var items = LayoutExport.FromSnapshot(final, text);
        if (reference is { } r)
        {
            var modal = s.Window.Overlays.FirstOrDefault(o => o.Options.Modal);
            var anchor = modal?.ContentBounds ?? rects[0];
            var (dx, dy) = LocalOffset(r, items, own, anchor);
            region = Clamp(new Bounds(r.Region.X + Snap(dx), r.Region.Y + Snap(dy), r.Region.Width, r.Region.Height), vw, vh);
        }
        if (region.Width <= 0 || region.Height <= 0) throw new StateException("target region lies outside the window");
        bool overflow = own.X < region.X - 2 || own.Y < region.Y - 2 || own.Right > region.Right + 2 || own.Bottom > region.Bottom + 2;
        var image = ShotRenderer.RasterizeRegion(final, region);
        return new StateShot(image, region, ItemsIn(items, region), s.Window.Overlays.Count, own, overflow);
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

    /// <summary>
    /// Offset GotX − reference of the target, in order of trust: (1) median Δ of the matched pairs lying inside the reference
    /// target box (its label, or the box itself); (2) the target box itself when both boxes are about the same size (±4 px);
    /// (3) median Δ of the text runs matched around the target (then boxes); (4) the target box anyway.
    /// </summary>
    static (float, float) LocalOffset(RefFrame r, List<LayoutItem> gotx, Bounds own, Bounds anchor)
    {
        var near = new Bounds(own.X - 48, own.Y - 48, own.Width + 96, own.Height + 96);
        var local = gotx.Where(i => i.Rect.X < near.Right && i.Rect.Right > near.X && i.Rect.Y < near.Bottom && i.Rect.Bottom > near.Y).ToList();
        var pairs = LayoutMatcher.Match(r.Items, local).Pairs;
        var t = r.Anchor;
        var inside = pairs.Where(p => LayoutAnalysis.RefBox(p) is var b && b.X >= t.X - 1 && b.Y >= t.Y - 1 && b.Right <= t.Right + 1 && b.Bottom <= t.Bottom + 1).ToList();
        if (inside.Count > 0) return Median(inside);
        if (MathF.Abs(anchor.Width - t.Width) <= 4 && MathF.Abs(anchor.Height - t.Height) <= 4) return (anchor.X - t.X, anchor.Y - t.Y);
        var texts = pairs.Where(p => p.IsText).ToList();
        if (texts.Count > 0) return Median(texts);
        if (pairs.Count > 0) return Median(pairs);
        return (anchor.X - t.X, anchor.Y - t.Y);
    }

    static (float, float) Median(List<LayoutPair> pairs)
    {
        static float M(IEnumerable<float> v) { var s = v.OrderBy(x => x).ToList(); return s[s.Count / 2]; }
        return (M(pairs.Select(p => p.Dx)), M(pairs.Select(p => p.Dy)));
    }

    static float Snap(float d) => MathF.Abs(d) <= 1 ? 0 : MathF.Round(d);

    static Bounds Clamp(Bounds b, int vw, int vh)
    {
        float x0 = MathF.Max(0, b.X), y0 = MathF.Max(0, b.Y), x1 = MathF.Min(vw, b.Right), y1 = MathF.Min(vh, b.Bottom);
        return new Bounds(x0, y0, x1 - x0, y1 - y0);
    }

    static Point Center(Bounds b) => new(b.X + b.Width / 2, b.Y + b.Height / 2);
    static Bounds Inflate(Bounds b, float d) => new(b.X - d, b.Y - d, b.Width + 2 * d, b.Height + 2 * d);
    static string Describe(GotXTarget t) => t.Key is not null ? $"key {t.Key}" + (t.Pick is { } p ? $" pick {p}" : t.Next is { } n ? $" next {n}" : "") : $"text \"{t.Text}\" #{t.Index}";
}
