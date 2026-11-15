namespace GotX.Components;

/// <summary>Một liên kết trong tấm nội dung của <see cref="NavigationMenu"/>.</summary>
public sealed record NavigationLink(string Title, string Description = "", string? Key = null);

/// <summary>Mục trên cùng: không có <paramref name="Links"/> thì là liên kết thẳng, có thì là nút mở tấm (lưới <paramref name="Columns"/> cột).</summary>
public sealed record NavigationMenuItem(string Title, IReadOnlyList<NavigationLink>? Links = null, int Columns = 1);

/// <summary>
/// Thanh điều hướng của trang web: mục liên kết thẳng và mục mở tấm nội dung chứa các liên kết có mô tả.
/// Mở bằng rê chuột (sau <see cref="OpenDelay"/>) hoặc bấm; rời cả mục lẫn tấm thì đóng sau <see cref="CloseDelay"/>;
/// khi đã mở, rê sang mục khác thì chuyển tấm ngay.
/// Bàn phím: mục focus được, ←/→/Home/End đi giữa các mục, Enter/Space bật/tắt tấm, ↓ đi vào tấm;
/// trong tấm các mũi tên đi giữa các liên kết (↑ ở liên kết đầu trở về mục), Enter chọn, Escape đóng và trả focus về mục.
/// </summary>
public sealed partial class NavigationMenu : View
{
    readonly PopupSlot _slot = new();
    readonly Delay _openDelay = new(), _closeDelay = new();
    FragmentView _content = null!;
    Window? _window;
    IReadOnlyList<NavigationMenuItem> _items = [];
    int _open = -1, _hovered = -1;
    bool _inContent, _openedByHover;

    /// <summary>Người dùng chọn một liên kết; <paramref name="Link"/> = -1 khi là mục liên kết thẳng trên thanh.</summary>
    public readonly record struct LinkSelected(int Item, int Link, string Title);

    public IReadOnlyList<NavigationMenuItem> Items
    {
        get => _items;
        set { Close(); _items = value; Notify(); }
    }

    /// <summary>Số giây chuột nằm trên một mục trước khi tấm của nó mở (Base UI: 50 ms).</summary>
    public double OpenDelay { get; set; } = 0.05;

    /// <summary>Số giây chờ sau khi chuột rời cả mục lẫn tấm rồi mới đóng (Base UI: 50 ms).</summary>
    public double CloseDelay { get; set; } = 0.05;

    /// <summary>Chỉ số mục đang mở tấm, -1 khi không có.</summary>
    public int OpenIndex => _slot.IsOpen ? _open : -1;

    /// <summary>Overlay đang mở (null khi đóng) – đọc vị trí thực tế.</summary>
    public Overlay? Overlay => _slot.Current;

    /// <summary>Tên anchor của mục thứ <paramref name="index"/> (duy nhất theo instance).</summary>
    public string AnchorName(int index) => RovingFocus.Names(ref _anchors, _items.Count, "gotx-navmenu-", Id)[index];

    /// <summary>Key của mục thứ <paramref name="index"/> trên thanh – phần tử focus được.</summary>
    public string TriggerKey(int index) => RovingFocus.Names(ref _triggerKeys, _items.Count, "gotx-navmenu-trigger-", Id)[index];

    /// <summary>Key của liên kết thứ <paramref name="link"/> trong tấm đang mở (Key riêng của liên kết nếu có).</summary>
    public string LinkKey(int link) =>
        OpenLinks is { } links && (uint)link < (uint)links.Count && links[link].Key is { } key ? key
        : RovingFocus.Names(ref _linkKeys, link + 1, "gotx-navmenu-link-", Id)[link];

    IReadOnlyList<NavigationLink>? OpenLinks => (uint)_open < (uint)_items.Count ? _items[_open].Links : null;

    /// <summary>Tạo view tấm nội dung dùng lại cho mọi mục.</summary>
    protected override void OnCreated() => _content = App.New(new FragmentView(RenderPanel));

