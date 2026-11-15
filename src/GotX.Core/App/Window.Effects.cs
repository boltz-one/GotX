using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using GotX.Text;
using GotX.Widgets;

namespace GotX;

public sealed partial class Window
{
    /// <summary>
    /// Đặt một hình chữ nhật toạ độ layout lên màn hình: trả về Bounds (device px) trước biến đổi
    /// và phần biến đổi tuyến tính quanh tâm (xoay/skew/phóng lệch trục) – đúng định dạng Primitive.
    /// </summary>
    Bounds Place(Bounds local, Xf xf, out Mat2 transform)
    {
        transform = default;
        if (xf.IsSimple)
        {
            var device = xf.ApplyAabb(local).Scale(_scale);
            return xf.S == 1 ? SnapToPixels(device) : device; // đang phóng/thu (animation scale) thì giữ toạ độ lẻ cho mượt
        }
        transform = xf.Shape;
        var c = xf.Apply(new Point(local.X + local.Width / 2, local.Y + local.Height / 2));
        float w = local.Width * xf.S * _scale, h = local.Height * xf.S * _scale;
        return new Bounds(c.X * _scale - w / 2, c.Y * _scale - h / 2, w, h);
    }

    /// <summary>
    /// Bắt bốn mép của hình vào lưới pixel thiết bị. Layout cho ra toạ độ lẻ (chia cột 1/3, chữ cao 19.25px, DPI 125%)
    /// và mép nằm giữa hai pixel sẽ bị nhoè; làm tròn từng mép (không làm tròn kích thước) nên hai hình chung mép
    /// vẫn khít nhau. Hình mỏng hơn một pixel vẫn giữ tối thiểu một pixel.
    /// </summary>
    static Bounds SnapToPixels(Bounds b)
    {
        float x0 = MathF.Floor(b.X + 0.5f), y0 = MathF.Floor(b.Y + 0.5f);
        float x1 = MathF.Floor(b.Right + 0.5f), y1 = MathF.Floor(b.Bottom + 0.5f);
        if (x1 <= x0 && b.Width > 0) x1 = x0 + 1;
        if (y1 <= y0 && b.Height > 0) y1 = y0 + 1;
        return new Bounds(x0, y0, x1 - x0, y1 - y0);
    }

    /// <summary>Thêm một lớp bóng cho phần tử có hộp layout lb; bóng (offset, blur, spread) biến đổi theo phần tử.</summary>
    void AddShadow(Bounds lb, in Corners corners, in BoxShadow sh, Rgba color, Bounds deviceClip, Xf xf)
    {
        float k = xf.S * _scale;
        var bounds = Place(lb.Offset(sh.OffsetX, sh.OffsetY).Inflate(sh.Spread), xf, out Mat2 rot);
        var box = Place(lb, xf, out _);
        var own = corners.Scale(k);
        Add(new Primitive
        {
            Kind = PrimitiveKind.Shadow,
            Gradient = 1, Source = box, BorderWidths = new Edges(own.TopLeft, own.TopRight, own.BottomRight, own.BottomLeft),
            Bounds = bounds,
            Transform = rot,
            Corners = Grow(corners, sh.Spread).Scale(k),
            Color = color,
            Blur = sh.Blur * k,
            Clip = deviceClip,
        });
    }

    void AddShadowPreset(ShadowPreset preset, Bounds lb, in Style s, float alpha, Bounds deviceClip, Xf xf)
    {
        if (preset == ShadowPreset.Custom)
        {
            var c = s.ShadowColor ?? s.CustomShadow.Color;
            AddShadow(lb, s.CornerRadii, s.CustomShadow, c with { A = c.A * alpha }, deviceClip, xf);
            return;
        }
        foreach (var layer in Theme.ShadowTable[(int)preset])
        {
            var c = s.ShadowColor ?? layer.Color;
            AddShadow(lb, s.CornerRadii, layer, c with { A = c.A * alpha }, deviceClip, xf);
        }
    }

