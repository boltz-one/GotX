using GotX.Text;

namespace GotX.Widgets;

public sealed partial class TextInput
{
    /// <summary>Độ cuộn ngang hiện tại của ô một dòng (logical px).</summary>
    internal float ScrollX;

    /// <summary>
    /// Ô một dòng có chữ dài hơn khung: trả hộp nội dung dời trái theo ScrollX sao cho con trỏ nằm trong khung.
    /// Ngược lại (nhiều dòng, hoặc vừa khung) trả nguyên hộp.
    /// </summary>
    internal Bounds FitViewport(Bounds b, string d, TextSystem text, in TextStyle ts)
    {
        float tw = Multiline || d.Length == 0 ? 0 : text.Measure(d, ts);
        if (tw <= b.Width) { ScrollX = 0; return b; }
        float cx = text.MeasureRange(d, 0, Math.Clamp(DisplayCaret, 0, d.Length), ts);
        float max = tw + 2 - b.Width;
        if (cx - ScrollX > b.Width - 2) ScrollX = cx - b.Width + 2;
        if (cx - ScrollX < 0) ScrollX = cx;
        ScrollX = Math.Clamp(ScrollX, 0, max);
        return new Bounds(b.X - ScrollX, b.Y, tw + 2, b.Height);
    }
}