    /// <summary>Bấm (hoặc Enter/Space trên) mục <paramref name="index"/>: bật/tắt tấm của nó; mục liên kết thẳng thì phát LinkSelected.</summary>
    public void Activate(int index)
    {
        if ((uint)index >= (uint)_items.Count || _window is null) return;
        var item = _items[index];
        if (item.Links is null) { Close(); Emit(new LinkSelected(index, -1, item.Title)); return; }
        // Tấm vừa tự mở do rê chuột thì cú bấm ngay sau đó không được đóng nó lại (người dùng định bấm để mở).
        if (OpenIndex == index && !_openedByHover) { Close(); return; }
        _openedByHover = false;
        Show(index);
    }

    void Show(int index)
    {
        _openDelay.Cancel();
        _closeDelay.Cancel();
        if (_window is null || OpenIndex == index || (uint)index >= (uint)_items.Count || _items[index].Links is null) return;
        _open = index;
        _content.Notify();
        // Offset 0 + lớp đệm trong suốt phía trên tấm: vùng hover nối liền với mục, chuột đi xuống không bị coi là rời.
        _slot.Open(_window, _content, new OverlayOptions { Anchor = AnchorName(index), Placement = Placement.BottomStart, Offset = 0 },
            () => { if (!_slot.IsOpen) { _inContent = false; _openedByHover = false; } Notify(); });
    }

    /// <summary>Đóng tấm nội dung đang mở.</summary>
    public void Close()
    {
        _openDelay.Cancel();
        _closeDelay.Cancel();
        _slot.Close();
    }

    void HoverTrigger(int index, bool inside)
    {
        if (!inside) { if (_hovered == index) _hovered = -1; Evaluate(); return; }
        _hovered = index;
        if (_items[index].Links is null) { Evaluate(); return; }
        _closeDelay.Cancel();
        if (OpenIndex >= 0) { if (OpenIndex != index) Show(index); return; }
        _openDelay.Start(App, OpenDelay, () => { _openedByHover = true; Show(index); });
    }

    // Chuột không còn ở mục có tấm lẫn trong tấm: huỷ lần mở đang chờ, hẹn đóng. Quay lại kịp thì huỷ hẹn đóng.
    void Evaluate()
    {
        bool over = _inContent || (_hovered >= 0 && _hovered == OpenIndex);
        if (over) { _closeDelay.Cancel(); return; }
        if (_hovered < 0 || _items[_hovered].Links is null) _openDelay.Cancel();
        if (OpenIndex >= 0 && !_closeDelay.Pending) _closeDelay.Start(App, CloseDelay, _slot.Close);
    }

    void PickLink(int link)
    {
        if (OpenIndex < 0 || OpenLinks is not { } links || (uint)link >= (uint)links.Count) return;
        int item = _open;
        Close();
        Emit(new LinkSelected(item, link, links[link].Title));
    }

    bool TriggerKeyDown(int index, KeyEvent e)
    {
        if (_window is null) return false;
        if (e.Key == Key.Down && _items[index].Links is { Count: > 0 })
        {
            Show(index);
            _window.FocusKey(LinkKey(0)); // tấm vừa mở chưa được vẽ: Core giữ yêu cầu tới frame kế
            return true;
        }
        int next = RovingFocus.FromKey(e.Key, this, _items.Count, index, loop: false, vertical: false, static (_, _) => true);
        if (next == RovingFocus.NotNavigation) return false;
        if (next >= 0 && next != index) _window.FocusKey(TriggerKey(next));
        return true;
    }

    bool PanelKeyDown(KeyEvent e)
    {
        if (_window is null || OpenLinks is not { } links || e.Key is not (Key.Up or Key.Down or Key.Left or Key.Right or Key.Home or Key.End)) return false;
        int at = -1;
        for (int i = 0; i < links.Count && at < 0; i++) if (_window.IsFocused(LinkKey(i))) at = i;
        if (at == 0 && e.Key == Key.Up) { _window.FocusKey(TriggerKey(_open)); return true; }
        bool back = e.Key is Key.Up or Key.Left;
        int next = e.Key == Key.Home ? 0 : e.Key == Key.End ? links.Count - 1 : Math.Clamp(at + (back ? -1 : 1), 0, links.Count - 1);
        if (next != at) _window.FocusKey(LinkKey(next));
        return true;
    }
}
