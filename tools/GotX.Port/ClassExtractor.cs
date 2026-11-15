using System.Text;
using System.Text.RegularExpressions;

namespace GotX.Port;

/// <summary>One class string found in a source file (<see cref="Line"/> is 1-based).</summary>
public sealed record ClassString(string File, int Line, string Text);

/// <summary>
/// Pulls Tailwind class strings out of HTML, JSX/TSX (className, cn/cva/clsx/twMerge calls), Svelte/Vue (class, :class) and
/// GotX C# (.Class("...")). Heuristic scanner, not a parser: dynamic fragments (${...}, {expr}) are dropped, string literals
/// inside them are kept.
/// </summary>
public static partial class ClassExtractor
{
    [GeneratedRegex(@"(?<![\w\-])(?<name>class|className|:class|v-bind:class|\[class\]|\[ngClass\]|class:list)\s*=\s*(?=[""'{])")]
    private static partial Regex AttrRx();

    [GeneratedRegex(@"(?<![\w\.])(?:cn|cva|clsx|twMerge|twJoin|classNames|classnames|cx)\s*\(|\.Class\s*\(")]
    private static partial Regex CallRx();

    [GeneratedRegex(@"(?<![\w\.])tw\s*`")]
    private static partial Regex TagRx();

    public static List<ClassString> Extract(string file, string text)
    {
        bool cs = file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
        bool htmlLike = file.EndsWith(".html", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".htm", StringComparison.OrdinalIgnoreCase)
            || file.EndsWith(".vue", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".svelte", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".astro", StringComparison.OrdinalIgnoreCase);
        var lines = LineStarts(text);
        var seen = new HashSet<int>();
        var result = new List<(int Pos, string Text)>();
        void Add(int pos, string value)
        {
            if (value.Trim().Length > 0 && seen.Add(pos)) result.Add((pos, value));
        }

        if (!cs)
            foreach (Match m in AttrRx().Matches(text))
            {
                int i = m.Index + m.Length;
                bool js = m.Groups["name"].Value is ":class" or "v-bind:class" or "[ngClass]" or "[class]";
                if (text[i] == '{')
                {
                    int close = Balanced(text, i, false);
                    if (close > 0) foreach (var l in Literals(text, i + 1, close, false)) Add(l.Pos, l.Value);
                }
                else if (ReadString(text, i, false, out string raw, out _))
                {
                    if (htmlLike) raw = HtmlParser.Decode(raw);
                    if (js) { foreach (var l in Literals(raw, 0, raw.Length, false)) Add(i + 1 + l.Pos, l.Value); continue; }
                    // Svelte / template markup inside a plain attribute: keep the static text, mine the expressions for literals.
                    var stat = new StringBuilder();
                    for (int k = 0; k < raw.Length; k++)
                    {
                        if (raw[k] == '{')
                        {
                            int c = Balanced(raw, k, false);
                            if (c < 0) c = raw.Length - 1;
                            foreach (var l in Literals(raw, k + 1, c, false)) Add(i + 1 + l.Pos, l.Value);
                            stat.Append(' ');
                            k = c;
                        }
                        else stat.Append(raw[k]);
                    }
                    Add(i, stat.ToString());
                }
            }

        foreach (Match m in CallRx().Matches(text))
        {
            bool isClassCall = m.Value.StartsWith(".Class");
            if (cs != isClassCall) continue;
            int open = m.Index + m.Length - 1;
            int close = Balanced(text, open, cs);
            if (close < 0) continue;
            bool cva = m.Value.StartsWith("cva");
            foreach (var l in Literals(text, open + 1, close, cs))
                if (!cva || KeepCva(text, open, close, l.Pos)) Add(l.Pos, l.Value);
        }
        if (!cs)
            foreach (Match m in TagRx().Matches(text))
                if (ReadString(text, m.Index + m.Length - 1, false, out string v, out _)) Add(m.Index + m.Length - 1, v);

        // C# class tables: `const string X = "..."`, `static readonly string[] Y = [..]`, `string Z => "..."` (the pattern components use
        // to avoid per-frame concatenation). Only literals that look like Tailwind classes are taken (>= 60 % of tokens recognised).
        if (cs)
            foreach (Match m in DeclRx().Matches(text))
            {
                int start = m.Index + m.Length, end = DeclEnd(text, start);
                foreach (var l in Literals(text, start, end, true))
                    if (LooksLikeClasses(l.Value)) Add(l.Pos, l.Value);
            }

        result.Sort(static (a, b) => a.Pos.CompareTo(b.Pos));
        return result.Select(r => new ClassString(file, LineOf(lines, r.Pos), r.Text)).ToList();
    }

