namespace GotX.Test;

/// <summary>Luật về tương tác và khai báo: vùng bấm bị che, khoá trùng, class không được hỗ trợ.</summary>
public static partial class UiLint
{
    /// <summary>
    /// <c>occluded</c>: tâm phần nhìn thấy của một phần tử bấm/focus được lại nằm dưới một vùng bấm khác vẽ SAU nó
    /// (không phải con của nó) → click vào giữa nút sẽ rơi vào phần tử kia. Chỉ xét trong cùng một lớp: overlay
    /// (menu, hộp thoại) che nội dung bên dưới là chủ ý.
    /// </summary>
    static void Occluded(LintContext ctx)
    {
        var nodes = ctx.Nodes;
        var root = new int[nodes.Count];
        for (int i = 0; i < nodes.Count; i++) root[i] = nodes[i].Parent < 0 ? i : root[nodes[i].Parent];
        var vp = ctx.Tree.ViewportBounds;
        foreach (var n in nodes)
        {
            if (!(n.Clickable || n.Focusable) || n.Disabled) continue;
            var seen = n.Visible.Intersect(vp);
            if (seen.IsEmpty) continue;
            var center = Fmt.Center(seen);
            int end = ctx.Tree.SubtreeEnd(n);
            for (int i = nodes.Count - 1; i >= end; i--)
            {
                var m = nodes[i];
                if (!m.Clickable || root[i] != root[n.Index] || !m.Visible.Contains(center)) continue;
                ctx.Add("occluded", Severity.Warning, n,
                    $"tâm {Fmt.P(center)} bị vùng bấm vẽ sau che: {ctx.Tree.Describe(m)} → click vào giữa sẽ không tới phần tử này", m);
                break;
            }
        }
    }

    /// <summary>
    /// <c>duplicate-key</c>: cùng một <c>.Key()</c> xuất hiện hai lần trong một view. Khoá là định danh của state giữ qua
    /// frame (cuộn, transition, focus) và là thứ test dùng để tìm phần tử – trùng khoá thì state bị dùng chung.
    /// Hai view khác nhau (kể cả cùng kiểu) dùng cùng khoá thì không sao.
    /// </summary>
    static void DuplicateKey(LintContext ctx)
    {
        var seen = new Dictionary<(int Owner, string Key), UiNode>();
        foreach (var n in ctx.Nodes)
        {
            if (n.Key is not { } key) continue;
            var owner = ctx.Tree.OwnerView(n);
            var id = (owner?.Index ?? -1, key);
            if (!seen.TryGetValue(id, out var first)) { seen[id] = n; continue; }
            ctx.Add("duplicate-key", Severity.Error, n,
                $"Key \"{key}\" dùng lần nữa trong view {owner?.View ?? "(gốc)"}; lần đầu ở {ctx.Tree.Describe(first)}", first);
        }
    }

    static readonly Dictionary<string, bool> s_tokenKnown = new(StringComparer.Ordinal);

    /// <summary>
    /// <c>unknown-class</c>: token Tailwind mà Core không biên dịch được (bị bỏ qua lúc chạy → phần tử thiếu style mà
    /// không ai hay). Mỗi token được hỏi lại <c>Tailwind.Parse</c> một lần rồi nhớ, nên kết quả không phụ thuộc việc
    /// chuỗi class đã nằm trong cache của Core từ trước hay chưa.
    /// </summary>
    static void UnknownClass(LintContext ctx)
    {
        foreach (var n in ctx.Nodes)
        {
            if (n.Classes is not { Length: > 0 } classes) continue;
            List<string>? bad = null;
            foreach (string token in classes.Split((char[])[' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
                if (!IsKnownClass(token) && bad?.Contains(token) != true) (bad ??= new()).Add(token);
            if (bad is not null)
                ctx.Add("unknown-class", Severity.Error, n, $"class không được hỗ trợ (bị bỏ qua khi chạy): {string.Join(", ", bad)}");
        }
    }

    /// <summary>Token Tailwind có được Core hỗ trợ không.</summary>
    public static bool IsKnownClass(string token)
    {
        if (s_tokenKnown.TryGetValue(token, out bool known)) return known;
        var hook = Tailwind.OnUnknownClass;
        Tailwind.OnUnknownClass = null; // đây là câu hỏi của công cụ, không phải class của app: đừng báo cho hook của người dùng
        try { known = Tailwind.Parse(token).Unknown.Count == 0; }
        finally { Tailwind.OnUnknownClass = hook; }
        return s_tokenKnown[token] = known;
    }
}
