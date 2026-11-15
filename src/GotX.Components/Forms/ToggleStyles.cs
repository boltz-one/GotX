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

    // Bảng ghép nằm trong ComponentStyleSheet (ToggleTable) để đổi theo style.
    static int Index(int v, int s, int p, int pos) => ((v * 3 + s) * 2 + p) * 4 + pos;

    /// <summary>Chuỗi class cho một nút toggle.</summary>
    public static string Get(ComponentStyleSheet sheet, ToggleVariant variant, ToggleSize size, bool pressed, int position) =>
        sheet.ToggleTable[Index((int)variant, (int)size, pressed ? 1 : 0, position)];

    /// <summary>Vị trí của nút thứ <paramref name="i"/> trong nhóm <paramref name="count"/> nút dính nhau.</summary>
    public static int Position(int i, int count) => count == 1 ? Solo : i == 0 ? First : i == count - 1 ? Last : Middle;
}
