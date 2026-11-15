using System.Text;

namespace GotX.Port;

public sealed class ChainReport
{
    public int Line;
    public string Root = "";
    public string Classes = "";
    public List<(string Cond, string Classes)> Conditional { get; } = [];
    public List<string> Unsupported { get; } = [];
    public List<string> Dynamic { get; } = [];
    public List<string> Notes { get; } = [];
    public List<string> Hints { get; } = [];
    public List<string> Untranslated { get; set; } = [];
}

public sealed class GpuiReport
{
    public List<ChainReport> Chains { get; } = [];
    /// <summary>File-level hints: (first line, occurrences, hint).</summary>
    public List<(int Line, int Count, string Hint)> Globals { get; } = [];
}

/// <summary>Scans GPUI source, translates style chains to Tailwind classes and collects behaviour hints.</summary>
public static class GpuiAnalyzer
{
    static readonly HashSet<string> Structural = ["child", "children", "id", "key_context", "map", "when", "when_some", "into_any_element", "into_element", "detach", "ok", "unwrap", "clone", "into", "to_string", "as_ref", "collect", "iter", "enumerate", "filter", "len", "is_some", "is_none", "cloned"];

    public static GpuiReport Analyze(string src)
    {
        var toks = GpuiTokenizer.Tokenize(src);
        var g = new GpuiChains(src, toks);
        var report = new GpuiReport();
        var consumed = new HashSet<int>();
        Visit(g, 0, toks.Count, consumed, report);
        Globals(toks, report);
        report.Chains.Sort(static (a, b) => a.Line.CompareTo(b.Line));
        return report;
    }

    static void Visit(GpuiChains g, int from, int to, HashSet<int> consumed, GpuiReport report)
    {
        foreach (var chain in g.Scan(from, to, consumed))
        {
            Process(g, chain, consumed, report);
            if (chain.RootArgStart >= 0) Visit(g, chain.RootArgStart, chain.RootArgEnd, consumed, report);
            foreach (var c in chain.Calls) if (c.HasArgs) Visit(g, c.ArgStart, c.ArgEnd, consumed, report);
        }
    }

    static void Process(GpuiChains g, Chain chain, HashSet<int> consumed, GpuiReport report)
    {
        var r = new StyleResult();
        var rep = new ChainReport { Line = chain.Line, Root = chain.RootText };
        string root = chain.RootName;
        bool cx = root is "cx" or "window";
        string? implied = GpuiHints.ImpliedClasses(root);
        if (implied is not null) r.Notes.Add($"{root}() is Zed's ui helper = div() + {implied}");
        string rootKey = chain.RootFirst.Length > 0 && char.IsUpper(chain.RootFirst[0]) && chain.RootFirst != root ? chain.RootFirst : root;
        if (chain.RootArgStart >= 0 && GpuiHints.ForRoot(rootKey) is { } rh && !cx) rep.Hints.Add($"{rootKey}(..): {rh}");
        foreach (var call in chain.Calls)
        {
            string arg = call.HasArgs ? g.Text(call.ArgStart, call.ArgEnd) : "";
            if (GpuiStyleMap.Translate(g, call, "", r, consumed)) continue;
            if (cx && call.HasArgs || cx && call.Name is "notify")
            {
                if (GpuiHints.ForCx(call.Name) is { } ch) { rep.Hints.Add($"{root}.{call.Name}(..): {ch}"); continue; }
            }
            if (GpuiHints.ForCall(call.Name, arg) is { } h) { rep.Hints.Add($".{call.Name}({Clip(arg)}): {h}"); continue; }
            if (call.Name is "background_executor" or "foreground_executor") { rep.Hints.Add($".{call.Name}(): {GpuiHints.ForCx(call.Name)}"); continue; }
            if (!Structural.Contains(call.Name) && !cx && GpuiHints.IsElementRoot(root)) rep.Untranslated.Add(call.Name);
        }
        // Chains that do not start from an element constructor need at least two recognised style calls to count as styling (filters self.cursor_offset(), bounds.top()...).
        bool elementLike = GpuiHints.IsElementRoot(root) || rootKey != root || chain.RootFirst == "div";
        if (!elementLike && r.Classes.Count + r.Conditional.Count < 2) { r.Classes.Clear(); r.Conditional.Clear(); r.Dynamic.Clear(); r.Notes.Clear(); }
        var all = new List<string>();
        if (implied is not null) all.AddRange(implied.Split(' '));
        all.AddRange(r.Classes);
        foreach (var (cond, cl) in r.Conditional) rep.Conditional.Add((cond, string.Join(' ', cl)));
        rep.Classes = string.Join(' ', all);
        foreach (string t in all.Concat(r.Conditional.SelectMany(c => c.Classes)).Distinct())
            if (ClassExtractor.IsClassToken(t) && !ClassAudit.IsSupported(t)) rep.Unsupported.Add($"{t} -> {Workarounds.Suggest(t)}");
        rep.Dynamic.AddRange(r.Dynamic);
        rep.Hints.AddRange(r.Hints);
        rep.Notes.AddRange(r.Notes);
        rep.Untranslated = rep.Untranslated.Distinct().ToList();
        bool interesting = all.Count > 0 || r.Conditional.Count > 0 || rep.Hints.Count > 0 || rep.Dynamic.Count > 0 || (GpuiHints.IsElementRoot(root) && chain.Calls.Count > 0);
        if (rep.Classes.Length == 0 && rep.Conditional.Count == 0) rep.Root = Clip70(g.Text(chain.StartTok, chain.EndTok));
        if (interesting) report.Chains.Add(rep);
    }

    static void Globals(List<Tok> toks, GpuiReport report)
    {
        var sb = new StringBuilder();
        var starts = new List<(int Pos, int Line)>();
        for (int i = 0; i < toks.Count; i++)
        {
            if (i > 0 && toks[i].Kind is TokKind.Ident or TokKind.Number && toks[i - 1].Kind is TokKind.Ident or TokKind.Number) sb.Append(' ');
            starts.Add((sb.Length, toks[i].Line));
            sb.Append(toks[i].Text);
        }
        string text = sb.ToString();
        foreach (var (rx, hint) in GpuiHints.Globals)
        {
            var ms = rx.Matches(text);
            if (ms.Count == 0) continue;
            int idx = starts.FindLastIndex(s => s.Pos <= ms[0].Index);
            report.Globals.Add((idx < 0 ? 1 : starts[idx].Line, ms.Count, hint));
        }
        report.Globals.Sort(static (a, b) => a.Line.CompareTo(b.Line));
    }

    static string Clip70(string s)
    {
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
        return s.Length > 70 ? s[..67] + "..." : s;
    }

    static string Clip(string s)
    {
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
        return s.Length > 40 ? s[..37] + "..." : s;
    }
}
