namespace GotX.Components;

/// <summary>Dòng được tô sáng sẵn khi menu mở: mở bằng chuột thì chưa có, bằng ↓/Enter là dòng đầu, bằng ↑ là dòng cuối.</summary>
internal enum MenuHighlight : byte { None, First, Last }

/// <summary>
/// Gốc chung của DropdownMenu, ContextMenu và Menubar: cả ba đều mở một hay nhiều <see cref="MenuPanel"/> và xử lý
/// bấm dòng giống nhau (dòng thường → phát <see cref="MenuItemSelected"/> rồi đóng; checkbox → đảo, phát
/// <see cref="MenuItemToggled"/>, menu vẫn mở; dòng menu con → mở panel con). Chỉ khác cách mở và vị trí neo.
/// </summary>
public abstract class MenuOwner : View
{
    private protected MenuOwner() { }

    /// <summary>Bảng class của style lúc tạo menu – các panel (vẽ trong overlay) dùng chung bảng này.</summary>
    // Bảng ghim lúc tạo nếu được tạo trong ComponentStyles.Use(...); không thì theo style chung của app (đổi được lúc chạy).
    readonly ComponentStyleSheet? _pinned = ComponentStyles.Scoped;
    internal ComponentStyleSheet Sheet => _pinned ?? ComponentStyles.S;

    /// <summary>Danh sách dòng của menu thứ <paramref name="menu"/>.</summary>
    private protected abstract IReadOnlyList<MenuItem> ItemsOf(int menu);

    /// <summary>Đóng menu đang mở (nếu có), kể cả các menu con.</summary>
    public abstract void Close();

    /// <summary>
    /// ↑/↓ ở dòng cuối/đầu quay vòng về đầu kia (mặc định của bản shadcn gốc – Base UI <c>loopFocus</c>).
    /// Tắt để dừng lại ở hai đầu như Radix.
    /// </summary>
    public bool LoopFocus { get; set; } = true;

    /// <summary>Class thêm cho tấm danh sách gốc (không áp cho menu con), ví dụ độ rộng <c>"w-40"</c>.</summary>
    public string MenuClass { get; set; } = "";

    /// <summary>Tạo panel cho menu thứ <paramref name="menu"/> (gọi trong OnCreated).</summary>
    private protected MenuPanel NewPanel(int menu) => App.New(new MenuPanel(this, menu, null));

    internal IReadOnlyList<MenuItem> MenuItems(int menu) => ItemsOf(menu);

    /// <summary>←/→ mà panel không dùng tới (không mở/đóng menu con): Menubar dùng để sang menu bên cạnh.</summary>
    internal virtual bool OnHorizontalKey(int direction) => false;

    /// <summary>
    /// Mở <paramref name="panel"/> trong <paramref name="slot"/>. Đóng overlay cũ TRƯỚC khi chuẩn bị panel: callback đóng
    /// của overlay cũ xoá dòng đang tô sáng, chạy sau thì sẽ xoá luôn lựa chọn vừa đặt cho lần mở mới.
    /// </summary>
    private protected void OpenPanel(PopupSlot slot, Window window, MenuPanel panel, OverlayOptions options, MenuHighlight highlight, Action changed)
    {
        slot.Close();
        panel.Prepare(highlight, takeFocus: true);
        slot.Open(window, panel, options, () =>
        {
            if (!slot.IsOpen) panel.OnClosed();
            changed();
        });
    }

    /// <summary>Đóng từ menu con sâu nhất trở ra: mỗi lớp trả focus về lớp mở ra nó, cuối cùng về nút mở.</summary>
    private protected static void ClosePanel(PopupSlot slot, MenuPanel? panel)
    {
        if (slot.IsOpen) panel?.CloseSub();
        slot.Close();
    }

    /// <summary>Phím trên nút mở đang focus: ↓/Enter/Space mở và tô dòng đầu, ↑ mở và tô dòng cuối.</summary>
    private protected static bool OpensMenu(Key key, out MenuHighlight highlight)
    {
        highlight = key == Key.Up ? MenuHighlight.Last : MenuHighlight.First;
        return key is Key.Down or Key.Up or Key.Enter or Key.Space;
    }

    internal void Activate(MenuPanel panel, int index, bool keyboard)
    {
        var items = panel.Items;
        if ((uint)index >= (uint)items.Count || !items[index].IsInteractive) return;
        var item = items[index];
        switch (item.Kind)
        {
            case MenuItemKind.Submenu:
                panel.OpenSub(index, keyboard);
                return;
            case MenuItemKind.Checkbox:
                item.Checked = !item.Checked;
                panel.Notify();
                Emit(new MenuItemToggled(panel.Menu, index, item, item.Checked));
                return;
        }
        Emit(new MenuItemSelected(panel.Menu, index, item));
        Close();
    }
}
