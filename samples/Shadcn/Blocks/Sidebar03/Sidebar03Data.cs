using GotX.Components;

/// <summary>Dữ liệu mẫu của block sidebar-03 (chép từ app-sidebar.tsx của shadcn/ui).</summary>
static class Sidebar03Data
{
    static SidebarNavItem Parent(string title, params string[] items) => new(title) { Emphasis = true, SubItems = items };

    /// <summary>Một nhóm không nhãn; mỗi nút cha chữ đậm kèm danh sách con luôn mở.</summary>
    public static readonly SidebarNavGroup[] Groups =
    [
        new(null,
        [
            Parent("Getting Started", "Installation", "Project Structure"),
            Parent("Build Your Application", "Routing", "Data Fetching", "Rendering", "Caching", "Styling", "Optimizing", "Configuring",
                "Testing", "Authentication", "Deploying", "Upgrading", "Examples"),
            Parent("API Reference", "Components", "File Conventions", "Functions", "next.config.js Options", "CLI", "Edge Runtime"),
            Parent("Architecture", "Accessibility", "Fast Refresh", "Next.js Compiler", "Supported Browsers", "Turbopack"),
            Parent("Community", "Contribution Guide"),
        ]) { SubMenus = SidebarSubMenus.Always },
    ];

    /// <summary>Mục đang chọn của bản gốc: Build Your Application › Data Fetching (isActive).</summary>
    public const int ActiveGroup = 0, ActiveItem = 1, ActiveSub = 1;
}
