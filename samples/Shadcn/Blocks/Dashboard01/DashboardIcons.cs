using GotX;

/// <summary>
/// Icon Tabler (@tabler/icons 3.31, MIT) mà dashboard-01 dùng, chép nguyên path của bản gốc (<c>@tabler/icons-react</c>):
/// nét 2px đầu tròn trong ô 24 như Lucide nên dựng bằng <see cref="VectorPath.Icon"/>; hai icon "filled" tô đặc.
/// Mỗi path tạo một lần (một VectorPath chiếm một mask trong atlas).
/// </summary>
public static class DashboardIcons
{
    static VectorPath I(params string[] d) => VectorPath.Icon(d);

    public static readonly VectorPath DashboardPath = I("M12 13m-2 0a2 2 0 1 0 4 0a2 2 0 1 0 -4 0", "M13.45 11.55l2.05 -2.05", "M6.4 20a9 9 0 1 1 11.2 0z");
    public static readonly VectorPath ListDetailsPath = I("M13 5h8", "M13 9h5", "M13 15h8", "M13 19h5",
        "M3 4m0 1a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v4a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1z", "M3 14m0 1a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v4a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1z");
    public static readonly VectorPath ChartBarPath = I("M3 13a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v6a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1z",
        "M15 9a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v10a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1z", "M9 5a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v14a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1z", "M4 20h14");
    public static readonly VectorPath FolderPath = I("M5 4h4l3 3h7a2 2 0 0 1 2 2v8a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2v-11a2 2 0 0 1 2 -2");
    public static readonly VectorPath UsersPath = I("M9 7m-4 0a4 4 0 1 0 8 0a4 4 0 1 0 -8 0", "M3 21v-2a4 4 0 0 1 4 -4h4a4 4 0 0 1 4 4v2",
        "M16 3.13a4 4 0 0 1 0 7.75", "M21 21v-2a4 4 0 0 0 -3 -3.85");
    public static readonly VectorPath DatabasePath = I("M12 6m-8 0a8 3 0 1 0 16 0a8 3 0 1 0 -16 0", "M4 6v6a8 3 0 0 0 16 0v-6", "M4 12v6a8 3 0 0 0 16 0v-6");
    public static readonly VectorPath ReportPath = I("M8 5h-2a2 2 0 0 0 -2 2v12a2 2 0 0 0 2 2h5.697", "M18 14v4h4", "M18 11v-4a2 2 0 0 0 -2 -2h-2",
        "M8 3m0 2a2 2 0 0 1 2 -2h2a2 2 0 0 1 2 2v0a2 2 0 0 1 -2 2h-2a2 2 0 0 1 -2 -2z", "M18 18m-4 0a4 4 0 1 0 8 0a4 4 0 1 0 -8 0", "M8 11h4", "M8 15h3");
    public static readonly VectorPath FileWordPath = I("M14 3v4a1 1 0 0 0 1 1h4", "M17 21h-10a2 2 0 0 1 -2 -2v-14a2 2 0 0 1 2 -2h7l5 5v11a2 2 0 0 1 -2 2",
        "M9 12l1.333 5l1.667 -4l1.667 4l1.333 -5");
    public static readonly VectorPath DotsPath = I("M5 12m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0", "M12 12m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0",
        "M19 12m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0");
    public static readonly VectorPath SettingsPath = I(
        "M10.325 4.317c.426 -1.756 2.924 -1.756 3.35 0a1.724 1.724 0 0 0 2.573 1.066c1.543 -.94 3.31 .826 2.37 2.37a1.724 1.724 0 0 0 1.065 2.572" +
        "c1.756 .426 1.756 2.924 0 3.35a1.724 1.724 0 0 0 -1.066 2.573c.94 1.543 -.826 3.31 -2.37 2.37a1.724 1.724 0 0 0 -2.572 1.065" +
        "c-.426 1.756 -2.924 1.756 -3.35 0a1.724 1.724 0 0 0 -2.573 -1.066c-1.543 .94 -3.31 -.826 -2.37 -2.37a1.724 1.724 0 0 0 -1.065 -2.572" +
        "c-1.756 -.426 -1.756 -2.924 0 -3.35a1.724 1.724 0 0 0 1.066 -2.573c-.94 -1.543 .826 -3.31 2.37 -2.37c1 .608 2.296 .07 2.572 -1.065z",
        "M9 12a3 3 0 1 0 6 0a3 3 0 0 0 -6 0");
    public static readonly VectorPath HelpPath = I("M12 12m-9 0a9 9 0 1 0 18 0a9 9 0 1 0 -18 0", "M12 17l0 .01", "M12 13.5a1.5 1.5 0 0 1 1 -1.5a2.6 2.6 0 1 0 -3 -4");
    public static readonly VectorPath SearchPath = I("M10 10m-7 0a7 7 0 1 0 14 0a7 7 0 1 0 -14 0", "M21 21l-6 -6");
    public static readonly VectorPath InnerShadowTopPath = I("M5.636 5.636a9 9 0 1 0 12.728 12.728a9 9 0 0 0 -12.728 -12.728z", "M16.243 7.757a6 6 0 0 0 -8.486 0");
    public static readonly VectorPath MailPath = I("M3 7a2 2 0 0 1 2 -2h14a2 2 0 0 1 2 2v10a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2v-10z", "M3 7l9 6l9 -6");
    public static readonly VectorPath DotsVerticalPath = I("M12 12m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0", "M12 19m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0",
        "M12 5m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0");
    public static readonly VectorPath TrendingUpPath = I("M3 17l6 -6l4 4l8 -8", "M14 7l7 0l0 7");
    public static readonly VectorPath TrendingDownPath = I("M3 7l6 6l4 -4l8 8", "M21 10l0 7l-7 0");
    public static readonly VectorPath ChevronDownPath = I("M6 9l6 6l6 -6");
    public static readonly VectorPath ChevronLeftPath = I("M15 6l-6 6l6 6");
    public static readonly VectorPath ChevronRightPath = I("M9 6l6 6l-6 6");
    public static readonly VectorPath ChevronsLeftPath = I("M11 7l-5 5l5 5", "M17 7l-5 5l5 5");
    public static readonly VectorPath ChevronsRightPath = I("M7 7l5 5l-5 5", "M13 7l5 5l-5 5");
    public static readonly VectorPath GripVerticalPath = I("M9 5m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0", "M9 12m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0",
        "M9 19m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0", "M15 5m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0", "M15 12m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0",
        "M15 19m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0");
    public static readonly VectorPath LayoutColumnsPath = I("M4 4m0 2a2 2 0 0 1 2 -2h12a2 2 0 0 1 2 2v12a2 2 0 0 1 -2 2h-12a2 2 0 0 1 -2 -2z", "M12 4l0 16");
    public static readonly VectorPath LoaderPath = I("M12 6l0 -3", "M16.25 7.75l2.15 -2.15", "M18 12l3 0", "M16.25 16.25l2.15 2.15", "M12 18l0 3",
        "M7.75 16.25l-2.15 2.15", "M6 12l-3 0", "M7.75 7.75l-2.15 -2.15");
    public static readonly VectorPath PlusPath = I("M12 5l0 14", "M5 12l14 0");
    public static readonly VectorPath Share3Path = I("M13 4v4c-6.575 1.028 -9.02 6.788 -10 12c-.037 .206 5.384 -5.962 10 -6v4l8 -7l-8 -7z");
    public static readonly VectorPath TrashPath = I("M4 7l16 0", "M10 11l0 6", "M14 11l0 6", "M5 7l1 12a2 2 0 0 0 2 2h8a2 2 0 0 0 2 -2l1 -12",
        "M9 7v-3a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v3");
    public static readonly VectorPath UserCirclePath = I("M12 12m-9 0a9 9 0 1 0 18 0a9 9 0 1 0 -18 0", "M12 10m-3 0a3 3 0 1 0 6 0a3 3 0 1 0 -6 0",
        "M6.168 18.849a4 4 0 0 1 3.832 -2.849h4a4 4 0 0 1 3.834 2.855");
    public static readonly VectorPath CreditCardPath = I("M3 5m0 3a3 3 0 0 1 3 -3h12a3 3 0 0 1 3 3v8a3 3 0 0 1 -3 3h-12a3 3 0 0 1 -3 -3z", "M3 10l18 0",
        "M7 15l.01 0", "M11 15l2 0");
    public static readonly VectorPath NotificationPath = I("M10 6h-3a2 2 0 0 0 -2 2v9a2 2 0 0 0 2 2h9a2 2 0 0 0 2 -2v-3", "M17 7m-3 0a3 3 0 1 0 6 0a3 3 0 1 0 -6 0");
    public static readonly VectorPath LogoutPath = I("M14 8v-2a2 2 0 0 0 -2 -2h-7a2 2 0 0 0 -2 2v12a2 2 0 0 0 2 2h7a2 2 0 0 0 2 -2v-2", "M9 12h12l-3 -3", "M18 15l3 -3");
    public static readonly VectorPath CirclePlusFilledPath = VectorPath.FilledIcon(
        "M4.929 4.929a10 10 0 1 1 14.141 14.141a10 10 0 0 1 -14.14 -14.14zm8.071 4.071a1 1 0 1 0 -2 0v2h-2a1 1 0 1 0 0 2h2v2a1 1 0 1 0 2 0v-2h2a1 1 0 1 0 0 -2h-2v-2z");
    public static readonly VectorPath CircleCheckFilledPath = VectorPath.FilledIcon(
        "M17 3.34a10 10 0 1 1 -14.995 8.984l-.005 -.324l.005 -.324a10 10 0 0 1 14.995 -8.336zm-1.293 5.953a1 1 0 0 0 -1.32 -.083l-.094 .083" +
        "l-3.293 3.292l-1.293 -1.292l-.094 -.083a1 1 0 0 0 -1.403 1.403l.083 .094l2 2l.094 .083a1 1 0 0 0 1.226 0l.094 -.083l4 -4l.083 -.094" +
        "a1 1 0 0 0 -.083 -1.32z");

