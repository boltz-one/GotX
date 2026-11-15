namespace GotX.Components;

/// <summary>Ô nhập nhiều dòng (textarea.tsx), cao tối thiểu 64px, tự cao thêm theo nội dung.</summary>
public sealed class Textarea : TextEntry
{
    protected override bool Multiline => true;

    /// <summary>Chiều cao tối thiểu của cả ô (như <c>min-h-[100px]</c>); 0 = min-h-16 (64px) của shadcn.</summary>
    public float MinBoxHeight { get; init; }

    // Chiều cao tối thiểu trừ py-2 và viền: min-h-16 → vùng chữ 46px.
    protected override float MinTextHeight => MinBoxHeight > 0 ? MinBoxHeight - 18 : 46;

    protected override string BoxClass(bool invalid) => Sheet.TextareaBox[InGroup ? 2 : invalid ? 1 : 0];

    protected override string DisabledClass => Sheet.TextareaDisabled[InGroup ? 1 : 0];
}