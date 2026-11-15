namespace GotX.Components;

/// <summary>
/// Thanh trượt một giá trị (slider.tsx): nhấn hoặc kéo trên thanh để đặt giá trị, làm tròn theo <see cref="Step"/>.
/// Bàn phím như Base UI: ←/↓ giảm và →/↑ tăng một bước, PageDown/PageUp (hoặc Shift + mũi tên) nhảy
/// <see cref="LargeStep"/>, Home/End về Min/Max; mỗi lần đổi bằng phím phát cả <see cref="ValueCommitted"/>.
/// Bản gốc cho nhiều con chạy (khoảng); ở đây chỉ một con chạy.
/// </summary>
public sealed class Slider : View
{
    float _min, _max = 100, _step = 1, _value;

    /// <summary>Giá trị vừa đổi (đã làm tròn theo bước).</summary>
    public readonly record struct ValueChanged(float Value);

    /// <summary>Kéo xong (thả chuột) – tương ứng onValueCommitted.</summary>
    public readonly record struct ValueCommitted(float Value);

    /// <summary>Giá trị nhỏ nhất.</summary>
    public float Min { get => _min; set { _min = value; Value = _value; Notify(); } }

    /// <summary>Giá trị lớn nhất.</summary>
    public float Max { get => _max; set { _max = value; Value = _value; Notify(); } }

    /// <summary>Bước nhảy (&gt; 0).</summary>
    public float Step { get => _step; set { _step = value > 0 ? value : 1; Value = _value; } }

    /// <summary>Bước nhảy lớn cho PageUp/PageDown và Shift + mũi tên (mặc định 10 như Base UI).</summary>
    public float LargeStep { get; set; } = 10;

    /// <summary>Giá trị hiện tại; gán vào sẽ được kẹp trong [Min, Max] và làm tròn theo bước.</summary>
    public float Value
    {
        get => _value;
        set
        {
            float v = Snap(value);
            if (v == _value) return;
            _value = v;
            Notify();
            Emit(new ValueChanged(v));
        }
    }

    /// <summary>Vô hiệu hoá: mờ đi, bỏ qua nhấn/kéo.</summary>
    public bool Disabled { get; set; }

    /// <summary>Tỉ lệ của giá trị trên thanh, 0..1.</summary>
    public float Fraction => _max > _min ? (_value - _min) / (_max - _min) : 0;

    float Snap(float v)
    {
        if (_max <= _min) return _min;
        v = Math.Clamp(v, _min, _max);
        // Làm tròn theo số bước tính từ Min (không phải từ 0), rồi kẹp lại vì bước cuối có thể vượt Max.
        float snapped = _min + MathF.Round((v - _min) / _step) * _step;
        return Math.Clamp(MathF.Round(snapped, 4), _min, _max);
    }

    void OnDrag(DragEvent e)
    {
        if (Disabled) return;
        Value = _min + e.FractionX * (_max - _min);
        if (e.Phase == DragPhase.End) Emit(new ValueCommitted(_value));
    }

    bool OnKey(KeyEvent e)
    {
        if (Disabled) return false;
        float step = e.Shift ? LargeStep : _step;
        float target = e.Key switch
        {
            Key.Left or Key.Down => _value - step,
            Key.Right or Key.Up => _value + step,
            Key.PageDown => _value - LargeStep,
            Key.PageUp => _value + LargeStep,
            Key.Home => _min,
            Key.End => _max,
            _ => float.NaN,
        };
        if (float.IsNaN(target)) return false;
        float before = _value;
        Value = target;
        if (_value != before) Emit(new ValueCommitted(_value));
        return true; // ở mép vẫn nuốt phím để mũi tên không cuộn trang hay chuyển sang control khác
    }

    const string Thumb = "absolute top-[2px] size-3 -translate-x-1/2 rounded-full border border-ring bg-white ring-ring/50 transition-shadow hover:ring-3 active:ring-3";
    const string ThumbFocused = Thumb + " ring-3";

    string? _key;

    public override Div Render(Window w)
    {
        _key ??= "gotx-slider-" + Id.Value;
        float f = Fraction;
        // Thanh cao 16px để dễ nhấn trúng; đường ray 4px nằm giữa, con chạy tuyệt đối theo tỉ lệ. Cả thanh là phần tử
        // focus (nhấn vào đường ray cũng focus như Base UI), còn vòng focus vẽ ở con chạy nên class chọn theo trạng thái.
        return w.Div().Class("relative flex h-4 w-full items-center cursor-pointer disabled:opacity-50")
            .Key(_key).Focusable()
            .Disabled(Disabled)
            .OnDrag(this, static (s, e) => s.OnDrag(e))
            .OnKey(this, static (s, e) => s.OnKey(e))
            .Child(w.Div().Class("relative h-1 w-full overflow-hidden rounded-full bg-muted")
                .Child(w.Div().Class("h-full bg-primary").W(Length.Fraction(f))))
            .Child(w.Div().Class(w.FocusVisible && w.IsFocused(_key) ? ThumbFocused : Thumb).Left(Length.Fraction(f)));
    }
}
