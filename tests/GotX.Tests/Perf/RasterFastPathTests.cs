using GotX;
using GotX.Raster;
using GotX.Test;
using GotX.Text;

/// <summary>
/// Đường nhanh của Quad (tô đoạn SIMD, lặp một pixel giữa cạnh) và raster theo ô hỏng phải cho ĐÚNG TỪNG PIXEL như đường SDF từng pixel / vẽ cả frame.
/// Dữ liệu ngẫu nhiên có seed: toạ độ lẻ, bán kính, viền từng cạnh khác nhau / trong suốt / gạch, nền trong suốt.
/// </summary>
static class RasterFastPathTests
{
    const int W = 420, H = 300;

    static Primitive RandomQuad(Random r)
    {
        float x = r.Next(-20, 300) + (r.Next(3) == 0 ? 0.37f : 0), y = r.Next(-20, 200) + (r.Next(3) == 0 ? 0.61f : 0);
        float w = r.Next(1, 360) + (r.Next(3) == 0 ? 0.5f : 0), h = r.Next(1, 240) + (r.Next(3) == 0 ? 0.25f : 0);
        float R() => r.Next(4) == 0 ? 0 : r.Next(0, 40);
        float B() => r.Next(3) == 0 ? 0 : r.Next(0, 7) + (r.Next(4) == 0 ? 0.5f : 0);
        Rgba C(bool alpha) => new((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble(), alpha && r.Next(2) == 0 ? (float)r.NextDouble() : 1);
        bool uniform = r.Next(2) == 0;
        float bw = B();
        var side = C(false);
        var p = new Primitive
        {
            Kind = PrimitiveKind.Quad, Bounds = new Bounds(x, y, w, h), Color = r.Next(5) == 0 ? new Rgba(0, 0, 0, 0) : C(true),
            Corners = r.Next(2) == 0 ? Corners.All(R()) : new Corners(R(), R(), R(), R()),
            Clip = r.Next(4) == 0 ? new Bounds(r.Next(0, 100), r.Next(0, 80), r.Next(50, 400), r.Next(50, 300)) : new Bounds(0, 0, W, H),
            BorderWidths = r.Next(3) == 0 ? default : r.Next(2) == 0 ? new Edges(B(), B(), B(), B()) : new Edges(bw, bw, bw, bw),
            BorderStyle = r.Next(8) == 0 ? BorderStyle.Dashed : r.Next(10) == 0 ? BorderStyle.Dotted : BorderStyle.Solid,
            BorderTop = side, BorderRight = uniform ? side : C(true), BorderBottom = uniform ? side : C(true), BorderLeft = uniform ? side : C(true),
        };
        return p;
    }

    static Scene RandomScene(int seed, int count)
    {
        var r = new Random(seed);
        var scene = new Scene(new GlyphAtlas()) { ClearColor = new Rgba(0.95f, 0.96f, 0.98f, 1) };
        for (int i = 0; i < count; i++)
        {
            var p = RandomQuad(r);
            if (r.Next(6) == 0) p.ClipShape = scene.AddClipShape(new ClipShape(new Bounds(r.Next(0, 80), r.Next(0, 60), r.Next(100, 340), r.Next(80, 240)), Corners.All(r.Next(0, 30)), default));
            scene.Add(p);
        }
        return scene;
    }

    /// <summary>Primitive đầu tiên mà đường nhanh khác SDF khi vẽ riêng lẻ (chẩn đoán).</summary>
    static string Culprit(Scene scene)
    {
        foreach (var p in scene.Primitives)
        {
            var one = new Scene(new GlyphAtlas()) { ClearColor = scene.ClearColor };
            var q = p;
            if (q.ClipShape != 0) q.ClipShape = one.AddClipShape(scene.Shape(q.ClipShape));
            one.Add(q);
            var a = new Canvas(); a.Resize(W, H); var b = new Canvas(); b.Resize(W, H);
            SoftwareRenderer.Render(one, a);
            SoftwareRenderer.QuadFastPaths = false;
            try { SoftwareRenderer.Render(one, b); } finally { SoftwareRenderer.QuadFastPaths = true; }
            if (Same(a, b)) continue;
            int i = 0; while (a.Pixels[i] == b.Pixels[i]) i++;
            return $"bounds {p.Bounds} corners {p.Corners} bw {p.BorderWidths} style {p.BorderStyle} color {p.Color} clipShape {p.ClipShape} px ({i % W},{i / W}) {a.Pixels[i]:X8} vs {b.Pixels[i]:X8}";
        }
        return "no single culprit";
    }

    static bool Same(Canvas a, Canvas b) => a.Pixels.AsSpan(0, a.Width * a.Height).SequenceEqual(b.Pixels.AsSpan(0, b.Width * b.Height));

    public static void Register(TestSuite suite)
    {
        suite.Add("perf/raster/quad-fast-paths-match-per-pixel-sdf", t =>
        {
            int bad = 0, total = 0; string? first = null;
            for (int seed = 0; seed < 300; seed++)
            {
                var scene = RandomScene(seed, 12);
                var fast = new Canvas(); fast.Resize(W, H);
                var slow = new Canvas(); slow.Resize(W, H);
                SoftwareRenderer.QuadFastPaths = true; SoftwareRenderer.Render(scene, fast);
                SoftwareRenderer.QuadFastPaths = false;
                try { SoftwareRenderer.Render(scene, slow); } finally { SoftwareRenderer.QuadFastPaths = true; }
                total++;
                if (!Same(fast, slow)) { bad++; first ??= $"seed {seed}: {Culprit(scene)}"; }
            }
            t.Assert.Equal(bad, 0, $"{bad}/{total} cảnh ngẫu nhiên: đường nhanh khác SDF từng pixel ({first})");
        });

        suite.Add("perf/raster/damaged-tiles-match-full-raster", t =>
        {
            int bad = 0, total = 0; string? first = null;
            const int tx = (W + SceneDamage.Tile - 1) / SceneDamage.Tile, ty = (H + SceneDamage.Tile - 1) / SceneDamage.Tile;
            for (int seed = 0; seed < 150; seed++)
            {
                var scene = RandomScene(1000 + seed, 25);
                var rnd = new Random(seed);
                var dirty = new bool[tx * ty];
                int count = 0;
                double density = 0.1 + 0.8 * rnd.NextDouble();
                for (int i = 0; i < dirty.Length; i++) if (rnd.NextDouble() < density) { dirty[i] = true; count++; }
                if (count == 0) continue;
                var full = new Canvas(); full.Resize(W, H);
                SoftwareRenderer.Render(scene, full);
                // Canvas giữ lại: nền cũ khác hẳn (rác), chỉ ô hỏng được vẽ lại; ô không hỏng phải khớp vẽ cả frame nên bắt đầu từ chính ảnh đầy đủ.
                var retained = new Canvas(); retained.Resize(W, H);
                full.Pixels.AsSpan(0, W * H).CopyTo(retained.Pixels);
                for (int ty0 = 0; ty0 < ty; ty0++)
                    for (int tx0 = 0; tx0 < tx; tx0++)
                        if (dirty[ty0 * tx + tx0])
                            for (int y = ty0 * SceneDamage.Tile; y < Math.Min(H, (ty0 + 1) * SceneDamage.Tile); y++)
                                for (int x = tx0 * SceneDamage.Tile; x < Math.Min(W, (tx0 + 1) * SceneDamage.Tile); x++) retained.Pixels[y * W + x] = 0xFFFF00FFu;
                long px = SoftwareRenderer.RenderDamaged(scene, retained, new SceneDamage.Result(false, false, tx, ty, dirty, count));
                total++;
                if (!Same(full, retained)) { bad++; first ??= $"seed {seed} ({count}/{tx * ty} ô)"; }
                t.Assert.True(px > 0 && px <= (long)W * H, $"seed {seed}: số pixel vẽ lại {px} ngoài khoảng");
            }
            t.Assert.Equal(bad, 0, $"{bad}/{total} cảnh: raster theo ô hỏng khác vẽ cả frame ({first})");
        });
    }
}
