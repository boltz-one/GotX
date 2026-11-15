using GotX.Widgets;

namespace GotX.Components;

/// <summary>
/// Phần chung của <see cref="Input"/> và <see cref="Textarea"/>: bọc một <see cref="TextInput"/> của Core, gán class
/// kiểu shadcn và phát lại event với kiểu của component (người dùng không phải biết tới TextInput bên trong).
/// Thuộc tính đặt được trước khi gắn vào app: giá trị được giữ lại rồi chuyển cho ô nhập lúc <c>OnCreated</c>.
/// </summary>
public abstract class TextEntry : View
{
    TextInput? _box;
    string _text = "", _placeholder = "";
    bool _invalid;
    Window? _window;

    /// <summary>Bảng class của style đang hiệu lực lúc tạo ô (xem <see cref="ComponentStyles"/>).</summary>
    // Bảng ghim lúc tạo nếu được tạo trong ComponentStyles.Use(...); không thì theo style chung của app (đổi được lúc chạy).
    readonly ComponentStyleSheet? _pinned = ComponentStyles.Scoped;
    private protected ComponentStyleSheet Sheet => _pinned ?? ComponentStyles.S;

    /// <summary>Nội dung đổi (gõ, dán, xoá, hoặc gán <see cref="Text"/>).</summary>
    public readonly record struct TextChanged(string Text);

    /// <summary>Nhấn Enter trong ô một dòng.</summary>
    public readonly record struct Submitted(string Text);

    /// <summary>Nội dung hiện tại.</summary>
    public string Text
    {
        get => _box?.Text ?? _text;
        set { _text = value ?? ""; if (_box is not null) _box.Text = _text; else Notify(); }
    }

    /// <summary>Chữ gợi ý khi ô trống.</summary>
    public string Placeholder
    {
        get => _placeholder;
        set { _placeholder = value ?? ""; if (_box is not null) { _box.Placeholder = _placeholder; _box.Notify(); } }
    }

    /// <summary>Vô hiệu hoá: vẽ hộp tĩnh mờ thay cho ô nhập, nên không nhận focus và không gõ được.</summary>
    public bool Disabled { get; set { field = value; Notify(); } }

    /// <summary>Trạng thái lỗi (aria-invalid của shadcn): viền và vòng đỏ.</summary>
    public bool Invalid
    {
        get => _invalid;
        set { _invalid = value; ApplyClass(); }
    }

    /// <summary>
    /// Class khung tự đặt thay cho bảng của style (đối số = đang lỗi); vẫn đúng khi <see cref="Invalid"/> đổi. Hàm nên trả
    /// chuỗi dựng sẵn – nó được gọi mỗi lần ô vẽ lại.
    /// </summary>
    public Func<bool, string>? BoxClassOverride { get; set { field = value; ApplyClass(); } }

    /// <summary>Ô nằm trong <c>Ui.InputGroup</c>: bỏ viền/nền/vòng focus để nhóm vẽ thay.</summary>
    public bool InGroup { get; set { field = value; ApplyClass(); } }

    /// <summary>Ô nhập của Core bên trong – dùng khi cần focus bằng <c>window.Focus(entry.Box)</c>.</summary>
    public TextInput Box => _box ?? throw new InvalidOperationException("Component chưa được tạo qua App.New(...).");

    /// <summary>Ô đang giữ focus bàn phím.</summary>
    public bool IsFocused => _box is { IsFocused: true };

    /// <summary>Đưa focus vào ô (bấm nhãn, bấm phần phụ của InputGroup). Không làm gì khi bị vô hiệu hoá hoặc chưa vẽ lần nào.</summary>
    public void Focus()
    {
        if (!Disabled && _box is { } box) _window?.Focus(box);
    }

    /// <summary>Ô nhiều dòng hay một dòng.</summary>
    protected abstract bool Multiline { get; }

    /// <summary>Chiều cao tối thiểu vùng chữ (chỉ có nghĩa với ô nhiều dòng).</summary>
    protected virtual float MinTextHeight => 0;

    /// <summary>Class khung ô theo trạng thái lỗi.</summary>
    protected abstract string BoxClass(bool invalid);

    /// <summary>Class hộp tĩnh dùng khi bị vô hiệu hoá.</summary>
    protected abstract string DisabledClass { get; }

    protected override void OnCreated()
    {
        _box = App.New(new TextInput
        {
            Multiline = Multiline, MinHeight = MinTextHeight, Text = _text, Placeholder = _placeholder,
            BoxClass = CurrentBoxClass,
        });
        App.Subscribe<TextInput.TextChanged>(_box, e => { _text = e.Text; Emit(new TextChanged(e.Text)); });
        App.Subscribe<TextInput.Submitted>(_box, e => Emit(new Submitted(e.Text)));
    }

    string CurrentBoxClass => BoxClassOverride?.Invoke(_invalid) ?? BoxClass(_invalid);

    /// <summary>Gọi lại khi lớp con đổi một tuỳ chọn ảnh hưởng tới class khung.</summary>
    protected void ApplyClass()
    {
        if (_box is null) return;
        _box.BoxClass = CurrentBoxClass;
        _box.Notify();
    }

    public override Div Render(Window w)
    {
        _window = w;
        if (Disabled)
        {
            string text = Text;
            return w.Div().Class(DisabledClass).Disabled(true)
                .Child(w.Div().Class(text.Length > 0 ? "truncate" : "truncate text-muted-foreground").Child(text.Length > 0 ? text : _placeholder));
        }
        var box = Box;
        // Style của app đổi lúc chạy (ComponentTheme.Use) hay bảng ghim khác: cập nhật class khung của ô nhập bên trong.
        string cls = CurrentBoxClass;
        if (!ReferenceEquals(box.BoxClass, cls)) { box.BoxClass = cls; box.Notify(); }
        // Màu placeholder/vùng chọn của TextInput là Rgba cố định chứ không phải class, nên đổi theo dark mode ở đây.
        box.PlaceholderColor = w.DarkMode ? Rgba.FromHex(0xa1a1aa) : Rgba.FromHex(0x737373);
        box.SelectionColor = w.DarkMode ? Rgba.FromHex(0x3f3f46) : Rgba.FromHex(0xd4d4d8);
        return w.View(box);
    }
}
