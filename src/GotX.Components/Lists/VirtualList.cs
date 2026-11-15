using System.Globalization;

namespace GotX.Components;

/// <summary>
/// Danh sách dọc ảo hoá: chỉ các hàng đang nhìn thấy (cộng <see cref="Overscan"/> hàng mỗi phía) thành node, nên 100.000
/// hàng vẫn chỉ tốn vài trăm node mỗi frame. Hai chế độ: chiều cao cố định (<see cref="ItemHeight"/>, vị trí = phép nhân) và
/// chiều cao thay đổi (<see cref="VariableHeight"/>: hàng chưa đo lấy <see cref="EstimatedHeight"/>, hàng đã vẽ được đo lại
/// bằng hình học thật và nhớ theo key; tổng chiều cao là ước lượng nên thanh cuộn nhích dần khi đo thêm).
/// Mỗi hàng mang key do <see cref="ItemKey"/> trả về (phải duy nhất và đi theo dữ liệu, không theo chỉ số) nên focus,
/// transition và vị trí cuộn không nhảy sang hàng khác khi dữ liệu đổi; sau khi đổi dữ liệu gọi <see cref="Changed"/>.
/// Thanh cuộn là của vùng cuộn Core (scrollbar-hover như <see cref="Ui.ScrollArea"/>). Phím ↑ ↓ PageUp PageDown Home End
/// di chuyển hàng "đang chọn" (<see cref="ActiveIndex"/>), Enter phát <see cref="Activated"/>. Hàng không tự bắt click:
/// cần thì đặt <c>OnClick</c> trong <see cref="RenderItem"/> rồi gán <see cref="ActiveIndex"/>.
/// <code>
/// _list = App.New(new VirtualList { Count = rows.Count, ItemKey = i =&gt; rows[i].Id, ItemHeight = 36, Class = "h-96",
///     RenderItem = (w, i, active) =&gt; w.Div().Class(active ? "bg-muted px-3" : "px-3").Child(rows[i].Name) });
/// </code>
/// </summary>
public sealed partial class VirtualList : View
{
    /// <summary>Hàng đang chọn (bằng phím) vừa đổi; -1 = không có.</summary>
    public readonly record struct ActiveChanged(int Index);

    /// <summary>Người dùng nhấn Enter trên hàng đang chọn.</summary>
    public readonly record struct Activated(int Index);

    /// <summary>Đưa hàng vào tầm nhìn: gần nhất (chỉ cuộn khi hàng ngoài khung), đầu khung, hoặc cuối khung.</summary>
    public enum ScrollAlign : byte { Nearest, Start, End }

    const float AssumedViewport = 400; // trước frame đầu chưa biết khung cao bao nhiêu

    int _count, _overscan = 6;
    float _itemHeight = 32, _estimated = 40;
    bool _variable;
    string _rowClass = "h-[32px] shrink-0 overflow-hidden";
    string? _key, _activeKey;
    int _active = -1;
    HeightIndex? _index;
    Window? _window;
    AfterFrame? _after;

    /// <summary>Số hàng. Đổi số hàng = dữ liệu đổi: tự gọi <see cref="Changed"/>.</summary>
    public int Count { get => _count; set { value = Math.Max(0, value); if (_count == value) return; _count = value; Changed(); } }

    /// <summary>Key duy nhất của hàng thứ i (ổn định theo dữ liệu). Bắt buộc.</summary>
    public Func<int, string>? ItemKey { get; set; }

    /// <summary>Dựng hàng thứ i; tham số cuối = hàng đang chọn. List tự gắn key, chiều cao (chế độ cố định) và shrink-0.</summary>
    public Func<Window, int, bool, Div>? RenderItem { get; set; }

    /// <summary>Chiều cao mỗi hàng (px) ở chế độ cố định.</summary>
    public float ItemHeight
    {
        get => _itemHeight;
        set
        {
            if (_itemHeight == value || value < 1) return;
            _itemHeight = value;
            _rowClass = "h-[" + value.ToString(CultureInfo.InvariantCulture) + "px] shrink-0 overflow-hidden";
            if (!_variable) { _index = null; Notify(); }
        }
    }

    /// <summary>Bật chế độ chiều cao thay đổi: hàng tự cao theo nội dung và được đo lại sau khi vẽ.</summary>
    public bool VariableHeight { get => _variable; set { if (_variable == value) return; _variable = value; _index = null; Notify(); } }

