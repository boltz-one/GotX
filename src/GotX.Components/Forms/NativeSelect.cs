namespace GotX.Components;

/// <summary>
/// native-select.tsx. GotX không có control gốc của hệ điều hành, nên đây là lớp mỏng trên <see cref="Select"/>:
/// cùng hộp và danh sách bật ra, chỉ đổi tên event cho đúng component. Hộp của Select đã có đúng class của
/// NativeSelect (h-8, rounded-lg, border-input, chevron bên phải); cỡ <c>sm</c> không làm vì Select không cho đổi class.
/// Bàn phím thừa hưởng từ Select: hộp là điểm dừng Tab có vòng focus, Enter/Space/↑/↓ mở, trong danh sách
/// ↑/↓/Home/End rồi Enter/Space chọn, Escape đóng; đóng xong focus về hộp.
/// </summary>
public sealed class NativeSelect : View
{
    Select _select = null!;
    IReadOnlyList<string> _items = [];
    int _index = -1;
    string _placeholder = "Chọn…";
    bool _disabled;

    /// <summary>Lựa chọn vừa đổi.</summary>
    public readonly record struct ValueChanged(int Index, string Value);

    /// <summary>Các lựa chọn (option).</summary>
    public IReadOnlyList<string> Items
    {
        get => _items;
        set { _items = value; if (_select is not null) _select.Items = value; }
    }

    /// <summary>Chỉ số đang chọn, -1 = chưa chọn.</summary>
    public int SelectedIndex
    {
        get => _select?.SelectedIndex ?? _index;
        set { _index = value; if (_select is not null) _select.SelectedIndex = value; }
    }

    /// <summary>Nhãn đang chọn.</summary>
    public string? Value => _select?.Value;

    /// <summary>Chữ khi chưa chọn.</summary>
    public string Placeholder
    {
        get => _placeholder;
        set { _placeholder = value; if (_select is not null) { _select.Placeholder = value; _select.Notify(); } }
    }

    /// <summary>Vô hiệu hoá: mờ đi, không mở danh sách.</summary>
    public bool Disabled
    {
        get => _disabled;
        set { _disabled = value; if (_select is not null) { _select.Disabled = value; _select.Notify(); } }
    }

    /// <summary>Danh sách đang mở.</summary>
    public bool IsOpen => _select?.IsOpen == true;

    /// <summary>Mục đang được tô sáng trong danh sách mở (bằng phím hoặc chuột); -1 khi chưa có.</summary>
    public int ActiveIndex => _select?.ActiveIndex ?? -1;

    /// <summary>Key của hộp – phần tử focus được (Tab), nhận lại focus khi danh sách đóng.</summary>
    public string TriggerKey => _select.TriggerKey;

    /// <summary>Key của mục thứ <paramref name="index"/> trong danh sách đang mở.</summary>
    public string ItemKey(int index) => _select.ItemKey(index);

    protected override void OnCreated()
    {
        _select = App.New(new Select { Items = _items, Placeholder = _placeholder, Disabled = _disabled });
        if (_index >= 0) _select.SelectedIndex = _index;
        App.Subscribe<Select.ValueChanged>(_select, e => Emit(new ValueChanged(e.Index, e.Value)));
    }

    public override Div Render(Window w) => w.Div().Class("inline-flex").Child(w.View(_select));
}
