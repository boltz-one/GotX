namespace GotX;

internal sealed partial class LayoutEngine
{
    /// <summary>
    /// aspect-*: biết chiều cao (h-8) thì suy ra bề rộng; ngược lại lấy bề rộng (đã đặt, hoặc bề rộng khả dụng như khối
    /// auto / con cột kéo giãn / ô grid) rồi suy ra chiều cao. Kết quả vẫn bị min/max của trục được suy ra bó lại.
    /// Hàng flex không đặt bề rộng sẽ nhận cả hàng (CSS phức tạp hơn: suy từ chiều cao dòng) – chưa mô phỏng.
    /// </summary>
    void ApplyAspect(in Style s, float availW, float availH, ref float? w, ref float? h)
    {
        float r = s.AspectRatio;
        if (w is null && h is { } hh) { w = Clamp(hh * r, s.MinWidth, s.MaxWidth, availW); return; }
        if (h is not null) return;
        if (w is null && !float.IsNaN(availW)) w = Clamp(availW, s.MinWidth, s.MaxWidth, availW);
        if (w is { } ww) h = Clamp(ww / r, s.MinHeight, s.MaxHeight, availH);
    }
}
