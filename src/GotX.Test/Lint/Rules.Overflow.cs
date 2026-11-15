using GotX.Text;

namespace GotX.Test;

/// <summary>Luật về nội dung không vừa chỗ: con tràn khỏi cha, chữ tràn khỏi hộp, phần tử bị cắt một phần.</summary>
public static partial class UiLint
{
    /// <summary>Phần <paramref name="inner"/> thò ra ngoài <paramref name="outer"/> nhiều nhất bao nhiêu px, và ở cạnh nào.</summary>
    static float Excess(Bounds inner, Bounds outer, out string side)
    {
        float l = outer.X - inner.X, t = outer.Y - inner.Y, r = inner.Right - outer.Right, b = inner.Bottom - outer.Bottom;
        float max = MathF.Max(MathF.Max(l, r), MathF.Max(t, b));
        side = max == r ? "phải" : max == b ? "dưới" : max == l ? "trái" : "trên";
        return max;
    }

    /// <summary>
    /// <c>overflow</c>: hộp của con thò ra ngoài hộp của cha trong khi cha không cắt (<c>overflow-hidden</c>) cũng không cuộn
    /// → nội dung tràn đè lên thứ bên cạnh. Chỉ tính phần CÒN NHÌN THẤY của con: phần tràn đã bị một tổ tiên xa hơn cắt
    /// hoặc cuộn (slide của carousel, hàng dài trong vùng cuộn ngang) không đè lên ai. Bỏ qua: con <c>absolute</c> (huy hiệu,
    /// nút đóng đặt ở góc là cố ý), phần tử đang transform, con có margin âm, và node chữ (đã có <c>text-overflow</c> lo).
    /// </summary>
    static void Overflow(LintContext ctx)
    {
        foreach (var n in ctx.Nodes)
        {
            if (n.Parent < 0 || n.Kind == "text" || !LintContext.InFlow(n)) continue;
            var p = ctx.Nodes[n.Parent];
            if (p.Scrollable || p.Overflow == GotX.Overflow.Hidden || p.Transformed) continue;
            if (n.Visible.IsEmpty) continue;
            float over = Excess(n.Visible, p.Screen, out string side);
            if (over <= ctx.Tol || ctx.NegativeMargin(n)) continue;
            ctx.Add("overflow", Severity.Warning, n,
                $"tràn khỏi cha {Fmt.N(over)}px về bên {side} (con {Fmt.B(n.Screen)}, cha {ctx.Tree.Label(p)} {Fmt.B(p.Screen)} không cắt/cuộn)", p);
        }
    }

    /// <summary>
    /// <c>text-overflow</c>: chữ vẽ ra rộng/cao hơn chỗ nó có mà không có ellipsis (<c>truncate</c>). Đo lại chữ bằng
    /// đúng kiểu chữ đã tính của node (<see cref="UiNode.TextStyle"/>): <c>whitespace-nowrap</c> → bề rộng cả dòng;
    /// chữ tự xuống dòng → số dòng × line-height. Phần vẽ ra được so với hộp của chính node chữ và padding box của cha.
    /// </summary>
    static void TextOverflow(LintContext ctx)
    {
        var lines = new List<TextLine>();
        foreach (var n in ctx.Nodes)
        {
            if (n.Kind != "text" || n.Text is not { Length: > 0 } raw || n.Ellipsis || n.Transformed || n.Parent < 0) continue;
            var p = ctx.Nodes[n.Parent];
            if (p.Scrollable) continue;
            var ts = n.TextStyle;
            string text = ctx.Text.ApplyCase(raw, ts.Case);
            var box = n.Layout;
            ctx.Text.Wrap(text, ts, ts.NoWrap ? float.NaN : box.Width + 0.01f, lines);
            float width = 0;
            foreach (var l in lines) width = MathF.Max(width, l.Width);
            float height = lines.Count * ts.LineHeight;
            // Phần chữ thật sự chiếm, neo ở góc trái-trên của hộp (căn giữa/phải chỉ làm lệch phía tràn, không đổi độ lớn).
            var ink = new Bounds(box.X, box.Y, MathF.Max(width, box.Width), MathF.Max(height, box.Height));
            var pad = new Bounds(p.Layout.X + p.Border.Left, p.Layout.Y + p.Border.Top,
                p.Layout.Width - p.Border.Horizontal, p.Layout.Height - p.Border.Vertical);
            float own = Excess(ink, box, out string ownSide), outside = Excess(ink, pad, out string outSide);
            if (own <= ctx.Tol && outside <= ctx.Tol) continue;
            ctx.TextOverflowed.Add(n.Index);
            bool cut = p.Overflow == GotX.Overflow.Hidden || n.Visible.Width < n.Screen.Width - ctx.Tol || n.Visible.Height < n.Screen.Height - ctx.Tol;
            string size = $"chữ cần {Fmt.N(width)}×{Fmt.N(height)}px, hộp chữ {Fmt.N(box.Width)}×{Fmt.N(box.Height)}, cha {Fmt.N(pad.Width)}×{Fmt.N(pad.Height)}";
            string effect = outside > ctx.Tol
                ? $"tràn {Fmt.N(outside)}px về bên {outSide} của cha {ctx.Tree.Label(p)}" + (cut ? " và bị cắt" : " (đè lên phần bên cạnh)")
                : $"tràn {Fmt.N(own)}px về bên {ownSide} của hộp chữ";
            ctx.Add("text-overflow", Severity.Warning, n, $"{effect}, không có truncate/ellipsis — {size}", p);
        }
    }

    /// <summary>
    /// <c>clipped</c>: chữ / icon / ảnh / ô nhập / vùng bấm bị <c>overflow-hidden</c> của một tổ tiên cắt mất MỘT PHẦN.
    /// Đi ngược lên tổ tiên: gặp vùng cuộn trước thì thôi (bị che trong vùng cuộn là bình thường); gặp tổ tiên
    /// overflow-hidden mà hộp của node thò ra ngoài nó thì đó là thủ phạm. Phần tử bị cắt HẾT không báo (nội dung
    /// accordion đang đóng, slide ngoài khung). Con của một phần tử đã báo thì không báo lại.
    /// </summary>
    static void Clipped(LintContext ctx)
    {
        var reported = new HashSet<int>();
        foreach (var n in ctx.Nodes)
        {
            if (n.Parent >= 0 && reported.Contains(n.Parent)) { reported.Add(n.Index); continue; }
            if (!(n.Kind is "text" or "path" or "image" or "input" || n.Clickable) || n.Transformed) continue;
            var s = n.Screen; var v = n.Visible;
            if (v.IsEmpty || (v.Width >= s.Width - ctx.Tol && v.Height >= s.Height - ctx.Tol)) continue;
            if (ctx.TextOverflowed.Contains(n.Index)) continue;
            UiNode? clipper = null;
            foreach (var a in ctx.Tree.Ancestors(n))
            {
                if (a.Scrollable) break;
                if (a.Overflow == GotX.Overflow.Hidden && Excess(s, a.Screen, out _) > ctx.Tol) { clipper = a; break; }
            }
            if (clipper is null) continue;
            reported.Add(n.Index);
            ctx.Add("clipped", Severity.Warning, n,
                $"bị cắt mất {Fmt.N(s.Width - v.Width)}×{Fmt.N(s.Height - v.Height)}px (còn thấy {Fmt.B(v)}) bởi overflow-hidden của {ctx.Tree.Label(clipper)} {Fmt.B(clipper.Screen)}", clipper);
        }
    }
}
