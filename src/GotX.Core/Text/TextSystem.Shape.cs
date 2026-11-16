using System.Globalization;
using System.Text;

namespace GotX.Text;

/// <summary>
/// Định hình một đoạn chữ thành dãy <see cref="ShapedGlyph"/>: chọn font theo từng codepoint (font chính → chuỗi dự phòng →
/// .notdef của font chính), ghép dấu tiếng Việt thành ký tự dựng sẵn khi font có glyph (như HarfBuzz), GSUB ccmp/liga/clig,
/// kerning (chỉ giữa hai glyph cùng font), GPOS mark-to-base/mark-to-mark, rồi (khi <c>visual</c>) sắp lại thứ tự hiển thị theo UAX#9.
/// Bộ đệm dùng chung và chỉ tăng: ổn định thì không cấp phát. Kết quả chỉ hợp lệ tới lần Shape kế tiếp (không tái nhập).
/// </summary>
public sealed partial class TextSystem
{
    ShapedGlyph[] _sg = new ShapedGlyph[256], _sgTmp = new ShapedGlyph[256];
    byte[] _lvl = new byte[256], _glv = new byte[256];
    int[] _order = new int[256], _gstart = new int[257];

    ShapedGlyph[]? _view;

    /// <summary>Glyph của lần Shape gần nhất (có thể là mảng của bộ đệm: chỉ đọc, hợp lệ tới lần Shape kế tiếp).</summary>
    public ShapedGlyph[] Glyphs => _view ?? _sg;

    static bool Ignorable(int cp) =>
        (cp < 0x20 && cp != '\t') || (cp >= 0x7F && cp <= 0x9F) || cp == 0xAD || cp == 0x034F || (cp >= 0x200B && cp <= 0x200F)
        || (cp >= 0x202A && cp <= 0x202E) || (cp >= 0x2060 && cp <= 0x2069) || (cp >= 0xFE00 && cp <= 0xFE0F) || cp == 0xFEFF;

    static bool AllPrintableAscii(string t, int start, int end)
    {
        for (int i = start; i < end; i++) if ((uint)(t[i] - 0x20) > 0x5E) return false;
        return true;
    }

    static bool IsNonSpacing(int cp) => cp >= 0x300 && CharUnicodeInfo.GetUnicodeCategory(cp) == UnicodeCategory.NonSpacingMark;

    /// <summary>Định hình text[start, end); trả số glyph trong <see cref="Glyphs"/>. visual = đảo thứ tự theo bidi (vẽ / caret); false = thứ tự logic (đo, ngắt dòng).</summary>
    int ShapeCore(string text, int start, int end, in TextStyle style, bool visual)
    {
        var primary = FontFor(style);
        int cap = (end - start) * 2 + 8;
        if (_sg.Length < cap) { cap = Math.Max(cap, _sg.Length * 2); _sg = new ShapedGlyph[cap]; _sgTmp = new ShapedGlyph[cap]; } // nở gấp đôi: gõ văn bản dài không cấp lại mỗi phím
        bool tnum = style.TabularNums, rtl = visual && Bidi.HasRtl(text, start, end);
        if (rtl)
        {
            if (_lvl.Length < end - start) _lvl = new byte[end - start + 64];
            Bidi.Resolve(text.AsSpan(start, end - start), _lvl);
        }
        int n = 0;
        bool needShape = false;
        bool ascii = !rtl && AllPrintableAscii(text, start, end);
        for (int i = start; i < end;)
        {
            int cp = text[i], len = 1;
            if (ascii) // đường nhanh cho chữ ASCII in được: không dấu kết hợp, không bidi, không ký tự bị bỏ qua
            {
                Font fa = primary;
                ushort ga = primary.Glyph(cp, tnum);
                if (ga == 0 && FallbackFor(cp) is { } fb0) { fa = fb0; ga = fb0.Glyph(cp, tnum); }
                ref var sa = ref _sg[n++];
                sa.Font = fa; sa.Id = ga; sa.Cluster = i; sa.ClusterEnd = i + 1; sa.Flags = 0;
                needShape |= fa.MayShape(ga);
                i++;
                continue;
            }
            if (char.IsHighSurrogate((char)cp)) { Rune.DecodeFromUtf16(text.AsSpan(i, end - i), out var r, out len); cp = r.Value; }
            else if (i + 1 < end && Vietnamese.IsComposingMark(text[i + 1]))
            {
                int q = i + 1;
                while (q < end && Vietnamese.IsComposingMark(text[q])) q++;
                char c = Vietnamese.ComposeRun(text, i, q);
                if (c != '\0' && primary.GlyphIndex(c) != 0) { cp = c; len = q - i; } // font có glyph dựng sẵn: dùng nó
            }
            if (Ignorable(cp)) { if (n > 0) _sg[n - 1].ClusterEnd = i + len; i += len; continue; }
            if (rtl && (_lvl[i - start] & 1) == 1) cp = Bidi.Mirror(cp);
            bool mark = IsNonSpacing(cp);
            Font f = primary;
            ushort g = 0;
            bool got = false;
            if (mark && n > 0 && _sg[n - 1].Font != primary)
            {
                g = _sg[n - 1].Font.Glyph(cp, tnum); // dấu đi theo font của chữ gốc để GPOS neo đúng
                if (g != 0) { f = _sg[n - 1].Font; got = true; }
            }
            if (!got)
            {
                g = primary.Glyph(cp, tnum);
                if (g == 0 && FallbackFor(cp) is { } fb) { f = fb; g = fb.Glyph(cp, tnum); }
            }
            ref var s = ref _sg[n++];
            s.Font = f; s.Id = g; s.Cluster = i; s.ClusterEnd = i + len; s.Flags = mark ? ShapedGlyph.Mark : (byte)0;
            needShape |= f.MayShape(g);
            i += len;
        }
        if (needShape) ApplyShaping(ref n, style.LetterSpacing == 0);
        Position(n);
        if (rtl) Reorder(n, start);
        return n;
    }

