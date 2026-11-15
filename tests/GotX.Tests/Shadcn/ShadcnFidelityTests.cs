using System.Globalization;
using System.Runtime.CompilerServices;
using GotX.Test;
using GotX.Text;
using ShadcnShots;

/// <summary>
/// Fidelity of samples/Shadcn against the committed ui.shadcn.com captures (samples/Shadcn/screenshots), offline:
/// <list type="bullet">
/// <item>pages: render headless with Geist, pair elements with the reference layout map by text (<see cref="LayoutMatcher"/>);
/// matched counts must not drop and median / p90 Δ must not grow (one test per page × viewport, light — layout is theme-independent);</item>
/// <item>interaction states (tools/ShadcnRef/scenarios.json): replay with real input on GotX, compare the clipped region with
/// the reference state image; the per-channel-24 pixel diff must not grow and the number of open overlays must not change.</item>
/// </list>
/// Thresholds live in <c>screenshots/reference/fidelity-baseline.txt</c>: the first run records them, <c>--update-baseline</c> refreshes.
/// </summary>
static class ShadcnFidelityTests
{
    const float MedianSlack = 0.5f, P90Slack = 1f;
    const double DiffSlack = 0.02;
    static TextSystem? s_fonts;
    static TextSystem Fonts => s_fonts ??= GeistFonts.Create();

    static string ScreenshotsDir([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", "..", "samples", "Shadcn", "screenshots"));

    static string BaselineFile => Path.Combine(ScreenshotsDir(), "reference", "fidelity-baseline.txt");

    public static void Register(TestSuite suite)
    {
        foreach (var target in ShotTargets.All())
        {
            if (target.Dark) continue;
            string id = $"{target.Page.Id}-{target.Width}";
            suite.Add("shadcn/fidelity/" + id, t => CheckPage(t, target, id));
        }
        foreach (var sc in Scenarios.Load())
        {
            if (StateRunner.PageOf(sc.Page) is not { } page) continue;
            string id = $"state/{sc.Page}-{sc.Name}";
            suite.Add("shadcn/fidelity/" + id, t => CheckState(t, sc, page, id));
        }
    }

    static void CheckPage(UiTest t, ShotTarget target, string id)
    {
        string refJson = Path.Combine(ScreenshotsDir(), "reference", target.Name + ".json");
        if (!File.Exists(refJson)) { t.Notes.Add($"no reference layout map {refJson}: skipped"); return; }
        var shot = ShotRenderer.Render(target, Fonts, pixels: false);
        var m = LayoutMatcher.Match(LayoutMapFile.Read(refJson, out _, out _), shot.Items);
        var now = new Dictionary<string, string>
        {
            ["texts"] = I(m.MatchedTexts), ["boxes"] = I(m.MatchedBoxes), ["median"] = F(m.Median), ["p90"] = F(m.P90),
        };
        if (Record(t, id, now) is not { } b) return;
        if (m.MatchedTexts < int.Parse(b["texts"], CultureInfo.InvariantCulture)) t.Fail($"{id}: matched texts {m.MatchedTexts} < baseline {b["texts"]}");
        if (m.MatchedBoxes < int.Parse(b["boxes"], CultureInfo.InvariantCulture)) t.Fail($"{id}: matched boxes {m.MatchedBoxes} < baseline {b["boxes"]}");
        if (m.Median > P(b["median"]) + MedianSlack) t.Fail($"{id}: median Δ {m.Median:0.##} px > baseline {b["median"]} (+{MedianSlack})");
        if (m.P90 > P(b["p90"]) + P90Slack) t.Fail($"{id}: p90 Δ {m.P90:0.##} px > baseline {b["p90"]} (+{P90Slack})");
    }

    static void CheckState(UiTest t, StateScenario sc, SitePage page, string id)
    {
        string root = Path.Combine(ScreenshotsDir(), "states"), name = sc.FileName("light");
        if (!File.Exists(Path.Combine(root, "reference", name + ".png"))) { t.Notes.Add($"no reference capture for {name}: skipped"); return; }
        StateShot shot;
        try { shot = StatePlayer.Play(sc, page, Fonts, dark: false); }
        catch (StateException e) { t.Fail($"{id}: {e.Message}"); return; }
        var (stats, _) = StateRunner.Compare(root, name, shot, writeImages: false);
        var now = new Dictionary<string, string> { ["diff24"] = stats!.Diff24.ToString("0.0000", CultureInfo.InvariantCulture), ["overlays"] = I(shot.Overlays) };
        if (Record(t, id, now) is not { } b) return;
        if (stats.Diff24 > P(b["diff24"]) + DiffSlack) t.Fail($"{id}: pixel diff (tol 24) {stats.Diff24:P1} > baseline {P(b["diff24"]):P1} (+{DiffSlack:P0})");
        if (now["overlays"] != b["overlays"]) t.Fail($"{id}: {shot.Overlays} overlay(s) open, baseline {b["overlays"]}");
    }

    /// <summary>Returns the stored baseline, or records <paramref name="now"/> (first run / --update-baseline) and returns null.</summary>
    static Dictionary<string, string>? Record(UiTest t, string id, Dictionary<string, string> now)
    {
        var all = ReadBaselines();
        if (!t.Options.UpdateBaseline && all.TryGetValue(id, out var b) && now.Keys.All(b.ContainsKey))
        {
            if (t.Options.Verbose) t.Notes.Add($"{id}: {Line(now)} (baseline {Line(b)})");
            return b;
        }
        all[id] = now;
        WriteBaselines(all);
        t.Notes.Add($"fidelity baseline {(t.Options.UpdateBaseline ? "updated" : "recorded")}: {id} {Line(now)}");
        return null;
    }

    static string I(int v) => v.ToString(CultureInfo.InvariantCulture);
    static string F(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    static float P(string s) => float.Parse(s, CultureInfo.InvariantCulture);
    static string Line(Dictionary<string, string> kv) => string.Join(' ', kv.Select(p => $"{p.Key}={p.Value}"));

    static SortedDictionary<string, Dictionary<string, string>> ReadBaselines()
    {
        var all = new SortedDictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        if (!File.Exists(BaselineFile)) return all;
        foreach (var raw in File.ReadAllLines(BaselineFile))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            all[parts[0]] = parts.Skip(1).Select(p => p.Split('=')).Where(p => p.Length == 2).ToDictionary(p => p[0], p => p[1]);
        }
        return all;
    }

    static void WriteBaselines(SortedDictionary<string, Dictionary<string, string>> all)
    {
        var lines = new List<string>
        {
            "# Fidelity baseline of samples/Shadcn vs the reference captures (tests/GotX.Tests/Shadcn/ShadcnFidelityTests.cs).",
            "# <page>-<width>: matched texts / boxes, median and p90 Δ (px). state/<page>-<state>: pixel diff (tol 24, fraction) and open overlays (light).",
            "# Refresh: dotnet run --project tests/GotX.Tests -- --filter shadcn/fidelity --update-baseline",
        };
        foreach (var (id, kv) in all) lines.Add($"{id} {Line(kv)}");
        File.WriteAllText(BaselineFile, string.Join('\n', lines) + "\n");
    }
}
