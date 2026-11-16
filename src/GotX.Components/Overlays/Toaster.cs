namespace GotX.Components;

/// <summary>
/// Chồng thông báo ở một góc cửa sổ. Tạo một Toaster cho cả app, rồi <c>toaster.Show(window, new Toast { … })</c>.
/// Thông báo tự đóng sau <see cref="Duration"/> (hẹn giờ của App, chạy trên main thread); rê chuột lên chồng thông báo
/// thì mọi đếm ngược tạm dừng và chạy tiếp phần còn lại khi chuột rời. Đóng sớm (×, hành động, Dismiss) huỷ hẹn giờ.
/// Chồng được vẽ trên một overlay không modal, chỉ mở khi có thông báo; nó không chặn chuột ngoài các thẻ và
/// không giữ Escape (Escape vẫn tới dropdown/dialog đang mở bên dưới).
/// </summary>
public sealed class Toaster : View
{
    readonly PopupSlot _slot = new();
    readonly List<Toast> _toasts = new();
    Indexed<Toaster>[]? _refs;
    int _nextId;
    bool _paused;

    /// <summary>Thông báo vừa đóng (bấm ×, bấm hành động, hết giờ, hoặc gọi Dismiss).</summary>
    public readonly record struct Dismissed(int Id);

    /// <summary>Nút hành động của thông báo được bấm (thông báo cũng tự đóng).</summary>
    public readonly record struct ActionClicked(int Id);

    public ToastPosition Position { get; set; } = ToastPosition.BottomRight;
    public bool AutoDismiss { get; set; } = true;
    /// <summary>Thời gian tự đóng mặc định.</summary>
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(4);

    /// <summary>Số thông báo tối đa cùng lúc; thêm nữa thì đóng cái cũ nhất.</summary>
    public int MaxVisible { get; set; } = 4;

    /// <summary>Các thông báo đang hiện, cũ trước mới sau.</summary>
    public IReadOnlyList<Toast> Toasts => _toasts;

    /// <summary>Đếm ngược tự đóng đang tạm dừng vì chuột nằm trên chồng thông báo.</summary>
    public bool IsPaused => _paused;

    /// <summary>Overlay đang chứa chồng thông báo (null khi không có thông báo).</summary>
    public Overlay? Overlay => _slot.Current;

    /// <summary>Hiện một thông báo đơn giản; trả về Id.</summary>
    public int Show(Window window, string title, string description = "", ToastVariant variant = ToastVariant.Default) =>
        Show(window, new Toast { Title = title, Description = description, Variant = variant });

    /// <summary>Hiện <paramref name="toast"/>; trả về Id.</summary>
    public int Show(Window window, Toast toast)
    {
        int id = toast.Id = ++_nextId;
        toast.Key = "gotx-toast-" + id;
        toast.CloseKey = "gotx-toast-close-" + id;
        toast.ActionKey = "gotx-toast-action-" + id;
        _toasts.Add(toast);
        while (_toasts.Count > MaxVisible) Dismiss(_toasts[0].Id);
        // Thông báo "đang tải" không tự đóng: người gọi tự Dismiss khi việc xong.
        if (AutoDismiss && toast.Variant != ToastVariant.Loading)
        {
            toast.Remaining = (toast.Duration ?? Duration).TotalSeconds;
            if (!_paused) Arm(toast);
        }
        if (!_slot.IsOpen)
            _slot.Open(window, this, new OverlayOptions
            {
                LayerClass = LayerFor(Position), DismissOnEscape = false, DismissOnOutsideClick = false,
            }, () => { if (!_slot.IsOpen) _paused = false; Notify(); });
        Notify();
        return id;
    }

    void Arm(Toast toast)
    {
        int id = toast.Id;
        toast.Due = App.Clock() + toast.Remaining;
        toast.Timer = App.SetTimeout(toast.Remaining, () => Dismiss(id));
    }

