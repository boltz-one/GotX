using GotX.Text;

namespace GotX;

/// <summary>Các bước của <see cref="Paint"/>, tách riêng cho dễ đọc. Thứ tự gọi quyết định thứ tự vẽ.</summary>
public sealed partial class Window
{
    /// <summary>
    /// Trạng thái chuột (hover:, active:, group, cursor) – theo hộp chưa tính transform riêng của phần tử.
    /// Ghi vùng theo dõi chuột, rồi áp các variant trạng thái đang bật lên bản sao Style. Trả về: chuột có nằm trên phần tử.
    /// </summary>
    bool ApplyMouseState(ref Node n, ref Style s, Bounds lb, Bounds clip, Xf xf, long focusKey)
    {
        var cl = n.Classes;
        bool scrollHover = (s.ScrollX || s.ScrollY) && s.ScrollbarMode == ScrollbarMode.Hover;
        bool tracksMouse = scrollHover || n.Hover is not null || n.IsGroup || s.Cursor != CursorKind.Default || n.HoverHandler is not null
            || (cl is not null && (cl.States & (StateVariant.Hover | StateVariant.Active | StateVariant.GroupHover)) != 0);
        bool inside = false;
        if (tracksMouse)
        {
            var region = new HoverRegion
            {
                Local = lb, Xf = xf, Clip = clip, ViewId = CurrentView, Cursor = s.Cursor,
                Handler = n.HoverHandler, State = n.HoverState, Invoke = n.HoverInvoke,
                HasActive = cl is not null && (cl.States & StateVariant.Active) != 0,
                Visual = scrollHover || n.Hover is not null || n.IsGroup
                    || (cl is not null && (cl.States & (StateVariant.Hover | StateVariant.Active | StateVariant.GroupHover)) != 0),
            };
            inside = region.Inside = !_blockHover && region.Contains(_mouse);
            region.Pressed = inside && _mouseDown;
            _hovers.Add(region);
        }
        if (cl is not null)
        {
            var active = StateVariant.None;
            if (inside) active |= StateVariant.Hover;
            if (inside && _mouseDown) active |= StateVariant.Active;
            // Ô nhập luôn hiện vòng focus (như trình duyệt); phần tử khác chỉ hiện focus-visible khi focus bằng bàn phím.
            if (n.FocusInput is { } fi && fi == Focused) active |= StateVariant.Focus | StateVariant.FocusVisible;
            if (focusKey != 0 && focusKey == _focusKey) active |= _focusVisible ? StateVariant.Focus | StateVariant.FocusVisible : StateVariant.Focus;
            if (_groupHover > 0) active |= StateVariant.GroupHover;
            if (n.Disabled) active |= StateVariant.Disabled;
            cl.ApplyState(ref s, active, _tree.Variants);
        }
        if (inside && n.Hover is { } hover) hover(ref s);
        return inside;
    }

    /// <summary>Bóng đổ + ring (ghép như --tw-shadow / --tw-ring-shadow, dưới cùng → trên cùng), rồi nền + viền từng cạnh.</summary>
    void PaintBackdrop(in Style s, in TextStyle ts, Bounds lb, Edges bw, float op,
        ShadowPreset fromShadow, float shadowMix, Bounds deviceClip, Xf nodeXf)
    {
        if (shadowMix < 1 && fromShadow != s.Shadow)
        {
            if (fromShadow != ShadowPreset.None) AddShadowPreset(fromShadow, lb, s, op * (1 - shadowMix), deviceClip, nodeXf);
            if (s.Shadow != ShadowPreset.None) AddShadowPreset(s.Shadow, lb, s, op * shadowMix, deviceClip, nodeXf);
        }
        else if (s.Shadow != ShadowPreset.None) AddShadowPreset(s.Shadow, lb, s, op, deviceClip, nodeXf);
        if (s.RingWidth > 0)
        {
            // Ring là box-shadow có spread: CSS không vẽ bóng bên trong hộp, nên vẽ thành vành (quad chỉ có viền)
            // nằm ngoài border box. Nhờ vậy phần tử nền trong suốt không bị màu ring tràn vào trong.
            AddRing(lb, s.CornerRadii, s.RingOffsetWidth + s.RingWidth, s.RingWidth, Fade(s.RingColor ?? ts.Color, op), deviceClip, nodeXf);
            if (s.RingOffsetWidth > 0)
                AddRing(lb, s.CornerRadii, s.RingOffsetWidth, s.RingOffsetWidth, Fade(s.RingOffsetColor ?? Rgba.White, op), deviceClip, nodeXf);
        }

        // Nền + viền: màu riêng từng cạnh, mặc định currentColor
        bool hasBorder = bw.Top > 0 || bw.Right > 0 || bw.Bottom > 0 || bw.Left > 0;
        if (s.BgClip != BgClip.Border && (s.Background.A > 0 || s.Gradient.Kind != GradientKind.None))
        {
            PaintClippedFill(s, ts, lb, bw, op, deviceClip, nodeXf);
            return;
        }
        int gradient = AddGradientFill(s, lb, op);
        if (s.Background.A <= 0 && !hasBorder && gradient == 0) return;
        var baseBorder = s.BorderColorValue ?? ts.Color;
        float k = nodeXf.S * _scale;
        Add(new Primitive
        {
            Kind = PrimitiveKind.Quad,
            Bounds = Place(lb, nodeXf, out Mat2 rot),
            Transform = rot,
            Corners = s.CornerRadii.Scale(k),
            Color = Fade(s.Background, op),
            BorderWidths = new Edges(bw.Top * k, bw.Right * k, bw.Bottom * k, bw.Left * k),
            BorderTop = Fade(s.BorderTopColor ?? baseBorder, op),
            BorderRight = Fade(s.BorderRightColor ?? baseBorder, op),
            BorderBottom = Fade(s.BorderBottomColor ?? baseBorder, op),
            BorderLeft = Fade(s.BorderLeftColor ?? baseBorder, op),
            BorderStyle = s.BorderStyle,
            Gradient = gradient,
            Clip = deviceClip,
        });
    }

