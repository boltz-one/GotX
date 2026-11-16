namespace GotX;

/// <summary>
/// Gợi ý cuộn của Window cho renderer có giữ canvas (phần mềm): nội dung trong <see cref="Region"/> (px thiết bị) có thể đã dời <see cref="Dy"/> hàng so với
/// <see cref="Old"/> (scene của frame trước, còn nguyên tới lần Draw kế). Chỉ là đề xuất: SceneDamage tự kiểm chứng từng primitive, gợi ý sai chỉ mất công.
/// </summary>
public readonly record struct ScrollShift(Bounds Region, int Dy, Scene Old);

/// <summary>
/// Vùng đổi do <see cref="Window"/> tích luỹ cho từng renderer: khung một phần cộng hộp cũ + mới của các primitive thật sự đổi
/// trong đoạn bị thay (kể cả blur bóng, vùng cắt, biến đổi); khung đầy đủ đánh dấu Full. Mỗi renderer có <see cref="Consumer"/> riêng
/// (so với lần NÓ vẽ gần nhất); chưa ai gắn thì Window không tốn gì để ghi.
/// </summary>
public sealed class FrameDamage
{
    const int MaxRects = 128;
    readonly List<Consumer> _consumers = new();

    public sealed class Consumer
    {
        internal readonly List<Bounds> RectList = new();
        /// <summary>Phải vẽ lại cả frame (mới gắn, dựng đầy đủ, quá nhiều hộp).</summary>
        public bool Full { get; internal set; } = true;
        public ReadOnlySpan<Bounds> Rects => System.Runtime.InteropServices.CollectionsMarshal.AsSpan(RectList);
        /// <summary>Gợi ý cuộn kèm khung Full (chỉ renderer khai báo <see cref="AcceptsShift"/> mới nhận).</summary>
        public ScrollShift? Shift { get; internal set; }
        /// <summary>Renderer giữ canvas và tự dời được vùng cuộn (blit).</summary>
        public bool AcceptsShift { get; internal init; }
        /// <summary>Renderer đã vẽ xong: quên những gì đã tích luỹ.</summary>
        public void Consume() { Full = false; RectList.Clear(); Shift = null; }
    }

    public bool Active => _consumers.Count > 0;
    /// <summary>Mọi consumer đều đang Full (không có gì để ghi).</summary>
    internal bool AllFull { get { foreach (var c in _consumers) if (!c.Full) return false; return true; } }

    public Consumer Attach(bool acceptsShift = false) { var c = new Consumer { AcceptsShift = acceptsShift }; _consumers.Add(c); return c; }
    public void Detach(Consumer c) => _consumers.Remove(c);

    public void MarkFull() { foreach (var c in _consumers) { c.Full = true; c.RectList.Clear(); c.Shift = null; } }

    /// <summary>Khung đầy đủ kèm gợi ý cuộn: chỉ consumer đang "sạch" (đã vẽ hết frame trước, nhận gợi ý cuộn) được gắn gợi ý; còn lại như <see cref="MarkFull"/>.</summary>
    internal void MarkFull(Scene old, Bounds region, int dy)
    {
        foreach (var c in _consumers)
        {
            bool clean = c.AcceptsShift && !c.Full && c.RectList.Count == 0;
            c.Full = true; c.RectList.Clear();
            c.Shift = clean ? new ScrollShift(region, dy, old) : null;
        }
    }

    internal void Add(Bounds device)
    {
        if (device.IsEmpty) return;
        foreach (var c in _consumers)
        {
            if (c.Full) continue;
            if (c.RectList.Count >= MaxRects) { c.Full = true; c.RectList.Clear(); continue; }
            c.RectList.Add(device);
        }
    }
}
