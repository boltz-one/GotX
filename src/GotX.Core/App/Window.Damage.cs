using System.Runtime.InteropServices;

namespace GotX;

public sealed partial class Window
{
    /// <summary>Vùng đổi tích luỹ cho renderer (xem <see cref="FrameDamage"/>); gắn vào mọi Scene do cửa sổ này trả về.</summary>
    public FrameDamage Damage { get; } = new();

    ulong[] _dmgHash = new ulong[64];
    Bounds[] _dmgBox = new Bounds[64];

    /// <summary>Trước khi thay đoạn: nhớ hash + hộp của primitive cũ (chỉ khi renderer đang dùng vùng hỏng).</summary>
    bool SnapshotOldSegment(in ViewCache old)
    {
        if (!Damage.Active || Damage.AllFull) return false;
        int n = old.PrimEnd - old.PrimStart;
        if (_dmgHash.Length < n) { _dmgHash = new ulong[n + n / 4 + 64]; _dmgBox = new Bounds[n + n / 4 + 64]; }
        var prims = _scene.Primitives;
        for (int i = 0; i < n; i++)
        {
            _dmgHash[i] = SceneDamageMath.Hash(_scene, prims[old.PrimStart + i]);
            _dmgBox[i] = SceneDamageMath.Visual(_scene, prims[old.PrimStart + i]);
        }
        return true;
    }

    /// <summary>Sau khi thay đoạn: bỏ tiền tố / hậu tố giống hệt, phần giữa (cũ lẫn mới) thành vùng hỏng.</summary>
    void RecordSegmentDamage(in ViewCache old, int newCount)
    {
        int oldN = old.PrimEnd - old.PrimStart;
        var prims = _scene.Primitives;
        int s = old.PrimStart;
        if (_dmgHash.Length < Math.Max(oldN, newCount)) { Damage.MarkFull(); return; }
        int pre = 0;
        while (pre < oldN && pre < newCount && SceneDamageMath.Hash(_scene, prims[s + pre]) == _dmgHash[pre]) pre++;
        int suf = 0;
        while (suf < oldN - pre && suf < newCount - pre && SceneDamageMath.Hash(_scene, prims[s + newCount - 1 - suf]) == _dmgHash[oldN - 1 - suf]) suf++;
        for (int i = pre; i < oldN - suf; i++) Damage.Add(_dmgBox[i]);
        for (int i = pre; i < newCount - suf; i++) Damage.Add(SceneDamageMath.Visual(_scene, prims[s + i]));
    }
}
