namespace GotX.Components;

/// <summary>Phần dựng cây của <see cref="SidebarNav"/>: nhóm → nút → (nút phụ | badge) → danh sách con.</summary>
public sealed partial class SidebarNav
{
    /// <summary>Vẽ vùng cuộn chứa các nhóm. Class và key đều tra bảng dựng sẵn nên frame ổn định không cấp phát.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        var rows = EnsureRows();
        var itemRefs = Indexed<SidebarNav>.Ensure(ref _itemRefs, this, rows.Length);
        var subRefs = Indexed<SidebarNav>.Ensure(ref _subRefs, this, _subRow.Length);
        var root = w.Div().Key(ContentKey).Class(_collapsed ? ContentCollapsed : ContentExpanded);
        for (int g = 0; g < _groups.Count; g++)
        {
            var group = _groups[g];
            if (_collapsed && group.HideWhenCollapsed) continue;
            // mt-auto của shadcn: phần tử đệm chiếm chỗ trống phía trên nhóm (-mt-2 bù khoảng gap thừa do có thêm một con).
            if (group.PushToBottom) root.Child(w.Div().Class("-mt-2 flex-1"));
            var box = w.Div().Class(_collapsed ? GroupCollapsedClass : GroupClass);
            if (!_collapsed && group.Label is { } label) box.Child(w.Div().Class(GroupLabelClass).Child(label));
            var menu = w.Div().Class(MenuClass);
            for (int i = 0, flat = _groupStart[g]; i < group.Items.Count; i++, flat++)
                menu.Child(RenderItem(w, rows[flat], group.SubMenus, itemRefs[flat], subRefs));
            root.Child(box.Child(menu));
        }
        // View của menu nút phụ không có hình: đặt absolute để nó không chiếm một khoảng gap trong cột.
        return root.Child(w.View(_menu).Class("absolute"));
    }

    Div RenderItem(Window w, Row row, SidebarSubMenus mode, Indexed<SidebarNav> self, Indexed<SidebarNav>[] subRefs)
    {
        var item = row.Data;
        bool hasSubs = item.SubItems.Count > 0, collapsible = hasSubs && mode == SidebarSubMenus.Collapsible;
        bool action = !_collapsed && item.HasAction;
        bool active = row.Flat == _activeRow && _activeSub < 0;

        var button = w.Div().Key(row.Key).Focusable().Class(ButtonClass(_collapsed, active, action, item))
            .OnClick(self, static (r, _) => r.Owner.Activate(r.Index));
        if (collapsible) button.OnKey(self, static (r, e) => r.Owner.ItemKeyDown(r.Index, e));
        if (item.Icon is { } icon) button.Child(icon(w));
        else if (_collapsed) button.Child(InitialIcon(w, item.Initial));
        if (!_collapsed)
        {
            button.Child(w.Div().Class(LabelClass).Child(item.Title));
            // Mũi tên có key riêng để transition xoay bám đúng phần tử khi các hàng phía trên mở/đóng.
            if (collapsible)
                button.Child(w.Div().Key(row.ChevronKey).Class(row.Open ? ChevronOpen : ChevronClosed).Child(Icon.ChevronRight(w)));
        }

        // "group": nút phụ showOnHover hiện khi rê chuột vào bất kỳ đâu trong hàng.
        var wrap = w.Div().Class(action ? "group relative" : "relative").Child(button);
        if (_collapsed) return wrap;
        if (action) wrap.Child(RenderAction(w, row, self));
        else if (item.Badge is { } badge) wrap.Child(w.Div().Class(BadgeClass).Child(badge));
        if (!hasSubs || (collapsible && !row.Open)) return wrap;

        var list = w.Div().Class(SubListClass);
        for (int s = 0; s < item.SubItems.Count; s++)
        {
            var sub = w.Div().Key(row.SubKeys[s]).Focusable()
                .Class(row.Flat == _activeRow && s == _activeSub ? SubActiveClass : SubClass)
                .OnClick(subRefs[row.SubStart + s], static (r, _) => r.Owner.ActivateSub(r.Index))
                .Child(w.Div().Class("min-w-0 truncate").Child(item.SubItems[s]));
            if (collapsible) sub.OnKey(subRefs[row.SubStart + s], static (r, e) => r.Owner.SubKeyDown(r.Index, e));
            list.Child(sub);
        }
        // Danh sách lệch 1px so với tâm nút cha (mx-3.5 + translate-x-px để viền trái trùng tâm icon): bọc nó trong một
        // khối rộng bằng nút để hàng vẫn là các khối thẳng mép nhau.
        return wrap.Child(w.Div().Child(list));
    }

    Div RenderAction(Window w, Row row, Indexed<SidebarNav> self)
    {
        var item = row.Data;
        // Menu đang mở thì nút phụ luôn hiện (data-[state=open]:opacity-100) dù con trỏ đã sang menu.
        bool hidden = item.ActionOnHover && _menuRow != row.Flat;
        return w.Div().Key(row.ActionKey).Anchor(row.ActionKey).Focusable().Class(hidden ? ActionHoverClass : ActionClass)
            .OnClick(self, static (r, _) => r.Owner.ActionClicked(r.Index))
            .OnDrag(self, static (r, e) => r.Owner.ActionPressed(r.Index, e))
            .OnKey(self, static (r, e) => r.Owner.ActionKeyDown(r.Index, e))
            .Child(item.Action is { } icon ? icon(w) : Icon.MoreHorizontal(w));
    }
}
