using GotX.Text;

namespace GotX.Widgets;

public sealed partial class TextInput
{
    // ---------- Hình học dòng (theo Lines của lần vẽ trước) ----------

    /// <summary>Dòng chứa vị trí pos. Tại điểm ngắt mềm, con trỏ thuộc dòng sau.</summary>
    internal int LineOf(int pos)
    {
        for (int i = 0; i < Lines.Count; i++)
        {
            var l = Lines[i];
            if (pos > l.End) continue;
            if (pos == l.End && i + 1 < Lines.Count && Lines[i + 1].Start == pos) continue;
            return i;
        }
        return Math.Max(0, Lines.Count - 1);
    }

    /// <summary>Vị trí cuối dòng mà con trỏ còn nằm trên dòng đó.</summary>
    int LineEnd(int i)
    {
        var l = Lines[i];
        bool softWrap = i + 1 < Lines.Count && Lines[i + 1].Start == l.End;
        return softWrap && l.End > l.Start ? l.End - 1 : l.End;
    }

    /// <summary>Độ lệch ngang của dòng trong hộp nội dung do text-right / text-center (0 với text-left).</summary>
    internal float LineOffset(int line)
    {
        if (line < 0 || line >= Lines.Count || Style.Align == TextAlign.Left) return 0;
        float free = MathF.Max(0, Content.Width - Lines[line].Width);
        return Style.Align == TextAlign.Center ? free / 2 : free;
    }

    /// <summary>Vị trí x của pos tính từ mép trái hộp nội dung (đã gồm độ lệch căn lề) – để di chuyển lên/xuống giữ cột.</summary>
    internal float ContentXAt(int pos) => XAt(pos) + LineOffset(LineOf(pos));

    internal float XAt(int pos)
    {
        if (TextSys is null || Lines.Count == 0) return 0;
        var l = Lines[LineOf(pos)];
        string d = DisplayText;
        return TextSys.MeasureRange(d, l.Start, Math.Clamp(pos, l.Start, d.Length), Style);
    }
}
