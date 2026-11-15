using System.Text.RegularExpressions;

namespace GotX.Port;

/// <summary>
/// Advice for Tailwind tokens GotX lacks. The table is ONLY consulted for tokens <c>Tailwind.Parse</c> already rejected, so it can
/// never claim a supported class is unsupported; each rule lists examples that the unit tests re-check against the live parser.
/// </summary>
public static partial class Workarounds
{
    public sealed record Rule(string Name, string Pattern, string Advice, params string[] Examples)
    {
        public Regex Rx { get; } = new(Pattern, RegexOptions.CultureInvariant);
    }

    public const string Generic = "no GotX equivalent known: use an arbitrary value ([..]) or add the utility (skill gotx-tailwind)";
    public const string Custom = "not a Tailwind utility (custom/marker class): drop it or translate its CSS by hand";

    // Matched against each variant prefix (text before ':'), first hit wins.
    public static readonly Rule[] VariantRules =
    [
        new("child-variant", @"^\*{1,2}$", "*: / **: child variants: put the classes directly on each child element in the builder", "*:p-2", "**:p-2"),
        new("peer", @"^peer(-|/|$)", "peer-*: no sibling state. Compute the state in C# and pick classes (_checked ? \"..\" : \"..\")", "peer-hover:bg-red-500", "peer-disabled:opacity-50"),
        new("group-other", @"^group-(?!hover$)|^group/", "only `group` + `group-hover:` exist. Use an OnHover handler + C# class condition for other group states / named groups", "group-focus:bg-red-500", "group-hover/item:p-2"),
        new("aria", @"^aria-", "aria-*: choose classes with a C# condition (_selected ? \"bg-accent\" : \"\")", "aria-selected:bg-red-500", "aria-[x]:p-2"),
        new("data", @"^(group-)?data-|^in-data-", "data-[..]: GotX has no data attributes; pick classes from the state in C# (_open ? \"..\" : \"..\")", "data-[state=open]:bg-red-500", "data-open:p-2"),
        new("has", @"^has-|^not-|^in-|^supports-", "has-/not-/in-/supports-: structural/feature conditions do not exist; decide in C# and apply classes directly", "has-[>svg]:p-2", "not-first:p-2", "supports-[x]:p-2"),
        new("container", @"^@", "container queries: GotX only has viewport breakpoints (sm: md: lg: ...). Use them or choose the class from the measured width", "@sm:p-2"),
        new("structural", @"^(first|last|odd|even|only)(-of-type|-child)?$|^(first|last)-(letter|line)$|^empty$", "first:/last:/odd:/even:/only: do not exist; pass the index into the builder loop and pick the class in C# (i == 0 ? \"..\" : \"..\")", "first:p-2", "last:p-2", "odd:p-2", "even:p-2", "only:p-2", "first-letter:uppercase"),
        new("pseudo-content", @"^(before|after|marker|placeholder|file|selection|backdrop)$", "pseudo-elements (before/after/marker/placeholder/file/selection) do not exist; render a real child (absolute Div / text), the Input placeholder prop, or drop", "before:content-[x]", "after:absolute", "marker:text-red-500", "placeholder:text-red-500", "file:p-2", "selection:bg-red-500", "backdrop:bg-red-500"),
        new("state-other", @"^(focus-within|open|checked|invalid|required|read-only|enabled|inert|target|visited|indeterminate|default|optional|valid|in-range|out-of-range|user-valid|user-invalid)$", "state variant missing (only hover active focus focus-visible disabled group-hover dark + breakpoints): hold the state in C# and pick classes", "focus-within:ring-2", "open:p-2", "checked:bg-red-500", "invalid:ring-red-500", "required:p-2", "read-only:p-2", "enabled:p-2"),
        new("media", @"^(motion-(reduce|safe)|print|landscape|portrait|rtl|ltr|contrast-(more|less)|forced-colors|pointer-(coarse|fine)|any-pointer-.*|noscript)$", "media/feature variant missing: drop it (print/rtl/motion) or decide in C#", "motion-reduce:transition-none", "motion-safe:animate-spin", "print:hidden", "landscape:p-2", "rtl:p-2"),
        new("arbitrary-variant", @"^\[", "arbitrary selector variant ([&_svg]:..): apply the classes directly on the child element in the builder", "[&_svg]:size-4", "[&>*]:p-2", "[&_p]:text-sm"),
    ];

