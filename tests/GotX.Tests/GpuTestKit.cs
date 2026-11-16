using GotX;
using GotX.Platform.Windows;
using GotX.Raster;
using GotX.Text;

/// <summary>Dụng cụ chung của test GPU: renderer D3D11 offscreen (WARP), so ảnh CPU↔GPU, dựng Scene tay.</summary>
static class GpuKit
{
    /// <summary>Tạo renderer WARP offscreen; null nếu máy không tạo nổi thiết bị D3D11 (test phải ghi chú rõ rồi bỏ qua).</summary>
    /// <summary>GOTX_GPU_TEST_HW=1 chạy trên adapter phần cứng thật thay vì WARP (parity có dung sai kênh ≤ 3).</summary>
    public static D3D11Renderer? TryRenderer(int w, int h) =>
        D3D11Renderer.TryCreateOffscreen(w, h, warp: Environment.GetEnvironmentVariable("GOTX_GPU_TEST_HW") != "1");

    public static Canvas Gpu(D3D11Renderer r, Scene s, int w, int h)
    {
        r.Render(s, w, h);
        var c = new Canvas();
        r.ReadPixels(c);
        return c;
    }

    public static Canvas Cpu(Scene s, int w, int h)
    {
        var c = new Canvas();
        c.Resize(w, h);
        SoftwareRenderer.Render(s, c);
        return c;
    }

    public readonly record struct Diff(double Within, int MaxDelta, int Tx, int Ty, int TileBad, int TileMax, int Width, int Height)
    {
        public override string ToString() =>
            $"{Within * 100:F2}% px within tol, max Δ{MaxDelta}; worst tile ({Tx},{Ty}) {TileBad} bad px (max Δ{TileMax})";
    }

    /// <summary>So từng kênh BGRA premultiplied; vùng xấu nhất = ô 32×32 có nhiều pixel lệch quá tol nhất.</summary>
    public static Diff Compare(Canvas a, Canvas b, int tol)
    {
        int w = a.Width, h = a.Height, bad = 0, max = 0, tiles = (w + 31) / 32;
        var tileBad = new int[tiles * ((h + 31) / 32)];
        var tileMax = new int[tileBad.Length];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                uint p = a.Pixels[y * w + x], q = b.Pixels[y * w + x];
                int d = 0;
                for (int s = 0; s < 32; s += 8) d = Math.Max(d, Math.Abs((int)((p >> s) & 0xFF) - (int)((q >> s) & 0xFF)));
                max = Math.Max(max, d);
                int ti = y / 32 * tiles + x / 32;
                tileMax[ti] = Math.Max(tileMax[ti], d);
                if (d > tol) { bad++; tileBad[ti]++; }
            }
        int worst = Array.IndexOf(tileBad, tileBad.Max());
        return new Diff(1.0 - bad / (double)(w * h), max, worst % tiles * 32, worst / tiles * 32, tileBad[worst], tileMax[worst], w, h);
    }

    public static Primitive Quad(Bounds b, Bounds view, Rgba color, float radius = 0) =>
        new() { Kind = PrimitiveKind.Quad, Bounds = b, Clip = view, Color = color, Corners = Corners.All(radius) };

    public static Primitive Shadow(Bounds b, Bounds view, Rgba color, float radius, float blur) =>
        new() { Kind = PrimitiveKind.Shadow, Bounds = b, Clip = view, Color = color, Corners = Corners.All(radius), Blur = blur };

    public static Scene NewScene(Rgba clear) => new(new GlyphAtlas()) { ClearColor = clear };
}
