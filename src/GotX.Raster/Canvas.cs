namespace GotX.Raster;

/// <summary>Bộ đệm pixel 32-bit premultiplied, bố cục bộ nhớ B,G,R,A (đúng định dạng DIB của Windows).</summary>
public sealed class Canvas
{
    // Kéo giãn cửa sổ đổi kích thước mỗi frame: cấp đúng cỡ sẽ cấp lại cả mảng LOH (hàng chục MB, gen2) gần như mỗi frame. Nên cấp dư 25 %,
    // và chỉ thu nhỏ khi bộ đệm lớn gấp 4 lần nhu cầu suốt ShrinkAfter lần Resize liên tiếp (thu về nhu cầu + 25 %).
    const int ShrinkAfter = 120;
    int _underused;

    public uint[] Pixels { get; private set; } = Array.Empty<uint>();
    public int Width { get; private set; }
    public int Height { get; private set; }

    public void Resize(int w, int h)
    {
        long need = (long)w * h;
        if (need > Pixels.Length) { Pixels = new uint[need + need / 4]; _underused = 0; }
        else if (need * 4 < Pixels.Length && Pixels.Length > 1 << 16)
        {
            if (++_underused >= ShrinkAfter) { Pixels = new uint[need + need / 4]; _underused = 0; }
        }
        else _underused = 0;
        Width = w; Height = h;
    }

    public void Clear(Rgba c) => Pixels.AsSpan(0, Width * Height).Fill(Pack(c.R * c.A, c.G * c.A, c.B * c.A, c.A));

    internal static uint Pack(float r, float g, float b, float a) =>
        ((uint)(Math.Clamp(a, 0, 1) * 255 + 0.5f) << 24) | ((uint)(Math.Clamp(r, 0, 1) * 255 + 0.5f) << 16) |
        ((uint)(Math.Clamp(g, 0, 1) * 255 + 0.5f) << 8) | (uint)(Math.Clamp(b, 0, 1) * 255 + 0.5f);
}
