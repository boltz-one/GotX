using System.Text;

namespace GotX.Test;

/// <summary>Đường dẫn phần tử – cách gọi tên một node trong mọi thông báo để agent tìm lại được nó trong mã nguồn.</summary>
public sealed partial class UiTree
{
    /// <summary>
    /// Nhãn một node: view → tên kiểu; chữ → <c>text "trích đoạn"</c>; còn lại → loại + <c>#key</c>, hoặc
    /// <c>[n]</c> (thứ tự trong cha, từ 1) khi không có khoá.
    /// </summary>
    public string Label(UiNode n)
    {
        if (n.Kind == "view") return (n.View ?? "View") + (n.Key is { } vk ? "#" + vk : "");
        if (n.Kind == "text") return "text \"" + Fmt.Cut((n.Text ?? "").Replace('\n', ' ').Replace('|', '/'), 24) + "\"";
        if (n.Key is { } key) return n.Kind + "#" + key;
        int at = n.Parent >= 0 ? Nodes[n.Parent].Children.IndexOf(n.Index) + 1 : 0;
        return at > 0 ? $"{n.Kind}[{at}]" : n.Kind;
    }

    /// <summary>
    /// Đường dẫn dạng <c>GalleryView &gt; div#toolbar &gt;&gt; div[2] &gt; div#save .px-3 py-1.5…</c>.
    /// Giữ lại: mọi view, mọi node có khoá, và hai mức cuối; các mức vô danh ở giữa gộp thành <c>&gt;&gt;</c>.
    /// Chuỗi class của node cuối được cắt còn <paramref name="classChars"/> ký tự (0 = bỏ).
    /// </summary>
    public string Path(UiNode n, int classChars = 36)
    {
        var chain = new List<UiNode> { n };
        foreach (var a in Ancestors(n)) chain.Add(a);
        chain.Reverse();
        var sb = new StringBuilder();
        bool skipped = false;
        for (int i = 0; i < chain.Count; i++)
        {
            var c = chain[i];
            bool keep = c.Kind == "view" || c.Key is not null || i >= chain.Count - 2 || i == 0;
            if (!keep) { skipped = true; continue; }
            if (sb.Length > 0) sb.Append(skipped ? " >> " : " > ");
            skipped = false;
            sb.Append(Label(c));
        }
        if (classChars > 0 && n.Classes is { Length: > 0 } cls) sb.Append(" .").Append(Fmt.Cut(cls, classChars));
        return sb.ToString();
    }

    /// <summary>Đường dẫn + hộp trên màn hình – dạng ngắn dùng trong thông báo.</summary>
    public string Describe(UiNode n) => Path(n) + " " + Fmt.B(n.Screen);
}
