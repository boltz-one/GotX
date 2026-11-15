namespace GotX.Components;

/// <summary>
/// Trace của agent ("Đã suy nghĩ 12 giây ›"): dòng tiêu đề bấm để mở/đóng danh sách các bước, mỗi bước có icon
/// trạng thái. Danh sách nằm sau một vạch dọc bên trái như bản gốc. Không có hiệu ứng trượt chiều cao vì GotX
/// chưa transition được <c>height: auto</c>; chỉ chevron xoay có chuyển động.
/// </summary>
public sealed class Trace : View
{
    /// <summary>Danh sách bước vừa được mở (true) hoặc đóng (false).</summary>
    public readonly record struct OpenChanged(bool Open);

    bool _open;
    string? _triggerKey, _chevronKey, _contentKey;
    string _countTitle = "0 bước"; // tiêu đề mặc định, dựng lại khi Steps đổi chứ không phải mỗi lần render
    IReadOnlyList<TraceStep> _steps = [];

    /// <summary>Chữ của dòng mở/đóng; null thì tự tóm tắt theo số bước.</summary>
    public string? Title { get; set; }

    /// <summary>Các bước theo thứ tự thực hiện.</summary>
    public IReadOnlyList<TraceStep> Steps
    {
        get => _steps;
        set { _steps = value; _countTitle = $"{value.Count} bước"; Notify(); }
    }

    /// <summary>Danh sách bước đang mở; đổi thì phát <see cref="OpenChanged"/>.</summary>
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

    /// <summary>Đảo trạng thái mở/đóng (bấm dòng tiêu đề).</summary>
    public void Toggle() => Open = !_open;

    /// <summary>Trạng thái gộp hiển thị trên dòng tiêu đề: lỗi thắng đang chạy, đang chạy thắng xong.</summary>
    public TraceStatus Summary
    {
        get
        {
            var s = TraceStatus.Done;
            foreach (var step in _steps)
            {
                if (step.Status == TraceStatus.Error) return TraceStatus.Error;
                if (step.Status == TraceStatus.Running) s = TraceStatus.Running;
            }
            return s;
        }
    }

    /// <summary>Vẽ dòng tiêu đề và, khi mở, danh sách bước sau vạch dọc.</summary>
    public override Div Render(Window w)
    {
        var summary = Summary;
        string title = Title ?? _countTitle;
        var trigger = w.Div().Key(_triggerKey ??= "gotx-trace-trigger-" + Id.Value)
            .Class(summary == TraceStatus.Error
                ? "flex min-w-0 max-w-full self-start items-center gap-1 rounded-md text-sm text-destructive cursor-pointer transition-colors focus-visible:ring-3 focus-visible:ring-ring/50"
                : "flex min-w-0 max-w-full self-start items-center gap-1 rounded-md text-sm text-muted-foreground cursor-pointer transition-colors hover:text-foreground focus-visible:ring-3 focus-visible:ring-ring/50")
            .Focusable()
            .OnClick(this, static (t, _) => t.Toggle())
            .Child(w.Div().Class(summary == TraceStatus.Running ? "min-w-0 truncate text-foreground animate-pulse" : "min-w-0 truncate").Child(title))
            .Child(w.Div().Class(_open
                    ? "flex size-4 shrink-0 items-center justify-center transition-transform rotate-90"
                    : "flex size-4 shrink-0 items-center justify-center transition-transform")
                .Key(_chevronKey ??= "gotx-trace-chevron-" + Id.Value)
                .Child(Icon.ChevronRight(w)));

        var root = w.Div().Class("flex w-full min-w-0 flex-col").Child(trigger);
        if (!_open) return root;

        // Vạch dọc là một phần tử w-px riêng thay cho border-l: viền một cạnh hiện để lại vệt mờ ở các cạnh còn lại.
        var list = w.Div().Class("flex min-w-0 flex-1 flex-col gap-2 pl-4 pr-1 leading-relaxed text-muted-foreground");
        root.Child(w.Div().Key(_contentKey ??= "gotx-trace-content-" + Id.Value).Class("mt-2 mb-1 ml-[7px] flex min-w-0")
            .Child(w.Div().Class("w-px shrink-0 bg-border"))
            .Child(list));
        foreach (var step in _steps)
        {
            if (step.Detail is not { } detail) { list.Child(Ui.TraceLine(w, step.Title, step.Status, step.Description)); continue; }
            list.Child(w.Div().Class("flex min-w-0 flex-col gap-1")
                .Child(Ui.TraceLine(w, step.Title, step.Status, step.Description))
                .Child(w.Div().Class(step.Status == TraceStatus.Error ? "pl-6 text-xs text-destructive/80" : "pl-6 text-xs text-muted-foreground")
                    .Child(detail)));
        }
        return root;
    }
}
