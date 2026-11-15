using System.Text;
using System.Text.Json;

namespace GotX.Port;

/// <summary><c>gpui &lt;file.rs|--sample&gt; [--json]</c>: GPUI style chains to Tailwind class strings plus behaviour hints.</summary>
public static class GpuiCommand
{
    public static int Run(string[] argv, TextWriter o)
    {
        var args = argv.ToList();
        bool json = PortApp.Flag(args, "--json");
        bool sample = PortApp.Flag(args, "--sample");
        if (PortApp.Flag(args, "--sample-source")) { o.Write(GpuiSample.Source); return 0; }
        if (!sample && args.Count != 1) { o.WriteLine("usage: gpui <file.rs> [--json] | gpui --sample | gpui --sample-source"); return 0; }
        string src = sample ? GpuiSample.Source : File.ReadAllText(args[0]);
        var report = GpuiAnalyzer.Analyze(src);
        if (json) WriteJson(report, o); else WriteText(report, sample ? "<sample>" : args[0], o);
        return 0;
    }

    public static void WriteText(GpuiReport rep, string file, TextWriter o)
    {
        int translated = rep.Chains.Count(c => c.Classes.Length > 0 || c.Conditional.Count > 0);
        o.WriteLine($"gpui: {file}: {rep.Chains.Count} chains ({translated} with classes), {rep.Globals.Count} file-level hints");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in rep.Chains)
        {
            o.WriteLine($"line {c.Line}: {c.Root} -> \"{c.Classes}\"");
            foreach (var (cond, cl) in c.Conditional) o.WriteLine($"    when {cond}: \"{cl}\"");
            foreach (string u in c.Unsupported)
            {
                string tok = u[..u.IndexOf(' ')];
                o.WriteLine(seen.Add(tok) ? $"    UNSUPPORTED {u}" : $"    UNSUPPORTED {tok} (see above)");
            }
            foreach (string d in c.Dynamic) o.WriteLine("    dynamic: " + d);
            foreach (string n in c.Notes.Distinct()) o.WriteLine("    note: " + n);
            // Same method repeated in one chain (13x on_action...): show the first, count the rest.
            foreach (var grp in c.Hints.GroupBy(h => h[..Math.Max(1, h.IndexOf('('))]))
                o.WriteLine("    hint: " + grp.First() + (grp.Count() > 1 ? $"  [x{grp.Count()} in this chain]" : ""));
            if (c.Untranslated.Count > 0) o.WriteLine("    untranslated calls: " + string.Join(", ", c.Untranslated));
        }
        foreach (var (line, count, hint) in rep.Globals)
            o.WriteLine($"line {line}: hint{(count > 1 ? $" (x{count})" : "")}: {hint}");
        o.WriteLine("note: GPUI rounded_* px (verified in gpui_macros styles.rs: xs 2, sm 4, md 6, lg 8, xl 12, 2xl 16, 3xl 24) differs from the GotX shadcn theme (sm 6, md 8, lg 10, xl 14): use rounded-[Npx] where exactness matters");
        o.WriteLine("note: GPUI default line height is 1.618 x font size (TextStyle default phi(), verified style.rs); text_* only set the size. Tailwind text-sm leading is 20px: add leading-[1.618] to match line boxes");
    }

    static void WriteJson(GpuiReport rep, TextWriter o)
    {
        using var ms = new MemoryStream();
        using (var j = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true }))
        {
            j.WriteStartObject();
            j.WriteStartArray("chains");
            foreach (var c in rep.Chains)
            {
                j.WriteStartObject();
                j.WriteNumber("line", c.Line); j.WriteString("root", c.Root); j.WriteString("classes", c.Classes);
                Arr(j, "conditional", c.Conditional.Select(x => $"{x.Cond}: {x.Classes}"));
                Arr(j, "unsupported", c.Unsupported); Arr(j, "dynamic", c.Dynamic); Arr(j, "notes", c.Notes.Distinct());
                Arr(j, "hints", c.Hints); Arr(j, "untranslated", c.Untranslated);
                j.WriteEndObject();
            }
            j.WriteEndArray();
            j.WriteStartArray("fileHints");
            foreach (var (line, count, hint) in rep.Globals)
            {
                j.WriteStartObject();
                j.WriteNumber("line", line); j.WriteNumber("count", count); j.WriteString("hint", hint);
                j.WriteEndObject();
            }
            j.WriteEndArray();
            j.WriteEndObject();
        }
        o.WriteLine(Encoding.UTF8.GetString(ms.ToArray()));
    }

    static void Arr(Utf8JsonWriter j, string name, IEnumerable<string> items)
    {
        j.WriteStartArray(name);
        foreach (string s in items) j.WriteStringValue(s);
        j.WriteEndArray();
    }
}
