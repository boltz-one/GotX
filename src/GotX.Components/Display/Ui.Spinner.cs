namespace GotX.Components;

public static partial class Ui
{
    /// <summary>
    /// Vòng quay chờ (spinner.tsx): cung Loader2 của Lucide quay bằng <c>animate-spin</c>, lấy màu chữ của cha.
    /// <paramref name="size"/> tính bằng px (mặc định 16 = size-4); nét dày theo tỉ lệ như SVG.
    /// </summary>
    public static Div Spinner(Window w, float size = 16)
    {
        var spinner = w.Path(IconPaths.LoaderCircle).Class("shrink-0 animate-spin");
        spinner.Style.Width = Length.Px(size);
        spinner.Style.Height = Length.Px(size);
        return spinner;
    }
}
