using System.Diagnostics;
using GotX;
using GotX.Port;
using GotX.Test;

/// <summary>Bổ sung: bảng class trong C#, phân loại variant, no-op / xấp xỉ, diff lệch kích thước, compare-layout.mjs (cần node).</summary>
static partial class PortToolTests
{
    static string Text(Action<StringWriter> run) { var sw = new StringWriter(); run(sw); return sw.ToString(); }

    static string? FindNode()
    {
        string[] candidates = [@"C:\Program Files\nodejs\node.exe", "/usr/bin/node", "/usr/local/bin/node"];
        return candidates.FirstOrDefault(File.Exists);
    }

    static void RegisterExtra(TestSuite suite)
    {
        suite.Add("port/classes/extract-cs-const-tables", t =>
        {
            string cs = """
                static class Sheet
                {
                    const string Card = "flex w-[360px] flex-col gap-4 rounded-xl border border-border bg-card p-6";
                    public static readonly string[] Sizes = ["h-8 px-3 text-sm", "h-9 px-4", "h-10 px-6"];
                    static string Muted => "text-sm text-muted-foreground";
                    const string Title = "Create project";
                    static readonly string Greeting = "Hello there, welcome back";
                    const string Interp = $"size-4 {Title}";
                    void Build(Window w) => w.Div().Class(Card).Child(w.Div().Class("p-2 hover:bg-accent"));
                }
                """;
            var parts = Strings("Sheet.cs", cs);
            string got = Join(parts);
            foreach (string expected in new[] { "flex w-[360px] flex-col gap-4 rounded-xl border border-border bg-card p-6", "h-8 px-3 text-sm", "h-9 px-4", "h-10 px-6", "text-sm text-muted-foreground", "p-2 hover:bg-accent" })
                t.Assert.True(parts.Contains(expected), $"found '{expected}' ({got})");
            t.Assert.True(!parts.Contains("Create project") && !parts.Contains("Hello there, welcome back"), "ordinary text constants are ignored (< 60 % recognised tokens)");
            t.Assert.Equal(parts.Count(p => p == "p-2 hover:bg-accent"), 1, ".Class(\"..\") literal counted once");
        });

        suite.Add("port/classes/noop-variants-approximations", t =>
        {
            var audit = ClassAudit.Analyze(ClassExtractor.Extract("a.html",
                "<div class=\"antialiased lg:flex md:items-baseline inline-flex transform will-change-transform aria-pressed:p-2 text-sm space-y-2 w-fit overflow-clip\"></div>"));
            var file = audit.Files.Single();
            t.Assert.Equal(Join(file.NoOps.Select(u => u.Token)), "antialiased | transform | will-change-transform", "valid no-ops are not unsupported");
            t.Assert.Equal(Join(file.Unsupported.Select(u => u.Token)), "aria-pressed:p-2", "only the real gap stays unsupported");
            t.Assert.Equal(Join(file.Approximated.Select(u => u.Token).Order(StringComparer.Ordinal)), "inline-flex | overflow-clip | space-y-2", "approximated tokens");
            t.Assert.Equal(audit.UniqueUnsupported, 1, "no-ops are not counted");

            string text = Text(sw => ClassesCommand.Run([Write(t, "a.html", "<div class=\"lg:flex md:p-2 dark:bg-card hover:p-1 aria-selected:p-2 peer-x:p-1\">")], sw));
            t.Assert.True(text.Contains("variants (breakpoints/other supported): ") && text.Contains("lg=1") && text.Contains("md=1"), $"lg/md are supported variants ({text})");
            string unsupportedLine = text.Split('\n').First(l => l.StartsWith("variants (UNSUPPORTED)"));
            t.Assert.True(!unsupportedLine.Contains("lg=") && unsupportedLine.Contains("aria-selected=1") && unsupportedLine.Contains("peer-x=1"), $"unsupported variants listed separately ({unsupportedLine})");

            // Every approximation entry is something the parser really accepts; what the parser rejects must not be filed as approximated.
            foreach (string ok in new[] { "inline", "inline-block", "inline-flex", "inline-grid", "space-x-2", "space-y-2", "h-min", "min-w-max", "max-w-fit", "overflow-clip", "overflow-scroll", "text-pretty" })
            {
                t.Assert.True(ClassAudit.IsSupported(ok), $"{ok}: parser accepts");
                t.Assert.True(Workarounds.Approximation(ok) is not null, $"{ok}: listed as approximated");
            }
            foreach (string exact in new[] { "items-baseline", "self-baseline", "w-fit" })
                t.Assert.True(ClassAudit.IsSupported(exact) && Workarounds.Approximation(exact) is null, $"{exact}: real baseline alignment, supported and not approximated");
            foreach (string bad in new[] { "backdrop-blur", "backdrop-blur-sm" })
                t.Assert.True(!ClassAudit.IsSupported(bad) && Workarounds.Approximation(bad) is null, $"{bad}: rejected by the parser (so unsupported, not approximated)");
            foreach (string noop in new[] { "antialiased", "subpixel-antialiased", "transform", "transform-gpu", "will-change-scroll" })
                t.Assert.True(!ClassAudit.IsSupported(noop) && Workarounds.NoOp(noop) is not null, $"{noop}: parser rejects, table calls it a no-op");
        });

        suite.Add("port/diff/size-mismatch-wording", t =>
        {
            const int w = 30, h = 10;
            string dir = Path.Combine(t.Options.OutDir ?? Path.GetTempPath(), "port-diff2");
            Directory.CreateDirectory(dir);
            string a = Path.Combine(dir, "a.png"), b = Path.Combine(dir, "b.png");
            GotX.Raster.PngWriter.Write(a, ToCanvas(Solid(w, h, 100), w, h));
            GotX.Raster.PngWriter.Write(b, ToCanvas(Solid(w + 6, h + 4, 100), w + 6, h + 4));
            string text = Text(sw => DiffCommand.Run([a, b], sw));
            t.Assert.True(text.Contains("SIZE MISMATCH (gotx is +6 wide, +4 tall)"), $"size mismatch reported on its own line ({text.Trim()})");
            t.Assert.True(text.Contains("compared: ONLY the overlapping top-left 30x10 corner") && text.Contains("NOT in the percentages"), "overlap-only statement");
            t.Assert.True(text.Contains("differing (overlap only): 0.00% @tol0 (0 px)"), "percentages cover the overlap only");
        });

        suite.Add("port/layout/compare-text-by-alignment", t =>
        {
            string? node = FindNode();
            if (node is null) { t.Notes.Add("node not found: compare-layout.mjs test skipped"); return; }
            string dir = Path.Combine(t.Options.OutDir ?? Path.GetTempPath(), "port-layout");
            Directory.CreateDirectory(dir);
            // Browser: 600px container, three 20px lines; glyph runs 100px wide placed left / right / centre.
            File.WriteAllText(Path.Combine(dir, "ref.json"), """
                {"width":600,"height":60,"elements":[
                 {"tag":"div","own":true,"text":"Left text","rect":{"x":0,"y":0,"w":600,"h":20},"textRect":{"x":0,"y":0,"w":100,"h":20},"style":{"lineHeight":"20px","fontSize":"14px","textAlign":"left"}},
                 {"tag":"div","own":true,"text":"Right text","rect":{"x":0,"y":20,"w":600,"h":20},"textRect":{"x":500,"y":20,"w":100,"h":20},"style":{"lineHeight":"20px","fontSize":"14px","textAlign":"right"}},
                 {"tag":"div","own":true,"text":"Centre text","rect":{"x":0,"y":40,"w":600,"h":20},"textRect":{"x":250,"y":40,"w":100,"h":20},"style":{"lineHeight":"20px","fontSize":"14px","textAlign":"center"}}]}
                """);
            // GotX tree dump: text nodes carry the container box (full width).
            File.WriteAllText(Path.Combine(dir, "gotx.json"), """
                {"viewport":[600,60],"nodes":[
                 {"index":0,"parent":-1,"kind":"div","screen":[0,0,600,60]},
                 {"index":1,"parent":0,"kind":"text","text":"Left text","screen":[0,0,600,20]},
                 {"index":2,"parent":0,"kind":"text","text":"Right text","screen":[0,20,600,20]},
                 {"index":3,"parent":0,"kind":"text","text":"Centre text","screen":[0,40,600,20]}]}
                """);
            string script = Path.GetFullPath(Path.Combine(Fixtures(), "..", "..", "..", "..", ".claude", "skills", "gotx-port", "scripts", "compare-layout.mjs"));
            string output = RunNode(node, script, Path.Combine(dir, "ref.json"), Path.Combine(dir, "gotx.json"));
            t.Assert.True(output.Contains("matched 3: 3/3 texts") && output.Contains("max 0  within 2px: 3/3"), $"left / right / centre text in a stretched container all read delta 0 ({output.Trim()})");
            string pos = RunNode(node, script, Path.Combine(dir, "ref.json"), Path.Combine(dir, "gotx.json"), "--position-only");
            t.Assert.True(pos.Contains("[position only]"), "--position-only accepted");
        });
    }

    static string Write(UiTest t, string name, string content)
    {
        string dir = Path.Combine(t.Options.OutDir ?? Path.GetTempPath(), "port-input");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    static string RunNode(string node, params string[] args)
    {
        var psi = new ProcessStartInfo(node) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (string a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi)!;
        string o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit();
        return o;
    }
}
