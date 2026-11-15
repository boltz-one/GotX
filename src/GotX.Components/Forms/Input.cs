namespace GotX.Components;

/// <summary>Ô nhập một dòng kiểu shadcn (input.tsx). Enter phát <see cref="TextEntry.Submitted"/>.</summary>
public sealed class Input : TextEntry
{
    // Ô nhập đang focus luôn khớp focus-visible: (như trình duyệt: bấm chuột vào ô cũng hiện vòng focus).
    // Trong InputGroup: khung, viền và nền do nhóm vẽ, ô chỉ còn vùng chữ.
    protected override bool Multiline => false;

    protected override string BoxClass(bool invalid) => Sheet.InputBox[InGroup ? 2 : invalid ? 1 : 0];

    protected override string DisabledClass => Sheet.InputDisabled[InGroup ? 1 : 0];
}