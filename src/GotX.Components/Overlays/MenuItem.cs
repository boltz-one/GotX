namespace GotX.Components;

/// <summary>Loại dòng trong menu.</summary>
public enum MenuItemKind : byte { Item, Checkbox, Label, Separator, Submenu, Custom }

/// <summary>
/// Một dòng của DropdownMenu / ContextMenu / Menubar. Dùng các hàm tạo tĩnh:
/// <code>
/// [MenuItem.Label("Tài khoản"), MenuItem.Action("Hồ sơ", "Ctrl+Shift+P"), MenuItem.Separator(),
///  MenuItem.Check("Thanh trạng thái", true), MenuItem.Sub("Mời", [MenuItem.Action("Email"), MenuItem.Action("Tin nhắn")]),
///  MenuItem.Action("Xoá", destructive: true)]
/// </code>
/// <see cref="Checked"/> của dòng checkbox được menu tự đảo khi bấm, nên giữ chính object này làm nguồn trạng thái.
/// </summary>
public sealed class MenuItem
{
    public MenuItemKind Kind { get; init; }
    public string Text { get; init; } = "";

    /// <summary>Phím tắt hiển thị bên phải (chỉ để đọc: menu không tự đăng ký phím tắt toàn cục).</summary>
    public string Shortcut { get; init; } = "";

    public bool Disabled { get; set; }

    /// <summary>Kiểu nguy hiểm (chữ đỏ), cho các thao tác như Xoá.</summary>
    public bool Destructive { get; init; }

    /// <summary>Trạng thái của dòng checkbox.</summary>
    public bool Checked { get; set; }

    /// <summary>Icon bên trái (tuỳ chọn), ví dụ <c>Icon.Plus</c>.</summary>
    public Func<Window, Div>? Icon { get; init; }

    /// <summary>Các dòng của menu con (chỉ với <see cref="MenuItemKind.Submenu"/>).</summary>
    public IReadOnlyList<MenuItem> Items { get; init; } = [];

    /// <summary>Hàm dựng của dòng tự vẽ (chỉ với <see cref="MenuItemKind.Custom"/>).</summary>
    public Func<Window, Div>? Content { get; init; }

    /// <summary>Key gắn lên dòng (tuỳ chọn) – để tìm dòng trong kiểm tra headless.</summary>
    public string? Key { get; init; }

    /// <summary>Dòng hành động thường: bấm thì phát sự kiện và đóng menu.</summary>
    public static MenuItem Action(string text, string shortcut = "", bool destructive = false, bool disabled = false, string? key = null) =>
        new() { Text = text, Shortcut = shortcut, Destructive = destructive, Disabled = disabled, Key = key };

    /// <summary>Dòng checkbox: bấm thì đảo <see cref="Checked"/>, menu vẫn mở để bật/tắt tiếp.</summary>
    public static MenuItem Check(string text, bool isChecked = false, string shortcut = "", string? key = null) =>
        new() { Kind = MenuItemKind.Checkbox, Text = text, Checked = isChecked, Shortcut = shortcut, Key = key };

    /// <summary>Dòng mở menu con bên cạnh (→ / Enter / rê chuột để mở, ← để đóng).</summary>
    public static MenuItem Sub(string text, IReadOnlyList<MenuItem> items, bool disabled = false, string? key = null) =>
        new() { Kind = MenuItemKind.Submenu, Text = text, Items = items, Disabled = disabled, Key = key };

    /// <summary>Tiêu đề nhóm (chữ nhỏ, mờ, không bấm được).</summary>
    public static MenuItem Label(string text) => new() { Kind = MenuItemKind.Label, Text = text };

    /// <summary>Dòng tự vẽ, không bấm được (vd. đầu menu tài khoản: avatar + tên + email).</summary>
    public static MenuItem Custom(Func<Window, Div> content) => new() { Kind = MenuItemKind.Custom, Content = content };

    /// <summary>Đường kẻ ngăn nhóm.</summary>
    public static MenuItem Separator() => new() { Kind = MenuItemKind.Separator };

    /// <summary>Dòng có thể bấm (không phải nhãn, đường kẻ, hay dòng bị tắt).</summary>
    public bool IsInteractive => Kind is (MenuItemKind.Item or MenuItemKind.Checkbox or MenuItemKind.Submenu) && !Disabled;
}

/// <summary>
/// Một dòng hành động được chọn. <paramref name="Menu"/> là chỉ số menu (Menubar), 0 với menu đơn;
/// <paramref name="Index"/> là chỉ số trong danh sách chứa dòng đó (với dòng của menu con: trong menu con).
/// </summary>
public readonly record struct MenuItemSelected(int Menu, int Index, MenuItem Item);

/// <summary>Một dòng checkbox vừa đổi trạng thái.</summary>
public readonly record struct MenuItemToggled(int Menu, int Index, MenuItem Item, bool Checked);
