namespace GotX.Components;

/// <summary>
/// Icon Lucide mà các block sidebar của shadcn dùng nhưng <see cref="Icon"/> chưa có. Mỗi path tạo một lần
/// (static readonly): một instance VectorPath giữ một mask riêng trong atlas, không được tạo lại mỗi frame.
/// </summary>
public static class SidebarIcons
{
    static VectorPath I(params string[] parts) => VectorPath.Icon(parts);

    static readonly VectorPath GalleryVerticalEndPath = I("M7 2h10", "M5 6h14", "rect 3 10 18 12 2");
    static readonly VectorPath AudioWaveformPath = I(
        "M2 13a2 2 0 0 0 2-2V7a2 2 0 0 1 4 0v13a2 2 0 0 0 4 0V4a2 2 0 0 1 4 0v13a2 2 0 0 0 4 0v-4a2 2 0 0 1 2-2");
    static readonly VectorPath CommandPath = I("M15 6v12a3 3 0 1 0 3-3H6a3 3 0 1 0 3 3V6a3 3 0 1 0-3 3h12a3 3 0 1 0-3-3");
    static readonly VectorPath SquareTerminalPath = I("m7 11 2-2-2-2", "M11 13h4", "rect 3 3 18 18 2");
    static readonly VectorPath BotPath = I("M12 8V4H8", "rect 4 8 16 12 2", "M2 14h2", "M20 14h2", "M15 13v2", "M9 13v2");
    static readonly VectorPath BookOpenPath = I(
        "M12 7v14",
        "M3 18a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1h5a4 4 0 0 1 4 4 4 4 0 0 1 4-4h5a1 1 0 0 1 1 1v13a1 1 0 0 1-1 1h-6a3 3 0 0 0-3 3 3 3 0 0 0-3-3z");
    static readonly VectorPath Settings2Path = I("M14 17H5", "M19 7h-9", "circle 17 17 3", "circle 7 7 3");
    static readonly VectorPath FramePath = I("line 22 6 2 6", "line 22 18 2 18", "line 6 2 6 22", "line 18 2 18 22");
    static readonly VectorPath PieChartPath = I(
        "M21 12c.552 0 1.005-.449.95-.998a10 10 0 0 0-8.953-8.951c-.55-.055-.998.398-.998.95v8a1 1 0 0 0 1 1z",
        "M21.21 15.89A10 10 0 1 1 8 2.83");
    static readonly VectorPath MapPath = I(
        "M14.106 5.553a2 2 0 0 0 1.788 0l3.659-1.83A1 1 0 0 1 21 4.619v12.764a1 1 0 0 1-.553.894l-4.553 2.277a2 2 0 0 1-1.788 0l-4.212-2.106a2 2 0 0 0-1.788 0l-3.659 1.83A1 1 0 0 1 3 19.381V6.618a1 1 0 0 1 .553-.894l4.553-2.277a2 2 0 0 1 1.788 0z",
        "M15 5.764v15", "M9 3.236v15");
    static readonly VectorPath ForwardPath = I("m15 17 5-5-5-5", "M4 18v-2a4 4 0 0 1 4-4h12");
    static readonly VectorPath Trash2Path = I(
        "M10 11v6", "M14 11v6", "M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6", "M3 6h18", "M8 6V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2");
    static readonly VectorPath SparklesPath = I(
        "M9.937 15.5A2 2 0 0 0 8.5 14.063l-6.135-1.582a.5.5 0 0 1 0-.962L8.5 9.936A2 2 0 0 0 9.937 8.5l1.582-6.135a.5.5 0 0 1 .963 0L14.063 8.5A2 2 0 0 0 15.5 9.937l6.135 1.581a.5.5 0 0 1 0 .964L15.5 14.063a2 2 0 0 0-1.437 1.437l-1.582 6.135a.5.5 0 0 1-.963 0z",
        "M20 3v4", "M22 5h-4", "M4 17v2", "M5 18H3");
    static readonly VectorPath BadgeCheckPath = I(
        "M3.85 8.62a4 4 0 0 1 4.78-4.77 4 4 0 0 1 6.74 0 4 4 0 0 1 4.78 4.78 4 4 0 0 1 0 6.74 4 4 0 0 1-4.77 4.78 4 4 0 0 1-6.75 0 4 4 0 0 1-4.78-4.77 4 4 0 0 1 0-6.76Z",
        "m9 12 2 2 4-4");
    static readonly VectorPath CreditCardPath = I("rect 2 5 20 14 2", "line 2 10 22 10");

    static Div Draw(Window w, VectorPath path) => w.Path(path).Class("size-4 shrink-0");

    public static Div GalleryVerticalEnd(Window w) => Draw(w, GalleryVerticalEndPath);
    public static Div AudioWaveform(Window w) => Draw(w, AudioWaveformPath);
    public static Div Command(Window w) => Draw(w, CommandPath);
    public static Div SquareTerminal(Window w) => Draw(w, SquareTerminalPath);
    public static Div Bot(Window w) => Draw(w, BotPath);
    public static Div BookOpen(Window w) => Draw(w, BookOpenPath);
    public static Div Settings2(Window w) => Draw(w, Settings2Path);
    public static Div Frame(Window w) => Draw(w, FramePath);
    public static Div PieChart(Window w) => Draw(w, PieChartPath);
    public static Div Map(Window w) => Draw(w, MapPath);
    public static Div Forward(Window w) => Draw(w, ForwardPath);
    public static Div Trash2(Window w) => Draw(w, Trash2Path);
    public static Div Sparkles(Window w) => Draw(w, SparklesPath);
    public static Div BadgeCheck(Window w) => Draw(w, BadgeCheckPath);
    public static Div CreditCard(Window w) => Draw(w, CreditCardPath);
}
