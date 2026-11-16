using GotX.Raster;
using GotX.Text;

namespace GotX.Test;

/// <summary>
/// Lint bố cục trên một <see cref="UiTree"/>: tìm những lỗi mà mắt người thấy ngay còn test hành vi thì bỏ sót –
/// lệch 1 px, tràn, bị cắt, bị che, chữ khó đọc. Mỗi luật là một hàm tĩnh trong <c>Rules.*.cs</c>; heuristic và
/// giới hạn của từng luật ghi ở <c>docs/testing.md</c>.
/// </summary>
public static partial class UiLint
{
    /// <summary>Mã các luật, theo thứ tự chạy.</summary>
    public static readonly string[] Rules =
    [
        "duplicate-key", "unknown-class", "zero-size", "offscreen", "subpixel", "misaligned", "overlap",
        "overflow", "text-overflow", "clipped", "small-target", "occluded", "low-contrast", "unnamed-control",
    ];

    /// <summary>Luật <c>low-contrast</c> cần ảnh đã dựng; thiếu <paramref name="canvas"/> thì luật đó được bỏ qua.</summary>
    public static List<LintIssue> Run(UiTree tree, Canvas? canvas = null, LintOptions? options = null, TextSystem? text = null)
    {
        var ctx = new LintContext(tree, canvas, options ?? new LintOptions(), text ?? TestFonts.Shared);
        if (ctx.On("duplicate-key")) DuplicateKey(ctx);
        if (ctx.On("unknown-class")) UnknownClass(ctx);
        if (ctx.On("zero-size")) ZeroSize(ctx);
        if (ctx.On("offscreen")) Offscreen(ctx);
        if (ctx.On("subpixel")) Subpixel(ctx);
        if (ctx.On("misaligned") || ctx.On("overlap")) Siblings(ctx);
        if (ctx.On("overflow")) Overflow(ctx);
        if (ctx.On("text-overflow")) TextOverflow(ctx);
        if (ctx.On("clipped")) Clipped(ctx);
        if (ctx.On("small-target")) SmallTarget(ctx);
        if (ctx.On("occluded")) Occluded(ctx);
        if (ctx.On("low-contrast") && canvas is not null) LowContrast(ctx);
        if (ctx.On("unnamed-control")) UnnamedControl(ctx);
        return ctx.Issues;
    }

    /// <summary>Luật có cần ảnh đã dựng không (để harness khỏi raster khi không cần).</summary>
    public static bool NeedsCanvas(LintOptions? options) => (options ?? new LintOptions()).Enabled("low-contrast");
}

/// <summary>Trạng thái dùng chung của một lượt lint.</summary>
internal sealed class LintContext(UiTree tree, Canvas? canvas, LintOptions options, TextSystem text)
{
    public readonly UiTree Tree = tree;
    public readonly Canvas? Canvas = canvas;
    public readonly LintOptions Options = options;
    public readonly TextSystem Text = text;
    public readonly List<LintIssue> Issues = new();
    /// <summary>Node chữ đã bị <c>text-overflow</c> báo – để <c>clipped</c> không báo lại cùng một nguyên nhân.</summary>
    public readonly HashSet<int> TextOverflowed = new();

    public IReadOnlyList<UiNode> Nodes => Tree.Nodes;
    public float Scale => Tree.Scale;
    public float Tol => Options.Tolerance;
    public bool On(string rule) => Options.Enabled(rule);

    public void Add(string rule, Severity severity, UiNode node, string message, UiNode? related = null)
    {
        string path = Tree.Path(node);
        if (Options.Suppressed(rule, node, path)) return;
        Issues.Add(new LintIssue
        {
            Rule = rule, Severity = severity, Message = message, Path = path,
            Bounds = node.Screen, Related = related?.Screen, NodeIndex = node.Index,
        });
    }

    /// <summary>Còn phần nào nhìn thấy sau khi cắt.</summary>
    public static bool Shown(UiNode n) => !n.Visible.IsEmpty;

    /// <summary>Con nằm trong luồng bố cục của cha và có kích thước: đối tượng của các luật so anh em.</summary>
    public static bool InFlow(UiNode n) =>
        n.Position != Position.Absolute && !n.Transformed && n.Screen.Width > 0 && n.Screen.Height > 0;

    /// <summary>
    /// Phần tử (hoặc cha qua <c>-space-x/y</c>) dùng margin âm: chồng/tràn là cố ý (nhóm avatar xếp đè, đường kẻ tràn lề).
    /// Chỉ nhìn được qua chuỗi class vì UiNode không mang margin.
    /// </summary>
    public bool NegativeMargin(UiNode n)
    {
        if (HasToken(n.Classes, static t => t.StartsWith("-m", StringComparison.Ordinal))) return true;
        return n.Parent >= 0 && HasToken(Nodes[n.Parent].Classes, static t => t.StartsWith("-space-", StringComparison.Ordinal));
    }

    static bool HasToken(string? classes, Func<string, bool> match)
    {
        if (classes is null) return false;
        foreach (string token in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            if (match(token[(token.LastIndexOf(':') + 1)..])) return true; // bỏ tiền tố variant (md:, hover:)
        return false;
    }
}
