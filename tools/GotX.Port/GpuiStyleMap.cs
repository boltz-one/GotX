using System.Text.RegularExpressions;

namespace GotX.Port;

/// <summary>Accumulated translation of one chain (or one closure body).</summary>
public sealed class StyleResult
{
    public List<string> Classes { get; } = [];
    public List<(string Cond, List<string> Classes)> Conditional { get; } = [];
    public List<string> Dynamic { get; } = [];
    public List<string> Notes { get; } = [];
    public List<string> Hints { get; } = [];
    public bool Any => Classes.Count + Conditional.Count + Dynamic.Count > 0;
}

/// <summary>
/// GPUI style method name -> Tailwind token. Method names checked against zed gpui/styled.rs and gpui_macros/styles.rs
/// (fetched); the exact-name table, state-variant wrappers and value mapping beyond that are from memory of GPUI usage.
/// </summary>
public static partial class GpuiStyleMap
{
    static readonly Dictionary<string, string> Exact = new()
    {
        ["flex"] = "flex", ["grid"] = "grid", ["block"] = "block", ["hidden"] = "hidden", ["visible"] = "visible", ["invisible"] = "invisible",
        ["flex_col"] = "flex-col", ["flex_row"] = "flex-row", ["flex_col_reverse"] = "flex-col-reverse", ["flex_row_reverse"] = "flex-row-reverse",
        ["flex_wrap"] = "flex-wrap", ["flex_nowrap"] = "flex-nowrap", ["flex_wrap_reverse"] = "flex-wrap-reverse",
        ["flex_1"] = "flex-1", ["flex_auto"] = "flex-auto", ["flex_initial"] = "flex-initial", ["flex_none"] = "flex-none",
        ["flex_grow"] = "grow", ["flex_grow_0"] = "grow-0", ["flex_grow_1"] = "grow", ["flex_shrink"] = "shrink", ["flex_shrink_0"] = "shrink-0", ["flex_shrink_1"] = "shrink",
        ["relative"] = "relative", ["absolute"] = "absolute",
        ["overflow_hidden"] = "overflow-hidden", ["overflow_x_hidden"] = "overflow-x-hidden", ["overflow_y_hidden"] = "overflow-y-hidden",
        ["overflow_scroll"] = "overflow-auto", ["overflow_x_scroll"] = "overflow-x-auto", ["overflow_y_scroll"] = "overflow-y-auto",
        ["whitespace_nowrap"] = "whitespace-nowrap", ["whitespace_normal"] = "whitespace-normal",
        ["text_ellipsis"] = "text-ellipsis", ["truncate"] = "truncate", ["text_left"] = "text-left", ["text_center"] = "text-center", ["text_right"] = "text-right",
        ["text_xs"] = "text-xs", ["text_sm"] = "text-sm", ["text_base"] = "text-base", ["text_lg"] = "text-lg", ["text_xl"] = "text-xl", ["text_2xl"] = "text-2xl", ["text_3xl"] = "text-3xl",
        ["italic"] = "italic", ["not_italic"] = "not-italic", ["underline"] = "underline", ["line_through"] = "line-through", ["text_decoration_none"] = "no-underline",
        ["border_dashed"] = "border-dashed", ["aspect_square"] = "aspect-square", ["col_span_full"] = "col-span-full", ["row_span_full"] = "row-span-full",
        ["shadow_none"] = "shadow-none", ["shadow_2xs"] = "shadow-2xs", ["shadow_xs"] = "shadow-xs", ["shadow_sm"] = "shadow-sm", ["shadow_md"] = "shadow-md",
        ["shadow_lg"] = "shadow-lg", ["shadow_xl"] = "shadow-xl", ["shadow_2xl"] = "shadow-2xl",
    };

    static readonly Dictionary<string, string> BoxPrefix = new()
    {
        ["p"] = "p", ["px"] = "px", ["py"] = "py", ["pt"] = "pt", ["pb"] = "pb", ["pl"] = "pl", ["pr"] = "pr",
        ["m"] = "m", ["mx"] = "mx", ["my"] = "my", ["mt"] = "mt", ["mb"] = "mb", ["ml"] = "ml", ["mr"] = "mr",
        ["gap"] = "gap", ["gap_x"] = "gap-x", ["gap_y"] = "gap-y", ["w"] = "w", ["h"] = "h", ["size"] = "size",
        ["min_w"] = "min-w", ["min_h"] = "min-h", ["max_w"] = "max-w", ["max_h"] = "max-h",
        ["top"] = "top", ["bottom"] = "bottom", ["left"] = "left", ["right"] = "right", ["inset"] = "inset",
    };

    static readonly string[] Variants = ["hover", "active", "focus", "focus_visible", "in_focus", "group_hover", "group_active", "group_focus"];