    public static readonly Rule[] UtilityRules =
    [
        new("icon-marker", @"^(lucide(-.*)?|tabler-icon(-.*)?|recharts-.*|fa-.*|bi-.*|material-icons|svg-inline--fa|icon)$", "icon-library marker class: no effect in GotX, drop it (size/colour come from size-* and text-*)", "lucide", "lucide-panel-left", "tabler-icon", "recharts-layer"),
        new("markers", @"^(peer(/.*)?|group/.*|@container(/.*)?|@container-.*)$", "peer / named group / @container markers have no effect in GotX (only plain `group` works): drop them and decide in C#", "peer", "group/item", "@container"),
        new("auto-margin", @"^-?m[xytrbl]?-auto$", "no auto margins: center with justify-center / items-center on the parent; push apart with justify-between or a flex-1 spacer Div before the item", "mx-auto", "ml-auto", "m-auto", "my-auto", "mt-auto"),
        new("z-index", @"^-?z-", "no z-index: paint order is tree order (later siblings and absolute children on top); true layering = w.OpenOverlay(...)", "z-10", "z-[5]", "z-auto"),
        new("fixed", @"^fixed$", "no fixed: open an overlay (w.OpenOverlay) or use absolute inside a relative root", "fixed"),
        new("sticky", @"^sticky$", "no sticky: split into a pinned header Div + an overflow-y-auto body (flex flex-col, body flex-1 min-h-0) with a .Key", "sticky"),
        new("filters", @"^(backdrop-|blur|brightness-|contrast-|grayscale|saturate-|hue-rotate-|invert|sepia|drop-shadow|filter$)", "no filters/blur/backdrop: use a solid colour with alpha (bg-black/50) and shadow-*", "backdrop-blur", "backdrop-blur-sm", "blur", "blur-sm", "blur-[2px]", "grayscale", "brightness-50", "invert", "drop-shadow-md", "sepia"),
        new("conic-mask", @"^(bg-conic|mask-)", "no conic gradient / mask: approximate with bg-linear-* / bg-radial or drop the effect", "bg-conic", "bg-conic-180", "mask-linear-to-b"),
        new("transform-3d", @"^-?(rotate-[xyz]|translate-z|perspective|transform-3d|backface-)", "no 3D transforms: use 2D rotate-* scale-* translate-x/y-* skew-*", "rotate-x-12", "perspective-500", "translate-z-4"),
        new("border-double", @"^border-double$", "only border-solid/dashed/dotted: use border-solid (or two nested boxes)", "border-double"),
        new("columns", @"^(columns-|break-(before|after|inside))", "no CSS columns: grid grid-cols-N, or N flex-col siblings", "columns-2"),
        new("float", @"^(float|clear)-", "no floats: flex row with gap / justify-between; image beside text = flex items-start", "float-left", "float-right", "clear-both"),
        new("table", @"^(table(-.*)?|caption-.*|border-spacing-.*|border-collapse)$", "no table layout: grid grid-cols-[..] for header + rows, or Ui.Table* from GotX.Components", "table", "table-cell", "table-row", "table-fixed", "border-collapse", "border-spacing-2", "caption-top"),
        new("display", @"^(contents|flow-root|list-item)$", "contents: drop the wrapper and flatten its children; flow-root/list-item: use block or flex flex-col", "contents", "flow-root", "list-item"),
        new("object-position", @"^object-", "object-fit: only object-cover / object-contain; object-position unsupported: crop the source or size the container", "object-center", "object-top", "object-left"),
        new("resize", @"^resize(-.*)?$", "no resize handle: drop it (Textarea has its own sizing)", "resize", "resize-none", "resize-y"),
        new("select", @"^select-", "no user-select control: drop it (TextInput text is selectable, static text is not)", "select-none", "select-all", "select-text"),
        new("pointer-events", @"^pointer-events-", "no pointer-events: drop it; to block input don't attach the handler or use .Disabled(true)", "pointer-events-none", "pointer-events-auto"),
        new("transition-arbitrary", @"^transition-\[", "arbitrary transition-[..]: use transition | transition-colors | -opacity | -shadow | -transform | -all", "transition-[width]", "transition-[height,width]"),
        new("animate", @"^animate-", "only animate-spin/pulse/ping/bounce/none: custom keyframes = transition + state, or App.SetInterval + Notify()", "animate-[wiggle_1s]", "animate-wiggle"),
        new("viewport-units", @"^(w|h|min-w|min-h|max-w|max-h)-(d|s|l)v(h|w)$", "dvh/svh/lvh unsupported: h-screen or h-full", "h-dvh", "h-svh", "min-h-dvh"),
        new("blend", @"^(mix-blend-|bg-blend-|isolate$)", "no blend modes/isolation: drop or pre-mix the colour", "mix-blend-multiply", "bg-blend-multiply", "isolate"),
        new("order", @"^-?order-", "no order-*: reorder the .Child(...) calls in C#", "order-1", "order-last"),
        new("reverse", @"^(flex-(row|col)-reverse|(space|divide)-[xy]-reverse)$", "no *-reverse: add the children in reverse order (or justify-end)", "flex-row-reverse", "flex-col-reverse", "divide-x-reverse", "space-x-reverse"),
        new("list", @"^list-", "no list markers: add a bullet child (\"•\" / Icon.Dot) in a flex row", "list-disc", "list-none", "list-decimal"),
        new("align", @"^align-", "vertical-align is inline-flow only: use items-* on a flex parent", "align-middle", "align-top"),
        new("break", @"^break-(words|all|normal|keep)$", "no break-*: text wraps at spaces; use min-w-0 + truncate/line-clamp-N for long strings", "break-words", "break-all", "break-normal"),
        new("ignorable", @"^(hyphens-|indent-|will-change-|appearance-|scroll-|snap-|touch-|overscroll-|caret-|accent-)", "no GotX equivalent; drop it (accent-/appearance-: use the Checkbox/Switch/RadioGroup components)", "hyphens-auto", "indent-4", "will-change-transform", "appearance-none", "scroll-smooth", "snap-x", "snap-mandatory", "touch-none", "overscroll-contain", "caret-red-500", "accent-red-500"),
        new("sr-only", @"^(sr-only|not-sr-only)$", "visually-hidden text: omit the element (or use `hidden`)", "sr-only", "not-sr-only"),
        new("shadow-inset", @"^(inset-shadow|inset-ring|text-shadow|shadow-inner|ring-inset)", "no inset ring/shadow, no text-shadow: use border + bg-*/alpha overlay (ring-inset = border inside the box); drop text-shadow", "inset-shadow-sm", "text-shadow-sm", "shadow-inner", "ring-inset"),
        new("logical", @"^-?(ps|pe|ms|me)-", "logical spacing: LTR only, use pl-/pr-/ml-/mr-", "ps-2", "pe-2", "ms-2", "me-2"),
        new("logical-inset", @"^-?(start|end)-", "start-/end-: use left-/right-", "start-0", "end-0"),
        new("logical-rounded", @"^rounded-(s|e|ss|se|es|ee)(-|$)", "rounded-s/e: use rounded-l/r (or rounded-tl/bl/tr/br)", "rounded-s-lg", "rounded-e-lg", "rounded-ss-lg"),
        new("subgrid", @"^(grid-(cols|rows)-subgrid|auto-(cols|rows)-)", "no subgrid/auto-cols/auto-rows: repeat the tracks on the child (grid-cols-[..]) or use flex", "grid-cols-subgrid", "auto-cols-fr", "auto-rows-min"),
        new("place", @"^(place-(items|content|self)-|justify-(items|self)-)", "place-*/justify-items/justify-self: flex parent with items-* + justify-*; per-cell alignment = self-* inside a flex wrapper", "place-items-center", "place-content-center", "place-self-center", "justify-items-center", "justify-self-center"),
        new("content-align", @"^content-(center|start|end|between|around|evenly|stretch|normal|baseline)$", "align-content is always start in GotX: add fixed rows (h-*) or a flex-1 spacer", "content-center", "content-between"),
        new("text-justify", @"^text-justify$", "no justified text: use text-left", "text-justify"),
        new("whitespace-pre", @"^whitespace-(pre|pre-wrap|pre-line|break-spaces)$", "no pre/pre-wrap: only whitespace-nowrap/normal; split code/lines into one Div per line", "whitespace-pre", "whitespace-pre-wrap", "whitespace-pre-line"),
        new("overflow-ellipsis", @"^overflow-ellipsis$", "v3 name: use text-ellipsis (or truncate)", "overflow-ellipsis"),
        new("decoration-style", @"^decoration-(wavy|double|dotted|dashed)$", "only solid text decoration", "decoration-wavy"),
        new("background-image", @"^bg-(cover|contain|center|top|bottom|left|right|no-repeat|repeat.*|fixed|local|scroll|clip-.*|origin-.*|size-.*|position-.*)$", "no CSS background images/clip: put w.Image(..) (object-cover) in an absolute inset-0 child; gradient text unsupported", "bg-clip-text", "bg-cover", "bg-center", "bg-no-repeat", "bg-fixed"),
        new("svg-paint", @"^(fill|stroke)-", "SVG paint comes from text colour (currentColor): put text-<color> on the w.Path element; stroke width is fixed in VectorPath.Stroke/Icon", "fill-current", "stroke-current", "fill-red-500", "stroke-2", "stroke-red-500"),
        new("cursor", @"^cursor-", "only cursor-pointer / cursor-text / cursor-default exist (cursor-not-allowed -> Disabled(true) + cursor-default)", "cursor-not-allowed", "cursor-grab", "cursor-wait", "cursor-help", "cursor-crosshair", "cursor-move"),
        new("font-family", @"^font-(serif|\[)", "only font-sans / font-mono", "font-serif"),
        new("container-class", @"^container$", "no .container: w-full max-w-7xl (xs..7xl) and center with justify-center on the parent", "container"),
        new("max-w-screen", @"^max-w-screen-", "v3 screen scale: use max-w-sm..7xl or max-w-[1024px]", "max-w-screen-lg"),
        new("prose", @"^prose(-.*)?$", "typography plugin unsupported: style headings/paragraphs explicitly", "prose"),
        new("arbitrary-property", @"^\[[a-zA-Z-]+:", "arbitrary CSS property: translate to the matching utility", "[mask-type:luminance]"),
    ];

