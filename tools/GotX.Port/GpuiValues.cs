using System.Globalization;
using System.Text.RegularExpressions;

namespace GotX.Port;

/// <summary>Value translation for GPUI style arguments: lengths (px/rems/relative), colours (rgb/hsla/theme tokens), numbers.</summary>
public static partial class GpuiValues
{
    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    [GeneratedRegex(@"^(?:\w+::)*(px|rems|relative|percentage)\(\s*(-?[\d_]*\.?[\d_]*)(?:f32)?\s*\)$")]
    private static partial Regex LenRx();

    [GeneratedRegex(@"^(?:\w+::)+\w+\(+\s*(?:\w+::)*px\(\s*(-?[\d_]*\.?[\d_]*)\s*\)\s*\)+$")]
    private static partial Regex WrappedPxRx();

    [GeneratedRegex(@"^(?:\w+::)*(?:auto)\(\)$|^Length::Auto$|^DefiniteLength::\w*Auto$")]
    private static partial Regex AutoRx();

    /// <summary>"12px" / "2rem" / "50%" / "auto" for a GPUI length expression, or null when it is not a literal (dynamic).</summary>
    public static string? Length(string arg)
    {
        string a = StripInto(arg);
        if (AutoRx().IsMatch(a)) return "auto";
        var m = LenRx().Match(a);
        if (m.Success && double.TryParse(m.Groups[2].Value.Replace("_", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
            return m.Groups[1].Value switch { "px" => F(v) + "px", "rems" => F(v) + "rem", _ => F(v * 100) + "%" };
        var w = WrappedPxRx().Match(a);
        if (w.Success && double.TryParse(w.Groups[1].Value.Replace("_", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return F(v) + "px";
        return null;
    }

    public static double? Number(string arg)
    {
        string a = Regex.Replace(StripInto(arg), @"f32$|f64$", "").Replace("_", "");
        return double.TryParse(a, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : null;
    }

    static string StripInto(string s) => Regex.Replace(s.Trim(), @"\s+", "").Replace(".into()", "");

    // ---------- colours ----------

    public sealed record ColorResult(string? Token, string? Note);

    [GeneratedRegex(@"^(?:\w+::)*rgba?\(0x([0-9a-fA-F_]+)\)$")]
    private static partial Regex RgbRx();

    [GeneratedRegex(@"^(?:\w+::)*hsla?\(([^()]*)\)$")]
    private static partial Regex HslRx();

    [GeneratedRegex(@"(?:colors\(\)|colors|status\(\))\s*\.\s*(\w+)")]
    private static partial Regex ThemeRx();

    [GeneratedRegex(@"\.(?:opacity|alpha)\(\s*([\d.]+)(?:f32)?\s*\)$")]
    private static partial Regex OpacityRx();

    static readonly Dictionary<string, string> NamedColors = new()
    {
        ["white"] = "white", ["black"] = "black", ["transparent_black"] = "transparent", ["transparent_white"] = "transparent",
        ["red"] = "red-500", ["green"] = "green-500", ["blue"] = "blue-500", ["yellow"] = "yellow-500",
    };

    // Zed ThemeColors / StatusColors field -> nearest GotX semantic token. WRITTEN FROM MEMORY of Zed's theme crate: candidates only.
    static readonly Dictionary<string, string> ThemeTokens = new()
    {
        ["background"] = "background", ["surface_background"] = "card", ["elevated_surface_background"] = "popover", ["editor_background"] = "background",
        ["panel_background"] = "sidebar", ["title_bar_background"] = "sidebar", ["status_bar_background"] = "sidebar", ["toolbar_background"] = "background",
        ["tab_bar_background"] = "muted", ["tab_active_background"] = "background", ["tab_inactive_background"] = "muted",
        ["border"] = "border", ["border_variant"] = "border", ["border_disabled"] = "border", ["border_focused"] = "ring", ["border_selected"] = "ring", ["border_transparent"] = "transparent",
        ["text"] = "foreground", ["text_muted"] = "muted-foreground", ["text_placeholder"] = "muted-foreground", ["text_disabled"] = "muted-foreground", ["text_accent"] = "primary",
        ["element_background"] = "secondary", ["element_hover"] = "accent", ["element_active"] = "accent", ["element_selected"] = "accent", ["element_disabled"] = "muted",
        ["ghost_element_background"] = "transparent", ["ghost_element_hover"] = "accent", ["ghost_element_active"] = "accent", ["ghost_element_selected"] = "accent", ["ghost_element_disabled"] = "transparent",
        ["icon"] = "foreground", ["icon_muted"] = "muted-foreground", ["icon_disabled"] = "muted-foreground", ["icon_accent"] = "primary",
        ["error"] = "destructive", ["warning"] = "amber-500", ["success"] = "green-500", ["info"] = "blue-500",
    };

    /// <summary>Colour part of a utility ("[#1e1e1e]", "[#1e1e1e]/50", "background", "red-500") or a note when it cannot be resolved.</summary>
    public static ColorResult Color(string arg)
    {
        string a = Regex.Replace(arg.Trim(), @"\s+", "");
        double alpha = 1;
        var op = OpacityRx().Match(a);
        if (op.Success) { alpha = double.Parse(op.Groups[1].Value, CultureInfo.InvariantCulture); a = a[..op.Index]; }
        a = a.Replace(".into()", "");
        if (RgbRx().Match(a) is { Success: true } m)
        {
            string hex = m.Groups[1].Value.Replace("_", "").ToLowerInvariant();
            if (a.Contains("rgba(") && hex.Length == 8) { alpha *= Convert.ToInt32(hex[6..], 16) / 255.0; hex = hex[..6]; }
            if (hex.Length == 6) return new ColorResult(Alpha("[#" + hex + "]", alpha), null);
        }
        if (HslRx().Match(a) is { Success: true } h)
        {
            var p = h.Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Number).ToList();
            if (p.Count >= 3 && p.All(x => x is not null))
            {
                double al = p.Count > 3 ? p[3]!.Value : 1;
                var (r, g, b) = HslToRgb(p[0]!.Value, p[1]!.Value, p[2]!.Value);
                return new ColorResult(Alpha($"[#{r:x2}{g:x2}{b:x2}]", alpha * al), null);
            }
        }
        var call = Regex.Match(a, @"^(?:\w+::)*(\w+)\(\)$");
        if (call.Success && NamedColors.TryGetValue(call.Groups[1].Value, out string? named))
            return new ColorResult(Alpha(named, alpha), named.Contains('-') ? $"{call.Groups[1].Value}() is GPUI's palette colour; {named} is the nearest Tailwind shade" : null);
        if (ThemeRx().Match(a) is { Success: true } t)
        {
            string field = t.Groups[1].Value;
            if (ThemeTokens.TryGetValue(field, out string? token))
                return new ColorResult(Alpha(token, alpha), $"theme colour '{field}' -> candidate token '{token}' (guess, check against the intended design)");
            return new ColorResult(null, $"theme colour '{field}' has no known mapping: pick a semantic token (background card popover muted accent primary secondary destructive border input ring sidebar)");
        }
        return new ColorResult(null, null);
    }

    static string Alpha(string token, double alpha) => alpha >= 0.995 ? token : $"{token}/{(int)Math.Round(alpha * 100)}";

    static (int R, int G, int B) HslToRgb(double h, double s, double l)
    {
        h = (h % 1 + 1) % 1;
        double q = l < 0.5 ? l * (1 + s) : l + s - l * s, p = 2 * l - q;
        static double Hue(double p, double q, double t)
        {
            t = (t % 1 + 1) % 1;
            return t < 1.0 / 6 ? p + (q - p) * 6 * t : t < 0.5 ? q : t < 2.0 / 3 ? p + (q - p) * (2.0 / 3 - t) * 6 : p;
        }
        static int B(double v) => (int)Math.Round(Math.Clamp(v, 0, 1) * 255);
        return (B(Hue(p, q, h + 1.0 / 3)), B(Hue(p, q, h)), B(Hue(p, q, h - 1.0 / 3)));
    }

    public static readonly Dictionary<string, string> Weights = new()
    {
        ["THIN"] = "thin", ["EXTRA_LIGHT"] = "extralight", ["LIGHT"] = "light", ["NORMAL"] = "normal", ["MEDIUM"] = "medium",
        ["SEMIBOLD"] = "semibold", ["BOLD"] = "bold", ["EXTRA_BOLD"] = "extrabold", ["BLACK"] = "black",
    };
}
