namespace GotX.Text;

/// <summary>
/// GSUB: thay glyph theo OpenType feature. Hiện chỉ cần thay thế đơn (lookup type 1, kể cả bọc trong Extension type 7)
/// cho "tnum" (tabular-nums: chữ số cùng bề rộng). Mỗi glyph vẫn ứng với đúng một ký tự nên chỉ số con trỏ, chọn chữ,
/// ngắt dòng không đổi. Ligature (liga, type 4) gộp nhiều ký tự thành một glyph nên chưa bật.
/// Bảng thay thế được dựng một lần cho mỗi feature (duyệt coverage), sau đó tra từ điển không cấp phát.
/// </summary>
public sealed partial class Font
{
    const uint TagTnum = 0x746E756D; // "tnum"
    Dictionary<ushort, ushort>? _tnum;

    /// <summary>Glyph của codepoint, đã áp tabular-nums nếu được yêu cầu và font có feature tnum.</summary>
    public ushort Glyph(int cp, bool tabularNums)
    {
        ushort g = GlyphIndex(cp);
        if (!tabularNums) return g;
        _tnum ??= BuildSingleSubst(TagTnum);
        return _tnum.TryGetValue(g, out ushort sub) ? sub : g;
    }

    /// <summary>Font có thay thế tnum nào không (để test / chẩn đoán).</summary>
    public bool HasTabularNums => (_tnum ??= BuildSingleSubst(TagTnum)).Count > 0;

    /// <summary>
    /// Bảng thay thế cuối cùng của một feature: lấy feature theo script "latn" (rồi "DFLT"; không có thì mọi script) –
    /// font như Segoe UI có hàng chục feature tnum cho từng hệ chữ –, áp các lookup theo thứ tự chỉ số, lookup sau nhận
    /// kết quả của lookup trước (như trình bày chữ OpenType).
    /// </summary>
    Dictionary<ushort, ushort> BuildSingleSubst(uint tag)
    {
        var result = new Dictionary<ushort, ushort>();
        int gsub = TableOffset(0x47535542); // GSUB
        if (gsub <= 0) return result;
        int features = gsub + U16(gsub + 6), lookups = gsub + U16(gsub + 8);
        var allowed = ScriptFeatures(gsub, 0x6C61746E) ?? ScriptFeatures(gsub, 0x44464C54); // latn, DFLT
        var wanted = new SortedSet<int>();
        int nFeatures = U16(features);
        for (int i = 0; i < nFeatures; i++)
        {
            int rec = features + 2 + 6 * i;
            if (U32(rec) != tag || (allowed is not null && !allowed.Contains(i))) continue;
            int f = features + U16(rec + 4);
            int count = U16(f + 2);
            for (int k = 0; k < count; k++) wanted.Add(U16(f + 4 + 2 * k));
        }
        var steps = new List<Dictionary<ushort, ushort>>();
        foreach (int index in wanted)
        {
            if (index >= U16(lookups)) continue;
            int lookup = lookups + U16(lookups + 2 + 2 * index);
            int type = U16(lookup), subCount = U16(lookup + 4);
            var map = new Dictionary<ushort, ushort>();
            for (int s = 0; s < subCount; s++)
            {
                int sub = lookup + U16(lookup + 6 + 2 * s);
                int realType = type;
                if (type == 7) { realType = U16(sub + 2); sub += (int)U32(sub + 4); } // Extension
                if (realType == 1) AddSingleSubst(sub, map);
            }
            if (map.Count > 0) steps.Add(map);
        }
        foreach (var step in steps)
            foreach (ushort key in step.Keys)
            {
                if (result.ContainsKey(key)) continue;
                ushort g = key;
                foreach (var m in steps) if (m.TryGetValue(g, out ushort next)) g = next;
                if (g != key) result[key] = g;
            }
        return result;
    }

    /// <summary>Chỉ số feature của LangSys mặc định (hoặc LangSys đầu tiên) của một script; null nếu font không có script đó.</summary>
    HashSet<int>? ScriptFeatures(int gsub, uint script)
    {
        int list = gsub + U16(gsub + 4);
        for (int i = 0; i < U16(list); i++)
        {
            int rec = list + 2 + 6 * i;
            if (U32(rec) != script) continue;
            int so = list + U16(rec + 4);
            int ls = U16(so) != 0 ? so + U16(so) : U16(so + 2) > 0 ? so + U16(so + 4 + 4) : 0;
            if (ls == 0) return null;
            var set = new HashSet<int>();
            if (U16(ls + 2) != 0xFFFF) set.Add(U16(ls + 2)); // required feature
            for (int k = 0; k < U16(ls + 4); k++) set.Add(U16(ls + 6 + 2 * k));
            return set;
        }
        return null;
    }
    /// <summary>SingleSubst format 1 (cộng delta) và format 2 (mảng glyph thay thế theo thứ tự coverage).</summary>
    void AddSingleSubst(int sub, Dictionary<ushort, ushort> map)
    {
        int fmt = U16(sub), cov = sub + U16(sub + 2);
        int covFmt = U16(cov), n = U16(cov + 2);
        int covIndex = 0;
        for (int i = 0; i < n; i++)
        {
            int first, last;
            if (covFmt == 1) { first = last = U16(cov + 4 + 2 * i); covIndex = i; }
            else { int rec = cov + 4 + 6 * i; first = U16(rec); last = U16(rec + 2); covIndex = U16(rec + 4); }
            for (int g = first; g <= last; g++, covIndex++)
            {
                ushort result;
                if (fmt == 1) result = (ushort)((g + I16(sub + 4)) & 0xFFFF);
                else if (fmt == 2 && covIndex < U16(sub + 4)) result = U16(sub + 6 + 2 * covIndex);
                else continue;
                map.TryAdd((ushort)g, result); // subtable đứng trước trong cùng lookup thắng
            }
        }
    }
}
