namespace GotX.Components;

/// <summary>Default: thanh nền muted, tab đang chọn nổi lên như thẻ. Line: không nền, gạch chân tab đang chọn.</summary>
public enum TabsVariant : byte { Default, Line }

/// <summary>Horizontal: thanh tab nằm trên nội dung. Vertical: cột tab bên trái nội dung.</summary>
public enum TabsOrientation : byte { Horizontal, Vertical }

/// <summary>
/// Thẻ tab: danh sách nút + nội dung của tab đang chọn.
/// Bàn phím như Radix: cả thanh tab là một điểm dừng Tab (focus vào tab đang chọn); ←/→ (hướng dọc: ↑/↓) chuyển focus
/// và chọn luôn tab đó, quay vòng ở hai đầu; Home/End về tab đầu/cuối.
/// </summary>
public sealed class Tabs : View
{
    const string TriggerBase =
        "relative inline-flex grow shrink-0 items-center gap-1.5 rounded-md border border-transparent " +
        "px-2 py-0.5 text-sm font-medium whitespace-nowrap cursor-pointer transition-all " +
        "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 ";
    const string Inactive = "text-foreground/60 hover:text-foreground dark:text-muted-foreground dark:hover:text-foreground";
    const string ActiveCard = "bg-background text-foreground shadow-sm dark:border-input dark:bg-input/30";

    // [hướng * 4 + variant * 2 + đang chọn]: tra bảng thay vì nối chuỗi mỗi frame.
    static readonly string[] TriggerClasses = BuildTriggerClasses();

    static string[] BuildTriggerClasses()
    {
        string[] shape = ["h-full justify-center ", "justify-start "];
        var table = new string[8];
        for (int o = 0; o < 2; o++)
        {
            table[o * 4] = TriggerBase + shape[o] + Inactive;
            table[o * 4 + 1] = TriggerBase + shape[o] + ActiveCard;
            table[o * 4 + 2] = TriggerBase + shape[o] + Inactive;
            table[o * 4 + 3] = TriggerBase + shape[o] + "text-foreground";
        }
        return table;
    }

    // [hướng * 2 + variant]
    static readonly string[] ListClasses =
    [
        "inline-flex h-8 items-center justify-center rounded-lg p-[3px] text-muted-foreground bg-muted",
        "inline-flex h-8 items-center justify-center gap-1 p-[3px] text-muted-foreground",
        "inline-flex shrink-0 flex-col items-stretch rounded-lg p-[3px] text-muted-foreground bg-muted",
        "inline-flex shrink-0 flex-col items-stretch gap-1 p-[3px] text-muted-foreground",
    ];

    // Line: gạch nằm sát mép thanh (đệm 3px + viền 1px của nút → -5px như shadcn); hướng dọc thì gạch ở mép phải.
    static readonly string[] IndicatorClasses =
        ["absolute inset-x-0 -bottom-[5px] h-0.5 bg-foreground", "absolute inset-y-0 -right-1 w-0.5 bg-foreground"];

    IReadOnlyList<string> _items = [];
    int _index;
    string[] _keys = [];
    Indexed<Tabs>[]? _refs;
    Window? _window;

    /// <summary>Phát khi đổi tab.</summary>
    public readonly record struct ValueChanged(int Index, string Value);

    /// <summary>Nhãn các tab.</summary>
    public IReadOnlyList<string> Items
    {
        get => _items;
        set { _items = value; if (_index >= value.Count) _index = 0; Notify(); }
    }

    /// <summary>Kiểu trình bày danh sách tab.</summary>
    public TabsVariant Variant { get; set; }

    /// <summary>Hướng của danh sách tab; quyết định cả phím mũi tên dùng để di chuyển.</summary>
    public TabsOrientation Orientation { get; set; }

    /// <summary>Nội dung theo chỉ số tab đang chọn; null = chỉ có thanh tab.</summary>
    public Func<Window, int, Div>? Content { get; set; }

