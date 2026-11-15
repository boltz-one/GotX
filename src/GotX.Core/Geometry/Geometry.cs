namespace GotX;

/// <summary>Điểm 2D (logical pixel trong core, device pixel trong Scene).</summary>
public readonly record struct Point(float X, float Y);

public readonly record struct Size(float Width, float Height);

/// <summary>Hình chữ nhật trục Y hướng xuống.</summary>
public readonly record struct Bounds(float X, float Y, float Width, float Height)
{
    public float Right => X + Width;
    public float Bottom => Y + Height;
    public bool IsEmpty => Width <= 0 || Height <= 0;

    public static readonly Bounds Infinite = new(-1e9f, -1e9f, 2e9f, 2e9f);

    public bool Contains(Point p) => p.X >= X && p.X < Right && p.Y >= Y && p.Y < Bottom;

    public Bounds Intersect(in Bounds o)
    {
        float x0 = MathF.Max(X, o.X), y0 = MathF.Max(Y, o.Y);
        float x1 = MathF.Min(Right, o.Right), y1 = MathF.Min(Bottom, o.Bottom);
        return new Bounds(x0, y0, MathF.Max(0, x1 - x0), MathF.Max(0, y1 - y0));
    }

    public Bounds Inflate(float d) => new(X - d, Y - d, Width + 2 * d, Height + 2 * d);
    public Bounds Offset(float dx, float dy) => new(X + dx, Y + dy, Width, Height);
    public Bounds Scale(float s) => new(X * s, Y * s, Width * s, Height * s);
}

/// <summary>Bốn cạnh (padding, margin…) đã quy ra pixel.</summary>
public record struct Edges(float Top, float Right, float Bottom, float Left)
{
    public readonly float Horizontal => Left + Right;
    public readonly float Vertical => Top + Bottom;
}

/// <summary>Bán kính bốn góc, đơn vị logical pixel.</summary>
public record struct Corners(float TopLeft, float TopRight, float BottomRight, float BottomLeft)
{
    public static Corners All(float r) => new(r, r, r, r);
    public readonly Corners Scale(float s) => new(TopLeft * s, TopRight * s, BottomRight * s, BottomLeft * s);
}

/// <summary>Màu sRGB không premultiply, kênh 0..1.</summary>
public readonly record struct Rgba(float R, float G, float B, float A)
{
    public static Rgba FromHex(uint rgb, float alpha = 1f) =>
        new(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, alpha);

    public Rgba WithAlpha(float a) => this with { A = a };

    public static readonly Rgba Transparent = default;
    public static readonly Rgba White = new(1, 1, 1, 1);
    public static readonly Rgba Black = new(0, 0, 0, 1);
}
