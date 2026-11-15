using System.Globalization;
using GotX;
using GotX.Components;

/// <summary>Một hàng của bảng "Outline" trong dashboard-01 (data.json của bản gốc).</summary>
public sealed class DashboardSection(int id, string header, string type, string status, int target, int limit, string reviewer)
{
    public int Id { get; } = id;
    public string Header { get; } = header;
    public string Type { get; } = type;
    public string Status { get; } = status;
    public int Target { get; } = target;
    public int Limit { get; } = limit;
    public string Reviewer { get; } = reviewer;

    // Chuỗi dựng một lần cho mỗi hàng: ô không định dạng số hay nối key mỗi lần vẽ.
    public string TargetText { get; } = target.ToString(CultureInfo.InvariantCulture);
    public string LimitText { get; } = limit.ToString(CultureInfo.InvariantCulture);
    public string LinkKey { get; } = "dashboard-section-" + id;
    public bool Done => Status == "Done";
    public bool Assigned => Reviewer != Dashboard01Data.Unassigned;

    /// <summary>Bảng chứa hàng này – để link tiêu đề (lambda static) gọi lại được nơi mở tấm chi tiết.</summary>
    public DashboardTable? Owner { get; set; }
}

/// <summary>Dữ liệu mẫu của block dashboard-01: menu sidebar, các thẻ số liệu, dữ liệu biểu đồ, các hàng của bảng.</summary>
public static class Dashboard01Data
{
    public const string UserName = "shadcn", UserEmail = "m@example.com", UserInitials = "CN", Unassigned = "Assign reviewer";

    public static readonly MenuItem[] DocumentMenu =
    [
        MenuItem.Action("Open", key: "dashboard-doc-open"), MenuItem.Action("Share"), MenuItem.Separator(), MenuItem.Action("Delete", destructive: true),
    ];

    public static readonly SidebarNavGroup[] Groups =
    [
        new(null,
        [
            new("Dashboard", DashboardIcons.Dashboard), new("Lifecycle", DashboardIcons.List), new("Analytics", DashboardIcons.ChartBar),
            new("Projects", Icon.Folder), new("Team", DashboardIcons.Users),
        ]),
        new("Documents",
        [
            new("Data Library", DashboardIcons.Database) { Menu = DocumentMenu, ActionOnHover = true },
            new("Reports", Icon.FileText) { Menu = DocumentMenu, ActionOnHover = true },
            new("Word Assistant", Icon.File) { Menu = DocumentMenu, ActionOnHover = true },
            new("More", Icon.MoreHorizontal) { Muted = true },
        ]) { HideWhenCollapsed = true },
        new(null, [new("Settings", Icon.Settings), new("Get Help", DashboardIcons.Help), new("Search", Icon.Search)]) { PushToBottom = true },
    ];

    public static readonly MenuItem[] UserMenu =
    [
        MenuItem.Label(UserName), MenuItem.Label(UserEmail), MenuItem.Separator(),
        new() { Text = "Account", Icon = Icon.User, Key = "dashboard-user-account" }, new() { Text = "Billing", Icon = SidebarIcons.CreditCard },
        new() { Text = "Notifications", Icon = Icon.Bell }, MenuItem.Separator(), new() { Text = "Log out", Icon = Icon.LogOut },
    ];

    public static readonly MenuItem[] RowMenu =
    [
        MenuItem.Action("Edit", key: "dashboard-row-edit"), MenuItem.Action("Make a copy"), MenuItem.Action("Favorite"), MenuItem.Separator(),
        MenuItem.Action("Delete", destructive: true, key: "dashboard-row-delete"),
    ];

    /// <summary>Thẻ số liệu: mô tả, giá trị, badge xu hướng, hai dòng chân thẻ.</summary>
    public sealed record Stat(string Label, string Value, string Trend, bool Up, string Headline, string Note);

    public static readonly Stat[] Stats =
    [
        new("Total Revenue", "$1,250.00", "+12.5%", true, "Trending up this month", "Visitors for the last 6 months"),
        new("New Customers", "1,234", "-20%", false, "Down 20% this period", "Acquisition needs attention"),
        new("Active Accounts", "45,678", "+12.5%", true, "Strong user retention", "Engagement exceed targets"),
        new("Growth Rate", "4.5%", "+4.5%", true, "Steady performance increase", "Meets growth projections"),
    ];

    public static readonly string[] Tabs = ["Outline", "Past Performance", "Key Personnel", "Focus Documents"];

    // Mười hàng đầu chép từ data.json của bản gốc; các hàng sau (tới 68 như bản gốc) sinh theo công thức cố định từ danh
    // sách tiêu đề, để phân trang có "Page 1 of 7" mà không phải chép cả tệp.
    public static List<DashboardSection> BuildSections()
    {
        var list = new List<DashboardSection>
        {
            new(1, "Cover page", "Cover page", "In Process", 18, 5, "Eddie Lake"),
            new(2, "Table of contents", "Table of contents", "Done", 29, 24, "Eddie Lake"),
            new(3, "Executive summary", "Narrative", "Done", 10, 13, "Eddie Lake"),
            new(4, "Technical approach", "Narrative", "Done", 27, 23, "Jamik Tashpulatov"),
            new(5, "Design", "Narrative", "In Process", 2, 16, "Jamik Tashpulatov"),
            new(6, "Capabilities", "Narrative", "In Process", 20, 8, "Jamik Tashpulatov"),
            new(7, "Integration with existing systems", "Narrative", "In Process", 19, 21, "Jamik Tashpulatov"),
            new(8, "Innovation and Advantages", "Narrative", "Done", 25, 26, Unassigned),
            new(9, "Overview of EMR's Innovative Solutions", "Technical content", "Done", 7, 23, Unassigned),
            new(10, "Advanced Algorithms and Machine Learning", "Narrative", "Done", 30, 28, Unassigned),
        };
        string[] titles =
        [
            "Adaptive Communication Protocols", "Advantages Over Current Technologies", "Past Performance", "Customer Feedback and Satisfaction Levels",
            "Implementation Challenges and Solutions", "Security Measures and Data Protection Policies", "Scalability and Future Proofing", "Cost-Benefit Analysis",
            "User Training and Onboarding Experience", "Future Development Roadmap", "System Architecture Overview", "Risk Management Plan",
            "Compliance Documentation", "API Documentation", "User Interface Mockups", "Database Schema", "Testing Methodology", "Deployment Strategy",
            "Budget Breakdown", "Market Analysis", "Competitor Comparison", "Maintenance Plan", "User Personas", "Accessibility Compliance",
            "Performance Metrics", "Disaster Recovery Plan", "Third-party Integrations", "User Feedback Summary", "Localization Strategy",
        ];
        string[] types = ["Narrative", "Technical content", "Plain language", "Legal", "Research", "Planning", "Financial", "Visual"];
        string[] reviewers = ["Eddie Lake", "Jamik Tashpulatov", Unassigned, "Maya Johnson", "Carlos Rodriguez", "Sarah Chen", Unassigned, "Raj Patel"];
        for (int i = 0; list.Count < 68; i++)
        {
            string title = i < titles.Length ? titles[i] : titles[i % titles.Length] + " " + (i / titles.Length + 1);
            list.Add(new(list.Count + 1, title, types[i % types.Length], i % 3 == 0 ? "In Process" : "Done", 5 + i * 7 % 26, 3 + i * 11 % 27, reviewers[i % reviewers.Length]));
        }
        return list;
    }
}