    /// <summary>Chiều cao giả định (px) cho hàng chưa đo ở chế độ thay đổi.</summary>
    public float EstimatedHeight { get => _estimated; set { if (value < 1 || _estimated == value) return; _estimated = value; if (_variable) { _index = null; Notify(); } } }

    /// <summary>Số hàng dựng thêm ngoài khung nhìn mỗi phía (đệm khi cuộn nhanh).</summary>
    public int Overscan { get => _overscan; set { _overscan = Math.Max(1, value); Notify(); } }

    /// <summary>Class thêm cho khung ngoài – thường là chiều cao.</summary>
    public string Class { get; set; } = "h-80";

    /// <summary>Key của khung ngoài nhận focus bàn phím.</summary>
    public string FocusKey => _key ??= "gotx-virtual-list-" + Id.Value;

    /// <summary>Key của vùng cuộn (cho <c>Window.GetScroll</c> / <c>ScrollTo</c>).</summary>
    public string ScrollKey => FocusKey + "-scroll";

    /// <summary>Chỉ số hàng đang chọn; -1 = không có.</summary>
    public int ActiveIndex
    {
        get => _active;
        set
        {
            value = value < 0 || _count == 0 ? -1 : Math.Min(value, _count - 1);
            if (_active == value) return;
            _active = value;
            _activeKey = value >= 0 ? ItemKey?.Invoke(value) : null;
            Notify();
            Emit(new ActiveChanged(value));
        }
    }

    /// <summary>
    /// Báo dữ liệu đã đổi (thêm/xoá/sắp xếp lại): dựng lại bảng chiều cao, tìm lại hàng đang chọn theo key. Vị trí cuộn giữ
    /// nguyên (vùng cuộn có key) và bị kẹp vào phạm vi mới.
    /// </summary>
    public void Changed(bool keepAnchor = true)
    {
        // Hàng đầu khung (key + độ lệch bên trong hàng) là mỏ neo: thêm/xoá phía trên không làm nội dung đang đọc trôi đi.
        string? anchor = null;
        double within = 0;
        int from = FirstVisible;
        if (keepAnchor && _index is { } old && _window?.GetScroll(ScrollKey) is { Offset.Y: > 0 } info && (uint)(from - _start) < (uint)(_end - _start))
        {
            anchor = _renderedKeys[from - _start];
            within = info.Offset.Y - old.Offset(from);
        }
        // Chế độ cố định chỉ phụ thuộc số hàng: giữ nguyên bảng nếu số hàng không đổi.
        if (_variable || _index is null || _index.Count != _count) _index = null;
        if (anchor is not null && FindKey(anchor, from) is var at and >= 0 && at != from)
        {
            _targetY = (float)(Ensure().Offset(at) + within);
            _sendTarget = true;
            _animateTarget = false;
        }
        if (_activeKey is { } key && ItemKey is { } keyOf)
        {
            int found = -1;
            for (int i = 0; i < _count && found < 0; i++) if (keyOf(i) == key) found = i;
            if (found != _active) { _active = found; if (found < 0) _activeKey = null; Emit(new ActiveChanged(found)); }
        }
        else if (_active >= _count) _active = -1;
        Notify();
    }

    /// <summary>Chỉ số hàng nhìn thấy đầu / cuối ở lần dựng gần nhất; hàng được dựng là [<see cref="FirstRendered"/>, <see cref="EndRendered"/>).</summary>
    public int FirstVisible { get; private set; }
    public int LastVisible { get; private set; }
    public int FirstRendered => _start;
    public int EndRendered => _end;

    /// <summary>Tổng chiều cao nội dung (px): chính xác ở chế độ cố định, ước lượng ở chế độ thay đổi.</summary>
    public double ContentHeight => Ensure().Total;

    protected override void OnCreated() => _after = AfterFrame.Create(App, OnAfterFrame);

    HeightIndex Ensure()
    {
        if (_index is { } ready) return ready;
        var index = new HeightIndex(_count, _variable ? _estimated : _itemHeight);
        if (_variable && _known.Count > 0 && ItemKey is { } keyOf)
            for (int i = 0; i < _count; i++) if (_known.TryGetValue(keyOf(i), out float h)) index.Set(i, h);
        return _index = index;
    }
}
