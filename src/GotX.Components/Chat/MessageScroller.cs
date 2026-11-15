namespace GotX.Components;

/// <summary>
/// Danh sách tin nhắn cuộn dọc, bám đáy: mở ra ở tin mới nhất, tin thêm vào khi đang ở đáy thì tự cuộn theo, cuộn
/// lên xem tin cũ thì hiện nút "về tin mới nhất". Dùng <c>Window.ScrollToEnd</c> để cuộn và <c>OnScroll</c> để biết
/// khung nhìn đang ở đâu. Chiều cao đặt bằng <see cref="Class"/> (ví dụ <c>"h-80"</c>).
/// </summary>
public sealed class MessageScroller : View
{
    /// <summary>Khung nhìn vừa tới đáy (true) hoặc rời đáy (false) – lúc nút "về tin mới nhất" ẩn/hiện.</summary>
    public readonly record struct AtEndChanged(bool AtEnd);

    const float Threshold = 24; // cách đáy chừng này vẫn coi là ở đáy, như ngưỡng của bản gốc

    readonly List<Func<Window, Div>> _items = new();
    string? _key, _endAnchor;
    bool _atEnd = true;
    // Đang cuộn về đáy theo yêu cầu: trong lúc đó chưa tới đáy không có nghĩa là người dùng đã rời đáy.
    bool _following = true;
    bool _jumpPending = true, _jumpAnimate; // lần render kế sẽ gọi ScrollToEnd (cần Window, chỉ có trong Render)
    float _lastOffset;

    /// <summary>Class thêm cho khung ngoài – thường là chiều cao.</summary>
    public string Class { get; set; } = "h-80";

    /// <summary>Đang ở (gần) đáy danh sách.</summary>
    public bool IsAtEnd => _atEnd;

    /// <summary>Các tin theo thứ tự hiển thị (mỗi tin là hàm dựng phần tử).</summary>
    public IReadOnlyList<Func<Window, Div>> Items => _items;

    /// <summary>Thêm một tin vào cuối. Nếu đang ở đáy thì cuộn theo tin mới.</summary>
    public void Append(Func<Window, Div> item)
    {
        _items.Add(item);
        if (_atEnd || _following) Follow(animate: true);
        Notify();
    }

    /// <summary>Thay toàn bộ danh sách và nhảy về tin mới nhất.</summary>
    public void SetItems(IEnumerable<Func<Window, Div>> items)
    {
        _items.Clear();
        _items.AddRange(items);
        Follow(animate: false);
        Notify();
    }

    /// <summary>Cuộn mượt về tin mới nhất (nút "về tin mới nhất" gọi hàm này).</summary>
    public void ScrollToEnd()
    {
        Follow(animate: true);
        SetAtEnd(true);
        Notify();
    }

    void Follow(bool animate)
    {
        _following = true;
        _jumpPending = true;
        _jumpAnimate = animate;
    }

    void OnScrolled(ScrollInfo info)
    {
        bool atEnd = info.RemainingY <= Threshold;
        float offset = info.Offset.Y, previous = _lastOffset;
        _lastOffset = offset;
        if (_following)
        {
            if (atEnd) _following = false;
            else if (offset >= previous - 1) return; // vẫn đang trên đường về đáy
            else _following = false;                 // vị trí lùi lại: người dùng đã lăn ngược lên
        }
        SetAtEnd(atEnd);
    }

    void SetAtEnd(bool atEnd)
    {
        if (_atEnd == atEnd) return;
        _atEnd = atEnd;
        Notify();
        Emit(new AtEndChanged(atEnd));
    }

    /// <summary>Vẽ khung nhìn cuộn, mốc cuối nội dung và nút về tin mới nhất khi đã rời đáy.</summary>
    public override Div Render(Window w)
    {
        _key ??= "gotx-message-scroller-" + Id.Value;
        _endAnchor ??= _key + "-end";
        if (_jumpPending)
        {
            _jumpPending = false;
            w.ScrollToEnd(_key, _jumpAnimate);
        }

        var content = w.Div().Class("flex min-h-full flex-col gap-6 p-4");
        foreach (var item in _items)
            content.Child(w.Div().Class("min-w-0 shrink-0").Child(item(w)));
        content.Child(w.Div().Class("h-px shrink-0").Anchor(_endAnchor));

        var viewport = w.Div().Class("size-full min-h-0 min-w-0 overflow-y-auto")
            .Key(_key).Anchor(_key)
            .OnScroll(this, static (s, info) => s.OnScrolled(info))
            .Child(content);
        var root = w.Div().Class("relative flex min-h-0 flex-col overflow-hidden").Class(Class).Child(viewport);
        // Căn giữa bằng một hàng absolute full-width thay cho left-1/2 -translate-x-1/2: hộp layout trùng chỗ vẽ.
        if (!_atEnd)
            root.Child(w.Div().Class("absolute inset-x-0 bottom-4 flex justify-center")
                .Child(Ui.Button(w, ButtonVariant.Secondary, ButtonSize.IconSm)
                    .Class("rounded-full border-border bg-background text-foreground shadow-sm hover:bg-muted dark:bg-input/30 dark:hover:bg-input/50")
                    .Key(_key + "-jump")
                    .OnClick(this, static (s, _) => s.ScrollToEnd())
                    .Child(Icon.ArrowDown(w))));
        return root;
    }
}
