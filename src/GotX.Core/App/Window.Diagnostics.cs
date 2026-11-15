using System.Diagnostics;

namespace GotX;

/// <summary>
/// Móc chẩn đoán cho công cụ test/debug (GotX.Test): dòng sự kiện, số liệu frame, ảnh chụp cây giao diện.
/// Khi không ai dùng thì chỉ tốn vài phép so sánh null mỗi frame.
/// </summary>
public sealed partial class Window
{
    List<UiNode>? _inspect;
    readonly List<int> _inspectStack = new();
    long _layoutTicks, _paintTicks;
    // Số primitive lúc sự kiện soi gần nhất (bắt đầu/kết thúc một node). Nền, viền, chữ của một node được vẽ TRƯỚC khi
    // InspectBegin của nó chạy, nên đoạn primitive của node phải tính từ mốc này chứ không phải từ _scene.Count hiện tại.
    int _inspectMark;

    /// <summary>Nhận mọi sự việc đáng kể: input, click, focus, view render / bị đánh dấu bẩn, overlay mở/đóng.</summary>
    public Action<TraceEvent>? Trace { get; set; }

    /// <summary>Số liệu của lần Draw gần nhất.</summary>
    public FrameStats LastFrame { get; private set; }

    void Emit(TraceKind kind, string message, Bounds bounds = default, int viewId = 0) =>
        Trace?.Invoke(new TraceEvent(kind, message, bounds, viewId, _frame));

    void TraceInput(in InputEvent e)
    {
        string what = e.Kind switch
        {
            InputKind.KeyDown => $"{e.Kind} {e.Key}" + (e.Modifiers != Modifiers.None ? $" +{e.Modifiers}" : ""),
            InputKind.TextInput or InputKind.ImeCompose => $"{e.Kind} \"{e.Text}\"",
            InputKind.Scroll => $"{e.Kind} {e.Delta.X:0.#},{e.Delta.Y:0.#} tại {e.Position.X:0.#},{e.Position.Y:0.#}",
            InputKind.MouseLeave => e.Kind.ToString(),
            _ => $"{e.Kind} {e.Button} tại {e.Position.X:0.#},{e.Position.Y:0.#}",
        };
        Emit(e.Kind == InputKind.KeyDown ? TraceKind.Key : TraceKind.Input, what);
    }

    void RecordFrame(long startTicks)
    {
        double ms = 1000.0 / Stopwatch.Frequency;
        LastFrame = new FrameStats(_frame, _layoutTicks * ms, _paintTicks * ms, (Stopwatch.GetTimestamp() - startTicks) * ms,
            _rendered, _replayed, _tree.Count, _scene.Count, _scene.ShapeCount, _hitboxes.Count, _hovers.Count,
            LastFrameAllocatedBytes, WantsAnimationFrame);
        if (Trace is not null)
            Emit(TraceKind.Frame, $"render {_rendered}, phát lại {_replayed}, {_scene.Count} primitive, {LastFrame.TotalMs:0.00} ms");
    }

    /// <summary>
    /// Vẽ một frame với cache tắt và ghi lại mọi phần tử: hộp layout, hộp thật trên màn hình, vùng cắt, class,
    /// kiểu đã tính, đoạn primitive nó sinh ra. Dùng để soi lệch pixel, tràn, bị che… mà không cần nhìn ảnh.
    /// Có tác dụng phụ như một frame bình thường (animation tiến theo đồng hồ, OnScroll có thể được gọi).
    /// </summary>
    public UiSnapshot Inspect(Size logicalSize, float scale)
    {
        bool cache = CacheEnabled;
        var nodes = new List<UiNode>();
        _inspect = nodes;
        _inspectStack.Clear();
        _inspectMark = 0;
        _tree.Diagnostics = true;
        CacheEnabled = false;
        try { return new UiSnapshot(nodes, logicalSize, scale, Draw(logicalSize, scale)); }
        finally
        {
            _inspect = null;
            _tree.Diagnostics = false;
            CacheEnabled = cache;
        }
    }

    void InspectBegin(int id, ref Node n, in Style s, in TextStyle ts, Bounds lb, Bounds clip, Xf xf)
    {
        var nodes = _inspect!;
        int parent = _inspectStack.Count > 0 ? _inspectStack[^1] : -1;
        var pad = s.Padding.Resolve(lb.Width, RemSize);
        var node = new UiNode
        {
            Index = nodes.Count, Parent = parent, Depth = _inspectStack.Count,
            Kind = n.Kind switch
            {
                NodeKind.Text => "text", NodeKind.View => "view", NodeKind.Input => "input",
                _ => n.Vector is not null ? "path" : n.Image is not null ? "image" : "div",
            },
            Key = _tree.KeyName(n.Key), View = n.View?.GetType().Name, Classes = n.ClassText, Text = n.Text ?? n.Input?.Text,
            Anchor = n.Anchor, Layout = lb, Screen = PaintedBounds(lb, xf), Clip = clip, ContentSize = n.ContentSize,
            Clickable = n.Click is not null && !n.Disabled, Focusable = n.Focusable && !n.Disabled, Disabled = n.Disabled,
            Scrollable = s.ScrollX || s.ScrollY, Transformed = !xf.IsSimple,
            Display = s.Display, AlignItems = s.AlignItems, AlignSelf = s.AlignSelf, Position = s.Position, Overflow = s.Overflow,
            Background = s.Background, Color = ts.Color, Border = s.BorderBox, Padding = pad, Radius = s.CornerRadii,
            FontSize = ts.FontSize, LineHeight = ts.LineHeight, Weight = ts.Weight, Opacity = ts.Opacity,
            NoWrap = ts.NoWrap, Ellipsis = ts.Ellipsis, TextStyle = ts,
            PrimStart = _inspectMark,
        };
        _inspectMark = _scene.Count;
        node.Hovered = !_blockHover && node.Visible.Contains(_mouse);
        if (_hitboxes.Count > 0 && _hitboxes[^1] is { IsKey: true } key && key.Local == lb) node.Focused = key.FocusKey == _focusKey;
        if (n.FocusInput is { } input && input == Focused) node.Focused = true;
        if (parent >= 0) nodes[parent].Children.Add(node.Index);
        nodes.Add(node);
        _inspectStack.Add(node.Index);
    }

    /// <summary>Hộp thật trên màn hình đúng như lúc vẽ: mép đã bắt vào lưới pixel thiết bị (xem <see cref="SnapToPixels"/>).</summary>
    Bounds PaintedBounds(Bounds lb, Xf xf)
    {
        var screen = xf.ApplyAabb(lb);
        return xf.IsSimple && xf.S == 1 ? SnapToPixels(screen.Scale(_scale)).Scale(1 / _scale) : screen;
    }

    void InspectEnd()
    {
        int index = _inspectStack[^1];
        _inspectStack.RemoveAt(_inspectStack.Count - 1);
        _inspect![index].PrimEnd = _inspectMark = _scene.Count;
    }
}