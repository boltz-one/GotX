namespace GotX.Raster;

public static partial class SoftwareRenderer
{
    // ---------- Shadow: SDF + làm mờ Gauss (dùng hàm erf) ----------

    static void Shadow(Canvas c, in Primitive p, in Shape clip)
    {
        float sigma = p.Blur / 2;
        var r = Area(p, sigma * 3 + 1, c);
        if (r.Empty) return;

        var b = p.Bounds;
        float hw = b.Width / 2, hh = b.Height / 2;
        var f = new Frame(b, p.Transform);
        var corners = ClampCorners(p.Corners, MathF.Max(0, MathF.Min(hw, hh)));
        var col = p.Color;
        float inv = sigma > 0 ? 1f / (sigma * 1.41421356f) : 0;
        var px = c.Pixels;

        for (int y = r.Y0; y < r.Y1; y++)
        {
            int row = y * c.Width;
            float py = y + 0.5f;
            for (int x = r.X0; x < r.X1; x++)
            {
                float pxx = x + 0.5f;
                f.Local(pxx, py, out float lx, out float ly);
                float d = RoundedBox(lx, ly, hw, hh, PickRadius(corners, lx, ly));
                float a = sigma > 0 ? 0.5f * (1 - Erf(d * inv)) : Math.Clamp(0.5f - d, 0, 1);
                a *= col.A;
                if (clip.On && a > 0.002f) a *= clip.Coverage(pxx, py);
                if (a > 0.002f) Blend(ref px[row + x], col.R * a, col.G * a, col.B * a, a);
            }
        }
    }

    /// <summary>Xấp xỉ Abramowitz–Stegun 7.1.26 (sai số ~1.5e-7).</summary>
    static float Erf(float x)
    {
        float s = MathF.Sign(x); x = MathF.Abs(x);
        float t = 1f / (1f + 0.3275911f * x);
        float y = 1f - ((((1.061405429f * t - 1.453152027f) * t + 1.421413741f) * t - 0.284496736f) * t + 0.254829592f) * t * MathF.Exp(-x * x);
        return s * y;
    }
}
