namespace GotX.Components;

public static partial class Ui
{
    const string KbdClass =
        "inline-flex h-5 min-w-5 items-center justify-center gap-1 rounded-sm bg-muted px-1 font-sans text-xs font-medium text-muted-foreground";

    /// <summary>
    /// Phím tắt. Như shadcn, dùng <c>font-sans</c> kể cả khi nằm trong khối chữ mono. Bỏ <c>pointer-events-none</c>/
    /// <c>select-none</c> (GotX chưa có, mà Kbd cũng không gắn handler).
    /// </summary>
    public static Div Kbd(Window w, string key) => w.Div().Class(KbdClass).Child(key);

    /// <summary>Phím rỗng để đặt icon (⏎, ⌘…): <c>Ui.Kbd(w).Child(Icon.CornerDownLeft(w).Class("size-3"))</c>.</summary>
    public static Div Kbd(Window w) => w.Div().Class(KbdClass);

    /// <summary>Nhóm phím (ví dụ Ctrl + K), con thêm bằng <c>.Child(Ui.Kbd(...))</c>.</summary>
    public static Div KbdGroup(Window w) => w.Div().Class("inline-flex items-center gap-1");
}
