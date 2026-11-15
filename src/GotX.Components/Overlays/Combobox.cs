namespace GotX.Components;

/// <summary>
/// Hộp chọn có ô lọc: nút hiện giá trị đang chọn; bấm thì mở tấm neo dưới nút gồm ô gõ để lọc và danh sách khớp
/// (focus sẵn ô gõ). Tấm chính là một <see cref="Command"/>, nên lọc/nhóm/↑↓/Enter giống hệt bảng lệnh.
/// Nút focus được (Enter/Space/↑/↓ mở); chọn xong hoặc Escape thì focus trở về nút.
/// <code>
/// _fw = App.New(new Combobox { Items = ["Next.js", "SvelteKit", "Nuxt"], Placeholder = "Chọn framework…" });
/// App.Subscribe&lt;Combobox.ValueChanged&gt;(_fw, e =&gt; …);
/// </code>
/// </summary>
public sealed class Combobox : View
{
    readonly PopupSlot _slot = new();
    Command _command = null!;
    Window? _window;
    string? _anchor, _triggerKey;
    IReadOnlyList<string> _items = [];
    int _index = -1;

    /// <summary>Giá trị vừa đổi.</summary>
    public readonly record struct ValueChanged(int Index, string Value);

    public IReadOnlyList<string> Items
    {
        get => _items;
        set { _items = value; if (_index >= value.Count) _index = -1; Rebuild(); Notify(); }
    }

    public string Placeholder { get; set; } = "Chọn…";
    public string SearchPlaceholder { get; set; } = "Tìm…";
    public string EmptyText { get; set; } = "Không tìm thấy.";

    /// <summary>Class độ rộng của nút (tấm rộng ít nhất bằng nút).</summary>
    public string WidthClass { get; set; } = "w-56";

    public int SelectedIndex
    {
        get => _index;
        set
        {
            if (value < -1 || value >= _items.Count) throw new ArgumentOutOfRangeException(nameof(value));
            if (_index == value) return;
            _index = value;
            if (_command is not null) _command.CheckedText = Value;
            Notify();
            _command?.Notify();
            if (value >= 0) Emit(new ValueChanged(value, _items[value]));
        }
    }

    public string? Value => _index >= 0 ? _items[_index] : null;
    public bool IsOpen => _slot.IsOpen;

    /// <summary>Bảng lọc bên trong tấm – đọc chuỗi lọc, số dòng đang hiện.</summary>
    public Command Panel => _command;

    /// <summary>Overlay đang mở (null khi đóng).</summary>
    public Overlay? Overlay => _slot.Current;

    /// <summary>Tên anchor của nút (duy nhất theo instance).</summary>
    public string AnchorName => _anchor ??= "gotx-combobox-" + Id.Value;

    /// <summary>Key của nút – phần tử focus được, nhận lại focus khi tấm đóng.</summary>
    public string TriggerKey => _triggerKey ??= "gotx-combobox-trigger-" + Id.Value;

    /// <summary>Key của dòng thứ <paramref name="index"/> trong tấm – để tìm dòng trong kiểm tra headless.</summary>
    public string ItemKey(int index) => $"gotx-combobox-{Id.Value}-{index}";

    /// <summary>Tạo bảng lọc và nghe dòng được chọn.</summary>
    protected override void OnCreated()
    {
        _command = App.New(new Command
        {
            Placeholder = SearchPlaceholder, EmptyText = EmptyText, CheckedText = Value,
            // Ô gõ giãn hết chỗ được cho, nên tấm cần độ rộng cố định (bằng nút) thay vì tự co theo nội dung.
            PanelClass = "rounded-lg shadow-md ring-1 ring-foreground/10 " + WidthClass,
        });
        App.Subscribe<Command.ItemSelected>(_command, e => { SelectedIndex = e.Index; Close(); });
        Rebuild();
    }

    // Danh sách chuỗi → một nhóm không tiêu đề của Command; dựng lại chỉ khi Items đổi, không phải mỗi frame.
    void Rebuild()
    {
        if (_command is null) return;
        var rows = new CommandItem[_items.Count];
        for (int i = 0; i < rows.Length; i++) rows[i] = new CommandItem(_items[i], Key: ItemKey(i));
        _command.Groups = [new CommandGroup("", rows)];
    }

    /// <summary>Mở tấm chọn, xoá chuỗi lọc cũ và focus ô gõ.</summary>
    public void Open()
    {
        if (IsOpen || _window is null) return;
        _command.Reset();
        _slot.Open(_window, _command, new OverlayOptions { Anchor = AnchorName, Placement = Placement.BottomStart, MatchAnchorWidth = true },
            OnSlotChanged);
        _window.Focus(_command.Input);
    }

    /// <summary>Đóng tấm chọn, giữ nguyên giá trị.</summary>
    public void Close() => _slot.Close();

    void OnSlotChanged()
    {
        // Core trả focus về nút khi tấm đóng; nếu ô gõ (đã rời khỏi cây) vẫn còn giữ focus thì bỏ đi,
        // để phím gõ sau đó không lọt vào ô đã ẩn.
        if (!_slot.IsOpen && _window?.Focused == _command.Input) _window.Focus(null);
        Notify();
    }

    /// <summary>Vẽ nút hiện giá trị; tấm chọn được vẽ riêng trong lớp overlay.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        var button = w.Div().Class(
                "flex h-8 items-center justify-between gap-1.5 rounded-lg border border-input bg-transparent pl-2.5 pr-2 text-sm " +
                "whitespace-nowrap cursor-pointer transition-colors hover:bg-muted/50 dark:bg-input/30 dark:hover:bg-input/50 " + FocusRing.Ring)
            .Key(TriggerKey).Anchor(AnchorName).Focusable()
            .OnClick(this, static (c, _) => { if (c.IsOpen) c.Close(); else c.Open(); })
            // Enter/Space đi qua OnClick; ↑/↓ cũng mở như ô chọn thường.
            .OnKey(this, static (c, e) => { if (e.Key is not (Key.Up or Key.Down)) return false; c.Open(); return true; })
            .Child(w.Div().Class(_index >= 0 ? "truncate" : "truncate text-muted-foreground").Child(Value ?? Placeholder))
            .Child(w.Div().Class("text-muted-foreground").Child(Icon.ChevronDown(w)));
        return WidthClass.Length > 0 ? button.Class(WidthClass) : button;
    }
}
