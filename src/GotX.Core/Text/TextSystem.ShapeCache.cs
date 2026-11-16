using System.Runtime.CompilerServices;

namespace GotX.Text;

public sealed partial class TextSystem
{
    // Bộ đệm kết quả định hình theo (chuỗi, khoảng, font chính, cờ): vẽ lại chữ tĩnh mỗi frame chỉ còn đọc mảng glyph.
    // Ánh xạ trực tiếp 2048 ô; chỉ giữ đoạn ≤ 128 glyph. Glyph của mọi ô nằm trong vài khối lớn dùng chung (bump allocation) thay vì mảng riêng mỗi ô:
    // kéo giãn cửa sổ đổi chỗ ngắt dòng liên tục nên liên tục sinh khoá mới; mảng riêng làm mỗi khoá mới tốn một lần cấp phát,
    // còn khối dùng chung chỉ cấp một lần mỗi ~16k glyph và được tái dùng vòng tròn khi đạt trần (ô trỏ vào khối bị ghi đè tự hết hiệu lực nhờ số thế hệ).
    const int ShapeCacheSlots = 2048, ShapeCacheMaxGlyphs = 128, ShapeChunkGlyphs = 16384, ShapeMaxChunks = 6;

    struct ShapeSlot
    {
        public string? Text;
        public int Start, End, Gen, Count, Seen;
        public Font? Primary;
        public byte Flags;
        public int Chunk, Offset, ChunkEpoch; // vị trí glyph trong khối dùng chung (hợp lệ khi Count >= 0 và khối còn đúng thế hệ)
    }

    readonly ShapeSlot[] _shapeCache = new ShapeSlot[ShapeCacheSlots];
    readonly ShapedGlyph[]?[] _chunks = new ShapedGlyph[ShapeMaxChunks][];
    readonly int[] _chunkEpoch = new int[ShapeMaxChunks];
    int _chunkCur = -1, _chunkUsed, _shapeGen, _viewCount;

    /// <summary>Số thứ tự frame (Window tăng mỗi Draw): một khoá phải được thấy ở frame KHÁC mới được nhận vào bộ đệm, nên chuỗi đang gõ (mới mỗi frame, nhưng được đo + vẽ hai lần trong frame) không tốn chỗ.</summary>
    internal int FrameTick;

    /// <summary>Số lần trúng / trượt bộ đệm định hình (chẩn đoán, test).</summary>
    public long ShapeHits, ShapeMisses;

    void InvalidateShapeCache() => _shapeGen++;

    /// <summary>Cấp n ô glyph trong khối dùng chung: (khối, vị trí, thế hệ của khối).</summary>
    (int Chunk, int Offset, int Epoch) AllocGlyphs(int n)
    {
        if (_chunkCur < 0 || _chunkUsed + n > ShapeChunkGlyphs)
        {
            _chunkCur = (_chunkCur + 1) % ShapeMaxChunks;
            _chunks[_chunkCur] ??= new ShapedGlyph[ShapeChunkGlyphs];
            _chunkEpoch[_chunkCur]++; // mọi ô đang trỏ vào khối này (nếu có) hết hiệu lực
            _chunkUsed = 0;
        }
        int off = _chunkUsed;
        _chunkUsed += n;
        return (_chunkCur, off, _chunkEpoch[_chunkCur]);
    }

    public int Shape(string text, int start, int end, in TextStyle style, bool visual)
    {
        var primary = FontFor(style);
        byte flags = (byte)((visual ? 1 : 0) | (style.TabularNums ? 2 : 0) | (style.LetterSpacing == 0 ? 4 : 0));
        int h = (RuntimeHelpers.GetHashCode(text) * 31 + start) * 31 + end ^ flags ^ RuntimeHelpers.GetHashCode(primary);
        ref var slot = ref _shapeCache[(h ^ (h >>> 8)) & (ShapeCacheSlots - 1)];
        bool same = ReferenceEquals(slot.Text, text) && slot.Start == start && slot.End == end && slot.Flags == flags
            && ReferenceEquals(slot.Primary, primary) && slot.Gen == _shapeGen;
        if (same && slot.Count >= 0 && _chunkEpoch[slot.Chunk] == slot.ChunkEpoch)
        {
            _view = _chunks[slot.Chunk]; _viewOff = slot.Offset; _viewCount = slot.Count; // đọc thẳng từ khối, không sao chép
            ShapeHits++;
            return slot.Count;
        }
        ShapeMisses++;
        _view = null;
        int n = ShapeCore(text, start, end, style, visual);
        // Nhận vào bộ đệm ở lần thứ hai thấy cùng khoá: chuỗi đổi mỗi frame (đang gõ) không bao giờ chiếm chỗ.
        if (same && slot.Seen != FrameTick && n <= ShapeCacheMaxGlyphs)
        {
            var (chunk, off, epoch) = AllocGlyphs(n);
            Array.Copy(_sg, 0, _chunks[chunk]!, off, n);
            slot.Chunk = chunk; slot.Offset = off; slot.ChunkEpoch = epoch; slot.Count = n;
        }
        else { slot.Text = text; slot.Start = start; slot.End = end; slot.Flags = flags; slot.Primary = primary; slot.Gen = _shapeGen; slot.Count = -1; slot.Seen = FrameTick; }
        return n;
    }
}