    void ApplyTransition(long key, ref Style s, in TextStyle parentText, Bounds lb, ref ShadowPreset fromShadow, ref float shadowMix)
    {
        if (!_transitions.TryGetValue(key, out var st)) _transitions[key] = st = new TransitionState();
        st.Frame = _frame;
        var text = s.TextColorValue ?? parentText.Color;
        var target = new AnimVals(s.Background, s.BorderColorValue ?? text, text, s.RingColor ?? text, s.OutlineColor ?? text,
            s.Opacity, s.RingWidth, s.OutlineWidth,
            s.TranslateX.Resolve(lb.Width, RemSize) ?? 0, s.TranslateY.Resolve(lb.Height, RemSize) ?? 0,
            s.ScaleX, s.ScaleY, s.Rotate, s.SkewX, s.SkewY);
        float duration = s.DurationMs > 0 ? s.DurationMs : 150;
        if (st.Update(target, s.Shadow, _now, duration, s.DelayMs, s.Easing)) MarkAnimating();

        var c = st.Current;
        var p = s.Transition;
        if ((p & TransitionProps.Colors) != 0)
        {
            s.Background = c.Bg; s.BorderColorValue = c.Border; s.TextColorValue = c.Text;
            if (s.RingColor is not null || s.RingWidth > 0) s.RingColor = c.Ring;
            if (s.OutlineColor is not null || s.OutlineWidth > 0) s.OutlineColor = c.Outline;
        }
        if ((p & TransitionProps.Opacity) != 0) s.Opacity = c.Opacity;
        if ((p & TransitionProps.Shadow) != 0)
        {
            s.RingWidth = c.RingWidth; s.OutlineWidth = c.OutlineWidth;
            fromShadow = st.FromShadow; shadowMix = st.ShadowMix;
        }
        if ((p & TransitionProps.Transform) != 0)
        {
            s.TranslateX = Length.Px(c.Tx); s.TranslateY = Length.Px(c.Ty);
            s.ScaleX = c.ScaleX; s.ScaleY = c.ScaleY; s.Rotate = c.Rotate; s.SkewX = c.SkewX; s.SkewY = c.SkewY;
        }
    }

    ScrollState UpdateScroll(long key, Size content, Bounds lb, bool sx, bool sy)
    {
        if (!_scrolls.TryGetValue(key, out var st)) _scrolls[key] = st = new ScrollState();
        st.Frame = _frame;
        st.MaxX = sx ? MathF.Max(0, content.Width - lb.Width) : 0;
        st.MaxY = sy ? MathF.Max(0, content.Height - lb.Height) : 0;
        st.Viewport = new Size(lb.Width, lb.Height);
        ApplyPendingScroll(key, st);
        st.TargetX = Math.Clamp(st.TargetX, 0, st.MaxX);
        st.TargetY = Math.Clamp(st.TargetY, 0, st.MaxY);
        // Cuộn mượt: tiến dần về đích theo hàm mũ (hằng số 60ms), độc lập với tốc độ khung hình
        double dt = double.IsNaN(st.LastTime) ? 1 : Math.Clamp(_now - st.LastTime, 0, 0.1);
        float k = 1 - (float)Math.Exp(-dt / 0.06);
        st.X += (st.TargetX - st.X) * k;
        st.Y += (st.TargetY - st.Y) * k;
        if (MathF.Abs(st.TargetX - st.X) < 0.25f) st.X = st.TargetX;
        if (MathF.Abs(st.TargetY - st.Y) < 0.25f) st.Y = st.TargetY;
        st.X = Math.Clamp(st.X, 0, st.MaxX);
        st.Y = Math.Clamp(st.Y, 0, st.MaxY);
        st.LastTime = _now;
        if (st.X != st.TargetX || st.Y != st.TargetY) MarkAnimating();
        return st;
    }

