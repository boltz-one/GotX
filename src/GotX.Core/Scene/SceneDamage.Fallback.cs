namespace GotX;

public sealed partial class SceneDamage
{
    // Khung dựng đầy đủ (Window đánh dấu Full) thường vẫn chỉ đổi vài primitive: view lớn bị hover/click, view có ranh giới layout hỏng, phát lại cache.
    // Khi bề mặt không đổi (cùng cỡ / nền / atlas) và hash khung trước còn khớp thì so hash từng primitive để thu vùng hỏng về đúng phần đổi.
    // Hash tốn O(số primitive): với scene lớn, nếu liên tiếp ra gần như phủ kín (cuộn, đổi kích thước) thì lùi dần (bỏ qua 2, 4, … 64 frame) để không trả phí vô ích.
    const double WideCoverage = 0.8;
    const int CheapPrims = 20000; // dưới ngưỡng này hash rẻ hơn nhiều so với raster một frame: luôn hash
    int _wideStreak, _skip;
    bool _hashedNow;

    // Cả hai cặp mảng (hiện tại + trước, đổi chỗ mỗi frame) nở cùng lúc một lần: nở lẻ từng cặp làm frame kế tiếp lại cấp phát.
    void EnsureCapacity(int n)
    {
        if (_curHash.Length >= n && _prevHash.Length >= n && _matchedB.Length >= n) return;
        int cap = n + n / 4;
        if (_curHash.Length < cap) { _curHash = new ulong[cap]; _curBox = new Bounds[cap]; }
        if (_prevHash.Length < cap) { Array.Resize(ref _prevHash, cap); Array.Resize(ref _prevBox, cap); }
        if (_matchedB.Length < cap) _matchedB = new bool[cap];
    }

    Result FullHint(Scene scene, bool sameFrame, int tx, int ty)
    {
        var full = new Result(true, false, tx, ty, null, tx * ty);
        if (_skip > 0 || !sameFrame) { if (_skip > 0) _skip--; _prevCount = -1; return full; } // không hash (lùi dần / bề mặt đổi, vd kéo giãn cửa sổ): hash cũ hết khớp, lần thử kế tiếp chỉ gieo hash
        var prims = scene.Primitives;
        int n = prims.Length;
        EnsureCapacity(n);
        for (int i = 0; i < n; i++)
        {
            _curHash[i] = SceneDamageMath.Hash(scene, prims[i]);
            _curBox[i] = SceneDamageMath.Visual(scene, prims[i]);
        }
        bool diffed = sameFrame && _prevCount >= 0;
        var result = diffed ? Diff(n, tx, ty) : full;
        (_prevHash, _curHash) = (_curHash, _prevHash);
        (_prevBox, _curBox) = (_curBox, _prevBox);
        _prevCount = n;
        _hashedNow = true;
        if (!result.Full && !result.Empty) result = result with { Boxes = _prevBox }; // sau đổi chỗ, hộp frame này nằm ở _prevBox
        if (!diffed) return result;
        if (n >= CheapPrims && (result.Full || result.Coverage >= WideCoverage)) { _wideStreak++; _skip = Math.Min(64, 1 << Math.Min(6, _wideStreak)); }
        else _wideStreak = 0;
        return result;
    }
}
