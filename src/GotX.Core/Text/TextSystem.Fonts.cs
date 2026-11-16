namespace GotX.Text;

/// <summary>
/// Đăng ký font theo đường dẫn, nạp LAZY: file chỉ được đọc vào RAM khi có chữ thật chọn tới font đó (FontFor) hoặc khi
/// một ký tự thiếu glyph phải tra font dự phòng. Font dự phòng (<see cref="TryAddFallbackFont"/>) nằm trong chuỗi riêng,
/// theo thứ tự đăng ký; chọn theo từng codepoint khi font chính không có glyph (cmap = 0).
/// Id font dự phòng bắt đầu từ <see cref="FallbackIdBase"/> nên khoá atlas (font, glyph, cỡ) không đụng nhau.
/// </summary>
public sealed partial class TextSystem
{
    public const int FallbackIdBase = 1 << 12;

    sealed class Entry
    {
        public string Path = "";
        public FontWeight Weight;
        public FontFamily Family;
        public bool Italic, Broken;
        public int Id;
        public Font? Font;
    }

    readonly List<Entry> _entries = new();
    readonly List<Entry> _fallbacks = new();

    public bool HasFonts => _entries.Count > 0;

    /// <summary>Số font đã nạp thật vào RAM (chính + dự phòng).</summary>
    public int ResidentFontCount
    {
        get { int n = 0; foreach (var e in _entries) if (e.Font is not null) n++; foreach (var e in _fallbacks) if (e.Font is not null) n++; return n; }
    }

    public int FallbackCount => _fallbacks.Count;

    /// <summary>Số trang cache font dự phòng đang giữ (tối đa 128 trang x 256 B).</summary>
    public int FallbackPageCount => _fbPageCount;

    public bool TryAddFont(string path, FontWeight weight, FontFamily family = FontFamily.Sans, bool italic = false)
    {
        if (!Font.Probe(path)) return false;
        InvalidateShapeCache(); _entries.Add(new Entry { Path = path, Weight = weight, Family = family, Italic = italic, Id = _entries.Count });
        FontsChanged();
        return true;
    }

    /// <summary>Thêm font dự phòng vào cuối chuỗi: dùng cho codepoint mà font chính (và các dự phòng đứng trước) không có.</summary>
    public bool TryAddFallbackFont(string path, FontWeight weight = FontWeight.Normal, bool italic = false)
    {
        if (!Font.Probe(path)) return false;
        InvalidateShapeCache(); _fallbacks.Add(new Entry { Path = path, Weight = weight, Italic = italic, Id = FallbackIdBase + _fallbacks.Count });
        FontsChanged();
        return true;
    }

    void FontsChanged()
    {
        Array.Clear(_fontCache);
        Array.Clear(_fbPages); _fbPageCount = 0;
        _wrapIndex.Clear(); _wrapLines.Clear(); _wrapChars = 0; _wrapGen++; _styledWidth.Clear(); // font cho một kiểu chữ có thể đã đổi
    }

    static Font? Load(Entry e)
    {
        if (e.Font is not null || e.Broken) return e.Font;
        try { e.Font = Font.FromFile(e.Path, e.Id, e.Weight, e.Family, e.Italic); }
        catch (Exception ex) when (ex is NotSupportedException or IOException or IndexOutOfRangeException or ArgumentOutOfRangeException) { e.Broken = true; }
        return e.Font;
    }

    /// <summary>Có font thật cho họ / kiểu nghiêng này (không phải lấy font khác thế vào).</summary>
    public bool HasFont(FontFamily family, bool italic)
    {
        foreach (var f in _entries) if (f.Family == family && f.Italic == italic) return true;
        return false;
    }

    /// <summary>Font sans có độ đậm gần nhất với yêu cầu.</summary>
    public Font FontFor(FontWeight weight) => FontFor(FontFamily.Sans, weight, false);

    public Font FontFor(in TextStyle style) => FontFor(style.Family, style.Weight, style.Italic);

    // FontFor chạy cho mỗi lần đo/ngắt dòng/vẽ một node chữ: nhớ kết quả theo (họ, độ đậm 100..900, nghiêng) thay vì chấm điểm mọi font.
    readonly Font?[] _fontCache = new Font?[2 * 10 * 2];

    /// <summary>Font khớp nhất: ưu tiên đúng họ, rồi đúng kiểu nghiêng, rồi độ đậm gần nhất.</summary>
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
        if (_entries.Count == 0) throw new InvalidOperationException("Chưa nạp font nào.");
        while (true)
        {
            Entry? best = null;
            int bestScore = int.MaxValue;
            foreach (var f in _entries)
            {
                if (f.Broken) continue;
                int score = Math.Abs((int)f.Weight - (int)weight) + (f.Family == family ? 0 : 100_000) + (f.Italic == italic ? 0 : 10_000);
                if (score < bestScore) { best = f; bestScore = score; }
            }
            if (best is null) throw new InvalidOperationException("Không font nào nạp được.");
            if (Load(best) is { } font) return font;
        }
    }

    // Font dự phòng cho từng codepoint: trang 256 codepoint, mỗi byte = chỉ số font + 1 (0 = không font nào có, 0xFF = chưa tra).
    // Tra từng ký tự khi cần nên chỉ nạp font dự phòng khi thật sự có ký tự thiếu glyph. Tối đa 128 trang (32 KB), quá thì xoá hết.
    const int MaxFallbackPages = 128;
    readonly byte[]?[] _fbPages = new byte[]?[0x1100];
    int _fbPageCount;

    /// <summary>Font dự phòng đầu tiên có glyph cho cp; null nếu không có (khi đó dùng .notdef của font chính).</summary>
    internal Font? FallbackFor(int cp)
    {
        if (_fallbacks.Count == 0 || (uint)cp > 0x10FFFF) return null;
        var page = _fbPages[cp >> 8];
        if (page is null)
        {
            if (_fbPageCount >= MaxFallbackPages) { Array.Clear(_fbPages); _fbPageCount = 0; }
            page = _fbPages[cp >> 8] = new byte[256];
            Array.Fill(page, (byte)0xFF);
            _fbPageCount++;
        }
        byte v = page[cp & 0xFF];
        if (v == 0xFF)
        {
            v = 0;
            for (int i = 0; i < _fallbacks.Count && i < 250; i++)
                if (Load(_fallbacks[i]) is { } f && f.GlyphIndex(cp) != 0) { v = (byte)(i + 1); break; }
            page[cp & 0xFF] = v;
        }
        return v == 0 ? null : _fallbacks[v - 1].Font;
    }

    /// <summary>Có font nào (chính hoặc dự phòng) vẽ được codepoint này (không phải ô vuông .notdef).</summary>
    public bool CanRender(int cp, in TextStyle style) => FontFor(style).GlyphIndex(cp) != 0 || FallbackFor(cp) is not null;
}