    [GeneratedRegex(@"\bstring(?:\[\])?\??\s+\w+\s*(?:=>|=)")]
    private static partial Regex DeclRx();

    /// <summary>End of a declaration initialiser: the next ';' outside strings and brackets.</summary>
    static int DeclEnd(string s, int from)
    {
        int depth = 0;
        for (int i = from; i < s.Length; i++)
        {
            char ch = s[i];
            bool prefixed = (ch is '@' or '$') && i + 1 < s.Length && s[i + 1] is '"' or '@' or '$';
            if (ch == '"' || prefixed) { if (!ReadString(s, i, true, out _, out int e)) return s.Length; i = e; }
            else if (ch is '(' or '[' or '{') depth++;
            else if (ch is ')' or ']' or '}') depth--;
            else if (ch == ';' && depth <= 0) return i;
        }
        return s.Length;
    }

    /// <summary>True when at least 60 % of the whitespace-separated tokens are recognisable Tailwind utilities (parser-supported or covered by a workaround rule).</summary>
    public static bool LooksLikeClasses(string text)
    {
        var tokens = Tokens(text).ToList();
        if (tokens.Count == 0) return false;
        int known = tokens.Count(t => ClassAudit.IsSupported(t) || Workarounds.IsKnownUnsupported(t));
        return known * 10 >= tokens.Count * 6;
    }

    /// <summary>Class-like token: not a template remnant or punctuation.</summary>
    public static bool IsClassToken(string t)
    {
        if (t.Length == 0 || t.Contains("${") || t.Contains("{{") || t.Contains("}}") || t.Contains('\\') || t.Contains("=>")) return false;
        if (t[0] is '=' or '?' or '+' or '&' or '|' or ';' or ',' or '(' or ')' or '{' or '}') return false;
        foreach (char c in t) if (char.IsLetterOrDigit(c)) return true;
        return false;
    }

