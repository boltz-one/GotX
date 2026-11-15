namespace GotX.Components;

/// <summary>Phần vẽ của <see cref="NavigationMenu"/>: thanh mục và tấm liên kết.</summary>
public sealed partial class NavigationMenu
{
    const string TriggerBase =
        "flex h-9 items-center gap-1 rounded-lg px-2.5 py-1.5 text-sm font-medium cursor-pointer transition-all " +
        "hover:bg-muted focus-visible:bg-muted focus-visible:ring-3 focus-visible:ring-ring/50 ";
    const string Chevron = "transition-transform duration-300";
    const string LinkClass =
        "flex flex-col gap-1 rounded-md p-2 text-sm cursor-pointer transition-all hover:bg-muted " +
        "focus-visible:bg-muted focus-visible:ring-3 focus-visible:ring-ring/50";

    Indexed<NavigationMenu>[]? _refs, _linkRefs;
    string[] _anchors = [], _triggerKeys = [], _chevronKeys = [], _linkKeys = [];

    /// <summary>Vẽ thanh mục; tấm nội dung được vẽ riêng trong lớp overlay.</summary>
    public override Div Render(Window w)
    {
        _window = w;
        var refs = Indexed<NavigationMenu>.Ensure(ref _refs, this, _items.Count);
        var chevrons = RovingFocus.Names(ref _chevronKeys, _items.Count, "gotx-navmenu-chevron-", Id);
        var bar = w.Div().Class("flex items-center gap-0");
        int open = OpenIndex;
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var trigger = w.Div()
                .Class(i == open ? TriggerBase + "bg-muted/50" : TriggerBase)
                .Key(TriggerKey(i)).Anchor(AnchorName(i)).Focusable()
                .OnClick(refs[i], static (r, _) => r.Owner.Activate(r.Index))
                .OnKey(refs[i], static (r, e) => r.Owner.TriggerKeyDown(r.Index, e))
                .OnHover(refs[i], static (r, inside) => r.Owner.HoverTrigger(r.Index, inside))
                .Child(item.Title);
            // Mũi tên xoay 180° khi tấm mở; key riêng để transition bám đúng phần tử.
            if (item.Links is not null)
                trigger.Child(w.Div().Key(chevrons[i]).Class(i == open ? Chevron + " rotate-180" : Chevron).Child(Icon.ChevronDown(w)));
            bar.Child(trigger);
        }
        return bar;
    }

    Div RenderPanel(Window w)
    {
        var item = (uint)_open < (uint)_items.Count ? _items[_open] : null;
        var links = item?.Links ?? [];
        var refs = Indexed<NavigationMenu>.Ensure(ref _linkRefs, this, links.Count);
        var grid = w.Div().Class(item?.Columns >= 2
                ? "grid w-[32rem] grid-cols-2 gap-1 rounded-lg bg-popover p-1 text-popover-foreground shadow ring-1 ring-foreground/10"
                : "grid w-72 grid-cols-1 gap-1 rounded-lg bg-popover p-1 text-popover-foreground shadow ring-1 ring-foreground/10")
            .OnKey(this, static (n, e) => n.PanelKeyDown(e));
        for (int i = 0; i < links.Count; i++)
        {
            var link = w.Div().Key(LinkKey(i)).Class(LinkClass).Focusable()
                .OnClick(refs[i], static (r, _) => r.Owner.PickLink(r.Index))
                .Child(w.Div().Class("font-medium leading-none").Child(links[i].Title));
            if (links[i].Description.Length > 0)
                link.Child(w.Div().Class("text-muted-foreground leading-snug").Child(links[i].Description));
            grid.Child(link);
        }
        // Lớp đệm pt-1.5 thay cho khoảng hở giữa mục và tấm; cả lớp là vùng hover của tấm.
        return w.Div().Class("pt-1.5")
            .OnHover(this, static (n, inside) => { n._inContent = inside; n.Evaluate(); })
            .Child(grid);
    }
}
