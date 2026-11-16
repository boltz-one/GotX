using GotX.Widgets;

namespace GotX;

/// <summary>Một phần tử trong <see cref="SemanticsTree"/>. Đối tượng được tái dùng giữa các lần dựng: đừng giữ tham chiếu qua lần dựng kế.</summary>
public sealed class SemanticsNode
{
    public int Index, Parent;
    public readonly List<int> Children = new();
    /// <summary>Định danh ổn định giữa các lần dựng (theo .Key() hoặc đường đi trong cây ngữ nghĩa).</summary>
    public long Id;
    /// <summary>Thứ tự trong lượt duyệt vẽ – trùng <c>UiNode.Index</c> của <c>Window.Inspect</c> ở cùng trạng thái.</summary>
    public int PaintOrdinal;
    public SemanticRole Role;
    public SemanticState States;
    public string? Name, Description, Value, Key;
    /// <summary>Hộp trên màn hình (logical px, theo cửa sổ) và phần còn nhìn thấy sau khi cắt.</summary>
    public Bounds Bounds, Visible;
    public bool Focusable, Clickable, ExplicitRole;
    public bool HasRange;
    public float Min, Max, Now;
    public int Caret, SelectionStart, SelectionEnd;
    internal int DescribedBy, LabelledBy, TextStart, TextEnd, KeyId;
    internal TextInput? Input;

    public bool Has(SemanticState s) => (States & s) != 0;
    public bool Focused => Has(SemanticState.Focused);
    public bool Disabled => Has(SemanticState.Disabled);

    internal void Reset()
    {
        Children.Clear();
        Role = SemanticRole.None; States = SemanticState.None;
        Name = Description = Value = Key = null;
        Bounds = Visible = default;
        Focusable = Clickable = ExplicitRole = HasRange = false;
        Min = Max = Now = 0; Caret = SelectionStart = SelectionEnd = 0;
        DescribedBy = LabelledBy = TextStart = TextEnd = KeyId = 0;
        Input = null;
    }

    public override string ToString() => $"{Role} \"{Name}\"" + (States != SemanticState.None ? $" [{States}]" : "") + (Value is null ? "" : $" ={Value}");
}
