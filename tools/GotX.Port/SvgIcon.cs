using System.Globalization;
using System.Text.RegularExpressions;

namespace GotX.Port;

/// <summary>Result of reading an inline &lt;svg&gt;: the VectorPath factory call and what to check by hand.</summary>
public sealed record SvgIconInfo(string Factory, int Parts, List<string> Notes);

/// <summary>Turns inline SVG (path/circle/ellipse/rect/line/polyline/polygon) into the <c>VectorPath</c> string forms GotX understands.</summary>
public static partial class SvgIcon
{
    public static SvgIconInfo Convert(HtmlNode svg)
    {
        var parts = new List<string>();
        var notes = new List<string>();
        Collect(svg, parts, notes);
        string vb = svg.Attr("viewBox") ?? "0 0 24 24";
        var nums = Numbers(vb);
        float vw = nums.Count == 4 ? nums[2] : 24, vh = nums.Count == 4 ? nums[3] : 24;
        bool is24 = vw == 24 && vh == 24 && (nums.Count != 4 || (nums[0] == 0 && nums[1] == 0));
        string fill = svg.Attr("fill") ?? "";
        bool stroke = !string.IsNullOrEmpty(svg.Attr("stroke")) || fill is "none";
        string sw = svg.Attr("stroke-width") ?? "2";
        if (!is24) notes.Add($"viewBox {vb}: not 24x24, check scaling");
        if (parts.Count > 6) notes.Add($"{parts.Count} parts: probably a chart/illustration, not an icon (see Ui.Chart / VectorPath.Polyline in GotX.Components)");
        if (stroke && sw != "2") notes.Add($"stroke-width {sw}: VectorPath.Icon fixes 2");
        if (stroke && svg.Attr("stroke-linecap") is { } cap && cap != "round") notes.Add($"stroke-linecap {cap}: GotX strokes use round caps/joins");
        string args = string.Join(", ", parts.Select(Quote));
        if (parts.Count == 0) return new SvgIconInfo("null! /* TODO: no drawable elements found */", 0, notes);
        string factory;
        if (is24) factory = stroke ? $"VectorPath.Icon({args})" : $"VectorPath.FilledIcon({args})";
        else if (parts.Count == 1 && IsPathD(parts[0]))
            factory = stroke ? $"VectorPath.Stroke({args}, {sw}, {F(vw)}, {F(vh)})" : $"VectorPath.Fill({args}, {F(vw)}, {F(vh)})";
        else
        {
            notes.Add("multi-part non-24 viewBox: VectorPath.Icon/FilledIcon assume 24x24, rescale coordinates");
            factory = stroke ? $"VectorPath.Icon({args})" : $"VectorPath.FilledIcon({args})";
        }
        return new SvgIconInfo(factory, parts.Count, notes);
    }

    static void Collect(HtmlNode n, List<string> parts, List<string> notes)
    {
        foreach (var c in n.Children)
        {
            if (c.IsText) continue;
            string? d = c.Tag switch
            {
                "path" => c.Attr("d"),
                "circle" => $"circle {N(c, "cx")} {N(c, "cy")} {N(c, "r")}",
                "ellipse" => $"ellipse {N(c, "cx")} {N(c, "cy")} {N(c, "rx")} {N(c, "ry")}",
                "line" => $"line {N(c, "x1")} {N(c, "y1")} {N(c, "x2")} {N(c, "y2")}",
                "rect" => c.Has("rx") ? $"rect {N(c, "x")} {N(c, "y")} {N(c, "width")} {N(c, "height")} {N(c, "rx")}" : $"rect {N(c, "x")} {N(c, "y")} {N(c, "width")} {N(c, "height")}",
                "polyline" => "polyline " + Pairs(c.Attr("points")),
                "polygon" => "polygon " + Pairs(c.Attr("points")),
                _ => null,
            };
            if (d is not null && c.Attr("stroke") == "none" && c.Attr("fill") == "none") { }
            else if (d is not null) parts.Add(d.Trim());
            else if (c.Tag is "g" or "defs" or "symbol" or "clippath") { if (c.Tag is "g") Collect(c, parts, notes); else notes.Add($"<{c.Tag}> skipped"); }
            else if (c.Tag is "title" or "desc") { }
            else notes.Add($"<{c.Tag}> not convertible");
        }
    }

    static string N(HtmlNode c, string attr) => c.Attr(attr) is { Length: > 0 } v ? v : "0";

    static string Pairs(string? points)
    {
        var n = Numbers(points ?? "");
        var sb = new List<string>();
        for (int i = 0; i + 1 < n.Count; i += 2) sb.Add($"{F(n[i])},{F(n[i + 1])}");
        return string.Join(' ', sb);
    }

    [GeneratedRegex(@"-?\d*\.?\d+(?:[eE][-+]?\d+)?")]
    private static partial Regex NumRx();

    static List<float> Numbers(string s) =>
        NumRx().Matches(s).Select(m => float.Parse(m.Value, CultureInfo.InvariantCulture)).ToList();

    static bool IsPathD(string s) => s.Length > 0 && char.IsLetter(s[0]) && !s.StartsWith("circle") && !s.StartsWith("ellipse") && !s.StartsWith("line")
        && !s.StartsWith("rect") && !s.StartsWith("polyline") && !s.StartsWith("polygon");

    static string F(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);
    static string Quote(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
