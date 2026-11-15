using System.Globalization;
using System.Text;

namespace GotX.Text;

/// <summary>Quản lý font theo họ / độ đậm / nghiêng, đo chữ và cache glyph.</summary>
public sealed partial class TextSystem
{
    readonly List<Font> _fonts = new();
    public GlyphAtlas Atlas { get; } = new();

    public bool HasFonts => _fonts.Count > 0;

    public bool TryAddFont(string path, FontWeight weight, FontFamily family = FontFamily.Sans, bool italic = false)
    {
        try
        {
            if (!File.Exists(path)) return false;
            _fonts.Add(new Font(File.ReadAllBytes(path), _fonts.Count, weight) { Family = family, Italic = italic });
            Array.Clear(_fontCache);
            _wrapIndex.Clear(); _wrapLines.Clear(); _wrapChars = 0; _wrapGen++; _styledWidth.Clear(); // font cho một kiểu chữ có thể đã đổi
            return true;
        }
        catch (NotSupportedException) { return false; }
    }

    /// <summary>Có font thật cho họ / kiểu nghiêng này (không phải lấy font khác thế vào).</summary>
    public bool HasFont(FontFamily family, bool italic)
    {
        foreach (var f in _fonts) if (f.Family == family && f.Italic == italic) return true;
        return false;
    }

    /// <summary>Font sans có độ đậm gần nhất với yêu cầu.</summary>
    public Font FontFor(FontWeight weight) => FontFor(FontFamily.Sans, weight, false);

    public Font FontFor(in TextStyle style) => FontFor(style.Family, style.Weight, style.Italic);

    /// <summary>Font khớp nhất: ưu tiên đúng họ, rồi đúng kiểu nghiêng, rồi độ đậm gần nhất.</summary>
    // FontFor chạy cho mỗi lần đo/ngắt dòng/vẽ một node chữ: nhớ kết quả theo (họ, độ đậm 100..900, nghiêng) thay vì chấm điểm mọi font.
    readonly Font?[] _fontCache = new Font?[2 * 10 * 2];

    public Font FontFor(FontFamily family, FontWeight weight, bool italic)
    {
        int w = (int)weight / 100;
        int slot = w is >= 0 and < 10 && (int)family < 2 ? ((int)family * 10 + w) * 2 + (italic ? 1 : 0) : -1;
        if (slot >= 0 && _fontCache[slot] is { } cached) return cached;
        var found = PickFont(family, weight, italic);
        if (slot >= 0 && (int)weight % 100 == 0) _fontCache[slot] = found;
        return found;
    }

    Font PickFont(FontFamily family, FontWeight weight, bool italic)
    {
        if (_fonts.Count == 0) throw new InvalidOperationException("Chưa nạp font nào.");
        Font best = _fonts[0];
        int bestScore = int.MaxValue;
        foreach (var f in _fonts)
        {
            int score = Math.Abs((int)f.Weight - (int)weight)
                + (f.Family == family ? 0 : 100_000) + (f.Italic == italic ? 0 : 10_000);
            if (score < bestScore) { best = f; bestScore = score; }
        }
        return best;
    }

    /// <summary>Độ rộng một dòng chữ (logical px), không xuống dòng.</summary>
    public float Measure(string text, in TextStyle style) => MeasureRange(text, 0, text.Length, style);

    /// <summary>Độ rộng đoạn [start, end) theo đơn vị UTF-16 (gồm cả letter-spacing sau mỗi ký tự).</summary>
    public float MeasureRange(string text, int start, int end, in TextStyle style)
    {
        var font = FontFor(style);
        float k = style.FontSize / font.UnitsPerEm, sp = style.LetterSpacing;
        bool kern = font.HasKerning;
        float w = 0;
        ushort prev = 0;
        for (int i = start; i < end;)
        {
            Rune.DecodeFromUtf16(text.AsSpan(i, end - i), out var r, out int len);
            ushort g = font.Glyph(r.Value, style.TabularNums);
            if (kern) w += font.Kern(prev, g) * k;
            w += font.AdvanceWidth(g) * k + sp;
            prev = g;
            i += len;
        }
        return w;
    }

    /// <summary>Vị trí xa nhất trong [start, end) mà đoạn [start, vị trí) vẫn rộng không quá maxWidth.</summary>
    public int FitIndex(string text, int start, int end, float maxWidth, in TextStyle style)
    {
        var font = FontFor(style);
        float k = style.FontSize / font.UnitsPerEm, sp = style.LetterSpacing, w = 0;
        bool kern = font.HasKerning;
        ushort prev = 0;
        int i = start;
        while (i < end)
        {
            Rune.DecodeFromUtf16(text.AsSpan(i, end - i), out var r, out int len);
            ushort g = font.Glyph(r.Value, style.TabularNums);
            float a = font.AdvanceWidth(g) * k + sp + (kern ? font.Kern(prev, g) * k : 0);
            if (w + a > maxWidth) break;
            w += a; i += len; prev = g;
        }
        return i;
    }

    /// <summary>Vị trí (UTF-16, theo ranh giới grapheme) gần toạ độ x nhất trong đoạn [start, end).</summary>
    public int IndexAtX(string text, int start, int end, float x, in TextStyle style)
    {
        float cur = 0;
        for (int p = start; p < end;)
        {
            int len = Math.Min(StringInfo.GetNextTextElementLength(text, p), end - p);
            if (len <= 0) break;
            float w = MeasureRange(text, p, p + len, style);
            if (x < cur + w / 2) return p;
            cur += w; p += len;
        }
        return end;
    }

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