    void ApplyShaping(ref int n, bool ligatures)
    {
        for (int a = 0; a < n;)
        {
            var f = _sg[a].Font;
            int b = a + 1;
            while (b < n && _sg[b].Font == f) b++;
            if (f.HasLigatures) f.ApplyShaping(_sg, a, ref b, ref n, ligatures);
            a = b;
        }
    }

    /// <summary>Advance, kerning và vị trí dấu (theo thứ tự logic).</summary>
    void Position(int n)
    {
        int pen = 0, prevBase = -1;
        Font? kf = null;
        bool kern = false, marks = false;
        for (int i = 0; i < n; i++)
        {
            ref var s = ref _sg[i];
            var f = s.Font;
            if (f != kf) { kf = f; kern = f.HasKerning; marks = f.HasMarkPositioning; }
            s.Level = 0; s.XOff = s.YOff = s.Pre = 0;
            if (s.IsMark)
            {
                s.Advance = 0; // như HarfBuzz: dấu kết hợp không chiếm chỗ, đặt bằng neo GPOS
                if (marks && i > 0 && _sg[i - 1].Font == f)
                {
                    ref var t = ref _sg[i - 1];
                    if (f.TryMarkOffset(s.Id, t.Id, t.IsMark, out int dx, out int dy))
                    {
                        s.XOff = t.Pen + t.Pre + t.XOff + dx - pen;
                        s.YOff = t.YOff + dy;
                    }
                }
            }
            else
            {
                s.Advance = f.AdvanceWidth(s.Id);
                if (kern && prevBase >= 0 && _sg[prevBase].Font == f) s.Pre = f.Kern(_sg[prevBase].Id, s.Id);
                prevBase = i;
            }
            s.Pen = pen;
            pen += s.Pre + s.Advance;
        }
    }

    /// <summary>L2 theo nhóm (chữ gốc + dấu của nó đi liền nhau); kerning được tính lại cho các cặp kề nhau mới.</summary>
    void Reorder(int n, int start)
    {
        if (_order.Length < n + 1) { _order = new int[n + 16]; _glv = new byte[n + 16]; _gstart = new int[n + 17]; }
        int gc = 0;
        bool odd = false;
        for (int i = 0; i < n; i++)
        {
            _sg[i].Level = _lvl[_sg[i].Cluster - start];
            if (i > 0 && _sg[i].IsMark) continue;
            _gstart[gc] = i; _glv[gc] = _sg[i].Level; _order[gc] = gc; odd |= (_sg[i].Level & 1) == 1; gc++;
        }
        _gstart[gc] = n;
        if (!odd) return;
        Bidi.Reorder(_order.AsSpan(0, gc), _glv.AsSpan(0, gc));
        int m = 0;
        for (int p = 0; p < gc; p++)
        {
            int g = _order[p];
            if (p > 0) // kerning tính lại: chỉ giữa hai nhóm kề nhau cùng cấp (RTL: cặp logic là (phải, trái))
            {
                int prev = _order[p - 1];
                ref var a = ref _sg[_gstart[prev]]; ref var b = ref _sg[_gstart[g]];
                int pre = a.Level == b.Level && (b.Level & 1) == 1 && a.Font == b.Font && b.Font.HasKerning ? b.Font.Kern(b.Id, a.Id) : a.Level == b.Level ? b.Pre : 0;
                _sgTmp[m] = b; _sgTmp[m].Pre = pre; m++;
                for (int k = _gstart[g] + 1; k < _gstart[g + 1]; k++) _sgTmp[m++] = _sg[k];
                continue;
            }
            _sgTmp[m] = _sg[_gstart[g]]; _sgTmp[m++].Pre = 0;
            for (int k = _gstart[g] + 1; k < _gstart[g + 1]; k++) _sgTmp[m++] = _sg[k];
        }
        (_sg, _sgTmp) = (_sgTmp, _sg);
    }
}
