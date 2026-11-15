using System.Text;
using System.Text.Json;

namespace GotX.Port;

/// <summary><c>classes</c>: Tailwind audit of a file, a directory tree or stdin (<c>-</c>).</summary>
public static class ClassesCommand
{
    static readonly string[] Exts = [".html", ".htm", ".tsx", ".jsx", ".ts", ".js", ".mjs", ".svelte", ".vue", ".astro", ".cs", ".razor", ".md", ".mdx", ".txt"];
    static readonly string[] SkipDirs = ["node_modules", "bin", "obj", ".git", "dist", "build", ".next"];

    public static int Run(string[] argv, TextWriter o)
    {
        var args = argv.ToList();
        bool json = PortApp.Flag(args, "--json");
        if (args.Count == 0) { o.WriteLine("usage: classes <file|dir|-> [--json]"); return 0; }
        var strings = new List<ClassString>();
        foreach (string path in args)
        {
            if (path == "-") strings.AddRange(ClassExtractor.Extract("<stdin>.html", Console.In.ReadToEnd()));
            else if (Directory.Exists(path)) foreach (string f in Files(path)) strings.AddRange(ClassExtractor.Extract(f, File.ReadAllText(f)));
            else if (File.Exists(path)) strings.AddRange(ClassExtractor.Extract(path, File.ReadAllText(path)));
            else throw new IOException($"not found: {path}");
        }
        var audit = ClassAudit.Analyze(strings);
        if (json) WriteJson(audit, o); else WriteText(audit, o);
        return 0; // an audit never fails the build
    }

    static IEnumerable<string> Files(string dir) =>
        Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
            .Where(f => Exts.Contains(Path.GetExtension(f).ToLowerInvariant()) && !f.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(p => SkipDirs.Contains(p)))
            .Order(StringComparer.Ordinal);

    static void WriteText(AuditResult a, TextWriter o)
    {
        o.WriteLine($"classes: {a.Files.Count} file(s), {a.Files.Sum(f => f.Strings)} class strings, {a.TotalTokens} tokens ({a.UniqueTokens} unique), {a.UniqueUnsupported} unique unsupported");
        foreach (var f in a.Files)
        {
            o.WriteLine($"== {f.File}  ({f.Strings} strings, {f.Tokens} tokens, {f.Unsupported.Count} unsupported)");
            foreach (var grp in f.Unsupported.GroupBy(u => u.Suggestion).OrderByDescending(g => g.Sum(u => u.Count)))
            {
                o.WriteLine("  -> " + grp.Key);
                Wrap(o, "     ", grp.Select(u => $"{u.Token} x{u.Count} :{u.FirstLine}"));
            }
            foreach (var u in f.NoOps) o.WriteLine($"  = {u.Token} x{u.Count} :{u.FirstLine}  (no-op) {u.Suggestion}");
            foreach (var u in f.Approximated) o.WriteLine($"  ~ {u.Token} x{u.Count} :{u.FirstLine}  (approximated) {u.Suggestion}");
        }
        // Classified by the parser itself (`v:flex` accepted), never by a name list: lg/md/max-md/min-[..] are supported, aria-*/data-*/peer-* are not.
        string[] state = ["dark", "hover", "focus", "focus-visible", "active", "disabled", "group-hover"];
        var support = a.Variants.Keys.ToDictionary(v => v, v => ClassAudit.IsSupported(v + ":flex"), StringComparer.Ordinal);
        string Fmt(IEnumerable<string> names) => string.Join(' ', names.Select(n => $"{n}={a.Variants[n]}"));
        var okNames = a.Variants.Keys.Where(v => support[v]).ToList();
        o.WriteLine("variants (state/theme): " + (okNames.Any(state.Contains) ? Fmt(state.Where(okNames.Contains)) : "none"));
        string bp = Fmt(okNames.Where(v => !state.Contains(v)).OrderByDescending(v => a.Variants[v]));
        if (bp.Length > 0) o.WriteLine("variants (breakpoints/other supported): " + bp);
        string bad = Fmt(a.Variants.Keys.Where(v => !support[v]).OrderByDescending(v => a.Variants[v]));
        if (bad.Length > 0) o.WriteLine("variants (UNSUPPORTED): " + bad);
    }

    static void Wrap(TextWriter o, string indent, IEnumerable<string> items)
    {
        var line = new StringBuilder(indent);
        foreach (string it in items)
        {
            if (line.Length > indent.Length && line.Length + it.Length > 150) { o.WriteLine(line.ToString().TrimEnd().TrimEnd(',')); line.Clear().Append(indent); }
            line.Append(it).Append(", ");
        }
        if (line.Length > indent.Length) o.WriteLine(line.ToString().TrimEnd().TrimEnd(','));
    }

    static void WriteJson(AuditResult a, TextWriter o)
    {
        using var ms = new MemoryStream();
        using (var j = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true }))
        {
            j.WriteStartObject();
            j.WriteNumber("tokens", a.TotalTokens); j.WriteNumber("uniqueTokens", a.UniqueTokens); j.WriteNumber("uniqueUnsupported", a.UniqueUnsupported);
            j.WriteStartObject("variants");
            foreach (var kv in a.Variants.OrderByDescending(k => k.Value)) j.WriteNumber(kv.Key, kv.Value);
            j.WriteEndObject();
            j.WriteStartArray("files");
            foreach (var f in a.Files)
            {
                j.WriteStartObject();
                j.WriteString("file", f.File); j.WriteNumber("strings", f.Strings); j.WriteNumber("tokens", f.Tokens);
                foreach (var (name, list) in new[] { ("unsupported", f.Unsupported), ("approximated", f.Approximated), ("noop", f.NoOps) })
                {
                    j.WriteStartArray(name);
                    foreach (var u in list)
                    {
                        j.WriteStartObject();
                        j.WriteString("token", u.Token); j.WriteNumber("count", u.Count); j.WriteNumber("line", u.FirstLine); j.WriteString("suggestion", u.Suggestion);
                        j.WriteEndObject();
                    }
                    j.WriteEndArray();
                }
                j.WriteEndObject();
            }
            j.WriteEndArray();
            j.WriteEndObject();
        }
        o.WriteLine(Encoding.UTF8.GetString(ms.ToArray()));
    }
}
