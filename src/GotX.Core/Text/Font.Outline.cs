using System.Buffers;
using System.Buffers.Binary;

namespace GotX.Text;

public sealed partial class Font
{
    internal void Outline<TSink>(ushort g, TSink sink, in Affine m, int depth = 0) where TSink : IOutlineSink
    {
        if (depth > 8) return;
        var (s, e) = GlyphRange(g);
        if (e <= s) return;
        int o = _glyf + s;
        short contours = I16(o);
        if (contours >= 0) SimpleOutline(o, contours, sink, m);
        else CompositeOutline(o + 10, sink, m, depth);
    }

    void CompositeOutline<TSink>(int p, TSink sink, in Affine parent, int depth) where TSink : IOutlineSink
    {
        ushort flags;
        do
        {
            flags = U16(p);
            ushort glyph = U16(p + 2);
            p += 4;
            float dx, dy;
            if ((flags & 0x0001) != 0) { dx = I16(p); dy = I16(p + 2); p += 4; }
            else { dx = (sbyte)_d[p]; dy = (sbyte)_d[p + 1]; p += 2; }
            if ((flags & 0x0002) == 0) { dx = 0; dy = 0; } // khớp điểm (hiếm) – bỏ qua

            float a = 1, b = 0, c = 0, d = 1;
            if ((flags & 0x0008) != 0) { a = d = F2Dot14(p); p += 2; }
            else if ((flags & 0x0040) != 0) { a = F2Dot14(p); d = F2Dot14(p + 2); p += 4; }
            else if ((flags & 0x0080) != 0) { a = F2Dot14(p); b = F2Dot14(p + 2); c = F2Dot14(p + 4); d = F2Dot14(p + 6); p += 8; }

            var child = parent.Then(new Affine(a, b, c, d, dx, dy));
            Outline(glyph, sink, child, depth + 1);
        } while ((flags & 0x0020) != 0);
    }

    void SimpleOutline<TSink>(int o, int contours, TSink sink, in Affine m) where TSink : IOutlineSink
    {
        if (contours == 0) return;
        int p = o + 10;
        int endPtsO = p;
        int numPts = U16(endPtsO + 2 * (contours - 1)) + 1;
        p += 2 * contours;
        p += 2 + U16(p); // bỏ qua instructions

        var flags = ArrayPool<byte>.Shared.Rent(numPts);
        var xs = ArrayPool<float>.Shared.Rent(numPts);
        var ys = ArrayPool<float>.Shared.Rent(numPts);
        try
        {
            for (int i = 0; i < numPts;)
            {
                byte f = _d[p++];
                flags[i++] = f;
                if ((f & 0x08) != 0)
                {
                    int rep = _d[p++];
                    while (rep-- > 0 && i < numPts) flags[i++] = f;
                }
            }
            int v = 0;
            for (int i = 0; i < numPts; i++)
            {
                byte f = flags[i];
                if ((f & 0x02) != 0) { int dx = _d[p++]; v += (f & 0x10) != 0 ? dx : -dx; }
                else if ((f & 0x10) == 0) { v += I16(p); p += 2; }
                xs[i] = v;
            }
            v = 0;
            for (int i = 0; i < numPts; i++)
            {
                byte f = flags[i];
                if ((f & 0x04) != 0) { int dy = _d[p++]; v += (f & 0x20) != 0 ? dy : -dy; }
                else if ((f & 0x20) == 0) { v += I16(p); p += 2; }
                ys[i] = v;
            }
            for (int i = 0; i < numPts; i++) (xs[i], ys[i]) = m.Apply(xs[i], ys[i]);

            int start = 0;
            for (int c = 0; c < contours; c++)
            {
                int end = U16(endPtsO + 2 * c);
                EmitContour(flags, xs, ys, start, end, sink);
                start = end + 1;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(flags);
            ArrayPool<float>.Shared.Return(xs);
            ArrayPool<float>.Shared.Return(ys);
        }
    }

    /// <summary>Phát contour với các điểm on-curve ngầm giữa hai điểm off-curve liên tiếp.</summary>
    static void EmitContour<TSink>(byte[] fl, float[] xs, float[] ys, int s, int e, TSink sink) where TSink : IOutlineSink
    {
        if (e < s) return;
        bool On(int i) => (fl[i] & 1) != 0;

        float sx, sy;
        int first, last;
        if (On(s)) { sx = xs[s]; sy = ys[s]; first = s + 1; last = e; }
        else if (On(e)) { sx = xs[e]; sy = ys[e]; first = s; last = e - 1; }
        else { sx = (xs[s] + xs[e]) / 2; sy = (ys[s] + ys[e]) / 2; first = s; last = e; }

        sink.MoveTo(sx, sy);
        bool hasCtrl = false;
        float cx = 0, cy = 0;
        for (int i = first; i <= last; i++)
        {
            float x = xs[i], y = ys[i];
            if (On(i))
            {
                if (hasCtrl) sink.QuadTo(cx, cy, x, y); else sink.LineTo(x, y);
                hasCtrl = false;
            }
            else
            {
                if (hasCtrl) sink.QuadTo(cx, cy, (cx + x) / 2, (cy + y) / 2);
                cx = x; cy = y; hasCtrl = true;
            }
        }
        if (hasCtrl) sink.QuadTo(cx, cy, sx, sy); else sink.LineTo(sx, sy);
        sink.Close();
    }
}
