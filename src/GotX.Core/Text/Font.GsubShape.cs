namespace GotX.Text;

/// <summary>
/// GSUB cho định hình: ccmp (bắt buộc) + liga/clig (tuỳ chọn, tắt khi có letter-spacing như CSS) với lookup type 1 (đơn),
/// 2 (nhiều) và 4 (ligature), kể cả bọc Extension (7). Lookup theo thứ tự chỉ số; chưa hỗ trợ type 5/6 (ngữ cảnh) và cờ
/// lookup bỏ qua dấu. Bộ glyph "có thể bị thay" (<c>_shapeFirst</c>) dựng một lần để chữ Latin thường bỏ qua toàn bộ bước này.
/// </summary>
public sealed partial class Font
{
    readonly record struct ShapeLookup(int[] Subs, int[] Types, bool Optional);
    ShapeLookup[]? _shapeLookups;
    bool[]? _shapeFirst;

    /// <summary>Font có lookup ccmp/liga/clig nào (để biết có cần kéo dài vùng đo quanh caret hay không).</summary>
    public bool HasLigatures { get { EnsureShapeLookups(); return _shapeLookups!.Length > 0; } }

    /// <summary>Glyph này có thể là đầu của một phép thay thế ccmp/liga/clig.</summary>
    internal bool MayShape(ushort g)
    {
        EnsureShapeLookups();
        var f = _shapeFirst;
        return f is not null && g < f.Length && f[g];
    }

    void EnsureShapeLookups()
    {
        if (_shapeLookups is not null) return;
        var result = new List<ShapeLookup>();
        int gsub = TableOffset(0x47535542);
        if (gsub > 0)
        {
            int features = gsub + U16(gsub + 6), lookups = gsub + U16(gsub + 8);
            var allowed = ScriptFeatures(gsub, 0x6C61746E) ?? ScriptFeatures(gsub, 0x44464C54);
            var wanted = new SortedDictionary<int, bool>(); // chỉ số lookup → tuỳ chọn?
            for (int i = 0, nf = U16(features); i < nf; i++)
            {
                int rec = features + 2 + 6 * i;
                uint tag = U32(rec);
                if ((tag != TagCcmp && tag != TagLiga && tag != TagClig) || (allowed is not null && !allowed.Contains(i))) continue;
                int f = features + U16(rec + 4);
                for (int k = 0, c = U16(f + 2); k < c; k++)
                {
                    int li = U16(f + 4 + 2 * k);
                    wanted[li] = tag != TagCcmp && (!wanted.TryGetValue(li, out bool opt) || opt);
                }
            }
            foreach (var (index, optional) in wanted)
            {
                if (index >= U16(lookups)) continue;
                int lookup = lookups + U16(lookups + 2 + 2 * index);
                int type = U16(lookup), subCount = U16(lookup + 4);
                var subs = new List<int>(); var types = new List<int>();
                for (int s = 0; s < subCount; s++)
                {
                    int sub = lookup + U16(lookup + 6 + 2 * s), real = type;
                    if (type == 7) { real = U16(sub + 2); sub += (int)U32(sub + 4); }
                    if (real is 1 or 2 or 4) { subs.Add(sub); types.Add(real); }
                }
                if (subs.Count > 0) result.Add(new ShapeLookup(subs.ToArray(), types.ToArray(), optional));
            }
        }
        if (result.Count > 0)
        {
            var first = new bool[_numGlyphs];
            foreach (var l in result) foreach (int sub in l.Subs) MarkCoverage(sub + U16(sub + 2), first);
            _shapeFirst = first;
        }
        _shapeLookups = result.ToArray();
    }

    void MarkCoverage(int cov, bool[] set)
    {
        int fmt = U16(cov), n = U16(cov + 2);
        for (int i = 0; i < n; i++)
        {
            if (fmt == 1) { int g = U16(cov + 4 + 2 * i); if (g < set.Length) set[g] = true; continue; }
            int rec = cov + 4 + 6 * i;
            for (int g = U16(rec), e = U16(rec + 2); g <= e && g < set.Length; g++) set[g] = true;
        }
    }

    const uint TagCcmp = 0x63636D70, TagLiga = 0x6C696761, TagClig = 0x636C6967;

    /// <summary>Áp lookup lên buf[a, b) (cùng một font); sửa tại chỗ, cập nhật tổng số glyph <paramref name="total"/> và b.</summary>
    internal void ApplyShaping(ShapedGlyph[] buf, int a, ref int b, ref int total, bool ligatures)
    {
        EnsureShapeLookups();
        foreach (var lk in _shapeLookups!)
        {
            if (lk.Optional && !ligatures) continue;
            for (int i = a; i < b; i++)
            {
                ushort g = buf[i].Id;
                if (g >= _shapeFirst!.Length || !_shapeFirst[g]) continue;
                for (int s = 0; s < lk.Subs.Length; s++)
                    if (ApplyOne(lk.Subs[s], lk.Types[s], buf, i, ref b, ref total)) break;
            }
        }
    }

    bool ApplyOne(int sub, int type, ShapedGlyph[] buf, int i, ref int b, ref int total)
    {
        ushort g = buf[i].Id;
        int ci = CoverageIndex(sub + U16(sub + 2), g);
        if (ci < 0) return false;
        if (type == 1)
        {
            if (U16(sub) == 1) buf[i].Id = (ushort)((g + I16(sub + 4)) & 0xFFFF);
            else if (ci < U16(sub + 4)) buf[i].Id = U16(sub + 6 + 2 * ci);
            else return false;
            return true;
        }
        if (type == 2)
        {
            if (ci >= U16(sub + 4)) return false;
            int seq = sub + U16(sub + 6 + 2 * ci), n = U16(seq);
            if (n == 0 || total + n - 1 > buf.Length) return false;
            int extra = n - 1;
            Array.Copy(buf, i + 1, buf, i + 1 + extra, total - i - 1);
            var src = buf[i];
            for (int k = 0; k < n; k++) { buf[i + k] = src; buf[i + k].Id = U16(seq + 2 + 2 * k); }
            total += extra; b += extra;
            return true;
        }
        if (ci >= U16(sub + 4)) return false;
        int set = sub + U16(sub + 6 + 2 * ci);
        for (int l = 0, lc = U16(set); l < lc; l++)
        {
            int lig = set + U16(set + 2 + 2 * l), comps = U16(lig + 2);
            if (comps < 2 || i + comps > b) continue;
            bool ok = true;
            for (int k = 1; k < comps && ok; k++) ok = buf[i + k].Id == U16(lig + 2 + 2 * (k - 1) + 2);
            if (!ok) continue;
            buf[i].Id = U16(lig);
            buf[i].ClusterEnd = buf[i + comps - 1].ClusterEnd;
            Array.Copy(buf, i + comps, buf, i + 1, total - i - comps);
            total -= comps - 1; b -= comps - 1;
            return true;
        }
        return false;
    }
}
