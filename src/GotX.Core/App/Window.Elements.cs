using System.Runtime.InteropServices;
using GotX.Text;
using GotX.Widgets;

namespace GotX;

/// <summary>Tạo phần tử trong lúc Render và tra cứu kết quả của frame gần nhất (hitbox, hộp theo khoá, vị trí cuộn).</summary>
public sealed partial class Window
{
    public Div Div() => _tree.NewDiv();

    /// <summary>Nhúng một view con. View con được cache độc lập với view cha.</summary>
    public Div View(View view)
    {
        if (!view.IsAttached) throw new InvalidOperationException("View chưa được tạo qua app.New(...).");
        return _tree.NewView(view);
    }

    public Div InputArea(TextInput input) => _tree.NewInput(input);

    /// <summary>
    /// Hình vector (icon, đường biểu đồ) vẽ bằng màu chữ hiện tại. Kích thước mặc định bằng viewBox; đổi bằng
    /// class (<c>size-4</c>). Mặc định giữ tỉ lệ và căn giữa; <c>object-fill</c> kéo giãn theo hộp.
    /// </summary>
    public Div Path(VectorPath path)
    {
        var d = _tree.NewDiv();
        ref var n = ref _tree[d.Id];
        n.Vector = path;
        n.Style.Width = Length.Px(path.Width); n.Style.Height = Length.Px(path.Height);
        n.Style.FlexShrinkValue = 0;
        return d;
    }

    /// <summary>
    /// Ảnh bitmap. Kích thước mặc định bằng kích thước ảnh; <c>object-cover</c> / <c>object-contain</c> /
    /// <c>object-fill</c> chọn cách lấp hộp, bo góc của phần tử cắt luôn ảnh (<c>rounded-full</c>).
    /// </summary>
    public Div Image(ImageSource image)
    {
        var d = _tree.NewDiv();
        ref var n = ref _tree[d.Id];
        n.Image = image;
        n.Style.Width = Length.Px(image.Width); n.Style.Height = Length.Px(image.Height);
        n.Style.ObjectFit = ObjectFit.Cover;
        return d;
    }

    public int HitboxCount => _hitboxes.Count;
    public Bounds HitboxBounds(int i) => _hitboxes[i].Xf.ApplyAabb(_hitboxes[i].Local).Intersect(_hitboxes[i].Clip);
    public bool HitboxIsInput(int i) => _hitboxes[i].Input is not null;
    public bool HitboxIsClick(int i) => _hitboxes[i].Handler is not null && !_hitboxes[i].IsKey;
    public bool HitboxIsScroll(int i) => _hitboxes[i].ScrollKey != 0;

    /// <summary>Bounds layout (chưa tính cuộn/transform) của phần tử có <c>.Key(key)</c> trong frame gần nhất.</summary>
    public Bounds? FindBounds(string key)
    {
        if (!_tree.TryKeyId(key, out int k)) return null;
        for (int i = 0; i < _tree.Count; i++)
            if (_tree[i].Key == k) return _tree[i].Bounds;
        return null;
    }

    /// <summary>Vị trí cuộn hiện tại của vùng cuộn thứ i (theo thứ tự hitbox) – dùng cho test/công cụ.</summary>
    public Point ScrollOffset(int hitbox) =>
        _scrolls.TryGetValue(_hitboxes[hitbox].ScrollKey, out var st) ? new Point(st.X, st.Y) : default;
}
