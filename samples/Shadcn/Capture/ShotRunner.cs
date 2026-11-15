using System.Runtime.CompilerServices;
using GotX;
using GotX.Raster;

namespace ShadcnShots;

/// <summary>
/// Command line of the capture mode:
/// <c>--screenshots [dir] [--only ids]</c> renders every page headless into <c>gotx/</c>, then compares;
/// <c>--compare [dir] [--only ids]</c> compares the existing <c>gotx/</c> page files only (states are always replayed).
/// Interaction states (tools/ShadcnRef/scenarios.json) run after the pages: <c>--state a,b</c> filters them, <c>--no-states</c> skips them.
/// Default dir: <c>samples/Shadcn/screenshots</c>.
/// </summary>
public static class ShotRunner
{
    public static bool Wants(string[] args) => args.Contains("--screenshots") || args.Contains("--compare");

    public static int Run(string[] args)
    {
        bool render = args.Contains("--screenshots");
        string dir = Path.GetFullPath(DirArg(args, render ? "--screenshots" : "--compare") ?? DefaultDir());
        string? only = Arg(args, "--only");
        foreach (var sub in new[] { "reference", "gotx", "diff", "compare" }) Directory.CreateDirectory(Path.Combine(dir, sub));
        var text = GeistFonts.Create();
        var rows = new List<ShotRow>();
        var done = new HashSet<string>();
        foreach (var target in ShotTargets.All(only))
        {
            string gotPng = Path.Combine(dir, "gotx", target.Name + ".png"), gotJson = Path.Combine(dir, "gotx", target.Name + ".json");
            Canvas canvas;
            List<LayoutItem> items;
            if (render)
            {
                var shot = ShotRenderer.Render(target, text);
                canvas = shot.Canvas;
                items = shot.Items;
                PngWriter.Write(gotPng, canvas);
                LayoutMapFile.Write(gotJson, target.Page.Id, canvas.Width, canvas.Height, target.Theme, items);
            }
            else
            {
                if (!File.Exists(gotPng) || !File.Exists(gotJson)) { Console.WriteLine($"{target.Name}: no GotX capture, skipped"); continue; }
                canvas = Decode(gotPng);
                items = LayoutMapFile.Read(gotJson, out _, out _);
            }
            done.Add(target.Name);
            rows.Add(Compare(dir, target, canvas, items));
        }
        var referenceOnly = Directory.GetFiles(Path.Combine(dir, "reference"), "*.png")
            .Select(Path.GetFileNameWithoutExtension).Where(n => !done.Contains(n!)).Select(n => n!).Order().ToList();
        string? onlyStates = Arg(args, "--state");
        List<StateRow> states = args.Contains("--no-states") ? [] : StateRunner.Run(dir, text, only, onlyStates);
        if (only is null && onlyStates is null && !args.Contains("--no-states")) ShotReport.Write(dir, rows, referenceOnly, states);
        else Console.WriteLine("--only / --state / --no-states: README.md not rewritten (it indexes the full set).");
        return 0;
    }

    static ShotRow Compare(string dir, ShotTarget target, Canvas canvas, List<LayoutItem> items)
    {
        string refPng = Path.Combine(dir, "reference", target.Name + ".png"), refJson = Path.Combine(dir, "reference", target.Name + ".json");
        PixelStats? stats = null;
        LayoutMatch? layout = null;
        if (File.Exists(refPng))
        {
            var reference = PngDecoder.Decode(File.ReadAllBytes(refPng), out int rw, out int rh);
            stats = PixelCompare.Compare(reference, rw, rh, canvas, out var diff);
            PngWriter.Write(Path.Combine(dir, "diff", target.Name + ".png"), diff);
            PngWriter.Write(Path.Combine(dir, "compare", target.Name + ".png"), PixelCompare.Sheet(reference, rw, rh, canvas, diff));
        }
        if (File.Exists(refJson)) layout = LayoutMatcher.Match(LayoutMapFile.Read(refJson, out _, out _), items);
        Console.WriteLine($"{target.Name}: " + (stats is null ? "no reference" :
            $"{stats.GotXWidth}×{stats.GotXHeight} (ref {stats.RefWidth}×{stats.RefHeight}), diff {stats.Diff0:P2} / {stats.Diff24:P2}") +
            (layout is null ? "" : $", layout {layout.MatchedTexts}/{layout.RefTexts} texts {layout.MatchedBoxes}/{layout.RefBoxes} boxes, median Δ {layout.Median:0.#} px, max {layout.Max:0.#}"));
        return new ShotRow(target, stats, layout);
    }

    static Canvas Decode(string png)
    {
        var rgba = PngDecoder.Decode(File.ReadAllBytes(png), out int w, out int h);
        var c = new Canvas();
        c.Resize(w, h);
        for (int i = 0; i < w * h; i++)
        {
            uint a = rgba[i * 4 + 3];
            uint r = (rgba[i * 4] * a + 127) / 255, g = (rgba[i * 4 + 1] * a + 127) / 255, b = (rgba[i * 4 + 2] * a + 127) / 255;
            c.Pixels[i] = (a << 24) | (r << 16) | (g << 8) | b;
        }
        return c;
    }

    static string? Arg(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    static string? DirArg(string[] args, string flag) => Arg(args, flag) is { } v && !v.StartsWith("--", StringComparison.Ordinal) ? v : null;

    /// <summary><c>samples/Shadcn/screenshots</c> of the source tree (this file lives in samples/Shadcn/Screenshots).</summary>
    public static string DefaultDir([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source) ?? ".", "..", "screenshots"));
}
