using GotX;
using GotX.Components;

/// <summary>Dữ liệu và khối dựng cho trang gallery Navigation (tách ra để NavigationPage gọn dưới 200 dòng).</summary>
static partial class NavigationDemos
{
    public static readonly AccordionItem[] FaqItems =
    [
        new("Có hỗ trợ dark mode không?", "Có. Mọi token màu của shadcn tự đổi theo window.DarkMode."),
        new("Có cần cài package nào không?", "Không. GotX.Components chỉ dựa trên GotX.Core, không có NuGet package."),
        new("Dùng bàn phím được không?", "Được. Tab tới tiêu đề, ↑/↓ đi giữa các mục, Enter hoặc Space để mở/đóng."),
    ];

    static readonly string ChevronBox = Ui.ButtonClasses(ButtonVariant.Ghost, ButtonSize.IconSm);
    static readonly string[] Repos = ["@radix-ui/primitives", "@radix-ui/colors", "@stitches/react"];

    /// <summary>Phần đầu collapsible: tiêu đề + nút icon, như ví dụ của shadcn.</summary>
    public static Div RepoHeader(Window w) =>
        w.Div().Class("flex flex-col gap-2")
            .Child(w.Div().Class("flex items-center justify-between gap-4 px-1")
                .Child(w.Div().Class("text-sm font-semibold").Child("@peduarte đã gắn sao 3 repo"))
                // Cả phần đầu là một nút (Collapsible bọc nó), nên ô icon chỉ mang hình dáng nút chứ không tự focus.
                .Child(w.Div().Class(ChevronBox).Child(Icon.ChevronDown(w))))
            .Child(RepoRow(w, Repos[0]));

    /// <summary>Phần chỉ hiện khi mở.</summary>
    public static Div RepoList(Window w) =>
        w.Div().Class("flex flex-col gap-2").Child(RepoRow(w, Repos[1])).Child(RepoRow(w, Repos[2]));

    static Div RepoRow(Window w, string name) =>
        w.Div().Class("rounded-md border border-border px-4 py-2 text-sm").Child(name);

    static readonly string[] TabBodies =
    [
        "Đổi thông tin tài khoản ở đây. Bấm lưu khi xong.",
        "Đổi mật khẩu ở đây. Sau khi lưu bạn sẽ bị đăng xuất.",
        "Chọn loại thông báo bạn muốn nhận.",
    ];

    /// <summary>Nội dung tab theo chỉ số.</summary>
    public static Div TabContent(Window w, int index) =>
        w.Div().Class("rounded-lg border border-border p-3 text-muted-foreground").Child(TabBodies[index % TabBodies.Length]);

    static readonly string[] PageLabels = Enumerable.Range(0, 11).Select(p => $"Đang ở trang {p}").ToArray();

    /// <summary>Chữ "Đang ở trang n" dựng sẵn.</summary>
    public static string PageLabel(int page) => PageLabels[page];

    /// <summary>Breadcrumb mẫu: Trang chủ / … / Components / Breadcrumb.</summary>
    public static Div Breadcrumb(Window w) =>
        Ui.Breadcrumb(w)
            .Child(Ui.BreadcrumbLink(w, "Trang chủ")).Child(Ui.BreadcrumbSeparator(w))
            .Child(Ui.BreadcrumbEllipsis(w)).Child(Ui.BreadcrumbSeparator(w))
            .Child(Ui.BreadcrumbLink(w, "Components")).Child(Ui.BreadcrumbSeparator(w))
            .Child(Ui.BreadcrumbPage(w, "Breadcrumb"));

    /// <summary>Hàm dựng nội dung panel resizable có nhãn.</summary>
    public static Func<Window, Div> Panel(string label) =>
        w => w.Div().Class("flex items-center justify-center p-4 text-sm font-semibold").Child(label);

    static readonly string[] SlideLabels = ["1", "2", "3", "4", "5"];

    /// <summary>Slide vuông có số ở giữa.</summary>
    public static Div Slide(Window w, int index) =>
        w.Div().Class("p-1").Child(w.Div().Class("flex h-36 items-center justify-center rounded-xl border border-border bg-card text-4xl font-semibold")
            .Child(SlideLabels[index % SlideLabels.Length]));

    /// <summary>Hai vùng cuộn: danh sách thẻ dọc và dải ảnh ngang (hộp màu).</summary>
    public static Div ScrollAreas(Window w)
    {
        var list = Ui.ScrollArea(w, "nav-demo-scroll-v").Class("h-40 w-40 shrink-0 rounded-md border border-border");
        var inner = w.Div().Class("p-4").Child(w.Div().Class("mb-4 text-sm font-medium").Child("Tags"));
        foreach (var tag in Tags)
            inner.Child(w.Div().Class("text-sm py-1.5 border-b border-border").Child(tag));
        list.Child(inner);

        var strip = w.Div().Class("flex gap-3 p-3");
        foreach (var color in Swatches)
            strip.Child(w.Div().Class(color));
        return w.Div().Class("flex items-start gap-4")
            .Child(list)
            .Child(Ui.ScrollArea(w, "nav-demo-scroll-h", ScrollAxis.Horizontal).Class("flex-1 rounded-md border border-border").Child(strip));
    }

    static readonly string[] Tags = Enumerable.Range(1, 30).Select(i => $"v1.2.0-beta.{30 - i + 1}").ToArray();
    static readonly string[] Swatches =
    [
        "h-24 w-36 shrink-0 rounded-md bg-chart-1", "h-24 w-36 shrink-0 rounded-md bg-chart-2", "h-24 w-36 shrink-0 rounded-md bg-chart-3",
        "h-24 w-36 shrink-0 rounded-md bg-chart-4", "h-24 w-36 shrink-0 rounded-md bg-chart-5",
    ];
}
