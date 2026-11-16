using GotX.Text;

namespace GotX;

public enum PrimitiveKind : byte { Quad, Shadow, Glyph, Image }

/// <summary>
/// Một lệnh vẽ, toạ độ device pixel. Renderer (phần mềm hoặc GPU) chỉ cần hiểu 4 loại này:
/// hình chữ nhật bo góc, bóng đổ, mask một màu (glyph, hình vector) và ảnh.
/// </summary>
public struct Primitive
{
    public PrimitiveKind Kind;
    public Bounds Bounds;
    public Bounds Clip;
    public Rgba Color;        // nền (Quad), màu bóng (Shadow), màu chữ (Glyph)
    public Edges BorderWidths; // độ rộng viền từng cạnh (device px)
    public Rgba BorderTop, BorderRight, BorderBottom, BorderLeft;
    public Corners Corners;
    public float Blur;        // Shadow: bán kính blur kiểu CSS
    public int Glyph;         // Glyph: index trong GlyphAtlas; Image: ImageSource.Id
    /// <summary>Image: vùng nguồn trong ảnh (pixel ảnh) được kéo giãn vào Bounds. Corners bo góc ảnh, Color.A là độ mờ.</summary>
    public Bounds Source;
    public BorderStyle BorderStyle;
    /// <summary>
    /// Biến đổi tuyến tính quanh tâm của Bounds (xoay, skew, phóng lệch trục), định thức ≈ 1.
    /// Bounds là hình chữ nhật trước biến đổi; renderer đưa mỗi pixel về hệ toạ độ đó rồi tính SDF.
    /// default = không biến đổi.
    /// </summary>
    public Mat2 Transform;
    /// <summary>Vùng cắt theo hình (bo góc / xoay): 0 = không có, n = Scene.ClipShapes[n - 1].</summary>
    public int ClipShape;
    /// <summary>Quad: gradient phủ lên màu nền (background-image trên background-color): 0 = không, n = Scene.Gradients[n - 1].</summary>
    public int Gradient;
    // Shadow dùng lại các trường của quad cho vùng LOẠI TRỪ (CSS: box-shadow ngoài chỉ vẽ ngoài hộp viền của phần tử): Gradient = 1 khi có,
    // Source = hộp viền (device px, trước biến đổi, cùng Transform với bóng), BorderWidths = bán kính góc (Top=TL, Right=TR, Bottom=BR, Left=BL).
    public readonly bool HasExclusion => Kind == PrimitiveKind.Shadow && Gradient == 1;
    public readonly bool HasBorder =>
        BorderWidths.Top > 0 || BorderWidths.Right > 0 || BorderWidths.Bottom > 0 || BorderWidths.Left > 0;
}

/// <summary>
/// Vùng cắt có hình dạng: hình chữ nhật bo góc, có thể bị xoay/skew (overflow-hidden trên phần tử
/// bo góc hoặc bị biến đổi). Cắt có anti-alias. Clip thẳng trục của Primitive vẫn dùng để loại nhanh.
/// </summary>
public readonly record struct ClipShape(Bounds Rect, Corners Corners, Mat2 Transform);

/// <summary>Danh sách primitive theo thứ tự vẽ. Clear() giữ nguyên dung lượng mảng.</summary>
public sealed class Scene
{
    Primitive[] _items = new Primitive[1024];
    int _count;

    public Scene(GlyphAtlas atlas) { Atlas = atlas; }

    public GlyphAtlas Atlas { get; }
    /// <summary>Vùng đổi do Window tích luỹ (null nếu scene dựng tay): renderer dùng thay cho so hash cả scene.</summary>
    public FrameDamage? Damage { get; set; }
    public Rgba ClearColor { get; set; } = Rgba.White;
    public int Count => _count;
    public ReadOnlySpan<Primitive> Primitives => new(_items, 0, _count);

    ClipShape[] _shapes = new ClipShape[16];
    int _shapeCount;

    public int ShapeCount => _shapeCount;
    public ReadOnlySpan<ClipShape> ClipShapes => new(_shapes, 0, _shapeCount);
    public ClipShape Shape(int oneBased) => _shapes[oneBased - 1];

    /// <summary>Thêm vùng cắt; trả về chỉ số 1-based để gán vào Primitive.ClipShape.</summary>
    public int AddClipShape(in ClipShape s)
    {
        if (_shapeCount == _shapes.Length) Array.Resize(ref _shapes, _shapeCount * 2);
        _shapes[_shapeCount++] = s;
        return _shapeCount;
    }

