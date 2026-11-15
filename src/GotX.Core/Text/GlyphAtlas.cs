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
        _faces.Clear();
        _index.Clear();
        _paths.Clear();
        _masks.Clear();
        _bytes = 0;
        Generation++;
    }

    readonly Dictionary<long, GlyphFace> _faces = new();

    /// <summary>
    /// Bảng glyph → chỉ số atlas cho một (font, cỡ chữ): một lượt vẽ chữ lấy mặt chữ một lần rồi tra bằng chỉ số mảng,
    /// không băm khoá ba trường cho từng glyph. Hết hiệu lực sau <see cref="Reset"/> (chỉ gọi giữa hai frame).
    /// </summary>
    public GlyphFace Face(Font font, float sizePx)
    {
        int sizeQ = (int)MathF.Round(sizePx * 4);
        long key = ((long)font.Id << 32) | (uint)sizeQ;
        if (_faces.TryGetValue(key, out var face)) return face;
        if (_faces.Count > 256) _faces.Clear();
        return _faces[key] = new GlyphFace(this, font, sizePx);
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

/// <summary>Bảng tra glyph → atlas của một (font, cỡ chữ), xem <see cref="GlyphAtlas.Face"/>.</summary>
public sealed class GlyphFace
{
    readonly GlyphAtlas _atlas;
    readonly Font _font;
    readonly float _size;
    int[] _index = new int[512];

    internal GlyphFace(GlyphAtlas atlas, Font font, float size)
    {
        _atlas = atlas; _font = font; _size = size;
        _index.AsSpan().Fill(-1);
    }

    /// <summary>Chỉ số atlas của glyph (raster nếu chưa có).</summary>
    public int Get(ushort glyph)
    {
        var index = _index;
        if (glyph < index.Length && index[glyph] >= 0) return index[glyph];
        return Slow(glyph);
    }

    int Slow(ushort glyph)
    {
        int i = _atlas.Get(_font, glyph, _size);
        if (glyph >= _index.Length)
        {
            int old = _index.Length;
            Array.Resize(ref _index, Math.Max(glyph + 1, old * 2));
            _index.AsSpan(old).Fill(-1);
        }
        _index[glyph] = i;
        return i;
    }
}
