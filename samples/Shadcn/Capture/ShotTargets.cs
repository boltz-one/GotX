namespace ShadcnShots;

/// <summary>One capture: a site page at one viewport and theme. <see cref="Name"/> is the shared file stem with the reference.</summary>
public readonly record struct ShotTarget(SitePage Page, int Width, int Height, bool Dark)
{
    /// <summary>Home is captured full-page (window as tall as the content); blocks at the viewport only.</summary>
    public bool FullPage => !Page.FullBleed;
    public string Theme => Dark ? "dark" : "light";
    public string Name => $"{Page.Id}-{Width}-{Theme}";
}

/// <summary>The capture matrix — must match tools/ShadcnRef/capture.mjs (same pages, viewports and themes).</summary>
public static class ShotTargets
{
    /// <summary>Home: 1440/1024/640 wide (900 tall viewport, then full page). Blocks: 1440×900 and 1024×768.</summary>
    public static readonly (int Width, int Height)[] HomeSizes = [(1440, 900), (1024, 900), (640, 900)];
    public static readonly (int Width, int Height)[] BlockSizes = [(1440, 900), (1024, 768)];

    /// <summary>Every page (Home + <see cref="BlockCatalog.All"/>, so merged blocks appear automatically) × size × theme.</summary>
    public static List<ShotTarget> All(string? only = null)
    {
        var list = new List<ShotTarget>();
        var pages = new List<SitePage> { HomePage.Page };
        pages.AddRange(BlockCatalog.All);
        var filter = only?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var page in pages)
        {
            if (filter is not null && !filter.Contains(page.Id)) continue;
            foreach (var (w, h) in page.FullBleed ? BlockSizes : HomeSizes)
                foreach (bool dark in new[] { false, true })
                    list.Add(new ShotTarget(page, w, h, dark));
        }
        return list;
    }
}
