using GotX;
using GotX.Components;

/// <summary>Dữ liệu của block dashboard-01, chép nguyên từ bản gốc (app-sidebar.tsx, section-cards.tsx, data-table.tsx, data.json).</summary>
public static class Dashboard01Data
{
    public const string UserName = "shadcn", UserEmail = "m@example.com", UserInitials = "CN", Unassigned = "Assign reviewer";

    /// <summary>Menu nút "More" (…) của mục Documents: Open / Share / — / Delete (nav-documents.tsx).</summary>
    public static readonly MenuItem[] DocumentMenu =
    [
        new() { Text = "Open", Icon = static w => DashboardIcons.Draw(w, DashboardIcons.FolderPath), Key = "dashboard-doc-open" },
        new() { Text = "Share", Icon = static w => DashboardIcons.Draw(w, DashboardIcons.Share3Path) },
        MenuItem.Separator(),
        new() { Text = "Delete", Icon = static w => DashboardIcons.Draw(w, DashboardIcons.TrashPath), Destructive = true },
    ];

    /// <summary>Nhóm sidebar: NavMain (sau hàng Quick Create), NavDocuments, NavSecondary (mt-auto). Không mục nào active.</summary>
    public static readonly SidebarNavGroup[] Groups =
    [
        new(null,
        [
            new("Dashboard", DashboardIcons.Dashboard), new("Lifecycle", DashboardIcons.ListDetails), new("Analytics", DashboardIcons.ChartBar),
            new("Projects", DashboardIcons.Folder), new("Team", DashboardIcons.Users),
        ]),
        new("Documents",
        [
            new("Data Library", DashboardIcons.Database) { Menu = DocumentMenu, ActionOnHover = true, Action = DashboardIcons.Dots },
            new("Reports", DashboardIcons.Report) { Menu = DocumentMenu, ActionOnHover = true, Action = DashboardIcons.Dots },
            new("Word Assistant", DashboardIcons.FileWord) { Menu = DocumentMenu, ActionOnHover = true, Action = DashboardIcons.Dots },
            new("More", DashboardIcons.Dots) { Muted = true },
        ]) { HideWhenCollapsed = true },
        new(null, [new("Settings", DashboardIcons.Settings), new("Get Help", DashboardIcons.Help), new("Search", DashboardIcons.Search)]) { PushToBottom = true },
    ];

