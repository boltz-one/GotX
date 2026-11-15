namespace GotX.Components;

/// <summary>
/// Hộp thoại modal ở giữa cửa sổ: backdrop, Escape / bấm ra ngoài để đóng, nút × ở góc.
/// Khi mở, tấm nhận focus; Tab chỉ đi vòng trong hộp thoại (Core chặn ở lớp modal); đóng thì focus về nơi cũ.
/// Bản thân Dialog là View của tấm nội dung; <see cref="Open"/> đưa nó lên lớp overlay của cửa sổ.
/// <code>
/// _dialog = App.New(new Dialog { Title = "Sửa hồ sơ", Description = "…", Body = w =&gt; …, Footer = w =&gt; … });
/// button.OnClick(this, static (v, _) =&gt; v._dialog.Open(v._window));
/// </code>
/// </summary>
public sealed class Dialog : View
{
    Overlay? _overlay;
    string? _contentKey, _closeKey;

    public readonly record struct OpenChanged(bool Open);

    public string Title { get; set; } = "";
    public string Description { get; set; } = "";

    /// <summary>Phần thân giữa tiêu đề và chân.</summary>
    public Func<Window, Div>? Body { get; set; }

    /// <summary>Hàng nút ở chân (tự căn phải). Nút tự gọi <see cref="Close"/> khi cần.</summary>
    public Func<Window, Div>? Footer { get; set; }

    public bool ShowCloseButton { get; set; } = true;
    public bool DismissOnOutsideClick { get; set; } = true;
    public bool DismissOnEscape { get; set; } = true;

    /// <summary>Class thêm cho tấm nội dung (ví dụ đổi độ rộng: <c>"w-[32rem]"</c>).</summary>
    public string PanelClass { get; set; } = "";

    public bool IsOpen => _overlay is { IsOpen: true };

    /// <summary>Key của tấm nội dung – phần tử giữ focus ngay sau khi mở.</summary>
    public string ContentKey => _contentKey ??= "gotx-dialog-" + Id.Value;

    /// <summary>Key của nút × ở góc.</summary>
    public string CloseKey => _closeKey ??= "gotx-dialog-close-" + Id.Value;

    public void Open(Window window)
    {
        if (IsOpen) return;
        _overlay = window.OpenOverlay(this, new OverlayOptions
        {
            Modal = true,
            DismissOnOutsideClick = DismissOnOutsideClick,
            DismissOnEscape = DismissOnEscape,
            LayerClass = "flex items-center justify-center p-4",
        });
        _overlay.Closed += OnClosed;
        Emit(new OpenChanged(true));
    }

    public void Close() => _overlay?.Close();

    void OnClosed()
    {
        _overlay = null;
        Emit(new OpenChanged(false));
    }

    public override Div Render(Window w)
    {
        var panel = w.Div().Class(
            "relative flex flex-col gap-4 w-96 max-w-full rounded-xl bg-popover p-4 text-sm text-popover-foreground " +
            "ring-1 ring-foreground/10 shadow-lg").TakeFocus(ContentKey);
        if (PanelClass.Length > 0) panel.Class(PanelClass);

        if (Title.Length > 0 || Description.Length > 0)
        {
            var header = w.Div().Class("flex flex-col gap-2");
            if (Title.Length > 0) header.Child(w.Div().Class("text-base font-medium leading-none").Child(Title));
            if (Description.Length > 0) header.Child(w.Div().Class("text-sm text-muted-foreground").Child(Description));
            panel.Child(header);
        }
        if (Body is { } body) panel.Child(body(w));
        if (Footer is { } footer) panel.Child(w.Div().Class("flex justify-end gap-2").Child(footer(w)));
        if (ShowCloseButton)
            panel.Child(OverlayParts.CloseButton(w, CloseKey, "absolute top-2 right-2").OnClick(this, static (d, _) => d.Close()));
        return panel;
    }
}
