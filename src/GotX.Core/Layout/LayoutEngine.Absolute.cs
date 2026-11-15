using System.Buffers;
using GotX.Text;

namespace GotX;

internal sealed partial class LayoutEngine
{
    /// <summary>Kích thước nội dung của vùng cuộn (tính từ góc trên-trái border box, gồm padding cuối).</summary>
    void ComputeScrollExtent(int id, Bounds b, Edges pad, Edges bw)
    {
        ref var n = ref _t[id];
        float right = b.X, bottom = b.Y;
        for (int c = n.FirstChild; c >= 0; c = _t[c].NextSibling)
        {
            ref var cn = ref _t[c];
            if (cn.Style.Display == Display.None) continue;
            var m = cn.Style.Margin.Resolve(b.Width, _rem);
            right = MathF.Max(right, cn.Bounds.Right + m.Right);
            bottom = MathF.Max(bottom, cn.Bounds.Bottom + m.Bottom);
        }
        n.ContentSize = new Size(right - b.X + pad.Right + bw.Right, bottom - b.Y + pad.Bottom + bw.Bottom);
    }

    /// <summary>
    /// Đặt các con position:absolute theo top/right/bottom/left so với padding box của cha trực tiếp
    /// (CSS dùng tổ tiên "relative" gần nhất; ở đây luôn là cha – hãy đặt <c>relative</c> cho cha).
    /// </summary>
    void ArrangeAbsolute(int id, Bounds b, in TextStyle ts)
    {
        ref var n = ref _t[id];
        var bw = n.Style.BorderBox;
        var cb = new Bounds(b.X + bw.Left, b.Y + bw.Top, MathF.Max(0, b.Width - bw.Horizontal), MathF.Max(0, b.Height - bw.Vertical));
        for (int c = n.FirstChild; c >= 0; c = _t[c].NextSibling)
        {
            ref var cn = ref _t[c];
            ref readonly var cs = ref cn.Style;
            if (cs.Position != Position.Absolute || cs.Display == Display.None) continue;
            float? l = Spec(cs.Inset.Left, cb.Width), r = Spec(cs.Inset.Right, cb.Width);
            float? t = Spec(cs.Inset.Top, cb.Height), btm = Spec(cs.Inset.Bottom, cb.Height);
            float? w = Spec(cs.Width, cb.Width), h = Spec(cs.Height, cb.Height);
            if (w is null && l is not null && r is not null) w = cb.Width - l.Value - r.Value;
            if (h is null && t is not null && btm is not null) h = cb.Height - t.Value - btm.Value;
            if (w is null || h is null)
            {
                var m = Measure(c, ts, w ?? cb.Width, h ?? cb.Height);
                w ??= m.Width; h ??= m.Height;
            }
            float x = l is not null ? cb.X + l.Value : r is not null ? cb.Right - r.Value - w.Value : cb.X;
            float y = t is not null ? cb.Y + t.Value : btm is not null ? cb.Bottom - btm.Value - h.Value : cb.Y;
            Arrange(c, new Bounds(x, y, MathF.Max(0, w.Value), MathF.Max(0, h.Value)), ts);
        }
    }
}
