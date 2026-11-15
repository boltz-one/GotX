using System.Buffers;
using GotX.Text;

namespace GotX;

internal sealed partial class LayoutEngine
{
    /// <summary>Block và flexbox, có flex-wrap (chia dòng). Trong vùng cuộn, phần tử không bị co theo trục cuộn.</summary>
    void ArrangeFlow(int id, in TextStyle ts, Bounds content)
    {
        ref var n = ref _t[id];
        ref readonly var s = ref n.Style;
        bool flex = s.Display == Display.Flex, row = IsRow(s);
        var justify = flex ? s.Justify : Justify.Start;
        var align = flex ? s.AlignItems : Align.Stretch;
        float mainAvail = row ? content.Width : content.Height;
        float crossAvail = row ? content.Height : content.Width;
        bool wrap = flex && s.FlexWrap;
        float mainGap = flex ? Spec(row ? s.ColumnGap : s.RowGap, mainAvail) ?? 0 : 0;
        float crossGap = wrap ? Spec(row ? s.RowGap : s.ColumnGap, crossAvail) ?? 0 : 0;
        bool noShrink = row ? s.ScrollX : s.ScrollY;

        int count = 0;
        for (int c = n.FirstChild; c >= 0; c = _t[c].NextSibling)
            if (InFlow(_t[c].Style)) count++;
        if (count == 0) return;

        var ids = ArrayPool<int>.Shared.Rent(count);
        var main = ArrayPool<float>.Shared.Rent(count);
        var cross = ArrayPool<float>.Shared.Rent(count);
        try
        {
            // ---- Pass 1: flex-basis và kích thước cross giả định ----
            int i = 0;
            for (int c = n.FirstChild; c >= 0; c = _t[c].NextSibling)
            {
                ref readonly var cs = ref _t[c].Style;
                if (!InFlow(cs)) continue;
                var a = flex ? cs.AlignSelf ?? align : align;
                var m = cs.Margin.Resolve(content.Width, _rem);
                float mCross = row ? m.Vertical : m.Horizontal;
                float? specMain = Spec(row ? cs.Width : cs.Height, mainAvail);
                float? specCross = Spec(row ? cs.Height : cs.Width, crossAvail);
                float? basis = Spec(cs.FlexBasis, mainAvail) ?? specMain;

                // Cột: con kéo giãn được đo với đúng độ rộng nó sẽ nhận (chữ xuống dòng chính xác).
                // Cột: chiều cao cha chưa xác định với con → h-full của con quy về auto (như CSS).
                float measureW = !row && specCross is null && a == Align.Stretch ? crossAvail - mCross : content.Width;
                Size measured = default;
                if (basis is null || specCross is null)
                    measured = Measure(c, ts, measureW, row ? content.Height : float.NaN);

                ids[i] = c;
                main[i] = basis ?? (row ? measured.Width : measured.Height);
                cross[i] = specCross is { } sc ? Clamp(sc, row ? cs.MinHeight : cs.MinWidth, row ? cs.MaxHeight : cs.MaxWidth, crossAvail)
                    : row ? measured.Height : measured.Width;
                i++;
            }

            // ---- Chia dòng (flex-wrap) rồi xử lý từng dòng ----
            float crossCursor = 0;
            for (int start = 0; start < count;)
            {
                int end = start;
                float used = 0;
                for (; end < count; end++)
                {
                    float add = (end > start ? mainGap : 0) + main[end] + MarginMain(ids[end], row, content.Width);
                    if (wrap && end > start && used + add > mainAvail) break;
                    used += add;
                }

                // grow / shrink trong dòng
                float free = mainAvail - used;
                if (flex && free < 0 && noShrink) free = 0;
                else if (flex)
                {
                    // Chia grow/shrink theo thuật toán CSS (đóng băng phần tử chạm min/max, chia lại phần còn lại).
                    int len = end - start;
                    var before = ArrayPool<float>.Shared.Rent(len);
                    try
                    {
                        float sumBefore = 0, sumAfter = 0;
                        for (int k = 0; k < len; k++) { before[k] = main[start + k]; sumBefore += before[k]; }
                        ResolveFlexible(ids.AsSpan(start, len), main.AsSpan(start, len), free, row, mainAvail, ts);
                        for (int k = 0; k < len; k++)
                        {
                            sumAfter += main[start + k];
                            // Hàng: độ rộng đổi → đo lại chiều cao (chữ có thể xuống dòng khác)
                            if (row && main[start + k] != before[k] && Spec(_t[ids[start + k]].Style.Height, crossAvail) is null)
                                cross[start + k] = Measure(ids[start + k], ts, main[start + k], content.Height).Height;
                        }
                        free -= sumAfter - sumBefore; // phần còn dư (khi chạm max-width) cho justify-content
                    }
                    finally { ArrayPool<float>.Shared.Return(before); }
                }
                // kích thước cross của dòng: một dòng = cả vùng chứa; nhiều dòng = phần tử cao nhất
                float lineCross = crossAvail;
                if (wrap)
                {
                    lineCross = 0;
                    for (int k = start; k < end; k++)
                        lineCross = MathF.Max(lineCross, cross[k] + MarginCross(ids[k], row, content.Width));
                }

                // justify-content
                int nItems = end - start;
                float cursor = 0, between = mainGap;
                if (free > 0)
                {
                    switch (justify)
                    {
                        case Justify.Center: cursor = free / 2; break;
                        case Justify.End: cursor = free; break;
                        case Justify.SpaceBetween: if (nItems > 1) between += free / (nItems - 1); break;
                        case Justify.SpaceAround: cursor = free / nItems / 2; between += free / nItems; break;
                        case Justify.SpaceEvenly: cursor = free / (nItems + 1); between += free / (nItems + 1); break;
                    }
                }

                // đặt vị trí + align-items / align-self
                for (int k = start; k < end; k++)
                {
                    int c = ids[k];
                    ref readonly var cs = ref _t[c].Style;
                    var a = flex ? cs.AlignSelf ?? align : align;
                    var m = cs.Margin.Resolve(content.Width, _rem);
                    float mCross = row ? m.Vertical : m.Horizontal;
                    bool stretch = a == Align.Stretch && Spec(row ? cs.Height : cs.Width, crossAvail) is null;
                    float crossSize = MathF.Max(0, stretch
                        ? Clamp(lineCross - mCross, row ? cs.MinHeight : cs.MinWidth, row ? cs.MaxHeight : cs.MaxWidth, crossAvail)
                        : cross[k]);
                    float crossFree = lineCross - crossSize - mCross;
                    float crossOff = crossCursor + (row ? m.Top : m.Left) + a switch
                    {
                        Align.Center => crossFree / 2,
                        Align.End => crossFree,
                        _ => 0,
                    };
                    cursor += row ? m.Left : m.Top;
                    var cb = row
                        ? new Bounds(content.X + cursor, content.Y + crossOff, main[k], crossSize)
                        : new Bounds(content.X + crossOff, content.Y + cursor, crossSize, main[k]);
                    Arrange(c, cb, ts);
                    cursor += main[k] + (row ? m.Right : m.Bottom) + between;
                }

                crossCursor += lineCross + crossGap;
                start = end;
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(ids);
            ArrayPool<float>.Shared.Return(main);
            ArrayPool<float>.Shared.Return(cross);
        }
    }
}