    public static string Suggest(string token)
    {
        foreach (string v in ClassAudit.VariantsOf(token))
            foreach (var r in VariantRules)
                if (r.Rx.IsMatch(v) && !ClassAudit.IsSupported(v + ":flex")) return $"{r.Advice}";
        string core = Core(token);
        foreach (var r in UtilityRules)
            if (r.Rx.IsMatch(core)) return r.Advice;
        return CustomName().IsMatch(core) ? Custom : Generic;
    }

    /// <summary>
    /// Tokens the parser accepts but only approximates. Each entry was read off the Core mapping (src/GotX.Core/Styling):
    /// Tailwind.LayoutUtilities.cs (space-x/y -> Gap; items-/self-baseline are real first-baseline alignment now), Tailwind.Values.cs (fit/min/max -> Length.Auto),
    /// Tailwind.Utilities.cs (inline* -> block/flex/grid, overflow-clip -> hidden, overflow-scroll -> auto). null = exact.
    /// </summary>
    public static string? Approximation(string token)
    {
        string core = Core(token).TrimStart('-');
        if (SpaceXy().IsMatch(core)) return "space-x/y is approximated by gap on the parent: prefer gap-* (also applies before the first child / no margin collapse differences)";
        if (core == "w-fit") return null; // shrink-to-fit width is real now (block flow / flex column), clamped to the available width
        if (ContentSize().IsMatch(core)) return "fit/min/max-content behave like auto (no intrinsic sizing keywords)";
        if (core is "inline" or "inline-block" or "inline-flex" or "inline-grid") return core == "inline" || core == "inline-block" ? "inline = block: no inline flow, the element takes its own line" : $"{core}: shrink-to-fit width like the CSS keyword, but the element still takes its own line (no inline flow with surrounding text)";
        if (core is "overflow-clip" or "overflow-x-clip" or "overflow-y-clip") return "overflow-clip = overflow-hidden";
        if (core is "overflow-scroll" or "overflow-x-scroll" or "overflow-y-scroll") return "overflow-scroll = overflow-auto (scrollbar shown only when content overflows)";
        if (core == "text-pretty") return "text-pretty approximates Chromium's algorithm";
        return null;
    }

