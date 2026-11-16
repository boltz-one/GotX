namespace GotX;

/// <summary>Dữ liệu ngữ nghĩa của một node, nằm trong bảng phụ của <see cref="ElementTree"/> (Node chỉ giữ chỉ số).</summary>
internal struct SemanticData
{
    public SemanticRole Role;
    public SemanticState States;
    public string? Name, Description, Value;
    public int DescribedBy, LabelledBy; // số khoá .Key() của phần tử chứa chữ mô tả / nhãn
    public float Min, Max, Now;
    public bool HasRange;
}