    GradientFill[] _gradients = new GradientFill[8];
    int _gradientCount;

    public int GradientCount => _gradientCount;
    public ReadOnlySpan<GradientFill> Gradients => new(_gradients, 0, _gradientCount);
    public GradientFill Gradient(int oneBased) => _gradients[oneBased - 1];

    /// <summary>Thêm gradient; trả về chỉ số 1-based để gán vào Primitive.Gradient.</summary>
    public int AddGradient(in GradientFill g)
    {
        if (_gradientCount == _gradients.Length) Array.Resize(ref _gradients, _gradientCount * 2);
        _gradients[_gradientCount++] = g;
        return _gradientCount;
    }

    /// <summary>Chép các gradient [start, end) từ scene khác (phát lại view). Gradient tính theo Bounds nên không cần dời.</summary>
    public void AddGradients(Scene src, int start, int end)
    {
        for (int i = start; i < end; i++) AddGradient(src._gradients[i]);
    }

    internal ref Primitive Item(int i) => ref _items[i];

    public void Clear() { _count = 0; _shapeCount = 0; _gradientCount = 0; }

    /// <summary>Đảm bảo chứa được n primitive bằng MỘT lần cấp phát (nở dần gấp đôi tốn tổng ~2 lần và giật frame đầu tiên dùng scene này).</summary>
    public void Reserve(int n)
    {
        if (n > _items.Length) Array.Resize(ref _items, n + n / 8);
    }

    /// <summary>Bỏ primitive nhưng giữ vùng cắt / gradient (công cụ đo từng primitive một).</summary>
    public void ClearPrimitives() => _count = 0;

    /// <summary>Chép đoạn [start, end) từ scene khác (phát lại view đã cache).</summary>
    public void AddRange(Scene src, int start, int end)
    {
        int n = end - start;
        if (n <= 0) return;
        if (_count + n > _items.Length) Array.Resize(ref _items, Math.Max(_items.Length * 2, _count + n));
        Array.Copy(src._items, start, _items, _count, n);
        _count += n;
    }

    /// <summary>Tịnh tiến các primitive [start, end) (device px) – dùng khi phát lại view bị dời chỗ.</summary>
    public void TranslateRange(int start, int end, float dx, float dy, Bounds outerClip)
    {
        for (int i = start; i < end; i++)
        {
            ref var p = ref _items[i];
            p.Bounds = p.Bounds.Offset(dx, dy);
            if (p.HasExclusion) p.Source = p.Source.Offset(dx, dy);
            if (p.Clip != outerClip) p.Clip = p.Clip.Offset(dx, dy).Intersect(outerClip);
        }
    }

    /// <summary>Chép các vùng cắt [start, end) từ scene khác (1 lần mỗi view phát lại), dời nếu cần.</summary>
    public void AddShapes(Scene src, int start, int end, float dx, float dy)
    {
        for (int i = start; i < end; i++)
        {
            var c = src._shapes[i];
            AddClipShape(dx == 0 && dy == 0 ? c : c with { Rect = c.Rect.Offset(dx, dy) });
        }
    }

    /// <summary>Thay đoạn [start, end) bằng [srcStart, srcEnd) của scene khác tại chỗ (khung một phần): chỉ dịch phần đuôi khi độ dài đổi.</summary>
    public void ReplaceRange(int start, int end, Scene src, int srcStart, int srcEnd)
    {
        int n = srcEnd - srcStart, delta = n - (end - start);
        if (delta != 0)
        {
            if (_count + delta > _items.Length) Array.Resize(ref _items, Math.Max(_items.Length * 2, _count + delta));
            Array.Copy(_items, end, _items, end + delta, _count - end);
            _count += delta;
        }
        if (n > 0) Array.Copy(src._items, srcStart, _items, start, n);
    }

    /// <summary>Ghi đè [start, start + count) vùng cắt bằng [srcStart, ...) của scene khác (cùng số lượng, chỉ số không đổi).</summary>
    public void OverwriteShapes(int start, Scene src, int srcStart, int count) => Array.Copy(src._shapes, srcStart, _shapes, start, count);

    public void OverwriteGradients(int start, Scene src, int srcStart, int count) => Array.Copy(src._gradients, srcStart, _gradients, start, count);

    public void Add(in Primitive p)
    {
        if (_count == _items.Length) Array.Resize(ref _items, _count * 2);
        _items[_count++] = p;
    }
}
