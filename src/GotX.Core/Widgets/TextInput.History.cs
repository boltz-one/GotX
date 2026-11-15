namespace GotX.Widgets;

public sealed partial class TextInput
{
    enum EditKind : byte { None, Typing, Backspace, Delete, Other }

    const int MaxHistory = 200;
    readonly List<(string Text, int Caret)> _undo = new(), _redo = new();
    EditKind _lastKind;
    int _lastCaret;

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    /// <summary>Ghi trạng thái trước khi sửa. Chuỗi gõ / xoá liên tiếp tại cùng vị trí chỉ chiếm một mục.</summary>
    void Record(EditKind kind)
    {
        bool continues = kind is EditKind.Typing or EditKind.Backspace or EditKind.Delete
            && kind == _lastKind && Caret == _lastCaret && _undo.Count > 0;
        _redo.Clear();
        if (continues) return;
        _undo.Add((_text, Caret));
        if (_undo.Count > MaxHistory) _undo.RemoveAt(0);
    }

    void ClearHistory() { _undo.Clear(); _redo.Clear(); _lastKind = EditKind.None; }

    public void Undo() => Step(_undo, _redo);
    public void Redo() => Step(_redo, _undo);

    void Step(List<(string Text, int Caret)> from, List<(string Text, int Caret)> to)
    {
        if (from.Count == 0 || Composition.Length > 0) return;
        to.Add((_text, Caret));
        var (text, caret) = from[^1];
        from.RemoveAt(from.Count - 1);
        _text = text;
        Caret = Anchor = Math.Clamp(caret, 0, text.Length);
        _lastKind = EditKind.None;
        OnChanged();
    }
}
