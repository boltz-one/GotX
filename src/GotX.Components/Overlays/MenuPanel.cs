namespace GotX.Components;

/// <summary>
/// Tấm danh sách dòng của một menu, vẽ trong lớp overlay. Dùng chung cho mọi loại menu và cho menu con
/// (panel con là một MenuPanel khác, neo vào dòng mở ra nó). Tấm giữ focus bàn phím; dòng đang chọn là
/// <see cref="ActiveIndex"/> – cùng một chỉ số cho cả phím mũi tên lẫn rê chuột, nên không bao giờ có hai dòng sáng.
/// </summary>
public sealed partial class MenuPanel : View
{

    readonly MenuOwner _owner;
    readonly MenuPanel? _parent;
    Indexed<MenuPanel>[]? _refs;
    string[] _anchors = [];
    string? _contentKey;
    Window? _window;
    int _active = -1;
    bool _takeFocus = true;

    internal MenuPanel(MenuOwner owner, int menu, MenuPanel? parent) { _owner = owner; Menu = menu; _parent = parent; }

    /// <summary>Chỉ số menu trong owner (0 với menu đơn).</summary>
    public int Menu { get; }

    /// <summary>Dòng đang được tô sáng (bằng phím hoặc chuột); -1 khi chưa có.</summary>
    public int ActiveIndex => _active;

    /// <summary>Panel con đang mở (null khi không có) – đọc dòng đang chọn của menu con.</summary>
    public MenuPanel? OpenSubmenu => _slot.IsOpen ? _sub : null;

    /// <summary>Key của tấm – phần tử giữ focus khi menu mở (<c>window.IsFocused(panel.ContentKey)</c>).</summary>
    public string ContentKey => _contentKey ??= "gotx-menu-" + Id.Value;

    /// <summary>Các dòng của tấm: menu gốc lấy từ owner, menu con lấy từ dòng đang mở của tấm cha.</summary>
    internal IReadOnlyList<MenuItem> Items =>
        _parent is null ? _owner.MenuItems(Menu)
        : (uint)_parent._subIndex < (uint)_parent.Items.Count ? _parent.Items[_parent._subIndex].Items : [];

    /// <summary>Đặt trạng thái cho lần mở sắp tới: dòng tô sẵn và có lấy focus ngay không (menu con mở bằng rê chuột thì không).</summary>
    internal void Prepare(MenuHighlight highlight, bool takeFocus)
    {
        var items = Items;
        _active = highlight == MenuHighlight.None ? -1
            : RovingFocus.Edge(items, items.Count, highlight == MenuHighlight.Last, static (list, i) => list[i].IsInteractive);
        _takeFocus = takeFocus;
        Notify();
    }

    // Tên anchor từng dòng menu con: panel con neo vào đúng dòng mở ra nó.
    string RowAnchor(int index) => RovingFocus.Names(ref _anchors, index + 1, "gotx-menu-", Id)[index];

    /// <summary>Vẽ danh sách dòng; class chọn từ hằng nên không nối chuỗi mỗi frame.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        var sheet = _owner.Sheet;
        using var _ = ComponentStyles.Use(sheet); // icon/nội dung dòng do người dùng dựng
        var items = Items;
        var refs = Indexed<MenuPanel>.Ensure(ref _refs, this, items.Count);
        var list = w.Div().Key(ContentKey)
            .Class(sheet.MenuPanel).Role(SemanticRole.Menu)
            .Focusable().AutoFocus(_takeFocus)
            .OnKey(this, static (p, e) => p.HandleKey(e))
            .OnHover(this, static (p, inside) => p._pointerInside = inside);
        if (_parent is null && _owner.MenuClass.Length > 0) list.Class(_owner.MenuClass);
        int subOpen = _slot.IsOpen ? _subIndex : -1;
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            switch (item.Kind)
            {
                case MenuItemKind.Separator:
                    list.Child(w.Div().Class(sheet.MenuSeparator).Role(SemanticRole.Separator));
                    continue;
                case MenuItemKind.Custom:
                    if (item.Content is { } content) list.Child(content(w));
                    continue;
                case MenuItemKind.Label:
                    list.Child(w.Div().Class(sheet.MenuLabel).Child(item.Text));
                    continue;
            }
            // Dòng đang mở menu con giữ nền sáng (data-open:bg-accent) dù con trỏ đã sang panel con.
            bool on = !item.Disabled && (i == _active || i == subOpen);
            var row = w.Div().Class(sheet.MenuRow[item.Disabled ? 4 : (item.Destructive ? 2 : 0) + (on ? 1 : 0)])
                .Disabled(item.Disabled).Role(SemanticRole.MenuItem)
                .AriaState(SemanticState.Checked, item.Kind == MenuItemKind.Checkbox && item.Checked).AriaState(SemanticState.Expanded, i == subOpen)
                .OnClick(refs[i], static (r, _) => r.Owner._owner.Activate(r.Owner, r.Index, keyboard: false))
                .OnHover(refs[i], static (r, inside) => r.Owner.HoverRow(r.Index, inside));
            if (item.Key is { } key) row.Key(key);
            if (item.Icon is { } icon) row.Child(sheet.MenuIcon.Length > 0 && !item.Destructive ? icon(w).Class(sheet.MenuIcon) : icon(w));
            row.Child(w.Div().Class("flex-1").Child(item.Text));
            if (item.Shortcut.Length > 0)
                row.Child(w.Div().Class(sheet.MenuShortcut[on && !item.Destructive ? 1 : 0]).Child(item.Shortcut));
            // Ô dấu tích giữ chỗ cả khi chưa chọn để chữ không nhảy khi bật/tắt.
            if (item.Kind == MenuItemKind.Checkbox)
                row.Child(item.Checked ? Icon.Check(w) : w.Div().Class("size-4 shrink-0"));
            else if (item.Kind == MenuItemKind.Submenu)
                row.Anchor(RowAnchor(i)).Child(w.Div().Class(on ? "pl-2" : "pl-2 text-muted-foreground").Child(Icon.ChevronRight(w)));
            list.Child(row);
        }
        return list;
    }
}
