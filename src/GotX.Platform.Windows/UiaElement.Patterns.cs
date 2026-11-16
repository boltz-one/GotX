using static GotX.Platform.Windows.Win32;

namespace GotX.Platform.Windows;

internal sealed unsafe partial class UiaElement
{
    public int Invoke() => Act(SemanticAction.Invoke);

    /// <summary>Toggle / Expand / Collapse / Select đều là một lần bấm thật; chỉ bấm khi trạng thái cần đổi.</summary>
    public int Act(SemanticAction action, double number = 0, string? text = null, bool when = true)
    {
        var n = Node;
        if (n is null) return UIA_E_ELEMENTNOTAVAILABLE;
        if (!n.Enabled) return unchecked((int)0x80040200); // UIA_E_ELEMENTNOTENABLED
        if (when) _bridge.Perform(Id, action, number, text);
        return S_OK;
    }

    public int Expand() => Act(SemanticAction.Invoke, when: Node is { } n && !n.Has(SemanticState.Expanded));
    public int Collapse() => Act(SemanticAction.Invoke, when: Node is { } n && n.Has(SemanticState.Expanded));
    public int Select() => Act(SemanticAction.Invoke, when: Node is { } n && !UiaProps.IsSelectedNow(n));

    public int GetInt(int* result, int property)
    {
        *result = 0;
        var n = Node;
        if (n is null) return UIA_E_ELEMENTNOTAVAILABLE;
        *result = (int)UiaProps.Get(n, property).data;
        return S_OK;
    }

    public int GetBstr(nint* result)
    {
        *result = 0;
        var n = Node;
        if (n is null) return UIA_E_ELEMENTNOTAVAILABLE;
        *result = (nint)UiaProps.Get(n, UiaProps.ValueValue).data;
        return S_OK;
    }

    public int ReadOnly(int* result)
    {
        var n = Node;
        *result = n is null || n.Role != SemanticRole.Textbox || n.Has(SemanticState.ReadOnly) || !n.Enabled ? -1 : 0;
        return n is null ? UIA_E_ELEMENTNOTAVAILABLE : S_OK;
    }

    public int GetRange(double* result, int what)
    {
        *result = 0;
        var n = Node;
        if (n is null) return UIA_E_ELEMENTNOTAVAILABLE;
        float span = n.Max - n.Min;
        *result = what switch { 0 => n.Now, 1 => n.Max, 2 => n.Min, 3 => span / 10, _ => span / 100 };
        return S_OK;
    }

    public int SetRange(double value) =>
        Node is { Role: SemanticRole.Slider } ? Act(SemanticAction.SetRange, value) : unchecked((int)0x80131509); // InvalidOperationException-like
}