    /// <summary>Thanh cuộn nổi, tính trong toạ độ layout của vùng cuộn rồi đặt theo phép biến đổi của nó.</summary>
    /// <summary>
    /// Thanh cuộn có nên hiện ở frame này. Hover: hiện khi con trỏ ở trên vùng cuộn, rời ra thì còn 600ms (như Radix
    /// ScrollArea type="hover"). Scroll: hiện khi vị trí cuộn đổi, đứng yên 600ms thì ẩn. Trong thời gian chờ ẩn,
    /// view được vẽ lại mỗi frame để thanh biến mất đúng lúc.
    /// </summary>
    bool ScrollbarVisible(ScrollState st, ScrollbarMode mode, bool hovered)
    {
        const double HideDelay = 0.6;
        switch (mode)
        {
            case ScrollbarMode.None: return false;
            case ScrollbarMode.Hover:
                if (hovered) { st.Hovered = true; return true; }
                if (st.Hovered) { st.Hovered = false; st.ShowUntil = _now + HideDelay; }
                break;
            case ScrollbarMode.Scroll:
                if (st.X != st.ShownX || st.Y != st.ShownY)
                {
                    if (!float.IsNaN(st.ShownX)) st.ShowUntil = _now + HideDelay; // lần vẽ đầu không tính là cuộn
                    st.ShownX = st.X; st.ShownY = st.Y;
                }
                break;
            default: return true;
        }
        if (_now >= st.ShowUntil) return false;
        MarkAnimating();
        return true;
    }

    void PaintScrollbars(ScrollState st, in Style s, Bounds lb, Edges bw, Xf xf, Bounds deviceClip, float op)
    {
        var color = Fade(s.ScrollbarColor ?? (_dark ? Rgba.FromHex(0x94a3b8, 0.45f) : Rgba.FromHex(0x64748b, 0.4f)), op);
        // Rãnh rộng ScrollbarWidth sát mép (viền trong 1px trong suốt + đệm 1px như ScrollAreaScrollbar của shadcn);
        // không đặt thì là thanh nổi mảnh 4px cách mép 2px.
        float thick = s.ScrollbarWidth > 0 ? MathF.Max(1, s.ScrollbarWidth - 3) : 4;
        float inset = s.ScrollbarWidth > 0 ? 1 : 2;
        float radius = thick / 2 * xf.S * _scale;
        if (st.MaxY > 0)
        {
            float track = lb.Height - bw.Vertical - 2 * inset;
            float thumb = MathF.Max(24, track * (lb.Height / (lb.Height + st.MaxY)));
            float y = lb.Y + bw.Top + inset + (track - thumb) * (st.Y / st.MaxY);
            AddLocalRect(new Bounds(lb.Right - bw.Right - inset - thick, y, thick, thumb), color, deviceClip, xf, radius);
        }
        if (st.MaxX > 0)
        {
            float track = lb.Width - bw.Horizontal - 2 * inset;
            float thumb = MathF.Max(24, track * (lb.Width / (lb.Width + st.MaxX)));
            float x = lb.X + bw.Left + inset + (track - thumb) * (st.X / st.MaxX);
            AddLocalRect(new Bounds(x, lb.Bottom - bw.Bottom - inset - thick, thumb, thick), color, deviceClip, xf, radius);
        }
    }

    void AddLocalRect(Bounds local, Rgba color, Bounds deviceClip, Xf xf, float radius)
    {
        var bounds = Place(local, xf, out Mat2 rot);
        Add(new Primitive
        {
            Kind = PrimitiveKind.Quad, Bounds = bounds, Transform = rot, Color = color, Clip = deviceClip, Corners = Corners.All(radius),
        });
    }

    void AddRect(Bounds layout, Rgba color, Bounds deviceClip, Xf xf)
    {
        var bounds = Place(layout, xf, out Mat2 rot);
        Add(new Primitive { Kind = PrimitiveKind.Quad, Bounds = bounds, Transform = rot, Color = color, Clip = deviceClip });
    }
}
