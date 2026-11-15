using System.Globalization;

namespace GotX.Widgets;

public sealed partial class TextInput
{
    const double MultiClickSeconds = 0.45;
    const float MultiClickSlop = 5;
    int _clicks, _mode;            // _mode: 0 ký tự, 2 từ, 3 dòng (kiểu chọn khi kéo sau nhấp đúp / ba)
    int _modeStart, _modeEnd;      // đơn vị đã chọn lúc nhấp
    double _lastClickAt;
    Point _lastClickPoint;

    /// <summary>
    /// Đặt con trỏ theo vị trí chuột (logical px). extend = giữ anchor để chọn (kéo / Shift+nhấp).
    /// Nhấn không extend lặp lại tại chỗ: lần 2 chọn từ, lần 3 chọn dòng (cả văn bản nếu một dòng).
    /// </summary>
    public void SetCaretFromPoint(Point screen, bool extend)
    {
        if (TextSys is null || Lines.Count == 0 || Composition.Length > 0) return;
        int pos = PosAt(Xf.Inverse(screen), extend);
        if (extend)
        {
            if (_mode == 0) MoveTo(pos, true); else ExtendUnit(pos);
            return;
        }
        double now = App?.Clock() ?? 0;
        bool again = _clicks > 0 && now - _lastClickAt <= MultiClickSeconds
            && MathF.Abs(screen.X - _lastClickPoint.X) <= MultiClickSlop && MathF.Abs(screen.Y - _lastClickPoint.Y) <= MultiClickSlop;
        _clicks = again ? _clicks % 3 + 1 : 1;
        _lastClickAt = now; _lastClickPoint = screen;
        if (_clicks == 1) { MoveTo(pos, false); return; }
        (_modeStart, _modeEnd) = Unit(pos, _clicks);
        _mode = _clicks;
        Anchor = _modeStart; Caret = _modeEnd;
        Moved();
    }

    int PosAt(Point p, bool dragging)
    {
        if (dragging && Multiline)
        {
            if (p.Y < Content.Y) return 0;
            if (p.Y >= Content.Y + Lines.Count * Style.LineHeight) return _text.Length;
        }
        int line = Math.Clamp((int)((p.Y - Content.Y) / Style.LineHeight), 0, Lines.Count - 1);
        var l = Lines[line];
        return TextSys!.IndexAtX(_text, l.Start, LineEnd(line), p.X - Content.X - LineOffset(line), Style);
    }

    void ExtendUnit(int pos)
    {
        var (s, e) = Unit(pos, _mode);
        if (s < _modeStart) { Anchor = _modeEnd; Caret = s; }
        else { Anchor = _modeStart; Caret = Math.Max(e, _modeEnd); }
        Moved();
    }

    (int Start, int End) Unit(int pos, int mode)
    {
        if (mode >= 3)
        {
            if (!Multiline) return (0, _text.Length);
            int a = pos == 0 ? 0 : _text.LastIndexOf('\n', pos - 1) + 1, z = _text.IndexOf('\n', pos);
            return (a, z < 0 ? _text.Length : z);
        }
        if (_text.Length == 0) return (0, 0);
        int at = pos < _text.Length && _text[pos] != '\n' ? pos : Math.Max(0, pos - 1);
        if (_text[at] == '\n') return (pos, pos);
        int k = Kind(at), s = at, e = at + 1;
        while (s > 0 && _text[s - 1] != '\n' && Kind(s - 1) == k) s--;
        while (e < _text.Length && _text[e] != '\n' && Kind(e) == k) e++;
        return (s, e);
    }

    /// <summary>0 khoảng trắng, 1 chữ/số/_ , 2 dấu câu và ký tự khác.</summary>
    int Kind(int i)
    {
        char c = _text[i];
        if (char.IsWhiteSpace(c)) return 0;
        if (c == '_' || char.IsLetterOrDigit(_text, i) || char.IsSurrogate(c)
            || CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) return 1;
        return 2;
    }
}
