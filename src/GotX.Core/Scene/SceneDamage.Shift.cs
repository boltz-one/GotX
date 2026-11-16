namespace GotX;

/// <summary>
/// Cuộn bằng blit: Window gợi ý "vùng R đã cuộn dy hàng" (<see cref="ScrollShift"/>); ở đây KIỂM CHỨNG rồi mới dùng. Ghép từng primitive của scene mới với
/// scene cũ (khớp hash, rồi so từng trường): bản dời đúng dy pixel nguyên (Y, tâm, đáy cộng đúng dy trong float, nên raster ra đúng bản dịch của raster cũ)
/// hoặc bản y hệt tại chỗ. Pixel trong R lấy từ canvas cũ dời dy; hỏng = dải mới lộ ra + hộp của mọi primitive không ghép được (mới tại chỗ; cũ tại chỗ và
/// tại nơi pixel cũ vừa dời tới) + primitive đứng yên chồng lên R mà không phải một màu phẳng phủ kín R + vùng góc của vùng cắt hình đứng yên.
/// Thứ tự vẽ giữ nguyên vì chuỗi chỉ số cũ của các cặp ghép được luôn tăng. Không ghép được cả frame (kể cả gợi ý sai) → null, đi đường hash thường.
/// </summary>
public sealed partial class SceneDamage
{
    const int MaxShiftPrims = 100_000;
    /// <summary>Đếm để chẩn đoán (Stress in ra): số lần thử / thành công / cửa sổ từ điển / primitive không ghép được / ticks.</summary>
    public static int ShiftTried, ShiftOk, ShiftDict, ShiftLost, ShiftPrims; public static long ShiftTicks;

    /// <summary>Bật cho renderer giữ canvas và chép được vùng cuộn (RenderDamaged); renderer GPU để tắt.</summary>
    public bool AcceptShift { get; set; }

    int[] _chain = [];
    int _winStart, _winEnd;
    const int Window = 1536;
    /// <summary>Một primitive ghép tốn ~ chừng này pixel raster: vùng cuộn nhỏ hơn (số primitive × giá trị này) thì không thử. 0 = luôn thử (test).</summary>
    public int ShiftPrimCostPx { get; set; } = 100;
    int _shiftSkip, _shiftFails;
    bool[] _oUsed = [];
    readonly Dictionary<ulong, int> _head = new();

    Result? TryShift(Scene scene, ScrollShift sh, int width, int height, int tx, int ty)
    {
        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        ShiftTried++;
        var r = TryShiftCore(scene, sh, width, height, tx, ty);
        if (r is null) _shiftSkip = Math.Min(32, 1 << Math.Min(5, ++_shiftFails)); else _shiftFails = 0;
        if (r is not null) ShiftOk++;
        ShiftTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
        return r;
    }

    Result? TryShiftCore(Scene scene, ScrollShift sh, int width, int height, int tx, int ty)
    {
        var old = sh.Old;
        int dy = sh.Dy, n = scene.Count, m = old.Count;
        if (dy == 0 || n == 0 || m == 0 || n > MaxShiftPrims || m > MaxShiftPrims) return null;
        int rx0 = Math.Max(0, (int)MathF.Floor(sh.Region.X)), ry0 = Math.Max(0, (int)MathF.Floor(sh.Region.Y));
        int rx1 = Math.Min(width, (int)MathF.Ceiling(sh.Region.Right)), ry1 = Math.Min(height, (int)MathF.Ceiling(sh.Region.Bottom));
        if (rx1 <= rx0 || ry1 - ry0 <= Math.Abs(dy)) return null;
        // Phép ghép tốn ~50 ns / primitive, raster ~2-3 ns / pixel: chỉ đáng khi vùng cuộn đủ lớn so với số primitive; thất bại thì lùi dần (như FullHint).
        if (_shiftSkip > 0) { _shiftSkip--; return null; }
        if ((long)n * ShiftPrimCostPx > (long)(rx1 - rx0) * (ry1 - ry0)) return null;
        var region = new Bounds(rx0, ry0, rx1 - rx0, ry1 - ry0);
        var prims = scene.Primitives; var oprims = old.Primitives;

        EnsureCapacity(n);
        if (_chain.Length < m) { _chain = new int[m + m / 4]; _oUsed = new bool[m + m / 4]; }
        Array.Clear(_oUsed, 0, m);
        if (_dirty.Length < tx * ty) _dirty = new bool[tx * ty];
        Array.Clear(_dirty, 0, tx * ty);

        for (int j = 0; j < n; j++) _curBox[j] = SceneDamageMath.Visual(scene, prims[j]);
        int ptr = 0, last = -1, lost = 0;
        bool dict = false; int dicts = 0;
        for (int j = 0; j < n; j++)
        {
            ref readonly var p = ref prims[j];
            var box = _curBox[j];
            if (box.IsEmpty) continue; // không chạm pixel nào (bị cắt hết): không cần ghép
            bool inR = !box.Intersect(region).IsEmpty;
            // Đường nhanh: cặp kế tiếp theo thứ tự (cuộn thuần: hai chuỗi primitive thẳng hàng); bỏ qua primitive cũ vô hình đứng chắn ở ptr.
            bool moving = false;
            int at = ptr < m && Probe(scene, old, p, oprims[ptr], dy, inR, region, out moving) ? ptr : -1;
            if (at < 0)
            {
                int q = ptr;
                while (q < m && SceneDamageMath.Visual(old, oprims[q]).IsEmpty) q++;
                if (q != ptr && q < m && Probe(scene, old, p, oprims[q], dy, inR, region, out moving)) at = q;
                ptr = q;
            }
            if (at < 0)
            {
                // Lệch hàng (view mới vào / cũ ra): dựng từ điển hash của một cửa sổ primitive cũ chưa duyệt rồi tra.
                if (!dict || ptr < _winStart || (ptr > _winEnd - 256 && _winEnd < m)) { BuildOldIndex(old, oprims, ptr, Math.Min(m, ptr + Window)); dict = true; dicts++; }
                if (inR) { at = FindShifted(scene, old, p, oprims, last, dy, region); moving = at >= 0; }
                if (at < 0) at = FindStatic(scene, old, p, SceneDamageMath.Hash(scene, p), oprims, last, region);
            }
            if (at < 0)
            {
                Mark(box, tx, ty);
                if (++lost > 64 && lost * 3 > n) return null; // phần lớn không ghép được: không phải cuộn thuần
                continue;
            }
            _oUsed[at] = true; last = at; ptr = at + 1;
            if (!inR) continue;
            if (moving)
            {
                if (p.ClipShape != 0 && StaticShape(scene, old, p, oprims[at])) MarkShapeCorners(scene.Shape(p.ClipShape), tx, ty);
            }
            else if (!UniformOver(p, region)) { Mark(box, tx, ty); Mark(box.Offset(0, dy).Intersect(region), tx, ty); }
        }
        for (int i = 0; i < m; i++)
        {
            if (_oUsed[i]) continue;
            var ob = SceneDamageMath.Visual(old, oprims[i]);
            if (ob.IsEmpty) continue;
            if (!Within(ob, region)) Mark(ob, tx, ty);
            Mark(ob.Offset(0, dy).Intersect(region), tx, ty);
        }
        // Dải mới lộ ra: hàng của R mà nguồn (hàng - dy) nằm ngoài R.
        Mark(dy > 0 ? new Bounds(rx0, ry0, rx1 - rx0, dy) : new Bounds(rx0, ry1 + dy, rx1 - rx0, -dy), tx, ty);

        int count = 0;
        for (int i = 0; i < tx * ty; i++) if (_dirty[i]) count++;
        _prevCount = -1; // không băm scene mới: lần so hash kế tiếp (nếu có) gieo lại
        _hashedNow = false;
        _wideStreak = 0;
        ShiftPrims += n; ShiftLost += lost; ShiftDict += dicts;
        return new Result(false, false, tx, ty, _dirty, count) { Boxes = _curBox, ShiftRect = region, ShiftDy = dy };
    }

