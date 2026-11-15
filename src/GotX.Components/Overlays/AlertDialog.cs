namespace GotX.Components;

/// <summary>Kích thước AlertDialog: Sm cho câu hỏi ngắn (hai nút chia đôi hàng chân).</summary>
public enum AlertDialogSize : byte { Default, Sm }

/// <summary>
/// Hộp thoại xác nhận: modal nhưng KHÔNG đóng khi bấm ra ngoài – người dùng phải chọn Huỷ hoặc hành động,
/// vì đây là câu hỏi có hậu quả (xoá, đăng xuất…). Escape được coi là Huỷ. Khi mở, nút Huỷ nhận focus (lựa chọn an toàn,
/// như Radix); Tab chỉ đi giữa hai nút; đóng thì focus về nơi cũ.
/// <code>
/// _confirm = App.New(new AlertDialog { Title = "Xoá dự án?", ActionText = "Xoá", ActionVariant = ButtonVariant.Destructive });
/// App.Subscribe&lt;AlertDialog.Confirmed&gt;(_confirm, _ =&gt; Delete());
/// </code>
/// </summary>
public sealed class AlertDialog : View
{
    readonly PopupSlot _slot = new();
    bool _decided;

    /// <summary>Hộp thoại vừa mở / vừa đóng.</summary>
    public readonly record struct OpenChanged(bool Open);

    /// <summary>Người dùng bấm nút hành động.</summary>
    public readonly record struct Confirmed;

    /// <summary>Người dùng bấm Huỷ hoặc Escape.</summary>
    public readonly record struct Cancelled;

    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string CancelText { get; set; } = "Huỷ";
    public string ActionText { get; set; } = "Tiếp tục";
    public ButtonVariant ActionVariant { get; set; } = ButtonVariant.Default;
    public AlertDialogSize Size { get; set; }

    /// <summary>Ô icon phía trên tiêu đề (tuỳ chọn), vẽ trong khung vuông nền muted.</summary>
    public Func<Window, Div>? Media { get; set; }

    /// <summary>Nội dung thêm giữa phần mô tả và hàng nút.</summary>
    public Func<Window, Div>? Body { get; set; }

    public bool IsOpen => _slot.IsOpen;

    /// <summary>Key của nút Huỷ – để tìm trong kiểm tra headless.</summary>
    public string CancelKey => _cancelKey ??= "gotx-alert-cancel-" + Id.Value;
    /// <summary>Key của nút hành động.</summary>
    public string ActionKey => _actionKey ??= "gotx-alert-action-" + Id.Value;
    string? _cancelKey, _actionKey;

    /// <summary>Mở hộp thoại trên lớp overlay của <paramref name="window"/>.</summary>
    public void Open(Window window)
    {
        if (IsOpen) return;
        _decided = false;
        _slot.Open(window, this, new OverlayOptions
        {
            Modal = true, DismissOnOutsideClick = false, DismissOnEscape = true,
            LayerClass = "flex items-center justify-center p-4",
        }, OnSlotChanged);
    }

    /// <summary>Đóng mà không phát Confirmed/Cancelled.</summary>
    public void Close() { _decided = true; _slot.Close(); }

    void OnSlotChanged()
    {
        bool open = _slot.IsOpen;
        // Đóng bằng Escape (không qua nút nào) cũng là một lần huỷ.
        if (!open && !_decided) { _decided = true; Emit(new Cancelled()); }
        Emit(new OpenChanged(open));
    }

    void Decide(bool confirm)
    {
        _decided = true;
        if (confirm) Emit(new Confirmed()); else Emit(new Cancelled());
        _slot.Close();
    }

    /// <summary>Vẽ tấm nội dung (được gọi trong lớp overlay).</summary>
    public override Div Render(Window w)
    {
        bool sm = Size == AlertDialogSize.Sm;
        var panel = w.Div().Class(sm
            ? "flex flex-col gap-4 w-80 max-w-full rounded-xl bg-popover p-4 text-popover-foreground ring-1 ring-foreground/10 shadow-lg"
            : "flex flex-col gap-4 w-96 max-w-full rounded-xl bg-popover p-4 text-popover-foreground ring-1 ring-foreground/10 shadow-lg");

        var header = w.Div().Class(sm ? "flex flex-col items-center gap-1.5 text-center" : "flex flex-col gap-1.5");
        if (Media is { } media)
            header.Child(w.Div().Class("mb-2 flex size-10 items-center justify-center rounded-md bg-muted").Child(media(w)));
        if (Title.Length > 0) header.Child(w.Div().Class("text-base font-medium").Child(Title));
        if (Description.Length > 0) header.Child(w.Div().Class("text-sm text-muted-foreground").Child(Description));
        panel.Child(header);
        if (Body is { } body) panel.Child(body(w));

        // Chân: hai nút; bản Sm chia đôi hàng như grid-cols-2 của shadcn.
        var footer = w.Div().Class(sm
            ? "-mx-4 -mb-4 grid grid-cols-2 gap-2 rounded-b-xl border-t border-border bg-muted/50 p-4"
            : "-mx-4 -mb-4 flex justify-end gap-2 rounded-b-xl border-t border-border bg-muted/50 p-4");
        footer.Child(Ui.Button(w, CancelText, ButtonVariant.Outline).Class(FocusRing.Ring).Key(CancelKey).Focusable().AutoFocus()
            .OnClick(this, static (d, _) => d.Decide(false)));
        footer.Child(Ui.Button(w, ActionText, ActionVariant).Class(FocusRing.Ring).Key(ActionKey).Focusable()
            .OnClick(this, static (d, _) => d.Decide(true)));
        return panel.Child(footer);
    }
}