    /// <summary>Icon 16px (cỡ mặc định <c>[&amp;_svg:not([class*='size-'])]:size-4</c> của shadcn).</summary>
    public static Div Draw(Window w, VectorPath path) => w.Path(path).Class("size-4 shrink-0");

    public static Div Dashboard(Window w) => Draw(w, DashboardPath);
    public static Div ListDetails(Window w) => Draw(w, ListDetailsPath);
    public static Div ChartBar(Window w) => Draw(w, ChartBarPath);
    public static Div Folder(Window w) => Draw(w, FolderPath);
    public static Div Users(Window w) => Draw(w, UsersPath);
    public static Div Database(Window w) => Draw(w, DatabasePath);
    public static Div Report(Window w) => Draw(w, ReportPath);
    public static Div FileWord(Window w) => Draw(w, FileWordPath);
    public static Div Dots(Window w) => Draw(w, DotsPath);
    public static Div Settings(Window w) => Draw(w, SettingsPath);
    public static Div Help(Window w) => Draw(w, HelpPath);
    public static Div Search(Window w) => Draw(w, SearchPath);
    public static Div DotsVertical(Window w) => Draw(w, DotsVerticalPath);
    public static Div TrendingUp(Window w) => Draw(w, TrendingUpPath);
    public static Div TrendingDown(Window w) => Draw(w, TrendingDownPath);
}
