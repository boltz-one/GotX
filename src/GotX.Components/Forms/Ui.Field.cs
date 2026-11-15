namespace GotX.Components;

/// <summary>Hướng xếp nhãn và control trong <see cref="Ui.Field"/>.</summary>
public enum FieldOrientation : byte { Vertical, Horizontal }

/// <summary>
/// Các phần của field.tsx: khung bố cục cho nhãn + control + mô tả + lỗi. shadcn chọn kiểu theo <c>has-[...]</c>,
/// <c>data-[...]</c> và <c>@container</c> (GotX chưa có), nên ở đây người gọi chọn bằng tham số; bố cục
/// "responsive" (dọc trên màn hẹp, ngang trên màn rộng) bị bỏ vì variant màn hình không đổi layout.
/// </summary>
public static partial class Ui
{
    /// <summary>Nhóm các field có chung chú thích (fieldset).</summary>
    public static Div FieldSet(Window w) => w.Div().Class("flex flex-col gap-4");

    /// <summary>Tiêu đề của <see cref="FieldSet"/>; <paramref name="asLabel"/> = cỡ chữ nhãn thay vì cỡ legend.</summary>
    public static Div FieldLegend(Window w, string text, bool asLabel = false) =>
        w.Div().Class(asLabel ? "mb-1.5 text-sm font-medium" : "mb-1.5 text-base font-medium").Child(text);

    /// <summary>Danh sách field xếp dọc.</summary>
    public static Div FieldGroup(Window w) => w.Div().Class("flex w-full flex-col gap-5");

    /// <summary>Một field; <paramref name="invalid"/> nhuộm đỏ chữ bên trong như <c>data-[invalid=true]</c>.</summary>
    public static Div Field(Window w, FieldOrientation orientation = FieldOrientation.Vertical, bool invalid = false) =>
        w.Div().Class((orientation, invalid) switch
        {
            (FieldOrientation.Vertical, false) => "flex w-full flex-col gap-2",
            (FieldOrientation.Vertical, true) => "flex w-full flex-col gap-2 text-destructive",
            (_, false) => "flex w-full flex-row items-center gap-2",
            _ => "flex w-full flex-row items-center gap-2 text-destructive",
        });

    /// <summary>Cột chứa nhãn + mô tả khi đứng cạnh control (field ngang).</summary>
    public static Div FieldContent(Window w) => w.Div().Class("flex flex-1 flex-col gap-1 leading-snug");

    /// <summary>Nhãn của field.</summary>
    public static Div FieldLabel(Window w, string text) => w.Div().Class("flex gap-2 text-sm font-medium leading-snug").Child(text);

    /// <summary>Nhãn gắn với ô nhập của field như <c>htmlFor</c>: bấm nhãn thì ô nhận focus.</summary>
    public static Div FieldLabel(Window w, string text, TextEntry control) =>
        FieldLabel(w, text).OnClick(control, static (c, _) => c.Focus());

    /// <summary>
    /// Nhãn dạng thẻ chọn: khung viền bọc cả một field (radio/checkbox kèm mô tả); <paramref name="selected"/> tô nền
    /// như <c>has-data-checked</c>.
    /// </summary>
    public static Div FieldLabelCard(Window w, bool selected) => w.Div().Class(selected
        ? "flex w-full flex-col rounded-lg border border-primary/30 bg-primary/5 p-2.5 dark:border-primary/20 dark:bg-primary/10"
        : "flex w-full flex-col rounded-lg border border-border p-2.5 hover:bg-muted/50");

    /// <summary>Tiêu đề field không phải nhãn (vd. trong thẻ chọn).</summary>
    public static Div FieldTitle(Window w, string text) => w.Div().Class("flex items-center gap-2 text-sm font-medium").Child(text);

    /// <summary>Dòng mô tả dưới nhãn hoặc control.</summary>
    public static Div FieldDescription(Window w, string text) =>
        w.Div().Class("text-left text-sm leading-normal font-normal text-muted-foreground").Child(text);

    /// <summary>Thông báo lỗi của field.</summary>
    public static Div FieldError(Window w, string text) => w.Div().Class("text-sm font-normal text-destructive").Child(text);

    /// <summary>Nhiều lỗi: danh sách gạch đầu dòng (shadcn dùng <c>list-disc</c>, ở đây vẽ chấm bằng hộp tròn).</summary>
    public static Div FieldError(Window w, IReadOnlyList<string> errors)
    {
        if (errors.Count == 1) return FieldError(w, errors[0]);
        var list = w.Div().Class("flex flex-col gap-1 pl-1 text-sm font-normal text-destructive");
        for (int i = 0; i < errors.Count; i++)
            list.Child(w.Div().Class("flex items-start gap-2")
                .Child(w.Div().Class("mt-2 size-1 shrink-0 rounded-full bg-destructive"))
                .Child(w.Div().Child(errors[i])));
        return list;
    }

    /// <summary>Đường kẻ ngang giữa các field, có thể kèm chữ ở giữa (vd. "Hoặc").</summary>
    public static Div FieldSeparator(Window w, string? text = null)
    {
        // shadcn đè chữ lên đường kẻ bằng nền bg-background, lộ thành ô đen khi field nằm trong card ở dark mode;
        // ở đây vẽ hai nửa đường kẻ hai bên chữ nên hợp với mọi nền.
        var sep = w.Div().Class("flex h-5 items-center gap-2 text-sm").Child(w.Div().Class("h-px flex-1 bg-border"));
        if (text is null) return sep;
        return sep.Child(w.Div().Class("text-muted-foreground").Child(text)).Child(w.Div().Class("h-px flex-1 bg-border"));
    }
}
