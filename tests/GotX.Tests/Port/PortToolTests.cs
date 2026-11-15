using System.Runtime.CompilerServices;
using GotX;
using GotX.Port;
using GotX.Test;

/// <summary>
/// tools/GotX.Port: trích class theo từng định dạng, bảng gợi ý cho class GotX chưa hỗ trợ (đối chiếu với parser thật),
/// html → khung C# (so với file chuẩn trong fixtures/), gpui → class Tailwind, diff ảnh. Phần gpui/diff: PortToolTests.Gpui.cs.
/// </summary>
static partial class PortToolTests
{
    static string Fixtures([CallerFilePath] string source = "") => Path.Combine(Path.GetDirectoryName(source)!, "fixtures");

    static string[] Strings(string file, string text) => ClassExtractor.Extract(file, text).Select(s => s.Text.Trim()).ToArray();

    static string Join(IEnumerable<string> items) => string.Join(" | ", items);

    public static void Register(TestSuite suite)
    {
        RegisterGpuiAndDiff(suite);
        RegisterExtra(suite);

        suite.Add("port/classes/extract-html", t =>
        {
            var found = ClassExtractor.Extract("a.html", "<div class=\"flex gap-2 p-4\">\n  <span class='text-sm'>x</span><p class=unquoted>y</p>\n  <b id=\"q\" classy=\"nope\">z</b></div>");
            t.Assert.Equal(Join(found.Select(f => f.Text)), "flex gap-2 p-4 | text-sm", "class=\"..\" and class='..' (not classy=, not unquoted)");
            t.Assert.Equal(found[1].Line, 2, "line of the second class string");
            t.Assert.Equal(Join(Strings("b.html", "<i class=\"a &amp;&gt;b [&amp;_svg]:size-4\"></i>")), "a &>b [&_svg]:size-4", "entities decoded in HTML attributes");
        });

        suite.Add("port/classes/extract-jsx", t =>
        {
            string src = """
                export const B = cva("inline-flex items-center", {
                  variants: { variant: { default: "bg-primary text-primary-foreground", ghost: "hover:bg-accent" }, size: { sm: "h-8" } },
                  compoundVariants: [{ variant: "ghost", size: "sm", class: "px-2" }],
                  defaultVariants: { variant: "default", size: "sm" },
                });
                function X({ on }) {
                  return <div className="flex gap-2" aria-label="x">
                    <span className={cn("text-sm", on && "font-bold", `p-${on ? 1 : 2} rounded`)} />
                    <i className={'italic'} />
                  </div>;
                }
                """;
            var parts = Strings("a.tsx", src);
            string got = Join(parts);
            t.Assert.True(parts.Contains("inline-flex items-center") && parts.Contains("bg-primary text-primary-foreground") && parts.Contains("hover:bg-accent") && parts.Contains("h-8"), $"cva base + variants ({got})");
            t.Assert.True(parts.Contains("px-2"), "compoundVariants class: kept");
            t.Assert.True(!parts.Contains("default") && !parts.Contains("sm") && !parts.Contains("ghost"), "variant names (defaultVariants, compound selectors) are not classes");
            t.Assert.True(parts.Contains("flex gap-2") && parts.Contains("text-sm") && parts.Contains("font-bold") && parts.Contains("p-  rounded") && parts.Contains("italic"), $"className / cn() / template literal / {{'..'}} ({got})");
            t.Assert.True(!got.Contains("${") && !got.Contains("aria-label"), "dynamic fragments dropped, other attributes ignored");
        });

        suite.Add("port/classes/extract-cs-svelte-vue", t =>
        {
            string cs = "w.Div().Class(\"flex gap-2\").Child(w.Div().Class(\"p-2 \" + \"text-sm\")).Child(w.Div().Class($\"size-{n} rounded\").Key(\"k\"));";
            t.Assert.Equal(Join(Strings("v.cs", cs)), "flex gap-2 | p-2 | text-sm | size-  rounded", "C# .Class(..): literals, concatenation, interpolation holes");
            t.Assert.Equal(Join(Strings("a.svelte", "<div class=\"a {open ? 'b c' : 'd'} e\">x</div>")), "a   e | b c | d", "Svelte: literals inside {..} + static text");
            t.Assert.Equal(Join(Strings("a.vue", "<div :class=\"[on ? 'x y' : 'z', { 'w-4': big }]\" class=\"base\"></div>")), "x y | z | w-4 | base", "Vue :class literals + class");
        });

        suite.Add("port/classes/unsupported-and-suggestions", t =>
        {
            var audit = ClassAudit.Analyze(ClassExtractor.Extract("a.html",
                "<div class=\"flex mx-auto z-10 peer-hover:bg-red-500 hover:bg-primary dark:text-foreground group-hover:p-2 data-[state=open]:p-2 space-x-2 weird-marker\"></div>"));
            var file = audit.Files.Single();
            t.Assert.Equal(Join(file.Unsupported.Select(u => u.Token).Order(StringComparer.Ordinal)), "data-[state=open]:p-2 | mx-auto | peer-hover:bg-red-500 | weird-marker | z-10", "unsupported = exactly what the parser rejects");
            t.Assert.Equal(Join(file.Approximated.Select(u => u.Token)), "space-x-2", "accepted but approximated");
            foreach (var u in file.Unsupported.Where(u => u.Token != "weird-marker"))
                t.Assert.True(u.Suggestion != Workarounds.Generic, $"{u.Token}: has a specific suggestion ({u.Suggestion})");
            t.Assert.True(file.Unsupported.Single(u => u.Token == "weird-marker").Suggestion == Workarounds.Custom, "custom/marker class advice");
            t.Assert.True(audit.Variants.ContainsKey("dark") && audit.Variants.ContainsKey("hover") && audit.Variants["group-hover"] == 1, "variants counted");
        });

        // The suggestion table must never describe a class the parser accepts, and every rule's examples must be really unsupported.
        suite.Add("port/classes/table-matches-parser", t =>
        {
            foreach (var rule in Workarounds.VariantRules.Concat(Workarounds.UtilityRules))
                foreach (string ex in rule.Examples)
                {
                    t.Assert.True(!ClassAudit.IsSupported(ex), $"rule {rule.Name}: example '{ex}' is rejected by Tailwind.Parse");
                    t.Assert.Equal(Workarounds.Suggest(ex), rule.Advice, $"rule {rule.Name}: '{ex}' gets this rule's advice");
                }
            string[] supported = ["flex", "group", "group-hover:p-2", "hover:bg-primary", "dark:bg-card", "md:flex", "outline-none", "border-s", "text-start", "text-balance", "w-fit",
                "min-h-screen", "overflow-scroll", "scrollbar-none", "text-transparent", "space-y-2", "object-cover", "bg-radial", "focus-visible:ring-2", "max-[640px]:hidden"];
            foreach (string s in supported)
                t.Assert.True(ClassAudit.IsSupported(s), $"{s}: parser supports it, so it must never be reported");
            var audit = ClassAudit.Analyze([new ClassString("x", 1, string.Join(' ', supported))]);
            t.Assert.Equal(audit.Files.Single().Unsupported.Count, 0, "supported tokens produce no unsupported entries");
        });

        suite.Add("port/html/golden-skeleton", t =>
        {
            string actual = HtmlToCs.Generate(File.ReadAllText(Path.Combine(Fixtures(), "card.html")), "Fixture", "CardView").Replace("\r\n", "\n");
            string expectedPath = Path.Combine(Fixtures(), "card.expected.cs.txt");
            if (Environment.GetEnvironmentVariable("GOTX_PORT_UPDATE_GOLDEN") == "1")
            {
                File.WriteAllText(expectedPath, actual, new System.Text.UTF8Encoding(false));
                t.Notes.Add("golden rewritten: " + expectedPath);
                return;
            }
            string expected = File.ReadAllText(expectedPath).Replace("\r\n", "\n");
            if (actual != expected)
            {
                string tmp = Path.Combine(t.Options.OutDir ?? Path.GetTempPath(), "card.actual.cs.txt");
                File.WriteAllText(tmp, actual);
                t.Fail($"html skeleton differs from fixtures/card.expected.cs.txt (actual written to {tmp}; set GOTX_PORT_UPDATE_GOLDEN=1 to accept)");
            }
            t.Assert.True(actual.Contains("// UNSUPPORTED: mx-auto") && actual.Contains(".Key(\"root\")") && actual.Contains("VectorPath.Icon(\"M20 6 9 17l-5-5\", \"circle 12 12 3\")"), "key facts: unsupported comment, id as Key, svg shapes as Icon parts");
        });

        suite.Add("port/html/tolerant-parser", t =>
        {
            string cs = HtmlToCs.Generate("<p>a &amp; b &#65; &#x42; &bogus;<br>c<input disabled value=x><img src=a.png><p>two<unclosed class=\"flex\"><!-- c --><div>&lt;ok&gt;</div>", "N", "V");
            t.Assert.True(cs.Contains("a & b A B &bogus;") && cs.Contains("<ok>"), "entities (named, decimal, hex, unknown kept) decode");
            t.Assert.True(cs.Contains("class V : View") && cs.Contains("Render(Window w)"), "still produces a View with Render");
            t.Assert.True(HtmlToCs.Generate("", "N", "V").Contains("w.Div()"), "empty input yields a root Div");
        });
    }
}
