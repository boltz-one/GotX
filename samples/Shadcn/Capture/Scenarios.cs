using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ShadcnShots;

/// <summary>GotX side of a scenario target: <c>key</c> (optionally its <c>pick</c>-th interactive descendant or the
/// <c>next</c>-th interactive node after it) or <c>text</c> (the <c>index</c>-th text node with that text → its interactive ancestor).</summary>
public sealed record GotXTarget(string? Key, string? Text, int? Pick, int? Next, int Index);

public sealed record StateStep(string Target, string Do, string? Text, GotXTarget GotX);

/// <summary>One interaction state of a page, e.g. <c>get-started-hover</c>.</summary>
public sealed record StateScenario(string Page, string Name, int Width, int Height, string[] Themes, List<StateStep> Steps)
{
    public string FileName(string theme) => $"{Page}-{Name}-{theme}";
}

/// <summary>
/// Reads <c>tools/ShadcnRef/scenarios.json</c> — the same file states.mjs drives Edge with — and expands it exactly like
/// scenarios.mjs: <c>{ target, do: [a, b] }</c> → states <c>target-a</c>, <c>target-b</c>; <c>{ name, steps }</c> → one state.
/// </summary>
public static class Scenarios
{
    /// <summary>Margin around target / overlay boxes (CSS px) — INFLATE in scenarios.mjs.</summary>
    public const int Inflate = 24;

    public static string DefaultPath([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source) ?? ".", "..", "..", "..", "tools", "ShadcnRef", "scenarios.json"));

    public static List<StateScenario> Load(string? path = null)
    {
        using var doc = JsonDocument.Parse(File.ReadAllBytes(path ?? DefaultPath()));
        var list = new List<StateScenario>();
        foreach (var p in doc.RootElement.GetProperty("pages").EnumerateArray())
        {
            string page = p.GetProperty("page").GetString()!;
            int width = p.GetProperty("width").GetInt32(), height = p.GetProperty("height").GetInt32();
            string[] themes = p.TryGetProperty("themes", out var th) ? th.EnumerateArray().Select(t => t.GetString()!).ToArray() : ["light", "dark"];
            var targets = new Dictionary<string, GotXTarget>();
            foreach (var t in p.GetProperty("targets").EnumerateObject()) targets[t.Name] = ParseTarget(t.Value.GetProperty("gotx"));
            StateStep Step(JsonElement s, string? action = null)
            {
                string target = s.GetProperty("target").GetString()!;
                if (!targets.TryGetValue(target, out var g)) throw new InvalidDataException($"scenario {page}: unknown target {target}");
                return new StateStep(target, action ?? s.GetProperty("do").GetString()!, Str(s, "text"), g);
            }
            foreach (var s in p.GetProperty("states").EnumerateArray())
            {
                if (s.TryGetProperty("steps", out var steps))
                    list.Add(new StateScenario(page, s.GetProperty("name").GetString()!, width, height, themes, steps.EnumerateArray().Select(x => Step(x)).ToList()));
                else
                {
                    var actions = s.GetProperty("do");
                    foreach (var a in actions.ValueKind == JsonValueKind.Array ? actions.EnumerateArray().Select(x => x.GetString()!) : [actions.GetString()!])
                        list.Add(new StateScenario(page, $"{s.GetProperty("target").GetString()}-{a}", width, height, themes, [Step(s, a)]));
                }
            }
        }
        return list;
    }

    static GotXTarget ParseTarget(JsonElement e) => new(Str(e, "key"), Str(e, "text"), Int(e, "pick"), Int(e, "next"), Int(e, "index") ?? 0);

    static string? Str(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    static int? Int(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;
}
