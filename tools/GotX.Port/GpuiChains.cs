namespace GotX.Port;

/// <summary>One <c>.name(args)</c> (or <c>.field</c>) of a chain. Arg range is token indices [ArgStart, ArgEnd) inside the parens; -1 = no parens.</summary>
public sealed record Call(string Name, int ArgStart, int ArgEnd, int Line)
{
    public bool HasArgs => ArgStart >= 0;
}

/// <summary>A postfix chain: root (path + optional call/macro) followed by method calls.</summary>
public sealed class Chain
{
    public int Line, StartTok, EndTok;
    public string RootName = "";
    /// <summary>First path segment (the type in <c>Button::new</c>), or the same as RootName for a plain identifier.</summary>
    public string RootFirst = "";
    public string RootText = "";
    public int RootArgStart = -1, RootArgEnd = -1;
    public List<Call> Calls { get; } = [];
}

/// <summary>Chain recogniser over the token list (not a parser: expressions between chains are skipped).</summary>
public sealed class GpuiChains(string src, List<Tok> toks)
{
    static readonly HashSet<string> Keywords = ["let", "mut", "fn", "pub", "impl", "for", "if", "else", "match", "return", "use", "mod", "struct", "enum", "trait", "where", "as", "in", "move", "ref", "while", "loop", "const", "static", "type", "dyn", "async", "await", "unsafe", "crate", "super", "true", "false"];

    public string Src => src;
    public List<Tok> Toks => toks;

    public string Text(int from, int to) => from >= to || from < 0 ? "" : src[toks[from].Start..toks[to - 1].End];

    /// <summary>Index of the token closing the bracket at <paramref name="open"/>; toks.Count if unbalanced.</summary>
    public int Close(int open)
    {
        int depth = 0;
        for (int i = open; i < toks.Count; i++)
        {
            if (toks[i].Kind != TokKind.Punct) continue;
            if (toks[i].Text is "(" or "[" or "{") depth++;
            else if (toks[i].Text is ")" or "]" or "}" && --depth == 0) return i;
        }
        return toks.Count;
    }

    /// <summary>Splits [from,to) at top-level commas.</summary>
    public List<(int From, int To)> SplitArgs(int from, int to)
    {
        var list = new List<(int, int)>();
        int depth = 0, start = from;
        for (int i = from; i < to; i++)
        {
            if (toks[i].Kind != TokKind.Punct) continue;
            string t = toks[i].Text;
            if (t is "(" or "[" or "{") depth++;
            else if (t is ")" or "]" or "}") depth--;
            else if (t == "|" && depth == 0 && IsClosureStart(i, start)) { i = SkipClosureParams(i); }
            else if (t == "," && depth == 0) { list.Add((start, i)); start = i + 1; }
        }
        if (start < to) list.Add((start, to));
        return list;
    }

    bool IsClosureStart(int i, int argStart) => i == argStart || (toks[i - 1].Kind == TokKind.Punct && toks[i - 1].Text is "move");

    int SkipClosureParams(int open)
    {
        int j = open + 1;
        while (j < toks.Count && !(toks[j].Kind == TokKind.Punct && toks[j].Text == "|")) j++;
        return j;
    }

    /// <summary>Token index where the closure body starts (after <c>|params|</c>, block braces unwrapped); -1 if [from,to) is not a closure.</summary>
    public int ClosureBody(int from, int to)
    {
        int i = from;
        if (i < to && toks[i].Text == "move") i++;
        if (i >= to) return -1;
        if (toks[i].Text == "||") i++;
        else if (toks[i].Text == "|") i = SkipClosureParams(i) + 1;
        else return -1;
        if (i < to && toks[i].Text == "{") i++;
        return i < to ? i : -1;
    }

    /// <summary>Scans [from,to) and invokes <paramref name="found"/> for each chain start, innermost chains are found via the callback's recursion.</summary>
    public IEnumerable<Chain> Scan(int from, int to, HashSet<int> consumed)
    {
        for (int i = from; i < to;)
        {
            var t = toks[i];
            bool start = t.Kind == TokKind.Ident && !Keywords.Contains(t.Text) && (i == 0 || toks[i - 1].Text is not ("." or "::")) && !consumed.Contains(i);
            if (start && ParseChain(i, to) is { } ch)
            {
                yield return ch;
                i = Math.Max(i + 1, ch.EndTok);
                continue;
            }
            i++;
        }
    }

    public Chain? ParseChain(int i, int limit)
    {
        var ch = new Chain { Line = toks[i].Line, StartTok = i };
        int p = i;
        string last = toks[p].Text;
        p++;
        while (p + 1 < limit && toks[p].Text == "::" && (toks[p + 1].Kind == TokKind.Ident || toks[p + 1].Text == "<"))
        {
            if (toks[p + 1].Text == "<") { p = SkipAngle(p + 1); continue; }
            last = toks[p + 1].Text;
            p += 2;
        }
        ch.RootName = last;
        ch.RootFirst = toks[i].Text;
        if (p < limit && toks[p].Text == "!" && p + 1 < limit && toks[p + 1].Text is "(" or "[" or "{")
        {
            int close = Close(p + 1);
            ch.RootArgStart = p + 2; ch.RootArgEnd = close; ch.RootName += "!";
            p = close + 1;
        }
        else if (p < limit && toks[p].Text == "(")
        {
            int close = Close(p);
            ch.RootArgStart = p + 1; ch.RootArgEnd = close;
            p = close + 1;
        }
        ch.RootText = Clip(Text(i, p));
        while (p + 1 < limit)
        {
            if (toks[p].Text == "?") { p++; continue; }
            if (toks[p].Text != "." || toks[p + 1].Kind != TokKind.Ident) break;
            string name = toks[p + 1].Text;
            int line = toks[p + 1].Line;
            p += 2;
            if (p + 1 < limit && toks[p].Text == "::" && toks[p + 1].Text == "<") p = SkipAngle(p + 1);
            if (p < limit && toks[p].Text == "(")
            {
                int close = Close(p);
                ch.Calls.Add(new Call(name, p + 1, close, line));
                p = close + 1;
            }
            else ch.Calls.Add(new Call(name, -1, -1, line));
        }
        ch.EndTok = p;
        return ch;
    }

    int SkipAngle(int open)
    {
        int depth = 0, i = open;
        for (; i < toks.Count; i++)
        {
            if (toks[i].Text == "<") depth++;
            else if (toks[i].Text == ">" && --depth == 0) return i + 1;
            else if (toks[i].Text is ";" or "{") break;
        }
        return i;
    }

    static string Clip(string s)
    {
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
        return s.Length > 70 ? s[..67] + "..." : s;
    }
}
