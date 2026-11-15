using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GotX.Port;

public sealed class HtmlNode
{
    public string Tag = "";           // "" for text
    public string Text = "";          // text nodes: decoded, not collapsed
    public List<KeyValuePair<string, string>> Attrs = [];
    public List<HtmlNode> Children = [];
    public HtmlNode? Parent;
    public int Line;

    public bool IsText => Tag.Length == 0;
    public string? Attr(string name) => Attrs.FirstOrDefault(a => a.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;
    public bool Has(string name) => Attrs.Any(a => a.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Tolerant hand-written HTML parser: void elements, unquoted attributes, comments, raw-text style/script, entities, implied end tags.</summary>
public static partial class HtmlParser
{
    static readonly HashSet<string> Void = ["area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr", "param"];
    static readonly HashSet<string> RawText = ["script", "style", "textarea", "title"];
    static readonly HashSet<string> BlockStarts = ["div", "p", "ul", "ol", "li", "table", "section", "article", "header", "footer", "nav", "aside", "main", "form", "h1", "h2", "h3", "h4", "h5", "h6", "pre", "blockquote", "fieldset", "hr"];

    /// <summary>Parses a fragment or a page; returns a synthetic root whose children are the top-level nodes.</summary>
    public static HtmlNode Parse(string html)
    {
        var root = new HtmlNode { Tag = "#root" };
        var cur = root;
        int i = 0, line = 1;
        var text = new StringBuilder();
        int textLine = 1;
        void FlushText()
        {
            if (text.Length == 0) return;
            cur.Children.Add(new HtmlNode { Text = Decode(text.ToString()), Parent = cur, Line = textLine });
            text.Clear();
        }
        while (i < html.Length)
        {
            char c = html[i];
            if (c != '<' || i + 1 >= html.Length || !(char.IsLetter(html[i + 1]) || html[i + 1] is '/' or '!' or '?'))
            {
                if (text.Length == 0) textLine = line;
                if (c == '\n') line++;
                text.Append(c); i++;
                continue;
            }
            if (html.AsSpan(i).StartsWith("<!--"))
            {
                int e = html.IndexOf("-->", i + 4, StringComparison.Ordinal);
                int end = e < 0 ? html.Length : e + 3;
                line += Count(html, i, end);
                i = end;
                continue;
            }
            if (html[i + 1] is '!' or '?')
            {
                int e = html.IndexOf('>', i);
                int end = e < 0 ? html.Length : e + 1;
                line += Count(html, i, end);
                i = end;
                continue;
            }
            if (html[i + 1] == '/')
            {
                int e = html.IndexOf('>', i);
                if (e < 0) break;
                string name = html[(i + 2)..e].Trim().ToLowerInvariant();
                FlushText();
                // Close the nearest open element with that name; stray end tags are ignored.
                for (var p = cur; p is { Parent: not null }; p = p.Parent)
                    if (p.Tag == name) { cur = p.Parent; break; }
                line += Count(html, i, e);
                i = e + 1;
                continue;
            }
            // start tag
            int j = i + 1;
            while (j < html.Length && !char.IsWhiteSpace(html[j]) && html[j] is not ('>' or '/')) j++;
            string tag = html[(i + 1)..j].ToLowerInvariant();
            var attrs = new List<KeyValuePair<string, string>>();
            bool selfClose = false;
            j = ReadAttrs(html, j, attrs, ref selfClose, ref line);
            FlushText();
            // implied end tags
            if (tag == "li") for (var p = cur; p.Parent is not null && p.Tag is not ("ul" or "ol"); p = p.Parent) if (p.Tag == "li") { cur = p.Parent; break; }
            if (BlockStarts.Contains(tag) && cur.Tag == "p") cur = cur.Parent!;
            if (tag is "td" or "th" && cur.Tag is "td" or "th") cur = cur.Parent!;
            if (tag == "tr" && cur.Tag is "td" or "th") cur = cur.Parent!.Parent ?? cur;
            if (tag == "tr" && cur.Tag == "tr") cur = cur.Parent!;
            var node = new HtmlNode { Tag = tag, Attrs = attrs, Parent = cur, Line = line };
            cur.Children.Add(node);
            i = j;
            if (RawText.Contains(tag) && !selfClose)
            {
                int e = html.IndexOf("</" + tag, i, StringComparison.OrdinalIgnoreCase);
                int end = e < 0 ? html.Length : e;
                string raw = html[i..end];
                if (tag == "textarea" || tag == "title") node.Children.Add(new HtmlNode { Text = Decode(raw), Parent = node, Line = line });
                line += Count(html, i, end);
                int close = e < 0 ? html.Length : html.IndexOf('>', e) + 1;
                if (close <= 0) close = html.Length;
                i = close;
                continue;
            }
            if (!selfClose && !Void.Contains(tag)) cur = node;
        }
        FlushText();
        return root;
    }

    static int ReadAttrs(string s, int j, List<KeyValuePair<string, string>> attrs, ref bool selfClose, ref int line)
    {
        while (j < s.Length)
        {
            char c = s[j];
            if (c == '\n') line++;
            if (char.IsWhiteSpace(c)) { j++; continue; }
            if (c == '>') return j + 1;
            if (c == '/') { selfClose = j + 1 < s.Length && s[j + 1] == '>'; j++; continue; }
            int ns = j;
            while (j < s.Length && !char.IsWhiteSpace(s[j]) && s[j] is not ('=' or '>' or '/' ) ) j++;
            if (j == ns) { j++; continue; }
            string name = s[ns..j];
            int k = j;
            while (k < s.Length && char.IsWhiteSpace(s[k])) k++;
            string value = "";
            if (k < s.Length && s[k] == '=')
            {
                k++;
                while (k < s.Length && char.IsWhiteSpace(s[k])) k++;
                if (k < s.Length && s[k] is '"' or '\'')
                {
                    char q = s[k];
                    int e = s.IndexOf(q, k + 1);
                    if (e < 0) e = s.Length;
                    value = s[(k + 1)..e];
                    line += Count(s, k, e);
                    j = Math.Min(s.Length, e + 1);
                }
                else
                {
                    int e = k;
                    while (e < s.Length && !char.IsWhiteSpace(s[e]) && s[e] != '>') e++;
                    value = s[k..e];
                    j = e;
                }
            }
            attrs.Add(new(name, Decode(value)));
        }
        return j;
    }

    static int Count(string s, int from, int to)
    {
        int n = 0;
        for (int i = from; i < Math.Min(to, s.Length); i++) if (s[i] == '\n') n++;
        return n;
    }

    [GeneratedRegex(@"&(#x[0-9a-fA-F]+|#[0-9]+|[a-zA-Z][a-zA-Z0-9]*);")]
    private static partial Regex EntityRx();

    static readonly Dictionary<string, string> Named = new()
    {
        ["amp"] = "&", ["lt"] = "<", ["gt"] = ">", ["quot"] = "\"", ["apos"] = "'", ["nbsp"] = " ", ["copy"] = "©",
        ["reg"] = "®", ["hellip"] = "…", ["mdash"] = "—", ["ndash"] = "–", ["rarr"] = "→", ["larr"] = "←",
        ["middot"] = "·", ["bull"] = "•", ["times"] = "×", ["laquo"] = "«", ["raquo"] = "»", ["lsquo"] = "‘",
        ["rsquo"] = "’", ["ldquo"] = "“", ["rdquo"] = "”", ["trade"] = "™", ["deg"] = "°",
    };

    public static string Decode(string s)
    {
        if (s.IndexOf('&') < 0) return s;
        return EntityRx().Replace(s, m =>
        {
            string e = m.Groups[1].Value;
            if (e[0] == '#')
            {
                bool hex = e.Length > 1 && e[1] is 'x' or 'X';
                if (int.TryParse(e.AsSpan(hex ? 2 : 1), hex ? NumberStyles.HexNumber : NumberStyles.Integer, CultureInfo.InvariantCulture, out int cp) && cp is > 0 and <= 0x10FFFF && cp is < 0xD800 or > 0xDFFF)
                    return char.ConvertFromUtf32(cp);
                return m.Value;
            }
            return Named.TryGetValue(e, out string? v) ? v : m.Value;
        });
    }
}
