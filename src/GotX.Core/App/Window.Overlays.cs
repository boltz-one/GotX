using System.Runtime.InteropServices;

namespace GotX;

/// <summary>
/// Lớp overlay của cửa sổ: các View mở bằng <see cref="OpenOverlay"/> được dựng, layout và vẽ sau view gốc,
/// mỗi overlay trong một lớp phủ kín viewport. Thứ tự mở = thứ tự chồng; overlay trên cùng nhận Escape / bấm ngoài.
/// </summary>
public sealed partial class Window
{
    readonly List<Overlay> _overlays = new();
    int _hoverFloor;   // vùng hover có chỉ số nhỏ hơn nằm dưới một overlay modal → coi như chuột không ở trên
    bool _blockHover;  // đang vẽ phần nằm dưới một overlay modal

    public IReadOnlyList<Overlay> Overlays => _overlays;

    /// <summary>Mở <paramref name="content"/> thành một lớp trên cùng. Trả về handle để đóng và nghe sự kiện Closed.</summary>
    public Overlay OpenOverlay(View content, OverlayOptions? options = null)
    {
        if (!content.IsAttached) throw new InvalidOperationException("View chưa được tạo qua app.New(...).");
        var overlay = new Overlay(this, content, options ?? new OverlayOptions());
        // Nhớ focus lúc mở để trả lại khi đóng (nút mở menu, ô nhập phía sau hộp thoại).
        overlay.PreviousFocus = Focused;
        overlay.PreviousFocusKey = _focusKey;
        overlay.PreviousFocusVisible = _focusVisible;
        if (overlay.Options.Modal) Focus(null);
        if (Trace is not null) Emit(TraceKind.Overlay, "mở " + content.GetType().Name, viewId: content.Id.Value);
        _overlays.Add(overlay);
        _overlayStamp++;
        RequestFrame();
        return overlay;
    }

    public void CloseOverlay(Overlay overlay)
    {
        if (!_overlays.Remove(overlay)) return;
        _overlayStamp++;
        overlay.IsOpen = false;
        if (Trace is not null) Emit(TraceKind.Overlay, "đóng " + overlay.Content.GetType().Name, viewId: overlay.Content.Id.Value);
        // Trả focus về nơi nó ở trước khi mở, nếu overlay là modal hoặc focus đang nằm bên trong nó.
        int root = overlay.Content.Id.Value;
        // Quên dấu AutoFocus của nội dung vừa đóng: mở lại ngay trong cùng một sự kiện vẫn tự focus.
        foreach (var kv in _autoFocus) if (ViewWithin((int)(kv.Key >> 32), root)) _autoFocus.Remove(kv.Key);
        bool inside = (_focusKey != 0 && ViewWithin((int)(_focusKey >> 32), root)) || (Focused is { } f && ViewWithin(f.Id.Value, root));
        if (overlay.Options.Modal || inside)
        {
            if (overlay.PreviousFocus is { IsAttached: true } previous) Focus(previous);
            else { Focus(null); SetElementFocus(overlay.PreviousFocusKey, overlay.PreviousFocusVisible); }
        }
        RequestFrame();
        overlay.RaiseClosed();
    }

    /// <summary>Vị trí trên màn hình (logical px, đã tính cuộn/transform) của phần tử có <c>.Anchor(name)</c> ở frame gần nhất.</summary>
    public Bounds? AnchorBounds(string name)
    {
        // Gọi trong Render thì danh sách của frame này chưa dựng xong: tra frame trước.
        var span = CollectionsMarshal.AsSpan(_drawing ? _prevHitboxes : _hitboxes);
        for (int i = span.Length - 1; i >= 0; i--)
            if (span[i].Anchor == name) return span[i].Xf.ApplyAabb(span[i].Local);
        return null;
    }

    bool HasModalFrom(int index)
    {
        for (int i = index; i < _overlays.Count; i++)
            if (_overlays[i].Options.Modal) return true;
        return false;
    }

