namespace GotX;

/// <summary>
/// Vùng hỏng giữa hai frame (prototype, mặc định TẮT ở mọi renderer). Chỉ giữ hash + hộp nhìn thấy của frame trước (không giữ Scene, vì Window
/// tái dùng Scene), so với frame mới: cắt tiền tố / hậu tố giống nhau, phần giữa đối chiếu theo tập đa hợp hash; primitive thêm / bớt / đổi
/// (cả bản cũ lẫn bản mới, gồm bóng mờ, vùng cắt, biến đổi) đánh dấu ô 64×64 hỏng. Thứ tự vẽ của phần còn lại đổi → Full (an toàn).
/// Kết quả đúng từng pixel với vẽ toàn frame vì renderer chỉ vẽ lại các ô hỏng bằng đúng mọi primitive chạm vào ô đó.
/// </summary>
public sealed class SceneDamage
{
    public const int Tile = 64;

    public readonly struct Result(bool full, bool empty, int tx, int ty, bool[]? dirty, int dirtyCount)
    {
        /// <summary>Phải vẽ lại cả frame (frame đầu, đổi kích thước / nền / atlas, đổi thứ tự).</summary>
        public bool Full { get; } = full;
        /// <summary>Không có gì khác frame trước: không cần vẽ, không cần present.</summary>
        public bool Empty { get; } = empty;
        public int TilesX { get; } = tx;
        public int TilesY { get; } = ty;
        /// <summary>Lưới ô hỏng (TilesX × TilesY, hàng trước); null nếu Full.</summary>
        public bool[]? Dirty { get; } = dirty;
        public int DirtyTiles { get; } = dirtyCount;
        public double Coverage => Full ? 1 : TilesX * TilesY == 0 ? 0 : DirtyTiles / (double)(TilesX * TilesY);

        /// <summary>Hộp bao (px thiết bị, bội của Tile, đã cắt theo kích thước) của mọi ô hỏng.</summary>
        public Bounds BoundingRect(int width, int height)
        {
            if (Full || Dirty is null) return new Bounds(0, 0, width, height);
            int x0 = TilesX, y0 = TilesY, x1 = -1, y1 = -1;
            for (int y = 0; y < TilesY; y++)
                for (int x = 0; x < TilesX; x++)
                    if (Dirty[y * TilesX + x]) { x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y); }
            if (x1 < 0) return default;
            return new Bounds(x0 * Tile, y0 * Tile, Math.Min(width, (x1 + 1) * Tile) - x0 * Tile, Math.Min(height, (y1 + 1) * Tile) - y0 * Tile);
        }
    }

    ulong[] _prevHash = [], _curHash = [];
    Bounds[] _prevBox = [], _curBox = [];
    bool[] _dirty = [], _matchedB = [];
    int _prevCount, _w, _h, _gen = -1;
    Rgba _clear;
    bool _has;
    readonly Dictionary<ulong, int> _counts = new();

    /// <summary>Quên frame trước: lần Update kế tiếp trả Full (dùng khi renderer mất đích vẽ giữ lại).</summary>
    public void Invalidate() => _has = false;

    /// <summary>Tắt thì luôn so hash cả scene (prototype cũ, GOTX_DAMAGE=hash); mặc định dùng hint của Window khi có.</summary>
    public bool UseHint { get; set; } = true;

    /// <summary>
    /// Vùng hỏng từ <see cref="FrameDamage"/> của Window: không hash, không so scene – chỉ đổi các hộp đã tích luỹ sang lưới ô.
    /// Đúng vì Window chỉ ghi hộp cũ + mới của primitive thật sự đổi và đánh dấu Full ở mọi lần dựng đầy đủ.
    /// </summary>
    FrameDamage.Consumer? _consumer;
    FrameDamage? _consumerOf;

    Result UpdateFromHint(Scene scene, FrameDamage source, int width, int height)
    {
        if (_consumer is null || !ReferenceEquals(_consumerOf, source)) { _consumer = source.Attach(); _consumerOf = source; }
        var hint = _consumer;
        bool sameFrame = _has && width == _w && height == _h && scene.ClearColor == _clear && scene.Atlas.Generation == _gen && scene.ClearColor.A >= 1;
        int tx = (width + Tile - 1) / Tile, ty = (height + Tile - 1) / Tile;
        Result result;
        if (hint.Full || !sameFrame) result = new Result(true, false, tx, ty, null, tx * ty);
        else
        {
            if (_dirty.Length < tx * ty) _dirty = new bool[tx * ty];
            Array.Clear(_dirty, 0, tx * ty);
            foreach (var r in hint.Rects) Mark(r, tx, ty);
            int count = 0;
            for (int i = 0; i < tx * ty; i++) if (_dirty[i]) count++;
            result = count == 0 ? new Result(false, true, tx, ty, null, 0) : new Result(false, false, tx, ty, _dirty, count);
        }
        hint.Consume();
        _prevCount = -1; // trạng thái hash không còn khớp frame hiển thị: lần so hash kế tiếp (nếu có) phải vẽ cả frame
        _w = width; _h = height; _clear = scene.ClearColor; _gen = scene.Atlas.Generation; _has = true;
        _hashStale = true;
        return result;
    }

    bool _hashStale;

    public Result Update(Scene scene, int width, int height)
    {
        if (UseHint && scene.Damage is { } hint) return UpdateFromHint(scene, hint, width, height);
        if (_hashStale) { _has = false; _hashStale = false; }
        var prims = scene.Primitives;
        int n = prims.Length;
        if (_curHash.Length < n) { _curHash = new ulong[n * 2]; _curBox = new Bounds[n * 2]; _matchedB = new bool[n * 2]; }
        for (int i = 0; i < n; i++)
        {
            _curHash[i] = SceneDamageMath.Hash(scene, prims[i]);
            _curBox[i] = SceneDamageMath.Visual(scene, prims[i]);
        }
        bool sameFrame = _has && width == _w && height == _h && scene.ClearColor == _clear && scene.Atlas.Generation == _gen && scene.ClearColor.A >= 1;
        int tx = (width + Tile - 1) / Tile, ty = (height + Tile - 1) / Tile;
        Result result;
        if (!sameFrame) result = new Result(true, false, tx, ty, null, tx * ty);
        else result = Diff(n, tx, ty);

        (_prevHash, _curHash) = (_curHash, _prevHash);
        (_prevBox, _curBox) = (_curBox, _prevBox);
        _prevCount = n; _w = width; _h = height; _clear = scene.ClearColor; _gen = scene.Atlas.Generation; _has = true;
        if (_matchedB.Length < n) _matchedB = new bool[n * 2];
        return result;
    }

    Result Diff(int n, int tx, int ty)
    {
        int m = _prevCount, pre = 0;
        while (pre < n && pre < m && _curHash[pre] == _prevHash[pre]) pre++;
        int suf = 0;
        while (suf < n - pre && suf < m - pre && _curHash[n - 1 - suf] == _prevHash[m - 1 - suf]) suf++;
        int aEnd = m - suf, bEnd = n - suf; // phần giữa: prev [pre, aEnd), cur [pre, bEnd)
        if (aEnd == pre && bEnd == pre) return new Result(false, true, tx, ty, null, 0);

        if (_dirty.Length < tx * ty) _dirty = new bool[tx * ty];
        Array.Clear(_dirty, 0, tx * ty);
        _counts.Clear();
        for (int i = pre; i < aEnd; i++) _counts[_prevHash[i]] = _counts.GetValueOrDefault(_prevHash[i]) + 1;
        int matched = 0;
        for (int j = pre; j < bEnd; j++)
        {
            if (_counts.TryGetValue(_curHash[j], out int c) && c > 0) { _counts[_curHash[j]] = c - 1; _matchedB[j] = true; matched++; }
            else { _matchedB[j] = false; Mark(_curBox[j], tx, ty); }
        }
        // Bản cũ không còn khớp (bị bớt / đổi): đánh dấu hộp cũ, đồng thời kiểm tra các primitive còn lại giữ nguyên thứ tự tương đối.
        int mb = pre;
        for (int i = pre; i < aEnd; i++)
        {
            if (_counts.TryGetValue(_prevHash[i], out int c) && c > 0) { _counts[_prevHash[i]] = c - 1; Mark(_prevBox[i], tx, ty); continue; }
            while (mb < bEnd && !_matchedB[mb]) mb++;
            if (mb >= bEnd || _curHash[mb] != _prevHash[i]) return new Result(true, false, tx, ty, null, tx * ty); // đổi thứ tự vẽ
            mb++;
        }
        int dirtyCount = 0;
        for (int i = 0; i < tx * ty; i++) if (_dirty[i]) dirtyCount++;
        return dirtyCount == 0 ? new Result(false, true, tx, ty, null, 0) : new Result(false, false, tx, ty, _dirty, dirtyCount);
    }

    void Mark(in Bounds b, int tx, int ty)
    {
        if (b.IsEmpty) return;
        int x0 = Math.Max(0, (int)MathF.Floor(b.X / Tile)), x1 = Math.Min(tx - 1, (int)MathF.Floor((b.Right - 0.001f) / Tile));
        int y0 = Math.Max(0, (int)MathF.Floor(b.Y / Tile)), y1 = Math.Min(ty - 1, (int)MathF.Floor((b.Bottom - 0.001f) / Tile));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++) _dirty[y * tx + x] = true;
    }
}