    [GeneratedRegex(@"^(p|px|py|pt|pb|pl|pr|m|mx|my|mt|mb|ml|mr|gap_x|gap_y|gap|min_w|min_h|max_w|max_h|size|top|bottom|left|right|inset|w|h)_(neg_)?(.+)$")]
    private static partial Regex BoxRx();

    [GeneratedRegex(@"^border(?:_(t|b|l|r|x|y))?_(\d+)$")]
    private static partial Regex BorderRx();

    [GeneratedRegex(@"^rounded(?:_(t|b|l|r|tl|tr|bl|br))?_(none|xs|sm|md|lg|xl|2xl|3xl|full)$")]
    private static partial Regex RoundedRx();

    [GeneratedRegex(@"^(items|justify|self|content)_([a-z_]+)$")]
    private static partial Regex AlignRx();

    [GeneratedRegex(@"^cursor_(pointer|default|text|move|not_allowed|context_menu|crosshair|vertical_text|alias|copy|no_drop|grab|grabbing|ew_resize|ns_resize|nesw_resize|nwse_resize|col_resize|row_resize|n_resize|e_resize|s_resize|w_resize)$")]
    private static partial Regex CursorRx();

    public static bool IsStyleName(string name) =>
        Exact.ContainsKey(name) || AlignRx().IsMatch(name) || BoxRx().IsMatch(name) || BorderRx().IsMatch(name) || RoundedRx().IsMatch(name) || CursorRx().IsMatch(name)
        || BoxPrefix.ContainsKey(name) || Variants.Contains(name) || name is "bg" or "border_color" or "text_color" or "text_size" or "font_weight" or "opacity" or "line_height"
            or "grid_cols" or "grid_rows" or "col_span" or "row_span" or "col_start" or "col_end" or "row_start" or "row_end" or "line_clamp" or "font_family" or "flex_basis"
            or "text_decoration_color" or "text_bg" or "shadow" or "aspect_ratio" or "when" or "when_some" or "map" or "group" or "rounded" or "border" or "border_t" or "border_b" or "border_l" or "border_r" or "border_x" or "border_y"
            or "rounded_t" or "rounded_b" or "rounded_l" or "rounded_r" or "rounded_tl" or "rounded_tr" or "rounded_bl" or "rounded_br" or "scrollbar_width";

