namespace GotX;

/// <summary>Vẽ hình vector và ảnh bitmap của một phần tử (<c>window.Path</c>, <c>window.Image</c>).</summary>
public sealed partial class Window
{
    /// <summary>Hộp nội dung (bỏ viền và padding) của phần tử, toạ độ layout.</summary>
    Bounds ContentBox(in Style s, Bounds lb, Edges bw)
    {
        var pad = s.Padding.Resolve(lb.Width, RemSize);
        return new Bounds(lb.X + bw.Left + pad.Left, lb.Y + bw.Top + pad.Top,
            MathF.Max(0, lb.Width - bw.Horizontal - pad.Horizontal), MathF.Max(0, lb.Height - bw.Vertical - pad.Vertical));
    }

    /// <summary>
    /// Raster path đúng theo số pixel thiết bị của hộp (nên nét luôn sắc ở mọi DPI/scale) rồi vẽ như một glyph
    /// bằng màu chữ. Giữ tỉ lệ viewBox và căn giữa, trừ khi phần tử có <c>object-fill</c>.
    /// </summary>
    void PaintVector(VectorPath path, in Style s, in TextStyle ts, Bounds lb, Edges bw, Bounds deviceClip, Xf xf)
    {
        var box = ContentBox(s, lb, bw);
        if (box.IsEmpty) return;
        if (s.ObjectFit != ObjectFit.Fill)
        {
            float k = MathF.Min(box.Width / path.Width, box.Height / path.Height);
            float w = path.Width * k, h = path.Height * k;
            box = new Bounds(box.X + (box.Width - w) / 2, box.Y + (box.Height - h) / 2, w, h);
        }
        float unit = xf.S * _scale; // pixel thiết bị trên một đơn vị layout
        int pw = Math.Max(1, (int)MathF.Round(box.Width * unit)), ph = Math.Max(1, (int)MathF.Round(box.Height * unit));
        int gi = Text.Atlas.GetPath(path, pw, ph);
        var mask = Text.Atlas[gi];
        if (mask.Width <= 0) return;
        var color = Fade(ts.Color, ts.Opacity);
        if (xf.IsSimple)
        {
            var o = xf.Apply(new Point(box.X, box.Y));
            Add(new Primitive
            {
                Kind = PrimitiveKind.Glyph, Glyph = gi, Color = color, Clip = deviceClip,
                Bounds = new Bounds(MathF.Round(o.X * _scale) + mask.Left, MathF.Round(o.Y * _scale) + mask.Top, mask.Width, mask.Height),
            });
            return;
        }
        var local = new Bounds(box.X + mask.Left / unit, box.Y + mask.Top / unit, mask.Width / unit, mask.Height / unit);
        var bounds = Place(local, xf, out Mat2 rot);
        Add(new Primitive { Kind = PrimitiveKind.Glyph, Glyph = gi, Color = color, Clip = deviceClip, Bounds = bounds, Transform = rot });
    }

    /// <summary>Ảnh lấp hộp nội dung theo object-fit; góc bo của phần tử (trừ đi viền) cắt luôn ảnh.</summary>
    void PaintImage(ImageSource image, in Style s, Bounds lb, Edges bw, float opacity, Bounds deviceClip, Xf xf)
    {
        var box = ContentBox(s, lb, bw);
        if (box.IsEmpty || opacity <= 0) return;
        var source = new Bounds(0, 0, image.Width, image.Height);
        float boxRatio = box.Width / box.Height, imageRatio = (float)image.Width / image.Height;
        var corners = s.CornerRadii;
        if (s.ObjectFit == ObjectFit.Cover)
        {
            // Cắt bớt ảnh theo chiều thừa, giữ phần giữa
            if (imageRatio > boxRatio) { float w = image.Height * boxRatio; source = new Bounds((image.Width - w) / 2, 0, w, image.Height); }
            else { float h = image.Width / boxRatio; source = new Bounds(0, (image.Height - h) / 2, image.Width, h); }
        }
        else if (s.ObjectFit == ObjectFit.Contain)
        {
            float w = imageRatio > boxRatio ? box.Width : box.Height * imageRatio;
            float h = imageRatio > boxRatio ? box.Width / imageRatio : box.Height;
            box = new Bounds(box.X + (box.Width - w) / 2, box.Y + (box.Height - h) / 2, w, h);
            corners = default;
        }
        float inset = MathF.Max(MathF.Max(bw.Left, bw.Top), MathF.Max(bw.Right, bw.Bottom));
        Add(new Primitive
        {
            Kind = PrimitiveKind.Image, Glyph = image.Id, Source = source,
            Bounds = Place(box, xf, out Mat2 rot), Transform = rot,
            Corners = Grow(corners, -inset).Scale(xf.S * _scale),
            Color = new Rgba(1, 1, 1, opacity), Clip = deviceClip,
        });
    }
}