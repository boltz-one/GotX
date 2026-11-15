namespace GotX;

public enum TraceKind : byte { Input, Click, Key, Focus, ViewRender, ViewDirty, Overlay, Scroll, Frame }

/// <summary>Một sự việc bên trong Window, phát qua <see cref="Window.Trace"/> để công cụ test/debug dựng lại chuỗi nhân quả.</summary>
public readonly record struct TraceEvent(TraceKind Kind, string Message, Bounds Bounds, int ViewId, int Frame)
{
    public override string ToString() =>
        $"[{Frame}] {Kind}: {Message}" + (ViewId > 0 ? $" (view #{ViewId})" : "") +
        (Bounds.Width > 0 ? $" @ {Bounds.X:0.#},{Bounds.Y:0.#} {Bounds.Width:0.#}×{Bounds.Height:0.#}" : "");
}

/// <summary>Số liệu của một lần <see cref="Window.Draw"/>.</summary>
public readonly record struct FrameStats(
    int Frame, double LayoutMs, double PaintMs, double TotalMs,
    int RenderedViews, int ReplayedViews, int Nodes, int Primitives, int ClipShapes,
    int Hitboxes, int HoverRegions, long AllocatedBytes, bool Animating);

/// <summary>Một phần tử trong ảnh chụp cây giao diện (<see cref="Window.Inspect"/>). Toạ độ là logical px.</summary>
public sealed class UiNode
{
    public int Index, Parent = -1, Depth;
    /// <summary>div, text, view, input, path, image.</summary>
    public string Kind = "div";
    public string? Key, View, Classes, Text, Anchor;
    /// <summary>Hộp layout (trước cuộn/transform) và hộp thật trên màn hình.</summary>
    public Bounds Layout, Screen;
    /// <summary>Vùng cắt đang áp lên phần tử (toạ độ màn hình).</summary>
    public Bounds Clip;
    public Size ContentSize;
    public bool Clickable, Focusable, Focused, Scrollable, Disabled, Transformed, Hovered;
    public Display Display;
    public Position Position;
    public Rgba Background, Color;
    public Edges Border, Padding;
    public Corners Radius;
    public float FontSize, LineHeight, Opacity;
    public FontWeight Weight;
    public bool NoWrap, Ellipsis;
    /// <summary>Kiểu chữ đầy đủ đã tính (họ font, nghiêng, tracking, hoa/thường…) – để công cụ đo lại chữ đúng như lúc vẽ.</summary>
    public TextStyle TextStyle;
    public Overflow Overflow;
    /// <summary>Đoạn primitive [PrimStart, PrimEnd) trong Scene do phần tử này và cây con của nó vẽ ra.</summary>
    public int PrimStart, PrimEnd;
    public readonly List<int> Children = new();

    /// <summary>Phần nhìn thấy được trên màn hình (hộp thật giao với vùng cắt).</summary>
    public Bounds Visible => Screen.Intersect(Clip);
}

/// <summary>Ảnh chụp toàn bộ cây giao diện của một frame, kèm Scene đã dựng.</summary>
public sealed class UiSnapshot(List<UiNode> nodes, Size viewport, float scale, Scene scene)
{
    public IReadOnlyList<UiNode> Nodes { get; } = nodes;
    public Size Viewport { get; } = viewport;
    public float Scale { get; } = scale;
    public Scene Scene { get; } = scene;

    public UiNode? Find(string key)
    {
        foreach (var n in Nodes) if (n.Key == key) return n;
        return null;
    }
}
