namespace GotX.Text;

/// <summary>
/// Kerning: khoảng chỉnh giữa hai glyph đứng cạnh nhau (font units). Đọc từ GPOS (lookup PairPos của feature "kern",
/// format 1 theo cặp glyph và format 2 theo lớp glyph, kể cả khi bọc trong lookup Extension) và, nếu font không có GPOS,
/// từ bảng "kern" cũ (format 0). Kết quả từng cặp được nhớ lại nên mỗi cặp chỉ tra bảng một lần.
/// </summary>
public sealed partial class Font
{
    int[]? _pairLookups;   // offset tuyệt đối của các subtable PairPos (đã gỡ Extension)
    int _kernTable = -1;   // offset bảng "kern" cũ (format 0), -1 = chưa tìm, 0 = không có
    readonly Dictionary<uint, short> _kernCache = new();

    /// <summary>Có dữ liệu kerning hay không (để bỏ qua hẳn việc tra cặp khi không cần).</summary>
    public bool HasKerning
    {
        get { EnsureKerning(); return _pairLookups!.Length > 0 || _kernTable > 0; }
    }

    /// <summary>Khoảng chỉnh (font units, âm = kéo lại gần) khi glyph <paramref name="right"/> đứng ngay sau <paramref name="left"/>.</summary>
    public int Kern(ushort left, ushort right)
    {
        if (left == 0 || right == 0) return 0;
        uint key = (uint)left << 16 | right;
        if (_kernCache.TryGetValue(key, out short cached)) return cached;
        EnsureKerning();
        int value = 0;
        foreach (int sub in _pairLookups!)
            if (PairPos(sub, left, right, out value)) break;
        if (value == 0 && _pairLookups.Length == 0 && _kernTable > 0) value = LegacyKern(left, right);
        if (_kernCache.Count > 65536) _kernCache.Clear();
        _kernCache[key] = (short)value;
        return value;
    }

    void EnsureKerning()
    {
        if (_pairLookups is not null) return;
        var subs = new List<int>();
        int gpos = TableOffset(0x47504F53); // GPOS
        if (gpos > 0)
        {
            int features = gpos + U16(gpos + 6), lookups = gpos + U16(gpos + 8);
            var wanted = new HashSet<int>();
            int nFeatures = U16(features);
            for (int i = 0; i < nFeatures; i++)
            {
                int rec = features + 2 + 6 * i;
                if (U32(rec) != 0x6B65726E) continue; // "kern"
                int f = features + U16(rec + 4);
                int count = U16(f + 2);
                for (int k = 0; k < count; k++) wanted.Add(U16(f + 4 + 2 * k));
            }
            foreach (int index in wanted.OrderBy(static x => x))
            {
                if (index >= U16(lookups)) continue;
                int lookup = lookups + U16(lookups + 2 + 2 * index);
                int type = U16(lookup), subCount = U16(lookup + 4);
                for (int s = 0; s < subCount; s++)
                {
                    int sub = lookup + U16(lookup + 6 + 2 * s);
                    int realType = type;
                    if (type == 9) { realType = U16(sub + 2); sub += (int)U32(sub + 4); } // Extension
                    if (realType == 2) subs.Add(sub);
                }
            }
        }
        _pairLookups = subs.ToArray();
        _kernTable = TableOffset(0x6B65726E); // "kern"
    }

    int TableOffset(uint tag)
    {
        int numTables = U16(4);
        for (int i = 0; i < numTables; i++)
            if (U32(12 + 16 * i) == tag) return (int)U32(12 + 16 * i + 8);
        return 0;
    }

    static int ValueSize(int format) => System.Numerics.BitOperations.PopCount((uint)format) * 2;

    /// <summary>XAdvance trong một ValueRecord (0 nếu format không có trường này).</summary>
    int XAdvance(int record, int format)
    {
        if ((format & 4) == 0) return 0;
        int skip = System.Numerics.BitOperations.PopCount((uint)(format & 3)) * 2;
        return I16(record + skip);
    }

