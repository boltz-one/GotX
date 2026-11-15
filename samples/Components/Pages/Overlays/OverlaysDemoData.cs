using GotX;
using GotX.Components;

/// <summary>Dữ liệu mẫu cho trang Overlays (menu, liên kết, lệnh) – tách riêng để trang chỉ còn phần dựng giao diện.</summary>
public static class OverlaysDemoData
{
    /// <summary>Menu tài khoản: nhãn, phím tắt, dòng tắt, checkbox, dòng nguy hiểm.</summary>
    public static MenuItem[] AccountMenu() =>
    [
        MenuItem.Label("Tài khoản của tôi"),
        MenuItem.Action("Hồ sơ", "Ctrl+Shift+P"),
        MenuItem.Action("Thanh toán", "Ctrl+B"),
        MenuItem.Action("Cài đặt", "Ctrl+S"),
        MenuItem.Separator(),
        MenuItem.Check("Thanh trạng thái", true),
        MenuItem.Check("Bảng hoạt động"),
        MenuItem.Separator(),
        MenuItem.Sub("Mời người dùng", [MenuItem.Action("Email"), MenuItem.Action("Tin nhắn"), MenuItem.Separator(), MenuItem.Action("Khác…")]),
        MenuItem.Action("API", disabled: true),
        MenuItem.Action("Đăng xuất", "Ctrl+Shift+Q", destructive: true),
    ];

    /// <summary>Menu chuột phải kiểu trình duyệt.</summary>
    public static MenuItem[] BrowserMenu() =>
    [
        MenuItem.Action("Quay lại", "Alt+←"),
        MenuItem.Action("Tiến tới", "Alt+→", disabled: true),
        MenuItem.Action("Tải lại", "Ctrl+R"),
        MenuItem.Separator(),
        MenuItem.Check("Hiện dấu trang", true),
        MenuItem.Check("Hiện đường dẫn đầy đủ"),
        MenuItem.Separator(),
        MenuItem.Action("Xoá", destructive: true),
    ];

    /// <summary>Các menu của thanh menu.</summary>
    public static MenubarMenu[] AppMenus() =>
    [
        new("Tệp", [MenuItem.Action("Tab mới", "Ctrl+T"), MenuItem.Action("Cửa sổ mới", "Ctrl+N"), MenuItem.Separator(),
            MenuItem.Sub("Chia sẻ", [MenuItem.Action("Liên kết email"), MenuItem.Action("Tin nhắn")]), MenuItem.Action("In…", "Ctrl+P")]),
        new("Sửa", [MenuItem.Action("Hoàn tác", "Ctrl+Z"), MenuItem.Action("Làm lại", "Ctrl+Shift+Z"), MenuItem.Separator(), MenuItem.Action("Cắt"), MenuItem.Action("Chép")]),
        new("Xem", [MenuItem.Check("Luôn hiện thanh dấu trang"), MenuItem.Check("Luôn hiện URL đầy đủ", true), MenuItem.Separator(), MenuItem.Action("Toàn màn hình")]),
    ];

    /// <summary>Mục điều hướng: hai mục mở tấm và một liên kết thẳng.</summary>
    public static NavigationMenuItem[] Navigation() =>
    [
        new("Bắt đầu",
        [
            new("Giới thiệu", "Component dựng lại theo shadcn/ui trên GotX."),
            new("Cài đặt", "Thêm GotX.Components vào dự án."),
            new("Kiểu chữ", "Thang chữ và màu dùng chung."),
        ]),
        new("Component",
        [
            new("Alert Dialog", "Hộp thoại xác nhận buộc chọn."),
            new("Hover Card", "Xem trước nội dung khi rê chuột."),
            new("Progress", "Thanh tiến độ."),
            new("Tooltip", "Chú thích ngắn khi rê chuột."),
        ], Columns: 2),
        new("Tài liệu"),
    ];

    /// <summary>Nhóm lệnh của bảng lệnh.</summary>
    public static CommandGroup[] Commands() =>
    [
        new("Gợi ý", [new CommandItem("Lịch"), new CommandItem("Tìm emoji"), new CommandItem("Máy tính", Disabled: true)]),
        new("Cài đặt", [new CommandItem("Hồ sơ", "Ctrl+P"), new CommandItem("Thanh toán", "Ctrl+B"), new CommandItem("Cài đặt", "Ctrl+S")]),
    ];

    public static readonly string[] Frameworks = ["Next.js", "SvelteKit", "Nuxt.js", "Remix", "Astro"];
}
