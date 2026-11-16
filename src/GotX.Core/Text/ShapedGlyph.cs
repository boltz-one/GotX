namespace GotX.Text;

/// <summary>
/// Một glyph sau khi định hình (shaping). Đơn vị ngang/dọc là font units của <see cref="Font"/> riêng của glyph
/// (font dự phòng có thể khác UnitsPerEm). Cluster / ClusterEnd là khoảng UTF-16 trong chuỗi gốc mà glyph đại diện
/// (ligature hoặc ký tự dựng sẵn từ nhiều ký tự thì dài hơn 1) – dùng cho caret, chọn chữ và hit-test.
/// </summary>
public struct ShapedGlyph
{
    public const byte Mark = 1;     // dấu kết hợp (Mn): advance = 0
    public Font Font;
    public int Cluster, ClusterEnd;
    public ushort Id;
    public byte Flags, Level;       // Level: cấp bidi (lẻ = phải sang trái)
    public int Pre, Advance, XOff, YOff, Pen; // Pre = kerning cộng trước khi đặt glyph; Pen chỉ dùng nội bộ lúc định vị dấu

    public readonly bool IsMark => (Flags & Mark) != 0;
}