    /// <summary>
    /// background-clip: padding / content. Nền (màu + gradient) là một quad riêng thu vào hộp đệm / hộp nội dung, bán kính góc = bán kính ngoài
    /// trừ độ dày viền (và padding với content), kẹp ≥ 0 như CSS; viền vẫn vẽ ở hộp viền bằng quad chỉ có viền nên thấy được phần nằm sau phần tử.
    /// </summary>
    void PaintClippedFill(in Style s, in TextStyle ts, Bounds lb, Edges bw, float op, Bounds deviceClip, Xf nodeXf)
    {
        var cr = s.CornerRadii;
        var box = new Bounds(lb.X + bw.Left, lb.Y + bw.Top, MathF.Max(0, lb.Width - bw.Horizontal), MathF.Max(0, lb.Height - bw.Vertical));
        var inner = new Corners(
            MathF.Max(0, cr.TopLeft - MathF.Max(bw.Left, bw.Top)), MathF.Max(0, cr.TopRight - MathF.Max(bw.Right, bw.Top)),
            MathF.Max(0, cr.BottomRight - MathF.Max(bw.Right, bw.Bottom)), MathF.Max(0, cr.BottomLeft - MathF.Max(bw.Left, bw.Bottom)));
        if (s.BgClip == BgClip.Content)
        {
            var pad = s.Padding.Resolve(lb.Width, RemSize);
            box = new Bounds(box.X + pad.Left, box.Y + pad.Top, MathF.Max(0, box.Width - pad.Horizontal), MathF.Max(0, box.Height - pad.Vertical));
            inner = new Corners(
                MathF.Max(0, inner.TopLeft - MathF.Max(pad.Left, pad.Top)), MathF.Max(0, inner.TopRight - MathF.Max(pad.Right, pad.Top)),
                MathF.Max(0, inner.BottomRight - MathF.Max(pad.Right, pad.Bottom)), MathF.Max(0, inner.BottomLeft - MathF.Max(pad.Left, pad.Bottom)));
        }
        float k = nodeXf.S * _scale;
        int gradient = AddGradientFill(s, box, op);
        if ((s.Background.A > 0 || gradient != 0) && !box.IsEmpty)
            Add(new Primitive
            {
                Kind = PrimitiveKind.Quad, Bounds = Place(box, nodeXf, out Mat2 rot), Transform = rot, Corners = inner.Scale(k),
                Color = Fade(s.Background, op), Gradient = gradient, Clip = deviceClip,
            });
        if (bw.Top > 0 || bw.Right > 0 || bw.Bottom > 0 || bw.Left > 0)
        {
            var baseBorder = s.BorderColorValue ?? ts.Color;
            Add(new Primitive
            {
                Kind = PrimitiveKind.Quad, Bounds = Place(lb, nodeXf, out Mat2 brot), Transform = brot, Corners = s.CornerRadii.Scale(k),
                BorderWidths = new Edges(bw.Top * k, bw.Right * k, bw.Bottom * k, bw.Left * k),
                BorderTop = Fade(s.BorderTopColor ?? baseBorder, op), BorderRight = Fade(s.BorderRightColor ?? baseBorder, op),
                BorderBottom = Fade(s.BorderBottomColor ?? baseBorder, op), BorderLeft = Fade(s.BorderLeftColor ?? baseBorder, op),
                BorderStyle = s.BorderStyle, Clip = deviceClip,
            });
        }
    }

