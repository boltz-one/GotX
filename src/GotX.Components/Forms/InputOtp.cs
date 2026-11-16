using GotX.Widgets;

namespace GotX.Components;

/// <summary>
/// Ô nhập mã OTP gồm <see cref="Length"/> ô một ký tự (input-otp.tsx). Phím chỉ tới <see cref="TextInput"/> đang focus,
/// nên giống thư viện input-otp gốc: một TextInput trong suốt phủ lên các ô nhận phím và chuột, các ô chỉ vẽ lại
/// chuỗi của nó. Nhờ vậy gõ, xoá, dán, IME đều dùng lại của Core. Chỉ nhận chữ số; ký tự khác bị lọc bỏ.
/// </summary>
public sealed class InputOtp : View
{
    TextInput _input = null!;
    string _value = "";
    bool _syncing;

    /// <summary>Mã vừa đổi.</summary>
    public readonly record struct ValueChanged(string Value);

    /// <summary>Đã điền đủ <see cref="Length"/> ô.</summary>
    public readonly record struct Completed(string Value);

    const string SlotBase = "relative flex size-8 items-center justify-center border-input text-sm transition-all dark:bg-input/30 ";

    // [vị trí trong nhóm × đang active]: viền trái chỉ ở ô đầu nhóm để các ô dính nhau không bị viền đôi.
    static readonly string[] SlotClasses =
    [
        SlotBase + "border rounded-lg", SlotBase + "border rounded-lg border-ring ring-3 ring-ring/50",
        SlotBase + "border rounded-l-lg", SlotBase + "border rounded-l-lg border-ring ring-3 ring-ring/50",
        SlotBase + "border-y border-r", SlotBase + "border-y border-r border-ring ring-3 ring-ring/50",
        SlotBase + "border-y border-r rounded-r-lg", SlotBase + "border-y border-r rounded-r-lg border-ring ring-3 ring-ring/50",
    ];

    static readonly string[] Digits = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"];

    /// <summary>Số ô (mặc định 6).</summary>
    public int Length { get; set; } = 6;

    /// <summary>Số ô mỗi nhóm; giữa các nhóm có dấu gạch (InputOTPSeparator). 0 = một nhóm.</summary>
    public int GroupSize { get; set; }

    /// <summary>Vô hiệu hoá: mờ đi, không nhận focus.</summary>
    public bool Disabled { get; set; }

    /// <summary>Mã hiện tại (chỉ chữ số, tối đa <see cref="Length"/> ký tự).</summary>
    public string Value
    {
        get => _value;
        set
        {
            string v = Clean(value);
            if (_input is not null && _input.Text != v) { _syncing = true; _input.Text = v; _syncing = false; }
            Apply(v);
        }
    }

    string Clean(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        Span<char> buf = stackalloc char[Math.Min(s.Length, Length)];
        int n = 0;
        foreach (char c in s) if (char.IsAsciiDigit(c) && n < buf.Length) buf[n++] = c;
        return new string(buf[..n]);
    }

    void Apply(string v)
    {
        if (v == _value) return;
        _value = v;
        Notify();
        Emit(new ValueChanged(v));
        if (v.Length == Length) Emit(new Completed(v));
    }

    protected override void OnCreated()
    {
        _input = App.New(new TextInput { Multiline = false, MinHeight = 0, Text = _value, BoxClass = "h-8 w-full cursor-text", AriaLabel = "One-time code" });
        App.Subscribe<TextInput.TextChanged>(_input, e =>
        {
            if (_syncing) return;
            string v = Clean(e.Text);
            // Ký tự bị lọc (chữ cái, quá độ dài): đưa ô ẩn về chuỗi sạch để con trỏ và nội dung khớp với các ô.
            if (v != e.Text) { _syncing = true; _input.Text = v; _syncing = false; }
            Apply(v);
        });
    }

    public override Div Render(Window w)
    {
        int length = Math.Max(1, Length), group = GroupSize > 0 ? GroupSize : length;
        int active = _input.IsFocused ? Math.Min(_value.Length, length - 1) : -1;
        var slots = w.Div().Class("flex items-center gap-2");
        Div row = default;
        for (int i = 0; i < length; i++)
        {
            int inGroup = i % group, groupCount = Math.Min(group, length - (i - inGroup));
            if (inGroup == 0)
            {
                if (i > 0) slots.Child(w.Div().Class("flex items-center text-muted-foreground").Child(Icon.Minus(w)));
                row = w.Div().Class("flex items-center");
                slots.Child(row);
            }
            int pos = ToggleStyles.Position(inGroup, groupCount);
            var slot = w.Div().Class(SlotClasses[pos * 2 + (i == active ? 1 : 0)]);
            if (i < _value.Length) slot.Child(Digits[_value[i] - '0']);
            else if (i == active) slot.Child(w.Div().Class("h-4 w-px bg-foreground animate-pulse"));
            row.Child(slot);
        }

        var root = w.Div().Class(Disabled ? "relative inline-flex items-center opacity-50" : "relative inline-flex items-center").Child(slots);
        // Ô nhập thật nằm trên cùng (absolute vẽ sau) và trong suốt: nhận click để focus, nhận phím để gõ.
        if (!Disabled) root.Child(w.Div().Class("absolute inset-0 opacity-0").Child(w.View(_input)));
        return root;
    }
}
