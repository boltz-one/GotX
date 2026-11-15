namespace GotX;

/// <summary>
/// Ảnh bitmap để vẽ bằng <c>window.Image(...)</c>. Pixel lưu dạng B,G,R,A đã nhân alpha (premultiplied) –
/// đúng định dạng canvas CPU và texture GPU. Tạo một lần rồi giữ lại: renderer GPU giữ một texture cho mỗi ảnh
/// chừng nào ảnh còn được vẽ.
/// </summary>
public sealed class ImageSource
{
    // Bảng tra Id → ảnh bằng tham chiếu yếu: Primitive chỉ mang Id, ảnh bị bỏ thì tự được thu hồi.
    // Id tăng dần và không cấp lại (renderer GPU nhớ texture theo Id); mục của ảnh đã chết được dọn khi bảng lớn gấp đôi.
    static readonly object s_gate = new();
    static readonly Dictionary<int, WeakReference<ImageSource>> s_all = new();
    static int s_nextId, s_sweepAt = 64;

    ImageSource(int width, int height, byte[] bgra)
    {
        Width = width; Height = height; Pixels = bgra;
        lock (s_gate)
        {
            Id = ++s_nextId;
            s_all[Id] = new WeakReference<ImageSource>(this);
            if (s_all.Count < s_sweepAt) return;
            foreach (var kv in s_all) if (!kv.Value.TryGetTarget(out _)) s_all.Remove(kv.Key);
            s_sweepAt = Math.Max(64, s_all.Count * 2);
        }
    }
    /// <summary>Số định danh (từ 1) mà Primitive dùng để trỏ tới ảnh.</summary>
    public int Id { get; }
    public int Width { get; }
    public int Height { get; }
    /// <summary>Width × Height × 4 byte: B, G, R, A premultiplied, hàng trên cùng trước.</summary>
    public byte[] Pixels { get; }

    /// <summary>Ảnh theo Id, hoặc null nếu ảnh đã bị thu hồi (renderer bỏ qua primitive đó).</summary>
    public static ImageSource? Find(int id)
    {
        lock (s_gate) return s_all.TryGetValue(id, out var weak) && weak.TryGetTarget(out var image) ? image : null;
    }
    /// <summary>Từ pixel R,G,B,A không premultiply (4 byte mỗi pixel, hàng trên cùng trước).</summary>
    public static ImageSource FromRgba(int width, int height, ReadOnlySpan<byte> rgba)
    {
        if (width <= 0 || height <= 0 || rgba.Length < width * height * 4)
            throw new ArgumentException("Kích thước ảnh không khớp dữ liệu pixel.");
        var bgra = new byte[width * height * 4];
        for (int i = 0; i < bgra.Length; i += 4)
        {
            int a = rgba[i + 3];
            bgra[i] = (byte)((rgba[i + 2] * a + 127) / 255);
            bgra[i + 1] = (byte)((rgba[i + 1] * a + 127) / 255);
            bgra[i + 2] = (byte)((rgba[i] * a + 127) / 255);
            bgra[i + 3] = (byte)a;
        }
        return new ImageSource(width, height, bgra);
    }

    /// <summary>Giải mã PNG (8/16 bit, mọi kiểu màu, không interlace). Ném <see cref="NotSupportedException"/> nếu không đọc được.</summary>
    public static ImageSource FromPng(ReadOnlySpan<byte> png)
    {
        var rgba = PngDecoder.Decode(png, out int width, out int height);
        return FromRgba(width, height, rgba);
    }

    public static ImageSource FromPngFile(string path) => FromPng(File.ReadAllBytes(path));
}