    static readonly string[] NoOps = ["antialiased", "subpixel-antialiased", "transform", "transform-gpu", "transform-cpu"];

    /// <summary>Valid Tailwind utilities that have no effect in GotX by design (font smoothing, GPU hints): not reported as unsupported.</summary>
    public static string? NoOp(string token)
    {
        string core = Core(token);
        if (Array.IndexOf(NoOps, core) >= 0 || core.StartsWith("will-change-", StringComparison.Ordinal))
            return "valid Tailwind but a no-op in GotX (font smoothing / GPU hint): drop it";
        return null;
    }

    /// <summary>The token matches one of the specific advice rules or is a known no-op (i.e. it is recognisably Tailwind even though GotX lacks it).</summary>
    public static bool IsKnownUnsupported(string token)
    {
        if (NoOp(token) is not null) return true;
        string a = Suggest(token);
        return a != Generic && a != Custom;
    }

    /// <summary>Utility part: variants, '!' and the negative sign removed.</summary>
    public static string Core(string token)
    {
        int depth = 0, start = 0;
        for (int i = 0; i < token.Length; i++)
        {
            char c = token[i];
            if (c is '[' or '(') depth++;
            else if (c is ']' or ')') depth--;
            else if (c == ':' && depth == 0) start = i + 1;
        }
        string u = token[start..].Trim('!');
        return u;
    }

    [GeneratedRegex(@"^[a-z][a-z0-9]*(-[a-z0-9]+)*$")]
    private static partial Regex CustomName();

    [GeneratedRegex(@"^space-[xy]-")]
    private static partial Regex SpaceXy();

    [GeneratedRegex(@"^(w|h|min-w|min-h|max-w|max-h)-(fit|min|max)$")]
    private static partial Regex ContentSize();
}
