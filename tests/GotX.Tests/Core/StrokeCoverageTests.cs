using GotX;
using GotX.Test;
using GotX.Text;

/// <summary>Nét vector: độ phủ phải khớp diện tích giải tích (không bị cộng dồn nhiều lần ở mép nơi các đoạn chồng nhau).</summary>
static class StrokeCoverageTests
{
    static GlyphMask Mask(VectorPath p, int size) { var atlas = new GlyphAtlas(); return atlas[atlas.GetPath(p, size, size)]; }

    public static void Register(TestSuite suite)
    {
        suite.Add("core/stroke/annulus-area-matches-analytic", t =>
        {
            // circle r=10 trong lưới 24, nét 2, vẽ ở 16 px: bán kính đường tâm 6.667, nét 1.333 px → diện tích 2π·r·w
            var m = Mask(VectorPath.Icon("circle 12 12 10"), 16);
            double ink = m.Alpha.Sum(a => a / 255.0), expected = 2 * Math.PI * (10 * 16 / 24.0) * (2 * 16 / 24.0);
            t.Assert.True(Math.Abs(ink - expected) / expected < 0.04, $"tổng độ phủ {ink:0.0} so với diện tích giải tích {expected:0.0}");
            t.Assert.True(m.Alpha.Max() >= 230 && m.Alpha.Max() <= 255, $"đỉnh độ phủ {m.Alpha.Max()}");
            // một lát cắt ngang qua tâm: mỗi mép nét chiếm ≤ 2 pixel, không phải "2 px đặc"
            int row = m.Height / 2, thick = 0;
            for (int x = 0; x < m.Width / 2; x++) if (m.Alpha[row * m.Width + x] > 8) thick++;
            t.Assert.True(thick <= 3, $"bề dày nét theo lát cắt: {thick} pixel có mực (≤ 3)");
        });

        suite.Add("core/stroke/straight-line-edges-are-exact-area", t =>
        {
            // đường ngang tại y=12/24 ở 16 px → y = 8: giữa hai hàng pixel; nét 1.333 → mỗi hàng phủ 0.667
            var m = Mask(VectorPath.Icon("line 2 12 22 12"), 16);
            int col = m.Width / 2;
            var cov = Enumerable.Range(0, m.Height).Select(y => m.Alpha[y * m.Width + col] / 255.0).Where(v => v > 0.001).ToArray();
            t.Assert.Equal(cov.Length, 2, "đúng hai hàng pixel có mực");
            t.Assert.True(cov.All(v => Math.Abs(v - 0.667) < 0.02), $"mỗi hàng phủ ≈ 0.667 ({string.Join(", ", cov.Select(v => v.ToString("0.000")))})");
        });

        suite.Add("core/stroke/overlapping-joints-do-not-darken", t =>
        {
            // đường gấp khúc có nhiều đỉnh gần nhau: độ phủ không vượt quá của một đoạn thẳng đơn lẻ
            var single = Mask(VectorPath.Icon("line 4 12 20 12"), 24);
            var poly = Mask(VectorPath.Icon("polyline 4 12 8 12 12 12 16 12 20 12"), 24);
            t.Assert.Equal(poly.Alpha.Sum(a => (int)a), single.Alpha.Sum(a => (int)a), "đỉnh dư thừa trên một đường thẳng không đổi độ phủ");
        });
    }
}
