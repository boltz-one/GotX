using GotX.Widgets;

namespace GotX.Components;

/// <summary>Một dòng của <see cref="Command"/>.</summary>
public sealed record CommandItem(string Text, string Shortcut = "", bool Disabled = false, string? Key = null);

/// <summary>Một nhóm dòng có tiêu đề (tiêu đề rỗng thì không vẽ).</summary>
public sealed record CommandGroup(string Heading, IReadOnlyList<CommandItem> Items);

/// <summary>
/// Bảng lệnh: ô lọc + danh sách nhóm; gõ chữ thì chỉ giữ các dòng chứa chuỗi đó (không phân biệt hoa thường),
/// nhóm không còn dòng nào thì ẩn. Luôn có một dòng đang chọn (<see cref="ActiveIndex"/>, mặc định dòng khớp đầu tiên
/// như cmdk): focus ở yên trong ô gõ, ↑/↓ đổi dòng đang chọn, rê chuột cũng đổi, Enter hoặc bấm → <see cref="ItemSelected"/>.
/// Nhúng thẳng bằng <c>w.View(cmd)</c> hoặc mở thành hộp thoại bằng <see cref="OpenDialog"/>.
/// </summary>
public sealed partial class Command : View
{
    readonly PopupSlot _dialog = new();
    TextInput _input = null!;
    Window? _window;
    IReadOnlyList<CommandGroup> _groups = [];
    CommandItem[] _flat = [];
    string _filter = "";
    int _active = -1;

    /// <summary>Một dòng được chọn: chỉ số nhóm, chỉ số trong nhóm, và dòng.</summary>
    public readonly record struct ItemSelected(int Group, int Index, CommandItem Item);

    public IReadOnlyList<CommandGroup> Groups
    {
        get => _groups;
        set
        {
            _groups = value;
            // Bản phẳng của mọi dòng: phím ↑/↓ và chỉ số handler đều đếm liên tục qua các nhóm.
            int total = 0;
            foreach (var g in value) total += g.Items.Count;
            _flat = new CommandItem[total];
            int at = 0;
            foreach (var g in value) foreach (var it in g.Items) _flat[at++] = it;
            _active = FirstSelectable();
            Notify();
        }
    }

    public string Placeholder { get; set; } = "Gõ lệnh hoặc tìm kiếm…";
    public string EmptyText { get; set; } = "Không có kết quả.";

    /// <summary>Dòng có chữ này được đánh dấu tích (Combobox dùng để hiện giá trị đang chọn).</summary>
    public string? CheckedText { get; set; }

    /// <summary>Class thêm cho khung (ví dụ <c>"w-80 border border-border"</c> khi nhúng thẳng).</summary>
    public string PanelClass { get; set; } = "";

    /// <summary>Ô lọc – để focus (<c>window.Focus(cmd.Input)</c>) hoặc đọc chữ.</summary>
    public TextInput Input => _input;

    /// <summary>Chuỗi lọc hiện tại.</summary>
    public string Filter => _filter;

    /// <summary>Chỉ số phẳng (đếm qua mọi nhóm) của dòng đang chọn; -1 khi không dòng nào khớp.</summary>
    public int ActiveIndex => _active;

    /// <summary>Dòng đang chọn (null khi không dòng nào khớp).</summary>
    public CommandItem? ActiveItem => (uint)_active < (uint)_flat.Length ? _flat[_active] : null;

    public bool IsDialogOpen => _dialog.IsOpen;

    /// <summary>Overlay hộp thoại đang mở (null khi không ở dạng hộp thoại).</summary>
    public Overlay? DialogOverlay => _dialog.Current;

    /// <summary>Tạo ô lọc một dòng và nghe thay đổi chữ / Enter.</summary>
    protected override void OnCreated()
    {
        _input = App.New(new TextInput
        {
            Multiline = false, MinHeight = 0, Placeholder = Placeholder,
            BoxClass = "text-sm cursor-text",
        });
        // Lọc lại thì dòng đang chọn về dòng khớp đầu tiên (như cmdk), nên Enter luôn có đích rõ ràng.
        App.Subscribe<TextInput.TextChanged>(_input, e => { _filter = e.Text.Trim(); _active = FirstSelectable(); Notify(); });
        App.Subscribe<TextInput.Submitted>(_input, _ => Pick(_active));
    }

    /// <summary>Dòng có khớp chuỗi lọc không.</summary>
    public bool Matches(CommandItem item) => _filter.Length == 0 || item.Text.Contains(_filter, StringComparison.OrdinalIgnoreCase);

    /// <summary>Số dòng đang hiện sau khi lọc.</summary>
    public int VisibleCount
    {
        get
        {
            int n = 0;
            foreach (var it in _flat) if (Matches(it)) n++;
            return n;
        }
    }

    bool Selectable(int flat) => !_flat[flat].Disabled && Matches(_flat[flat]);

    int FirstSelectable() => RovingFocus.Edge(this, _flat.Length, false, static (c, i) => c.Selectable(i));

    /// <summary>Xoá chuỗi lọc và đưa dòng đang chọn về đầu.</summary>
    public void Reset()
    {
        if (_input.Text.Length > 0) _input.Text = "";
        _active = FirstSelectable();
        Notify();
    }

    /// <summary>Mở bảng lệnh thành hộp thoại modal phía trên cửa sổ, focus sẵn ô lọc.</summary>
    public void OpenDialog(Window window)
    {
        if (IsDialogOpen) return;
        Reset();
        _dialog.Open(window, this, new OverlayOptions { Modal = true, LayerClass = "flex justify-center items-start p-4 pt-24" }, Notify);
        window.Focus(_input);
    }

    /// <summary>Đóng hộp thoại (nếu đang mở dạng hộp thoại).</summary>
    public void CloseDialog() => _dialog.Close();

    /// <summary>Chọn dòng thứ <paramref name="flat"/> tính liên tục qua mọi nhóm.</summary>
    void Pick(int flat)
    {
        if ((uint)flat >= (uint)_flat.Length || _flat[flat].Disabled) return;
        int index = flat;
        for (int g = 0; g < _groups.Count; g++)
        {
            int count = _groups[g].Items.Count;
            if (index < count)
            {
                Emit(new ItemSelected(g, index, _flat[flat]));
                CloseDialog();
                return;
            }
            index -= count;
        }
    }

    void SetActive(int flat, bool reveal)
    {
        if (_active == flat || (uint)flat >= (uint)_flat.Length || !Selectable(flat)) return;
        _active = flat;
        Notify();
        if (reveal) _window?.ScrollIntoView(RowAnchor(flat));
    }

    /// <summary>
    /// Phím nổi lên từ ô gõ: ô một dòng không dùng ↑/↓ nên chúng tới đây. Home/End thì ô gõ dùng để dời con trỏ,
    /// nên không nhảy đầu/cuối danh sách như cmdk. Không quay vòng ở hai đầu (cmdk <c>loop</c> mặc định tắt).
    /// </summary>
    bool KeyDown(KeyEvent e)
    {
        if (e.Key is not (Key.Up or Key.Down)) return false;
        SetActive(RovingFocus.Step(this, _flat.Length, _active, e.Key == Key.Down ? 1 : -1, loop: false, static (c, i) => c.Selectable(i)), reveal: true);
        return true;
    }
}
