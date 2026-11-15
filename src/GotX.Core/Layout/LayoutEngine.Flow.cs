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
                if (!row && specCross is null && (cs.ShrinkWrap || (cs.InlineLevel && !flex)) && a == Align.Stretch) a = Align.Start; // w-fit (mọi nơi) / inline-* (chỉ trong luồng block: con flex được blockify) → shrink-to-fit
                float? basis = Spec(cs.FlexBasis, mainAvail) ?? specMain;

                // Cột: con kéo giãn được đo với đúng độ rộng nó sẽ nhận (chữ xuống dòng chính xác).
                // Cột: chiều cao cha chưa xác định với con → h-full của con quy về auto (như CSS).
                float measureW = !row && specCross is null && a == Align.Stretch ? crossAvail - mCross : content.Width;
                Size measured = default;
                if (basis is null || specCross is null)
                    measured = Measure(c, ts, measureW, row ? content.Height : float.NaN);

                // Cột, con KHÔNG kéo giãn theo trục ngang (items-start/center/end, self-*): CSS cho bề rộng fit-content = max(min-content, min(max-content, còn lại)).
                // Chữ xuống dòng thì box nhận cả bề rộng còn lại (không co lại theo dòng rộng nhất); chiều cao đo lại đúng ở bề rộng đó.
                float fitW = float.NaN;
                if (!row && specCross is null && a != Align.Stretch && !float.IsNaN(crossAvail))
                {
                    float availW = MathF.Max(0, crossAvail - mCross);
                    float maxContent = CanProbe(c) ? Measure(c, ts, float.NaN, float.NaN).Width : 0;
                    // Chỉ khi max-content vượt chỗ còn lại (chữ sẽ xuống dòng / hàng sẽ tràn): còn vừa thì giữ phép đo ở bề rộng còn lại như trước
                    // (đo max-content bằng NaN không biết phần tử w-full bên trong nên nhỏ hơn thực tế ở vài khối lồng nhau).
                    if (maxContent > availW)
                    {
                        fitW = availW; // (CSS: max(min-content, availW); MinContent của ta ước lượng cao hơn Chrome ở khối lồng nhau nên bám theo vùng chứa)
                        if (basis is null) measured = Measure(c, ts, fitW, float.NaN);
                    }
                }

                // Hàng flex: flex-basis auto = max-content (chữ KHÔNG xuống dòng), rồi co theo shrink – không phải bề rộng đã xuống dòng ở chỗ còn lại.
                // Chỉ nâng basis khi max-content LỚN HƠN phép đo ở chỗ còn lại (chữ đã bị xuống dòng); nhỏ hơn thì giữ như cũ (khối chứa flex-1 phình ra khi đo hữu hạn).
                float natural = row && flex && basis is null && !float.IsNaN(mainAvail) && CanProbe(c) ? Measure(c, ts, float.NaN, content.Height).Width : float.NaN;
                ids[i] = c;
                main[i] = basis ?? (row ? (float.IsNaN(natural) || natural <= measured.Width ? measured.Width : natural) : measured.Height);
                cross[i] = specCross is { } sc ? Clamp(sc, row ? cs.MinHeight : cs.MinWidth, row ? cs.MaxHeight : cs.MaxWidth, crossAvail)
                    : !float.IsNaN(fitW) ? Clamp(fitW, cs.MinWidth, cs.MaxWidth, crossAvail)
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
                // align-items / align-self: baseline (hàng ngang): dòng chữ đầu tiên của các phần tử baseline thẳng hàng; dòng cao thêm phần lệch.
                float maxAsc = 0;
                if (row)
                    for (int k = start; k < end; k++)
                        if ((flex ? _t[ids[k]].Style.AlignSelf ?? align : align) == Align.Baseline)
                            maxAsc = MathF.Max(maxAsc, _t[ids[k]].Style.Margin.Resolve(content.Width, _rem).Top + FirstBaseline(ids[k], ts));
                float lineCross = crossAvail;
                if (wrap || maxAsc > 0)
                {
                    if (wrap) lineCross = 0;
                    for (int k = start; k < end; k++)
                        lineCross = MathF.Max(lineCross, BaselineShift(ids[k], row, flex, align, maxAsc, content.Width, ts) + cross[k] + MarginCross(ids[k], row, content.Width));
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
                    if (!row && Spec(cs.Width, crossAvail) is null && (cs.ShrinkWrap || (cs.InlineLevel && !flex)) && a == Align.Stretch) a = Align.Start;
                    bool stretch = a == Align.Stretch && Spec(row ? cs.Height : cs.Width, crossAvail) is null;
                    float crossSize = MathF.Max(0, stretch
                        ? Clamp(lineCross - mCross, row ? cs.MinHeight : cs.MinWidth, row ? cs.MaxHeight : cs.MaxWidth, crossAvail)
                        : cross[k]);
                    float crossFree = lineCross - crossSize - mCross;
                    float crossOff = crossCursor + (row ? m.Top : m.Left) + a switch
                    {
                        Align.Center => crossFree / 2,
                        Align.End => crossFree,
                        Align.Baseline => BaselineShift(c, row, flex, align, maxAsc, content.Width, ts),
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
