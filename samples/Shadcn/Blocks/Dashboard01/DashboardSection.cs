/// <summary>Một hàng của bảng "Outline" trong dashboard-01 (schema của data-table.tsx: mọi trường trừ id là chuỗi).</summary>
public sealed class DashboardSection(int id, string header, string type, string status, string target, string limit, string reviewer)
{
    public int Id { get; } = id;
    public string Header { get; } = header;
    public string Type { get; } = type;
    public string Status { get; } = status;

    /// <summary>Giá trị ô nhập Target / Limit (defaultValue của bản gốc; người dùng sửa được).</summary>
    public string Target { get; set; } = target;
    public string Limit { get; set; } = limit;

    /// <summary>Người duyệt; "Assign reviewer" = chưa có (ô hiện Select).</summary>
    public string Reviewer { get; set; } = reviewer;

    // Key dựng một lần cho mỗi hàng: ô không nối chuỗi mỗi lần vẽ. Đặt theo data-slot / chữ của bản gốc.
    public string RowKey { get; } = "row-" + id;
    public string LinkKey { get; } = "row-" + id + "-header";
    public string SelectKey { get; } = "row-" + id + "-select";
    public string DragKey { get; } = "row-" + id + "-drag";
    public string MenuKey { get; } = "row-" + id + "-menu";
    public string TargetKey { get; } = "row-" + id + "-target";
    public string LimitKey { get; } = "row-" + id + "-limit";
    public string ReviewerKey { get; } = "row-" + id + "-reviewer";

    public bool Done => Status == "Done";
    public bool Assigned => Reviewer != Dashboard01Data.Unassigned;
}
