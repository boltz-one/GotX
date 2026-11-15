using GotX.Raster;

namespace GotX.Test;

/// <summary>Một cụm pixel khác nhau: hộp bao (pixel thiết bị) và số pixel khác trong đó.</summary>
public readonly record struct DiffRegion(Bounds Area, int Pixels);

public sealed class DiffResult
{
    public int DiffPixels { get; init; }
    /// <summary>Chênh lệch lớn nhất trên một kênh (0..255).</summary>
    public int MaxDelta { get; init; }
    /// <summary>Các cụm khác nhau, lớn trước.</summary>
    public required List<DiffRegion> Regions { get; init; }
    /// <summary>Ảnh diff: ảnh hiện tại làm mờ, pixel khác tô đỏ tươi.</summary>
    public required Canvas Image { get; init; }
}

/// <summary>So hai ảnh cùng kích thước theo từng kênh và gom pixel khác thành vùng.</summary>
public static class ImageDiff
{
    // Pixel khác được gom theo ô lưới 8 px; các ô kề nhau (kể cả chéo) thuộc cùng một vùng. Nhờ vậy một dòng chữ đổi
    // thành MỘT vùng chứ không phải hàng chục cụm rời theo từng nét chữ.
    const int Cell = 8;

    /// <param name="actual">Ảnh hiện tại.</param>
    /// <param name="expectedRgba">Ảnh chuẩn dạng R,G,B,A 8 bit (đầu ra của <see cref="PngDecoder"/>), cùng kích thước.</param>
    /// <param name="tolerance">Chênh lệch mỗi kênh không quá mức này thì coi là giống.</param>
    public static DiffResult Compare(Canvas actual, byte[] expectedRgba, int tolerance)
    {
        int w = actual.Width, h = actual.Height;
        var mine = Pixels.ToRgba(actual);
        var image = new Canvas();
        image.Resize(w, h);
        int cw = (w + Cell - 1) / Cell, ch = (h + Cell - 1) / Cell;
        var cells = new int[cw * ch];
        int total = 0, maxDelta = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = (y * w + x) * 4;
                int d = Math.Max(Math.Max(Math.Abs(mine[i] - expectedRgba[i]), Math.Abs(mine[i + 1] - expectedRgba[i + 1])),
                    Math.Max(Math.Abs(mine[i + 2] - expectedRgba[i + 2]), Math.Abs(mine[i + 3] - expectedRgba[i + 3])));
                if (d > tolerance)
                {
                    total++;
                    maxDelta = Math.Max(maxDelta, d);
                    cells[y / Cell * cw + x / Cell]++;
                    image.Pixels[y * w + x] = 0xFFFF00C8;
                }
                else
                {
                    // Làm mờ ảnh gốc về phía trắng để pixel khác nổi bật mà vẫn nhận ra bố cục.
                    int gray = (mine[i] * 54 + mine[i + 1] * 183 + mine[i + 2] * 19) >> 8;
                    byte v = (byte)(170 + gray / 3);
                    image.Pixels[y * w + x] = Pixels.Pack(v, v, v);
                }
            }
        return new DiffResult { DiffPixels = total, MaxDelta = maxDelta, Regions = Cluster(cells, cw, ch, mine, expectedRgba, w, h, tolerance), Image = image };
    }

    static List<DiffRegion> Cluster(int[] cells, int cw, int ch, byte[] a, byte[] b, int w, int h, int tolerance)
    {
        var regions = new List<DiffRegion>();
        var visited = new bool[cells.Length];
        var stack = new Stack<int>();
        for (int start = 0; start < cells.Length; start++)
        {
            if (cells[start] == 0 || visited[start]) continue;
            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1, pixels = 0;
            visited[start] = true;
            stack.Push(start);
            while (stack.Count > 0)
            {
                int c = stack.Pop();
                int cx = c % cw, cy = c / cw;
                pixels += cells[c];
                Tighten(cx, cy, a, b, w, h, tolerance, ref x0, ref y0, ref x1, ref y1);
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = cx + dx, ny = cy + dy;
                        if (nx < 0 || ny < 0 || nx >= cw || ny >= ch) continue;
                        int n = ny * cw + nx;
                        if (cells[n] == 0 || visited[n]) continue;
                        visited[n] = true;
                        stack.Push(n);
                    }
            }
            regions.Add(new DiffRegion(new Bounds(x0, y0, x1 - x0 + 1, y1 - y0 + 1), pixels));
        }
        regions.Sort(static (p, q) => q.Pixels.CompareTo(p.Pixels));
        return regions;
    }

    /// <summary>Mở rộng hộp bao của vùng theo đúng các pixel khác trong một ô lưới (hộp bao sát, không theo biên ô).</summary>
    static void Tighten(int cx, int cy, byte[] a, byte[] b, int w, int h, int tolerance, ref int x0, ref int y0, ref int x1, ref int y1)
    {
        for (int y = cy * Cell; y < Math.Min(h, (cy + 1) * Cell); y++)
            for (int x = cx * Cell; x < Math.Min(w, (cx + 1) * Cell); x++)
            {
                int i = (y * w + x) * 4;
                if (Math.Abs(a[i] - b[i]) <= tolerance && Math.Abs(a[i + 1] - b[i + 1]) <= tolerance
                    && Math.Abs(a[i + 2] - b[i + 2]) <= tolerance && Math.Abs(a[i + 3] - b[i + 3]) <= tolerance) continue;
                x0 = Math.Min(x0, x); y0 = Math.Min(y0, y); x1 = Math.Max(x1, x); y1 = Math.Max(y1, y);
            }
    }
}
