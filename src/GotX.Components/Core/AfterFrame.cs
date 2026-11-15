namespace GotX.Components;

/// <summary>
/// Một việc chạy ngay SAU frame đang dựng, khi hình học của frame đã đủ và được phép mở/đóng overlay, đổi focus, cuộn.
/// <see cref="Request"/> gọi được trong <c>Render</c>: nó chỉ xếp một effect, và <c>Window.Draw</c> xử lý hàng đợi effect
/// ở cuối frame – không cần hẹn giờ, không phụ thuộc đồng hồ. Không phải View nên không làm view nào vẽ lại.
/// </summary>
internal sealed class AfterFrame : Entity
{
    /// <summary>Tạo một mốc gắn với <paramref name="action"/> (gọi trong <c>OnCreated</c>).</summary>
    public static AfterFrame Create(App app, Action action)
    {
        var mark = app.New(new AfterFrame());
        app.Observe(mark, action);
        return mark;
    }

    /// <summary>Yêu cầu chạy việc đã gắn khi frame hiện tại (hoặc sự kiện hiện tại) xử lý xong; gọi nhiều lần vẫn chỉ chạy một lượt.</summary>
    public void Request() => Notify();
}
