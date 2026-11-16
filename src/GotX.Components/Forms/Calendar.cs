namespace GotX.Components;

/// <summary>
/// Lịch tháng chọn một ngày (calendar.tsx, react-day-picker mode="single"). Lưới luôn 6 tuần × 7 ngày, tuần bắt đầu
/// thứ Hai; ngày của tháng bên cạnh hiện mờ và bấm vào thì chọn luôn và chuyển sang tháng đó. Bàn phím như
/// react-day-picker: lưới ngày là một điểm dừng Tab (ngày focus lần trước → ngày chọn → hôm nay → mồng 1), ←/→ lùi/tiến
/// một ngày, ↑/↓ một tuần, Home/End về đầu/cuối tuần, PageUp/PageDown đổi tháng (thêm Shift: đổi năm), Enter/Space chọn;
/// đi quá mép tháng thì lịch tự lật trang. Không có chọn khoảng, nhiều tháng hay dropdown tháng/năm.
/// </summary>
public sealed class Calendar : View
{
    const int Cells = 42;
    static readonly string[] DayNames = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"];
    static readonly string[] DayLabels = Enumerable.Range(0, 32).Select(static d => d.ToString()).ToArray();

    // Ô ngày: chọn > hôm nay > ngoài tháng > thường. Rộng lưới cố định để mỗi cột đúng 32px như --cell-size.
    const string DayBase = "flex size-8 items-center justify-center rounded-lg text-sm cursor-pointer transition-colors " +
        "focus-visible:ring-3 focus-visible:ring-ring/50 ";
    const string DaySelected = DayBase + "bg-primary text-primary-foreground";
    const string DayToday = DayBase + "bg-muted text-foreground hover:bg-muted";
    const string DayOutside = DayBase + "text-muted-foreground hover:bg-muted";
    const string DayNormal = DayBase + "hover:bg-muted hover:text-foreground";

    DateOnly _month;
    DateOnly? _selected;
    string _caption = "";
    Indexed<Calendar>[]? _refs;
    RovingFocus? _roving;
    Window? _window;
    DateOnly? _focusDay; // ngày giữ focus lần gần nhất: nơi Tab quay lại
    string? _prevKey, _nextKey;

    /// <summary>Ngày được chọn vừa đổi.</summary>
    public readonly record struct SelectedChanged(DateOnly Date);

    /// <summary>Tháng đang hiển thị vừa đổi; <see cref="Month"/> là ngày 1 của tháng.</summary>
    public readonly record struct MonthChanged(DateOnly Month);

    /// <summary>Tạo lịch hiển thị tháng của hôm nay.</summary>
    public Calendar() => SetMonth(DateOnly.FromDateTime(DateTime.Today), emit: false);

