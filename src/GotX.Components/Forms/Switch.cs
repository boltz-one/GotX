namespace GotX.Components;

/// <summary>Cỡ của <see cref="Switch"/>.</summary>
public enum SwitchSize : byte { Default, Sm }

/// <summary>
/// Công tắc bật/tắt (switch.tsx). Bấm vào công tắc hoặc nhãn để đổi; con chạy trượt bằng transition-transform.
/// </summary>
public sealed class Switch : View
{
    bool _checked;

    /// <summary>Trạng thái bật/tắt vừa đổi.</summary>
    public readonly record struct CheckedChanged(bool Checked);

    const string TrackBase = "relative inline-flex shrink-0 items-center rounded-full border transition-all ";
    const string TrackIdle = "border-transparent ", TrackFocus = "border-ring ring-3 ring-ring/50 ";

    // [(cỡ * 2 + bật) * 2 + đang focus bằng phím]: chọn sẵn chuỗi, không nối class mỗi frame. Cả hàng (công tắc + nhãn)
    // là phần tử focus, vòng focus chỉ vẽ quanh công tắc như shadcn.
    static readonly string[] Tracks = BuildTracks();

    static string[] BuildTracks()
    {
        string[] sizes = ["h-[18.4px] w-8 ", "h-[14px] w-6 "];
        string[] states = ["bg-input dark:bg-input/80", "bg-primary"];
        var table = new string[8];
        for (int i = 0; i < 8; i++)
            table[i] = TrackBase + ((i & 1) != 0 ? TrackFocus : TrackIdle) + sizes[i >> 2] + states[(i >> 1) & 1];
        return table;
    }

    // Con chạy dịch đúng (rộng track − rộng con chạy − 2px viền) như translate-x-[calc(100%-2px)] của shadcn.
    static readonly string[] Thumbs =
    [
        "block size-4 rounded-full bg-background transition-transform translate-x-0 dark:bg-foreground",
        "block size-4 rounded-full bg-background transition-transform translate-x-[14px] dark:bg-primary-foreground",
        "block size-3 rounded-full bg-background transition-transform translate-x-0 dark:bg-foreground",
        "block size-3 rounded-full bg-background transition-transform translate-x-[10px] dark:bg-primary-foreground",
    ];

    /// <summary>Đang bật.</summary>
    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            Notify();
            Emit(new CheckedChanged(value));
        }
    }

    /// <summary>Vô hiệu hoá: mờ đi và bỏ qua click.</summary>
    public bool Disabled { get; set; }

    /// <summary>Cỡ công tắc.</summary>
    public SwitchSize Size { get; set; }

    /// <summary>Nhãn bên phải; null = chỉ có công tắc.</summary>
    public string? Label { get; set; }

    /// <summary>Đảo trạng thái (không làm gì khi bị vô hiệu hoá).</summary>
    public void Toggle() { if (!Disabled) Checked = !Checked; }

    const string Row = "inline-flex items-center gap-2 cursor-pointer disabled:opacity-50";
    // Cỡ sm chỉ cao 14px: đệm 1px trên dưới bù bằng margin âm để vùng bấm đủ 16px mà bố cục không đổi.
    const string RowSm = Row + " -my-px py-px";
    string? _key;

    public override Div Render(Window w)
    {
        _key ??= "gotx-switch-" + Id.Value;
        int i = (int)Size * 2 + (_checked ? 1 : 0);
        bool ring = w.FocusVisible && w.IsFocused(_key);
        // Space/Enter đổi trạng thái: Core gọi OnClick của phần tử đang focus.
        var row = w.Div().Class(Size == SwitchSize.Sm ? RowSm : Row)
            .Key(_key).Focusable()
            .Disabled(Disabled)
            .OnClick(this, static (s, _) => s.Toggle())
            .Child(w.Div().Class(Tracks[i * 2 + (ring ? 1 : 0)]).Child(w.Div().Class(Thumbs[i])));
        if (Label is { } label) row.Child(w.Div().Class("text-sm font-medium leading-none").Child(label));
        return row;
    }
}
