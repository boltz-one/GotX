namespace GotX.Widgets;

public sealed partial class TextInput
{
    Window? _focusedIn;
    IDisposable? _blinkTimer;

    /// <summary>Có nhấp nháy con trỏ không. null = tự động: bật khi chạy với platform thật, tắt khi headless (tất định).</summary>
    public bool? Blink { get; set; }
    public double BlinkInterval { get; set; } = 0.53;
    /// <summary>Pha hiện của con trỏ; luôn true khi không nhấp nháy.</summary>
    public bool CaretVisible { get; private set; } = true;

    internal Window? FocusedIn
    {
        get => _focusedIn;
        set { if (_focusedIn == value) return; _focusedIn = value; ResetBlink(); }
    }

    /// <summary>Hiện con trỏ ngay và đếm lại nhịp (gọi khi gõ / di chuyển); mất focus thì dừng hẳn.</summary>
    void ResetBlink()
    {
        _blinkTimer?.Dispose();
        _blinkTimer = null;
        CaretVisible = true;
        if (_focusedIn is null || App is not { } app || !(Blink ?? app.Platform is not null)) return;
        _blinkTimer = app.SetInterval(BlinkInterval, () => { CaretVisible = !CaretVisible; Notify(); });
    }
}
