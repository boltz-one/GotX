using GotX.Text;

namespace GotX.Widgets;

public sealed partial class TextInput
{
    /// <summary>Đặt con trỏ theo vị trí chuột (logical px). extend = giữ anchor để chọn.</summary>
    public void SetCaretFromPoint(Point p, bool extend)
    {
        if (TextSys is null || Lines.Count == 0 || Composition.Length > 0) return;
        p = Xf.Inverse(p);
        int line = Math.Clamp((int)((p.Y - Content.Y) / Style.LineHeight), 0, Lines.Count - 1);
        var l = Lines[line];
        MoveTo(TextSys.IndexAtX(_text, l.Start, LineEnd(line), p.X - Content.X, Style), extend);
    }

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

    internal float XAt(int pos)
    {
        if (TextSys is null || Lines.Count == 0) return 0;
        var l = Lines[LineOf(pos)];
        string d = DisplayText;
        return TextSys.MeasureRange(d, l.Start, Math.Clamp(pos, l.Start, d.Length), Style);
    }
}
