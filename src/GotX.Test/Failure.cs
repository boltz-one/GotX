namespace GotX.Test;

/// <summary>Một lỗi được ghi nhận trong test: đủ thông tin để agent đi thẳng tới phần tử và file bằng chứng.</summary>
public sealed class Failure
{
    public required string Message { get; init; }
    /// <summary>Đường dẫn phần tử liên quan (xem <see cref="UiTree.Path"/>).</summary>
    public string? Path { get; init; }
    /// <summary>Hộp trên màn hình (logical px) của phần tử liên quan.</summary>
    public Bounds? Bounds { get; init; }
    /// <summary>Các dòng chi tiết thêm (danh sách view đã render, vùng ảnh khác biệt…).</summary>
    public List<string> Details { get; } = new();
    /// <summary>File bằng chứng: ảnh có khoanh vùng, bản in cây, ảnh diff.</summary>
    public List<string> Artifacts { get; } = new();
    /// <summary>Các sự kiện ngay trước khi lỗi xảy ra.</summary>
    public List<string> Trace { get; init; } = new();
    /// <summary>Stack trace nếu lỗi là một exception.</summary>
    public string? Stack { get; init; }
}

/// <summary>Ném ra khi test không thể đi tiếp (không tìm thấy phần tử cần bấm…). Lỗi đã được ghi vào test trước khi ném.</summary>
public sealed class TestAbortException(string message) : Exception(message);

/// <summary>Cấu hình chung của một lượt chạy, runner truyền vào từng test.</summary>
public sealed class TestOptions
{
    /// <summary>Thư mục ghi file bằng chứng (ảnh, bản in cây). null = không ghi file.</summary>
    public string? OutDir { get; set; }
    /// <summary>Thư mục ảnh chuẩn cho <c>Snapshot(name)</c>.</summary>
    public string? SnapshotDir { get; set; }
    public bool UpdateSnapshots { get; set; }
    public bool UpdateBaseline { get; set; }
    public bool Verbose { get; set; }
    /// <summary>Baseline lint dùng chung của bộ test (xem <see cref="LintBaseline"/>).</summary>
    public LintBaseline? Baseline { get; set; }
    /// <summary>Số lỗi tối đa mỗi test được kèm file bằng chứng (ảnh + cây) – tránh ghi hàng trăm file khi hỏng hàng loạt.</summary>
    public int MaxFailureArtifacts { get; set; } = 4;
}
