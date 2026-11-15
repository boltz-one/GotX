using System.Globalization;
using System.Text;
using GotX;
using GotX.Text;

namespace ShadcnShots;

/// <summary>Turns a GotX <see cref="UiSnapshot"/> into layout items comparable with the reference layout map.</summary>
public static class LayoutExport
{
    public static List<LayoutItem> FromSnapshot(UiSnapshot snap, TextSystem text)
    {
        var items = new List<LayoutItem>();
        var subtree = new string?[snap.Nodes.Count];
        foreach (var n in snap.Nodes)
        {
            if (!Shown(n)) continue;
            bool isText = n.Kind == "text" && !string.IsNullOrWhiteSpace(n.Text);
            bool box = !isText && (n.Background.A > 0 || n.Border.Horizontal + n.Border.Vertical > 0 || n.Clickable || n.Focusable || n.Kind == "input");
            if (!isText && !box) continue;
            string? label = isText ? n.Text : SubtreeText(snap, n.Index, subtree);
            if (string.IsNullOrWhiteSpace(label)) continue;
            items.Add(new LayoutItem
            {
                Tag = n.Kind, Key = n.Key, Classes = n.Classes, Text = Clip(label.Trim()), Own = isText, Box = box,
                Rect = n.Screen, TextRect = isText ? TextBox(n, text) : default,
                FontSize = Px(n.TextStyle.FontSize), FontWeight = ((int)n.TextStyle.Weight).ToString(CultureInfo.InvariantCulture),
                Color = Css(n.Color), Background = Css(n.Background),
            });
        }
        return items;
    }

    static bool Shown(UiNode n)
    {
        var v = n.Visible;
        return v.Width > 0 && v.Height > 0 && n.Opacity > 0;
    }

    [ThreadStatic] static List<TextLine>? t_lines;

    /// <summary>
    /// Union of the painted line boxes, like a DOM Range rect: the text is cased and wrapped exactly as Window.PaintText does
    /// (WrapStyled at box width + 0.01, so text-balance / tracking / tabular-nums / kerning apply; line-clamp limits the lines);
    /// each line sits at box.x + (box.width − lineWidth) × align. x = leftmost line start, w = widest line (an ellipsized line
    /// counts as the box width), y / h = first line top to last line bottom (line-height boxes; callers compare vertical centres).
    /// </summary>
    static Bounds TextBox(UiNode n, TextSystem text)
    {
        var b = n.Screen;
        var ts = n.TextStyle;
        string s = text.ApplyCase(n.Text!, ts.Case);
        var lines = t_lines ??= new List<TextLine>();
        text.WrapStyled(s, ts, ts.NoWrap ? float.NaN : b.Width + 0.01f, lines);
        int shown = ts.LineClamp > 0 ? Math.Min(lines.Count, ts.LineClamp) : lines.Count;
        if (shown == 0) return b;
        float align = ts.Align switch { TextAlign.Center => 0.5f, TextAlign.Right => 1f, _ => 0f };
        float x0 = float.MaxValue, x1 = float.MinValue;
        for (int i = 0; i < shown; i++)
        {
            float w = lines[i].Width;
            bool cut = (ts.Ellipsis && w > b.Width + 0.5f) || (i == shown - 1 && shown < lines.Count);
            float x = cut ? b.X : b.X + (b.Width - w) * align;
            if (cut) w = MathF.Min(w, b.Width);
            x0 = MathF.Min(x0, x);
            x1 = MathF.Max(x1, x + w);
        }
        return new Bounds(x0, b.Y, x1 - x0, shown * ts.LineHeight);
    }

    static string? SubtreeText(UiSnapshot snap, int index, string?[] cache)
    {
        if (cache[index] is { } hit) return hit;
        var n = snap.Nodes[index];
        if (n.Kind == "text") return cache[index] = n.Text ?? "";
        var sb = new StringBuilder();
        if (n.Kind == "input" && !string.IsNullOrEmpty(n.Text)) sb.Append(n.Text);
        foreach (int c in n.Children)
        {
            if (!Shown(snap.Nodes[c])) continue;
            string part = SubtreeText(snap, c, cache) ?? "";
            if (part.Length == 0) continue;
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(part);
            if (sb.Length > 200) break;
        }
        return cache[index] = sb.ToString();
    }

    static string Clip(string s) => s.Length > 60 ? s[..60] : s;

    static string Px(float v) => v.ToString("0.##", CultureInfo.InvariantCulture) + "px";

    static string Css(Rgba c)
    {
        static int B(float v) => (int)MathF.Round(Math.Clamp(v, 0, 1) * 255);
        return c.A >= 1 ? $"rgb({B(c.R)}, {B(c.G)}, {B(c.B)})"
            : $"rgba({B(c.R)}, {B(c.G)}, {B(c.B)}, {c.A.ToString("0.###", CultureInfo.InvariantCulture)})";
    }
}