    /// <summary>Menu tài khoản (nav-user.tsx): dòng nhãn avatar + tên + email, rồi Account / Billing / Notifications, Log out.</summary>
    public static readonly MenuItem[] UserMenu =
    [
        MenuItem.Custom(static w => w.Div().Class("flex items-center gap-2 px-1 py-1.5 text-left text-sm")
            .Child(w.Div().Class("flex size-8 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-muted").Child(UserInitials))
            .Child(w.Div().Class("grid min-w-0 flex-1 text-left text-sm leading-tight")
                .Child(w.Div().Class("truncate font-medium").Child(UserName))
                .Child(w.Div().Class("truncate text-xs text-muted-foreground").Child(UserEmail)))),
        MenuItem.Separator(),
        new() { Text = "Account", Icon = static w => DashboardIcons.Draw(w, DashboardIcons.UserCirclePath), Key = "dashboard-user-account" },
        new() { Text = "Billing", Icon = static w => DashboardIcons.Draw(w, DashboardIcons.CreditCardPath) },
        new() { Text = "Notifications", Icon = static w => DashboardIcons.Draw(w, DashboardIcons.NotificationPath) },
        MenuItem.Separator(),
        new() { Text = "Log out", Icon = static w => DashboardIcons.Draw(w, DashboardIcons.LogoutPath) },
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

    /// <summary>Các tab của bảng; số là Badge trong TabsTrigger (0 = không có).</summary>
    public static readonly (string Title, string Value, string? Badge)[] Tabs =
        [("Outline", "outline", null), ("Past Performance", "past-performance", "3"), ("Key Personnel", "key-personnel", "2"), ("Focus Documents", "focus-documents", null)];

    /// <summary>68 hàng của data.json (target / limit là chuỗi như bản gốc: ô nhập giữ nguyên chuỗi người dùng gõ).</summary>
    public static List<DashboardSection> BuildSections() =>
    [
        new(1, "Cover page", "Cover page", "In Process", "18", "5", "Eddie Lake"),
        new(2, "Table of contents", "Table of contents", "Done", "29", "24", "Eddie Lake"),
        new(3, "Executive summary", "Narrative", "Done", "10", "13", "Eddie Lake"),
        new(4, "Technical approach", "Narrative", "Done", "27", "23", "Jamik Tashpulatov"),
        new(5, "Design", "Narrative", "In Process", "2", "16", "Jamik Tashpulatov"),
        new(6, "Capabilities", "Narrative", "In Process", "20", "8", "Jamik Tashpulatov"),
        new(7, "Integration with existing systems", "Narrative", "In Process", "19", "21", "Jamik Tashpulatov"),
        new(8, "Innovation and Advantages", "Narrative", "Done", "25", "26", "Assign reviewer"),
        new(9, "Overview of EMR's Innovative Solutions", "Technical content", "Done", "7", "23", "Assign reviewer"),
        new(10, "Advanced Algorithms and Machine Learning", "Narrative", "Done", "30", "28", "Assign reviewer"),
        new(11, "Adaptive Communication Protocols", "Narrative", "Done", "9", "31", "Assign reviewer"),
        new(12, "Advantages Over Current Technologies", "Narrative", "Done", "12", "0", "Assign reviewer"),
        new(13, "Past Performance", "Narrative", "Done", "22", "33", "Assign reviewer"),
        new(14, "Customer Feedback and Satisfaction Levels", "Narrative", "Done", "15", "34", "Assign reviewer"),
        new(15, "Implementation Challenges and Solutions", "Narrative", "Done", "3", "35", "Assign reviewer"),
        new(16, "Security Measures and Data Protection Policies", "Narrative", "In Process", "6", "36", "Assign reviewer"),
        new(17, "Scalability and Future Proofing", "Narrative", "Done", "4", "37", "Assign reviewer"),
        new(18, "Cost-Benefit Analysis", "Plain language", "Done", "14", "38", "Assign reviewer"),
        new(19, "User Training and Onboarding Experience", "Narrative", "Done", "17", "39", "Assign reviewer"),
        new(20, "Future Development Roadmap", "Narrative", "Done", "11", "40", "Assign reviewer"),
        new(21, "System Architecture Overview", "Technical content", "In Process", "24", "18", "Maya Johnson"),
        new(22, "Risk Management Plan", "Narrative", "Done", "15", "22", "Carlos Rodriguez"),
        new(23, "Compliance Documentation", "Legal", "In Process", "31", "27", "Sarah Chen"),
        new(24, "API Documentation", "Technical content", "Done", "8", "12", "Raj Patel"),
        new(25, "User Interface Mockups", "Visual", "In Process", "19", "25", "Leila Ahmadi"),
        new(26, "Database Schema", "Technical content", "Done", "22", "20", "Thomas Wilson"),
        new(27, "Testing Methodology", "Technical content", "In Process", "17", "14", "Assign reviewer"),
        new(28, "Deployment Strategy", "Narrative", "Done", "26", "30", "Eddie Lake"),
        new(29, "Budget Breakdown", "Financial", "In Process", "13", "16", "Jamik Tashpulatov"),
        new(30, "Market Analysis", "Research", "Done", "29", "32", "Sophia Martinez"),
        new(31, "Competitor Comparison", "Research", "In Process", "21", "19", "Assign reviewer"),
        new(32, "Maintenance Plan", "Technical content", "Done", "16", "23", "Alex Thompson"),
        new(33, "User Personas", "Research", "In Process", "27", "24", "Nina Patel"),
        new(34, "Accessibility Compliance", "Legal", "Done", "18", "21", "Assign reviewer"),
        new(35, "Performance Metrics", "Technical content", "In Process", "23", "26", "David Kim"),
        new(36, "Disaster Recovery Plan", "Technical content", "Done", "14", "17", "Jamik Tashpulatov"),
        new(37, "Third-party Integrations", "Technical content", "In Process", "25", "28", "Eddie Lake"),
        new(38, "User Feedback Summary", "Research", "Done", "20", "15", "Assign reviewer"),
        new(39, "Localization Strategy", "Narrative", "In Process", "12", "19", "Maria Garcia"),
        new(40, "Mobile Compatibility", "Technical content", "Done", "28", "31", "James Wilson"),
        new(41, "Data Migration Plan", "Technical content", "In Process", "19", "22", "Assign reviewer"),
        new(42, "Quality Assurance Protocols", "Technical content", "Done", "30", "33", "Priya Singh"),
        new(43, "Stakeholder Analysis", "Research", "In Process", "11", "14", "Eddie Lake"),
        new(44, "Environmental Impact Assessment", "Research", "Done", "24", "27", "Assign reviewer"),
        new(45, "Intellectual Property Rights", "Legal", "In Process", "17", "20", "Sarah Johnson"),
        new(46, "Customer Support Framework", "Narrative", "Done", "22", "25", "Jamik Tashpulatov"),
        new(47, "Version Control Strategy", "Technical content", "In Process", "15", "18", "Assign reviewer"),
        new(48, "Continuous Integration Pipeline", "Technical content", "Done", "26", "29", "Michael Chen"),
        new(49, "Regulatory Compliance", "Legal", "In Process", "13", "16", "Assign reviewer"),
        new(50, "User Authentication System", "Technical content", "Done", "28", "31", "Eddie Lake"),
        new(51, "Data Analytics Framework", "Technical content", "In Process", "21", "24", "Jamik Tashpulatov"),
        new(52, "Cloud Infrastructure", "Technical content", "Done", "16", "19", "Assign reviewer"),
        new(53, "Network Security Measures", "Technical content", "In Process", "29", "32", "Lisa Wong"),
        new(54, "Project Timeline", "Planning", "Done", "14", "17", "Eddie Lake"),
        new(55, "Resource Allocation", "Planning", "In Process", "27", "30", "Assign reviewer"),
        new(56, "Team Structure and Roles", "Planning", "Done", "20", "23", "Jamik Tashpulatov"),
        new(57, "Communication Protocols", "Planning", "In Process", "15", "18", "Assign reviewer"),
        new(58, "Success Metrics", "Planning", "Done", "30", "33", "Eddie Lake"),
        new(59, "Internationalization Support", "Technical content", "In Process", "23", "26", "Jamik Tashpulatov"),
        new(60, "Backup and Recovery Procedures", "Technical content", "Done", "18", "21", "Assign reviewer"),
        new(61, "Monitoring and Alerting System", "Technical content", "In Process", "25", "28", "Daniel Park"),
        new(62, "Code Review Guidelines", "Technical content", "Done", "12", "15", "Eddie Lake"),
        new(63, "Documentation Standards", "Technical content", "In Process", "27", "30", "Jamik Tashpulatov"),
        new(64, "Release Management Process", "Planning", "Done", "22", "25", "Assign reviewer"),
        new(65, "Feature Prioritization Matrix", "Planning", "In Process", "19", "22", "Emma Davis"),
        new(66, "Technical Debt Assessment", "Technical content", "Done", "24", "27", "Eddie Lake"),
        new(67, "Capacity Planning", "Planning", "In Process", "21", "24", "Jamik Tashpulatov"),
        new(68, "Service Level Agreements", "Legal", "Done", "26", "29", "Assign reviewer"),

    ];
}