    public static IEnumerable<string> Tokens(string classes) =>
        classes.Split((char[])[' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Where(IsClassToken);

    // ---- cva: only class strings, not variant names ("default", "sm" in defaultVariants / compoundVariants selectors) ----
    static bool KeepCva(string text, int open, int close, int pos)
    {
        foreach (string kw in new[] { "defaultVariants", "compoundVariants" })
        {
            int k = text.IndexOf(kw, open, close - open, StringComparison.Ordinal);
            if (k < 0) continue;
            int b = k + kw.Length;
            while (b < close && text[b] is ' ' or ':' or '\n' or '\r' or '\t') b++;
            if (b >= close || text[b] is not ('{' or '[')) continue;
            int e = Balanced(text, b, false);
            if (e < 0 || pos < b || pos > e) continue;
            if (kw == "defaultVariants") return false;
            string key = KeyBefore(text, pos);
            return key is "class" or "className";
        }
        return true;
    }

    static string KeyBefore(string text, int pos)
    {
        int i = pos - 1;
        while (i >= 0 && char.IsWhiteSpace(text[i])) i--;
        if (i < 0 || text[i] != ':') return "";
        i--;
        while (i >= 0 && (char.IsWhiteSpace(text[i]) || text[i] is '"' or '\'')) i--;
        int end = i;
        while (i >= 0 && (char.IsLetterOrDigit(text[i]) || text[i] == '_')) i--;
        return text[(i + 1)..(end + 1)];
    }

    // ---- scanning helpers ----

    /// <summary>Index of the bracket closing the one at <paramref name="open"/>, string- and comment-aware; -1 if unbalanced.</summary>
    public static int Balanced(string s, int open, bool cs)
    {
        char o = s[open], c = o switch { '(' => ')', '{' => '}', '[' => ']', _ => o };
        int depth = 0;
        for (int i = open; i < s.Length; i++)
        {
            char ch = s[i];
            bool prefixed = cs && (ch is '@' or '$') && i + 1 < s.Length && s[i + 1] is '"' or '@' or '$';
            if (ch is '"' or '`' || (ch == '\'' && !cs) || prefixed)
            {
                if (!ReadString(s, i, cs, out _, out int end)) return -1;
                i = end;
            }
            else if (ch == '/' && i + 1 < s.Length && s[i + 1] == '/') { while (i < s.Length && s[i] != '\n') i++; }
            else if (ch == '/' && i + 1 < s.Length && s[i + 1] == '*') { int e = s.IndexOf("*/", i + 2, StringComparison.Ordinal); if (e < 0) return -1; i = e + 1; }
            else if (ch == o) depth++;
            else if (ch == c && --depth == 0) return i;
        }
        return -1;
    }

    /// <summary>All string literals between [start,end): (offset of the literal's opening quote, decoded value).</summary>
    public static List<(int Pos, string Value)> Literals(string s, int start, int end, bool cs)
    {
        var list = new List<(int, string)>();
        for (int i = start; i < end; i++)
        {
            char ch = s[i];
            if (ch == '/' && i + 1 < end && s[i + 1] == '/') { while (i < end && s[i] != '\n') i++; continue; }
            if (ch == '/' && i + 1 < end && s[i + 1] == '*') { int e = s.IndexOf("*/", i + 2, StringComparison.Ordinal); i = e < 0 ? end : e + 1; continue; }
            bool prefixed = cs && (ch is '@' or '$') && i + 1 < end && s[i + 1] is '"' or '@' or '$';
            if ((ch is '"' or '`' || (ch == '\'' && !cs) || prefixed) && ReadString(s, i, cs, out string v, out int e2))
            {
                list.Add((i, v));
                i = e2;
            }
        }
        return list;
    }

    /// <summary>Reads the string literal starting at <paramref name="i"/> (quote, backtick, C# @"…", $"…", """…""").</summary>
    public static bool ReadString(string s, int i, bool cs, out string value, out int end)
    {
        value = ""; end = i;
        bool verbatim = false, interp = false;
        int p = i;
        if (cs) while (p < s.Length && s[p] is '@' or '$') { verbatim |= s[p] == '@'; interp |= s[p] == '$'; p++; }
        if (p >= s.Length) return false;
        char q = s[p];
        if (q is not ('"' or '\'' or '`')) return false;
        if (cs && s.AsSpan(p).StartsWith("\"\"\""))
        {
            int e = s.IndexOf("\"\"\"", p + 3, StringComparison.Ordinal);
            if (e < 0) return false;
            value = s[(p + 3)..e]; end = e + 2;
            return true;
        }
        var sb = new StringBuilder();
        for (int k = p + 1; k < s.Length; k++)
        {
            char ch = s[k];
            if (ch == q)
            {
                if (verbatim && k + 1 < s.Length && s[k + 1] == '"') { sb.Append('"'); k++; continue; }
                value = sb.ToString(); end = k;
                return true;
            }
            if (ch == '\\' && !verbatim && k + 1 < s.Length) { sb.Append(s[k + 1]); k++; continue; }
            if ((q == '`' && ch == '$' && k + 1 < s.Length && s[k + 1] == '{') || (interp && ch == '{'))
            {
                if (interp && k + 1 < s.Length && s[k + 1] == '{') { sb.Append('{'); k++; continue; }
                int c = Balanced(s, q == '`' ? k + 1 : k, cs);
                if (c < 0) return false;
                sb.Append(' '); k = c;
                continue;
            }
            if (ch == '\n' && q != '`' && !verbatim) return false;
            sb.Append(ch);
        }
        return false;
    }

    static int[] LineStarts(string t)
    {
        var l = new List<int> { 0 };
        for (int i = 0; i < t.Length; i++) if (t[i] == '\n') l.Add(i + 1);
        return l.ToArray();
    }

    static int LineOf(int[] starts, int pos)
    {
        int i = Array.BinarySearch(starts, pos);
        return (i >= 0 ? i : ~i - 1) + 1;
    }
}
