using System.Text;
using System.Text.Json;
using GotX;

namespace ShadcnShots;

/// <summary>
/// One entry of a layout map (reference JSON from capture.mjs or GotX export — same shape).
/// <see cref="Own"/>: the element carries the text itself (a text run) → compared by <see cref="TextRect"/>.
/// <see cref="Box"/>: a box (data-slot element / GotX node with background, border or interaction) → compared by <see cref="Rect"/>.
/// </summary>
public sealed class LayoutItem
{
    public string Tag = "";
    public string? Slot, Text, Key, Classes;
    public bool Own, Box;
    public Bounds Rect, TextRect;
    public string? FontSize, FontWeight, Color, Background;

    public string Norm => Normalize(Text);

    /// <summary>Case-insensitive, whitespace-collapsed (DOM innerText applies text-transform, GotX keeps source text).</summary>
    public static string Normalize(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new StringBuilder(s.Length);
        bool space = false;
        foreach (char ch in s.Trim())
        {
            if (char.IsWhiteSpace(ch)) { space = true; continue; }
            if (space && sb.Length > 0) sb.Append(' ');
            space = false;
            sb.Append(char.ToLowerInvariant(ch));
        }
        return sb.ToString();
    }
}

/// <summary>Layout map file: reading (both formats) and writing (GotX export).</summary>
public static class LayoutMapFile
{
    public static List<LayoutItem> Read(string path, out int width, out int height)
    {
        using var doc = JsonDocument.Parse(File.ReadAllBytes(path));
        var root = doc.RootElement;
        width = root.GetProperty("width").GetInt32();
        height = root.GetProperty("height").GetInt32();
        var list = new List<LayoutItem>();
        foreach (var e in root.GetProperty("elements").EnumerateArray())
        {
            var item = new LayoutItem
            {
                Tag = Str(e, "tag") ?? "", Slot = Str(e, "slot"), Text = Str(e, "text"), Key = Str(e, "key"), Classes = Str(e, "cls"),
                Own = e.TryGetProperty("own", out var own) && own.GetBoolean(),
                Rect = RectOf(e, "rect"), TextRect = RectOf(e, "textRect"),
            };
            if (e.TryGetProperty("style", out var s))
            {
                item.FontSize = Str(s, "fontSize"); item.FontWeight = Str(s, "fontWeight");
                item.Color = Str(s, "color"); item.Background = Str(s, "backgroundColor");
            }
            // GotX export states it explicitly. Reference: a data-slot element with text that paints a box (background, border)
            // or is a control — text-only slots (card-title, label…) are compared as text runs instead.
            item.Box = e.TryGetProperty("box", out var box) ? box.GetBoolean()
                : item.Slot is not null && item.Text is not null && (Painted(e) || Control(item.Tag));
            if (item.Own && item.TextRect.Width <= 0) item.TextRect = item.Rect;
            list.Add(item);
        }
        return list;
    }

    public static void Write(string path, string page, int width, int height, string theme, List<LayoutItem> items)
    {
        using var fs = File.Create(path);
        using var j = new Utf8JsonWriter(fs);
        j.WriteStartObject();
        j.WriteString("page", page); j.WriteString("source", "GotX");
        j.WriteNumber("width", width); j.WriteNumber("height", height); j.WriteString("theme", theme);
        j.WriteStartArray("elements");
        foreach (var i in items)
        {
            j.WriteStartObject();
            j.WriteString("tag", i.Tag);
            if (i.Key is not null) j.WriteString("key", i.Key);
            if (i.Text is not null) j.WriteString("text", i.Text);
            if (i.Own) j.WriteBoolean("own", true);
            if (i.Box) j.WriteBoolean("box", true);
            WriteRect(j, "rect", i.Rect);
            if (i.Own) WriteRect(j, "textRect", i.TextRect);
            j.WriteStartObject("style");
            if (i.FontSize is not null) j.WriteString("fontSize", i.FontSize);
            if (i.FontWeight is not null) j.WriteString("fontWeight", i.FontWeight);
            if (i.Color is not null) j.WriteString("color", i.Color);
            if (i.Background is not null) j.WriteString("backgroundColor", i.Background);
            j.WriteEndObject();
            if (i.Classes is not null) j.WriteString("cls", i.Classes);
            j.WriteEndObject();
        }
        j.WriteEndArray();
        j.WriteEndObject();
    }

    static bool Control(string tag) => tag is "button" or "input" or "textarea" or "select" or "a";

    static bool Painted(JsonElement e)
    {
        if (!e.TryGetProperty("style", out var s)) return false;
        string bg = Str(s, "backgroundColor") ?? "";
        if (bg.Length > 0 && bg != "transparent" && !bg.EndsWith(", 0)", StringComparison.Ordinal)) return true;
        foreach (var part in (Str(s, "borderWidth") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
            if (part != "0px") return true;
        return false;
    }

    static string? Str(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) ? v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Number => v.GetRawText(),
            _ => null,
        } : null;

    static Bounds RectOf(JsonElement e, string name) =>
        e.TryGetProperty(name, out var r) && r.ValueKind == JsonValueKind.Object
            ? new Bounds(r.GetProperty("x").GetSingle(), r.GetProperty("y").GetSingle(), r.GetProperty("w").GetSingle(), r.GetProperty("h").GetSingle())
            : default;

    static void WriteRect(Utf8JsonWriter j, string name, Bounds b)
    {
        j.WriteStartObject(name);
        j.WriteNumber("x", MathF.Round(b.X, 2)); j.WriteNumber("y", MathF.Round(b.Y, 2));
        j.WriteNumber("w", MathF.Round(b.Width, 2)); j.WriteNumber("h", MathF.Round(b.Height, 2));
        j.WriteEndObject();
    }
}
