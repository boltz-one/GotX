using System.Buffers;
using System.Buffers.Binary;

namespace GotX.Text;

/// <summary>Đích nhận outline glyph (đã biến đổi sang toạ độ bitmap).</summary>
internal interface IOutlineSink
{
    void MoveTo(float x, float y);
    void LineTo(float x, float y);
    void QuadTo(float cx, float cy, float x, float y);
    void Close();
}

/// <summary>Ma trận affine 2D: x' = A·x + C·y + E, y' = B·x + D·y + F.</summary>
internal readonly record struct Affine(float A, float B, float C, float D, float E, float F)
{
    public (float, float) Apply(float x, float y) => (A * x + C * y + E, B * x + D * y + F);

    /// <summary>this ∘ inner: áp inner trước, rồi this.</summary>
    public Affine Then(in Affine inner) => new(
        A * inner.A + C * inner.B, B * inner.A + D * inner.B,
        A * inner.C + C * inner.D, B * inner.C + D * inner.D,
        A * inner.E + C * inner.F + E, B * inner.E + D * inner.F + F);
}

/// <summary>
/// Font TrueType (outline glyf) tự parse: head, hhea, maxp, hmtx, cmap (format 4/12), loca, glyf
/// kể cả glyph ghép (composite) – cần cho chữ Việt có dấu như "ệ", "ở".
/// Kerning đọc từ GPOS / bảng kern (Font.Kern.cs), tnum từ GSUB (Font.Gsub.cs). Chưa hỗ trợ: CFF/OTF, TTC, ligature.
/// </summary>
public sealed partial class Font
{
    readonly byte[] _d;
    readonly int _hmtx, _loca, _glyf, _cmap, _numHMetrics, _numGlyphs;
    readonly bool _locaLong;
    readonly ushort _cmapFormat;

    public int Id { get; }
    public FontWeight Weight { get; }
    public FontFamily Family { get; init; }
    public bool Italic { get; init; }
    public ushort UnitsPerEm { get; }
    public short Ascender { get; }
    public short Descender { get; }

    public static Font Load(string path, int id, FontWeight weight) => new(File.ReadAllBytes(path), id, weight);

    public Font(byte[] data, int id, FontWeight weight)
    {
        _d = data; Id = id; Weight = weight;
        if (U32(0) == 0x74746366) throw new NotSupportedException("TTC chưa được hỗ trợ.");
        int numTables = U16(4);
        int head = 0, hhea = 0, maxp = 0;
        for (int i = 0; i < numTables; i++)
        {
            int rec = 12 + 16 * i;
            uint tag = U32(rec);
            int off = (int)U32(rec + 8);
            switch (tag)
            {
                case 0x68656164: head = off; break;   // head
                case 0x68686561: hhea = off; break;   // hhea
                case 0x6D617870: maxp = off; break;   // maxp
                case 0x686D7478: _hmtx = off; break;  // hmtx
                case 0x6C6F6361: _loca = off; break;  // loca
                case 0x676C7966: _glyf = off; break;  // glyf
                case 0x636D6170: _cmap = off; break;  // cmap
            }
        }
        if (head == 0 || hhea == 0 || _glyf == 0 || _loca == 0 || _cmap == 0)
            throw new NotSupportedException("Chỉ hỗ trợ font TrueType có bảng glyf.");

        UnitsPerEm = U16(head + 18);
        _locaLong = I16(head + 50) != 0;
        Ascender = I16(hhea + 4);
        Descender = I16(hhea + 6);
        _numHMetrics = U16(hhea + 34);
        _numGlyphs = U16(maxp + 4);

        // Chọn subtable cmap: ưu tiên format 12 (Unicode đầy đủ), sau đó format 4 (BMP).
        int best4 = 0, best12 = 0;
        int n = U16(_cmap + 2);
        for (int i = 0; i < n; i++)
        {
            int rec = _cmap + 4 + 8 * i;
            int platform = U16(rec), enc = U16(rec + 2);
            int sub = _cmap + (int)U32(rec + 4);
            bool unicode = platform == 0 || (platform == 3 && (enc == 1 || enc == 10));
            if (!unicode) continue;
            ushort fmt = U16(sub);
            if (fmt == 12) best12 = sub;
            else if (fmt == 4 && best4 == 0) best4 = sub;
        }
        _cmap = best12 != 0 ? best12 : best4;
        if (_cmap == 0) throw new NotSupportedException("Không có cmap Unicode.");
        _cmapFormat = U16(_cmap);
    }

    ushort U16(int o) => BinaryPrimitives.ReadUInt16BigEndian(_d.AsSpan(o));
    short I16(int o) => BinaryPrimitives.ReadInt16BigEndian(_d.AsSpan(o));
    uint U32(int o) => BinaryPrimitives.ReadUInt32BigEndian(_d.AsSpan(o));
    float F2Dot14(int o) => I16(o) / 16384f;

    /// <summary>Codepoint → glyph id (0 = .notdef).</summary>
    public ushort GlyphIndex(int cp)
    {
        if (_cmapFormat == 12)
        {
            int nGroups = (int)U32(_cmap + 12);
            int lo = 0, hi = nGroups - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1, g = _cmap + 16 + 12 * mid;
                uint start = U32(g), end = U32(g + 4);
                if (cp < start) hi = mid - 1;
                else if (cp > end) lo = mid + 1;
                else return (ushort)(U32(g + 8) + (cp - start));
            }
            return 0;
        }
        if (cp > 0xFFFF) return 0;
        int segX2 = U16(_cmap + 6), segCount = segX2 / 2;
        int endO = _cmap + 14, startO = endO + segX2 + 2, deltaO = startO + segX2, rangeO = deltaO + segX2;
        int l = 0, h = segCount - 1;
        while (l < h) // tìm segment đầu tiên có endCode >= cp
        {
            int m = (l + h) >> 1;
            if (U16(endO + 2 * m) < cp) l = m + 1; else h = m;
        }
        int start4 = U16(startO + 2 * l);
        if (cp < start4 || cp > U16(endO + 2 * l)) return 0;
        int delta = U16(deltaO + 2 * l), ro = U16(rangeO + 2 * l);
        if (ro == 0) return (ushort)((cp + delta) & 0xFFFF);
        int glyph = U16(rangeO + 2 * l + ro + 2 * (cp - start4));
        return glyph == 0 ? (ushort)0 : (ushort)((glyph + delta) & 0xFFFF);
    }

    public int AdvanceWidth(ushort glyph) =>
        U16(_hmtx + 4 * Math.Min(glyph, _numHMetrics - 1));

    (int start, int end) GlyphRange(ushort g)
    {
        if (g >= _numGlyphs) return (0, 0);
        return _locaLong
            ? ((int)U32(_loca + 4 * g), (int)U32(_loca + 4 * g + 4))
            : (U16(_loca + 2 * g) * 2, U16(_loca + 2 * g + 2) * 2);
    }

    /// <summary>Hộp bao glyph (font units, y hướng lên). false nếu glyph rỗng (vd. dấu cách).</summary>
    public bool TryGetBox(ushort g, out short xMin, out short yMin, out short xMax, out short yMax)
    {
        var (s, e) = GlyphRange(g);
        if (e <= s) { xMin = yMin = xMax = yMax = 0; return false; }
        int o = _glyf + s;
        xMin = I16(o + 2); yMin = I16(o + 4); xMax = I16(o + 6); yMax = I16(o + 8);
        return true;
    }
}
