namespace GotX.Test;

/// <summary>Luật so các anh em trong cùng một cha: căn hàng lệch vài pixel, chồng lên nhau.</summary>
public static partial class UiLint
{
    // Cha có quá nhiều con thì chỉ so mỗi con với vài anh em đứng sau nó (tránh O(n²) trên bảng/danh sách dài).
    const int PairWindow = 12;
    // Hai phần tử chênh kích thước quá tỉ lệ này thì không coi là "định căn với nhau" (khối cao cạnh một icon nhỏ).
    const float MaxSizeRatio = 1.6f;
    // Core bắt mép vào lưới pixel, nên hai hộp có độ rộng chẵn/lẻ khác nhau lệch tâm đúng 0.5px là điều tất yếu: chỉ tính từ trên mức đó.
    const float MinMisalign = 0.75f;

    static void Siblings(LintContext ctx)
    {
        var flow = new List<UiNode>();
        var pairs = new List<(UiNode A, UiNode B, float D, string What)>();
        foreach (var parent in ctx.Nodes)
        {
            if (parent.Children.Count < 2) continue;
            flow.Clear();
            foreach (int c in parent.Children)
                if (LintContext.InFlow(ctx.Nodes[c])) flow.Add(ctx.Nodes[c]);
            if (flow.Count < 2) continue;
            pairs.Clear();
            for (int i = 0; i < flow.Count; i++)
                for (int j = i + 1; j < flow.Count && j <= i + PairWindow; j++)
                {
                    var a = flow[i]; var b = flow[j];
                    if (ctx.On("overlap")) Overlap(ctx, parent, a, b);
                    // hàng căn baseline: các hộp khác chiều cao lệch đỉnh/đáy là chủ ý (chỉ dòng chữ đầu thẳng hàng)
                    bool baseline = parent.Display == Display.Flex && (a.AlignSelf ?? parent.AlignItems) == Align.Baseline && (b.AlignSelf ?? parent.AlignItems) == Align.Baseline;
                    if (!baseline && ctx.On("misaligned") && LintContext.Shown(a) && LintContext.Shown(b) && Misaligned(a.Screen, b.Screen, ctx.Options.AlignMax, out float d, out string what))
                        pairs.Add((a, b, d, what));
                }
            ReportMisaligned(ctx, pairs);
        }
    }

    /// <summary>
    /// <c>misaligned</c>. Hai anh em được coi là cùng HÀNG khi đứng cạnh nhau (không chồng theo trục ngang) và khoảng
    /// dọc trùng nhau ít nhất nửa chiều cao của cái thấp hơn; cùng CỘT thì ngược lại. Khi đó người viết hẳn đã định căn
    /// chúng theo đỉnh, tâm hoặc đáy – ít nhất một trong ba phải khớp. Nếu cả ba đều lệch và độ lệch nhỏ nhất nằm trong
    /// (0, AlignMax] thì đó là "gần thẳng hàng mà lệch". Lệch nhiều hơn AlignMax coi là bố cục cố ý.
    /// Nhờ lấy min của ba cạnh, chữ khác cỡ căn giữa nhau (đỉnh/đáy lệch, tâm khớp) không bị báo.
    /// Lệch dưới nửa pixel cũng bỏ qua: mắt không thấy là "lệch hàng", và nguyên nhân (kích thước lẻ) đã có <c>subpixel</c> báo.
    /// </summary>
    static bool Misaligned(Bounds a, Bounds b, float max, out float d, out string what)
    {
        d = 0; what = "";
        float ovY = MathF.Min(a.Bottom, b.Bottom) - MathF.Max(a.Y, b.Y), ovX = MathF.Min(a.Right, b.Right) - MathF.Max(a.X, b.X);
        bool row = ovX <= 0.5f && ovY >= 0.5f * MathF.Min(a.Height, b.Height);
        bool col = ovY <= 0.5f && ovX >= 0.5f * MathF.Min(a.Width, b.Width);
        if (row == col) return false;
        float sa = row ? a.Height : a.Width, sb = row ? b.Height : b.Width;
        if (MathF.Max(sa, sb) > MaxSizeRatio * MathF.Min(sa, sb)) return false;
        float d0 = row ? a.Y - b.Y : a.X - b.X;                                           // đỉnh / trái
        float d2 = row ? a.Bottom - b.Bottom : a.Right - b.Right;                         // đáy / phải
        float d1 = (d0 + d2) / 2;                                                         // tâm
        float m = MathF.Min(MathF.Abs(d0), MathF.Min(MathF.Abs(d1), MathF.Abs(d2)));
        if (m < MinMisalign || m > max + 0.001f) return false;
        d = m;
        string[] names = row ? ["top", "centerY", "bottom"] : ["left", "centerX", "right"];
        string nearest = m == MathF.Abs(d1) ? names[1] : m == MathF.Abs(d0) ? names[0] : names[2];
        what = $"{(row ? "cùng hàng" : "cùng cột")}, gần khớp nhất là {nearest} (Δ{names[0]}={Fmt.N(MathF.Abs(d0))} Δ{names[1]}={Fmt.N(MathF.Abs(d1))} Δ{names[2]}={Fmt.N(MathF.Abs(d2))})";
        return true;
    }

