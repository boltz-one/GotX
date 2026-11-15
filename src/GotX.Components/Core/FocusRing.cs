namespace GotX.Components;

/// <summary>Vòng focus bàn phím của shadcn, dùng chung cho mọi component, và cách hỏi "focus bàn phím đang ở trong vùng này?".</summary>
internal static class FocusRing
{
    /// <summary>Vòng focus cho phần tử có viền (nút, ô chọn).</summary>
    public const string Ring = "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50";

    /// <summary>Vòng focus cho phần tử không có viền riêng (lớp bọc nút mở).</summary>
    public const string RingOnly = "focus-visible:ring-3 focus-visible:ring-ring/50";

    /// <summary>
    /// Focus bàn phím (focus-visible) đang nằm trong phần tử có <c>.Anchor(anchor)</c> – cách tooltip / hover card biết
    /// nút của mình vừa được Tab tới, vì Core không phát sự kiện focus. So theo hình học của frame gần nhất; gọi trong
    /// Render thì Core trả hình học của frame trước, vẫn đúng vì đổi focus không dời phần tử.
    /// </summary>
    public static bool Within(Window w, string anchor)
    {
        if (!w.FocusVisible || w.FocusedBounds is not { } f || w.AnchorBounds(anchor) is not { } a) return false;
        return a.Contains(new Point(f.X + f.Width / 2, f.Y + f.Height / 2));
    }
}
