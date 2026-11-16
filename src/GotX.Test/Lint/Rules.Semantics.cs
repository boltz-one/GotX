namespace GotX.Test;

/// <summary>Luật ngữ nghĩa: điều khiển nhận Tab mà công cụ hỗ trợ không đọc được tên.</summary>
public static partial class UiLint
{
    /// <summary>
    /// <c>unnamed-control</c>: phần tử nhận Tab (nút, ô đánh dấu, ô nhập, thanh trượt, mục menu…) không có tên truy cập – không có
    /// <c>.AriaLabel()</c> và không có chữ bên trong (nút chỉ có icon), nên đọc màn hình chỉ nói "button". Sửa: <c>.AriaLabel("Đóng")</c>.
    /// Tắt có chủ ý bằng <c>Suppress("unnamed-control", mẫu)</c> như các luật khác.
    /// </summary>
    static void UnnamedControl(LintContext ctx)
    {
        if (ctx.Tree.Semantics is not { } sem) return;
        foreach (var n in sem.UnnamedControls())
        {
            if (n.PaintOrdinal >= ctx.Nodes.Count) continue; // cây ngữ nghĩa và cây soi phải cùng một frame
            ctx.Add("unnamed-control", Severity.Warning, ctx.Nodes[n.PaintOrdinal],
                $"điều khiển ({(n.Role == SemanticRole.None ? "focusable" : n.Role.ToString().ToLowerInvariant())}) không có tên truy cập – thêm .AriaLabel(...) hoặc chữ bên trong");
        }
    }
}
