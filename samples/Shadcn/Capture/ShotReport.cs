using System.Globalization;
using System.Text;
using GotX;

namespace ShadcnShots;

/// <summary>One compared capture (README row).</summary>
public sealed record ShotRow(ShotTarget Target, PixelStats? Pixels, LayoutMatch? Layout);

/// <summary>Writes <c>screenshots/README.md</c>: index table, worst layout offenders per page, reference-only captures.</summary>
public static class ShotReport
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static void Write(string dir, List<ShotRow> rows, List<string> referenceOnly, List<StateRow> states)
    {
        var sb = new StringBuilder();
        sb.Append("""
            # shadcn/ui ↔ GotX screenshots

            Generated — do not edit by hand. Reference = ui.shadcn.com in headless Edge (see `manifest.json`); GotX = `samples/Shadcn`
            rendered headless with the CPU renderer and the Geist fonts from `samples/Shadcn/Assets/fonts`.

            - Re-capture references (network): `node tools/ShadcnRef/capture.mjs [--only home,login-03]`
            - Regenerate GotX + diff + this file: `dotnet run --project samples/Shadcn -- --screenshots [--only home]`
            - Compare only (existing `gotx/`): `dotnet run --project samples/Shadcn -- --compare`

            Home is captured full page **without the site header** on both sides (the GotX home view has no site header; the
            reference hides `header.sticky`). Blocks: viewport only. Folders: `reference/`, `gotx/`, `diff/` (dimmed reference;
            magenta = channel Δ > 24, amber = 1–24, blue = area only one image has), `compare/` (reference | GotX, diff below; halved when wide).

            **Layout match**: reference elements paired with GotX nodes by normalized text (case-insensitive). *Texts*: DOM text runs
            (Range rect = union of the rendered line boxes) ↔ GotX text nodes (union of the painted lines: wrapped exactly like the
            painter — case, tracking, kerning, text-balance, line-clamp — each line placed by text-align; x = leftmost line,
            w = widest line, y/h = first to last line box), Δ = max(|Δx|, |Δ vertical centre|, |Δw|) (line-box heights differ
            by definition: Range = font ascent+descent, GotX = line-height).
            *Boxes*: `data-slot` elements ↔ GotX nodes with background/border/interaction and the same subtree text,
            Δ = max(|Δx|, |Δy|, |Δw|, |Δh|). Δ in CSS px; median / p90 / max over all matched pairs, then texts and boxes separately.
            *Residual*: Δ of every pair inside a matched container (reference `data-slot` card / sidebar / sidebar-inset; innermost
            wins) after subtracting the container's own Δx/Δy — drift inside the card, without the offset the card inherited.
            *Jumps*: elements whose Δy (text: vertical centre, box: top) differs by > 2 px from the nearest matched element above
            them (horizontal overlap) or from the matched box enclosing them, whichever edge is nearer — where each vertical offset is introduced; fix them top-down.

            | Page | Viewport | Theme | Size ref → GotX | Pixel diff (tol 0 / 24) | Texts matched | Boxes matched | ≤2 px | Median Δ | P90 Δ | Max Δ | Text med / P90 | Box med / P90 | Residual med / P90 (n) | Jumps | Images |
            |---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|

            """);
        foreach (var r in rows)
        {
            var t = r.Target;
            var p = r.Pixels;
            var l = r.Layout;
            string size = p is null ? "–" : $"{p.RefWidth}×{p.RefHeight} → {p.GotXWidth}×{p.GotXHeight}" + (p.SizeMismatch ? " ⚠" : "");
            string pix = p is null ? "no reference" : $"{Pct(p.Diff0)} / {Pct(p.Diff24)}";
            string layout = l is null ? "| – | – | – | – | – | – | – | – | – | – "
                : $"| {l.MatchedTexts}/{l.RefTexts} | {l.MatchedBoxes}/{l.RefBoxes} | {l.Within2}/{l.Pairs.Count} | {F(l.Median)} | {F(l.P90)} | {F(l.Max)} "
                  + $"| {F(l.Texts.Median)} / {F(l.Texts.P90)} | {F(l.Boxes.Median)} / {F(l.Boxes.P90)} "
                  + $"| {F(l.Analysis.ResidualStats.Median)} / {F(l.Analysis.ResidualStats.P90)} ({l.Analysis.ResidualStats.Count}) | {l.Analysis.Jumps.Count} ";
            string links = $"[ref](reference/{t.Name}.png) · [gotx](gotx/{t.Name}.png)"
                + (p is null ? "" : $" · [diff](diff/{t.Name}.png) · [compare](compare/{t.Name}.png)");
            sb.Append(CultureInfo.InvariantCulture, $"| {t.Page.Id} | {t.Width}×{t.Height} | {t.Theme} | {size} | {pix} {layout}| {links} |\n");
        }

        sb.Append("\n## Worst layout offenders\n\nLight theme (layout is theme-independent). Rects are `x,y w×h`; Δ = GotX − reference.\n");
        foreach (var r in rows)
        {
            if (r.Target.Dark || r.Layout is not { Pairs.Count: > 0 } l) continue;
            sb.Append(CultureInfo.InvariantCulture, $"\n### {r.Target.Page.Id} {r.Target.Width}×{r.Target.Height}\n\n");
            sb.Append("| Kind | Text | Reference | GotX | Δx | Δy | Δw | Δh |\n|---|---|---|---|---|---|---|---|\n");
            foreach (var pair in l.Worst(15))
            {
                var a = pair.IsText ? pair.Ref.TextRect : pair.Ref.Rect;
                var b = pair.IsText ? pair.GotX.TextRect : pair.GotX.Rect;
                string kind = pair.IsText ? "text" : "box " + (pair.Ref.Slot ?? pair.Ref.Tag);
                sb.Append(CultureInfo.InvariantCulture,
                    $"| {kind} | {Esc(pair.Ref.Text)} | {R(a)} | {R(b)} | {F(pair.Dx)} | {F(pair.Dy)} | {F(pair.Dw)} | {F(pair.Dh)} |\n");
            }
            LayoutReport.Append(sb, l);
        }

        StateReport.Append(sb, states);

        if (referenceOnly.Count > 0)
        {
            sb.Append("\n## Reference only (page not ported yet)\n\n");
            foreach (var name in referenceOnly) sb.Append(CultureInfo.InvariantCulture, $"- [{name}](reference/{name}.png)\n");
        }
        File.WriteAllText(Path.Combine(dir, "README.md"), sb.ToString().Replace("\r\n", "\n"));
    }

    static string Pct(double v) => (v * 100).ToString("0.00", Inv) + " %";
    internal static string F(float v) => v.ToString("0.#", Inv);
    internal static string R(Bounds b) => $"{F(b.X)},{F(b.Y)} {F(b.Width)}×{F(b.Height)}";
    internal static string Esc(string? s) => (s ?? "").Replace("|", "\\|");
}
