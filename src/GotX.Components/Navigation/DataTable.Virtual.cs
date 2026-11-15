namespace GotX.Components;

/// <summary>
/// Chế độ ảo hoá của <see cref="DataTable{T}"/> (<see cref="Virtualized"/>): thay phân trang bằng một <see cref="VirtualList"/>
/// cuộn dọc trong khung bảng, tiêu đề đứng yên phía trên. Lọc, sắp xếp, chọn hàng, ẩn/hiện cột, menu hàng giữ nguyên;
/// ô đánh dấu ở tiêu đề chọn mọi hàng sau khi lọc. Hàng giữ key <see cref="RowKey"/> nên focus và vị trí cuộn đi theo dữ liệu.
/// </summary>
public sealed partial class DataTable<T>
{
    VirtualList? _virtual;
    bool _virtualized;
    float _virtualRowHeight = 49; // 1px viền + ô p-2 với chữ text-sm (36px nút) như hàng của shadcn

    /// <summary>Bật cuộn ảo: mọi hàng sau khi lọc nằm trong một vùng cuộn, chỉ hàng nhìn thấy mới thành node. Tắt <see cref="ShowPagination"/> ngầm.</summary>
    public bool Virtualized { get => _virtualized; set { if (_virtualized == value) return; _virtualized = value; Notify(); } }

    /// <summary>Chiều cao mỗi hàng (px) ở chế độ ảo hoá; hàng cắt phần thừa nên ô cao hơn thì tăng giá trị này.</summary>
    public float VirtualRowHeight { get => _virtualRowHeight; set { _virtualRowHeight = value; if (_virtual is not null) _virtual.ItemHeight = value; } }

    /// <summary>Class chiều cao của vùng cuộn ở chế độ ảo hoá.</summary>
    public string VirtualClass { get; set; } = "h-96";

    /// <summary>Danh sách ảo đứng sau bảng (null khi chưa vẽ ở chế độ ảo hoá) – để đọc số hàng được dựng, cuộn tới hàng.</summary>
    public VirtualList? List => _virtual;

    /// <summary>Cuộn tới hàng ở vị trí <paramref name="position"/> sau khi lọc và sắp xếp (chế độ ảo hoá).</summary>
    public void ScrollToRow(int position) { Refresh(); _virtual?.ScrollToIndex(position); }

    int PageStart => _virtualized ? 0 : _page * _pageSize;
    int PageEnd => _virtualized ? _count : Math.Min(_count, _page * _pageSize + _pageSize);

    VirtualList EnsureVirtual() => _virtual ??= App.New(new VirtualList
    {
        ItemHeight = _virtualRowHeight, Class = VirtualClass,
        ItemKey = VirtualKey, RenderItem = VirtualRow, Count = _count,
    });

    string VirtualKey(int position) => RowKey(_order[position]);

    Div VirtualRow(Window w, int position, bool active) =>
        BodyRow(w, position, Indexed<DataTable<T>>.Ensure(ref _rowRefs, this, _items.Count), Grid(), false);

    // Dữ liệu/lọc/sắp xếp vừa đổi: báo cho danh sách ảo số hàng mới (và dựng lại bảng chiều cao theo key).
    void SyncVirtual()
    {
        if (_virtual is null) return;
        _virtual.Count = _count;
        _virtual.Changed(keepAnchor: false);
        _virtual.ScrollToIndex(0, VirtualList.ScrollAlign.Start); // như phân trang về trang 1 khi lọc/sắp xếp/đổi dữ liệu
    }

    Div VirtualBody(Window w)
    {
        var body = Ui.TableBody(w);
        if (_count == 0) return body.Child(w.Div().Key(EmptyKey).Class("flex h-24 items-center justify-center text-sm").Child(EmptyText));
        var list = EnsureVirtual();
        list.Class = VirtualClass;
        return body.Child(w.View(list));
    }
}
