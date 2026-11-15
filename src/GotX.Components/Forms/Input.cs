namespace GotX.Components;

/// <summary>Ô nhập một dòng kiểu shadcn (input.tsx). Enter phát <see cref="TextEntry.Submitted"/>.</summary>
public sealed class Input : TextEntry
{
    // Ô nhập đang focus luôn khớp focus-visible: (như trình duyệt: bấm chuột vào ô cũng hiện vòng focus).
    const string Base =
        "h-8 w-full min-w-0 rounded-lg border bg-transparent px-2.5 py-1 text-sm cursor-text transition-colors dark:bg-input/30 ";
    const string Normal = Base + "border-input focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50";
    const string Error = Base + "border-destructive ring-3 ring-destructive/20 dark:border-destructive/50 dark:ring-destructive/40";
    // Trong InputGroup: khung, viền và nền do nhóm vẽ, ô chỉ còn vùng chữ.
    const string Grouped = "h-8 w-full min-w-0 bg-transparent px-2.5 py-1 text-sm cursor-text";

    protected override bool Multiline => false;

    protected override string BoxClass(bool invalid) => InGroup ? Grouped : invalid ? Error : Normal;

    protected override string DisabledClass => InGroup
        ? "flex h-8 w-full min-w-0 items-center px-2.5 py-1 text-sm opacity-50"
        : "flex h-8 w-full min-w-0 items-center rounded-lg border border-input bg-input/50 px-2.5 py-1 text-sm opacity-50 dark:bg-input/80";
}
