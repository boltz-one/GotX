namespace GotX.Components;

public static partial class Ui
{
    /// <summary>
    /// Hàng các thẻ đính kèm, cuộn ngang khi tràn. Vùng cuộn cần khoá ổn định để giữ vị trí cuộn qua các frame,
    /// nên người gọi truyền <paramref name="key"/> duy nhất. Bản gốc có snap/scroll-fade – GotX chưa hỗ trợ.
    /// </summary>
    public static Div AttachmentGroup(Window w, string key) =>
        w.Div().Class("flex min-w-0 gap-3 overflow-x-auto py-1").Key(key);
}
