namespace GotX;

/// <summary>Hash 64-bit của một primitive (mọi trường + nội dung vùng cắt / gradient nó trỏ tới) và hộp nhìn thấy của nó, cho <see cref="SceneDamage"/>.</summary>
public static class SceneDamageMath
{
    const ulong Prime = 1099511628211UL;

    static void Mix(ref ulong h, uint v) { h = (h ^ v) * Prime; h ^= h >> 29; }
    static void Mix(ref ulong h, float v) => Mix(ref h, BitConverter.SingleToUInt32Bits(v));
    static void Mix(ref ulong h, in Bounds b) { Mix(ref h, b.X); Mix(ref h, b.Y); Mix(ref h, b.Width); Mix(ref h, b.Height); }
    static void Mix(ref ulong h, in Rgba c) { Mix(ref h, c.R); Mix(ref h, c.G); Mix(ref h, c.B); Mix(ref h, c.A); }
    static void Mix(ref ulong h, in Corners c) { Mix(ref h, c.TopLeft); Mix(ref h, c.TopRight); Mix(ref h, c.BottomRight); Mix(ref h, c.BottomLeft); }
    static void Mix(ref ulong h, in Mat2 m) { Mix(ref h, m.M11); Mix(ref h, m.M12); Mix(ref h, m.M21); Mix(ref h, m.M22); }

    /// <summary>
    /// Hash theo từng từ 8 byte của chính struct Primitive (blittable; byte đệm luôn bằng 0 vì primitive dựng từ default/object initializer).
    /// Byte đệm khác 0 chỉ làm hash khác đi (thừa vùng hỏng, không bao giờ thiếu). Nhanh hơn trộn từng trường ~10 lần.
    /// </summary>
    public static ulong Hash(Scene scene, in Primitive p, float shapeUnshift = 0)
    {
        ulong h = 14695981039346656037UL;
        var words = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, ulong>(
            System.Runtime.InteropServices.MemoryMarshal.AsBytes(System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpan(in p, 1))[..(System.Runtime.CompilerServices.Unsafe.SizeOf<Primitive>() & ~7)]);
        // 4 làn độc lập (chuỗi nhân phụ thuộc nối tiếp chậm gấp ~4 lần) rồi gộp
        ulong h1 = h ^ 0x1, h2 = h ^ 0x2, h3 = h ^ 0x3;
        int i = 0;
        for (; i + 4 <= words.Length; i += 4)
        {
            h = (h ^ words[i]) * 0x9E3779B97F4A7C15UL + (h >> 32);
            h1 = (h1 ^ words[i + 1]) * 0x9E3779B97F4A7C15UL + (h1 >> 32);
            h2 = (h2 ^ words[i + 2]) * 0x9E3779B97F4A7C15UL + (h2 >> 32);
            h3 = (h3 ^ words[i + 3]) * 0x9E3779B97F4A7C15UL + (h3 >> 32);
        }
        for (; i < words.Length; i++) h = (h ^ words[i]) * 0x9E3779B97F4A7C15UL + (h >> 32);
        h = ((h * 31 + h1) * 31 + h2) * 31 + h3;
        Mix(ref h, (uint)p.Gradient); // đuôi struct có thể nằm ngoài các từ 8 byte; cờ vùng loại trừ của bóng nằm ở đây
        if (p.ClipShape != 0) { var s = scene.Shape(p.ClipShape); Mix(ref h, 1u); Mix(ref h, shapeUnshift == 0 ? s.Rect : s.Rect.Offset(0, -shapeUnshift)); Mix(ref h, s.Corners); Mix(ref h, s.Transform); }
        if (p.Kind == PrimitiveKind.Quad && p.Gradient != 0)
        {
            var g = scene.Gradient(p.Gradient);
            Mix(ref h, (uint)g.Kind); Mix(ref h, g.DirX); Mix(ref h, g.DirY); Mix(ref h, (g.Corner ? 1u : 0) + (g.Oklab ? 2u : 0)); Mix(ref h, (uint)g.Count);
            Mix(ref h, g.C0); Mix(ref h, g.C1); Mix(ref h, g.C2); Mix(ref h, g.P0); Mix(ref h, g.P1); Mix(ref h, g.P2);
        }
        return h;
    }

    /// <summary>Như <see cref="Visual"/> nhưng chưa cắt theo Clip / vùng cắt hình: hộp bao của riêng primitive (độc lập với vùng cắt, dời được bằng phép tịnh tiến).</summary>
    public static Bounds Reach(in Primitive p)
    {
        float margin = p.Kind == PrimitiveKind.Shadow ? p.Blur * 1.5f + 3 : 2;
        return Expand(p.Bounds.Inflate(margin), p.Transform);
    }

    /// <summary>Hộp pixel mà primitive có thể chạm tới: Bounds + lề (khử răng cưa, blur của bóng, viền), mở theo biến đổi, cắt theo Clip và vùng cắt hình.</summary>
    public static Bounds Visual(Scene scene, in Primitive p)
    {
        float margin = p.Kind == PrimitiveKind.Shadow ? p.Blur * 1.5f + 3 : 2;
        var b = Expand(p.Bounds.Inflate(margin), p.Transform).Intersect(p.Clip);
        if (p.ClipShape != 0)
        {
            var s = scene.Shape(p.ClipShape);
            b = b.Intersect(Expand(s.Rect.Inflate(2), s.Transform));
        }
        return b;
    }

    /// <summary>Hộp bao thẳng trục của hình chữ nhật sau biến đổi quanh tâm.</summary>
    static Bounds Expand(Bounds b, Mat2 m)
    {
        if (!m.IsTransform) return b;
        float hw = b.Width / 2, hh = b.Height / 2, cx = b.X + hw, cy = b.Y + hh;
        float ex = MathF.Abs(m.M11) * hw + MathF.Abs(m.M12) * hh, ey = MathF.Abs(m.M21) * hw + MathF.Abs(m.M22) * hh;
        return new Bounds(cx - ex, cy - ey, 2 * ex, 2 * ey);
    }
}