    static bool Within(in Bounds a, in Bounds r) => a.X >= r.X && a.Y >= r.Y && a.Right <= r.Right && a.Bottom <= r.Bottom;

    /// <summary>Thử ghép <paramref name="p"/> với primitive cũ <paramref name="o"/>: bản dời dy (nếu chạm R; thử các biến thể vùng cắt) hoặc bản y hệt tại chỗ.</summary>
    static bool Probe(Scene sn, Scene so, in Primitive p, in Primitive o, int dy, bool inR, in Bounds region, out bool moving)
    {
        moving = false;
        if (inR)
            for (int cs = 0; cs < 2; cs++)
                for (int ss = 0; ss < (p.ClipShape != 0 ? 2 : 1); ss++)
                    if (Same(so, o, sn, p, dy, cs == 1, ss == 1, region)) { moving = true; return true; }
        return Same(so, o, sn, p, 0, false, false, region);
    }


    /// <summary>Hash các primitive cũ [from, to) vào từ điển (đầu chuỗi = chỉ số nhỏ nhất). Chỉ một cửa sổ quanh ptr: view vào / ra mỗi frame chỉ lệch vài trăm primitive.</summary>
    void BuildOldIndex(Scene old, ReadOnlySpan<Primitive> oprims, int from, int to)
    {
        _head.Clear();
        _winStart = from; _winEnd = to;
        for (int i = to - 1; i >= from; i--)
        {
            ulong h = SceneDamageMath.Hash(old, oprims[i]);
            _chain[i] = _head.TryGetValue(h, out int first) ? first : -1;
            _head[h] = i;
        }
    }

    /// <summary>Thử các biến thể "bản dời dy": vùng cắt thẳng đứng yên hay dời theo, vùng cắt hình đứng yên hay dời theo (hash của bản dời ngược, rồi so từng trường).</summary>
    int FindShifted(Scene sn, Scene so, in Primitive p, ReadOnlySpan<Primitive> oprims, int last, int dy, in Bounds region)
    {
        for (int cs = 0; cs < 2; cs++)
            for (int ss = 0; ss < (p.ClipShape != 0 ? 2 : 1); ss++)
            {
                var q = p;
                q.Bounds = p.Bounds.Offset(0, -dy);
                if (p.HasExclusion) q.Source = p.Source.Offset(0, -dy);
                if (cs == 1) q.Clip = p.Clip.Offset(0, -dy);
                ulong h = SceneDamageMath.Hash(sn, q, ss == 1 ? dy : 0);
                if (!_head.TryGetValue(h, out int i)) continue;
                for (; i >= 0; i = _chain[i])
                    if (i > last && !_oUsed[i] && Same(so, oprims[i], sn, p, dy, cs == 1, ss == 1, region)) return i;
            }
        return -1;
    }

    int FindStatic(Scene sn, Scene so, in Primitive p, ulong h, ReadOnlySpan<Primitive> oprims, int last, in Bounds region)
    {
        if (!_head.TryGetValue(h, out int i)) return -1;
        for (; i >= 0; i = _chain[i])
            if (i > last && !_oUsed[i] && Same(so, oprims[i], sn, p, 0, false, false, region)) return i;
        return -1;
    }
}
