namespace GotX.Components;

/// <summary>Ô nhập nhiều dòng (textarea.tsx), cao tối thiểu 64px, tự cao thêm theo nội dung.</summary>
public sealed class Textarea : TextEntry
{
    const string Base = "w-full rounded-lg border bg-transparent px-2.5 py-2 text-sm cursor-text transition-colors dark:bg-input/30 ";

    protected override bool Multiline => true;

    // min-h-16 (64px) của shadcn trừ py-2 và viền: vùng chữ 46px.
    protected override float MinTextHeight => 46;

    protected override string BoxClass(bool invalid) => InGroup ? "w-full bg-transparent px-2.5 py-2 text-sm cursor-text" : invalid
        ? Base + "border-destructive ring-3 ring-destructive/20 dark:border-destructive/50 dark:ring-destructive/40"
        : Base + "border-input focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50";

    protected override string DisabledClass => InGroup ? "min-h-16 w-full px-2.5 py-2 text-sm opacity-50" :
        "min-h-16 w-full rounded-lg border border-input bg-input/50 px-2.5 py-2 text-sm opacity-50 dark:bg-input/80";
}
