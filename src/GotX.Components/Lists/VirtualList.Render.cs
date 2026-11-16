namespace GotX.Components;

/// <summary>Phần dựng của <see cref="VirtualList"/>: tính khoảng hàng cần dựng, đệm trên/dưới, các hàng.</summary>
public sealed partial class VirtualList
{
    // Đệm có chiều cao tuỳ ý mà không sinh class mới: tách chiều cao (số nguyên px) thành các bit, mỗi bit là một div
    // h-[2^k px] dùng chung 25 chuỗi dựng sẵn. Chuỗi mới mỗi vị trí cuộn sẽ làm tràn bộ nhớ đệm class (Tailwind.MaxCachedClassLists).
    static readonly string[] PowerClasses = BuildPowerClasses();

    static string[] BuildPowerClasses()
    {
        var classes = new string[25];
        for (int k = 0; k < classes.Length; k++) classes[k] = "h-[" + (1 << k) + "px] shrink-0";
        return classes;
    }

    static void Spacer(Window w, Div parent, double height)
    {
        int px = (int)Math.Min(height, (1 << 25) - 1);
        for (int k = 0; px != 0; k++, px >>= 1)
            if ((px & 1) != 0) parent.Child(w.Div().Class(PowerClasses[k]));
    }

    int _start, _end;
    float _targetY = float.NaN; // vị trí cuộn đã yêu cầu bằng code, chưa tới nơi: NaN = không có
    string[] _renderedKeys = [];
    bool _warnedUnbounded, _unbounded;

    static void Range(HeightIndex index, double y, double viewport, out int first, out int last)
    {
        first = index.IndexAt(y);
        last = index.IndexAt(y + Math.Max(viewport, 1));
    }

    public override Div Render(Window w)
    {
        _window = w;
        var index = Ensure();
        string scrollKey = ScrollKey;
        var scroll = w.GetScroll(scrollKey);
        double viewport = scroll?.Viewport.Height is { } vh and > 0 ? vh : AssumedViewport;
        double y = float.IsNaN(_targetY) ? scroll?.Offset.Y ?? 0 : _targetY;
        y = Math.Clamp(y, 0, Math.Max(0, index.Total - viewport));
        SendScroll(w);
        _targetY = float.NaN; // chỉ dùng cho đúng lần dựng này; từ frame sau vị trí thật đã nằm trong Window
        if (CheckBounded(w, index, viewport)) viewport = Math.Max(1, Math.Min(viewport, w.LogicalSize.Height));
        Range(index, y, viewport, out int first, out int last);
        FirstVisible = first;
        LastVisible = last;
        _start = Math.Max(0, first - _overscan);
        _end = _count == 0 ? 0 : Math.Min(_count, last + _overscan + 1);

        var area = Ui.ScrollArea(w, scrollKey).Class("size-full min-h-0 min-w-0").Anchor(scrollKey)
            .OnScroll(this, static (l, info) => l.OnScrolled(info));
        Spacer(w, area, index.Offset(_start));
        if (_renderedKeys.Length < _end - _start) _renderedKeys = new string[Math.Max(_end - _start, _renderedKeys.Length * 2)];
        if (ItemKey is { } keyOf && RenderItem is { } render)
            for (int i = _start; i < _end; i++)
            {
                string key = _renderedKeys[i - _start] = keyOf(i);
                var row = render(w, i, i == _active).Key(key).Class(_variable ? "shrink-0" : _rowClass).Role(SemanticRole.ListItem).AriaState(SemanticState.Selected, i == _active);
                area.Child(_variable ? row.Anchor(key) : row);
            }
        Spacer(w, area, index.Total - index.Offset(_end));
        _after?.Request();
        return w.Div().Class("relative flex min-h-0 flex-col rounded-md outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50")
            .Class(Class).Class(_unbounded ? "max-h-screen" : "").Key(FocusKey).Focusable().Role(SemanticRole.List)
            .OnKey(this, static (l, e) => l.OnKey(e))
            .Child(area);
    }

    // Không có chiều cao xác định (vd cha cao theo nội dung): vùng cuộn cao bằng nội dung nên "khung nhìn" đo được = tổng chiều cao,
    // và mọi hàng được dựng (100.000 hàng = 3.5 s/frame). Khung nhìn phủ trọn danh sách mà cao hơn hai lần cửa sổ là dấu hiệu chắc chắn:
    // chốt chế độ dự phòng (khung ngoài max-h-screen, khung nhìn = chiều cao cửa sổ) để chỉ dựng hàng nhìn thấy, và báo một lần.
    bool CheckBounded(Window w, HeightIndex index, double viewport)
    {
        if (!_unbounded)
        {
            double screen = w.LogicalSize.Height;
            if (_count <= 200 || screen <= 0 || viewport < index.Total - 1 || viewport <= 2 * screen) return false;
            _unbounded = true;
        }
        if (_warnedUnbounded) return true;
        _warnedUnbounded = true;
        w.Warn("virtual-list-unbounded", $"VirtualList \"{ScrollKey}\" không có chiều cao xác định; tạm giới hạn max-h-screen để chỉ dựng hàng nhìn thấy. Đặt Class = \"h-96\" (hoặc cha có chiều cao + min-h-0 + flex-1).");
        return true;
    }

    // Cuộn (hoặc khung đổi cỡ) làm hàng nhìn thấy tới sát mép vùng đã dựng thì dựng lại; nửa đệm là lề an toàn cho một lần lăn.
    void CheckRange(float offset, float viewport)
    {
        var index = Ensure();
        Range(index, offset, viewport, out int first, out int last);
        int margin = Math.Max(1, _overscan / 2);
        bool needStart = _start > 0 && first < _start + margin, needEnd = _end < _count && last >= _end - margin;
        if (needStart || needEnd || first < _start || last >= _end) Notify();
    }

    void OnScrolled(ScrollInfo info)
    {
        CheckRange(info.Offset.Y, info.Viewport.Height);
    }
}
