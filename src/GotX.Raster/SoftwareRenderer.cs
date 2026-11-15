namespace GotX.Raster;

/// <summary>
/// Renderer CPU cho Scene. Cùng mô hình với shader GPU: mỗi pixel được đưa về hệ toạ độ cục bộ của
/// primitive (bỏ xoay/skew), tính khoảng cách có dấu (SDF) tới hình chữ nhật bo góc rồi suy ra độ phủ
/// anti-alias. Viền từng cạnh có thể liền / gạch / chấm. Vùng cắt theo hình nhân thêm độ phủ của nó.
/// Pixel nằm hẳn bên trong (không biến đổi, không cắt hình, viền liền) được tô nhanh.
/// </summary>
public static partial class SoftwareRenderer
{
    public static void Render(Scene scene, Canvas canvas)
    {
        canvas.Clear(scene.ClearColor);
        RenderPrimitives(scene, canvas);
    }

    /// <summary>Vẽ các primitive lên canvas hiện có (không xoá nền) – để đo từng primitive.</summary>
    public static void RenderPrimitives(Scene scene, Canvas canvas)
    {
        foreach (ref readonly var p in scene.Primitives)
        {
            var clip = Shape.Of(scene, p.ClipShape);
            switch (p.Kind)
            {
                case PrimitiveKind.Quad: Quad(canvas, scene, p, clip); break;
                case PrimitiveKind.Shadow: Shadow(canvas, p, clip); break;
                case PrimitiveKind.Glyph: Glyph(canvas, scene, p, clip); break;
                case PrimitiveKind.Image: Image(canvas, p, clip); break;
            }
        }
    }

    // ---------- tiện ích ----------

    readonly struct Rect(int x0, int y0, int x1, int y1)
    {
        public readonly int X0 = x0, Y0 = y0, X1 = x1, Y1 = y1;
        public bool Empty => X1 <= X0 || Y1 <= Y0;
    }

    static Rect PixelRect(in Bounds area, in Bounds clip, Canvas c)
    {
        var b = area.Intersect(clip);
        // Giao rỗng ở toạ độ lẻ: floor/ceil sẽ biến nó thành dải rộng 1px nằm ngoài hình → đọc mask ngoài phạm vi.
        if (b.IsEmpty) return default;
        int x0 = Math.Max(0, (int)MathF.Floor(b.X)), y0 = Math.Max(0, (int)MathF.Floor(b.Y));
        int x1 = Math.Min(c.Width, (int)MathF.Ceiling(b.Right)), y1 = Math.Min(c.Height, (int)MathF.Ceiling(b.Bottom));
        return new Rect(x0, y0, x1, y1);
    }

    /// <summary>Đưa toạ độ pixel về hệ cục bộ (gốc ở tâm Bounds, đã bỏ biến đổi).</summary>
    readonly struct Frame
    {
        public readonly float Cx, Cy, I11, I12, I21, I22;
        public readonly bool T;

        public Frame(Bounds b, Mat2 m)
        {
            Cx = b.X + b.Width / 2; Cy = b.Y + b.Height / 2;
            T = m.IsTransform;
            var inv = T ? m.Inverse() : Mat2.Identity;
            I11 = inv.M11; I12 = inv.M12; I21 = inv.M21; I22 = inv.M22;
        }

        public void Local(float px, float py, out float lx, out float ly)
        {
            float dx = px - Cx, dy = py - Cy;
            if (T) { lx = I11 * dx + I12 * dy; ly = I21 * dx + I22 * dy; }
            else { lx = dx; ly = dy; }
        }
    }

