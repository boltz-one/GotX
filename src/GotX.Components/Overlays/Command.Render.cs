namespace GotX.Components;

/// <summary>Phần vẽ của <see cref="Command"/>: ô lọc và danh sách đã lọc.</summary>
public sealed partial class Command
{
    const string RowBase = "flex items-center gap-2 rounded-sm px-2 py-1.5 text-sm ";
    const string RowOff = RowBase + "opacity-50";
    const string RowIdle = RowBase + "cursor-pointer";
    const string RowOn = RowBase + "cursor-pointer bg-muted text-foreground";

    Indexed<Command>[]? _refs;
    string[] _anchors = [];
    string? _listKey;

    // Tên anchor từng dòng: để cuộn dòng đang chọn bằng phím vào tầm nhìn.
    string RowAnchor(int flat) => RovingFocus.Names(ref _anchors, _flat.Length, "gotx-command-", Id)[flat];

    /// <summary>Vẽ ô lọc và danh sách đã lọc.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        var refs = Indexed<Command>.Ensure(ref _refs, this, _flat.Length);

        // OnKey ở khung ngoài: focus nằm trong ô gõ (phần tử con), ↑/↓ nổi lên đây.
        var panel = w.Div().Class(IsDialogOpen
                ? "flex flex-col w-[28rem] max-w-full overflow-hidden rounded-xl bg-popover p-1 text-popover-foreground shadow-lg ring-1 ring-foreground/10"
                : "flex flex-col overflow-hidden rounded-xl bg-popover p-1 text-popover-foreground")
            .OnKey(this, static (c, e) => c.KeyDown(e));
        if (PanelClass.Length > 0) panel.Class(PanelClass);
        panel.Child(w.Div().Class("p-1 pb-0").Child(
            w.Div().Class("flex h-8 items-center gap-2 rounded-lg border border-input/30 bg-input/30 px-2.5")
                .Child(w.Div().Class("flex shrink-0 opacity-50").Child(Icon.Search(w)))
                .Child(w.View(_input).Class("min-w-0 flex-1"))));

        var list = w.Div().Class("flex flex-col max-h-72 overflow-y-auto p-1").Key(_listKey ??= "gotx-command-list-" + Id.Value);
        int flat = 0, shownGroups = 0;
        foreach (var group in _groups)
        {
            Div block = default;
            bool started = false;
            foreach (var item in group.Items)
            {
                int index = flat++;
                if (!Matches(item)) continue;
                if (!started)
                {
                    started = true;
                    if (shownGroups++ > 0) list.Child(w.Div().Class("-mx-1 my-1 h-px bg-border"));
                    block = w.Div().Class("flex flex-col");
                    if (group.Heading.Length > 0)
                        block.Child(w.Div().Class("px-2 py-1.5 text-xs font-medium text-muted-foreground").Child(group.Heading));
                    list.Child(block);
                }
                block.Child(Row(w, item, refs[index]));
            }
        }
        if (shownGroups == 0) list.Child(w.Div().Class("py-6 text-center text-sm").Child(EmptyText));
        return panel.Child(list);
    }

    Div Row(Window w, CommandItem item, Indexed<Command> r)
    {
        bool on = r.Index == _active;
        var row = w.Div().Class(item.Disabled ? RowOff : on ? RowOn : RowIdle)
            .Disabled(item.Disabled)
            .Anchor(RowAnchor(r.Index))
            .OnClick(r, static (x, _) => x.Owner.Pick(x.Index))
            // Rê chuột chỉ ĐỔI dòng đang chọn, rời đi không bỏ chọn (như cmdk): Enter luôn còn đích.
            .OnHover(r, static (x, inside) => { if (inside) x.Owner.SetActive(x.Index, reveal: false); })
            .Child(w.Div().Class("flex-1 truncate").Child(item.Text));
        if (item.Key is { } key) row.Key(key);
        if (item.Shortcut.Length > 0)
            row.Child(w.Div().Class(on ? "text-xs tracking-widest text-foreground" : "text-xs tracking-widest text-muted-foreground").Child(item.Shortcut));
        if (CheckedText is { } c && c == item.Text) row.Child(Icon.Check(w));
        return row;
    }
}
