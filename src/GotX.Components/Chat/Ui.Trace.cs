namespace GotX.Components;

/// <summary>Trạng thái một bước trong trace của agent. Done là mặc định (bản gốc: không có data-status).</summary>
public enum TraceStatus : byte { Done, Running, Error, Pending }

/// <summary>Một bước của trace: tiêu đề, mô tả ngắn bên phải (thời lượng…), chi tiết hiện bên dưới (tuỳ chọn).</summary>
public sealed record TraceStep(string Title, TraceStatus Status = TraceStatus.Done, string? Description = null, string? Detail = null);

public static partial class Ui
{
    static readonly string[] TraceLineClasses =
    [
        "flex min-w-0 max-w-full items-center gap-2 text-sm text-muted-foreground",
        "flex min-w-0 max-w-full items-center gap-2 text-sm text-muted-foreground",
        "flex min-w-0 max-w-full items-center gap-2 text-sm text-destructive",
        "flex min-w-0 max-w-full items-center gap-2 text-sm text-muted-foreground opacity-80",
    ];

    /// <summary>
    /// Một dòng trace: icon trạng thái + tiêu đề + mô tả mờ. Bước đang chạy dùng <c>animate-pulse</c> thay cho
    /// hiệu ứng shimmer (GotX chỉ có các animation dựng sẵn).
    /// </summary>
    public static Div TraceLine(Window w, string title, TraceStatus status = TraceStatus.Done, string? description = null)
    {
        var line = w.Div().Class(TraceLineClasses[(int)status])
            .Child(w.Div().Class("flex size-4 shrink-0 items-center justify-center").Child(status switch
            {
                TraceStatus.Running => Icon.Spinner(w),
                TraceStatus.Error => Icon.AlertCircle(w),
                TraceStatus.Pending => Icon.Circle(w),
                _ => Icon.Check(w),
            }))
            .Child(w.Div().Class(status == TraceStatus.Running ? "min-w-0 truncate text-foreground animate-pulse" : "min-w-0 truncate")
                .Child(title));
        if (description is { } d) line.Child(w.Div().Class("shrink-0 text-muted-foreground").Child(d));
        return line;
    }
}
