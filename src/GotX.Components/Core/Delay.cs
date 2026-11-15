namespace GotX.Components;

/// <summary>Một hẹn giờ huỷ được, dùng lại nhiều lần (trễ mở/đóng của hover card, menu con…): đặt mới thì huỷ cái đang chờ.</summary>
internal sealed class Delay
{
    IDisposable? _timer;

    public bool Pending => _timer is not null;

    /// <summary>Chạy <paramref name="action"/> sau <paramref name="seconds"/> giây (≤ 0 thì chạy ngay); huỷ hẹn giờ đang chờ.</summary>
    public void Start(App? app, double seconds, Action action)
    {
        Cancel();
        if (app is null || seconds <= 0) { action(); return; }
        _timer = app.SetTimeout(seconds, () => { _timer = null; action(); });
    }

    public void Cancel()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
