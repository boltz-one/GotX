namespace GotX.Components;

/// <summary>
/// Giữ một overlay đang mở cho component (dropdown, popover, tooltip…): mọi component nổi đều cần cùng một vòng đời
/// "mở → nghe Closed → quên handle", và phải bỏ qua Closed của overlay cũ khi đã mở overlay mới thay nó.
/// </summary>
internal sealed class PopupSlot
{
    Overlay? _overlay;

    public bool IsOpen => _overlay is { IsOpen: true };

    /// <summary>Overlay đang mở (null khi đóng) – để đọc <see cref="Overlay.ContentBounds"/>, ActualPlacement.</summary>
    public Overlay? Current => IsOpen ? _overlay : null;

    /// <summary>Đóng overlay cũ (nếu có) rồi mở <paramref name="content"/>; <paramref name="changed"/> chạy sau mỗi lần mở/đóng.</summary>
    public Overlay Open(Window window, View content, OverlayOptions options, Action changed)
    {
        Close();
        var overlay = _overlay = window.OpenOverlay(content, options);
        overlay.Closed += () =>
        {
            if (ReferenceEquals(_overlay, overlay)) _overlay = null;
            changed();
        };
        changed();
        return overlay;
    }

    /// <summary>Đóng overlay đang giữ (nếu có).</summary>
    public void Close() => _overlay?.Close();
}
