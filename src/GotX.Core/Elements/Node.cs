using GotX.Widgets;

namespace GotX;

/// <summary>Nhớ tối đa 4 kết quả đo (kích thước khả dụng → kích thước) – inline, không cấp phát.</summary>
internal struct MeasureMemo
{
    float _w0, _h0, _w1, _h1, _w2, _h2, _w3, _h3;
    Size _s0, _s1, _s2, _s3;
    byte _count, _next;

    public readonly bool TryGet(float w, float h, out Size s)
    {
        if (_count > 0 && _w0.Equals(w) && _h0.Equals(h)) { s = _s0; return true; }
        if (_count > 1 && _w1.Equals(w) && _h1.Equals(h)) { s = _s1; return true; }
        if (_count > 2 && _w2.Equals(w) && _h2.Equals(h)) { s = _s2; return true; }
        if (_count > 3 && _w3.Equals(w) && _h3.Equals(h)) { s = _s3; return true; }
        s = default; return false;
    }

    public readonly int Count => _count;

    public readonly void At(int i, out float w, out float h, out Size s)
    {
        switch (i)
        {
            case 0: w = _w0; h = _h0; s = _s0; break;
            case 1: w = _w1; h = _h1; s = _s1; break;
            case 2: w = _w2; h = _h2; s = _s2; break;
            default: w = _w3; h = _h3; s = _s3; break;
        }
    }

    public void Add(float w, float h, Size s)
    {
        switch (_next)
        {
            case 0: _w0 = w; _h0 = h; _s0 = s; break;
            case 1: _w1 = w; _h1 = h; _s1 = s; break;
            case 2: _w2 = w; _h2 = h; _s2 = s; break;
            default: _w3 = w; _h3 = h; _s3 = s; break;
        }
        _next = (byte)((_next + 1) & 3);
        if (_count < 4) _count++;
    }
}

internal enum NodeKind : byte { Div, Text, View, Input }

internal struct Node
{
    public NodeKind Kind;
    public Style Style;
    public string? Text;
    public View? View;
    public TextInput? Input;
    public TextInput? FocusInput; // bấm vào node này sẽ focus ô nhập liệu
    public ClassList? Classes;    // class có variant trạng thái (hover:, active:…) – đánh giá lúc vẽ
    public bool IsGroup;          // class "group": con có thể dùng group-hover:
    public bool Disabled;
    public int FirstChild, LastChild, NextSibling;
    public int Sem;                // chỉ số vào bảng ngữ nghĩa của ElementTree (0 = không có); chỉ ghi khi đang dựng cây ngữ nghĩa
    public StyleRefiner? Hover;
    public Delegate? Click;
    public object? ClickState;
    public ClickInvoker? ClickInvoke;
    public Delegate? AuxClick;    // click bằng nút phải / nút giữa
    public object? AuxState;
    public ClickInvoker? AuxInvoke;
    public Delegate? HoverHandler; // OnHover
    public object? HoverState;
    public HoverInvoker? HoverInvoke;
    public Delegate? DragHandler;  // OnDrag
    public object? DragState;
    public DragInvoker? DragInvoke;
    public string? Anchor;         // tên để overlay neo vào
    public VectorPath? Vector;     // hình vector vẽ bằng màu chữ (icon, đường biểu đồ)
    public ImageSource? Image;     // ảnh bitmap
    public Delegate? ScrollHandler; // OnScroll
    public object? ScrollState;
    public ScrollInvoker? ScrollInvoke;
    public bool Focusable;         // điểm dừng của Tab; Enter/Space kích hoạt OnClick
    public bool AutoFocus;         // nhận focus khi vừa xuất hiện
    public Delegate? KeyHandler;   // OnKey
    public object? KeyState;
    public KeyInvoker? KeyInvoke;
    public string? ClassText;      // chuỗi class gốc – chỉ ghi khi ElementTree.Diagnostics bật

    // Kết quả layout của frame hiện tại (logical px)
    public Bounds Bounds;
    public Size ContentSize;      // vùng cuộn: kích thước nội dung
    public int Key;               // khoá do .Key(...) đặt (giữ state cuộn / transition qua các frame)
    public TextStyle InheritedText; // kiểu chữ cha truyền xuống (khoá cache của view)

    // Các lần đo trong frame (cha có thể đo con với vài kích thước khả dụng khác nhau)
    public MeasureMemo Memo;
    public float MinContent;      // bề rộng tối thiểu theo nội dung (min-content), hợp lệ khi HasMinContent
    public bool HasMinContent;

    // Riêng node View
    public bool ViewRendered; // đã gọi Render() trong frame này
    public bool Replay;       // dùng lại kết quả vẽ của frame trước
}