    /// <summary>Dựng và vẽ các overlay sau view gốc. Anchor được đọc từ hitbox của chính frame này nên luôn khớp nội dung.</summary>
    void PaintOverlays(Size viewport)
    {
        _hoverFloor = 0;
        var full = new Bounds(0, 0, viewport.Width, viewport.Height);
        for (int i = 0; i < _overlays.Count; i++)
        {
            var o = _overlays[i];
            var options = o.Options;
            var layer = _tree.NewDiv().Class(o.LayerClasses);
            var content = _tree.NewView(o.Content);
            layer.Child(content);

            if (options.Anchor is not null || options.AnchorRect is not null)
            {
                var anchor = (options.Anchor is { } name ? AnchorBounds(name) : null) ?? options.AnchorRect ?? o.LastAnchor;
                o.LastAnchor = anchor;
                ref var style = ref content.Style;
                style.Position = Position.Absolute;
                if (anchor is { } a)
                {
                    if (options.MatchAnchorWidth) style.MinWidth = Length.Px(a.Width);
                    var size = _layout.MeasureNode(content.Id, BaseTextStyle, viewport.Width, viewport.Height, RemSize);
                    var rect = Floating.Place(a, size, full, options.Placement, options.Offset, options.ViewportPadding, out var used);
                    o.ActualPlacement = used;
                    style.Inset.Left = Length.Px(rect.X);
                    style.Inset.Top = Length.Px(rect.Y);
                }
            }

            if (options.Modal)
            {
                _hitboxes.Add(new Hitbox { Local = full, Xf = Xf.Identity, Clip = Bounds.Infinite, Blocker = true, ViewId = -1 });
                _hoverFloor = _hovers.Count;
            }
            _blockHover = HasModalFrom(i + 1);
            _keyScope = _scrollScope = 0;
            long t1 = System.Diagnostics.Stopwatch.GetTimestamp();
            _layout.Run(layer.Id, viewport, BaseTextStyle, RemSize);
            long t2 = System.Diagnostics.Stopwatch.GetTimestamp();
            Paint(layer.Id, BaseTextStyle, Bounds.Infinite, Xf.Identity);
            _layoutTicks += t2 - t1;
            _paintTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t2;
            o.ContentBounds = _tree[content.Id].Bounds;
        }
        _blockHover = false;
    }

    /// <summary>
    /// Escape và bấm ra ngoài đóng overlay trên cùng *cho phép đóng kiểu đó*; lớp không cho đóng và không modal
    /// (ví dụ chồng toast) thì được bỏ qua để sự kiện tới lớp bên dưới. Trả về true nếu sự kiện đã được dùng hết
    /// (bấm ra ngoài một modal không lọt xuống phần tử bên dưới; ngoài một dropdown thì vẫn lọt).
    /// </summary>
    bool HandleOverlayInput(in InputEvent e)
    {
        if (e.Kind == InputKind.KeyDown && e.Key == Key.Escape)
        {
            for (int i = _overlays.Count - 1; i >= 0; i--)
            {
                var o = _overlays[i];
                if (o.Options.DismissOnEscape) { CloseOverlay(o); return true; }
                if (o.Options.Modal) return true; // modal không cho Escape: phím dừng ở đây, không đóng lớp dưới và không làm mất focus bên trong
            }
            return false;
        }
        if (e.Kind != InputKind.MouseDown) return false;
        for (int i = _overlays.Count - 1; i >= 0; i--)
        {
            var o = _overlays[i];
            if (o.ContentBounds.Contains(e.Position)) return false;
            // Bấm vào chính nút mở (anchor): để handler của nút tự đóng/mở, tránh đóng rồi mở lại ngay.
            if (o.Options.Anchor is not null && o.LastAnchor is { } anchor && anchor.Contains(e.Position)) return false;
            if (o.Options.DismissOnOutsideClick) { CloseOverlay(o); return o.Options.Modal; }
            if (o.Options.Modal) return true;
        }
        return false;
    }
}