    /// <summary>Ngày được tô là "hôm nay" (mặc định theo đồng hồ máy; đặt lại được để hiển thị cố định).</summary>
    public DateOnly Today { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Tháng đang hiển thị (luôn là ngày 1); gán ngày bất kỳ sẽ lấy tháng chứa ngày đó.</summary>
    public DateOnly Month { get => _month; set => SetMonth(value, emit: true); }

    /// <summary>Ngày đang chọn, null = chưa chọn.</summary>
    public DateOnly? Selected
    {
        get => _selected;
        set
        {
            if (_selected == value) return;
            _selected = value;
            Notify();
            if (value is { } d) Emit(new SelectedChanged(d));
        }
    }

    /// <summary>Sang tháng sau.</summary>
    public void NextMonth() => SetMonth(_month.AddMonths(1), emit: true);

    /// <summary>Về tháng trước.</summary>
    public void PreviousMonth() => SetMonth(_month.AddMonths(-1), emit: true);

    void SetMonth(DateOnly day, bool emit)
    {
        var first = new DateOnly(day.Year, day.Month, 1);
        if (first == _month) return;
        _month = first;
        _caption = $"Tháng {first.Month} {first.Year}";
        Notify();
        if (emit) Emit(new MonthChanged(first));
    }

    /// <summary>Ngày ở ô đầu lưới: thứ Hai của tuần chứa ngày 1.</summary>
    DateOnly GridStart => _month.AddDays(-(((int)_month.DayOfWeek + 6) % 7));

    void PickCell(int index)
    {
        var date = GridStart.AddDays(index);
        Selected = date;
        if (date.Month == _month.Month) return;
        // Lật sang tháng của ngày vừa chọn: ô đang focus giờ là ngày khác, nên focus đi theo ngày – chọn bằng chuột thì
        // focus "im lặng", không hiện vòng focus.
        FocusDay(date, visible: _window is { FocusVisible: true });
    }

    /// <summary>Đưa focus tới <paramref name="date"/>, lật tháng nếu ngày đó không thuộc tháng đang hiện.</summary>
    void FocusDay(DateOnly date, bool visible = true)
    {
        if (date.Year != _month.Year || date.Month != _month.Month) SetMonth(date, emit: true);
        _focusDay = date;
        // 42 ô luôn có mặt với cùng khoá nên focus được ngay, trước cả khi lưới vẽ lại theo tháng mới.
        if (_window is { } w) _roving?.Focus(w, date.DayNumber - GridStart.DayNumber, visible);
    }

    bool OnCellKey(int index, KeyEvent e)
    {
        if (_window is not { } w || _roving is not { } roving) return false;
        if (e.Key == Key.Tab) { roving.LeaveOnTab(w, Cells, e.Shift); return false; }
        var date = GridStart.AddDays(index);
        int weekday = ((int)date.DayOfWeek + 6) % 7; // thứ Hai = 0
        DateOnly? target = e.Key switch
        {
            Key.Left => date.AddDays(-1),
            Key.Right => date.AddDays(1),
            Key.Up => date.AddDays(-7),
            Key.Down => date.AddDays(7),
            Key.Home => date.AddDays(-weekday),
            Key.End => date.AddDays(6 - weekday),
            Key.PageUp => e.Shift ? date.AddYears(-1) : date.AddMonths(-1),
            Key.PageDown => e.Shift ? date.AddYears(1) : date.AddMonths(1),
            _ => null, // Enter/Space: Core gọi OnClick của ô đang focus
        };
        if (target is not { } t) return false;
        FocusDay(t);
        return true;
    }

    /// <summary>Ô nhận focus khi Tab đi vào lưới.</summary>
    int HomeCell(DateOnly start)
    {
        bool InMonth(DateOnly d) => d.Year == _month.Year && d.Month == _month.Month;
        var home = _focusDay is { } f && InMonth(f) ? f : _selected is { } s && InMonth(s) ? s : InMonth(Today) ? Today : _month;
        return home.DayNumber - start.DayNumber;
    }

    public override Div Render(Window w)
    {
        _window = w;
        var roving = _roving ??= new RovingFocus("gotx-calendar-" + Id.Value);
        _prevKey ??= "gotx-calendar-" + Id.Value + "-prev";
        _nextKey ??= "gotx-calendar-" + Id.Value + "-next";
        var refs = Indexed<Calendar>.Ensure(ref _refs, this, Cells);
        var header = w.Div().Class("flex h-8 items-center justify-between")
            .Child(Ui.Button(w, ButtonVariant.Ghost, ButtonSize.Icon).Key(_prevKey).AriaLabel("Previous month").OnClick(this, static (c, _) => c.PreviousMonth()).Child(Icon.ChevronLeft(w)))
            .Child(w.Div().Class("text-sm font-medium").Child(_caption))
            .Child(Ui.Button(w, ButtonVariant.Ghost, ButtonSize.Icon).Key(_nextKey).AriaLabel("Next month").OnClick(this, static (c, _) => c.NextMonth()).Child(Icon.ChevronRight(w)));

        var names = w.Div().Class("grid grid-cols-7");
        foreach (var name in DayNames)
            names.Child(w.Div().Class("flex h-8 items-center justify-center text-[0.8rem] text-muted-foreground").Child(name));

        var grid = w.Div().Class("grid grid-cols-7");
        var start = GridStart;
        if (roving.Sync(w, Cells, HomeCell(start)) is >= 0 and var at) _focusDay = start.AddDays(at);
        for (int i = 0; i < Cells; i++)
        {
            var d = start.AddDays(i);
            string cls = d == _selected ? DaySelected : d == Today ? DayToday : d.Month != _month.Month ? DayOutside : DayNormal;
            grid.Child(w.Div().Class(cls).Key(roving.Key(i)).Focusable()
                .OnKey(refs[i], static (r, e) => r.Owner.OnCellKey(r.Index, e))
                .OnClick(refs[i], static (r, _) => r.Owner.PickCell(r.Index)).Child(DayLabels[d.Day]));
        }

        return w.Div().Class("flex w-[248px] shrink-0 flex-col gap-2 p-3 bg-background").Child(header).Child(names).Child(grid);
    }
}
