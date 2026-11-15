namespace GotX.Components;

/// <summary>Kiểu thông báo: quyết định icon bên trái.</summary>
public enum ToastVariant : byte { Default, Success, Info, Warning, Error, Loading }

/// <summary>Góc cửa sổ đặt chồng thông báo.</summary>
public enum ToastPosition : byte { BottomRight, BottomLeft, TopRight, TopLeft }

/// <summary>Một thông báo. Tạo rồi đưa vào <see cref="Toaster.Show(Window, Toast)"/>; <see cref="Id"/> được gán khi hiện.</summary>
public sealed class Toast
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public ToastVariant Variant { get; init; }

    /// <summary>Chữ nút hành động (ví dụ "Hoàn tác"); rỗng = không có nút.</summary>
    public string ActionText { get; init; } = "";

    /// <summary>Thời gian tự đóng riêng cho thông báo này; null = theo <see cref="Toaster.Duration"/>.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>Mã do Toaster gán khi hiện (bắt đầu từ 1).</summary>
    public int Id { get; internal set; }

    /// <summary>Key của thẻ thông báo – để tìm trong kiểm tra headless.</summary>
    public string Key { get; internal set; } = "";

    /// <summary>Key của nút đóng.</summary>
    public string CloseKey { get; internal set; } = "";

    /// <summary>Key của nút hành động.</summary>
    public string ActionKey { get; internal set; } = "";

    // Đếm ngược tự đóng do Toaster quản lý: hẹn giờ đang chạy, thời điểm tới hạn, và số giây còn lại khi đang tạm dừng
    // (âm = thông báo này không tự đóng).
    internal IDisposable? Timer;
    internal double Due, Remaining = -1;
}

/// <summary>Icon theo kiểu thông báo (cùng bộ Lucide như bản shadcn: circle-check, info, triangle-alert, octagon-x, loader).</summary>
internal static class ToastIcons
{
    public static Div? For(Window w, ToastVariant variant) => variant switch
    {
        ToastVariant.Success => Icon.CircleCheck(w),
        ToastVariant.Info => Icon.Info(w),
        ToastVariant.Warning => Icon.TriangleAlert(w),
        ToastVariant.Error => Icon.OctagonX(w).Class("text-destructive"),
        ToastVariant.Loading => Icon.Spinner(w),
        _ => null,
    };
}
