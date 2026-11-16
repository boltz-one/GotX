namespace GotX.Components;

/// <summary>
/// Băng chuyền: một slide chiếm trọn khung, nút tròn hai bên để lùi/tiến, chữ "i / n" bên dưới. Đổi slide bằng cách
/// dịch cả băng (translate-x theo phần trăm) có transition, như Embla. Không có vuốt/kéo, không lặp vòng, chỉ hướng ngang.
/// Nút nằm ngoài khung (-left-12 / -right-12) nên chỗ đặt carousel cần chừa lề ngang ~3rem.
/// Hai nút focus được; khi focus ở một trong hai nút, ←/→ lùi/tiến một slide (như onKeyDownCapture của bản gốc).
/// </summary>
public sealed class Carousel : View
{
    static readonly string NavClass = Ui.ButtonClasses(ButtonVariant.Outline, ButtonSize.IconSm) + " rounded-full " + FocusRing.Ring;
    Window? _window;

    int _index, _count;
    string[] _labels = [];
    string? _prevKey, _nextKey, _trackKey;

    /// <summary>Phát khi slide hiện tại đổi.</summary>
    public readonly record struct IndexChanged(int Index);

    /// <summary>Số slide.</summary>
    public int Count
    {
        get => _count;
        set { _count = Math.Max(0, value); if (_index >= _count) _index = Math.Max(0, _count - 1); Notify(); }
    }

    /// <summary>Hàm dựng slide thứ i.</summary>
    public Func<Window, int, Div>? Slide { get; set; }

    /// <summary>Hiện chữ "i / n" dưới khung.</summary>
    public bool ShowIndex { get; set; } = true;

    /// <summary>Slide hiện tại, kẹp trong [0, Count-1].</summary>
    public int Index
    {
        get => _index;
        set
        {
            value = Math.Clamp(value, 0, Math.Max(0, _count - 1));
            if (_index == value) return;
            _index = value;
            Notify();
            Emit(new IndexChanged(value));
        }
    }

    /// <summary>Còn slide phía trước không.</summary>
    public bool CanPrevious => _index > 0;

    /// <summary>Còn slide phía sau không.</summary>
    public bool CanNext => _index < _count - 1;

    bool KeyDown(KeyEvent e)
    {
        if (e.Key is not (Key.Left or Key.Right)) return false;
        if (e.Key == Key.Left) Previous(); else Next();
        // Tới đầu/cuối thì nút đang focus bị vô hiệu và Core bỏ focus của nó: chuyển sang nút còn lại để còn bấm phím tiếp.
        if (_window is { } w)
        {
            if (!CanNext && w.IsFocused(NextKey)) w.FocusKey(PreviousKey);
            else if (!CanPrevious && w.IsFocused(PreviousKey)) w.FocusKey(NextKey);
        }
        return true;
    }

    /// <summary>Lùi một slide.</summary>
    public void Previous() => Index = _index - 1;

    /// <summary>Tiến một slide.</summary>
    public void Next() => Index = _index + 1;

    /// <summary>Key nút lùi (kiểm tra headless).</summary>
    public string PreviousKey => _prevKey ??= "gotx-carousel-" + Id.Value + "-prev";

    /// <summary>Key nút tiến (kiểm tra headless).</summary>
    public string NextKey => _nextKey ??= "gotx-carousel-" + Id.Value + "-next";

    // Key cho băng slide: transition translate cần khoá ổn định.
    string TrackKey => _trackKey ??= "gotx-carousel-" + Id.Value + "-track";

    /// <summary>Vẽ khung cắt chứa băng slide, hai nút điều hướng và chỉ số.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        // Băng rộng bằng khung; dịch -100% × index đưa slide index vào khung (mỗi slide basis-full).
        var track = w.Div().Key(TrackKey).Class("flex transition-transform duration-300 ease-out");
        track.Style.TranslateX = Length.Fraction(-_index);
        for (int i = 0; i < _count; i++)
        {
            var slide = w.Div().Class("min-w-0 shrink-0 grow-0 basis-full");
            if (Slide is { } build) slide.Child(build(w, i));
            track.Child(slide);
        }

        // Nút đặt trong cột absolute cao bằng khung + items-center: tránh translate để vùng bấm trùng chỗ vẽ.
        var root = w.Div().Class("relative").OnKey(this, static (c, e) => c.KeyDown(e))
            .Child(w.Div().Class("overflow-hidden").Child(track))
            .Child(w.Div().Class("absolute inset-y-0 -left-12 flex items-center")
                .Child(w.Div().Key(PreviousKey).Class(NavClass).Disabled(!CanPrevious).Focusable().Role(SemanticRole.Button).AriaLabel("Previous slide")
                    .OnClick(this, static (c, _) => c.Previous()).Child(Icon.ChevronLeft(w))))
            .Child(w.Div().Class("absolute inset-y-0 -right-12 flex items-center")
                .Child(w.Div().Key(NextKey).Class(NavClass).Disabled(!CanNext).Focusable().Role(SemanticRole.Button).AriaLabel("Next slide")
                    .OnClick(this, static (c, _) => c.Next()).Child(Icon.ChevronRight(w))));

        if (!ShowIndex || _count == 0) return root;
        if (_labels.Length != _count)
        {
            _labels = new string[_count];
            for (int i = 0; i < _count; i++) _labels[i] = $"{i + 1} / {_count}";
        }
        return w.Div().Class("flex flex-col gap-2")
            .Child(root)
            .Child(w.Div().Class("text-center text-sm text-muted-foreground").Child(_labels[_index]));
    }
}
