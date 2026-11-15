using GotX;
using GotX.Raster;
using GotX.Text;

namespace ShadcnShots;

/// <summary>
/// Hosts one site page without the SiteShell chrome (the reference home capture hides the site header too):
/// Home flows at its natural height; full-bleed blocks fill the viewport.
/// </summary>
public sealed class ShotHost(SitePage page) : View
{
    View? _view;

    protected override void OnCreated() => _view = page.Create(App);

    public override Div Render(Window w) => page.FullBleed
        ? w.Div().Class("size-full bg-background text-foreground").Key("shot-root").Child(w.View(_view!).Class("size-full"))
        : w.Div().Class("w-full bg-background text-foreground").Key("shot-root").Child(w.View(_view!));
}

/// <summary>Result of one headless capture: pixels, the inspected tree and the exported layout items.</summary>
public sealed record ShotResult(Canvas Canvas, UiSnapshot Snapshot, List<LayoutItem> Items);

/// <summary>
/// Renders a <see cref="ShotTarget"/> headless (no window, CPU <see cref="SoftwareRenderer"/>) on a virtual clock that only
/// advances while frames are pending, so the result is deterministic: no running animation, every view in its initial state.
/// </summary>
public static class ShotRenderer
{
    /// <param name="pixels">false = layout only (tests): the canvas stays 0×0, no rasterization.</param>
    public static ShotResult Render(ShotTarget target, TextSystem text, bool pixels = true)
    {
        var session = new HeadlessSession(target.Page, text, target.Dark, new Size(target.Width, target.Height));
        if (target.FullPage)
        {
            // Full page: like document.scrollHeight — the window grows to the content height (never below the viewport).
            float content = ContentHeight(session.Inspect());
            session.Size = new Size(target.Width, MathF.Max(target.Height, MathF.Ceiling(content)));
            session.Settle();
        }
        var snapshot = session.Inspect();
        return new ShotResult(pixels ? Rasterize(snapshot) : new Canvas(), snapshot, LayoutExport.FromSnapshot(snapshot, text));
    }

    public static Canvas Rasterize(UiSnapshot snapshot)
    {
        var canvas = new Canvas();
        canvas.Resize((int)snapshot.Viewport.Width, (int)snapshot.Viewport.Height);
        SoftwareRenderer.Render(snapshot.Scene, canvas);
        return canvas;
    }

    /// <summary>
    /// Rasterizes only <paramref name="region"/> (integer window px): a copy of the scene shifted by −region.XY onto a
    /// region-sized canvas, so off-region primitives are rejected by the renderer's clamp instead of being painted.
    /// </summary>
    public static Canvas RasterizeRegion(UiSnapshot snapshot, Bounds region)
    {
        var src = snapshot.Scene;
        float dx = -region.X, dy = -region.Y;
        var scene = new Scene(src.Atlas) { ClearColor = src.ClearColor };
        scene.AddShapes(src, 0, src.ShapeCount, dx, dy);
        scene.AddGradients(src, 0, src.GradientCount);
        foreach (var p in src.Primitives)
        {
            var q = p;
            q.Bounds = p.Bounds.Offset(dx, dy);
            q.Clip = p.Clip.Offset(dx, dy);
            scene.Add(q);
        }
        var canvas = new Canvas();
        canvas.Resize((int)region.Width, (int)region.Height);
        SoftwareRenderer.Render(scene, canvas);
        return canvas;
    }

    /// <summary>Height of the page view inside the host (the host root itself is stretched to the window).</summary>
    public static float ContentHeight(UiSnapshot snapshot)
    {
        var root = snapshot.Find("shot-root") ?? throw new InvalidOperationException("shot-root not found.");
        float bottom = 0;
        foreach (int c in root.Children) bottom = MathF.Max(bottom, snapshot.Nodes[c].Layout.Bottom - root.Layout.Y);
        return bottom;
    }
}
