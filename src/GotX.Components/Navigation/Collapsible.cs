namespace GotX.Components;

/// <summary>
/// Vùng thu gọn được: phần <see cref="Trigger"/> luôn hiện và bấm vào thì đảo trạng thái, phần <see cref="Content"/>
/// chỉ có khi mở. Không tự vẽ khung – người dùng tự quyết hình dáng, giống Collapsible của shadcn.
/// Phần trigger là một nút: focus được bằng Tab, Enter/Space đảo trạng thái (nội dung bên trong nó chỉ để nhìn).
/// </summary>
public sealed class Collapsible : View
{
    bool _open;
    string? _key;

    /// <summary>Phát khi mở/đóng.</summary>
    public readonly record struct OpenChanged(bool Open);

    /// <summary>Đang mở hay không; đặt được trước khi gắn vào app.</summary>
    public bool Open
    {
        get => _open;
        set
        {
            if (_open == value) return;
            _open = value;
            Notify();
            Emit(new OpenChanged(value));
        }
    }

    /// <summary>Phần luôn hiện; bấm vào bất kỳ đâu trong nó đều đảo trạng thái.</summary>
    public Func<Window, Div>? Trigger { get; set; }

    /// <summary>Phần chỉ hiện khi mở.</summary>
    public Func<Window, Div>? Content { get; set; }

    /// <summary>Key của phần trigger (kiểm tra headless).</summary>
    public string TriggerKey => _key ??= "gotx-collapsible-" + Id.Value;

    /// <summary>Đảo trạng thái mở/đóng.</summary>
    public void Toggle() => Open = !Open;

    /// <summary>Vẽ trigger rồi (nếu mở) nội dung, cách nhau gap-2.</summary>
    public override Div Render(Window w)
    {
        var root = w.Div().Class("flex flex-col gap-2");
        var trigger = w.Div().Key(TriggerKey).Class("cursor-pointer rounded-md " + FocusRing.RingOnly).Focusable()
            .OnClick(this, static (c, _) => c.Toggle());
        if (Trigger is { } t) trigger.Child(t(w));
        root.Child(trigger);
        if (_open && Content is { } c) root.Child(c(w));
        return root;
    }
}
