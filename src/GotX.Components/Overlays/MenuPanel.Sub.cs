namespace GotX.Components;

/// <summary>Phím, rê chuột và menu con của <see cref="MenuPanel"/>.</summary>
public sealed partial class MenuPanel
{
    /// <summary>Rê chuột lên dòng menu con bao lâu (giây) thì panel con mở – cũng là thời gian chờ trước khi đóng nó.</summary>
    const double SubDelay = 0.1;

    readonly PopupSlot _slot = new();   // overlay của panel con
    readonly Delay _subDelay = new();
    MenuPanel? _sub;
    int _subIndex = -1;
    bool _pointerInside, _closingSub;

    bool HandleKey(KeyEvent e)
    {
        var items = Items;
        int next = RovingFocus.FromKey(e.Key, items, items.Count, _active, _owner.LoopFocus, vertical: true, static (list, i) => list[i].IsInteractive);
        if (next != RovingFocus.NotNavigation)
        {
            // Đi bằng phím thì panel con của dòng cũ đóng ngay, không chờ như khi rê chuột.
            _subDelay.Cancel();
            if (next != _subIndex) CloseSub();
            if (_active != next) { _active = next; Notify(); }
            return true;
        }
        bool onSub = (uint)_active < (uint)items.Count && items[_active] is { Kind: MenuItemKind.Submenu, Disabled: false };
        switch (e.Key)
        {
            case Key.Enter or Key.Space:
                if (_active >= 0) _owner.Activate(this, _active, keyboard: true);
                return true;
            case Key.Right:
                if (onSub) { OpenSub(_active, focusFirst: true); return true; }
                return _owner.OnHorizontalKey(+1);
            case Key.Left:
                if (_parent is not null) { _parent.CloseSub(); return true; }
                return _owner.OnHorizontalKey(-1);
            case Key.Tab:
                return true; // như Radix/Base UI: Tab không rời khỏi menu đang mở
        }
        return false;
    }

    void HoverRow(int index, bool inside)
    {
        var items = Items;
        if ((uint)index >= (uint)items.Count) return;
        if (!inside)
        {
            // Rời dòng thì tắt sáng, trừ dòng đang mở menu con (con trỏ thường đang đi sang panel con).
            if (_active == index && !(_slot.IsOpen && index == _subIndex)) { _active = -1; Notify(); }
            return;
        }
        _parent?.KeepSub();
        // Con trỏ vào tấm nào thì tấm đó nhận phím (menu con mở bằng rê chuột chưa lấy focus). Focus theo chuột nên
        // "im lặng": không bật focus-visible.
        if (_window is { } w && !w.IsFocused(ContentKey)) w.FocusKey(ContentKey, visible: false);
        int target = items[index].IsInteractive ? index : -1;
        if (_active != target) { _active = target; Notify(); }
        // Mở/đóng panel con có trễ: lướt chéo qua vài dòng khác để tới panel con không làm nó đóng mất.
        _subDelay.Start(App, SubDelay, SyncSub);
    }

    // Con trỏ đã vào panel con: huỷ lệnh đóng đang chờ và trả nền sáng về dòng mở ra nó.
    void KeepSub()
    {
        _subDelay.Cancel();
        if (_slot.IsOpen && _active != _subIndex) { _active = _subIndex; Notify(); }
    }

    void SyncSub()
    {
        var items = Items;
        if ((uint)_active < (uint)items.Count && items[_active] is { Kind: MenuItemKind.Submenu, Disabled: false })
            OpenSub(_active, focusFirst: false);
        else if (_active >= 0)
            CloseSub();
    }

    /// <summary>Mở panel con của dòng <paramref name="index"/>; <paramref name="focusFirst"/> (mở bằng phím) thì nó lấy focus và tô dòng đầu.</summary>
    internal void OpenSub(int index, bool focusFirst)
    {
        var items = Items;
        if (_window is null || (uint)index >= (uint)items.Count || items[index] is not { Kind: MenuItemKind.Submenu, Disabled: false }) return;
        _subDelay.Cancel();
        if (_slot.IsOpen && _subIndex == index)
        {
            if (focusFirst) { _sub!.Prepare(MenuHighlight.First, takeFocus: true); _window.FocusKey(_sub.ContentKey); }
            return;
        }
        CloseSub();
        _sub ??= App.New(new MenuPanel(_owner, Menu, this));
        _subIndex = _active = index;
        _sub.Prepare(focusFirst ? MenuHighlight.First : MenuHighlight.None, takeFocus: focusFirst);
        // Offset 2: panel con chồm nhẹ lên mép tấm cha, không có khe hở nào để con trỏ "rơi ra ngoài" khi đi sang.
        _slot.Open(_window, _sub, new OverlayOptions { Anchor = RowAnchor(index), Placement = Placement.RightStart, Offset = 2 }, OnSubChanged);
    }

    /// <summary>Đóng panel con (và các cấp sâu hơn) một cách chủ động; focus trở về tấm này nếu đang ở trong panel con.</summary>
    internal void CloseSub()
    {
        if (!_slot.IsOpen) return;
        _closingSub = true;
        try { _sub!.CloseSub(); _slot.Close(); }
        finally { _closingSub = false; }
    }

    void OnSubChanged()
    {
        Notify();
        if (_slot.IsOpen) return;
        _subIndex = -1;
        _sub?.OnClosed();
        // Panel con bị Core đóng (Escape / bấm ra ngoài) chứ không phải do tấm này: cả cây menu đóng theo như Radix –
        // trừ khi con trỏ đang ở trong tấm này, tức người dùng vừa bấm vào một dòng của nó.
        if (_closingSub || _pointerInside) return;
        if (_parent is not null) _parent._slot.Close();
        else _owner.Close();
    }

    /// <summary>Overlay của tấm này vừa đóng: dọn panel con và trạng thái để lần mở sau bắt đầu sạch.</summary>
    internal void OnClosed()
    {
        _subDelay.Cancel();
        CloseSub();
        _active = -1;
        _pointerInside = false;
    }
}
