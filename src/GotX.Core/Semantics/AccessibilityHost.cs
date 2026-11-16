namespace GotX;

/// <summary>Cửa Window mở cho cầu nối trợ năng của platform (UI Automation…): dựng cây ngữ nghĩa và thực hiện hành động.</summary>
public interface IAccessibilityHost
{
    /// <summary>Số thứ tự frame của Window – đổi nghĩa là nội dung có thể đã đổi.</summary>
    int Frame { get; }

    /// <summary>Dựng ảnh chụp ngữ nghĩa mới (chạy trên luồng giao diện).</summary>
    SemanticsTree Build();

    /// <summary>Thực hiện hành động trên node (chạy trên luồng giao diện).</summary>
    bool Perform(long id, SemanticAction action, double number = 0, string? text = null);
}

/// <summary>Cửa sổ platform hỗ trợ trợ năng: Application nối nó với Window sau khi mở. Cửa sổ chỉ tạo cầu nối khi công cụ hỗ trợ hỏi.</summary>
public interface IAccessibleWindow
{
    IAccessibilityHost? Accessibility { set; }
}
