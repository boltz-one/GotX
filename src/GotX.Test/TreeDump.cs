using System.Text;

namespace GotX.Test;

public sealed class DumpOptions
{
    /// <summary>Chỉ in cây con của phần tử có khoá này.</summary>
    public string? RootKey { get; set; }
    public int MaxDepth { get; set; } = int.MaxValue;
    /// <summary>Bỏ các node không còn phần nào nhìn thấy (bị cắt hết, nằm ngoài cửa sổ).</summary>
    public bool OnlyVisible { get; set; }
    public int ClassChars { get; set; } = 48;
    public int TextChars { get; set; } = 40;
    /// <summary>Các node cần đánh dấu <c>&gt;&gt;</c> ở đầu dòng (node gây lỗi).</summary>
    public IReadOnlyCollection<int>? Mark { get; set; }
}

/// <summary>
/// In cây giao diện thành văn bản cho agent đọc: mỗi node một dòng, thụt theo độ sâu.
/// <c>div#save .px-3 py-1.5… [120,40 64×32] click focusable "Lưu"</c>
/// </summary>
public static class TreeDump
{
    public static string Dump(UiTree tree, DumpOptions? options = null)
    {
        var o = options ?? new DumpOptions();
        var sb = new StringBuilder();
        int start = 0, end = tree.Nodes.Count, baseDepth = 0;
        if (o.RootKey is { } key)
        {
            if (tree.Find(key) is not { } root) return $"(không có phần tử #{key})\n";
            start = root.Index; end = tree.SubtreeEnd(root); baseDepth = root.Depth;
        }
        for (int i = start; i < end; i++)
        {
            var n = tree.Nodes[i];
            int depth = n.Depth - baseDepth;
            if (depth > o.MaxDepth) continue;
            if (o.OnlyVisible && n.Visible.IsEmpty) continue;
            sb.Append(o.Mark?.Contains(n.Index) == true ? ">>" : "  ").Append(' ', depth * 2);
            Line(sb, n, o);
            sb.Append('\n');
        }
        return sb.ToString();
    }

    static void Line(StringBuilder sb, UiNode n, DumpOptions o)
    {
        sb.Append(n.Kind);
        if (n.Key is { } key) sb.Append('#').Append(key);
        if (n.View is { } view) sb.Append(' ').Append(view);
        if (n.Classes is { Length: > 0 } cls && o.ClassChars > 0) sb.Append(" .").Append(Fmt.Cut(cls, o.ClassChars));
        sb.Append(' ').Append(Fmt.B(n.Screen));
        foreach (string flag in Flags(n)) sb.Append(' ').Append(flag);
        if (n.Kind is "text" or "input" && n.Text is { } text)
            sb.Append(" \"").Append(Fmt.Cut(text.Replace('\n', ' '), o.TextChars)).Append('"');
    }

    /// <summary>
    /// Cờ trạng thái của node: <c>clipped</c> (bị cắt một phần), <c>hidden</c> (bị cắt hết / ngoài vùng cắt),
    /// <c>click</c>, <c>focusable</c>, <c>focused</c>, <c>scroll</c>, <c>disabled</c>, <c>hover</c>, <c>xf</c> (đang bị xoay/skew), <c>abs</c>.
    /// </summary>
    public static IEnumerable<string> Flags(UiNode n)
    {
        var s = n.Screen; var v = n.Visible;
        if (!s.IsEmpty && v.IsEmpty) yield return "hidden";
        else if (!s.IsEmpty && (v.Width < s.Width - 0.5f || v.Height < s.Height - 0.5f)) yield return "clipped";
        if (n.Clickable) yield return "click";
        if (n.Focusable) yield return "focusable";
        if (n.Focused) yield return "focused";
        if (n.Scrollable) yield return "scroll";
        if (n.Disabled) yield return "disabled";
        if (n.Hovered && n.Clickable) yield return "hover";
        if (n.Transformed) yield return "xf";
        if (n.Position == Position.Absolute) yield return "abs";
    }

    /// <summary>Xuất cây thành JSON (mảng phẳng theo thứ tự vẽ, mỗi node có <c>parent</c>) cho công cụ khác xử lý.</summary>
    public static string ToJson(UiTree tree)
    {
        var j = new JsonWriter();
        j.BeginObject();
        j.Name("viewport").BeginArray().Value(tree.Viewport.Width).Value(tree.Viewport.Height).EndArray();
        j.Name("scale").Value(tree.Scale);
        j.Name("nodes").BeginArray();
        foreach (var n in tree.Nodes)
        {
            j.BeginObject();
            j.Name("index").Value(n.Index).Name("parent").Value(n.Parent).Name("depth").Value(n.Depth).Name("kind").Value(n.Kind);
            if (n.Key is not null) j.Name("key").Value(n.Key);
            if (n.View is not null) j.Name("view").Value(n.View);
            if (n.Classes is not null) j.Name("classes").Value(n.Classes);
            if (n.Text is not null) j.Name("text").Value(n.Text);
            j.Name("screen").Bounds(n.Screen).Name("layout").Bounds(n.Layout).Name("visible").Bounds(n.Visible);
            j.Name("flags").BeginArray();
            foreach (string f in Flags(n)) j.Value(f);
            j.EndArray();
            j.Name("background").Value(Fmt.C(n.Background)).Name("color").Value(Fmt.C(n.Color));
            j.Name("fontSize").Value(n.FontSize).Name("opacity").Value(n.Opacity);
            j.Name("prims").BeginArray().Value(n.PrimStart).Value(n.PrimEnd).EndArray();
            j.EndObject();
        }
        j.EndArray().EndObject();
        return j.ToString();
    }
}
