namespace GotX.Components;

/// <summary>Kiểu dòng đánh dấu trong luồng chat: chữ trơn, chữ giữa hai vạch ngang, hoặc chữ có gạch chân.</summary>
public enum MarkerVariant : byte { Default, Separator, Border }

public static partial class Ui
{
    /// <summary>
    /// Dòng mốc trong cuộc trò chuyện ("Hôm nay", "Đã chuyển sang model khác"…). Hai vạch của Separator là phần tử
    /// <c>flex-1</c> thật vì GotX không có <c>::before/::after</c>.
    /// </summary>
    public static Div Marker(Window w, string text, MarkerVariant variant = MarkerVariant.Default, Div? icon = null)
    {
        var marker = w.Div().Class(variant == MarkerVariant.Border
            ? "relative flex min-h-4 w-full items-center gap-2 border-b border-border pb-2 text-sm text-muted-foreground"
            : "relative flex min-h-4 w-full items-center gap-2 text-sm text-muted-foreground");
        if (variant == MarkerVariant.Separator) marker.Child(w.Div().Class("mr-1 h-px min-w-0 flex-1 bg-border"));
        if (icon is { } i) marker.Child(w.Div().Class("flex size-4 shrink-0 items-center justify-center").Child(i));
        marker.Child(w.Div().Class(variant == MarkerVariant.Separator ? "flex-none text-center" : "min-w-0").Child(text));
        if (variant == MarkerVariant.Separator) marker.Child(w.Div().Class("ml-1 h-px min-w-0 flex-1 bg-border"));
        return marker;
    }
}
