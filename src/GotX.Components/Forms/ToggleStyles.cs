namespace GotX.Components;

/// <summary>Kiểu của <see cref="Toggle"/> / <see cref="ToggleGroup"/>.</summary>
public enum ToggleVariant : byte { Default, Outline }

/// <summary>Cỡ của <see cref="Toggle"/> / <see cref="ToggleGroup"/>.</summary>
public enum ToggleSize : byte { Default, Sm, Lg }

/// <summary>
/// Bảng class dùng chung cho Toggle và ToggleGroup (toggle.tsx + toggle-group.tsx). GotX chưa có <c>first:</c>/<c>last:</c>
/// nên vị trí trong nhóm (đứng riêng / đầu / giữa / cuối) là một chiều của bảng, chọn bằng chỉ số lúc render.
/// </summary>
static class ToggleStyles
{
    /// <summary>Vị trí của một nút: đứng riêng, đầu nhóm, giữa nhóm, cuối nhóm.</summary>
    public const int Solo = 0, First = 1, Middle = 2, Last = 3;

    const string Base =
        "inline-flex shrink-0 items-center justify-center gap-1 text-sm font-medium whitespace-nowrap cursor-pointer " +
        "transition-all hover:bg-muted hover:text-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 " +
        "disabled:opacity-50";

    static readonly string[] Variants = ["bg-transparent", "border-input bg-transparent"];
    static readonly string[] Sizes = ["h-8 min-w-8 px-2.5", "h-7 min-w-7 px-2.5 text-[0.8rem]", "h-9 min-w-9 px-2.5"];

    // Viền chỉ có ở Outline; trong nhóm các nút dính nhau nên bỏ viền trái trừ nút đầu (như border-l-0 first:border-l).
    static readonly string[] OutlineBorders = ["border", "border", "border-y border-r", "border-y border-r"];
    static readonly string[] Radii = ["rounded-lg", "rounded-l-lg", "", "rounded-r-lg"];
    static readonly string[] SmallRadii = ["rounded-md", "rounded-l-md", "", "rounded-r-md"];

    static readonly string[] Table = Build();

    static string[] Build()
    {
        var table = new string[2 * 3 * 2 * 4];
        for (int v = 0; v < 2; v++)
            for (int s = 0; s < 3; s++)
                for (int p = 0; p < 2; p++)
                    for (int pos = 0; pos < 4; pos++)
                    {
                        string border = v == 1 ? OutlineBorders[pos] : "";
                        string radius = s == 1 ? SmallRadii[pos] : Radii[pos];
                        string pressed = p == 1 ? "bg-muted" : "";
                        table[Index(v, s, p, pos)] = string.Join(' ',
                            new[] { Base, Variants[v], Sizes[s], border, radius, pressed }.Where(x => x.Length > 0));
                    }
        return table;
    }

    static int Index(int v, int s, int p, int pos) => ((v * 3 + s) * 2 + p) * 4 + pos;

    /// <summary>Chuỗi class cho một nút toggle.</summary>
    public static string Get(ToggleVariant variant, ToggleSize size, bool pressed, int position) =>
        Table[Index((int)variant, (int)size, pressed ? 1 : 0, position)];

    /// <summary>Vị trí của nút thứ <paramref name="i"/> trong nhóm <paramref name="count"/> nút dính nhau.</summary>
    public static int Position(int i, int count) => count == 1 ? Solo : i == 0 ? First : i == count - 1 ? Last : Middle;
}
