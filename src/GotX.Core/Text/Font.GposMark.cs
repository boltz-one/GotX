namespace GotX.Text;

/// <summary>
/// GPOS mark-to-base (type 4) và mark-to-mark (type 6) của feature mark/mkmk: dấu kết hợp (dấu thanh tiếng Việt, v.v.)
/// đặt theo điểm neo của chữ cái gốc / dấu liền trước. Kết quả từng cặp được nhớ (tối đa 16384 cặp, vượt thì xoá).
/// Chưa hỗ trợ mark-to-ligature (type 5) và neo có device table.
/// </summary>
public sealed partial class Font
{
    int[]? _baseMarkSubs, _markMarkSubs;
    readonly Dictionary<ulong, (int Dx, int Dy, bool Ok)> _markCache = new();

    public bool HasMarkPositioning { get { EnsureMarkLookups(); return _baseMarkSubs!.Length + _markMarkSubs!.Length > 0; } }

    void EnsureMarkLookups()
    {
        if (_baseMarkSubs is not null) return;
        var bs = new List<int>(); var ms = new List<int>();
        int gpos = TableOffset(0x47504F53);
        if (gpos > 0)
        {
            int features = gpos + U16(gpos + 6), lookups = gpos + U16(gpos + 8);
            var wanted = new SortedSet<int>();
            for (int i = 0, nf = U16(features); i < nf; i++)
            {
                int rec = features + 2 + 6 * i;
                uint tag = U32(rec);
                if (tag != 0x6D61726B && tag != 0x6D6B6D6B) continue; // mark, mkmk
                int f = features + U16(rec + 4);
                for (int k = 0, c = U16(f + 2); k < c; k++) wanted.Add(U16(f + 4 + 2 * k));
            }
            foreach (int index in wanted)
            {
                if (index >= U16(lookups)) continue;
                int lookup = lookups + U16(lookups + 2 + 2 * index);
                int type = U16(lookup);
                for (int s = 0, sc = U16(lookup + 4); s < sc; s++)
                {
                    int sub = lookup + U16(lookup + 6 + 2 * s), real = type;
                    if (type == 9) { real = U16(sub + 2); sub += (int)U32(sub + 4); }
                    if (real == 4) bs.Add(sub); else if (real == 6) ms.Add(sub);
                }
            }
        }
        _markMarkSubs = ms.ToArray();
        _baseMarkSubs = bs.ToArray();
    }

    /// <summary>Độ dời (font units) để điểm neo của dấu trùng điểm neo của glyph gốc (hoặc dấu gốc nếu <paramref name="markToMark"/>).</summary>
    internal bool TryMarkOffset(ushort mark, ushort target, bool markToMark, out int dx, out int dy)
    {
        EnsureMarkLookups();
        ulong key = (ulong)mark << 17 | (ulong)target << 1 | (markToMark ? 1u : 0);
        if (_markCache.TryGetValue(key, out var hit)) { dx = hit.Dx; dy = hit.Dy; return hit.Ok; }
        dx = dy = 0;
        bool ok = false;
        foreach (int sub in markToMark ? _markMarkSubs! : _baseMarkSubs!)
        {
            int mi = CoverageIndex(sub + U16(sub + 2), mark), bi = CoverageIndex(sub + U16(sub + 4), target);
            if (mi < 0 || bi < 0) continue;
            int classCount = U16(sub + 6), markArray = sub + U16(sub + 8), baseArray = sub + U16(sub + 10);
            if (mi >= U16(markArray) || bi >= U16(baseArray)) continue;
            int rec = markArray + 2 + 4 * mi, cls = U16(rec);
            if (cls >= classCount) continue;
            int markAnchor = markArray + U16(rec + 2);
            int baseOff = U16(baseArray + 2 + 2 * (bi * classCount + cls));
            if (baseOff == 0) continue;
            int baseAnchor = baseArray + baseOff;
            dx = I16(baseAnchor + 2) - I16(markAnchor + 2);
            dy = I16(baseAnchor + 4) - I16(markAnchor + 4);
            ok = true; break;
        }
        if (_markCache.Count >= 16384) _markCache.Clear();
        _markCache[key] = (dx, dy, ok);
        return ok;
    }
}
