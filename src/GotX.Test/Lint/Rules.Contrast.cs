using GotX.Raster;

namespace GotX.Test;

/// <summary>Luật tương phản chữ – luật duy nhất cần ảnh đã dựng.</summary>
public static partial class UiLint
{
    /// <summary>
    /// <c>low-contrast</c>: tỉ lệ tương phản WCAG giữa màu chữ và NỀN THẬT phía sau nó. Nền không suy ra từ cây (nền có thể
    /// đến từ tổ tiên bất kỳ, có độ mờ, có lớp chồng) mà lấy từ ảnh: màu xuất hiện nhiều nhất trong hộp của node chữ –
    /// nét chữ thường chỉ phủ 15–30% hộp nên màu trội là nền. Nếu màu trội trùng màu chữ thì thử màu trội thứ hai
    /// (chữ rất đậm trong hộp chật); không có thì chữ thật sự lẫn vào nền.
    /// Bỏ qua: chữ trong phần tử disabled (WCAG miễn), chữ gần như trong suốt, chữ nằm dưới một overlay.
    /// Chữ lớn (≥ 24 px, hoặc ≥ 18.66 px đậm) chỉ cần 3:1 nên không có mức info.
    /// </summary>
    static void LowContrast(LintContext ctx)
    {
        var canvas = ctx.Canvas!;
        var covers = OverlayCovers(ctx);
        var counts = new Dictionary<uint, int>();
        foreach (var n in ctx.Nodes)
        {
            if (n.Kind != "text" || string.IsNullOrWhiteSpace(n.Text) || n.Transformed) continue;
            var seen = n.Visible.Intersect(ctx.Tree.ViewportBounds);
            float alpha = n.Color.A * n.Opacity;
            if (seen.IsEmpty || alpha < 0.05f || ctx.Tree.AnyAncestor(n, static a => a.Disabled)) continue;
            if (CoveredByLaterLayer(ctx, covers, n, seen)) continue;
            if (!DominantColors(canvas, seen.Scale(ctx.Scale), counts, out var first, out var second, out float secondShare)) continue;

            var bg = first;
            var ink = Over(n.Color, alpha, bg);
            float ratio = Ratio(ink, bg);
            if (ratio < 1.15f && secondShare >= 0.15f)
            {
                // Màu trội gần như chính là màu chữ: nhiều khả năng nét chữ phủ quá nửa hộp → nền là màu trội thứ hai.
                bg = second;
                ink = Over(n.Color, alpha, bg);
                ratio = Ratio(ink, bg);
            }
            bool large = n.FontSize >= 24 || (n.FontSize >= 18.66f && n.Weight >= FontWeight.Bold);
            Severity? severity = ratio < ctx.Options.ContrastWarn ? Severity.Warning
                : !large && ratio < ctx.Options.ContrastInfo ? Severity.Info : null;
            if (severity is not { } sev) continue;
            ctx.Add("low-contrast", sev, n,
                $"tương phản {Fmt.N(ratio)}:1 (cần ≥ {(large ? "3" : "4.5")}:1) — chữ {Fmt.C(ink)} trên nền {Fmt.C(bg)}, cỡ {Fmt.N(n.FontSize)}px");
        }
    }

    /// <summary>Hộp của các phần tử có nền thuộc lớp overlay (mọi gốc sau gốc đầu), kèm chỉ số gốc.</summary>
    static List<(int Root, Bounds Area)> OverlayCovers(LintContext ctx)
    {
        var covers = new List<(int, Bounds)>();
        int root = -1, roots = 0;
        foreach (var n in ctx.Nodes)
        {
            if (n.Parent < 0) { root = n.Index; roots++; }
            if (roots > 1 && n.Background.A > 0 && LintContext.Shown(n)) covers.Add((root, n.Visible));
        }
        return covers;
    }

    static bool CoveredByLaterLayer(LintContext ctx, List<(int Root, Bounds Area)> covers, UiNode n, Bounds seen)
    {
        if (covers.Count == 0) return false;
        int root = ctx.Tree.Root(n).Index;
        foreach (var (r, area) in covers)
            if (r > root && !area.Intersect(seen).IsEmpty) return true;
        return false;
    }

    /// <summary>Hai màu xuất hiện nhiều nhất trong vùng pixel thiết bị <paramref name="area"/>.</summary>
    static bool DominantColors(Canvas c, Bounds area, Dictionary<uint, int> counts, out Rgba first, out Rgba second, out float secondShare)
    {
        first = second = default; secondShare = 0;
        int x0 = Math.Max(0, (int)MathF.Floor(area.X)), y0 = Math.Max(0, (int)MathF.Floor(area.Y));
        int x1 = Math.Min(c.Width, (int)MathF.Ceiling(area.Right)), y1 = Math.Min(c.Height, (int)MathF.Ceiling(area.Bottom));
        if (x1 <= x0 || y1 <= y0) return false;
        counts.Clear();
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
            {
                uint p = c.Pixels[y * c.Width + x];
                counts[p] = counts.GetValueOrDefault(p) + 1;
            }
        uint a = 0, b = 0; int na = 0, nb = 0;
        foreach (var kv in counts)
        {
            if (kv.Value > na) { b = a; nb = na; a = kv.Key; na = kv.Value; }
            else if (kv.Value > nb) { b = kv.Key; nb = kv.Value; }
        }
        first = Pixels.Unpack(a);
        second = Pixels.Unpack(b);
        secondShare = nb / (float)((x1 - x0) * (y1 - y0));
        return true;
    }

    static Rgba Over(Rgba fg, float alpha, Rgba bg) =>
        new(fg.R * alpha + bg.R * (1 - alpha), fg.G * alpha + bg.G * (1 - alpha), fg.B * alpha + bg.B * (1 - alpha), 1);

    /// <summary>Tỉ lệ tương phản WCAG 2.x giữa hai màu đặc (1..21).</summary>
    public static float Ratio(Rgba a, Rgba b)
    {
        float la = Luminance(a), lb = Luminance(b);
        return (MathF.Max(la, lb) + 0.05f) / (MathF.Min(la, lb) + 0.05f);
    }

    static float Luminance(Rgba c)
    {
        static float Lin(float v) => v <= 0.04045f ? v / 12.92f : MathF.Pow((v + 0.055f) / 1.055f, 2.4f);
        return 0.2126f * Lin(c.R) + 0.7152f * Lin(c.G) + 0.0722f * Lin(c.B);
    }
}