    /// <summary>Chỉ số tab đang chọn.</summary>
    public int SelectedIndex
    {
        get => _index;
        set
        {
            if (value < 0 || value >= _items.Count) throw new ArgumentOutOfRangeException(nameof(value));
            if (_index == value) return;
            _index = value;
            Notify();
            Emit(new ValueChanged(value, _items[value]));
        }
    }

    /// <summary>Key của nút tab <paramref name="index"/> (kiểm tra headless, <c>window.IsFocused</c>).</summary>
    public string TriggerKey(int index) => EnsureKeys()[index];

    /// <summary>Key của vùng nội dung (kiểm tra headless).</summary>
    public string ContentKey => EnsureKeys()[_items.Count];

    string[] EnsureKeys()
    {
        if (_keys.Length == _items.Count + 1) return _keys;
        _keys = new string[_items.Count + 1];
        for (int i = 0; i < _items.Count; i++) _keys[i] = $"gotx-tabs-{Id.Value}-{i}";
        _keys[_items.Count] = $"gotx-tabs-{Id.Value}-content";
        return _keys;
    }

    bool KeyDown(KeyEvent e)
    {
        if (_window is null || _items.Count == 0) return false;
        var keys = EnsureKeys();
        if (e.Key == Key.Tab) return RovingFocus.TabOut(_window, e.Shift, keys[0], keys[_items.Count - 1]);
        int at = RovingFocus.FocusedIndex(_window, keys, _items.Count);
        int next = RovingFocus.FromKey(e.Key, this, _items.Count, at, loop: true, Orientation == TabsOrientation.Vertical, static (_, _) => true);
        if (next == RovingFocus.NotNavigation) return false;
        if (next >= 0) { _window.FocusKey(keys[next]); SelectedIndex = next; }
        return true;
    }

    /// <summary>Vẽ thanh tab (co theo nội dung) và nội dung giãn hết phần còn lại.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        var keys = EnsureKeys();
        var refs = Indexed<Tabs>.Ensure(ref _refs, this, _items.Count);
        // Tab từ ngoài vào rơi xuống nút đầu/cuối theo thứ tự vẽ; chuyển ngay về tab đang chọn để cả thanh hành xử như
        // một điểm dừng (roving tabindex). Chỉ làm với focus bàn phím: bấm chuột vào tab khác sẽ tự chọn tab đó.
        if (w.FocusVisible && RovingFocus.FocusedIndex(w, keys, _items.Count) is >= 0 and var focused && focused != _index)
            w.FocusKey(keys[_index]);

        int o = (int)Orientation, v = (int)Variant;
        var list = w.Div().Class(ListClasses[o * 2 + v]).Role(SemanticRole.TabList).OnKey(this, static (t, e) => t.KeyDown(e));
        for (int i = 0; i < _items.Count; i++)
        {
            bool active = i == _index;
            var trigger = w.Div().Key(keys[i]).Class(TriggerClasses[o * 4 + v * 2 + (active ? 1 : 0)]).Focusable().Role(SemanticRole.Tab).AriaState(SemanticState.Selected, active)
                .OnClick(refs[i], static (r, _) => r.Owner.SelectedIndex = r.Index)
                .Child(_items[i]);
            if (active && Variant == TabsVariant.Line) trigger.Child(w.Div().Class(IndicatorClasses[o]));
            list.Child(trigger);
        }
        var root = w.Div().Class(o == 0 ? "flex flex-col items-start gap-2" : "flex items-start gap-2").Child(list);
        if (Content is { } build && _index < _items.Count)
            root.Child(w.Div().Key(keys[_items.Count]).Role(SemanticRole.TabPanel).AriaLabel(_items[_index]).Class(o == 0 ? "self-stretch text-sm" : "min-w-0 flex-1 text-sm").Child(build(w, _index)));
        return root;
    }
}
