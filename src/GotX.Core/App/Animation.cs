namespace GotX;

/// <summary>Hàm easing cubic-bezier giống CSS (giá trị mặc định của Tailwind v4).</summary>
public static class Ease
{
    public static float Apply(Easing e, float t) => e switch
    {
        Easing.Linear => t,
        Easing.In => Bezier(0.4f, 0, 1, 1, t),
        Easing.Out => Bezier(0, 0, 0.2f, 1, t),
        _ => Bezier(0.4f, 0, 0.2f, 1, t), // ease-in-out = timing mặc định của transition
    };

    /// <summary>cubic-bezier(x1, y1, x2, y2) tại tiến độ t: tìm u với x(u) = t (Newton + chia đôi) rồi trả y(u).</summary>
    public static float Bezier(float x1, float y1, float x2, float y2, float t)
    {
        if (t <= 0) return 0;
        if (t >= 1) return 1;
        static float Curve(float a, float b, float u) => 3 * (1 - u) * (1 - u) * u * a + 3 * (1 - u) * u * u * b + u * u * u;
        static float Slope(float a, float b, float u) => 3 * (1 - u) * (1 - u) * a + 6 * (1 - u) * u * (b - a) + 3 * u * u * (1 - b);
        float x = t;
        for (int i = 0; i < 8; i++)
        {
            float err = Curve(x1, x2, x) - t;
            if (MathF.Abs(err) < 1e-5f) return Curve(y1, y2, x);
            float d = Slope(x1, x2, x);
            if (MathF.Abs(d) < 1e-6f) break;
            x -= err / d;
        }
        float lo = 0, hi = 1;
        x = t;
        for (int i = 0; i < 30; i++)
        {
            float v = Curve(x1, x2, x);
            if (MathF.Abs(v - t) < 1e-5f) break;
            if (v < t) lo = x; else hi = x;
            x = (lo + hi) / 2;
        }
        return Curve(y1, y2, x);
    }

    /// <summary>Trộn màu trong không gian premultiplied (không bị viền xám khi đi từ trong suốt).</summary>
    public static Rgba Mix(Rgba a, Rgba b, float t)
    {
        float A = a.A + (b.A - a.A) * t;
        if (A <= 1e-5f) return Rgba.Transparent;
        return new Rgba(
            (a.R * a.A + (b.R * b.A - a.R * a.A) * t) / A,
            (a.G * a.A + (b.G * b.A - a.G * a.A) * t) / A,
            (a.B * a.A + (b.B * b.A - a.B * a.A) * t) / A,
            A);
    }
}

/// <summary>Các giá trị có thể transition của một phần tử.</summary>
internal record struct AnimVals(Rgba Bg, Rgba Border, Rgba Text, Rgba Ring, Rgba Outline, float Opacity, float RingWidth,
    float OutlineWidth, float Tx, float Ty, float ScaleX, float ScaleY, float Rotate, float SkewX, float SkewY)
{
    static float L(float a, float b, float t) => a + (b - a) * t;

    public static AnimVals Lerp(in AnimVals a, in AnimVals b, float t) => new(
        Ease.Mix(a.Bg, b.Bg, t), Ease.Mix(a.Border, b.Border, t), Ease.Mix(a.Text, b.Text, t), Ease.Mix(a.Ring, b.Ring, t),
        Ease.Mix(a.Outline, b.Outline, t), L(a.Opacity, b.Opacity, t), L(a.RingWidth, b.RingWidth, t),
        L(a.OutlineWidth, b.OutlineWidth, t), L(a.Tx, b.Tx, t), L(a.Ty, b.Ty, t), L(a.ScaleX, b.ScaleX, t), L(a.ScaleY, b.ScaleY, t),
        L(a.Rotate, b.Rotate, t), L(a.SkewX, b.SkewX, t), L(a.SkewY, b.SkewY, t));
}

/// <summary>
/// Trạng thái transition của một phần tử, giữ qua các frame (khoá theo view + thứ tự / .Key()).
/// Khi giá trị đích đổi, tween mới bắt đầu từ giá trị đang hiển thị – nên đổi hướng giữa chừng vẫn mượt.
/// </summary>
internal sealed class TransitionState : ElementState
{
    AnimVals _from, _to;
    ShadowPreset _fromShadow, _toShadow;
    double _start;
    float _duration, _delay;
    Easing _ease;
    bool _init;

    public AnimVals Current;
    public ShadowPreset FromShadow => _fromShadow;
    public float ShadowMix { get; private set; } = 1;

    /// <summary>Cập nhật theo đích mới; trả về true nếu vẫn đang chạy.</summary>
    public bool Update(in AnimVals target, ShadowPreset shadow, double now, float durationMs, float delayMs, Easing ease)
    {
        if (!_init)
        {
            _from = _to = Current = target;
            _fromShadow = _toShadow = shadow;
            _init = true;
            return false;
        }
        if (target != _to || shadow != _toShadow)
        {
            _from = Current;
            _fromShadow = ShadowMix >= 0.5f ? _toShadow : _fromShadow;
            _to = target;
            _toShadow = shadow;
            _start = now;
            _duration = durationMs; _delay = delayMs; _ease = ease;
        }
        float elapsed = (float)((now - _start) * 1000) - _delay;
        float t = _duration <= 0 ? 1 : Math.Clamp(elapsed / _duration, 0, 1);
        float k = Ease.Apply(_ease, t);
        Current = AnimVals.Lerp(_from, _to, k);
        ShadowMix = _fromShadow == _toShadow ? 1 : k;
        return t < 1;
    }
}

/// <summary>Keyframes của animate-pulse / animate-ping / animate-bounce (giá trị của Tailwind v4).</summary>
internal static class Keyframes
{
    static float Frac(double v) => (float)(v - Math.Floor(v));

    /// <summary>Áp animation tại thời điểm now (giây). ty tính theo phần trăm chiều cao phần tử.</summary>
    public static void Apply(AnimationKind kind, double now, ref float opacity, ref float scaleX, ref float scaleY, ref float tyFraction, ref float rotate)
    {
        switch (kind)
        {
            case AnimationKind.Spin: // 1s linear infinite; to { rotate: 360deg }
                rotate += 360 * Frac(now);
                break;
            case AnimationKind.Pulse: // 2s cubic-bezier(.4,0,.6,1) infinite; 50% { opacity: .5 }
            {
                float p = Frac(now / 2);
                float v = p < 0.5f ? Ease.Bezier(0.4f, 0, 0.6f, 1, p * 2) : 1 - Ease.Bezier(0.4f, 0, 0.6f, 1, (p - 0.5f) * 2);
                opacity *= 1 - 0.5f * v;
                break;
            }
            case AnimationKind.Ping: // 1s cubic-bezier(0,0,.2,1) infinite; 75%, 100% { scale: 2; opacity: 0 }
            {
                float p = Frac(now);
                float v = p < 0.75f ? Ease.Bezier(0, 0, 0.2f, 1, p / 0.75f) : 1;
                scaleX *= 1 + v;
                scaleY *= 1 + v;
                opacity *= 1 - v;
                break;
            }
            case AnimationKind.Bounce: // 1s infinite; 0%,100% { translateY(-25%) } 50% { none }
            {
                float p = Frac(now);
                tyFraction += p < 0.5f
                    ? -0.25f * (1 - Ease.Bezier(0.8f, 0, 1, 1, p * 2))
                    : -0.25f * Ease.Bezier(0, 0, 0.2f, 1, (p - 0.5f) * 2);
                break;
            }
        }
    }
}
