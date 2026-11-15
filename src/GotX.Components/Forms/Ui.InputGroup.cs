namespace GotX.Components;

/// <summary>Vị trí phần phụ trong <see cref="Ui.InputGroup(Window, bool)"/>.</summary>
public enum InputGroupAlign : byte { InlineStart, InlineEnd, BlockStart, BlockEnd }

/// <summary>
/// input-group.tsx: khung viền chung chứa ô nhập và các phần phụ (icon, chữ, nút). shadcn dùng <c>order-*</c>
/// và <c>has-[...]</c> để tự xếp; GotX chưa có, nên người gọi thêm con đúng thứ tự (đầu → ô → cuối) và chọn
/// <c>stacked</c> khi có phần phụ trên/dưới. Ô bên trong là <see cref="Input"/>/<see cref="Textarea"/> với
/// <c>InGroup = true</c> (không có khung riêng); truyền nó cho <see cref="InputGroup(Window, TextEntry, bool)"/> để
/// cả nhóm hiện vòng focus khi ô đang được gõ (<c>has-[[data-slot=input-group-control]:focus-visible]</c>).
/// </summary>
public static partial class Ui
{

    /// <summary>Khung nhóm; <paramref name="stacked"/> = xếp dọc (phần phụ BlockStart/BlockEnd hoặc textarea).</summary>
    public static Div InputGroup(Window w, bool stacked = false) => w.Div().Class(ComponentStyles.S.InputGroup[stacked ? 2 : 0]);

    /// <summary>
    /// Khung nhóm có vòng focus: viền và vòng sáng lên khi <paramref name="control"/> giữ focus. Ô nhập đổi focus thì
    /// view chứa nó được vẽ lại, nên chỉ cần hỏi trạng thái lúc dựng.
    /// </summary>
    public static Div InputGroup(Window w, TextEntry control, bool stacked = false) =>
        w.Div().Class(ComponentStyles.S.InputGroup[(stacked ? 2 : 0) + (control.IsFocused ? 1 : 0)]);

    /// <summary>Ô nhập của nhóm: giãn hết phần còn lại. <paramref name="input"/> nên có <c>InGroup = true</c>.</summary>
    public static Div InputGroupControl(Window w, TextEntry input) => w.Div().Class("flex-1 min-w-0").Child(w.View(input));

    /// <summary>Phần phụ (icon, chữ, nút) ở đầu/cuối dòng hoặc trên/dưới ô.</summary>
    public static Div InputGroupAddon(Window w, InputGroupAlign align = InputGroupAlign.InlineStart) =>
        w.Div().Class(ComponentStyles.S.InputGroupAddon[(int)align]);

    /// <summary>
    /// Phần phụ mà bấm vào thì focus <paramref name="control"/> (như onClick của InputGroupAddon gốc). Nút bên trong
    /// vẫn nhận click riêng vì phần tử con nằm trên.
    /// </summary>
    public static Div InputGroupAddon(Window w, TextEntry control, InputGroupAlign align = InputGroupAlign.InlineStart) =>
        InputGroupAddon(w, align).Class("cursor-text").OnClick(control, static (c, _) => c.Focus());

    /// <summary>Chữ trong phần phụ (đơn vị, tiền tố URL…).</summary>
    public static Div InputGroupText(Window w, string text) =>
        w.Div().Class(ComponentStyles.S.InputGroupText).Child(text);

    /// <summary>Nút nhỏ trong phần phụ (mặc định ghost, cao 24px); là điểm dừng Tab như mọi nút.</summary>
    public static Div InputGroupButton(Window w, string? label = null, ButtonVariant variant = ButtonVariant.Ghost) =>
        label is null
            ? Button(w, variant, ButtonSize.IconXs)
            : Button(w, variant, ButtonSize.Xs).Child(label);
}
