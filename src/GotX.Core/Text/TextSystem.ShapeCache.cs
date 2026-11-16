using System.Runtime.CompilerServices;

namespace GotX.Text;

public sealed partial class TextSystem
{
    // Bộ đệm kết quả định hình theo (chuỗi, khoảng, font chính, cờ): vẽ lại chữ tĩnh mỗi frame chỉ còn sao chép mảng glyph.
    // Ánh xạ trực tiếp 2048 ô; chỉ giữ đoạn ≤ 128 glyph; ô tự cấp phát theo kích thước thật, ổn định thì không cấp phát.
    const int ShapeCacheSlots = 2048, ShapeCacheMaxGlyphs = 128;

    struct ShapeSlot
    {
        public string? Text;
        public int Start, End, Gen, Count, Seen;
        public Font? Primary;
        public byte Flags;
        public ShapedGlyph[]? Glyphs;
    }

    readonly ShapeSlot[] _shapeCache = new ShapeSlot[ShapeCacheSlots];
    int _shapeGen;

    /// <summary>Số thứ tự frame (Window tăng mỗi Draw): một khoá phải được thấy ở frame KHÁC mới được nhận vào bộ đệm, nên chuỗi đang gõ (mới mỗi frame, nhưng được đo + vẽ hai lần trong frame) không cấp phát mảng.</summary>
    internal int FrameTick;

    /// <summary>Số lần trúng / trượt bộ đệm định hình (chẩn đoán, test).</summary>
    public long ShapeHits, ShapeMisses;

    void InvalidateShapeCache() => _shapeGen++;

    public int Shape(string text, int start, int end, in TextStyle style, bool visual)
    {
        var primary = FontFor(style);
        byte flags = (byte)((visual ? 1 : 0) | (style.TabularNums ? 2 : 0) | (style.LetterSpacing == 0 ? 4 : 0));
        int h = (RuntimeHelpers.GetHashCode(text) * 31 + start) * 31 + end ^ flags ^ RuntimeHelpers.GetHashCode(primary);
        ref var slot = ref _shapeCache[(h ^ (h >>> 8)) & (ShapeCacheSlots - 1)];
        bool same = ReferenceEquals(slot.Text, text) && slot.Start == start && slot.End == end && slot.Flags == flags
            && ReferenceEquals(slot.Primary, primary) && slot.Gen == _shapeGen;
        if (same && slot.Count >= 0)
        {
            _view = slot.Glyphs; // đọc thẳng từ ô đệm, không sao chép
            ShapeHits++;
            return slot.Count;
        }
        ShapeMisses++;
        _view = null;
        int n = ShapeCore(text, start, end, style, visual);
        // Nhận vào bộ đệm ở lần thứ hai thấy cùng khoá: chuỗi đổi mỗi frame (đang gõ) không bao giờ cấp phát mảng.
        if (same && slot.Seen != FrameTick && n <= ShapeCacheMaxGlyphs)
        {
            if (slot.Glyphs is null || slot.Glyphs.Length < n) slot.Glyphs = new ShapedGlyph[n + 8];
            Array.Copy(_sg, slot.Glyphs, n);
            slot.Count = n;
        }
        else { slot.Text = text; slot.Start = start; slot.End = end; slot.Flags = flags; slot.Primary = primary; slot.Gen = _shapeGen; slot.Count = -1; slot.Seen = FrameTick; }
        return n;
    }
}
