using System.Buffers;
using GotX.Text;

namespace GotX;

internal sealed partial class LayoutEngine
{
    /// <summary>
    /// CSS Grid rút gọn: track fr / px / auto (grid-cols-3, grid-cols-[200px_1fr]), vị trí theo đường kẻ hoặc xếp tự động
    /// (LayoutEngine.GridPlace.cs), col-span / row-span, gap-x / gap-y, align-items theo chiều dọc (mặc định stretch).
    /// </summary>
    Size GridLayout(int id, in TextStyle ts, float innerW, float innerH, bool arrange, Bounds content)
    {
        ref var n = ref _t[id];
        ref readonly var s = ref n.Style;
        var cols = s.GridColumns ?? (s.GridFlowColumn ? GridTemplate.Empty : GridTemplate.Repeat(1));
        int nc = Math.Min(cols.Count, 64);
        float colGap = Spec(s.ColumnGap, innerW) ?? 0, rowGap = Spec(s.RowGap, innerH) ?? 0;
        int explicitRows = s.GridRows?.Count ?? 0;

        int count = 0, rowBudget = 1, maxLine = 0;
        for (int c = n.FirstChild; c >= 0; c = _t[c].NextSibling)
        {
            ref readonly var cs = ref _t[c].Style;
            if (!InFlow(cs)) continue;
            count++;
            rowBudget += cs.RowSpan == -1 ? Math.Max(1, explicitRows) : Math.Max(1, (int)cs.RowSpan);
            maxLine = Math.Max(maxLine, Math.Max(Math.Abs((int)cs.RowStart), Math.Abs((int)cs.RowEnd)));
        }
        if (count == 0) return default;
        rowBudget = Math.Max(rowBudget, explicitRows) + maxLine + 1;

        var ids = ArrayPool<int>.Shared.Rent(count);
        var place = ArrayPool<int>.Shared.Rent(count * 4); // row, col, colSpan, rowSpan
        var occ = ArrayPool<ulong>.Shared.Rent(rowBudget);
        var colSize = ArrayPool<float>.Shared.Rent(64); // grid-flow-col có thể sinh thêm cột ngầm
        var rowSize = ArrayPool<float>.Shared.Rent(rowBudget);
        var itemH = ArrayPool<float>.Shared.Rent(count);
        try
        {
            Array.Clear(occ, 0, rowBudget);
            Array.Clear(colSize, 0, 64);
            Array.Clear(rowSize, 0, rowBudget);

            int nRows = PlaceGridItems(id, s, ref nc, explicitRows, count, ids, place, occ, rowBudget);

            // ---- Độ rộng cột ----
            float frSum = 0, fixedSum = 0;
            for (int t = 0; t < nc; t++)
            {
                var tr = Track(cols, t);
                if (tr.Kind == TrackKind.Fixed) { colSize[t] = Spec(tr.Size, innerW) ?? 0; fixedSum += colSize[t]; }
                else if (tr.Kind == TrackKind.Fr && !float.IsNaN(innerW)) frSum += tr.Fr;
            }
            // track auto (và fr khi độ rộng chưa xác định) = phần tử rộng nhất chỉ chiếm 1 cột đó
            for (int k = 0; k < count; k++)
            {
                int t = place[k * 4 + 1];
                if (place[k * 4 + 2] != 1) continue;
                var tr = Track(cols, t);
                if (tr.Kind == TrackKind.Auto || (tr.Kind == TrackKind.Fr && float.IsNaN(innerW)))
                    colSize[t] = MathF.Max(colSize[t], Measure(ids[k], ts, float.NaN, float.NaN).Width + MarginMain(ids[k], true, innerW));
            }
            if (frSum > 0)
            {
                float autoSum = 0;
                for (int t = 0; t < nc; t++) if (Track(cols, t).Kind == TrackKind.Auto) autoSum += colSize[t];
                float unit = MathF.Max(0, innerW - colGap * (nc - 1) - fixedSum - autoSum) / frSum;
                for (int t = 0; t < nc; t++) if (Track(cols, t).Kind == TrackKind.Fr) colSize[t] = Track(cols, t).Fr * unit;
            }

            // ---- Chiều cao hàng ----
            var rows = s.GridRows;
            for (int t = 0; t < nRows; t++)
                if (rows is not null && t < rows.Count && rows.Tracks[t].Kind == TrackKind.Fixed)
                    rowSize[t] = Spec(rows.Tracks[t].Size, innerH) ?? 0;
            for (int k = 0; k < count; k++)
            {
                int c0 = place[k * 4 + 1], span = place[k * 4 + 2];
                float w = SpanSize(colSize, c0, span, colGap) - MarginMain(ids[k], true, innerW);
                itemH[k] = Measure(ids[k], ts, MathF.Max(0, w), float.NaN).Height + MarginCross(ids[k], true, innerW);
            }
            for (int pass = 0; pass < 2; pass++) // 1 hàng trước, nhiều hàng sau
                for (int k = 0; k < count; k++)
                {
                    int r0 = place[k * 4], rspan = place[k * 4 + 3];
                    if ((rspan == 1) != (pass == 0)) continue;
                    if (rspan == 1) { if (!IsFixedRow(rows, r0)) rowSize[r0] = MathF.Max(rowSize[r0], itemH[k]); continue; }
                    float have = SpanSize(rowSize, r0, rspan, rowGap);
                    int last = r0 + rspan - 1;
                    if (itemH[k] > have && !IsFixedRow(rows, last)) rowSize[last] += itemH[k] - have;
                }
            if (rows is not null && !float.IsNaN(innerH))
            {
                float frRows = 0, other = 0;
                for (int t = 0; t < nRows; t++)
                    if (t < rows.Count && rows.Tracks[t].Kind == TrackKind.Fr) frRows += rows.Tracks[t].Fr; else other += rowSize[t];
                if (frRows > 0)
                {
                    float unit = MathF.Max(0, innerH - rowGap * (nRows - 1) - other) / frRows;
                    for (int t = 0; t < Math.Min(nRows, rows.Count); t++)
                        if (rows.Tracks[t].Kind == TrackKind.Fr) rowSize[t] = rows.Tracks[t].Fr * unit;
                }
            }

            // ---- Đặt phần tử ----
            if (arrange)
            {
                for (int k = 0; k < count; k++)
                {
                    int c = ids[k];
                    ref readonly var cs = ref _t[c].Style;
                    int r0 = place[k * 4], c0 = place[k * 4 + 1], span = place[k * 4 + 2], rspan = place[k * 4 + 3];
                    float x = content.X + SpanSize(colSize, 0, c0, colGap) + (c0 > 0 ? colGap : 0);
                    float y = content.Y + SpanSize(rowSize, 0, r0, rowGap) + (r0 > 0 ? rowGap : 0);
                    float cw = SpanSize(colSize, c0, span, colGap), ch = SpanSize(rowSize, r0, rspan, rowGap);
                    var m = cs.Margin.Resolve(cw, _rem);
                    float w = Spec(cs.Width, cw) ?? cw - m.Horizontal;
                    // Phần tử có aspect-ratio: align "normal" của CSS là start, không kéo giãn (giữ đúng tỉ lệ)
                    var a = cs.AlignSelf ?? (cs.AspectRatio > 0 && cs.Height.IsAuto && s.AlignItems == Align.Stretch ? Align.Start : s.AlignItems);
                    float h = Spec(cs.Height, ch) ?? (a == Align.Stretch ? ch - m.Vertical : itemH[k] - m.Vertical);
                    float free = ch - h - m.Vertical;
                    float dy = a switch { Align.Center => free / 2, Align.End => free, _ => 0 };
                    Arrange(c, new Bounds(x + m.Left, y + m.Top + dy, MathF.Max(0, w), MathF.Max(0, h)), ts);
                }
            }

            return new Size(SpanSize(colSize, 0, nc, colGap), SpanSize(rowSize, 0, nRows, rowGap));
        }
        finally
        {
            ArrayPool<int>.Shared.Return(ids);
            ArrayPool<int>.Shared.Return(place);
            ArrayPool<ulong>.Shared.Return(occ);
            ArrayPool<float>.Shared.Return(colSize);
            ArrayPool<float>.Shared.Return(rowSize);
            ArrayPool<float>.Shared.Return(itemH);
        }
    }

    static readonly GridTrack AutoTrack = new(TrackKind.Auto, 0, default);
    static GridTrack Track(GridTemplate cols, int t) => t < cols.Count ? cols.Tracks[t] : AutoTrack;

    static bool IsFixedRow(GridTemplate? rows, int r) => rows is not null && r < rows.Count && rows.Tracks[r].Kind == TrackKind.Fixed;

    /// <summary>Tổng kích thước các track [start, start+count) cộng khoảng cách giữa chúng.</summary>
    static float SpanSize(float[] sizes, int start, int count, float gap)
    {
        float sum = 0;
        for (int k = start; k < start + count; k++) sum += sizes[k];
        return sum + gap * Math.Max(0, count - 1);
    }
}
