using GotX;

/// <summary>
/// Icon Lucide mà <c>GotX.Components.Icon</c> chưa có, dùng trong thẻ chat (message-scroller-demo.tsx dùng lucide-react, các
/// thẻ khác dùng Hugeicons – xem <see cref="HugeIcons"/>). Path chép từ lucide-static. Tạo một lần, không dựng trong Render.
/// </summary>
static class HomeIcons
{
    static Div Draw(Window w, VectorPath path) => w.Path(path).Class("size-4 shrink-0");

    static readonly VectorPath RotateCwPath = VectorPath.Icon("M21 12a9 9 0 1 1-9-9c2.52 0 4.93 1 6.74 2.74L21 8", "M21 3v5h-5");
    static readonly VectorPath MessageCircleDashedPath = VectorPath.Icon("M10.1 2.182a10 10 0 0 1 3.8 0", "M13.9 21.818a10 10 0 0 1-3.8 0",
        "M17.609 3.72a10 10 0 0 1 2.69 2.7", "M2.182 13.9a10 10 0 0 1 0-3.8", "M20.28 17.61a10 10 0 0 1-2.7 2.69", "M21.818 10.1a10 10 0 0 1 0 3.8",
        "M3.721 6.391a10 10 0 0 1 2.7-2.69", "m6.163 21.117-2.906.85a1 1 0 0 1-1.236-1.169l.965-2.98");
    static readonly VectorPath TelescopePath = VectorPath.Icon(
        "m10.065 12.493-6.18 1.318a.934.934 0 0 1-1.108-.702l-.537-2.15a1.07 1.07 0 0 1 .691-1.265l13.504-4.44", "m13.56 11.747 4.332-.924",
        "m16 21-3.105-6.21", "M16.485 5.94a2 2 0 0 1 1.455-2.425l1.09-.272a1 1 0 0 1 1.212.727l1.515 6.06a1 1 0 0 1-.727 1.213l-1.09.272a2 2 0 0 1-2.425-1.455z",
        "m6.158 8.633 1.114 4.456", "m8 21 3.105-6.21", "circle 12 13 2");

    public static Div RotateCw(Window w) => Draw(w, RotateCwPath);
    public static Div MessageCircleDashed(Window w) => Draw(w, MessageCircleDashedPath);
    public static Div Telescope(Window w) => Draw(w, TelescopePath);
}