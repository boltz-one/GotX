using GotX.Widgets;

namespace GotX;

/// <summary>Cách một overlay hiển thị và bị đóng.</summary>
public sealed class OverlayOptions
{
    /// <summary>Modal: có backdrop, chặn chuột và hover với mọi thứ bên dưới (Dialog, Drawer, Sheet).</summary>
    public bool Modal { get; set; }

    /// <summary>Class của lớp nền mờ khi Modal.</summary>
    public string BackdropClass { get; set; } = "bg-black/50";

    /// <summary>
    /// Bố cục nội dung trong viewport khi không có anchor: mặc định căn giữa (dialog).
    /// Drawer bên phải: <c>"flex justify-end"</c>; sheet đáy: <c>"flex items-end"</c>.
    /// </summary>
    public string LayerClass { get; set; } = "flex items-center justify-center";

    public bool DismissOnEscape { get; set; } = true;
    public bool DismissOnOutsideClick { get; set; } = true;

    /// <summary>Tên anchor (đặt bằng <c>.Anchor("tên")</c>) để neo nội dung vào; null = bố cục theo <see cref="LayerClass"/>.</summary>
    public string? Anchor { get; set; }

    /// <summary>Neo vào một hình chữ nhật cố định (logical px), ví dụ vị trí chuột của context menu.</summary>
    public Bounds? AnchorRect { get; set; }

    public Placement Placement { get; set; } = Placement.BottomStart;
    public float Offset { get; set; } = 4;

    /// <summary>Lề tối thiểu so với mép cửa sổ khi định vị theo anchor.</summary>
    public float ViewportPadding { get; set; } = 8;

    /// <summary>Nội dung rộng tối thiểu bằng anchor (danh sách của Select).</summary>
    public bool MatchAnchorWidth { get; set; }
}

/// <summary>
/// Một lớp giao diện đang mở trên cùng cửa sổ (tầng "Overlay Manager"): nội dung là một View được vẽ
/// ngoài cây của view gọi nó, nên không bị cha cắt, cuộn hay biến đổi – tương đương Portal + Top Layer.
/// Mở bằng <see cref="Window.OpenOverlay"/>.
/// </summary>
public sealed class Overlay
{
    internal Overlay(Window window, View content, OverlayOptions options)
    {
        Window = window; Content = content; Options = options;
        bool anchored = options.Anchor is not null || options.AnchorRect is not null;
        LayerClasses = (anchored ? "size-full relative" : "size-full " + options.LayerClass)
            + (options.Modal ? " " + options.BackdropClass : "");
    }

    public Window Window { get; }
    public View Content { get; }
    public OverlayOptions Options { get; }
    public bool IsOpen { get; internal set; } = true;

    /// <summary>Vị trí nội dung ở frame gần nhất (logical px).</summary>
    public Bounds ContentBounds { get; internal set; }

    /// <summary>Placement thực tế sau khi lật (chỉ có nghĩa khi neo theo anchor).</summary>
    public Placement ActualPlacement { get; internal set; }

    /// <summary>Overlay vừa đóng – do gọi <see cref="Close"/>, Escape, bấm ra ngoài, hay nội dung bị Release.</summary>
    public event Action? Closed;

    public void Close() => Window.CloseOverlay(this);

    internal readonly string LayerClasses;
    internal Bounds? LastAnchor;
    internal TextInput? PreviousFocus;
    internal long PreviousFocusKey;
    internal bool PreviousFocusVisible;
    internal void RaiseClosed() => Closed?.Invoke();
}
