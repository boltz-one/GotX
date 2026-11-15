namespace GotX.Components;

/// <summary>
/// Ô đánh dấu. Component có state là một View: tạo bằng <c>App.New(new Checkbox())</c>, nhúng bằng <c>w.View(box)</c>,
/// nghe thay đổi bằng <c>App.Subscribe&lt;Checkbox.CheckedChanged&gt;(box, e =&gt; ...)</c>.
/// </summary>
public sealed class Checkbox : View
{
    bool _checked;

    public readonly record struct CheckedChanged(bool Checked);

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            Notify();
            Emit(new CheckedChanged(value));
        }
    }

    public bool Disabled { get; set; }

    /// <summary>Nhãn bên phải ô; bấm vào nhãn cũng đổi trạng thái. null = chỉ có ô.</summary>
    public string? Label { get; set; }

    public void Toggle() { if (!Disabled) Checked = !Checked; }

    const string BoxOff = "flex size-4 shrink-0 items-center justify-center rounded-[4px] border border-input transition-colors dark:bg-input/30";
    const string BoxOn = "flex size-4 shrink-0 items-center justify-center rounded-[4px] border border-primary bg-primary text-primary-foreground transition-colors";
    const string FocusRing = " ring-3 ring-ring/50";

    // [đã chọn * 2 + đang focus bằng phím]. Cả hàng (ô + nhãn) là phần tử focus để bấm nhãn cũng focus như <label>
    // của HTML, nhưng vòng focus chỉ vẽ quanh ô như shadcn – nên chọn class theo trạng thái focus thay cho focus-visible:.
    static readonly string[] Boxes = [BoxOff, BoxOff + " border-ring" + FocusRing, BoxOn, BoxOn + FocusRing];

    string? _key;

    public override Div Render(Window w)
    {
        _key ??= "gotx-checkbox-" + Id.Value;
        bool ring = w.FocusVisible && w.IsFocused(_key);
        var box = w.Div().Class(Boxes[(_checked ? 2 : 0) + (ring ? 1 : 0)]);
        if (_checked) box.Child(Icon.Check(w).Class("size-3.5"));

        // Space/Enter đổi trạng thái: Core gọi OnClick của phần tử đang focus.
        var row = w.Div().Class("inline-flex items-center gap-2 cursor-pointer disabled:opacity-50")
            .Key(_key).Focusable()
            .Disabled(Disabled)
            .OnClick(this, static (c, _) => c.Toggle())
            .Child(box);
        if (Label is { } label) row.Child(w.Div().Class("text-sm font-medium").Child(label));
        return row;
    }
}
