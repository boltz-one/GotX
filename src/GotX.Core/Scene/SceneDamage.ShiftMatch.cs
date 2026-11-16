namespace GotX;

public sealed partial class SceneDamage
{
    /// <summary>Phép cộng dy chính xác trong float (không làm tròn): raster của bản dời khi đó là đúng bản dịch của raster bản cũ.</summary>
    static bool Exact(float o, float n, int d) => (double)n == (double)o + d;

    /// <summary>Cùng X / rộng / cao; Y, tâm và đáy (cách renderer tính) dời đúng d. d = 0: bằng nhau hoàn toàn.</summary>
    static bool ShiftedY(in Bounds o, in Bounds n, int d) =>
        o.X == n.X && o.Width == n.Width && o.Height == n.Height
        && (d == 0 ? o.Y == n.Y : Exact(o.Y, n.Y, d) && Exact(o.Y + o.Height / 2, n.Y + n.Height / 2, d) && Exact(o.Bottom, n.Bottom, d));

    static bool Covers(in Bounds clip, in Bounds region) =>
        MathF.Floor(clip.X) <= region.X && MathF.Floor(clip.Y) <= region.Y && MathF.Ceiling(clip.Right) >= region.Right && MathF.Ceiling(clip.Bottom) >= region.Bottom;

    /// <summary>Primitive mới <paramref name="p"/> có đúng là primitive cũ <paramref name="o"/> dời dy (dy = 0: giữ nguyên) với các biến thể vùng cắt đã chọn không.</summary>
    static bool Same(Scene so, in Primitive o, Scene sn, in Primitive p, int dy, bool clipShift, bool shapeShift, in Bounds region)
    {
        if (o.Kind != p.Kind || o.Glyph != p.Glyph || o.BorderStyle != p.BorderStyle) return false;
        if (!ShiftedY(o.Bounds, p.Bounds, dy)) return false;
        if (p.HasExclusion && !ShiftedY(o.Source, p.Source, dy)) return false;
        if (clipShift ? !ShiftedY(o.Clip, p.Clip, dy) : o.Clip != p.Clip || (dy != 0 && !Covers(p.Clip, region))) return false;
        if ((o.ClipShape != 0) != (p.ClipShape != 0)) return false;
        if (p.ClipShape != 0)
        {
            var a = so.Shape(o.ClipShape); var b = sn.Shape(p.ClipShape);
            if (a.Corners != b.Corners || a.Transform != b.Transform) return false;
            if (shapeShift ? !ShiftedY(a.Rect, b.Rect, dy) : a.Rect != b.Rect || (dy != 0 && b.Transform.IsTransform)) return false;
        }
        if (p.Kind == PrimitiveKind.Quad && p.Gradient != 0 && (o.Gradient == 0 || so.Gradient(o.Gradient) != sn.Gradient(p.Gradient))) return false;
        if (p.Kind != PrimitiveKind.Quad && o.Gradient != p.Gradient) return false;
        if (p.Kind == PrimitiveKind.Quad && (o.Gradient != 0) != (p.Gradient != 0)) return false;
        // Các trường còn lại (màu, viền, góc, blur, biến đổi, Source của ảnh…) phải trùng từng byte: chép bản cũ, vá những trường đã so ở trên rồi so khối.
        var t = o;
        t.Bounds = p.Bounds; t.Clip = p.Clip; t.ClipShape = p.ClipShape; t.Gradient = p.Gradient;
        if (p.HasExclusion) t.Source = p.Source;
        return System.Runtime.InteropServices.MemoryMarshal.AsBytes(System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpan(in t, 1))
            .SequenceEqual(System.Runtime.InteropServices.MemoryMarshal.AsBytes(System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpan(in p, 1)));
    }


    /// <summary>Primitive dời theo nội dung nhưng vùng cắt hình đứng yên (thẻ bo góc bao vùng cuộn): độ phủ ở góc / mép trên dưới của hình không dời theo.</summary>
    static bool StaticShape(Scene sn, Scene so, in Primitive p, in Primitive o) => so.Shape(o.ClipShape).Rect == sn.Shape(p.ClipShape).Rect;

    void MarkShapeCorners(in ClipShape s, int tx, int ty)
    {
        var c = s.Corners;
        float r = MathF.Max(MathF.Max(c.TopLeft, c.TopRight), MathF.Max(c.BottomRight, c.BottomLeft)) + 4;
        var b = s.Rect;
        Mark(new Bounds(b.X - 2, b.Y - 2, b.Width + 4, r), tx, ty);
        Mark(new Bounds(b.X - 2, b.Bottom + 2 - r, b.Width + 4, r), tx, ty);
    }

    /// <summary>Quad đứng yên một màu phẳng phủ kín R (mép / bo góc / viền đều nằm ngoài R): dời nội dung bên trên / dưới nó không đổi kết quả blend.</summary>
    static bool UniformOver(in Primitive p, in Bounds region)
    {
        if (p.Kind != PrimitiveKind.Quad || p.Gradient != 0 || p.ClipShape != 0 || p.Transform.IsTransform || !Covers(p.Clip, region)) return false;
        var bw = p.BorderWidths; var c = p.Corners;
        float inset = MathF.Max(MathF.Max(bw.Top, bw.Right), MathF.Max(bw.Bottom, bw.Left))
            + MathF.Max(MathF.Max(c.TopLeft, c.TopRight), MathF.Max(c.BottomRight, c.BottomLeft)) + 2;
        return p.Bounds.X + inset <= region.X && p.Bounds.Y + inset <= region.Y && p.Bounds.Right - inset >= region.Right && p.Bounds.Bottom - inset >= region.Bottom;
    }
}
