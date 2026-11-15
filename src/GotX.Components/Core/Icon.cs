namespace GotX.Components;

/// <summary>
/// Icon Lucide vẽ bằng <c>Window.Path</c>: ô vuông 16px (size-4), lấy màu chữ của phần tử cha, không co lại. Đổi cỡ
/// bằng <c>.Class("size-3.5")</c>, đổi màu bằng <c>.Class("text-…")</c>. Dữ liệu path nằm ở <see cref="IconPaths"/>.
/// </summary>
public static class Icon
{
    const string Box = "size-4 shrink-0";

    static Div Draw(Window w, VectorPath path) => w.Path(path).Class(Box);

    public static Div Check(Window w) => Draw(w, IconPaths.Check);
    public static Div X(Window w) => Draw(w, IconPaths.X);
    public static Div Plus(Window w) => Draw(w, IconPaths.Plus);
    public static Div Minus(Window w) => Draw(w, IconPaths.Minus);
    public static Div ChevronDown(Window w) => Draw(w, IconPaths.ChevronDown);
    public static Div ChevronUp(Window w) => Draw(w, IconPaths.ChevronUp);
    public static Div ChevronLeft(Window w) => Draw(w, IconPaths.ChevronLeft);
    public static Div ChevronRight(Window w) => Draw(w, IconPaths.ChevronRight);
    public static Div ChevronsUpDown(Window w) => Draw(w, IconPaths.ChevronsUpDown);
    public static Div ArrowLeft(Window w) => Draw(w, IconPaths.ArrowLeft);
    public static Div ArrowRight(Window w) => Draw(w, IconPaths.ArrowRight);
    public static Div ArrowUp(Window w) => Draw(w, IconPaths.ArrowUp);
    public static Div ArrowDown(Window w) => Draw(w, IconPaths.ArrowDown);
    public static Div CornerDownLeft(Window w) => Draw(w, IconPaths.CornerDownLeft);
    public static Div ExternalLink(Window w) => Draw(w, IconPaths.ExternalLink);

    /// <summary>Chấm tròn đặc 8px (trạng thái, mốc).</summary>
    public static Div Dot(Window w) => Draw(w, IconPaths.Dot);
    /// <summary>Vòng tròn rỗng.</summary>
    public static Div Circle(Window w) => Draw(w, IconPaths.Circle);
    public static Div MoreHorizontal(Window w) => Draw(w, IconPaths.MoreHorizontal);
    public static Div MoreVertical(Window w) => Draw(w, IconPaths.MoreVertical);
    public static Div GripVertical(Window w) => Draw(w, IconPaths.GripVertical);
    public static Div CircleCheck(Window w) => Draw(w, IconPaths.CircleCheck);
    public static Div CircleX(Window w) => Draw(w, IconPaths.CircleX);
    public static Div Info(Window w) => Draw(w, IconPaths.Info);
    public static Div AlertCircle(Window w) => Draw(w, IconPaths.AlertCircle);
    public static Div TriangleAlert(Window w) => Draw(w, IconPaths.TriangleAlert);
    public static Div OctagonX(Window w) => Draw(w, IconPaths.OctagonX);
    public static Div Clock(Window w) => Draw(w, IconPaths.Clock);

    /// <summary>Cung tròn Loader2 của Lucide, đứng yên (dùng khi tự đặt animation).</summary>
    public static Div Loader(Window w) => Draw(w, IconPaths.LoaderCircle);
    /// <summary>Vòng quay chờ: cung Loader2 với <c>animate-spin</c>.</summary>
    public static Div Spinner(Window w) => w.Path(IconPaths.LoaderCircle).Class("size-4 shrink-0 animate-spin");

    public static Div Search(Window w) => Draw(w, IconPaths.Search);
    public static Div Menu(Window w) => Draw(w, IconPaths.Menu);
    public static Div PanelLeft(Window w) => Draw(w, IconPaths.PanelLeft);
    public static Div Calendar(Window w) => Draw(w, IconPaths.Calendar);
    public static Div Filter(Window w) => Draw(w, IconPaths.Filter);
    public static Div Settings(Window w) => Draw(w, IconPaths.Settings);
    public static Div Home(Window w) => Draw(w, IconPaths.Home);
    public static Div Sun(Window w) => Draw(w, IconPaths.Sun);
    public static Div Moon(Window w) => Draw(w, IconPaths.Moon);
    public static Div LogOut(Window w) => Draw(w, IconPaths.LogOut);
    public static Div Lock(Window w) => Draw(w, IconPaths.Lock);
    public static Div Globe(Window w) => Draw(w, IconPaths.Globe);
    public static Div Refresh(Window w) => Draw(w, IconPaths.Refresh);
    public static Div RotateCcw(Window w) => Draw(w, IconPaths.RotateCcw);

