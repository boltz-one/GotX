using System.Text;

namespace GotX.Text;

public sealed partial class TextSystem
{
    /// <summary>
    /// Chia chữ thành các dòng vừa <paramref name="maxWidth"/> (NaN = không giới hạn).
    /// Ngắt tại khoảng trắng; từ dài hơn một dòng thì ngắt theo ký tự; '\n' là xuống dòng cứng.
    /// Mỗi dòng gồm cả khoảng trắng cuối, nhưng Width không tính phần đó (để căn lề đúng).
    /// </summary>
    public void Wrap(string text, in TextStyle style, float maxWidth, List<TextLine> lines)
    {
        lines.Clear();
        if (float.IsNaN(maxWidth)) maxWidth = float.PositiveInfinity;
        var font = FontFor(style);

        // Cùng chuỗi, font, cỡ chữ và độ rộng thì kết quả ngắt dòng không đổi: tra cache trước.
        // (Một node chữ được wrap ít nhất hai lần mỗi lần view render: lúc đo và lúc vẽ.)
        // Tầng 1: khoá theo THAM CHIẾU chuỗi (chuỗi tĩnh / giữ trong entity là cùng một đối tượng qua các frame) – không băm nội dung.
        // Tầng 2 (từ điển) băm cả chuỗi nên đắt với đoạn dài; chỉ dùng khi tầng 1 trượt (chuỗi mới dựng mỗi lần render).
        uint h = (uint)System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(text) * 2654435761u;
        h ^= (uint)font.Id * 40503u ^ BitConverter.SingleToUInt32Bits(style.FontSize) * 2246822519u ^ BitConverter.SingleToUInt32Bits(maxWidth) * 3266489917u
            ^ BitConverter.SingleToUInt32Bits(style.Tracking) * 668265263u ^ (style.TabularNums ? 0x9E3779B9u : 0);
        int slot = (int)(h >> 22); // 1024 ô
        ref var fast = ref _wrapFast[slot];
        if (ReferenceEquals(fast.Text, text) && fast.Font == font.Id && fast.FontSize == style.FontSize && fast.MaxWidth == maxWidth
            && fast.Tracking == style.Tracking && fast.Tnum == style.TabularNums && fast.Gen == _wrapGen)
        {
            for (int c = 0; c < fast.Count; c++) lines.Add(_wrapLines[fast.Start + c]);
            PerfCounters.WrapHit++;
            return;
        }
        // Tầng khoảng: cùng chuỗi/font/cỡ, bề rộng nằm trong khoảng cho cùng kết quả (kéo giãn cửa sổ gần như không ngắt dòng lại).
        uint sh = (uint)System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(text) * 2654435761u ^ (uint)font.Id * 40503u ^ BitConverter.SingleToUInt32Bits(style.FontSize) * 2246822519u
            ^ BitConverter.SingleToUInt32Bits(style.Tracking) * 668265263u ^ (style.TabularNums ? 0x9E3779B9u : 0);
        ref var span = ref _wrapSpan[(int)(sh >> 22)];
        if (ReferenceEquals(span.Text, text) && span.Font == font.Id && span.FontSize == style.FontSize && span.Tracking == style.Tracking
            && span.Tnum == style.TabularNums && span.Gen == _wrapGen && maxWidth >= span.Lo && (maxWidth < span.Hi || float.IsPositiveInfinity(span.Hi)))
        {
            for (int c = 0; c < span.Count; c++) lines.Add(_wrapLines[span.Start + c]);
            fast = new WrapFast(text, font.Id, style.FontSize, maxWidth, style.Tracking, style.TabularNums, span.Start, span.Count, _wrapGen);
            PerfCounters.WrapHit++;
            return;
        }
        var key = new WrapKey(text, font.Id, style.FontSize, maxWidth, style.Tracking, style.TabularNums);
        if (_wrapIndex.TryGetValue(key, out var hit))
        {
            for (int c = 0; c < hit.Count; c++) lines.Add(_wrapLines[hit.Start + c]);
            fast = new WrapFast(text, font.Id, style.FontSize, maxWidth, style.Tracking, style.TabularNums, hit.Start, hit.Count, _wrapGen);
            PerfCounters.WrapHit++;
            return;
        }
        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        // Cùng chuỗi dài vừa trượt ở bề rộng khác (kéo giãn): đi qua bảng từ thay vì quét + định hình lại.
        if (text.Length is >= WrapTableMinChars and <= WrapTableMaxChars && ReferenceEquals(span.Text, text) && span.Font == font.Id && span.FontSize == style.FontSize
            && span.Tracking == style.Tracking && span.Tnum == style.TabularNums)
            WrapTabled(text, style, maxWidth, lines);
        else WrapUncached(text, style, maxWidth, lines);
        PerfCounters.WrapMiss++;
        PerfCounters.WrapTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t0;
        if (MaxCachedWraps <= 0) return;
        if (_wrapIndex.Count >= MaxCachedWraps || _wrapChars > MaxCachedWrapChars)
        {
            _wrapIndex.Clear(); _wrapLines.Clear(); _wrapChars = 0; _wrapGen++;
        }
        _wrapIndex[key] = (_wrapLines.Count, lines.Count);
        span = new WrapSpan(text, font.Id, style.FontSize, style.Tracking, style.TabularNums, _wlo, _whi, _wrapLines.Count, lines.Count, _wrapGen);
        fast = new WrapFast(text, font.Id, style.FontSize, maxWidth, style.Tracking, style.TabularNums, _wrapLines.Count, lines.Count, _wrapGen);
        _wrapLines.AddRange(lines);
        _wrapChars += text.Length;
    }

    readonly record struct WrapSpan(string? Text, int Font, float FontSize, float Tracking, bool Tnum, float Lo, float Hi, int Start, int Count, int Gen);
    readonly WrapSpan[] _wrapSpan = new WrapSpan[1024];

    readonly record struct WrapKey(string Text, int Font, float FontSize, float MaxWidth, float Tracking, bool Tnum);

    readonly record struct WrapFast(string? Text, int Font, float FontSize, float MaxWidth, float Tracking, bool Tnum, int Start, int Count, int Gen);
    readonly WrapFast[] _wrapFast = new WrapFast[1024];
    int _wrapGen = 1; // đổi khi _wrapLines bị xoá: ô tầng 1 cũ trỏ vào chỉ số không còn hiệu lực

    // Kết quả wrap nằm liền nhau trong một danh sách chung, nên thêm mục mới không cấp phát mảng riêng.
    readonly Dictionary<WrapKey, (int Start, int Count)> _wrapIndex = new();
    readonly List<TextLine> _wrapLines = new();
    long _wrapChars;

    /// <summary>Số kết quả ngắt dòng được nhớ (0 = tắt cache). Chạm ngưỡng thì xoá hết.</summary>
    public int MaxCachedWraps { get; set; } = 4096;

    /// <summary>Số kết quả ngắt dòng đang được nhớ (chẩn đoán / báo cáo bộ nhớ).</summary>
    public int WrapCacheCount => _wrapIndex.Count;

    /// <summary>Tổng số ký tự của các chuỗi được cache giữ lại; vượt quá thì xoá hết (chặn văn bản dài bị giữ nhiều bản).</summary>
    public long MaxCachedWrapChars { get; set; } = 1 << 20;
}
