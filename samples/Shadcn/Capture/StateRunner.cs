using GotX;
using GotX.Raster;
using GotX.Text;

namespace ShadcnShots;

/// <summary>README row of one interaction state.</summary>
public sealed record StateRow(StateScenario Scenario, string Theme, PixelStats? Pixels, LayoutMatch? Layout, int RefOverlays, int GotXOverlays, string? Note)
{
    public string Name => Scenario.FileName(Theme);
}

/// <summary>Plays every scenario of the ported pages on GotX and compares with <c>states/reference</c> (from states.mjs).</summary>
public static class StateRunner
{
    public static List<StateRow> Run(string dir, TextSystem text, string? only, string? onlyStates)
    {
        string root = Path.Combine(dir, "states");
        foreach (var sub in new[] { "reference", "gotx", "diff", "compare" }) Directory.CreateDirectory(Path.Combine(root, sub));
        var filter = only?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var stateFilter = onlyStates?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var rows = new List<StateRow>();
        foreach (var sc in Scenarios.Load())
        {
            if (filter is not null && !filter.Contains(sc.Page)) continue;
            if (stateFilter is not null && !stateFilter.Contains(sc.Name)) continue;
            if (PageOf(sc.Page) is not { } page) continue; // page not ported yet: reference only
            foreach (var theme in sc.Themes)
            {
                var row = Capture(root, sc, page, text, theme);
                rows.Add(row);
                Console.WriteLine($"state {row.Name}: " + (row.Pixels is { } p ? $"diff {p.Diff0:P1} / {p.Diff24:P1}" : "–")
                    + $", overlays ref {row.RefOverlays} / gotx {row.GotXOverlays}" + (row.Note is null ? "" : $" — {row.Note}"));
            }
        }
        return rows;
    }

    public static SitePage? PageOf(string id)
    {
        if (id == HomePage.Page.Id) return HomePage.Page;
        foreach (var b in BlockCatalog.All) if (b.Id == id) return b;
        return null;
    }

    static StateRow Capture(string root, StateScenario sc, SitePage page, TextSystem text, string theme)
    {
        string name = sc.FileName(theme);
        StateShot shot;
        try { shot = StatePlayer.Play(sc, page, text, theme == "dark", ReferenceFrame(root, name)); }
        catch (StateException e) { return new StateRow(sc, theme, null, null, RefOverlays(root, name), 0, "GotX: " + e.Message); }
        PngWriter.Write(Path.Combine(root, "gotx", name + ".png"), shot.Image);
        LayoutMapFile.Write(Path.Combine(root, "gotx", name + ".json"), sc.Page, shot.Image.Width, shot.Image.Height, theme, shot.Items);
        var (stats, layout) = Compare(root, name, shot, writeImages: true);
        string? note = stats is null ? "no reference capture" : shot.Overflow ? "GotX content extends beyond the reference frame" : null;
        return new StateRow(sc, theme, stats, layout, RefOverlays(root, name), shot.Overlays, note);
    }

    /// <summary>Pixel + layout comparison of a GotX state shot with the reference files of <paramref name="name"/>.</summary>
    public static (PixelStats?, LayoutMatch?) Compare(string root, string name, StateShot shot, bool writeImages)
    {
        string refPng = Path.Combine(root, "reference", name + ".png"), refJson = Path.Combine(root, "reference", name + ".json");
        if (!File.Exists(refPng)) return (null, null);
        var reference = PngDecoder.Decode(File.ReadAllBytes(refPng), out int rw, out int rh);
        var stats = PixelCompare.Compare(reference, rw, rh, shot.Image, out var diff);
        if (writeImages)
        {
            PngWriter.Write(Path.Combine(root, "diff", name + ".png"), diff);
            PngWriter.Write(Path.Combine(root, "compare", name + ".png"), PixelCompare.Sheet(reference, rw, rh, shot.Image, diff));
        }
        var layout = File.Exists(refJson) ? LayoutMatcher.Match(LayoutMapFile.Read(refJson, out _, out _), shot.Items) : null;
        return (stats, layout);
    }

    /// <summary>The reference crop (region + anchor: first modal overlay, else the first target) from states.mjs JSON.</summary>
    public static RefFrame? ReferenceFrame(string root, string name)
    {
        string json = Path.Combine(root, "reference", name + ".json");
        if (!File.Exists(json)) return null;
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(json));
        var e = doc.RootElement;
        if (!e.TryGetProperty("region", out var region) || !e.TryGetProperty("target", out var target)) return null;
        var anchor = Rect(target);
        if (e.TryGetProperty("overlays", out var overlays))
            foreach (var o in overlays.EnumerateArray())
                if (o.TryGetProperty("modal", out var m) && m.GetBoolean()) { anchor = Rect(o); break; }
        var area = Rect(region);
        var items = LayoutMapFile.Read(json, out _, out _);
        foreach (var i in items)
        {
            i.Rect = new Bounds(i.Rect.X + area.X, i.Rect.Y + area.Y, i.Rect.Width, i.Rect.Height);
            i.TextRect = new Bounds(i.TextRect.X + area.X, i.TextRect.Y + area.Y, i.TextRect.Width, i.TextRect.Height);
        }
        return new RefFrame(area, anchor, items);
    }

    static Bounds Rect(System.Text.Json.JsonElement r) =>
        new(r.GetProperty("x").GetSingle(), r.GetProperty("y").GetSingle(), r.GetProperty("w").GetSingle(), r.GetProperty("h").GetSingle());

    static int RefOverlays(string root, string name)
    {
        string json = Path.Combine(root, "reference", name + ".json");
        if (!File.Exists(json)) return 0;
        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllBytes(json));
        return doc.RootElement.TryGetProperty("overlays", out var o) ? o.GetArrayLength() : 0;
    }
}
