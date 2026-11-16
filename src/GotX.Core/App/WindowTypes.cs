using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using GotX.Text;
using GotX.Widgets;

namespace GotX;

/// <summary>
/// Vùng bấm được: click, focus ô nhập, hoặc vùng cuộn. Lưu hộp layout + phép biến đổi lúc vẽ,
/// nên kiểm tra chuột chính xác cả khi phần tử bị xoay/phóng.
/// </summary>
internal struct Hitbox
{
    public Bounds Local, Clip;
    public Xf Xf;
    public Delegate? Handler;
    public object? State;
    public ClickInvoker? Invoke;
    public TextInput? Input;
    public long ScrollKey;
    public int ViewId;
    public bool Aux; // handler của nút phải / nút giữa (OnAuxClick)
    public bool Blocker;            // lớp modal: mọi hitbox đứng trước nó không nhận chuột
    public string? Anchor;          // chỉ để tra vị trí (.Anchor("tên")), không nhận chuột
    public DragInvoker? DragInvoke; // handler kéo chuột (OnDrag); Handler/State dùng chung
    // Mục bàn phím (IsKey): không nhận chuột. Handler/State/Invoke của nó là OnClick để Enter/Space kích hoạt.
    public bool IsKey, Focusable;
    public long FocusKey;           // định danh giữ qua các frame của phần tử focus được
    public Delegate? KeyHandler;    // OnKey
    public object? KeyState;
    public KeyInvoker? KeyInvoke;
    public int KeyParent;           // mục bàn phím bao ngoài (chỉ số 1-based trong danh sách hitbox, 0 = không có)
    public int ScrollParent;        // vùng cuộn bao ngoài (1-based, 0 = không có) – để cuộn phần tử vào tầm nhìn
    public readonly bool Contains(Point p) => Clip.Contains(p) && Local.Contains(Xf.Inverse(p));
}

/// <summary>Vùng cần theo dõi chuột: hover:, active:, group, cursor-*. Đổi trạng thái → đánh dấu bẩn view chứa nó.</summary>
internal struct HoverRegion
{
    public Bounds Local, Clip;
    public Xf Xf;
    public int ViewId;
    public bool Inside, HasActive, Pressed, Visual; // Visual = đổi trạng thái thì phải vẽ lại
    public Delegate? Handler;       // OnHover: gọi khi chuột vào / ra
    public object? State;
    public HoverInvoker? Invoke;
    public CursorKind Cursor;
    public readonly bool Contains(Point p) => Clip.Contains(p) && Local.Contains(Xf.Inverse(p));
}

/// <summary>Kết quả của một view ở frame trước: đủ để đo, đặt và vẽ lại mà không gọi Render().</summary>
internal struct ViewCache
{
    public int ParentView;
    public MeasureMemo Measures;
    public float MinContent;   // min-content của view ở frame trước (NaN = chưa tính)
    public Bounds Bounds, Clip;
    public Xf Xf;            // biến đổi lúc vẽ (cuộn, translate của tổ tiên)
    public int OuterShape;   // vùng cắt theo hình bao ngoài view (1-based trong scene của frame đó)
    public int ShapeStart, ShapeEnd; // vùng cắt theo hình tạo ra bên trong view
    public int GradStart, GradEnd;   // gradient tạo ra bên trong view
    public bool InnerClip;   // bên trong có vùng cắt bị vùng cắt ngoài xén mất → không thể chỉ tịnh tiến khi phát lại
    public TextStyle Text;
    public int PrimStart, PrimEnd, HitStart, HitEnd, HoverStart, HoverEnd, OrderStart, OrderEnd;
    // Ngữ cảnh lúc vào view để vẽ lại RIÊNG view này (khung một phần, Window.Partial.cs). KeyScope/ScrollScope: chỉ số hitbox 1-based.
    public int KeyScope, ScrollScope, GroupHover;
    public bool BlockHover;
}

/// <summary>Node View như layout đã thấy (Style của hộp bọc view) + kiểu chữ đã tinh chỉnh: đủ để đo/đặt/vẽ lại ruột view mà không cần cha.</summary>
internal struct ViewSnap
{
    public Style Style;
    public TextStyle Refined;
}

/// <summary>State của một phần tử giữ qua các frame. Frame = lần vẽ gần nhất còn dùng tới (để dọn state mồ côi).</summary>
internal abstract class ElementState
{
    public int Frame;
}

/// <summary>Đánh dấu phần tử AutoFocus đã xuất hiện (để chỉ tự focus ở frame đầu tiên).</summary>
internal sealed class FocusMark : ElementState;

/// <summary>Vị trí cuộn của một phần tử overflow-auto, giữ qua các frame. Cuộn mượt về Target.</summary>
internal sealed class ScrollState : ElementState
{
    public float X, Y, TargetX, TargetY, MaxX, MaxY;
    public Size Viewport;
    public float NotifiedX = float.NaN, NotifiedY, NotifiedMaxX, NotifiedMaxY; // giá trị đã báo cho OnScroll
    public double LastTime = double.NaN;
    public bool Moving;                        // còn đang cuộn mượt về đích sau lần cập nhật gần nhất
    public bool Hovered;                       // con trỏ đang ở trên vùng cuộn (chế độ scrollbar-hover)
    public double ShowUntil = double.NegativeInfinity; // thanh cuộn còn hiện tới lúc này (trễ ẩn 600ms như Radix)
    public float ShownX = float.NaN, ShownY;   // vị trí lúc vẽ lần trước (chế độ scrollbar-scroll)
}
