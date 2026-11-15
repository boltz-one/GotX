using GotX.Text;
using GotX.Widgets;

namespace GotX;

public sealed partial class Window
{
    void PaintInput(TextInput t, Bounds b, in TextStyle ts, Bounds deviceClip, Xf xf)
    {
        string d = t.DisplayText;
        t.Content = b; t.Style = ts; t.TextSys = Text; t.Xf = xf;
        Text.Wrap(d, ts, b.Width, t.Lines);
        float lh = ts.LineHeight;
        bool focused = t.FocusedIn == this;
        var lines = t.Lines;
        var textColor = Fade(ts.Color, ts.Opacity);
        // text-right / text-center: mỗi dòng đặt lệch trong hộp nội dung như PaintText (x = b.X + (b.Width - bề rộng dòng) * hệ số)
        float af = ts.Align switch { TextAlign.Center => 0.5f, TextAlign.Right => 1f, _ => 0f };
        float Off(int line) => t.LineOffset(line);

        // Vùng chọn
        if (focused && t.HasSelection)
        {
            int s0 = Math.Min(t.Caret, t.Anchor), s1 = Math.Max(t.Caret, t.Anchor);
            for (int i = 0; i < lines.Count; i++)
            {
                var l = lines[i];
                int a = Math.Max(s0, l.Start), z = Math.Min(s1, l.End);
                if (a > z || (a == z && s1 <= l.End)) continue;
                float x0 = Text.MeasureRange(d, l.Start, a, ts), x1 = Text.MeasureRange(d, l.Start, z, ts);
                if (s1 > l.End && i + 1 < lines.Count) x1 += 4; // đánh dấu xuống dòng được chọn
                AddRect(new Bounds(b.X + Off(i) + x0, b.Y + i * lh, x1 - x0, lh), Fade(t.SelectionColor, ts.Opacity), deviceClip, xf);
            }
        }

        // Chữ hoặc placeholder
        if (d.Length == 0 && t.Placeholder.Length > 0)
        {
            // placeholder xuống dòng theo cùng bề rộng như chữ thật (trình duyệt cũng vậy)
            var phColor = Fade(t.PlaceholderColor, ts.Opacity);
            if (!t.Multiline)
            {
                float pw = Text.Measure(t.Placeholder, ts);
                PaintRun(t.Placeholder, 0, t.Placeholder.Length, b.X + MathF.Max(0, b.Width - pw) * af, b.Y, ts, phColor, deviceClip, xf);
            }
            else Text.Wrap(t.Placeholder, ts, b.Width, _lines);
            for (int i = 0; t.Multiline && i < _lines.Count; i++)
                PaintRun(t.Placeholder, _lines[i].Start, _lines[i].End, b.X + (b.Width - _lines[i].Width) * af, b.Y + i * lh, ts, phColor, deviceClip, xf);
        }
        else
            for (int i = 0; i < lines.Count; i++)
                PaintRun(d, lines[i].Start, lines[i].End, b.X + Off(i), b.Y + i * lh, ts, textColor, deviceClip, xf);

        // Gạch chân chuỗi IME đang soạn
        if (t.Composition.Length > 0)
        {
            int c0 = t.Caret, c1 = t.Caret + t.Composition.Length;
            for (int i = 0; i < lines.Count; i++)
            {
                var l = lines[i];
                int a = Math.Max(c0, l.Start), z = Math.Min(c1, l.End);
                if (a >= z) continue;
                float x0 = Text.MeasureRange(d, l.Start, a, ts), x1 = Text.MeasureRange(d, l.Start, z, ts);
                AddRect(new Bounds(b.X + Off(i) + x0, b.Y + (i + 1) * lh - 3, x1 - x0, 1.5f), textColor, deviceClip, xf);
            }
        }

        // Con trỏ
        int caret = t.DisplayCaret;
        int line = t.LineOf(caret);
        var cl = lines[line];
        float cx = b.X + Off(line) + Text.MeasureRange(d, cl.Start, Math.Clamp(caret, cl.Start, d.Length), ts);
        float cy = b.Y + line * lh;
        t.CaretRect = xf.ApplyAabb(new Bounds(cx, cy, 1, lh));
        if (focused) AddRect(new Bounds(cx, cy + lh * 0.15f, 1.5f, lh * 0.7f), textColor, deviceClip, xf);
    }
}
