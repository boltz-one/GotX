namespace GotX.Port;

public sealed class UnsupportedToken
{
    public required string Token { get; init; }
    public int Count { get; set; }
    public int FirstLine { get; init; }
    public required string Suggestion { get; init; }
}

public sealed class FileAudit
{
    public required string File { get; init; }
    public int Strings, Tokens;
    public List<UnsupportedToken> Unsupported { get; } = [];
    /// <summary>Accepted by the parser but only approximated (see <see cref="Workarounds.Approximation"/>).</summary>
    public List<UnsupportedToken> Approximated { get; } = [];
    /// <summary>Valid Tailwind that GotX deliberately ignores (antialiased, transform, will-change-*): not counted as unsupported.</summary>
    public List<UnsupportedToken> NoOps { get; } = [];
}

/// <summary>Runs every token through <see cref="Tailwind.Parse"/> and groups the verdicts by file.</summary>
public sealed class AuditResult
{
    public List<FileAudit> Files { get; } = [];
    public Dictionary<string, int> Variants { get; } = new(StringComparer.Ordinal);
    public int TotalTokens, UniqueTokens, UniqueUnsupported;
}

public static class ClassAudit
{
    /// <summary>Does GotX accept this single token? (Same parser the runtime uses; theme tokens need ComponentTheme.Install first.)</summary>
    public static bool IsSupported(string token) => Tailwind.Parse(token).Unknown.Count == 0;

    public static AuditResult Analyze(IEnumerable<ClassString> strings)
    {
        var result = new AuditResult();
        var verdict = new Dictionary<string, bool>(StringComparer.Ordinal);
        var allUnsupported = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in strings.GroupBy(s => s.File))
        {
            var fa = new FileAudit { File = group.Key };
            var bad = new Dictionary<string, UnsupportedToken>(StringComparer.Ordinal);
            var approx = new Dictionary<string, UnsupportedToken>(StringComparer.Ordinal);
            var noops = new Dictionary<string, UnsupportedToken>(StringComparer.Ordinal);
            foreach (var s in group)
            {
                fa.Strings++;
                foreach (string token in ClassExtractor.Tokens(s.Text))
                {
                    fa.Tokens++;
                    result.TotalTokens++;
                    if (!verdict.TryGetValue(token, out bool ok)) verdict[token] = ok = IsSupported(token);
                    foreach (string v in VariantsOf(token)) result.Variants[v] = result.Variants.GetValueOrDefault(v) + 1;
                    string? noop = ok ? null : Workarounds.NoOp(token);
                    var target = ok ? approx : noop is not null ? noops : bad;
                    string? note = ok ? Workarounds.Approximation(token) : noop ?? Workarounds.Suggest(token);
                    if (ok && note is null) continue;
                    if (!ok && noop is null) allUnsupported.Add(token);
                    if (target.TryGetValue(token, out var u)) u.Count++;
                    else target[token] = new UnsupportedToken { Token = token, Count = 1, FirstLine = s.Line, Suggestion = note! };
                }
            }
            fa.Unsupported.AddRange(bad.Values.OrderByDescending(u => u.Count).ThenBy(u => u.FirstLine));
            fa.NoOps.AddRange(noops.Values.OrderByDescending(u => u.Count).ThenBy(u => u.FirstLine));
            fa.Approximated.AddRange(approx.Values.OrderByDescending(u => u.Count).ThenBy(u => u.FirstLine));
            result.Files.Add(fa);
        }
        result.UniqueTokens = verdict.Count;
        result.UniqueUnsupported = allUnsupported.Count;
        return result;
    }

    /// <summary>Variant prefixes of a token (everything before the last top-level ':'), e.g. "md:dark:hover:bg-x" gives md, dark, hover.</summary>
    public static IEnumerable<string> VariantsOf(string token)
    {
        int depth = 0, start = 0;
        for (int i = 0; i < token.Length; i++)
        {
            char c = token[i];
            if (c is '[' or '(') depth++;
            else if (c is ']' or ')') depth--;
            else if (c == ':' && depth == 0)
            {
                yield return token[start..i];
                start = i + 1;
            }
        }
    }
}
