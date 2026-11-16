namespace GotX;

/// <summary>
/// Loại view nằm hẳn ngoài vùng cắt (cuộn ra khỏi khung) khỏi scene: không chép / không giữ primitive của chúng, nên khung cuộn dài hàng nghìn view
/// chỉ tốn công theo số view đang nhìn thấy. Hitbox / hover / thứ tự view vẫn giữ nguyên (focus, ScrollIntoView, hover không đổi);
/// view bị loại đánh dấu <c>Culled</c> và được dựng lại (không phát lại) khi vào lại tầm nhìn. Kết quả pixel không đổi vì primitive bị loại
/// đều nằm ngoài vùng cắt của nó. Tắt bằng <c>GOTX_CULL=0</c> hoặc <see cref="CullViews"/>.
/// </summary>
public sealed partial class Window
{
    public static bool CullDefault { get; set; } = Environment.GetEnvironmentVariable("GOTX_CULL") != "0";
    public bool CullViews { get; set; } = CullDefault;
    /// <summary>View ít primitive hơn ngưỡng này thì chép còn rẻ hơn dựng lại khi vào tầm nhìn.</summary>
    const int CullMinPrims = 48;
    int _culledTotal;
    Bounds _replayExtent; bool _replayExtentKnown; // hộp bao (trước khi dời) của view sắp phát lại, nếu vừa tính

    /// <summary>Chỉ tính hộp bao khi có thể loại view: bật, đủ primitive, vùng cắt hữu hạn (view dưới vùng cắt vô hạn không bao giờ nằm ngoài).</summary>
    bool CullCandidate(int prims, Bounds clip) => CullViews && prims >= CullMinPrims && clip.Width < 1e8f;

    /// <summary>Hộp bao chưa cắt (px thiết bị) của mọi primitive trong [start, end).</summary>
    Bounds ViewExtent(int start, int end) => ViewExtent(_scene, start, end);

    static Bounds ViewExtent(Scene scene, int start, int end)
    {
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        var prims = scene.Primitives;
        for (int i = start; i < end; i++)
        {
            var r = SceneDamageMath.Reach(prims[i]);
            if (r.X < x0) x0 = r.X;
            if (r.Y < y0) y0 = r.Y;
            if (r.Right > x1) x1 = r.Right;
            if (r.Bottom > y1) y1 = r.Bottom;
        }
        return x1 < x0 ? default : new Bounds(x0, y0, x1 - x0, y1 - y0);
    }

    bool Offscreen(in Bounds extentDevice, Bounds clipLogical) => extentDevice.Intersect(clipLogical.Scale(_scale)).IsEmpty;

    /// <summary>View bao ngoài sắp được phát lại cả khối nhưng có view con đã bị loại mà nay (sau khi dời) lọt vào vùng cắt: phải dựng lại để vẽ nó.</summary>
    bool CulledChildVisible(in ViewCache prev, float ddx, float ddy, Bounds clip)
    {
        if (!prev.HasCulled) return false;
        for (int k = prev.OrderStart + 1; k < prev.OrderEnd; k++)
        {
            var c = _prevCache[_prevViewOrder[k]];
            if (c.Culled && !Offscreen(c.Extent.Offset(ddx, ddy), clip)) return true;
        }
        return false;
    }

    /// <summary>Bỏ primitive / vùng cắt hình / gradient của view vừa vẽ xong (và các view con của nó): đoạn rỗng, đánh dấu Culled.</summary>
    void DiscardView(int primStart, int shapeStart, int gradStart, int orderStart)
    {
        for (int k = orderStart + 1; k < _viewOrder.Count; k++)
        {
            ref var c = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrNullRef(_cache, _viewOrder[k]);
            if (System.Runtime.CompilerServices.Unsafe.IsNullRef(ref c)) continue;
            if (!c.ExtentKnown) { c.Extent = c.PrimEnd > c.PrimStart ? ViewExtent(_scene, c.PrimStart, c.PrimEnd) : default; c.ExtentKnown = true; }
            c.PrimStart = c.PrimEnd = primStart;
            c.ShapeStart = c.ShapeEnd = shapeStart;
            c.GradStart = c.GradEnd = gradStart;
            if (c.OuterShape > shapeStart) c.OuterShape = -1; // vùng cắt hình bao ngoài nằm trong đoạn đã bỏ (không còn biết): không vẽ lại riêng view này được (Window.Partial); còn lại là vùng bao của chính view bị loại, vẫn hợp lệ
            c.Culled = true;
        }
        _scene.TruncateTo(primStart, shapeStart, gradStart);
        _culledTotal++;
    }
}
