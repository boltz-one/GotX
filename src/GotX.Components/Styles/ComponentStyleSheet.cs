namespace GotX.Components;

/// <summary>
/// Bảng class của mọi component theo một style registry của shadcn (các file <c>ComponentStyleSheet.*.cs</c> giữ giá
/// trị mặc định, <c>RheaStyle.cs</c> giữ phần khác của style rhea). Style mới = một bảng mới:
/// <c>ComponentStyleSheet.Default with { ButtonBase = "…", … }</c> rồi <see cref="Compile"/> — không phải sửa component.
/// Các bảng ghép (variant × size…) dựng một lần trong <see cref="Compile"/>, lúc vẽ chỉ tra mảng.
/// </summary>
public sealed partial record ComponentStyleSheet
{
    /// <summary>Bảng mặc định của GotX.Components (khớp style của các block new-york-v4).</summary>
    public static readonly ComponentStyleSheet Default = new ComponentStyleSheet().Compile();

    /// <summary>Style "rhea" của shadcn (khối demo trang chủ ui.shadcn.com dùng <c>@/styles/base-rhea/ui/*</c>).</summary>
    public static readonly ComponentStyleSheet Rhea = RheaStyle.Create(Default).Compile();

    /// <summary>Style new-york-v4 (<c>apps/v4/registry/new-york-v4/ui/*</c>) – các block của ui.shadcn.com.</summary>
    public static readonly ComponentStyleSheet NewYorkV4 = NewYorkV4Style.Create(Default).Compile();

    /// <summary>Style gốc của bảng (bảng dẫn xuất bằng <c>with</c> giữ nguyên): file component tự giữ bảng riêng tra theo đây.</summary>
    public ComponentStyle Style { get; init; }

    /// <summary>
    /// Dựng các bảng ghép từ các thuộc tính hiện có. Gọi sau mỗi lần tạo bảng bằng <c>with</c>; trả về chính bảng để
    /// viết gọn <c>static readonly … = (Default with { … }).Compile()</c>.
    /// </summary>
    public ComponentStyleSheet Compile()
    {
        ButtonTable = Cross(ButtonBase, ButtonVariants, ButtonSizes);
        ItemTable = Cross(ItemBase, ItemVariants, ItemSizes);
        BadgeTable = Array.ConvertAll(BadgeVariants, v => BadgeBase + " " + v);
        BubbleContentTable = BuildBubbleContent();
        ToggleTable = BuildToggles();
        return this;
    }

    // Bảng [a * b.Length + c] = base + " " + a + " " + b: chuỗi giống hệt cách component vẫn ghép trước khi có style.
    static string[] Cross(string baseClass, string[] first, string[] second)
    {
        var table = new string[first.Length * second.Length];
        for (int i = 0; i < first.Length; i++)
            for (int j = 0; j < second.Length; j++)
                table[i * second.Length + j] = $"{baseClass} {first[i]} {second[j]}";
        return table;
    }

    string[] BuildBubbleContent()
    {
        var table = new string[BubbleContentVariants.Length * 2];
        for (int v = 0; v < BubbleContentVariants.Length; v++)
        {
            table[v * 2] = $"{BubbleContentBase} {BubbleContentVariants[v]} self-start";
            table[v * 2 + 1] = $"{BubbleContentBase} {BubbleContentVariants[v]} self-end";
        }
        return table;
    }

    internal string[] ButtonTable { get; private set; } = [];
    internal string[] ItemTable { get; private set; } = [];
    internal string[] BadgeTable { get; private set; } = [];
    internal string[] BubbleContentTable { get; private set; } = [];
}
