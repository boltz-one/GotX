namespace GotX.Raster;

public static partial class SoftwareRenderer
{
    /// <summary>
    /// Vẽ lại chỉ các ô hỏng vào canvas đã giữ từ frame trước (prototype, xem <see cref="SceneDamage"/>): mỗi dải ô liền nhau trên một hàng
    /// được xoá về màu nền rồi vẽ lại mọi primitive chạm vào dải đó, cắt theo dải. Kết quả trùng từng pixel với <see cref="Render"/>.
    /// Trả số pixel đã vẽ lại. Full → vẽ cả frame.
    /// </summary>
    public static long RenderDamaged(Scene scene, Canvas canvas, in SceneDamage.Result damage)
    {
        if (damage.Empty) return 0;
        if (damage.Full || damage.Dirty is null)
        {
            Render(scene, canvas);
            return (long)canvas.Width * canvas.Height;
        }
        long pixels = 0;
        var prims = scene.Primitives;
        for (int ty = 0; ty < damage.TilesY; ty++)
        {
            for (int tx = 0; tx < damage.TilesX; tx++)
            {
                if (!damage.Dirty[ty * damage.TilesX + tx]) continue;
                int run = tx;
                while (run + 1 < damage.TilesX && damage.Dirty[ty * damage.TilesX + run + 1]) run++;
                int x0 = tx * SceneDamage.Tile, x1 = Math.Min(canvas.Width, (run + 1) * SceneDamage.Tile);
                int y0 = ty * SceneDamage.Tile, y1 = Math.Min(canvas.Height, (ty + 1) * SceneDamage.Tile);
                pixels += (long)(x1 - x0) * (y1 - y0);
                var region = new Bounds(x0, y0, x1 - x0, y1 - y0);
                FillRegion(canvas, scene.ClearColor, x0, y0, x1, y1);
                foreach (ref readonly var p in prims)
                {
                    if (SceneDamageMath.Visual(scene, p).Intersect(region).IsEmpty) continue;
                    var q = p;
                    q.Clip = p.Clip.Intersect(region);
                    DrawOne(canvas, scene, q);
                }
                tx = run;
            }
        }
        return pixels;
    }

    static void FillRegion(Canvas c, Rgba clear, int x0, int y0, int x1, int y1)
    {
        uint v = Canvas.Pack(clear.R * clear.A, clear.G * clear.A, clear.B * clear.A, clear.A);
        for (int y = y0; y < y1; y++) c.Pixels.AsSpan(y * c.Width + x0, x1 - x0).Fill(v);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    static void DrawOne(Canvas canvas, Scene scene, in Primitive p)
    {
        var clip = Shape.Of(scene, p.ClipShape);
        switch (p.Kind)
        {
            case PrimitiveKind.Quad: Quad(canvas, scene, p, clip); break;
            case PrimitiveKind.Shadow: Shadow(canvas, p, clip); break;
            case PrimitiveKind.Glyph: Glyph(canvas, scene, p, clip); break;
            case PrimitiveKind.Image: Image(canvas, p, clip); break;
        }
    }
}
