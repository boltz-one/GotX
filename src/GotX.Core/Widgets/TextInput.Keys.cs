namespace GotX.Widgets;

public sealed partial class TextInput
{
    /// <summary>Xử lý phím điều khiển. Trả về true nếu đã dùng phím.</summary>
    public bool HandleKey(Key key, Modifiers mods)
    {
        if (Composition.Length > 0) return true; // IME đang soạn: phím thuộc về IME, không để nổi lên OnKey của phần tử cha
        bool shift = (mods & Modifiers.Shift) != 0, ctrl = (mods & Modifiers.Control) != 0;
        switch (key)
        {
            case Key.Backspace:
                if (!HasSelection && Caret > 0) Anchor = PrevBoundary(Caret);
                if (HasSelection) ReplaceSelection("");
                return true;
            case Key.Delete:
                if (!HasSelection && Caret < _text.Length) Anchor = NextBoundary(Caret);
                if (HasSelection) ReplaceSelection("");
                return true;
            case Key.Left:
                MoveTo(!shift && HasSelection ? SelStart : PrevBoundary(Caret), shift);
                return true;
            case Key.Right:
                MoveTo(!shift && HasSelection ? SelEnd : NextBoundary(Caret), shift);
                return true;
            case Key.Home:
                MoveTo(ctrl || Lines.Count == 0 ? 0 : Lines[LineOf(Caret)].Start, shift);
                return true;
            case Key.End:
                MoveTo(ctrl || Lines.Count == 0 ? _text.Length : LineEnd(LineOf(Caret)), shift);
                return true;
            case Key.Up:
            case Key.Down:
            {
                if (!Multiline) return false; // ô một dòng: để phần tử cha dùng (di chuyển trong danh sách gợi ý)
                if (Lines.Count == 0 || TextSys is null) return true;
                int line = LineOf(Caret), target = line + (key == Key.Up ? -1 : 1);
                if (target < 0) { MoveTo(0, shift); return true; }
                if (target >= Lines.Count) { MoveTo(_text.Length, shift); return true; }
                float x = XAt(Caret);
                var l = Lines[target];
                MoveTo(TextSys.IndexAtX(_text, l.Start, LineEnd(target), x, Style), shift);
                return true;
            }
            case Key.A when ctrl:
                SelectAll();
                return true;
            case Key.C when ctrl:
                Copy();
                return true;
            case Key.X when ctrl:
                Cut();
                return true;
            case Key.V when ctrl:
                Paste();
                return true;
            case Key.Enter:
                if (Multiline) Insert("\n"); else Emit(new Submitted(_text));
                return true;
        }
        return false;
    }
}
