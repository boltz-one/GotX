namespace GotX.Raster;

public static partial class SoftwareRenderer
{
    /// <summary>
    /// Màu một pixel của Quad (nền + gradient + viền, nhân độ phủ SDF): đúng công thức shader GPU. Hằng số của primitive tính một lần.
    /// Với hình thẳng trục, cách mọi góc >= bán kính + viền + 2, kết quả chỉ phụ thuộc y: toạ độ x chỉ vào qua |lx| - hw (rất âm: bị max(…, |ly| - hh) loại
    /// hoặc làm độ phủ / dải viền bão hoà ở 1 / 0 đúng bit), nên có thể lặp một pixel dọc đoạn giữa của hàng.
    /// </summary>
    readonly struct QuadShader
    {
        public readonly Frame F;
        public readonly float Hw, Hh, BgR, BgG, BgB, MaxBorder, Inset;
        public readonly Rgba Bg;
        public readonly bool HasGrad, HasBorder;
        /// <summary>Bốn cạnh cùng màu: chọn cạnh theo Side() không đổi kết quả (nếu khác màu, cạnh gần nhất đổi theo x ngay cả ở hàng sát mép).</summary>
        public bool UniformBorder => _top == _right && _top == _bottom && _top == _left;
        readonly Corners _corners, _inner;
        readonly GradientSampler _grad;
        readonly float _bt, _br, _bb, _bl, _icx, _icy, _ihw, _ihh;
        readonly BorderStyle _style;
        readonly Rgba _top, _right, _bottom, _left;

        public QuadShader(Scene scene, in Primitive p)
        {
            var b = p.Bounds;
            Hw = b.Width / 2; Hh = b.Height / 2;
            F = new Frame(b, p.Transform);
            _corners = ClampCorners(p.Corners, MathF.Min(Hw, Hh));
            // Viền từng cạnh → hộp bên trong (tâm lệch nếu các cạnh dày khác nhau)
            var w = p.BorderWidths;
            HasBorder = p.HasBorder && p.BorderStyle != BorderStyle.None;
            _bt = MathF.Min(w.Top, b.Height); _bb = MathF.Min(w.Bottom, b.Height); _bl = MathF.Min(w.Left, b.Width); _br = MathF.Min(w.Right, b.Width);
            float ix0 = -Hw + _bl, ix1 = Hw - _br, iy0 = -Hh + _bt, iy1 = Hh - _bb;
            _icx = (ix0 + ix1) / 2; _icy = (iy0 + iy1) / 2; _ihw = (ix1 - ix0) / 2; _ihh = (iy1 - iy0) / 2;
            _inner = new Corners(
                MathF.Max(0, _corners.TopLeft - MathF.Max(_bl, _bt)), MathF.Max(0, _corners.TopRight - MathF.Max(_br, _bt)),
                MathF.Max(0, _corners.BottomRight - MathF.Max(_br, _bb)), MathF.Max(0, _corners.BottomLeft - MathF.Max(_bl, _bb)));
            Bg = p.Color;
            BgR = Bg.R * Bg.A; BgG = Bg.G * Bg.A; BgB = Bg.B * Bg.A;
            HasGrad = p.Gradient != 0;
            _grad = HasGrad ? new GradientSampler(scene.Gradient(p.Gradient), Hw, Hh) : default;
            _style = p.BorderStyle;
            (_top, _right, _bottom, _left) = (p.BorderTop, p.BorderRight, p.BorderBottom, p.BorderLeft);
            float maxR = MathF.Max(MathF.Max(_corners.TopLeft, _corners.TopRight), MathF.Max(_corners.BottomRight, _corners.BottomLeft));
            MaxBorder = MathF.Max(MathF.Max(_bt, _bb), MathF.Max(_bl, _br));
            Inset = MathF.Max(maxR, MaxBorder);
        }

        /// <summary>false nếu pixel không được phủ; ngược lại (r, g, b, a) là tham số của <see cref="Blend"/> (đã nhân độ phủ).</summary>
        public bool Shade(float pxx, float py, in Shape clip, out float r, out float g, out float b, out float a)
        {
            r = g = b = a = 0;
            F.Local(pxx, py, out float lx, out float ly);
            float d = RoundedBox(lx, ly, Hw, Hh, PickRadius(_corners, lx, ly));
            float cov = Math.Clamp(0.5f - d, 0, 1);
            if (clip.On && cov > 0) cov *= clip.Coverage(pxx, py);
            if (cov <= 0) return false;

            float cr = BgR, cg = BgG, cb = BgB, ca = Bg.A;
            if (HasGrad)
            {
                var (gr, gg, gb, ga) = _grad.Sample(lx, ly);
                cr = gr + cr * (1 - ga); cg = gg + cg * (1 - ga); cb = gb + cb * (1 - ga); ca = ga + ca * (1 - ga);
            }
            if (HasBorder)
            {
                float ex = lx - _icx, ey = ly - _icy;
                float din = _ihw <= 0 || _ihh <= 0 ? 1 : RoundedBox(ex, ey, _ihw, _ihh, PickRadius(_inner, ex, ey));
                float band = 1 - Math.Clamp(0.5f - din, 0, 1); // 1 = trên viền, 0 = bên trong
                float amount = _style == BorderStyle.Solid ? band : Pattern(_style, lx, ly, Hw, Hh, _bt, _br, _bb, _bl, band);
                if (amount > 0)
                {
                    var side = Side(lx, ly, Hw, Hh, _bt, _br, _bb, _bl) switch { 0 => _top, 1 => _right, 2 => _bottom, _ => _left };
                    float sr = side.R * side.A, sg = side.G * side.A, sb = side.B * side.A;
                    if (side.A >= 1) { cr += (sr - cr) * amount; cg += (sg - cg) * amount; cb += (sb - cb) * amount; ca += (side.A - ca) * amount; }
                    else
                    {
                        // viền trong suốt / mờ vẽ ĐÈ LÊN nền (CSS: nền nằm dưới viền), không thay thế nền
                        float k = 1 - side.A * amount;
                        cr = sr * amount + cr * k; cg = sg * amount + cg * k; cb = sb * amount + cb * k; ca = side.A * amount + ca * k;
                    }
                }
            }
            r = cr * cov; g = cg * cov; b = cb * cov; a = ca * cov;
            return true;
        }
    }
}
