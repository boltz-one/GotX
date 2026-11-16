namespace GotX.Test;

/// <summary>
/// Cây giao diện của một frame (bọc <see cref="UiSnapshot"/>) kèm các phép tra cứu. Các node nằm theo thứ tự vẽ
/// (duyệt trước), nên cây con của một node là một đoạn liên tiếp – mọi phép duyệt ở đây dựa vào điều đó.
/// </summary>
public sealed partial class UiTree(UiSnapshot snapshot)
{
    public UiSnapshot Snapshot { get; } = snapshot;

    /// <summary>Nguồn dựng cây ngữ nghĩa của đúng trạng thái này (UiTest gán); chỉ chạy khi có người cần (luật <c>unnamed-control</c>, test ngữ nghĩa).</summary>
    public Func<SemanticsTree?>? SemanticsSource { get; set; }

    /// <summary>Cây ngữ nghĩa, dựng lần đầu khi cần (một frame riêng). null nếu không có nguồn.</summary>
    public SemanticsTree? Semantics => _semantics ??= SemanticsSource?.Invoke();
    SemanticsTree? _semantics;
    public IReadOnlyList<UiNode> Nodes => Snapshot.Nodes;
    public Size Viewport => Snapshot.Viewport;
    public float Scale => Snapshot.Scale;
    public Bounds ViewportBounds => new(0, 0, Viewport.Width, Viewport.Height);

    /// <summary>Phần tử có <c>.Key(key)</c>; nếu trùng khoá thì ưu tiên cái đang nhìn thấy.</summary>
    public UiNode? Find(string key)
    {
        UiNode? first = null;
        foreach (var n in Nodes)
        {
            if (n.Key != key) continue;
            if (!n.Visible.IsEmpty) return n;
            first ??= n;
        }
        return first;
    }

    public IEnumerable<UiNode> FindAll(Func<UiNode, bool> predicate)
    {
        foreach (var n in Nodes) if (predicate(n)) yield return n;
    }

    public UiNode? First(Func<UiNode, bool> predicate)
    {
        foreach (var n in Nodes) if (predicate(n)) return n;
        return null;
    }

    /// <summary>Node chữ đầu tiên khớp <paramref name="text"/> (mặc định: chứa; <paramref name="exact"/>: bằng đúng).</summary>
    public UiNode? FindText(string text, bool exact = false) => First(n =>
        n.Kind == "text" && n.Text is { } s && (exact ? s == text : s.Contains(text, StringComparison.Ordinal)));

    /// <summary>Node view đầu tiên có kiểu tên <paramref name="typeName"/>.</summary>
    public UiNode? FindView(string typeName) => First(n => n.Kind == "view" && n.View == typeName);

    public UiNode? Parent(UiNode n) => n.Parent >= 0 ? Nodes[n.Parent] : null;

    /// <summary>Từ cha trực tiếp lên tới gốc.</summary>
    public IEnumerable<UiNode> Ancestors(UiNode n)
    {
        for (int p = n.Parent; p >= 0; p = Nodes[p].Parent) yield return Nodes[p];
    }

    /// <summary>Chỉ số ngay sau node cuối của cây con.</summary>
    public int SubtreeEnd(UiNode n)
    {
        int i = n.Index + 1;
        while (i < Nodes.Count && Nodes[i].Depth > n.Depth) i++;
        return i;
    }

    public IEnumerable<UiNode> Descendants(UiNode n)
    {
        for (int i = n.Index + 1; i < Nodes.Count && Nodes[i].Depth > n.Depth; i++) yield return Nodes[i];
    }

    public bool IsDescendant(UiNode n, UiNode ancestor)
    {
        for (int p = n.Parent; p >= 0; p = Nodes[p].Parent) if (p == ancestor.Index) return true;
        return false;
    }

    /// <summary>Gốc của lớp chứa node: view gốc của cửa sổ, hoặc lớp của một overlay.</summary>
    public UiNode Root(UiNode n)
    {
        while (n.Parent >= 0) n = Nodes[n.Parent];
        return n;
    }

    /// <summary>View gần nhất bao ngoài node (không tính chính nó).</summary>
    public UiNode? OwnerView(UiNode n)
    {
        foreach (var a in Ancestors(n)) if (a.Kind == "view") return a;
        return null;
    }

    public UiNode? ScrollAncestor(UiNode n)
    {
        foreach (var a in Ancestors(n)) if (a.Scrollable) return a;
        return null;
    }

    public bool AnyAncestor(UiNode n, Func<UiNode, bool> predicate)
    {
        foreach (var a in Ancestors(n)) if (predicate(a)) return true;
        return false;
    }

    /// <summary>Toàn bộ chữ trong cây con (các node chữ nối bằng dấu cách) – "nhãn" của một nút, một dòng.</summary>
    public string TextOf(UiNode n)
    {
        if (n.Kind is "text" or "input") return n.Text ?? "";
        var sb = new System.Text.StringBuilder();
        foreach (var d in Descendants(n))
            if (d.Kind is "text" or "input" && d.Text is { Length: > 0 } s) sb.Append(sb.Length > 0 ? " " : "").Append(s);
        return sb.ToString();
    }

    /// <summary>Node trên cùng (vẽ sau cùng) còn nhìn thấy tại điểm logical <paramref name="p"/>.</summary>
    public UiNode? TopAt(Point p)
    {
        for (int i = Nodes.Count - 1; i >= 0; i--)
            if (Nodes[i].Visible.Contains(p)) return Nodes[i];
        return null;
    }

    /// <summary>Node sâu nhất mà hộp trên màn hình chứa trọn <paramref name="area"/> (logical px); null nếu không có.</summary>
    public UiNode? DeepestContaining(Bounds area)
    {
        UiNode? best = null;
        foreach (var n in Nodes)
        {
            var s = n.Visible;
            if (s.IsEmpty || s.X > area.X + 0.5f || s.Y > area.Y + 0.5f || s.Right < area.Right - 0.5f || s.Bottom < area.Bottom - 0.5f) continue;
            // Cùng độ sâu thì lấy node vẽ sau (nằm trên).
            if (best is null || n.Depth >= best.Depth) best = n;
        }
        return best;
    }

    /// <summary>Node sở hữu primitive thứ <paramref name="index"/> của Scene (node sâu nhất có đoạn primitive chứa nó).</summary>
    public UiNode? OwnerOfPrimitive(int index)
    {
        UiNode? best = null;
        foreach (var n in Nodes)
            if (index >= n.PrimStart && index < n.PrimEnd) best = n; // duyệt trước: node sau trong chuỗi tổ tiên là node sâu hơn
        return best;
    }
}
