namespace GotX.Test;

/// <summary>Luật nhìn từng phần tử riêng lẻ: lưới pixel, kích thước 0, nằm ngoài cửa sổ, vùng bấm nhỏ.</summary>
public static partial class UiLint
{
    /// <summary>Toạ độ (logical) sau khi nhân scale có lệch khỏi lưới pixel thiết bị quá 0.02 px không.</summary>
    static bool OffGrid(float v, float scale)
    {
        float d = v * scale;
        return MathF.Abs(d - MathF.Round(d)) > 0.02f;
    }

    static bool Paints(UiNode n) =>
        n.Kind == "image" || n.Background.A > 0 || n.Border.Top > 0 || n.Border.Right > 0 || n.Border.Bottom > 0 || n.Border.Left > 0;

    /// <summary>
    /// <c>subpixel</c>: phần tử có vẽ nền/viền/ảnh mà mép không rơi đúng pixel thiết bị → mép bị nhoè (anti-alias nửa pixel).
    /// Core tự làm tròn glyph và icon nên chữ không bị tính. Một vị trí lẻ thường do MỘT tổ tiên gây ra rồi kéo theo
    /// cả cây con: luật gom các phần tử bị ảnh hưởng về tổ tiên đầu tiên có vị trí lẻ và chỉ báo một lần ở đó.
    /// </summary>
    static void Subpixel(LintContext ctx)
    {
        float scale = ctx.Scale;
        var groups = new Dictionary<int, (int Count, UiNode Example)>();
        var order = new List<int>();
        foreach (var n in ctx.Nodes)
        {
            if (n.Transformed || !LintContext.Shown(n) || !Paints(n)) continue;
            var s = n.Screen;
            bool x = OffGrid(s.X, scale), y = OffGrid(s.Y, scale);
            if (!x && !y && !OffGrid(s.Right, scale) && !OffGrid(s.Bottom, scale)) continue;
            var origin = n;
            while ((x || y) && origin.Parent >= 0)
            {
                var p = ctx.Nodes[origin.Parent];
                if (p.Transformed || !((x && OffGrid(p.Screen.X, scale)) || (y && OffGrid(p.Screen.Y, scale)))) break;
                origin = p;
            }
            if (groups.TryGetValue(origin.Index, out var g)) groups[origin.Index] = (g.Count + 1, g.Example);
            else { groups[origin.Index] = (1, n); order.Add(origin.Index); }
        }
        foreach (int index in order)
        {
            var origin = ctx.Nodes[index];
            var (count, example) = groups[index];
            string at = $"@{Fmt.N(scale)}x";
            if (count == 1 && example == origin)
                ctx.Add("subpixel", Severity.Warning, origin, $"mép {Edges(origin.Screen, scale)} không nằm trên pixel thiết bị ({at}) → nền/viền bị nhoè");
            else
                ctx.Add("subpixel", Severity.Warning, origin,
                    $"vị trí lẻ {Edges(origin.Screen, scale)} ({at}) làm {count} phần tử có nền/viền bên trong bị nhoè mép, vd. {ctx.Tree.Describe(example)}", example);
        }
    }

    /// <summary>Liệt kê các mép lệch lưới theo pixel thiết bị: <c>left=12.5 bottom=40.33</c>.</summary>
    static string Edges(Bounds s, float scale)
    {
        var parts = new List<string>();
        if (OffGrid(s.X, scale)) parts.Add("left=" + Fmt.N(s.X * scale));
        if (OffGrid(s.Y, scale)) parts.Add("top=" + Fmt.N(s.Y * scale));
        if (OffGrid(s.Right, scale)) parts.Add("right=" + Fmt.N(s.Right * scale));
        if (OffGrid(s.Bottom, scale)) parts.Add("bottom=" + Fmt.N(s.Bottom * scale));
        return parts.Count == 0 ? "(cây con)" : string.Join(" ", parts);
    }

    /// <summary>
    /// <c>zero-size</c>: phần tử khai báo viền/nền nhưng rộng hoặc cao bằng 0 nên không vẽ gì. Có viền → warning
    /// (bẫy border-box của Core: <c>h-0 border-t</c> không ra đường kẻ); chỉ có nền → info (thanh tiến độ 0% là hợp lệ).
    /// </summary>
    static void ZeroSize(LintContext ctx)
    {
        foreach (var n in ctx.Nodes)
        {
            if (n.Kind is "text" or "view" || (n.Screen.Width > 0.01f && n.Screen.Height > 0.01f)) continue;
            bool border = n.Border.Top > 0 || n.Border.Right > 0 || n.Border.Bottom > 0 || n.Border.Left > 0;
            string size = $"{Fmt.N(n.Layout.Width)}×{Fmt.N(n.Layout.Height)}";
            if (border) ctx.Add("zero-size", Severity.Warning, n, $"có viền nhưng hộp {size} → không vẽ gì (kích thước là border-box: đặt chiều cao/rộng bằng độ dày nét)");
            else if (n.Background.A > 0) ctx.Add("zero-size", Severity.Info, n, $"có nền {Fmt.C(n.Background)} nhưng hộp {size} → không vẽ gì");
        }
    }

    /// <summary>
    /// <c>offscreen</c>: phần tử nằm hẳn ngoài cửa sổ mà không thuộc vùng cuộn nào → người dùng không cách gì thấy được.
    /// Chỉ báo node ngoài cùng (cha còn trong cửa sổ); bỏ qua phần tử đang bị transform (panel trượt vào) và phần tử
    /// vốn đã bị overflow-hidden của tổ tiên cắt mất.
    /// </summary>
    static void Offscreen(LintContext ctx)
    {
        var vp = ctx.Tree.ViewportBounds;
        foreach (var n in ctx.Nodes)
        {
            var s = n.Screen;
            if (s.IsEmpty || n.Transformed || !s.Intersect(vp).IsEmpty) continue;
            if (n.Clip.Width < 1e8f && s.Intersect(n.Clip).IsEmpty) continue; // bị cắt bởi tổ tiên, không phải do cửa sổ
            if (n.Parent >= 0 && ctx.Nodes[n.Parent].Screen is { IsEmpty: false } ps && ps.Intersect(vp).IsEmpty) continue;
            if (ctx.Tree.ScrollAncestor(n) is not null) continue;
            ctx.Add("offscreen", Severity.Warning, n,
                $"nằm hẳn ngoài cửa sổ {Fmt.N(vp.Width)}×{Fmt.N(vp.Height)} và không ở trong vùng cuộn nào");
        }
    }

    /// <summary><c>small-target</c>: vùng bấm có cạnh nhỏ hơn <see cref="LintOptions.MinTarget"/> (mặc định 16 px) – khó bấm trúng.</summary>
    static void SmallTarget(LintContext ctx)
    {
        float min = ctx.Options.MinTarget;
        foreach (var n in ctx.Nodes)
        {
            if (!n.Clickable || !LintContext.Shown(n)) continue;
            var s = n.Screen;
            if (s.Width >= min - 0.01f && s.Height >= min - 0.01f) continue;
            ctx.Add("small-target", Severity.Warning, n, $"vùng bấm {Fmt.N(s.Width)}×{Fmt.N(s.Height)} nhỏ hơn {Fmt.N(min)}×{Fmt.N(min)}");
        }
    }
}
