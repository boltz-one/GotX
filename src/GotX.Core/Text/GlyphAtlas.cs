namespace GotX.Text;

/// <summary>Cache glyph đã raster. Chỉ cấp phát khi gặp glyph/cỡ chữ mới.</summary>
public sealed class GlyphAtlas
{
    readonly Dictionary<(int font, ushort glyph, int sizeQ), int> _index = new();
    readonly List<GlyphMask> _masks = new();
    readonly GlyphRasterizer _raster = new();

    long _bytes;

    public GlyphMask this[int i] => _masks[i];

    /// <summary>Tăng mỗi lần <see cref="Reset"/>: mọi chỉ số glyph cấp trước đó hết hiệu lực.</summary>
    public int Generation { get; private set; }

    /// <summary>Ngân sách bộ nhớ cho bitmap glyph. Vượt quá thì Window xoá atlas ở đầu frame kế tiếp.</summary>
    public long MaxBytes { get; set; } = 32L << 20;

    public long UsedBytes => _bytes;
    public bool OverBudget => _bytes > MaxBytes;

    /// <summary>
    /// Xoá mọi glyph đã raster (ví dụ sau khi animation scale sinh ra rất nhiều cỡ chữ trung gian).
    /// Chỉ gọi giữa hai frame: Scene đã dựng trước đó còn giữ chỉ số cũ nên không được vẽ lại.
    /// </summary>
    public void Reset()
    {
        _index.Clear();
        _paths.Clear();
        _masks.Clear();
        _bytes = 0;
        Generation++;
    }

    public int Get(Font font, ushort glyph, float sizePx)
    {
        int sizeQ = (int)MathF.Round(sizePx * 4); // lượng tử 1/4 px
        var key = (font.Id, glyph, sizeQ);
        if (_index.TryGetValue(key, out int idx)) return idx;
        idx = _masks.Count;
        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        var mask = Rasterize(font, glyph, sizeQ / 4f);
        PerfCounters.GlyphRaster++;
        PerfCounters.GlyphRasterTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
        _masks.Add(mask);
        _bytes += mask.Alpha.Length + 64; // 64 ≈ chi phí object + mục từ điển
        _index[key] = idx;
        return idx;
    }

    readonly Dictionary<(int path, int w, int h), int> _paths = new();

    /// <summary>Mask của một <see cref="VectorPath"/> ở kích thước pixel cho trước (nằm chung danh sách với glyph).</summary>
    public int GetPath(VectorPath path, int width, int height)
    {
        var key = (path.Id, width, height);
        if (_paths.TryGetValue(key, out int idx)) return idx;
        idx = _masks.Count;
        var mask = PathRasterizer.Rasterize(path, width, height, _raster);
        _masks.Add(mask);
        _bytes += mask.Alpha.Length + 64;
        _paths[key] = idx;
        return idx;
    }

    GlyphMask Rasterize(Font font, ushort glyph, float sizePx)
    {
        var mask = new GlyphMask();
        if (!font.TryGetBox(glyph, out var xMin, out var yMin, out var xMax, out var yMax)) return mask;
        float k = sizePx / font.UnitsPerEm;
        int left = (int)MathF.Floor(xMin * k) - 1;
        int top = (int)MathF.Floor(-yMax * k) - 1;
        int w = (int)MathF.Ceiling(xMax * k) - left + 2;
        int h = (int)MathF.Ceiling(-yMin * k) - top + 2;
        if (w <= 0 || h <= 0 || w > 2048 || h > 2048) return mask;

        _raster.Reset(w, h);
        // font units (y lên) → bitmap (y xuống), dịch để nằm trong [0,w)×[0,h)
        font.Outline(glyph, _raster, new Affine(k, 0, 0, -k, -left, -top));
        mask.Width = w; mask.Height = h; mask.Left = left; mask.Top = top;
        mask.Alpha = _raster.ToAlpha();
        return mask;
    }
}
