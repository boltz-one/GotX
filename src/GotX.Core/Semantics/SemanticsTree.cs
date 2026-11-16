using System.Text;

namespace GotX;

/// <summary>
/// Ảnh chụp ngữ nghĩa của một frame: cây các phần tử có vai trò / focus được, kèm tên, trạng thái, hộp. Dựng theo yêu cầu bằng
/// <see cref="Window.Semantics"/>; bộ đệm (node, danh sách, chuỗi) được tái dùng ở lần dựng sau.
/// </summary>
public sealed partial class SemanticsTree
{
    readonly List<SemanticsNode> _nodes = new();
    readonly List<int> _roots = new();
    readonly Dictionary<long, int> _byId = new();
    readonly StringBuilder _text = new();

    /// <summary>Kích thước một node của arena (byte) – kiểm tra ngữ nghĩa không làm phình arena.</summary>
    public static int NodeSizeBytes => System.Runtime.CompilerServices.Unsafe.SizeOf<Node>();

    public int Count { get; private set; }
    public SemanticsNode this[int i] => _nodes[i];
    public IReadOnlyList<int> Roots => _roots;
    /// <summary>Số thứ tự frame của Window lúc dựng.</summary>
    public int Frame { get; internal set; }
    public Size Viewport { get; internal set; }
    public float Scale { get; internal set; }

    public IEnumerable<SemanticsNode> All()
    {
        for (int i = 0; i < Count; i++) yield return _nodes[i];
    }

    public SemanticsNode? ById(long id) => _byId.TryGetValue(id, out int i) ? _nodes[i] : null;

    public SemanticsNode? Find(string key)
    {
        for (int i = 0; i < Count; i++) if (_nodes[i].Key == key) return _nodes[i];
        return null;
    }

    public SemanticsNode? FindByRole(SemanticRole role, string? name = null)
    {
        for (int i = 0; i < Count; i++)
            if (_nodes[i].Role == role && (name is null || _nodes[i].Name == name)) return _nodes[i];
        return null;
    }

    public SemanticsNode? FocusedNode()
    {
        for (int i = 0; i < Count; i++) if (_nodes[i].Focused) return _nodes[i];
        return null;
    }

    /// <summary>Vai trò là điều khiển (không phải vùng chứa chỉ để nhận phím): phải có tên.</summary>
    public static bool IsControl(SemanticRole r) => r is SemanticRole.None or SemanticRole.Button or SemanticRole.Checkbox or SemanticRole.Radio
        or SemanticRole.Switch or SemanticRole.Textbox or SemanticRole.Slider or SemanticRole.Tab or SemanticRole.MenuItem or SemanticRole.Link or SemanticRole.ComboBox;

    /// <summary>Phần tử nhận Tab mà không có tên: thứ đọc màn hình sẽ chỉ nói "button".</summary>
    public IEnumerable<SemanticsNode> UnnamedControls()
    {
        for (int i = 0; i < Count; i++)
        {
            var n = _nodes[i];
            if (n.Focusable && !n.Disabled && IsControl(n.Role) && string.IsNullOrWhiteSpace(n.Name) && !n.Visible.IsEmpty) yield return n;
        }
    }

    /// <summary>Văn bản dạng cây thụt lề (cho test, nhật ký).</summary>
    public string Dump()
    {
        var sb = new StringBuilder();
        void Walk(int i, int depth)
        {
            sb.Append(' ', depth * 2).Append(_nodes[i]).AppendLine();
            foreach (int c in _nodes[i].Children) Walk(c, depth + 1);
        }
        foreach (int r in _roots) Walk(r, 0);
        return sb.ToString();
    }
}