    /// <summary>Vùng cắt theo hình (bo góc, xoay) áp lên primitive; On = false nếu không có.</summary>
    readonly struct Shape
    {
        public readonly bool On;
        readonly Frame _f;
        readonly float _hw, _hh;
        readonly Corners _c;
        // Hộp thẳng trục nằm chắc chắn trong hình (cách mép > bán kính góc lớn nhất + 1 px): ở đó độ phủ là đúng 1, khỏi tính SDF.
        readonly float _sx0, _sy0, _sx1, _sy1;

        Shape(in ClipShape s)
        {
            On = true;
            _f = new Frame(s.Rect, s.Transform);
            _hw = s.Rect.Width / 2; _hh = s.Rect.Height / 2;
            _c = ClampCorners(s.Corners, MathF.Min(_hw, _hh));
            if (!_f.T)
            {
                float inset = MathF.Max(MathF.Max(_c.TopLeft, _c.TopRight), MathF.Max(_c.BottomRight, _c.BottomLeft)) + 1;
                _sx0 = _f.Cx - _hw + inset; _sx1 = _f.Cx + _hw - inset; _sy0 = _f.Cy - _hh + inset; _sy1 = _f.Cy + _hh - inset;
            }
            else { _sx0 = _sy0 = float.PositiveInfinity; _sx1 = _sy1 = float.NegativeInfinity; }
        }

        /// <summary>Khoảng x (tâm pixel) chắc chắn phủ đủ trên hàng có tâm py; rỗng nếu không có.</summary>
        public bool SolidRow(float py, out float x0, out float x1)
        {
            x0 = _sx0; x1 = _sx1;
            return py >= _sy0 && py <= _sy1 && x0 <= x1;
        }

        public static Shape Of(Scene scene, int index) => index == 0 ? default : new Shape(scene.Shape(index));

        public float Coverage(float px, float py)
        {
            if (px >= _sx0 && px <= _sx1 && py >= _sy0 && py <= _sy1) return 1f;
            _f.Local(px, py, out float lx, out float ly);
            return Math.Clamp(0.5f - RoundedBox(lx, ly, _hw, _hh, PickRadius(_c, lx, ly)), 0, 1);
        }
    }

    /// <summary>Vùng pixel cần duyệt: hộp bao của hình (đã biến đổi nếu có), nới thêm margin.</summary>
    static Rect Area(in Primitive p, float margin, Canvas c)
    {
        var b = p.Bounds.Inflate(margin);
        if (p.Transform.IsTransform)
        {
            var m = p.Transform;
            float hw = b.Width / 2, hh = b.Height / 2;
            float ex = MathF.Abs(m.M11) * hw + MathF.Abs(m.M12) * hh, ey = MathF.Abs(m.M21) * hw + MathF.Abs(m.M22) * hh;
            float cx = b.X + hw, cy = b.Y + hh;
            b = new Bounds(cx - ex, cy - ey, 2 * ex, 2 * ey);
        }
        return PixelRect(b, p.Clip, c);
    }

    /// <summary>Source-over với màu premultiplied (0..1).</summary>
    static void Blend(ref uint dst, float r, float g, float b, float a)
    {
        if (a <= 0) return;
        if (a >= 0.999f) { dst = Canvas.Pack(r, g, b, 1); return; }
        float inv = 1 - a;
        uint d = dst;
        dst = Canvas.Pack(
            r + ((d >> 16) & 0xFF) / 255f * inv,
            g + ((d >> 8) & 0xFF) / 255f * inv,
            b + (d & 0xFF) / 255f * inv,
            a + (d >> 24) / 255f * inv);
    }

    /// <summary>SDF của hình chữ nhật bo góc tâm (0,0), nửa kích thước (hw, hh), bán kính r.</summary>
    static float RoundedBox(float px, float py, float hw, float hh, float r)
    {
        float qx = MathF.Abs(px) - hw + r, qy = MathF.Abs(py) - hh + r;
        float ox = MathF.Max(qx, 0), oy = MathF.Max(qy, 0);
        return MathF.Sqrt(ox * ox + oy * oy) + MathF.Min(MathF.Max(qx, qy), 0) - r;
    }

    static float PickRadius(in Corners c, float px, float py) =>
        px < 0 ? (py < 0 ? c.TopLeft : c.BottomLeft) : (py < 0 ? c.TopRight : c.BottomRight);

    static Corners ClampCorners(in Corners c, float max) => new(
        MathF.Min(c.TopLeft, max), MathF.Min(c.TopRight, max),
        MathF.Min(c.BottomRight, max), MathF.Min(c.BottomLeft, max));
}
