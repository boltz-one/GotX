namespace GotX.Components;

/// <summary>Nút bật/tắt hai trạng thái (toggle.tsx), vd. nút In đậm trên thanh công cụ.</summary>
public sealed class Toggle : View
{
    // Bảng ghim lúc tạo nếu được tạo trong ComponentStyles.Use(...); không thì theo style chung của app.
    readonly ComponentStyleSheet? _pinned = ComponentStyles.Scoped;
    ComponentStyleSheet _sheet => _pinned ?? ComponentStyles.S;

    bool _pressed;

    /// <summary>Trạng thái nhấn vừa đổi.</summary>
    public readonly record struct PressedChanged(bool Pressed);

    /// <summary>Đang được nhấn (bật).</summary>
    public bool Pressed
    {
        get => _pressed;
        set
        {
            if (_pressed == value) return;
            _pressed = value;
            Notify();
            Emit(new PressedChanged(value));
        }
    }

    /// <summary>Chữ trên nút (sau <see cref="Content"/> nếu có cả hai).</summary>
    public string? Label { get; set; }

    /// <summary>Nội dung tuỳ ý (thường là icon) do người dùng dựng.</summary>
    public Func<Window, Div>? Content { get; set; }

    /// <summary>Kiểu nút.</summary>
    public ToggleVariant Variant { get; set; }

    /// <summary>Cỡ nút.</summary>
    public ToggleSize Size { get; set; }

    /// <summary>Vô hiệu hoá: mờ đi và bỏ qua click.</summary>
    public bool Disabled { get; set; }

    /// <summary>Đảo trạng thái (không làm gì khi bị vô hiệu hoá).</summary>
    public void Flip() { if (!Disabled) Pressed = !Pressed; }

    public override Div Render(Window w)
    {
        // Space/Enter đảo trạng thái: Core gọi OnClick của phần tử đang focus.
        var button = w.Div().Class(ToggleStyles.Get(_sheet, Variant, Size, _pressed, ToggleStyles.Solo))
            .Focusable()
            .Disabled(Disabled)
            .OnClick(this, static (t, _) => t.Flip());
        if (Content is { } content) button.Child(content(w));
        if (Label is { } label) button.Child(label);
        return button;
    }
}
