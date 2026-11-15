namespace GotX;

/// <summary>Phần tuyến tính 2×2 của một phép biến đổi (device = tâm + M·(p − tâm)). default = đơn vị.</summary>
public readonly record struct Mat2(float M11, float M12, float M21, float M22)
{
    public static readonly Mat2 Identity = new(1, 0, 0, 1);
    /// <summary>Có biến đổi thật sự (không phải đơn vị, không phải giá trị default).</summary>
    public bool IsTransform => !(M12 == 0 && M21 == 0 && ((M11 == 1 && M22 == 1) || (M11 == 0 && M22 == 0)));
    public float Det => M11 * M22 - M12 * M21;
    public Mat2 Inverse() { float d = Det; return d == 0 ? Identity : new(M22 / d, -M12 / d, -M21 / d, M11 / d); }
    public Mat2 OrIdentity => IsTransform ? this : Identity;
}

/// <summary>
/// Phép biến đổi affine 2D: x' = A·x + C·y + Tx, y' = B·x + D·y + Ty (trục Y hướng xuống).
/// Đủ cho translate / rotate / scale-x/y / skew / transform-origin của Tailwind và cho cuộn; ghép lồng nhau được.
/// </summary>
internal readonly struct Xf : IEquatable<Xf>
{
    public readonly float A, B, C, D, Tx, Ty;

    public Xf(float a, float b, float c, float d, float tx, float ty) { A = a; B = b; C = c; D = d; Tx = tx; Ty = ty; }

    public static readonly Xf Identity = new(1, 0, 0, 1, 0, 0);
    public static Xf Translation(float x, float y) => new(1, 0, 0, 1, x, y);

    /// <summary>Hệ số phóng trung bình (căn bậc hai định thức) – dùng cho cỡ chữ, độ dày viền, blur.</summary>
    public float S => MathF.Sqrt(MathF.Abs(A * D - B * C));

    /// <summary>Chỉ phóng đều + tịnh tiến (không xoay, không skew, không phóng lệch trục).</summary>
    public bool IsSimple => B == 0 && C == 0 && A == D && A > 0;

    /// <summary>Phần tuyến tính đã chuẩn hoá theo S (định thức ≈ ±1) – gắn vào Primitive.</summary>
    public Mat2 Shape { get { float s = S; return s == 0 ? Mat2.Identity : new Mat2(A / s, C / s, B / s, D / s); } }

    public bool SameLinear(in Xf o) => A == o.A && B == o.B && C == o.C && D == o.D;

    public Point Apply(Point p) => new(A * p.X + C * p.Y + Tx, B * p.X + D * p.Y + Ty);

    /// <summary>Hộp bao thẳng trục của hình chữ nhật sau biến đổi.</summary>
    public Bounds ApplyAabb(Bounds b)
    {
        if (IsSimple) return new(b.X * A + Tx, b.Y * A + Ty, b.Width * A, b.Height * A);
        var p0 = Apply(new Point(b.X, b.Y)); var p1 = Apply(new Point(b.Right, b.Y));
        var p2 = Apply(new Point(b.X, b.Bottom)); var p3 = Apply(new Point(b.Right, b.Bottom));
        float x0 = MathF.Min(MathF.Min(p0.X, p1.X), MathF.Min(p2.X, p3.X)), x1 = MathF.Max(MathF.Max(p0.X, p1.X), MathF.Max(p2.X, p3.X));
        float y0 = MathF.Min(MathF.Min(p0.Y, p1.Y), MathF.Min(p2.Y, p3.Y)), y1 = MathF.Max(MathF.Max(p0.Y, p1.Y), MathF.Max(p2.Y, p3.Y));
        return new(x0, y0, x1 - x0, y1 - y0);
    }

    public Point Inverse(Point p)
    {
        float det = A * D - B * C;
        if (det == 0) return p;
        float x = p.X - Tx, y = p.Y - Ty;
        return new((D * x - C * y) / det, (-B * x + A * y) / det);
    }

    /// <summary>this ∘ inner: áp inner trước rồi this.</summary>
    public Xf Then(Xf i) => new(
        A * i.A + C * i.B, B * i.A + D * i.B,
        A * i.C + C * i.D, B * i.C + D * i.D,
        A * i.Tx + C * i.Ty + Tx, B * i.Tx + D * i.Ty + Ty);

    public Xf Translate(float dx, float dy) => new(A, B, C, D, Tx + dx, Ty + dy);

    /// <summary>
    /// Biến đổi riêng của phần tử theo CSS: dời gốc về origin → translate → rotate → scale → skew → dời gốc lại.
    /// (Tailwind v4 dùng các thuộc tính translate/rotate/scale riêng, skew nằm trong transform.)
    /// </summary>
    public static Xf Element(Point origin, float tx, float ty, float rotateDeg, float sx, float sy, float skewXDeg, float skewYDeg)
    {
        float r = rotateDeg * (MathF.PI / 180), cos = MathF.Cos(r), sin = MathF.Sin(r);
        float kx = MathF.Tan(skewXDeg * (MathF.PI / 180)), ky = MathF.Tan(skewYDeg * (MathF.PI / 180));
        // R · S · Kx · Ky  với  Kx = [1 kx; 0 1], Ky = [1 0; ky 1]
        var rs = new Xf(cos * sx, sin * sx, -sin * sy, cos * sy, 0, 0);
        var k = new Xf(1 + kx * ky, ky, kx, 1, 0, 0);
        var m = rs.Then(k);
        var mo = m.Apply(origin);
        return new Xf(m.A, m.B, m.C, m.D, origin.X + tx - mo.X, origin.Y + ty - mo.Y);
    }

    public bool Equals(Xf o) => A == o.A && B == o.B && C == o.C && D == o.D && Tx == o.Tx && Ty == o.Ty;
    public override bool Equals(object? obj) => obj is Xf o && Equals(o);
    public override int GetHashCode() => HashCode.Combine(A, B, C, D, Tx, Ty);
    public static bool operator ==(Xf a, Xf b) => a.Equals(b);
    public static bool operator !=(Xf a, Xf b) => !a.Equals(b);
}