    /// <summary>Translates one call. <paramref name="g"/> gives access to argument tokens (closures for hover/when).</summary>
    public static bool Translate(GpuiChains g, Call c, string prefix, StyleResult r, HashSet<int> consumed)
    {
        string name = c.Name;
        string arg = c.HasArgs ? g.Text(c.ArgStart, c.ArgEnd) : "";
        void Add(string token) => r.Classes.Add(prefix + token);
        void Dyn(string why) => r.Dynamic.Add($"{name}({Clip(arg)}) -> {why}");
        bool Paint(string prop)
        {
            var col = GpuiValues.Color(arg);
            if (col.Note is not null) r.Notes.Add(col.Note);
            if (col.Token is null) { if (col.Note is null) Dyn($"{prop}-<colour> (runtime colour: map to a token or {prop}-[#hex])"); }
            else Add($"{prop}-{col.Token}");
            return true;
        }

        if (!c.HasArgs) return false; // field access (rounding.top_left), not a method call
        if (Exact.TryGetValue(name, out string? t)) { Add(t); return true; }
        if (AlignRx().Match(name) is { Success: true } am)
        {
            string v = am.Groups[2].Value.Replace("flex_", "").Replace('_', '-');
            Add($"{am.Groups[1].Value}-{v}");
            return true;
        }
        if (arg.Length == 0 && CursorRx().Match(name) is { Success: true } cm) { Add("cursor-" + cm.Groups[1].Value.Replace('_', '-')); return true; }
        if (BorderRx().Match(name) is { Success: true } bm)
        {
            string side = bm.Groups[1].Value, w = bm.Groups[2].Value;
            string b = "border" + (side.Length > 0 ? "-" + side : "");
            Add(w == "1" ? b : w is "0" or "2" or "4" or "8" ? $"{b}-{w}" : $"{b}-[{w}px]");
            return true;
        }
        if (RoundedRx().Match(name) is { Success: true } rm)
        {
            Add("rounded" + (rm.Groups[1].Value.Length > 0 ? "-" + rm.Groups[1].Value : "") + "-" + rm.Groups[2].Value);
            return true;
        }
        if (BoxRx().Match(name) is { Success: true } xm && arg.Length == 0 && ValidSuffix(xm.Groups[3].Value))
        {
            string pre = BoxPrefix[xm.Groups[1].Value], v = Suffix(xm.Groups[3].Value);
            bool neg = xm.Groups[2].Success;
            Add(neg ? $"-{pre}-{v}" : $"{pre}-{v}");
            return true;
        }
        if (BoxPrefix.TryGetValue(name, out string? bp) && arg.Length > 0)
        {
            string? len = GpuiValues.Length(arg);
            if (len is null) Dyn($"{bp}-[<expr>] (compute px in C#, e.g. $\"{bp}-[{{px}}px]\")");
            else Add(len == "auto" ? $"{bp}-auto" : $"{bp}-[{len}]");
            return true;
        }
        switch (name)
        {
            case "bg": return Paint("bg");
            case "text_color": return Paint("text");
            case "border_color": return Paint("border");
            case "text_decoration_color": return Paint("decoration");
            case "text_bg": r.Notes.Add($"text_bg({Clip(arg)}): text highlight background has no GotX utility"); return true;
            case "font_weight":
                if (Regex.Match(arg, @"(\w+)\s*$") is { Success: true } wm && GpuiValues.Weights.TryGetValue(wm.Groups[1].Value, out string? fw)) Add("font-" + fw);
                else Dyn("font-<weight>");
                return true;
            case "font_family":
                if (arg.Contains("mono", StringComparison.OrdinalIgnoreCase) || arg.Contains("code", StringComparison.OrdinalIgnoreCase)) Add("font-mono");
                else r.Notes.Add($"font_family({Clip(arg)}): GotX has font-sans / font-mono only (fonts are registered per TextSystem)");
                return true;
            case "opacity":
                if (GpuiValues.Number(arg) is { } o) Add($"opacity-{(int)Math.Round(o * 100)}"); else Dyn("opacity-<n>");
                return true;
            case "text_size":
                if (GpuiValues.Length(arg) is { } ts) Add($"text-[{ts}]"); else Dyn("text-[<len>]");
                return true;
            case "line_height":
                if (GpuiValues.Length(arg) is { } lh) Add(lh.EndsWith('%') ? $"leading-[{double.Parse(lh[..^1]) / 100}]" : $"leading-[{lh}]"); else Dyn("leading-[<len>]");
                return true;
            case "grid_cols" or "grid_rows" or "col_span" or "row_span" or "col_start" or "col_end" or "row_start" or "row_end" or "line_clamp":
                if (GpuiValues.Number(arg) is { } n)
                    Add(name switch { "grid_cols" => "grid-cols-", "grid_rows" => "grid-rows-", "col_span" => "col-span-", "row_span" => "row-span-", "col_start" => "col-start-", "col_end" => "col-end-", "row_start" => "row-start-", "row_end" => "row-end-", _ => "line-clamp-" } + (int)n);
                else Dyn(name.Replace('_', '-') + "-<n>");
                return true;
            case "flex_basis":
                if (GpuiValues.Length(arg) is { } fb) Add($"basis-[{fb}]"); else Dyn("basis-[<len>]");
                return true;
            case "aspect_ratio":
                if (GpuiValues.Number(arg) is { } ar) Add($"aspect-[{ar}]"); else Dyn("aspect-[<ratio>]");
                return true;
            case "shadow": r.Notes.Add($"shadow({Clip(arg)}): custom BoxShadow list -> shadow-sm/md/lg or shadow-[0_Ypx_Bpx_rgba(..)]"); return true;
            case "scrollbar_width": r.Notes.Add($"scrollbar_width({Clip(arg)}): scrollbar-w-<n> (GotX extension)"); return true;
            case "group": Add("group"); return true;
        }
        if (name.StartsWith("rounded") && c.HasArgs) { r.Notes.Add($"{name}({Clip(arg)}): rounded-[<px>]"); if (GpuiValues.Length(arg) is { } rl) Add($"{name.Replace('_', '-')}-[{rl}]"); return true; }
        if (name is "border" or "border_t" or "border_b" or "border_l" or "border_r" or "border_x" or "border_y" && c.HasArgs)
        {
            string side = name.Length > 6 ? "-" + name[7..] : "";
            if (GpuiValues.Length(arg) is { } bl) Add($"border{side}-[{bl}]"); else Dyn("border-[<len>]");
            return true;
        }
        // state variants and conditionals: translate the closure body with a prefix / condition
        if (Variants.Contains(name)) return GpuiScopes.Variant(g, c, prefix, r, consumed);
        if (name is "when" or "when_some" or "map") return GpuiScopes.When(g, c, prefix, r, consumed);
        return false;
    }

    static string Clip(string s) { s = Regex.Replace(s, @"\s+", " "); return s.Length > 50 ? s[..47] + "..." : s; }

    [GeneratedRegex(@"^(\d+(p5)?|\d+_\d+|px|full|auto|screen|min|max|fit)$")]
    private static partial Regex SuffixRx();

    static bool ValidSuffix(string s) => SuffixRx().IsMatch(s);

    static string Suffix(string s)
    {
        if (Regex.Match(s, @"^(\d+)p5$") is { Success: true } h) return h.Groups[1].Value + ".5";
        if (Regex.Match(s, @"^(\d+)_(\d+)$") is { Success: true } f) return f.Groups[1].Value + "/" + f.Groups[2].Value;
        return s;
    }
}