    // Chuột vào chồng thông báo: dừng mọi đếm ngược và nhớ phần còn lại; chuột rời: chạy tiếp đúng phần đó.
    void SetPaused(bool paused)
    {
        if (_paused == paused) return;
        _paused = paused;
        double now = App.Clock();
        foreach (var t in _toasts)
        {
            if (paused && t.Timer is { } timer)
            {
                timer.Dispose();
                t.Timer = null;
                t.Remaining = Math.Max(0, t.Due - now);
            }
            else if (!paused && t.Timer is null && t.Remaining >= 0) Arm(t);
        }
    }

    /// <summary>Đóng thông báo có <paramref name="id"/> (không làm gì nếu đã đóng).</summary>
    public void Dismiss(int id)
    {
        int i = _toasts.FindIndex(t => t.Id == id);
        if (i < 0) return;
        var toast = _toasts[i];
        _toasts.RemoveAt(i);
        toast.Timer?.Dispose();
        toast.Timer = null;
        toast.Remaining = -1;
        if (_toasts.Count == 0) _slot.Close();
        Notify();
        Emit(new Dismissed(id));
    }

    /// <summary>Đóng mọi thông báo.</summary>
    public void DismissAll()
    {
        while (_toasts.Count > 0) Dismiss(_toasts[^1].Id);
    }

    static string LayerFor(ToastPosition p) => p switch
    {
        ToastPosition.BottomLeft => "flex flex-col items-start justify-end p-4",
        ToastPosition.TopRight => "flex flex-col items-end justify-start p-4",
        ToastPosition.TopLeft => "flex flex-col items-start justify-start p-4",
        _ => "flex flex-col items-end justify-end p-4",
    };

    void Action(int index)
    {
        if ((uint)index >= (uint)_toasts.Count) return;
        int id = _toasts[index].Id;
        Emit(new ActionClicked(id));
        Dismiss(id);
    }

    /// <summary>Vẽ chồng thông báo (trong lớp overlay); thông báo mới nhất nằm sát góc.</summary>
    public override Div Render(Window w)
    {
        var refs = Indexed<Toaster>.Ensure(ref _refs, this, _toasts.Count);
        bool top = Position is ToastPosition.TopLeft or ToastPosition.TopRight;
        var stack = w.Div().Class("flex flex-col gap-3 w-[22rem] max-w-full")
            .OnHover(this, static (t, inside) => t.SetPaused(inside));
        for (int k = 0; k < _toasts.Count; k++)
        {
            int i = top ? _toasts.Count - 1 - k : k;
            stack.Child(Card(w, _toasts[i], refs[i]));
        }
        return stack;
    }

    Div Card(Window w, Toast t, Indexed<Toaster> r)
    {
        var card = w.Div().Key(t.Key).Role(t.Variant is ToastVariant.Error or ToastVariant.Warning ? SemanticRole.Alert : SemanticRole.Status)
            .Class("flex items-center gap-3 rounded-2xl border border-border bg-popover p-4 text-popover-foreground shadow-lg");
        if (ToastIcons.For(w, t.Variant) is { } icon) card.Child(icon);
        var text = w.Div().Class("flex min-w-0 flex-1 flex-col gap-1");
        if (t.Title.Length > 0) text.Child(w.Div().Class("text-sm font-medium").Child(t.Title));
        if (t.Description.Length > 0) text.Child(w.Div().Class("text-sm text-muted-foreground").Child(t.Description));
        card.Child(text);
        if (t.ActionText.Length > 0)
            card.Child(Ui.Button(w, t.ActionText, ButtonVariant.Outline, ButtonSize.Sm).Class(FocusRing.Ring).Key(t.ActionKey).Focusable()
                .OnClick(r, static (x, _) => x.Owner.Action(x.Index)));
        return card.Child(OverlayParts.CloseButton(w, t.CloseKey, "shrink-0 text-muted-foreground hover:text-foreground")
            .OnClick(r, static (x, _) => { var all = x.Owner._toasts; if (x.Index < all.Count) x.Owner.Dismiss(all[x.Index].Id); }));
    }
}
