namespace GotX.Components;

public static partial class Ui
{
    /// <summary>
    /// Thanh tiến độ (progress.tsx), <paramref name="value"/> từ 0 tới 100. Độ dài phần đã xong đặt thẳng vào
    /// style (tỉ lệ) thay vì class <c>w-[x%]</c>, để mỗi giá trị không sinh thêm một chuỗi class phải biên dịch.
    /// </summary>
    public static Div Progress(Window w, float value)
    {
        float f = Math.Clamp(value, 0, 100) / 100f;
        var s = ComponentStyles.S;
        return w.Div().Class(s.ProgressTrack).Child(w.Div().Class(s.ProgressIndicator).W(Length.Fraction(f)));
    }

    /// <summary>Thanh tiến độ kèm nhãn bên trái và số phần trăm bên phải (ProgressLabel + ProgressValue).</summary>
    public static Div Progress(Window w, float value, string label, string valueText) =>
        w.Div().Class("flex w-full flex-col gap-3")
            .Child(w.Div().Class("flex items-center justify-between gap-3")
                .Child(w.Div().Class("text-sm font-medium").Child(label))
                .Child(w.Div().Class("text-sm text-muted-foreground").Child(valueText)))
            .Child(Progress(w, value));
}
