namespace GotX.Raster;

public static partial class SoftwareRenderer
{
    // ---------- Glyph: blit mask alpha (biến đổi: lấy mẫu song tuyến) ----------

    static void Glyph(Canvas c, Scene scene, in Primitive p, in Shape clip)
    {
        var mask = scene.Atlas[p.Glyph];
        if (p.Transform.IsTransform) { GlyphTransformed(c, mask, p, clip); return; }
        int gx = (int)p.Bounds.X, gy = (int)p.Bounds.Y;
        var r = PixelRect(new Bounds(gx, gy, mask.Width, mask.Height), p.Clip, c);
        if (r.Empty) return;
        var col = p.Color;
        var px = c.Pixels;
        var alpha = mask.Alpha;
        for (int y = r.Y0; y < r.Y1; y++)
        {
            int row = y * c.Width, mrow = (y - gy) * mask.Width - gx;
            for (int x = r.X0; x < r.X1; x++)
            {
                byte m = alpha[mrow + x];
                if (m == 0) continue;
                float a = Div255[m] * col.A;
                if (clip.On) a *= clip.Coverage(x + 0.5f, y + 0.5f);
                Blend(ref px[row + x], col.R * a, col.G * a, col.B * a, a);
            }
        }
    }

    static void GlyphTransformed(Canvas c, Text.GlyphMask mask, in Primitive p, in Shape clip)
    {
        var r = Area(p, 1, c);
        if (r.Empty) return;
        var b = p.Bounds;
        var f = new Frame(b, p.Transform);
        var col = p.Color;
        var px = c.Pixels;
        for (int y = r.Y0; y < r.Y1; y++)
        {
            int row = y * c.Width;
            for (int x = r.X0; x < r.X1; x++)
            {
                f.Local(x + 0.5f, y + 0.5f, out float lx, out float ly);
                // về hệ toạ độ của bitmap glyph (tâm điểm ảnh tại +0.5)
                float m = Sample(mask, lx + b.Width / 2 - 0.5f, ly + b.Height / 2 - 0.5f);
                if (m <= 0.002f) continue;
                float a = m * col.A;
                if (clip.On) a *= clip.Coverage(x + 0.5f, y + 0.5f);
                Blend(ref px[row + x], col.R * a, col.G * a, col.B * a, a);
            }
        }
    }

    static float Sample(Text.GlyphMask m, float u, float v)
    {
        int x0 = (int)MathF.Floor(u), y0 = (int)MathF.Floor(v);
        float fx = u - x0, fy = v - y0;
        float At(int x, int y) => (uint)x < (uint)m.Width && (uint)y < (uint)m.Height ? m.Alpha[y * m.Width + x] / 255f : 0;
        float top = At(x0, y0) + (At(x0 + 1, y0) - At(x0, y0)) * fx;
        float bot = At(x0, y0 + 1) + (At(x0 + 1, y0 + 1) - At(x0, y0 + 1)) * fx;
        return top + (bot - top) * fy;
    }
}
