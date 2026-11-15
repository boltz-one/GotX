namespace GotX.Components;

/// <summary>Single: mở một mục thì đóng mục khác. Multiple: các mục mở độc lập.</summary>
public enum AccordionType : byte { Single, Multiple }

/// <summary>Một mục của accordion: tiêu đề + nội dung dạng chữ (<paramref name="Text"/>) hoặc hàm dựng (<paramref name="Content"/>).</summary>
public sealed record AccordionItem(string Title, string? Text = null, Func<Window, Div>? Content = null);

/// <summary>
/// Danh sách mục co/giãn. Không có animation chiều cao (Core chưa nội suy được chiều cao auto) nên nội dung
/// hiện/ẩn ngay; chỉ mũi tên xoay có transition.
/// Bàn phím như Radix: mỗi tiêu đề là một điểm dừng Tab, Enter/Space mở/đóng, ↑/↓ đi giữa các tiêu đề (quay vòng), Home/End về đầu/cuối.
/// </summary>
public sealed class Accordion : View
{
    // Class theo style (ComponentStyleSheet.Accordion*); ghim lúc tạo nếu tạo trong ComponentStyles.Use(...).
    readonly ComponentStyleSheet? _pinned = ComponentStyles.Scoped;
    ComponentStyleSheet _sheet => _pinned ?? ComponentStyles.S;

    IReadOnlyList<AccordionItem> _items = [];
    bool[] _open = [];
    string[] _keys = [];
    Indexed<Accordion>[]? _refs;
    Window? _window;

    /// <summary>Phát cho từng mục đổi trạng thái (kể cả mục bị đóng tự động ở chế độ Single).</summary>
    public readonly record struct OpenChanged(int Index, bool Open);

    /// <summary>Chế độ mở một hay nhiều mục.</summary>
    public AccordionType Type { get; set; }

    /// <summary>Các mục; đổi danh sách thì giữ trạng thái mở theo chỉ số còn hợp lệ.</summary>
    public IReadOnlyList<AccordionItem> Items
    {
        get => _items;
        set { _items = value; Array.Resize(ref _open, value.Count); Notify(); }
    }

    /// <summary>Mục <paramref name="index"/> có đang mở không.</summary>
    public bool IsOpen(int index) => index >= 0 && index < _open.Length && _open[index];

    /// <summary>Mở/đóng một mục; ở chế độ Single, mở mục này sẽ đóng mục đang mở.</summary>
    public void SetOpen(int index, bool open)
    {
        if (index < 0 || index >= _open.Length) throw new ArgumentOutOfRangeException(nameof(index));
        if (_open[index] == open) return;
        if (open && Type == AccordionType.Single)
            for (int i = 0; i < _open.Length; i++)
                if (i != index && _open[i]) { _open[i] = false; Emit(new OpenChanged(i, false)); }
        _open[index] = open;
        Notify();
        Emit(new OpenChanged(index, open));
    }

    /// <summary>Đảo trạng thái một mục (hành vi khi bấm tiêu đề).</summary>
    public void Toggle(int index) => SetOpen(index, !IsOpen(index));

    /// <summary>Key của tiêu đề mục <paramref name="index"/> – để kiểm tra headless bấm/đo.</summary>
    public string TriggerKey(int index) => EnsureKeys()[index * 2];

    // Key ổn định cho tiêu đề và mũi tên: nội dung chèn vào giữa làm đổi thứ tự, transition xoay cần khoá riêng.
    string[] EnsureKeys()
    {
        if (_keys.Length == _items.Count * 2) return _keys;
        _keys = new string[_items.Count * 2];
        for (int i = 0; i < _items.Count; i++)
        {
            _keys[i * 2] = $"gotx-accordion-{Id.Value}-{i}";
            _keys[i * 2 + 1] = $"gotx-accordion-{Id.Value}-{i}-chevron";
        }
        return _keys;
    }

    // Phím nổi lên từ tiêu đề đang focus: đổi focus sang tiêu đề khác, không đổi trạng thái mở.
    bool KeyDown(KeyEvent e)
    {
        if (_window is null) return false;
        var keys = EnsureKeys();
        int at = -1;
        for (int i = 0; i < _items.Count && at < 0; i++) if (_window.IsFocused(keys[i * 2])) at = i;
        if (at < 0) return false; // focus đang ở trong nội dung một mục: để phím cho nội dung đó
        int next = RovingFocus.FromKey(e.Key, this, _items.Count, at, loop: true, vertical: true, static (_, _) => true);
        if (next == RovingFocus.NotNavigation) return false;
        if (next >= 0 && next != at) _window.FocusKey(keys[next * 2]);
        return true;
    }

    /// <summary>Vẽ các mục; tiêu đề bấm được, nội dung chỉ có khi mục mở.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        var keys = EnsureKeys();
        var refs = Indexed<Accordion>.Ensure(ref _refs, this, _items.Count);
        var s = _sheet;
        var root = w.Div().Class("flex w-full flex-col").OnKey(this, static (a, e) => a.KeyDown(e));
        for (int i = 0; i < _items.Count; i++)
        {
            var it = _items[i];
            bool open = _open[i];
            var item = w.Div();
            if (i < _items.Count - 1) item.Class(s.AccordionItemDivider);
            item.Child(w.Div().Key(keys[i * 2]).Class(s.AccordionTrigger).Focusable()
                .OnClick(refs[i], static (r, _) => r.Owner.Toggle(r.Index))
                .Child(w.Div().Class("flex-1").Child(it.Title))
                .Child(w.Div().Key(keys[i * 2 + 1]).Class(s.AccordionChevron[open ? 1 : 0]).Child(Icon.ChevronDown(w))));
            if (open)
            {
                var body = w.Div().Class(s.AccordionContent);
                if (it.Content is { } build) body.Child(build(w));
                else if (it.Text is { } text) body.Child(text);
                item.Child(body);
            }
            root.Child(item);
        }
        return root;
    }
}