    public static Div File(Window w) => Draw(w, IconPaths.File);
    public static Div FileText(Window w) => Draw(w, IconPaths.FileText);
    public static Div Folder(Window w) => Draw(w, IconPaths.Folder);
    public static Div Image(Window w) => Draw(w, IconPaths.Image);
    public static Div Copy(Window w) => Draw(w, IconPaths.Copy);
    public static Div Paperclip(Window w) => Draw(w, IconPaths.Paperclip);
    public static Div Upload(Window w) => Draw(w, IconPaths.Upload);
    public static Div Download(Window w) => Draw(w, IconPaths.Download);
    public static Div Link(Window w) => Draw(w, IconPaths.Link);
    public static Div Terminal(Window w) => Draw(w, IconPaths.Terminal);
    public static Div Code(Window w) => Draw(w, IconPaths.Code);

    public static Div User(Window w) => Draw(w, IconPaths.User);
    public static Div Mail(Window w) => Draw(w, IconPaths.Mail);
    public static Div Bell(Window w) => Draw(w, IconPaths.Bell);
    public static Div MessageSquare(Window w) => Draw(w, IconPaths.MessageSquare);
    public static Div Send(Window w) => Draw(w, IconPaths.Send);
    public static Div ThumbsUp(Window w) => Draw(w, IconPaths.ThumbsUp);
    public static Div ThumbsDown(Window w) => Draw(w, IconPaths.ThumbsDown);

    public static Div Star(Window w) => Draw(w, IconPaths.Star);
    public static Div Heart(Window w) => Draw(w, IconPaths.Heart);
    public static Div Eye(Window w) => Draw(w, IconPaths.Eye);
    public static Div EyeOff(Window w) => Draw(w, IconPaths.EyeOff);
    public static Div Trash(Window w) => Draw(w, IconPaths.Trash);
    public static Div Pencil(Window w) => Draw(w, IconPaths.Pencil);

    public static Div Bold(Window w) => Draw(w, IconPaths.Bold);
    public static Div Italic(Window w) => Draw(w, IconPaths.Italic);
    public static Div Underline(Window w) => Draw(w, IconPaths.Underline);
    public static Div AlignLeft(Window w) => Draw(w, IconPaths.AlignLeft);
    public static Div AlignCenter(Window w) => Draw(w, IconPaths.AlignCenter);
    public static Div AlignRight(Window w) => Draw(w, IconPaths.AlignRight);

    /// <summary>Mọi icon kèm tên, theo thứ tự khai báo – cho bảng icon của gallery và kiểm tra tự động.</summary>
    public static IReadOnlyList<(string Name, Func<Window, Div> Draw)> All { get; } =
    [
        (nameof(Check), Check), (nameof(X), X), (nameof(Plus), Plus), (nameof(Minus), Minus),
        (nameof(ChevronDown), ChevronDown), (nameof(ChevronUp), ChevronUp), (nameof(ChevronLeft), ChevronLeft), (nameof(ChevronRight), ChevronRight),
        (nameof(ChevronsUpDown), ChevronsUpDown), (nameof(ArrowLeft), ArrowLeft), (nameof(ArrowRight), ArrowRight), (nameof(ArrowUp), ArrowUp),
        (nameof(ArrowDown), ArrowDown), (nameof(CornerDownLeft), CornerDownLeft), (nameof(ExternalLink), ExternalLink), (nameof(Dot), Dot),
        (nameof(Circle), Circle), (nameof(MoreHorizontal), MoreHorizontal), (nameof(MoreVertical), MoreVertical), (nameof(GripVertical), GripVertical),
        (nameof(CircleCheck), CircleCheck), (nameof(CircleX), CircleX), (nameof(Info), Info), (nameof(AlertCircle), AlertCircle),
        (nameof(TriangleAlert), TriangleAlert), (nameof(OctagonX), OctagonX), (nameof(Clock), Clock), (nameof(Loader), Loader), (nameof(Search), Search),
        (nameof(Menu), Menu), (nameof(PanelLeft), PanelLeft), (nameof(Calendar), Calendar), (nameof(Filter), Filter),
        (nameof(Settings), Settings), (nameof(Home), Home), (nameof(Sun), Sun), (nameof(Moon), Moon),
        (nameof(LogOut), LogOut), (nameof(Lock), Lock), (nameof(Globe), Globe), (nameof(Refresh), Refresh),
        (nameof(RotateCcw), RotateCcw), (nameof(File), File), (nameof(FileText), FileText), (nameof(Folder), Folder),
        (nameof(Image), Image), (nameof(Copy), Copy), (nameof(Paperclip), Paperclip), (nameof(Upload), Upload),
        (nameof(Download), Download), (nameof(Link), Link), (nameof(Terminal), Terminal), (nameof(Code), Code),
        (nameof(User), User), (nameof(Mail), Mail), (nameof(Bell), Bell), (nameof(MessageSquare), MessageSquare),
        (nameof(Send), Send), (nameof(ThumbsUp), ThumbsUp), (nameof(ThumbsDown), ThumbsDown), (nameof(Star), Star),
        (nameof(Heart), Heart), (nameof(Eye), Eye), (nameof(EyeOff), EyeOff), (nameof(Trash), Trash),
        (nameof(Pencil), Pencil), (nameof(Bold), Bold), (nameof(Italic), Italic), (nameof(Underline), Underline),
        (nameof(AlignLeft), AlignLeft), (nameof(AlignCenter), AlignCenter), (nameof(AlignRight), AlignRight),
    ];
}
