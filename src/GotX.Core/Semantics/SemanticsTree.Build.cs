namespace GotX;

public sealed partial class SemanticsTree
{
    struct Entry { public int Node, TextStart, KeyId; }

    readonly List<Entry> _stack = new();
    readonly List<int> _open = new(); // các node ngữ nghĩa đang mở (tổ tiên gần nhất ở cuối)
    readonly Dictionary<int, (int Start, int End)> _keyText = new();
    readonly Dictionary<int, string> _keyNames = new();
    int _ordinal;

    internal void BeginBuild(int frame, Size viewport, float scale)
    {
        Count = 0; _ordinal = 0;
        _roots.Clear(); _byId.Clear(); _text.Clear(); _stack.Clear(); _open.Clear(); _keyText.Clear();
        Frame = frame; Viewport = viewport; Scale = scale;
    }

    /// <summary>Mở một node vẽ. Trả về node ngữ nghĩa mới (để điền) hoặc null nếu node này không có ngữ nghĩa.</summary>
    internal SemanticsNode? Push(bool semantic, int keyId, long keyedId)
    {
        var f = new Entry { Node = -1, TextStart = _text.Length, KeyId = keyId };
        SemanticsNode? n = null;
        if (semantic)
        {
            if (Count == _nodes.Count) _nodes.Add(new SemanticsNode());
            n = _nodes[Count];
            n.Reset();
            n.Index = Count; n.PaintOrdinal = _ordinal;
            n.Parent = _open.Count > 0 ? _open[^1] : -1;
            var siblings = n.Parent >= 0 ? _nodes[n.Parent].Children : _roots;
            long id = keyedId != 0 ? keyedId : (n.Parent >= 0 ? _nodes[n.Parent].Id : 17) * 1000003L + siblings.Count + 1;
            while (_byId.ContainsKey(id)) id += 0x9E3779B1L;
            n.Id = id; n.TextStart = f.TextStart; n.KeyId = keyId;
            _byId[id] = n.Index;
            siblings.Add(n.Index);
            f.Node = Count++;
            _open.Add(f.Node);
        }
        _ordinal++;
        _stack.Add(f);
        return n;
    }

    internal void Text(string text)
    {
        if (_text.Length > 0) _text.Append(' ');
        _text.Append(text);
    }

    internal void Pop()
    {
        var f = _stack[^1];
        _stack.RemoveAt(_stack.Count - 1);
        if (f.Node >= 0) { _nodes[f.Node].TextEnd = _text.Length; _open.RemoveAt(_open.Count - 1); }
        if (f.KeyId != 0) _keyText[f.KeyId] = (f.TextStart, _text.Length);
    }

    string Content(int start, int end) => end > start ? _text.ToString(start, end - start).Trim() : "";

    static bool NamedFromContent(SemanticRole r) => r is SemanticRole.None or SemanticRole.Button or SemanticRole.Checkbox or SemanticRole.Radio
        or SemanticRole.Switch or SemanticRole.Link or SemanticRole.Tab or SemanticRole.MenuItem or SemanticRole.Heading or SemanticRole.Cell
        or SemanticRole.ListItem or SemanticRole.Text or SemanticRole.Alert or SemanticRole.Status or SemanticRole.Tooltip;

    /// <summary>Hết lượt vẽ: gán tên khoá, tên / mô tả suy ra từ chữ bên trong và từ phần tử được tham chiếu.</summary>
    internal void Finish(ElementTree tree)
    {
        tree.CopyKeyNames(_keyNames);
        for (int i = 0; i < Count; i++)
        {
            var n = _nodes[i];
            if (n.KeyId != 0) n.Key = _keyNames.GetValueOrDefault(n.KeyId);
            if (n.LabelledBy != 0 && _keyText.TryGetValue(n.LabelledBy, out var lr) && n.Name is null) n.Name = Content(lr.Start, lr.End);
            if (n.Name is null && NamedFromContent(n.Role)) n.Name = Content(n.TextStart, n.TextEnd);
            if (n.Name is { Length: 0 }) n.Name = null;
            if (n.Description is not null && n.Role == SemanticRole.None && !n.Focusable && n.Children.Count == 1) _nodes[n.Children[0]].Description ??= n.Description; // vd. bọc Tooltip
            if (n.DescribedBy != 0 && _keyText.TryGetValue(n.DescribedBy, out var dr) && Content(dr.Start, dr.End) is { Length: > 0 } d) n.Description = d;
        }
    }
}
