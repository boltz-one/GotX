using GotX;
using GotX.Port;
using GotX.Raster;
using GotX.Test;

static partial class PortToolTests
{
    static GpuiReport Gpui() => GpuiAnalyzer.Analyze(File.ReadAllText(Path.Combine(Fixtures(), "counter.rs")));

    static byte[] Solid(int w, int h, byte v)
    {
        var px = new byte[w * h * 4];
        for (int i = 0; i < w * h; i++) { px[i * 4] = v; px[i * 4 + 1] = v; px[i * 4 + 2] = v; px[i * 4 + 3] = 255; }
        return px;
    }

    static void Paint(byte[] px, int w, int x0, int y0, int size, int delta)
    {
        for (int y = y0; y < y0 + size; y++)
            for (int x = x0; x < x0 + size; x++) px[(y * w + x) * 4] = (byte)(px[(y * w + x) * 4] + delta);
    }

    static Canvas ToCanvas(byte[] rgba, int w, int h)
    {
        var c = new Canvas();
        c.Resize(w, h);
        for (int i = 0; i < w * h; i++) c.Pixels[i] = 0xFF000000u | ((uint)rgba[i * 4] << 16) | ((uint)rgba[i * 4 + 1] << 8) | rgba[i * 4 + 2];
        return c;
    }

    static void RegisterGpuiAndDiff(TestSuite suite)
    {
        suite.Add("port/gpui/chain-translation", t =>
        {
            var chain = Gpui().Chains.First(c => c.Root.StartsWith("h_flex"));
            t.Assert.Equal(chain.Classes,
                "flex flex-row items-center size-full gap-0.5 px-4 w-[320px] min-w-[2rem] w-1/2 rounded-lg border-t border-[#ff0000] bg-popover text-[#ff0000]/50 font-semibold overflow-x-auto hover:bg-[#333333]",
                "h_flex implied classes + method chain -> Tailwind");
            t.Assert.Equal(Join(chain.Conditional.Select(c => $"{c.Cond}: {c.Classes}")), "self.active: opacity-50 flex-col", "when(cond, |this| ..) -> conditional classes");
            t.Assert.True(chain.Hints.Any(h => h.StartsWith(".id(\"row\"): .Key(\"row\")")), "id -> Key");
            t.Assert.True(chain.Hints.Any(h => h.Contains(".OnClick(state")) && chain.Hints.Any(h => h.Contains(".OnKey(")), "on_click / on_key_down hints");
            t.Assert.True(chain.Untranslated.Contains("z_index"), "unknown style-ish call is listed, not dropped");
        });

        suite.Add("port/gpui/unsupported-and-file-hints", t =>
        {
            var rep = Gpui();
            var child = rep.Chains.First(c => c.Root.StartsWith("div()") && c.Classes == "mx-auto");
            t.Assert.True(child.Unsupported.Any(u => u.StartsWith("mx-auto -> ")), "mx_auto -> unsupported with a suggestion");
            t.Assert.True(rep.Chains.Any(c => c.Hints.Any(h => h.Contains("no virtualised lists"))), "uniform_list note");
            t.Assert.True(rep.Globals.Any(g => g.Hint.StartsWith("impl Render for X")) && rep.Globals.Any(g => g.Hint.Contains("FocusHandle")), "file-level hints (impl Render, FocusHandle)");
            t.Assert.Equal(GpuiValues.Color("rgb(0x1e1e1e)").Token, "[#1e1e1e]", "rgb hex");
            t.Assert.Equal(GpuiValues.Color("rgba(0xffffff80)").Token, "[#ffffff]/50", "rgba alpha");
            t.Assert.Equal(GpuiValues.Length("px(12.5)"), "12.5px", "px length");
            t.Assert.Equal(GpuiValues.Length("rems(2.)"), "2rem", "rems length");
            t.Assert.Equal(GpuiValues.Length("self.width"), null, "runtime value is dynamic");
            // A class is reported unsupported iff the parser rejects it.
            foreach (var c in rep.Chains)
                foreach (string token in c.Classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    t.Assert.True(ClassAudit.IsSupported(token) == !c.Unsupported.Any(u => u.StartsWith(token + " ")), $"{token}: reported iff the parser rejects it");
        });

        suite.Add("port/diff/numbers-and-regions", t =>
        {
            const int w = 40, h = 20;
            var a = Solid(w, h, 200);
            var b = Solid(w, h, 200);
            Paint(b, w, 4, 4, 4, -100);   // 16 px, delta 100: tol0 and tol24
            Paint(b, w, 30, 10, 2, 10);  // 4 px, delta 10: tol0 only
            var rep = DiffCommand.Compare(a, w, h, b, w, h, out var image);
            t.Assert.True(rep.SameSize, "same size");
            t.Assert.Equal(rep.Diff0, 20, "pixels differing at tolerance 0");
            t.Assert.Equal(rep.Diff24, 16, "pixels differing at tolerance 24");
            t.Assert.Equal(rep.MaxDelta, 100, "max channel delta");
            t.Assert.Near((float)rep.Pct0, 100f * 20 / (w * h), 0.001f, "percent at tol0");
            t.Assert.Equal(rep.Regions.Count, 1, "one region above tolerance 24");
            t.Assert.True(rep.Regions[0].Area == new Bounds(4, 4, 4, 4) && rep.Regions[0].Pixels == 16, $"region box ({rep.Regions[0].Area})");
            var magenta = Pixels.Get(image, 5, 5);
            t.Assert.True(magenta.R > 0.9f && magenta.G < 0.1f && magenta.B > 0.7f, "magenta where delta > 24");
            var amber = Pixels.Get(image, 30, 10);
            t.Assert.True(amber.R > 0.9f && amber.G > 0.6f && amber.B < 0.1f, "amber where delta 1..24");

            var wide = DiffCommand.Compare(a, w, h, Solid(w + 8, h, 200), w + 8, h, out var wideImage);
            t.Assert.True(!wide.SameSize && wide.Diff0 == 0 && wideImage.Width == w + 8, "size mismatch: common corner compared, image has the larger size");
            t.Assert.True(Pixels.Get(wideImage, w + 2, 3).B > 0.9f, "area only in one image is blue");

            // End to end through the command: PNG files in, text + PNG out.
            string dir = Path.Combine(t.Options.OutDir ?? Path.GetTempPath(), "port-diff");
            Directory.CreateDirectory(dir);
            string pa = Path.Combine(dir, "a.png"), pb = Path.Combine(dir, "b.png"), po = Path.Combine(dir, "diff.png");
            PngWriter.Write(pa, ToCanvas(a, w, h));
            PngWriter.Write(pb, ToCanvas(b, w, h));
            var sw = new StringWriter();
            DiffCommand.Run([pa, pb, po], sw);
            string text = sw.ToString();
            t.Assert.True(text.Contains("same size") && text.Contains("2.50% @tol0 (20 px)") && text.Contains("2.00% @tol24 (16 px)") && text.Contains("1. [4,4 4x4] 16 px"), $"command output: {text.Trim()}");
            t.Assert.True(File.Exists(po), "diff image written");
        });
    }
}
