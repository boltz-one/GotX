using System.Globalization;
using System.Text;

namespace ShadcnShots;

/// <summary>"States" section of screenshots/README.md: how states are captured, how to add scenarios, the result table.</summary>
public static class StateReport
{
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static void Append(StringBuilder sb, List<StateRow> rows)
    {
        sb.Append("""

            ## States

            Interaction states (hover, keyboard focus, press, click/open, type) from `tools/ShadcnRef/scenarios.json`, one clipped
            region per state: the target box(es) and every floating layer that opened (menu, select list, tooltip), inflated by
            24 px — or only the modal content when a dialog opened. Images live in `states/{reference,gotx,diff,compare}/<page>-<state>-<theme>.png`
            (+ region layout maps `.json`). The region is computed on the reference; GotX is cropped with the SAME rect,
            translated by the target's offset — median GotX − reference Δ of the matched elements inside the target (its label),
            else the target / modal box when both are about the same size, else the text runs matched around it — rounded to whole px, 0 when ≤ 1 px per axis — so inherited page offsets and sub-pixel
            layout snapping do not shift the crops; what remains is the control's own look. When GotX's own targets/overlays reach
            > 2 px beyond that rect the row notes "GotX content extends beyond the reference frame". Both sides use real input: Edge via CDP `Input.dispatchMouseEvent` / `dispatchKeyEvent`
            (Tab presses, never `element.focus()`, so `:focus-visible` is real) / `insertText`; GotX via `Window.DispatchInput`
            (mouse move / down / up, `Key.Tab`, text input) on a virtual clock. Transitions: disabled by CSS on the reference
            (end state immediately), run to their end on GotX; after each step both wait 1.2 s (hover: tooltip delay) or 0.4 s.
            The reference viewport is the full page height so nothing scrolls; GotX uses the same window size.

            - Capture references: `node tools/ShadcnRef/states.mjs [--only home] [--state get-started-hover] [--theme light]`
            - GotX + comparison: part of `--screenshots` (`--state a,b` filters, `--no-states` skips)

            **Adding scenarios (e.g. a block):** add a `pages[]` entry to `scenarios.json` with `page` (the `SitePage` id), `url`
            (`https://ui.shadcn.com/view/new-york-v4/<id>`), `hideHeader: false`, `width`, `height` (the block viewport, e.g.
            1440 × 900), `themes`, a `targets` map and `states`. A target has a reference side `ref: { selector, text? | prefix?, index? }`
            (CSS selector, preferably `[data-slot=…]`; `text` matches the element's innerText, else aria-label, else placeholder,
            case-insensitive) and a GotX side `gotx: { key } | { key, pick: n } | { key, next: n } | { text, index? }` (`pick` = n-th
            clickable/focusable node inside the keyed node, `next` = n-th one after it, `text` = text node → nearest
            clickable/focusable ancestor). States: `{ "target": "t", "do": ["hover", "focus-key", "press", "click"] }` makes
            `t-hover` …; multi-step: `{ "name": "n", "steps": [{ "target": "a", "do": "click" }, { "target": "b", "do": "hover" }] }`;
            typing: `{ "target": "t", "do": "type", "text": "…" }`. Then run states.mjs for the page and `--screenshots`; the
            fidelity test picks the new states up automatically (first run records their baseline).

            """);
        if (rows.Count == 0) { sb.Append("No GotX states captured.\n"); return; }
        sb.Append("| State | Theme | Size ref → GotX | Pixel diff (tol 0 / 24) | Overlays ref / GotX | Layout matched (≤2 px) | Median Δ | Notes | Images |\n");
        sb.Append("|---|---|---|---|---|---|---|---|---|\n");
        foreach (var r in rows)
        {
            var p = r.Pixels;
            var l = r.Layout;
            string size = p is null ? "–" : $"{p.RefWidth}×{p.RefHeight} → {p.GotXWidth}×{p.GotXHeight}";
            string pix = p is null ? "–" : $"{Pct(p.Diff0)} / {Pct(p.Diff24)}";
            string lay = l is null ? "– | –" : $"{l.Pairs.Count} ({l.Within2}) | {l.Median.ToString("0.#", Inv)}";
            var notes = new List<string>();
            if (r.Note is not null) notes.Add(r.Note.Replace("|", "\\|"));
            if (r.RefOverlays != r.GotXOverlays) notes.Add("overlay count differs");
            if (p is { SizeMismatch: true }) notes.Add("region size differs");
            string n = r.Name;
            string links = $"[ref](states/reference/{n}.png) · [gotx](states/gotx/{n}.png)" + (p is null ? "" : $" · [compare](states/compare/{n}.png)");
            sb.Append(Inv, $"| {r.Scenario.Page} / {r.Scenario.Name} | {r.Theme} | {size} | {pix} | {r.RefOverlays} / {r.GotXOverlays} | {lay} | {string.Join("; ", notes)} | {links} |\n");
        }
    }

    static string Pct(double v) => (v * 100).ToString("0.0", Inv) + " %";
}
