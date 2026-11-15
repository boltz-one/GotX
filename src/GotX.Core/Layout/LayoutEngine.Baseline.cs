using GotX.Text;

namespace GotX;

internal sealed partial class LayoutEngine
{
    /// <summary>Phép đo thăm dò (max-content…) không được ép một view đang phát lại từ cache phải Render lại: view chưa render thì bỏ qua thăm dò.</summary>
    bool CanProbe(int c) => _t[c].Kind != NodeKind.View || _t[c].ViewRendered;

    /// <summary>Đường cơ sở dòng chữ ĐẦU TIÊN của node, tính từ mép trên hộp border của nó (px). Hộp không có chữ → mép trên padding.</summary>
    float FirstBaseline(int c, in TextStyle parentText)
    {
        ref var n = ref _t[c];
        if (n.Kind == NodeKind.View && !n.ViewRendered) Measure(c, parentText, float.NaN, float.NaN); // dựng nội dung view để có cây con
        ref readonly var s = ref _t[c].Style;
        var ts = parentText.Refine(s, _rem);
        if (_t[c].Kind is NodeKind.Text or NodeKind.Input)
        {
            var font = _text.FontFor(ts);
            float k = ts.FontSize / font.UnitsPerEm, ascent = font.Ascender * k, descent = -font.Descender * k;
            return (ts.LineHeight - (ascent + descent)) / 2 + ascent;
        }
        var pad = s.Padding.Resolve(0, _rem);
        float top = pad.Top + s.BorderBox.Top;
        for (int ch = _t[c].FirstChild; ch >= 0; ch = _t[ch].NextSibling)
        {
            ref readonly var cs = ref _t[ch].Style;
            if (!InFlow(cs)) continue;
            return top + cs.Margin.Resolve(0, _rem).Top + FirstBaseline(ch, ts);
        }
        return top;
    }

    /// <summary>Độ lệch xuống (từ mép trên dòng) của phần tử align baseline để dòng chữ đầu của nó thẳng hàng với phần tử có phần trên đường cơ sở lớn nhất; 0 với phần tử khác.</summary>
    float BaselineShift(int c, bool row, bool flex, Align container, float maxAsc, float parentW, in TextStyle ts)
    {
        if (!row || maxAsc <= 0 || (flex ? _t[c].Style.AlignSelf ?? container : container) != Align.Baseline) return 0;
        float asc = _t[c].Style.Margin.Resolve(parentW, _rem).Top + FirstBaseline(c, ts);
        return maxAsc - asc;
    }
}
