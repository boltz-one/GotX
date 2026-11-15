using GotX;

/// <summary>
/// Icon mà <c>GotX.Components.Icon</c> chưa có. Trang gốc dùng Hugeicons; ở đây lấy hình Lucide cùng nghĩa (cùng lưới 24px,
/// nét 2px như các icon sẵn có). Tạo một lần – <see cref="VectorPath"/> không được dựng trong Render.
/// </summary>
static class HomeIcons
{
    static Div Draw(Window w, VectorPath path) => w.Path(path).Class("size-4 shrink-0");

    static readonly VectorPath ChartColumnPath = VectorPath.Icon("M3 3v16a2 2 0 0 0 2 2h16", "M18 17V9", "M13 17V5", "M8 17v-3");
    static readonly VectorPath ArrowLeftRightPath = VectorPath.Icon("M8 3 4 7l4 4", "M4 7h16", "m16 21 4-4-4-4", "M20 17H4");
    static readonly VectorPath TrendingUpPath = VectorPath.Icon("polyline 22,7 13.5,15.5 8.5,10.5 2,17", "polyline 16,7 22,7 22,13");
    static readonly VectorPath LandmarkPath = VectorPath.Icon("line 3 22 21 22", "line 6 18 6 11", "line 10 18 10 11", "line 14 18 14 11", "line 18 18 18 11", "polygon 12,2 20,7 4,7");
    static readonly VectorPath PieChartPath = VectorPath.Icon("M21.21 15.89A10 10 0 1 1 8 2.83", "M22 12A10 10 0 0 0 12 2v10z");
    static readonly VectorPath WalletPath = VectorPath.Icon("M21 12V7H5a2 2 0 0 1 0-4h14v4", "M3 5v14a2 2 0 0 0 2 2h16v-5", "M18 12a2 2 0 0 0 0 4h4v-4Z");
    static readonly VectorPath ChartLinePath = VectorPath.Icon("M3 3v16a2 2 0 0 0 2 2h16", "m19 9-5 5-4-4-3 3");
    static readonly VectorPath TargetPath = VectorPath.Icon("circle 12 12 10", "circle 12 12 6", "circle 12 12 2");
    static readonly VectorPath HelpCirclePath = VectorPath.Icon("circle 12 12 10", "M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3", "M12 17h.01");
    static readonly VectorPath BookOpenPath = VectorPath.Icon("M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z", "M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z");
    static readonly VectorPath ActivityPath = VectorPath.Icon("polyline 22,12 18,12 15,21 9,3 6,12 2,12");
    static readonly VectorPath CreditCardPath = VectorPath.Icon("rect 2 5 20 14 2", "line 2 10 22 10");
    static readonly VectorPath ShieldPath = VectorPath.Icon("M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z");
    static readonly VectorPath PalettePath = VectorPath.Icon("circle 13.5 6.5 0.5", "circle 17.5 10.5 0.5", "circle 8.5 7.5 0.5", "circle 6.5 12.5 0.5",
        "M12 2C6.5 2 2 6.5 2 12s4.5 10 10 10c.926 0 1.648-.746 1.648-1.688 0-.437-.18-.835-.437-1.125-.29-.289-.438-.652-.438-1.125a1.64 1.64 0 0 1 1.668-1.668h1.996c3.051 0 5.555-2.503 5.555-5.554C21.965 6.012 17.461 2 12 2z");
    static readonly VectorPath CircleEllipsisPath = VectorPath.Icon("circle 12 12 10", "M17 12h.01", "M12 12h.01", "M7 12h.01");
    static readonly VectorPath RotateCwPath = VectorPath.Icon("M21 12a9 9 0 1 1-9-9c2.52 0 4.93 1 6.74 2.74L21 8", "M21 3v5h-5");
    static readonly VectorPath TelescopePath = VectorPath.Icon("circle 12 13 2", "m13.5 14.7 2.5 6.3", "m10.5 14.7-2.5 6.3", "m4 9 13-5 2 5-13 5z", "M2 11l2-1");

    public static Div ChartColumn(Window w) => Draw(w, ChartColumnPath);
    public static Div ArrowLeftRight(Window w) => Draw(w, ArrowLeftRightPath);
    public static Div TrendingUp(Window w) => Draw(w, TrendingUpPath);
    public static Div Landmark(Window w) => Draw(w, LandmarkPath);
    public static Div PieChart(Window w) => Draw(w, PieChartPath);
    public static Div Wallet(Window w) => Draw(w, WalletPath);
    public static Div ChartLine(Window w) => Draw(w, ChartLinePath);
    public static Div Target(Window w) => Draw(w, TargetPath);
    public static Div HelpCircle(Window w) => Draw(w, HelpCirclePath);
    public static Div BookOpen(Window w) => Draw(w, BookOpenPath);
    public static Div Activity(Window w) => Draw(w, ActivityPath);
    public static Div CreditCard(Window w) => Draw(w, CreditCardPath);
    public static Div Shield(Window w) => Draw(w, ShieldPath);
    public static Div Palette(Window w) => Draw(w, PalettePath);
    public static Div CircleEllipsis(Window w) => Draw(w, CircleEllipsisPath);
    public static Div RotateCw(Window w) => Draw(w, RotateCwPath);
    /// <summary>Kính viễn vọng giản lược (mục "Deep Research") – hình Lucide gốc quá nhiều nét cho ô 16px.</summary>
    public static Div Telescope(Window w) => Draw(w, TelescopePath);
}
