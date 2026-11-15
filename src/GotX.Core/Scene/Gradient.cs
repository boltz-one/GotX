namespace GotX;

public enum GradientKind : byte { None, Linear, Radial }

/// <summary>
/// Gradient của một Quad (nằm trong <see cref="Scene"/>, primitive trỏ tới bằng chỉ số). Hình học tính theo Bounds của
/// primitive (trước biến đổi) nên phát lại kèm tịnh tiến không phải sửa gì. Màu chưa premultiply, đã nhân opacity.
/// Linear: <see cref="Corner"/> = false thì (DirX, DirY) = (sin, -cos) của góc CSS; true thì đó là dấu của góc đích
/// (to top right = (+1, -1)) và hướng thật phụ thuộc tỉ lệ hộp, như CSS. Radial: ellipse farthest-corner tại tâm.
/// Vị trí điểm dừng là phần của đường gradient (0..1), đã sắp không giảm. Count = 2 hoặc 3 (có via).
/// </summary>
public readonly record struct GradientFill(GradientKind Kind, float DirX, float DirY, bool Corner, bool Oklab,
    Rgba C0, Rgba C1, Rgba C2, float P0, float P1, float P2, int Count);

/// <summary>
/// Công thức gradient dùng chung: renderer CPU gọi trực tiếp, shader HLSL (D3D11Renderer.Gradient.cs) chép y hệt.
/// Màu được nội suy dạng premultiplied (như CSS), trong Oklab (mặc định của Tailwind v4: "in oklab") hoặc sRGB.
/// </summary>
public static class GradientMath
{
    /// <summary>Điểm dừng ở dạng nội suy: (L·a, a·a, b·a, alpha) với Oklab, (r·a, g·a, b·a, alpha) với sRGB.</summary>
    public static (float X, float Y, float Z, float A) Prepare(Rgba c, bool oklab)
    {
        if (!oklab) return (c.R * c.A, c.G * c.A, c.B * c.A, c.A);
        ToOklab(c.R, c.G, c.B, out float l, out float a, out float b);
        return (l * c.A, a * c.A, b * c.A, c.A);
    }

    /// <summary>Tham số t (0 = điểm đầu, 1 = điểm cuối) tại điểm cục bộ (lx, ly) – gốc ở tâm hộp nửa kích thước (hw, hh).</summary>
    public static float T(in GradientFill g, float lx, float ly, float hw, float hh)
    {
        if (g.Kind == GradientKind.Radial)
        {
            // farthest-corner của ellipse cùng tỉ lệ hộp: bán trục = nửa cạnh × √2
            float rx = MathF.Max(hw, 1e-4f) * 1.41421356f, ry = MathF.Max(hh, 1e-4f) * 1.41421356f;
            float u = lx / rx, v = ly / ry;
            return MathF.Sqrt(u * u + v * v);
        }
        float dx = g.DirX, dy = g.DirY;
        if (g.Corner)
        {
            // "to top right": đường 50% đi qua hai góc còn lại → hướng vuông góc với đường chéo đó
            dx = g.DirX * hh; dy = g.DirY * hw;
            float n = MathF.Sqrt(dx * dx + dy * dy);
            if (n > 0) { dx /= n; dy /= n; }
        }
        float len = 2 * (MathF.Abs(hw * dx) + MathF.Abs(hh * dy)); // độ dài đường gradient theo CSS
        return len > 0 ? (lx * dx + ly * dy) / len + 0.5f : 0.5f;
    }

    /// <summary>Đổi màu đã nội suy về sRGB premultiplied (0..1).</summary>
    public static (float R, float G, float B, float A) Finish(float x, float y, float z, float a, bool oklab)
    {
        if (a <= 0) return (0, 0, 0, 0);
        if (!oklab) return (Math.Clamp(x, 0, a), Math.Clamp(y, 0, a), Math.Clamp(z, 0, a), a);
        FromOklab(x / a, y / a, z / a, out float r, out float g, out float b);
        return (r * a, g * a, b * a, a);
    }

    public static void ToOklab(float r, float g, float b, out float L, out float A, out float B)
    {
        r = ToLinear(r); g = ToLinear(g); b = ToLinear(b);
        float l = MathF.Cbrt(0.4122214708f * r + 0.5363325363f * g + 0.0514459929f * b);
        float m = MathF.Cbrt(0.2119034982f * r + 0.6806995451f * g + 0.1073969566f * b);
        float s = MathF.Cbrt(0.0883024619f * r + 0.2817188376f * g + 0.6299787005f * b);
        L = 0.2104542553f * l + 0.7936177850f * m - 0.0040720468f * s;
        A = 1.9779984951f * l - 2.4285922050f * m + 0.4505937099f * s;
        B = 0.0259040371f * l + 0.7827717662f * m - 0.8086757660f * s;
    }

    public static void FromOklab(float L, float A, float B, out float r, out float g, out float b)
    {
        float l = L + 0.3963377774f * A + 0.2158037573f * B;
        float m = L - 0.1055613458f * A - 0.0638541728f * B;
        float s = L - 0.0894841775f * A - 1.2914855480f * B;
        l = l * l * l; m = m * m * m; s = s * s * s;
        r = ToSrgb(4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s);
        g = ToSrgb(-1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s);
        b = ToSrgb(-0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s);
    }

    static float ToLinear(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);

    static float ToSrgb(float c)
    {
        c = Math.Clamp(c, 0, 1);
        return c <= 0.0031308f ? 12.92f * c : 1.055f * MathF.Pow(c, 1 / 2.4f) - 0.055f;
    }
}

/// <summary>Lấy mẫu một gradient cho từng pixel (renderer CPU): điểm dừng được đổi sang dạng nội suy một lần.</summary>
public readonly struct GradientSampler
{
    readonly GradientFill _g;
    readonly float _hw, _hh;
    readonly (float X, float Y, float Z, float A) _s0, _s1, _s2;

    public GradientSampler(in GradientFill g, float hw, float hh)
    {
        _g = g; _hw = hw; _hh = hh;
        _s0 = GradientMath.Prepare(g.C0, g.Oklab);
        _s1 = GradientMath.Prepare(g.C1, g.Oklab);
        _s2 = GradientMath.Prepare(g.C2, g.Oklab);
    }

    /// <summary>Màu sRGB premultiplied tại điểm cục bộ (lx, ly).</summary>
    public (float R, float G, float B, float A) Sample(float lx, float ly)
    {
        float t = GradientMath.T(_g, lx, ly, _hw, _hh);
        var last = _g.Count == 3 ? _s2 : _s1;
        float pLast = _g.Count == 3 ? _g.P2 : _g.P1;
        (float X, float Y, float Z, float A) c;
        if (t <= _g.P0) c = _s0;
        else if (t >= pLast) c = last;
        else if (t < _g.P1 || _g.Count == 2) c = Mix(_s0, _s1, _g.P0, _g.P1, t);
        else c = Mix(_s1, _s2, _g.P1, _g.P2, t);
        return GradientMath.Finish(c.X, c.Y, c.Z, c.A, _g.Oklab);
    }

    static (float, float, float, float) Mix((float X, float Y, float Z, float A) a, (float X, float Y, float Z, float A) b, float p0, float p1, float t)
    {
        float f = p1 > p0 ? (t - p0) / (p1 - p0) : 1;
        return (a.X + (b.X - a.X) * f, a.Y + (b.Y - a.Y) * f, a.Z + (b.Z - a.Z) * f, a.A + (b.A - a.A) * f);
    }
}
