namespace GotX.Components;

// Phần hai của bảng path Lucide: tệp, người, hành động, soạn thảo.
static partial class IconPaths
{
    // --- tệp, nội dung
    public static readonly VectorPath File = I("M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z", "M14 2v4a2 2 0 0 0 2 2h4");
    public static readonly VectorPath FileText = I("M15 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V7Z", "M14 2v4a2 2 0 0 0 2 2h4",
        "M10 9H8", "M16 13H8", "M16 17H8");
    public static readonly VectorPath Folder = I("M20 20a2 2 0 0 0 2-2V8a2 2 0 0 0-2-2h-7.9a2 2 0 0 1-1.69-.9L9.6 3.9A2 2 0 0 0 7.93 3H4a2 2 0 0 0-2 2v13a2 2 0 0 0 2 2Z");
    public static readonly VectorPath Image = I("rect 3 3 18 18 2", "circle 9 9 2", "m21 15-3.086-3.086a2 2 0 0 0-2.828 0L6 21");
    public static readonly VectorPath Copy = I("rect 8 8 14 14 2", "M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2");
    public static readonly VectorPath Paperclip = I("m21.44 11.05-9.19 9.19a6 6 0 0 1-8.49-8.49l8.57-8.57A4 4 0 1 1 18 8.84l-8.59 8.57a2 2 0 0 1-2.83-2.83l8.49-8.48");
    public static readonly VectorPath Upload = I("M12 3v12", "m17 8-5-5-5 5", "M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4");
    public static readonly VectorPath Download = I("M12 15V3", "M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4", "m7 10 5 5 5-5");
    public static readonly VectorPath Link = I("M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71",
        "M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71");
    public static readonly VectorPath Terminal = I("polyline 4,17 10,11 4,5", "line 12 19 20 19");
    public static readonly VectorPath Code = I("polyline 16,18 22,12 16,6", "polyline 8,6 2,12 8,18");

    // --- người, liên lạc
    public static readonly VectorPath User = I("M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2", "circle 12 7 4");
    public static readonly VectorPath Mail = I("rect 2 4 20 16 2", "m22 7-8.97 5.7a1.94 1.94 0 0 1-2.06 0L2 7");
    public static readonly VectorPath Bell = I("M10.268 21a2 2 0 0 0 3.464 0",
        "M3.262 15.326A1 1 0 0 0 4 17h16a1 1 0 0 0 .74-1.673C19.41 13.956 18 12.499 18 8A6 6 0 0 0 6 8c0 4.499-1.411 5.956-2.738 7.326");
    public static readonly VectorPath MessageSquare = I("M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z");
    public static readonly VectorPath Send = I(
        "M14.536 21.686a.5.5 0 0 0 .937-.024l6.5-19a.496.496 0 0 0-.635-.635l-19 6.5a.5.5 0 0 0-.024.937l7.93 3.18a2 2 0 0 1 1.112 1.11z",
        "m21.854 2.147-10.94 10.939");
    public static readonly VectorPath ThumbsUp = I("M7 10v12",
        "M15 5.88 14 10h5.83a2 2 0 0 1 1.92 2.56l-2.33 8A2 2 0 0 1 17.5 22H4a2 2 0 0 1-2-2v-8a2 2 0 0 1 2-2h2.76a2 2 0 0 0 1.79-1.11L12 2a3.13 3.13 0 0 1 3 3.88Z");
    public static readonly VectorPath ThumbsDown = I("M17 14V2",
        "M9 18.12 10 14H4.17a2 2 0 0 1-1.92-2.56l2.33-8A2 2 0 0 1 6.5 2H20a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2h-2.76a2 2 0 0 0-1.79 1.11L12 22a3.13 3.13 0 0 1-3-3.88Z");

    // --- hành động
    public static readonly VectorPath Star = I("M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z");
    public static readonly VectorPath Heart = I("M2 9.5a5.5 5.5 0 0 1 9.591-3.676.56.56 0 0 0 .818 0A5.49 5.49 0 0 1 22 9.5c0 2.29-1.5 4-3 5.5l-5.492 5.313a2 2 0 0 1-3 .019L5 15c-1.5-1.5-3-3.2-3-5.5");
    public static readonly VectorPath Eye = I("M2.062 12.348a1 1 0 0 1 0-.696 10.75 10.75 0 0 1 19.876 0 1 1 0 0 1 0 .696 10.75 10.75 0 0 1-19.876 0", "circle 12 12 3");
    public static readonly VectorPath EyeOff = I("M10.733 5.076a10.744 10.744 0 0 1 11.205 6.575 1 1 0 0 1 0 .696 10.747 10.747 0 0 1-1.444 2.49",
        "M14.084 14.158a3 3 0 0 1-4.242-4.242", "M17.479 17.499a10.75 10.75 0 0 1-15.417-5.151 1 1 0 0 1 0-.696 10.75 10.75 0 0 1 4.446-5.143", "m2 2 20 20");
    public static readonly VectorPath Trash = I("M3 6h18", "M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6", "M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2",
        "line 10 11 10 17", "line 14 11 14 17");
    public static readonly VectorPath Pencil = I(
        "M21.174 6.812a1 1 0 0 0-3.986-3.987L3.842 16.174a2 2 0 0 0-.5.83l-1.321 4.352a.5.5 0 0 0 .623.622l4.353-1.32a2 2 0 0 0 .83-.497z",
        "m15 5 4 4");

    // --- soạn thảo
    public static readonly VectorPath Bold = I("M6 12h9a4 4 0 0 1 0 8H7a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1h7a4 4 0 0 1 0 8");
    public static readonly VectorPath Italic = I("line 19 4 10 4", "line 14 20 5 20", "line 15 4 9 20");
    public static readonly VectorPath Underline = I("M6 4v6a6 6 0 0 0 12 0V4", "line 4 20 20 20");
    public static readonly VectorPath AlignLeft = I("line 21 6 3 6", "line 15 12 3 12", "line 17 18 3 18");
    public static readonly VectorPath AlignCenter = I("line 21 6 3 6", "line 17 12 7 12", "line 19 18 5 18");
    public static readonly VectorPath AlignRight = I("line 21 6 3 6", "line 21 12 9 12", "line 21 18 7 18");
}
