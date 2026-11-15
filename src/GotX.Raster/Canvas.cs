namespace GotX.Raster;

/// <summary>Bộ đệm pixel 32-bit premultiplied, bố cục bộ nhớ B,G,R,A (đúng định dạng DIB của Windows).</summary>
public sealed class Canvas
{
    public uint[] Pixels { get; private set; } = Array.Empty<uint>();
    public int Width { get; private set; }
    public int Height { get; private set; }

    public void Resize(int w, int h)
    {
        if (w == Width && h == Height) return;
        Width = w; Height = h;
        if (Pixels.Length < w * h) Pixels = new uint[w * h];
    }

    public void Clear(Rgba c) => Pixels.AsSpan(0, Width * Height).Fill(Pack(c.R * c.A, c.G * c.A, c.B * c.A, c.A));

    internal static uint Pack(float r, float g, float b, float a) =>
        ((uint)(Math.Clamp(a, 0, 1) * 255 + 0.5f) << 24) | ((uint)(Math.Clamp(r, 0, 1) * 255 + 0.5f) << 16) |
        ((uint)(Math.Clamp(g, 0, 1) * 255 + 0.5f) << 8) | (uint)(Math.Clamp(b, 0, 1) * 255 + 0.5f);
}
