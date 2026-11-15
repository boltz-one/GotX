namespace GotX.Text;

public sealed partial class TextSystem
{
    // Bề rộng ngắt dòng đã tính cho text-balance / text-pretty, theo (chuỗi, font, cỡ, bề rộng, kiểu) → không tính lại mỗi frame
    readonly Dictionary<(WrapKey, TextWrapStyle), float> _styledWidth = new();
    readonly List<TextLine> _probe = new();

    /// <summary>
    /// Ngắt dòng theo text-wrap-style: Auto như <see cref="Wrap"/>; Balance / Pretty ngắt ở bề rộng hẹp hơn
    /// (<see cref="StyledWidth"/>) nhưng phần tử vẫn giữ bề rộng của nó – như trình duyệt.
    /// </summary>
    public void WrapStyled(string text, in TextStyle style, float maxWidth, List<TextLine> lines)
    {
        if (style.WrapStyle != TextWrapStyle.Auto && float.IsFinite(maxWidth)) maxWidth = StyledWidth(text, style, maxWidth);
        Wrap(text, style, maxWidth, lines);
    }

    /// <summary>
    /// text-balance: bề rộng nhỏ nhất vẫn giữ nguyên số dòng (tìm nhị phân – cách Chromium làm), chỉ khi 2–6 dòng
    /// (Chromium giới hạn 6) và không có xuống dòng cứng. text-pretty: nếu dòng cuối chỉ còn một từ thì thu hẹp vừa đủ để
    /// từ cuối của dòng kề trên rơi xuống cùng, miễn số dòng không đổi (xấp xỉ: Chromium chỉ dàn lại vài dòng cuối).
    /// </summary>
    public float StyledWidth(string text, in TextStyle style, float maxWidth)
    {
        var font = FontFor(style);
        var key = (new WrapKey(text, font.Id, style.FontSize, maxWidth, style.Tracking, style.TabularNums), style.WrapStyle);
        if (_styledWidth.TryGetValue(key, out float cached)) return cached;
        float w = style.WrapStyle == TextWrapStyle.Balance ? Balance(text, font, style, maxWidth) : Pretty(text, font, style, maxWidth);
        if (_styledWidth.Count >= MaxCachedWraps) _styledWidth.Clear();
        _styledWidth[key] = w;
        return w;
    }

    int ProbeLines(string text, Font font, in TextStyle style, float width)
    {
        _probe.Clear();
        WrapUncached(text, font, style.FontSize, width, style.LetterSpacing, style.TabularNums, _probe);
        return _probe.Count;
    }

    float Balance(string text, Font font, in TextStyle style, float maxWidth)
    {
        int n = ProbeLines(text, font, style, maxWidth);
        if (n <= 1 || n > 6 || text.Contains('\n')) return maxWidth;
        float lo = 0, hi = maxWidth;
        while (hi - lo > 0.25f)
        {
            float mid = (lo + hi) / 2;
            if (ProbeLines(text, font, style, mid) <= n) hi = mid; else lo = mid;
        }
        return hi;
    }

    float Pretty(string text, Font font, in TextStyle style, float maxWidth)
    {
        int n = ProbeLines(text, font, style, maxWidth);
        if (n < 2 || text.Contains('\n')) return maxWidth;
        var last = _probe[n - 1];
        if (text.AsSpan(last.Start, last.Length).Trim().Contains(' ')) return maxWidth; // dòng cuối đã có nhiều từ
        var pen = _probe[n - 2];
        int end = pen.End;
        while (end > pen.Start && text[end - 1] == ' ') end--;
        int wordStart = text.LastIndexOf(' ', end - 1, end - pen.Start);
        if (wordStart <= pen.Start) return maxWidth;
        int keep = wordStart;
        while (keep > pen.Start && text[keep - 1] == ' ') keep--;
        float narrower = MeasureRange(text, pen.Start, keep, style) + 0.5f;
        if (narrower >= maxWidth || ProbeLines(text, font, style, narrower) != n) return maxWidth;
        var moved = _probe[n - 1];
        return text.AsSpan(moved.Start, moved.Length).Trim().Contains(' ') ? narrower : maxWidth;
    }
}
