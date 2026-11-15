using System.Buffers;
using GotX.Text;

namespace GotX;

internal sealed partial class LayoutEngine
{
    /// <summary>
    /// Kích thước của hàng flex-wrap khi ĐO: chia dòng theo flex-basis (flex-1 = basis 0 nên nhiều phần tử vẫn nằm một dòng), chia grow/shrink trong từng
    /// dòng rồi đo chiều cao mỗi phần tử ở đúng bề rộng cuối của nó – giống <see cref="ArrangeFlow"/>. Đo ở bề rộng tự nhiên thì phần tử flex-1 có chữ xuống
    /// dòng bị coi là rộng cả hàng, hàng bị cắt thành nhiều dòng và cao gấp đôi.
    /// </summary>
    Size MeasureWrapRow(int id, in TextStyle ts, float innerW, float innerH, float gap, float crossGap)
    {
        int count = 0;
        for (int c = _t[id].FirstChild; c >= 0; c = _t[c].NextSibling) if (InFlow(_t[c].Style)) count++;
        if (count == 0) return default;
        var ids = ArrayPool<int>.Shared.Rent(count);
        var main = ArrayPool<float>.Shared.Rent(count);
        try
        {
            int i = 0;
            for (int c = _t[id].FirstChild; c >= 0; c = _t[c].NextSibling)
            {
                ref readonly var cs = ref _t[c].Style;
                if (!InFlow(cs)) continue;
                ids[i] = c;
                main[i] = Spec(cs.FlexBasis, innerW) ?? Spec(cs.Width, innerW) ?? Measure(c, ts, innerW, innerH).Width;
                i++;
            }
            float totalCross = 0, widest = 0;
            int lines = 0;
            for (int start = 0; start < count;)
            {
                int end = start;
                float used = 0;
                for (; end < count; end++)
                {
                    float add = (end > start ? gap : 0) + main[end] + MarginMain(ids[end], true, innerW);
                    if (end > start && used + add > innerW) break;
                    used += add;
                }
                int len = end - start;
                ResolveFlexible(ids.AsSpan(start, len), main.AsSpan(start, len), innerW - used, true, innerW, ts);
                float lineMain = (len - 1) * gap, lineCross = 0, asc = 0, desc = 0;
                var itemsAlign = _t[id].Style.AlignItems;
                for (int k = start; k < end; k++)
                {
                    var cs = _t[ids[k]].Style;
                    lineMain += main[k] + MarginMain(ids[k], true, innerW);
                    float h = Spec(cs.Height, innerH) ?? Measure(ids[k], ts, main[k], innerH).Height;
                    float oc = h + MarginCross(ids[k], true, innerW);
                    if ((cs.AlignSelf ?? itemsAlign) == Align.Baseline)
                    {
                        float a = cs.Margin.Resolve(innerW, _rem).Top + FirstBaseline(ids[k], ts);
                        asc = MathF.Max(asc, a); desc = MathF.Max(desc, oc - a);
                    }
                    else lineCross = MathF.Max(lineCross, oc);
                }
                lineCross = MathF.Max(lineCross, asc + desc);
                widest = MathF.Max(widest, lineMain);
                totalCross += lineCross + (lines > 0 ? crossGap : 0);
                lines++;
                start = end;
            }
            return new Size(widest, totalCross);
        }
        finally { ArrayPool<int>.Shared.Return(ids); ArrayPool<float>.Shared.Return(main); }
    }
}
