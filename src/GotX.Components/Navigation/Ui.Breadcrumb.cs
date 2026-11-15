namespace GotX.Components;

/// <summary>
/// Breadcrumb: <c>Ui.Breadcrumb(w)</c> là hàng chứa, nối các <c>BreadcrumbLink / BreadcrumbSeparator / BreadcrumbEllipsis /
/// BreadcrumbPage</c>. Bỏ lớp nav/ol/li của HTML vì GotX không có ngữ nghĩa truy cập – chỉ giữ phần trình bày.
/// </summary>
public static partial class Ui
{
    /// <summary>Hàng breadcrumb, tự xuống dòng khi hẹp.</summary>
    public static Div Breadcrumb(Window w) =>
        w.Div().Class("flex flex-wrap items-center gap-1.5 text-sm text-muted-foreground");

    /// <summary>Một mục bọc link + phần phụ (vd. menu thả xuống) đứng sát nhau.</summary>
    public static Div BreadcrumbItem(Window w) => w.Div().Class("inline-flex items-center gap-1");

    /// <summary>
    /// Mục bấm được (focus được bằng Tab, Enter kích hoạt); người gọi nối <c>.OnClick(...)</c>. Breadcrumb có số mục thay đổi
    /// (thu gọn thành "…", đổi trang) thì nối thêm <c>.Key(...)</c> cho từng mục để focus bám đúng mục khi danh sách đổi.
    /// </summary>
    public static Div BreadcrumbLink(Window w, string text) =>
        w.Div().Class("cursor-pointer rounded-sm transition-colors hover:text-foreground focus-visible:text-foreground " + FocusRing.RingOnly)
            .Focusable().Child(text);

    /// <summary>Trang hiện tại: chữ đậm màu, không bấm được.</summary>
    public static Div BreadcrumbPage(Window w, string text) => w.Div().Class("font-normal text-foreground").Child(text);

    /// <summary>Dấu phân cách mặc định: mũi tên phải.</summary>
    public static Div BreadcrumbSeparator(Window w) => w.Div().Class("flex items-center").Child(Icon.ChevronRight(w));

    /// <summary>Dấu "…" thay cho các mục bị ẩn.</summary>
    public static Div BreadcrumbEllipsis(Window w) =>
        w.Div().Class("flex size-5 items-center justify-center").Child(Icon.MoreHorizontal(w));
}
