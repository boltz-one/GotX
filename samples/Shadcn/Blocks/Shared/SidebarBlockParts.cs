using GotX;
using GotX.Components;

/// <summary>Phần mà sidebar-07 và sidebar-03 dùng chung: breadcrumb của header, ô logo, vùng nội dung giữ chỗ.</summary>
static class SidebarBlockParts
{
    /// <summary>Breadcrumb "Build Your Application › Data Fetching"; mục đầu và dấu phân cách ẩn dưới md như bản gốc.</summary>
    public static Div Breadcrumb(Window w) =>
        Ui.Breadcrumb(w)
            .Child(Ui.BreadcrumbLink(w, "Build Your Application").Class("hidden md:block").Key("crumb-parent"))
            .Child(Ui.BreadcrumbSeparator(w).Class("hidden md:flex"))
            .Child(Ui.BreadcrumbPage(w, "Data Fetching"));

    /// <summary>Đường kẻ dọc cao 16px giữa nút trigger và breadcrumb (Separator orientation="vertical" className="mr-2 h-4").</summary>
    public static Div HeaderSeparator(Window w) =>
        Ui.Separator(w, SeparatorOrientation.Vertical).Class("mr-2 h-4 self-center");

    /// <summary>Ô logo 32px nền sidebar-primary ở đầu sidebar.</summary>
    public static Div LogoTile(Window w, Func<Window, Div> logo) =>
        w.Div().Class("flex size-8 shrink-0 items-center justify-center rounded-lg bg-sidebar-primary text-sidebar-primary-foreground")
            .Child(logo(w));

    /// <summary>
    /// Vùng nội dung giữ chỗ của bản gốc: ba ô 16:9 rồi một tấm lấp phần còn lại; tự cuộn khi cửa sổ thấp/hẹp.
    /// Core chưa có <c>aspect-video</c>: ô không có chiều cao riêng, padding đáy 56.25% (tính theo bề rộng ô) tạo tỉ lệ 16:9.
    /// </summary>
    public static Div Placeholder(Window w, string key, bool padTop)
    {
        var grid = w.Div().Class("grid shrink-0 gap-4 md:grid-cols-3");
        for (int i = 0; i < 3; i++) grid.Child(w.Div().Class("rounded-xl bg-muted/50 pb-[56.25%]"));
        return w.Div().Key(key)
            .Class(padTop ? "flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto p-4" : "flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto p-4 pt-0")
            .Child(grid)
            .Child(w.Div().Class("min-h-screen flex-1 rounded-xl bg-muted/50 md:min-h-0"));
    }
}
