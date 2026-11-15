namespace GotX.Components;

/// <summary>
/// Phân trang: nút Trước/Sau, các số trang (trang đầu, cuối, quanh trang hiện tại) và dấu "…" cho đoạn bị ẩn.
/// Số ô hiển thị giữ cố định khi đổi trang để các nút không nhảy chỗ dưới con trỏ. Trang đánh số từ 1.
/// Mọi nút là một điểm dừng Tab (nút bị vô hiệu thì bỏ qua), Enter/Space kích hoạt – như các thẻ &lt;a&gt; của bản gốc.
/// </summary>
public sealed class Pagination : View
{
    // Ghép một lần lúc khởi tạo kiểu: Prev/Next là nút ghost cỡ thường, đệm phía icon hẹp hơn như shadcn.
    static readonly string PrevClass = Ui.ButtonClasses(ButtonVariant.Ghost) + " pl-1.5 " + FocusRing.Ring;
    static readonly string NextClass = Ui.ButtonClasses(ButtonVariant.Ghost) + " pr-1.5 " + FocusRing.Ring;
    static readonly string LinkClass = Ui.ButtonClasses(ButtonVariant.Ghost, ButtonSize.Icon) + " " + FocusRing.Ring;
    static readonly string ActiveClass = Ui.ButtonClasses(ButtonVariant.Outline, ButtonSize.Icon) + " " + FocusRing.Ring;

    // Như bản gốc (hidden sm:block): cửa sổ hẹp hơn 640px thì Trước/Sau chỉ còn mũi tên.
    const string LabelClass = "hidden sm:block";

    int _page = 1, _count = 1;
    readonly List<int> _slots = new();
    string[] _labels = [], _keys = [];
    Indexed<Pagination>[]? _refs;

    /// <summary>Phát khi trang hiện tại đổi.</summary>
    public readonly record struct PageChanged(int Page);

    /// <summary>Tổng số trang (≥ 1); trang hiện tại bị kẹp lại nếu vượt.</summary>
    public int PageCount
    {
        get => _count;
        set { _count = Math.Max(1, value); if (_page > _count) Page = _count; Notify(); }
    }

    /// <summary>Trang hiện tại, kẹp trong [1, PageCount].</summary>
    public int Page
    {
        get => _page;
        set
        {
            value = Math.Clamp(value, 1, _count);
            if (_page == value) return;
            _page = value;
            Notify();
            Emit(new PageChanged(value));
        }
    }

    /// <summary>Số trang hiện hai bên trang hiện tại.</summary>
    public int SiblingCount { get; set; } = 1;

    /// <summary>Chữ trên nút lùi.</summary>
    public string PreviousLabel { get; set; } = "Trước";

    /// <summary>Chữ trên nút tiến.</summary>
    public string NextLabel { get; set; } = "Sau";

    /// <summary>Lùi một trang (không làm gì ở trang 1).</summary>
    public void Previous() => Page = _page - 1;

    /// <summary>Tiến một trang (không làm gì ở trang cuối).</summary>
    public void Next() => Page = _page + 1;

    /// <summary>Key nút lùi (kiểm tra headless).</summary>
    public string PreviousKey => EnsureCache()[0];

    /// <summary>Key nút tiến (kiểm tra headless).</summary>
    public string NextKey => EnsureCache()[_count + 1];

    /// <summary>Key nút số trang <paramref name="page"/> (chỉ tồn tại khi trang đó đang hiển thị).</summary>
    public string PageKey(int page) => EnsureCache()[page];

    // Chuỗi số trang và key tạo một lần cho mỗi PageCount: [0] = prev, [1..count] = trang, [count+1] = next.
    string[] EnsureCache()
    {
        if (_keys.Length == _count + 2) return _keys;
        _keys = new string[_count + 2];
        _labels = new string[_count + 1];
        _keys[0] = $"gotx-pagination-{Id.Value}-prev";
        _keys[_count + 1] = $"gotx-pagination-{Id.Value}-next";
        for (int p = 1; p <= _count; p++)
        {
            _keys[p] = $"gotx-pagination-{Id.Value}-{p}";
            _labels[p] = p.ToString();
        }
        return _keys;
    }

    /// <summary>Danh sách ô cần vẽ: số trang, hoặc 0 cho dấu "…". Luôn có trang đầu và trang cuối.</summary>
    public static void Slots(List<int> slots, int page, int count, int siblings)
    {
        slots.Clear();
        if (count <= siblings * 2 + 5)
        {
            for (int p = 1; p <= count; p++) slots.Add(p);
            return;
        }
        int left = Math.Max(page - siblings, 2), right = Math.Min(page + siblings, count - 1);
        // Gần hai đầu thì kéo dài đoạn giữa thay cho dấu "…" để tổng số ô không đổi.
        if (left <= 3) { left = 2; right = Math.Max(right, 3 + siblings * 2); }
        if (right >= count - 2) { right = count - 1; left = Math.Min(left, count - 2 - siblings * 2); }
        slots.Add(1);
        if (left > 2) slots.Add(0);
        for (int p = left; p <= right; p++) slots.Add(p);
        if (right < count - 1) slots.Add(0);
        slots.Add(count);
    }

    /// <summary>Vẽ hàng phân trang căn giữa.</summary>
    public override Div Render(Window w)
    {
        var keys = EnsureCache();
        var refs = Indexed<Pagination>.Ensure(ref _refs, this, _count + 1);
        Slots(_slots, _page, _count, SiblingCount);

        // Một hàng tự xuống dòng và căn giữa: ở chỗ hẹp các nút rớt xuống hàng dưới thay vì tràn ra ngoài khung.
        var row = w.Div().Class("flex w-full flex-wrap items-center justify-center gap-0.5");
        row.Child(w.Div().Key(keys[0]).Class(PrevClass).Disabled(_page <= 1).Focusable()
            .OnClick(this, static (p, _) => p.Previous())
            .Child(Icon.ChevronLeft(w)).Child(w.Div().Class(LabelClass).Child(PreviousLabel)));
        foreach (int p in _slots)
        {
            if (p == 0)
            {
                row.Child(w.Div().Class("flex size-8 shrink-0 items-center justify-center").Child(Icon.MoreHorizontal(w)));
                continue;
            }
            row.Child(w.Div().Key(keys[p]).Class(p == _page ? ActiveClass : LinkClass).Focusable()
                .OnClick(refs[p], static (r, _) => r.Owner.Page = r.Index)
                .Child(_labels[p]));
        }
        row.Child(w.Div().Key(keys[_count + 1]).Class(NextClass).Disabled(_page >= _count).Focusable()
            .OnClick(this, static (p, _) => p.Next())
            .Child(w.Div().Class(LabelClass).Child(NextLabel)).Child(Icon.ChevronRight(w)));
        return row;
    }
}
