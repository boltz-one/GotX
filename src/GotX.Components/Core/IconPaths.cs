namespace GotX.Components;

/// <summary>
/// Dữ liệu path của icon Lucide (viewBox 24, nét 2, đầu tròn). Mỗi <see cref="VectorPath"/> giữ một mask riêng trong
/// atlas nên chỉ được tạo một lần ở đây; <see cref="Icon"/> chỉ bọc chúng thành phần tử. Phần còn lại ở IconPaths.More.cs.
/// </summary>
static partial class IconPaths
{
    static VectorPath I(params string[] parts) => VectorPath.Icon(parts);

    // --- dấu, mũi tên
    public static readonly VectorPath Check = I("M20 6 9 17l-5-5");
    public static readonly VectorPath X = I("M18 6 6 18", "m6 6 12 12");
    public static readonly VectorPath Plus = I("M5 12h14", "M12 5v14");
    public static readonly VectorPath Minus = I("M5 12h14");
    public static readonly VectorPath ChevronDown = I("m6 9 6 6 6-6");
    public static readonly VectorPath ChevronUp = I("m18 15-6-6-6 6");
    public static readonly VectorPath ChevronLeft = I("m15 18-6-6 6-6");
    public static readonly VectorPath ChevronRight = I("m9 18 6-6-6-6");
    public static readonly VectorPath ChevronsUpDown = I("m7 15 5 5 5-5", "m7 9 5-5 5 5");
    public static readonly VectorPath ArrowLeft = I("m12 19-7-7 7-7", "M19 12H5");
    public static readonly VectorPath ArrowRight = I("M5 12h14", "m12 5 7 7-7 7");
    public static readonly VectorPath ArrowUp = I("m5 12 7-7 7 7", "M12 19V5");
    public static readonly VectorPath ArrowDown = I("M12 5v14", "m19 12-7 7-7-7");
    public static readonly VectorPath CornerDownLeft = I("M20 4v7a4 4 0 0 1-4 4H4", "m9 10-5 5 5 5");
    public static readonly VectorPath ExternalLink = I("M15 3h6v6", "M10 14 21 3", "M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6");

    // --- chấm, vòng
    // Dot không phải "dot" của Lucide (bán kính 1): đây là chấm đặc 8px dùng cho radio/trạng thái như bản trước.
    public static readonly VectorPath Dot = VectorPath.FilledIcon("circle 12 12 6");
    public static readonly VectorPath Circle = I("circle 12 12 10");
    public static readonly VectorPath MoreHorizontal = I("circle 12 12 1", "circle 19 12 1", "circle 5 12 1");
    public static readonly VectorPath MoreVertical = I("circle 12 12 1", "circle 12 5 1", "circle 12 19 1");
    public static readonly VectorPath GripVertical = I("circle 9 12 1", "circle 9 5 1", "circle 9 19 1", "circle 15 12 1", "circle 15 5 1", "circle 15 19 1");
    public static readonly VectorPath LoaderCircle = I("M21 12a9 9 0 1 1-6.219-8.56");
    public static readonly VectorPath CircleCheck = I("circle 12 12 10", "m9 12 2 2 4-4");
    public static readonly VectorPath CircleX = I("circle 12 12 10", "m15 9-6 6", "m9 9 6 6");
    public static readonly VectorPath Info = I("circle 12 12 10", "M12 16v-4", "M12 8h.01");
    public static readonly VectorPath AlertCircle = I("circle 12 12 10", "line 12 8 12 12", "line 12 16 12.01 16");
    public static readonly VectorPath TriangleAlert = I("m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3", "M12 9v4", "M12 17h.01");
    public static readonly VectorPath OctagonX = I("m15 9-6 6",
        "M2.586 16.726A2 2 0 0 1 2 15.312V8.688a2 2 0 0 1 .586-1.414l4.688-4.688A2 2 0 0 1 8.688 2h6.624a2 2 0 0 1 1.414.586l4.688 4.688A2 2 0 0 1 22 8.688v6.624a2 2 0 0 1-.586 1.414l-4.688 4.688a2 2 0 0 1-1.414.586H8.688a2 2 0 0 1-1.414-.586z",
        "m9 9 6 6");
    public static readonly VectorPath Clock = I("circle 12 12 10", "polyline 12,6 12,12 16,14");

    // --- giao diện
    public static readonly VectorPath Search = I("m21 21-4.34-4.34", "circle 11 11 8");
    public static readonly VectorPath Menu = I("M4 6h16", "M4 12h16", "M4 18h16");
    public static readonly VectorPath PanelLeft = I("rect 3 3 18 18 2", "M9 3v18");
    public static readonly VectorPath Calendar = I("M8 2v4", "M16 2v4", "rect 3 4 18 18 2", "M3 10h18");
    public static readonly VectorPath Filter = I("polygon 22,3 2,3 10,12.46 10,19 14,21 14,12.46");
    public static readonly VectorPath Settings = I(
        "M9.671 4.136a2.34 2.34 0 0 1 4.659 0 2.34 2.34 0 0 0 3.319 1.915 2.34 2.34 0 0 1 2.33 4.033 2.34 2.34 0 0 0 0 3.831 2.34 2.34 0 0 1-2.33 4.033 2.34 2.34 0 0 0-3.319 1.915 2.34 2.34 0 0 1-4.659 0 2.34 2.34 0 0 0-3.32-1.915 2.34 2.34 0 0 1-2.33-4.033 2.34 2.34 0 0 0 0-3.831A2.34 2.34 0 0 1 6.35 6.051a2.34 2.34 0 0 0 3.319-1.915",
        "circle 12 12 3");
    public static readonly VectorPath Home = I(
        "M15 21v-8a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1v8",
        "M3 10a2 2 0 0 1 .709-1.528l7-5.999a2 2 0 0 1 2.582 0l7 5.999A2 2 0 0 1 21 10v9a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z");
    public static readonly VectorPath Sun = I("circle 12 12 4", "M12 2v2", "M12 20v2", "m4.93 4.93 1.41 1.41", "m17.66 17.66 1.41 1.41",
        "M2 12h2", "M20 12h2", "m6.34 17.66-1.41 1.41", "m19.07 4.93-1.41 1.41");
    public static readonly VectorPath Moon = I("M12 3a6 6 0 0 0 9 9 9 9 0 1 1-9-9Z");
    public static readonly VectorPath LogOut = I("M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4", "polyline 16,17 21,12 16,7", "line 21 12 9 12");
    public static readonly VectorPath Lock = I("rect 3 11 18 11 2", "M7 11V7a5 5 0 0 1 10 0v4");
    public static readonly VectorPath Globe = I("circle 12 12 10", "M12 2a14.5 14.5 0 0 0 0 20 14.5 14.5 0 0 0 0-20", "M2 12h20");
    public static readonly VectorPath Refresh = I("M3 12a9 9 0 0 1 9-9 9.75 9.75 0 0 1 6.74 2.74L21 8", "M21 3v5h-5",
        "M21 12a9 9 0 0 1-9 9 9.75 9.75 0 0 1-6.74-2.74L3 16", "M8 16H3v5");
    public static readonly VectorPath RotateCcw = I("M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8", "M3 3v5h5");
}