    /// <summary>
    /// Một phần tử lệch trong hàng N phần tử tạo ra N−1 cặp lệch. Lặp: chọn phần tử dính nhiều cặp nhất làm thủ phạm,
    /// báo một lần (so với anh em đầu tiên), bỏ mọi cặp của nó – cho tới khi hết cặp.
    /// </summary>
    static void ReportMisaligned(LintContext ctx, List<(UiNode A, UiNode B, float D, string What)> pairs)
    {
        var degree = new Dictionary<int, int>();
        while (pairs.Count > 0)
        {
            degree.Clear();
            foreach (var p in pairs)
            {
                degree[p.A.Index] = degree.GetValueOrDefault(p.A.Index) + 1;
                degree[p.B.Index] = degree.GetValueOrDefault(p.B.Index) + 1;
            }
            int culprit = -1, best = 0;
            foreach (var kv in degree)
                if (kv.Value > best || (kv.Value == best && kv.Key > culprit)) { culprit = kv.Key; best = kv.Value; }
            var first = pairs.Find(p => p.A.Index == culprit || p.B.Index == culprit);
            var node = first.A.Index == culprit ? first.A : first.B;
            var other = first.A.Index == culprit ? first.B : first.A;
            ctx.Add("misaligned", Severity.Warning, node,
                $"lệch {Fmt.N(first.D)}px so với anh em {ctx.Tree.Label(other)} {Fmt.B(other.Screen)}: {first.What}", other);
            pairs.RemoveAll(p => p.A.Index == culprit || p.B.Index == culprit);
        }
    }

    /// <summary>
    /// <c>overlap</c>: hai anh em trong luồng (không absolute, không transform) đè lên nhau hơn 1 px theo cả hai trục.
    /// Bố cục flex/block/grid không bao giờ tự sinh ra điều này, trừ khi nội dung tràn hoặc dùng margin âm – trường hợp
    /// sau (<c>-ml-2</c>, <c>-space-x-2</c>) được coi là cố ý và bỏ qua, cũng như các ô grid cố tình đặt trùng nhau.
    /// </summary>
    static void Overlap(LintContext ctx, UiNode parent, UiNode a, UiNode b)
    {
        var i = a.Screen.Intersect(b.Screen);
        if (i.Width <= 1 || i.Height <= 1) return;
        if (parent.Display == Display.Grid && a.Screen == b.Screen) return;
        if (ctx.NegativeMargin(a) || ctx.NegativeMargin(b)) return;
        ctx.Add("overlap", Severity.Warning, b,
            $"đè lên anh em {ctx.Tree.Label(a)} {Fmt.B(a.Screen)} một vùng {Fmt.N(i.Width)}×{Fmt.N(i.Height)}px", a);
    }
}
