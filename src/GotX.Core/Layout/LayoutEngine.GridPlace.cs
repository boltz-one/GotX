namespace GotX;

internal sealed partial class LayoutEngine
{
    /// <summary>
    /// Xếp chỗ các con của grid theo thuật toán của CSS (rút gọn): (1) phần tử có cả hàng và cột xác định
    /// (col-start-2, row-[1_/_3]…), (2) phần tử chỉ có hàng xác định, (3) còn lại xếp tự động theo con trỏ –
    /// theo hàng (mặc định) hoặc theo cột (grid-flow-col, sinh thêm cột ngầm kiểu auto); grid-flow-dense quét lại từ đầu
    /// cho mỗi phần tử. place[k*4..] = hàng, cột, số cột, số hàng. Trả về số hàng; <paramref name="nc"/> có thể tăng.
    /// </summary>
    int PlaceGridItems(int id, in Style s, ref int nc, int explicitRows, int count, int[] ids, int[] place, ulong[] occ, int rowBudget)
    {
        int k = 0;
        for (int c = _t[id].FirstChild; c >= 0; c = _t[c].NextSibling)
        {
            ref readonly var cs = ref _t[c].Style;
            if (!InFlow(cs)) continue;
            ids[k] = c;
            ResolveLines(cs.ColStart, cs.ColEnd, cs.ColSpan, nc, out int c0, out int cspan);
            ResolveLines(cs.RowStart, cs.RowEnd, cs.RowSpan, explicitRows, out int r0, out int rspan);
            cspan = Math.Min(cspan, s.GridFlowColumn ? 64 : nc);
            if (c0 >= 0) c0 = Math.Min(c0, nc - Math.Min(cspan, nc)); // không sinh cột ngầm cho vị trí xác định
            if (r0 >= 0) r0 = Math.Min(r0, rowBudget - rspan - 1);
            place[k * 4] = r0; place[k * 4 + 1] = c0; place[k * 4 + 2] = cspan; place[k * 4 + 3] = rspan;
            k++;
        }

        int nRows = explicitRows;
        // (1) và (2): có hàng xác định
        for (int pass = 0; pass < 2; pass++)
            for (k = 0; k < count; k++)
            {
                int r0 = place[k * 4], c0 = place[k * 4 + 1], cspan = place[k * 4 + 2], rspan = place[k * 4 + 3];
                if (r0 < 0 || (c0 >= 0) != (pass == 0)) continue;
                if (c0 < 0)
                {
                    c0 = 0;
                    while (c0 + cspan < nc && !GridFree(occ, r0, c0, cspan, rspan)) c0++;
                }
                Occupy(occ, r0, c0, cspan, rspan);
                place[k * 4 + 1] = c0;
                nRows = Math.Max(nRows, r0 + rspan);
            }

        // (3) tự động theo con trỏ
        int rows = Math.Max(1, explicitRows);
        int cr = 0, cc = 0;
        for (k = 0; k < count; k++)
        {
            if (place[k * 4] >= 0) continue;
            int c0 = place[k * 4 + 1], cspan = place[k * 4 + 2], rspan = place[k * 4 + 3];
            if (s.GridFlowDense) cr = cc = 0;
            if (s.GridFlowColumn && c0 < 0)
            {
                // Theo cột: đi xuống trong số hàng tường minh rồi sang cột kế (cột ngầm tối đa 64)
                while (true)
                {
                    if (cr + rspan > rows && cr > 0) { cc++; cr = 0; }
                    if (cc + cspan > 64 || GridFree(occ, cr, cc, cspan, rspan)) break;
                    cr++;
                }
                c0 = Math.Min(cc, 64 - cspan);
            }
            else if (c0 >= 0)
            {
                if (c0 < cc) cr++;
                while (cr + rspan < rowBudget && !GridFree(occ, cr, c0, cspan, rspan)) cr++;
            }
            else
            {
                while (true)
                {
                    if (cc + cspan > nc) { cr++; cc = 0; }
                    if (cr + rspan >= rowBudget || GridFree(occ, cr, cc, cspan, rspan)) break;
                    cc++;
                }
                c0 = cc;
            }
            Occupy(occ, cr, c0, cspan, rspan);
            place[k * 4] = cr; place[k * 4 + 1] = c0;
            nRows = Math.Max(nRows, cr + rspan);
            nc = Math.Max(nc, c0 + cspan);
            if (s.GridFlowColumn) cr += rspan; else cc = c0 + cspan;
        }
        return nRows;
    }

    /// <summary>
    /// Đường kẻ start/end (1-based, âm = đếm từ cuối lưới tường minh) → vị trí bắt đầu (0-based, -1 = tự động) và số ô.
    /// Như CSS: có cả hai thì span = khoảng cách giữa hai đường; chỉ có end thì lùi lại span ô.
    /// </summary>
    static void ResolveLines(short start, short end, short spanValue, int tracks, out int pos, out int span)
    {
        span = spanValue == -1 ? Math.Max(1, tracks) : Math.Max(1, (int)spanValue);
        int Line(short l) => l > 0 ? l - 1 : tracks + 1 + l;
        if (start != 0 && end != 0)
        {
            int a = Line(start), b = Line(end);
            if (a == b) b = a + 1;
            pos = Math.Min(a, b); span = Math.Abs(b - a);
        }
        else if (start != 0) pos = Line(start);
        else if (end != 0) pos = Line(end) - span;
        else { pos = -1; return; }
        pos = Math.Max(0, pos);
    }

    static bool GridFree(ulong[] occ, int r, int c, int cspan, int rspan)
    {
        ulong mask = (cspan >= 64 ? ulong.MaxValue : (1UL << cspan) - 1) << c;
        for (int rr = r; rr < r + rspan; rr++)
            if ((occ[rr] & mask) != 0) return false;
        return true;
    }

    static void Occupy(ulong[] occ, int r, int c, int cspan, int rspan)
    {
        ulong mask = (cspan >= 64 ? ulong.MaxValue : (1UL << cspan) - 1) << c;
        for (int rr = r; rr < r + rspan; rr++) occ[rr] |= mask;
    }
}