    /// <summary>Vành rộng <paramref name="width"/> có mép ngoài cách border box <paramref name="grow"/>, bo theo góc của phần tử.</summary>
    void AddRing(Bounds lb, in Corners corners, float grow, float width, Rgba color, Bounds deviceClip, Xf nodeXf)
    {
        float k = nodeXf.S * _scale;
        float w = width * k + 0.5f; // lấn vào trong nửa pixel để không hở khe anti-alias giữa vành và mép phần tử
        Add(new Primitive
        {
            Kind = PrimitiveKind.Quad,
            Bounds = Place(lb.Inflate(grow), nodeXf, out var m),
            Transform = m,
            Corners = Grow(corners, grow).Scale(k),
            BorderWidths = new Edges(w, w, w, w),
            BorderTop = color, BorderRight = color, BorderBottom = color, BorderLeft = color,
            BorderStyle = BorderStyle.Solid,
            Clip = deviceClip,
        });
    }
    /// <summary>
    /// overflow-hidden: thu vùng cắt thẳng trục của các con vào padding box; phần tử bo góc hoặc bị xoay/skew
    /// thì thêm vùng cắt theo hình (có anti-alias). Trả về shape đang có hiệu lực trước đó để khôi phục
    /// sau khi vẽ xong các con, hoặc -1 nếu không đổi shape.
    /// </summary>
    int PushClip(in Style s, Bounds lb, Edges bw, Bounds clip, Xf nodeXf, ref Bounds childClip)
    {
        // Vùng cắt thẳng trục; phần tử bị xoay thì dùng hộp bao (xấp xỉ).
        var local = new Bounds(lb.X + bw.Left, lb.Y + bw.Top,
            MathF.Max(0, lb.Width - bw.Horizontal), MathF.Max(0, lb.Height - bw.Vertical));
        var rect = nodeXf.ApplyAabb(local);
        childClip = clip.Intersect(rect);
        // Vùng cắt nằm trọn trong vùng cắt ngoài thì khi phát lại chỉ cần dời theo; nếu bị xén thì không.
        if (!Within(rect, clip))
            for (int i = 0; i < _innerClip.Count; i++) _innerClip[i] = true;

        var cr = s.CornerRadii;
        if (nodeXf.IsSimple && cr.TopLeft <= 0 && cr.TopRight <= 0 && cr.BottomRight <= 0 && cr.BottomLeft <= 0) return -1;
        var inner = new Corners(
            MathF.Max(0, cr.TopLeft - MathF.Max(bw.Left, bw.Top)), MathF.Max(0, cr.TopRight - MathF.Max(bw.Right, bw.Top)),
            MathF.Max(0, cr.BottomRight - MathF.Max(bw.Right, bw.Bottom)), MathF.Max(0, cr.BottomLeft - MathF.Max(bw.Left, bw.Bottom)));
        var rectDev = Place(local, nodeXf, out var m);
        int saved = _shape;
        _shape = _scene.AddClipShape(new ClipShape(rectDev, inner.Scale(nodeXf.S * _scale), m));
        return saved;
    }

    /// <summary>Outline: ngoài border box, cách offset, theo bo góc; vẽ sau nội dung.</summary>
    void PaintOutline(in Style s, in TextStyle ts, Bounds lb, float op, Bounds deviceClip, Xf nodeXf)
    {
        float grow = s.OutlineOffset + s.OutlineWidth, k = nodeXf.S * _scale, w = s.OutlineWidth * k;
        var oc = Fade(s.OutlineColor ?? ts.Color, op);
        Add(new Primitive
        {
            Kind = PrimitiveKind.Quad,
            Bounds = Place(lb.Inflate(grow), nodeXf, out var om),
            Transform = om,
            Corners = Grow(s.CornerRadii, grow).Scale(k),
            BorderWidths = new Edges(w, w, w, w),
            BorderTop = oc, BorderRight = oc, BorderBottom = oc, BorderLeft = oc,
            BorderStyle = s.OutlineStyle,
            Clip = deviceClip,
        });
    }
}