    bool PairPos(int sub, ushort left, ushort right, out int value)
    {
        value = 0;
        int fmt = U16(sub);
        int coverage = CoverageIndex(sub + U16(sub + 2), left);
        if (coverage < 0) return false;
        int vf1 = U16(sub + 4), vf2 = U16(sub + 6);
        int size1 = ValueSize(vf1), size2 = ValueSize(vf2);
        if (fmt == 1)
        {
            if (coverage >= U16(sub + 8)) return false;
            int set = sub + U16(sub + 10 + 2 * coverage);
            int count = U16(set), stride = 2 + size1 + size2;
            int lo = 0, hi = count - 1;
            while (lo <= hi) // các cặp xếp theo glyph thứ hai
            {
                int mid = (lo + hi) >> 1, rec = set + 2 + stride * mid;
                int g = U16(rec);
                if (g < right) lo = mid + 1;
                else if (g > right) hi = mid - 1;
                else { value = XAdvance(rec + 2, vf1); return true; }
            }
            return false;
        }
        if (fmt == 2)
        {
            int c1 = ClassOf(sub + U16(sub + 8), left), c2 = ClassOf(sub + U16(sub + 10), right);
            int class1Count = U16(sub + 12), class2Count = U16(sub + 14);
            if (c1 >= class1Count || c2 >= class2Count) return false;
            int rec = sub + 16 + (c1 * class2Count + c2) * (size1 + size2);
            value = XAdvance(rec, vf1);
            return true; // format 2 phủ mọi cặp có glyph đầu trong coverage (giá trị 0 vẫn là câu trả lời)
        }
        return false;
    }

    int CoverageIndex(int cov, ushort g)
    {
        int fmt = U16(cov), count = U16(cov + 2);
        int lo = 0, hi = count - 1;
        if (fmt == 1)
        {
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1, v = U16(cov + 4 + 2 * mid);
                if (v < g) lo = mid + 1; else if (v > g) hi = mid - 1; else return mid;
            }
            return -1;
        }
        while (lo <= hi) // format 2: các dải [start, end] kèm chỉ số bắt đầu
        {
            int mid = (lo + hi) >> 1, rec = cov + 4 + 6 * mid;
            int start = U16(rec), end = U16(rec + 2);
            if (g < start) hi = mid - 1; else if (g > end) lo = mid + 1; else return U16(rec + 4) + g - start;
        }
        return -1;
    }

    int ClassOf(int def, ushort g)
    {
        int fmt = U16(def);
        if (fmt == 1)
        {
            int start = U16(def + 2), count = U16(def + 4);
            return g >= start && g < start + count ? U16(def + 6 + 2 * (g - start)) : 0;
        }
        int ranges = U16(def + 2), lo = 0, hi = ranges - 1;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1, rec = def + 4 + 6 * mid;
            int start = U16(rec), end = U16(rec + 2);
            if (g < start) hi = mid - 1; else if (g > end) lo = mid + 1; else return U16(rec + 4);
        }
        return 0;
    }

    /// <summary>Bảng "kern" kiểu Microsoft (version 0), subtable format 0 nằm ngang: danh sách cặp đã sắp xếp.</summary>
    int LegacyKern(ushort left, ushort right)
    {
        int t = _kernTable;
        if (U16(t) != 0) return 0;
        int n = U16(t + 2), sub = t + 4;
        for (int i = 0; i < n; i++)
        {
            int length = U16(sub + 2), coverage = U16(sub + 4);
            if ((coverage >> 8) == 0 && (coverage & 1) == 1)
            {
                int pairs = U16(sub + 6), lo = 0, hi = pairs - 1;
                uint key = (uint)left << 16 | right;
                while (lo <= hi)
                {
                    int mid = (lo + hi) >> 1, rec = sub + 14 + 6 * mid;
                    uint k = U32(rec);
                    if (k < key) lo = mid + 1; else if (k > key) hi = mid - 1; else return I16(rec + 4);
                }
            }
            sub += length;
        }
        return 0;
    }
}
