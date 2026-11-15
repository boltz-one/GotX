namespace GotX.Port;

/// <summary>Closure-taking style wrappers: hover/active/focus(|s| ..), group_hover("g", |s| ..) and when/when_some/map(.., |this| ..).</summary>
public static class GpuiScopes
{
    static string VariantPrefix(string name) => name switch
    {
        "focus_visible" => "focus-visible:",
        "in_focus" => "focus:",
        _ => name.Replace('_', '-') + ":",
    };

    public static bool Variant(GpuiChains g, Call c, string prefix, StyleResult r, HashSet<int> consumed)
    {
        if (!c.HasArgs) return false;
        var args = g.SplitArgs(c.ArgStart, c.ArgEnd);
        if (args.Count == 0) return false;
        string vp = VariantPrefix(c.Name);
        if (!Body(g, args[^1], c.ArgEnd, out var chain, consumed))
        {
            r.Notes.Add($"{c.Name}(..): closure body is not a style chain; translate by hand");
            return true;
        }
        Run(g, chain!, prefix + vp, r, consumed);
        return true;
    }

    public static bool When(GpuiChains g, Call c, string prefix, StyleResult r, HashSet<int> consumed)
    {
        if (!c.HasArgs) return false;
        var args = g.SplitArgs(c.ArgStart, c.ArgEnd);
        if (args.Count == 0) return false;
        var closure = args[^1];
        if (g.ClosureBody(closure.From, closure.To) < 0) return false; // e.g. Iterator::map(Some): not ours
        if (!Body(g, closure, c.ArgEnd, out var chain, consumed)) { if (c.Name != "map") r.Notes.Add($"{c.Name}(..): closure body is not a style chain"); return c.Name != "map"; }
        var sub = new StyleResult();
        Run(g, chain!, prefix, sub, consumed);
        if (!sub.Any && sub.Notes.Count == 0) return true;
        string cond = c.Name == "map" ? "" : Clip(g.Text(args[0].From, args[0].To));
        if (c.Name == "map") r.Classes.AddRange(sub.Classes);
        else if (sub.Classes.Count > 0) r.Conditional.Add((c.Name == "when_some" ? $"Some({cond})" : cond, sub.Classes));
        foreach (var (cd, cl) in sub.Conditional) r.Conditional.Add(($"{cond} && {cd}", cl));
        r.Dynamic.AddRange(sub.Dynamic);
        r.Hints.AddRange(sub.Hints);
        r.Notes.AddRange(sub.Notes);
        return true;
    }

    static bool Body(GpuiChains g, (int From, int To) arg, int limit, out Chain? chain, HashSet<int> consumed)
    {
        chain = null;
        int b = g.ClosureBody(arg.From, arg.To);
        if (b < 0 || g.Toks[b].Kind != TokKind.Ident) return false;
        // The chain must start from the closure parameter (`|s| s.bg(..)`); `iter.map(|i| div()..)` is an Iterator::map, not a style wrapper.
        int open = arg.From < b ? arg.From : b;
        for (; open < b && g.Toks[open].Text != "|"; open++) { }
        if (open + 1 >= b || g.Toks[open + 1].Text != g.Toks[b].Text) return false;
        chain = g.ParseChain(b, Math.Min(limit, arg.To));
        if (chain is null) return false;
        consumed.Add(b);
        return true;
    }

    static void Run(GpuiChains g, Chain chain, string prefix, StyleResult r, HashSet<int> consumed)
    {
        foreach (var call in chain.Calls)
        {
            if (call.Name is "child" or "children" || !call.HasArgs || GpuiStyleMap.Translate(g, call, prefix, r, consumed)) continue;
            string arg = g.Text(call.ArgStart, call.ArgEnd);
            if (GpuiHints.ForCall(call.Name, arg) is { } hint) r.Hints.Add($".{call.Name}({Clip(arg)}): {hint}");
        }
    }

    static string Clip(string s)
    {
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
        return s.Length > 50 ? s[..47] + "..." : s;
    }
}
