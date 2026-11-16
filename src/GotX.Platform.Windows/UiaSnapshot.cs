namespace GotX.Platform.Windows;

/// <summary>Một phần tử trong ảnh chụp bất biến cho UIA (UIA gọi provider từ luồng khác nên không đọc thẳng SemanticsTree dùng lại bộ đệm).</summary>
internal sealed class UiaNode
{
    public long Id;
    public int Parent;
    public int[] Children = [];
    public SemanticRole Role;
    public SemanticState States;
    public string? Name, Description, Value, Key;
    public Bounds Bounds, Visible;
    public bool Focusable, HasRange;
    public float Min, Max, Now;

    public bool Has(SemanticState s) => (States & s) != 0;
    public bool Enabled => !Has(SemanticState.Disabled);
}

/// <summary>Bản sao bất biến của <see cref="SemanticsTree"/>; được thay nguyên cả khối (tham chiếu volatile) mỗi lần dựng lại.</summary>
internal sealed class UiaSnapshot
{
    public UiaNode[] Nodes = [];
    public int[] Roots = [];
    public readonly Dictionary<long, int> ById = new();
    public float Scale = 1;
    public long FocusedId;

    public UiaNode? Get(long id) => ById.TryGetValue(id, out int i) ? Nodes[i] : null;

    public static UiaSnapshot Copy(SemanticsTree t)
    {
        var s = new UiaSnapshot { Nodes = new UiaNode[t.Count], Roots = [.. t.Roots], Scale = t.Scale };
        for (int i = 0; i < t.Count; i++)
        {
            var n = t[i];
            s.Nodes[i] = new UiaNode
            {
                Id = n.Id, Parent = n.Parent, Children = [.. n.Children], Role = n.Role, States = n.States, Name = n.Name,
                Description = n.Description, Value = n.Value, Key = n.Key, Bounds = n.Bounds, Visible = n.Visible,
                Focusable = n.Focusable, HasRange = n.HasRange, Min = n.Min, Max = n.Max, Now = n.Now,
            };
            s.ById[n.Id] = i;
            if (n.Focused) s.FocusedId = n.Id;
        }
        return s;
    }
}
