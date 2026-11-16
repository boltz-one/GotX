using System.Globalization;
using System.Text;

namespace GotX.Text;

/// <summary>
/// Quản lý font theo họ / độ đậm / nghiêng (TextSystem.Fonts.cs), định hình chữ (Shape.cs), đo chữ (Measure.cs),
/// ngắt dòng (Wrap*.cs) và cache glyph.
/// </summary>
public sealed partial class TextSystem
{
    public GlyphAtlas Atlas { get; } = new();

    readonly Dictionary<(string, TextCase), string> _cased = new();

    /// <summary>uppercase / lowercase / capitalize: chuỗi đã đổi được nhớ lại để mỗi frame không cấp phát.</summary>
    public string ApplyCase(string text, TextCase textCase)
    {
        if (textCase == TextCase.None || text.Length == 0) return text;
        if (_cased.TryGetValue((text, textCase), out var hit)) return hit;
        string result = textCase switch
        {
            TextCase.Upper => text.ToUpperInvariant(),
            TextCase.Lower => text.ToLowerInvariant(),
            _ => Capitalize(text),
        };
        if (_cased.Count >= 2048) _cased.Clear();
        return _cased[(text, textCase)] = result;
    }

    static string Capitalize(string text)
    {
        var chars = text.ToCharArray();
        bool start = true;
        for (int i = 0; i < chars.Length; i++)
        {
            if (char.IsWhiteSpace(chars[i])) { start = true; continue; }
            if (start) chars[i] = char.ToUpperInvariant(chars[i]);
            start = false;
        }
        return new string(chars);
    }
}

/// <summary>Một dòng sau khi wrap: [Start, Start+Length) trong chuỗi gốc.</summary>
public readonly record struct TextLine(int Start, int Length, float Width)
{
    public int End => Start + Length;
}
