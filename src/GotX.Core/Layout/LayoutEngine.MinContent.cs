using GotX.Text;

namespace GotX;

/// <summary>
/// Bề rộng tối thiểu theo nội dung (min-content) và cách nó chặn việc co phần tử flex. Trong CSS, phần tử flex có
/// <c>min-width: auto</c>: không co nhỏ hơn từ dài nhất / nội dung không xuống dòng được của nó (trừ khi có <c>min-w-0</c>
/// hoặc overflow khác visible). Thiếu quy tắc này thì nhãn trong hàng <c>flex-1</c> tràn sang phần tử bên cạnh.
/// </summary>
internal sealed partial class LayoutEngine
{
    /// <summary>Min-content (border-box, px) của node, nhớ lại trong frame.</summary>
    float MinContent(int id, in TextStyle parentText)
    {
        ref var n = ref _t[id];
        if (n.HasMinContent) return n.MinContent;
        if (n.Kind == NodeKind.View && !n.ViewRendered)
        {
            if (_w.TryCachedMinContent(n.View!, parentText, out float cached)) { n.MinContent = cached; n.HasMinContent = true; return cached; }
            _w.RenderView(id);
        }
        ref readonly var s = ref n.Style;
        float result = 0;
        if (s.Display != Display.None)
        {
            var ts = parentText.Refine(s, _rem);
            var pad = s.Padding.Resolve(0, _rem);
            float extra = pad.Horizontal + s.BorderBox.Horizontal;
            if (Spec(s.Width, float.NaN) is { } fixedW) result = fixedW;
            else
            {
                float content = n.Kind switch
                {
                    NodeKind.Text => TextMinContent(n.Text!, ts),
                    NodeKind.Input => 0,
                    _ => ChildrenMinContent(id, s, ts),
                };
                result = content + extra;
            }
            result = Clamp(result, s.MinWidth, s.MaxWidth, float.NaN);
        }
        ref var m = ref _t[id];
        m.MinContent = result; m.HasMinContent = true;
        return result;
    }

    float TextMinContent(string text, in TextStyle ts)
    {
        text = _text.ApplyCase(text, ts.Case);
        if (ts.NoWrap) return _text.Measure(text, ts);
        float widest = 0;
        int start = 0;
        for (int i = 0; i <= text.Length; i++)
        {
            if (i < text.Length && text[i] != ' ' && text[i] != '\n') continue;
            if (i > start) widest = MathF.Max(widest, _text.MeasureRange(text, start, i, ts));
            start = i + 1;
        }
        return widest; // không làm tròn lên: trình duyệt dùng bề rộng lẻ, làm tròn sẽ đẩy hàng tràn 1px
    }

    float ChildrenMinContent(int id, in Style s, in TextStyle ts)
    {
        bool sum = s.Display == Display.Flex && s.FlexDirection == FlexDirection.Row && !s.FlexWrap;
        float gap = sum ? Spec(s.ColumnGap, float.NaN) ?? 0 : 0;
        float total = 0;
        int count = 0;
        for (int c = _t[id].FirstChild; c >= 0; c = _t[c].NextSibling)
        {
            ref readonly var cs = ref _t[c].Style;
            if (!InFlow(cs)) continue;
            var m = cs.Margin.Resolve(0, _rem);
            float w = MinContent(c, ts) + m.Horizontal;
            if (sum) total += w + (count > 0 ? gap : 0);
            else total = MathF.Max(total, w);
            count++;
        }
        return total;
    }

    /// <summary>
    /// Cỡ nhỏ nhất mà phần tử flex được co tới theo trục chính: min-width/min-height đặt rõ, nếu không thì (hàng ngang,
    /// overflow visible) min-content – nhưng không vượt bề rộng đã đặt – như <c>min-width: auto</c> của CSS.
    /// </summary>
    float FlexFloor(int c, in TextStyle ts, bool row, float mainAvail)
    {
        ref readonly var cs = ref _t[c].Style;
        var min = row ? cs.MinWidth : cs.MinHeight;
        if (!min.IsAuto) return Spec(min, mainAvail) ?? 0;
        if (!row || cs.Overflow != Overflow.Visible || cs.ScrollX || cs.ScrollY) return 0;
        float floor = MinContent(c, ts);
        if (Spec(cs.Width, mainAvail) is { } w) floor = MathF.Min(floor, w);
        return floor;
    }

    /// <summary>
    /// Chia khoảng trống (dương: grow, âm: shrink) cho các phần tử [start, end) của một dòng flex, rồi lặp: phần tử nào vượt
    /// giới hạn min/max thì đóng băng ở giới hạn và chia lại phần còn lại cho các phần tử khác (thuật toán của CSS Flexbox).
    /// <paramref name="main"/> vào là flex-basis, ra là kích thước cuối. <paramref name="free"/> là khoảng trống ban đầu.
    /// </summary>
    void ResolveFlexible(ReadOnlySpan<int> ids, Span<float> main, float free, bool row, float mainAvail, in TextStyle ts)
    {
        int n = ids.Length;
        Span<bool> frozen = n <= 64 ? stackalloc bool[n] : new bool[n];
        Span<float> basis = n <= 64 ? stackalloc float[n] : new float[n];
        main.CopyTo(basis);
        for (int round = 0; round < n + 1; round++)
        {
            float totalGrow = 0, totalShrink = 0, remaining = free;
            for (int k = 0; k < n; k++)
            {
                if (frozen[k]) { remaining -= main[k] - basis[k]; continue; }
                ref readonly var cs = ref _t[ids[k]].Style;
                totalGrow += cs.FlexGrowValue;
                totalShrink += cs.FlexShrinkValue * basis[k];
            }
            bool grow = remaining > 0;
            if (grow ? totalGrow <= 0 : remaining >= 0 || totalShrink <= 0)
            {
                for (int k = 0; k < n; k++) if (!frozen[k]) main[k] = ClampFlex(ids[k], basis[k], row, mainAvail, ts);
                return;
            }
            bool violated = false;
            for (int k = 0; k < n; k++)
            {
                if (frozen[k]) continue;
                ref readonly var cs = ref _t[ids[k]].Style;
                float target = grow ? basis[k] + remaining * cs.FlexGrowValue / totalGrow
                    : basis[k] + remaining * cs.FlexShrinkValue * basis[k] / totalShrink;
                float clamped = ClampFlex(ids[k], MathF.Max(0, target), row, mainAvail, ts);
                main[k] = clamped;
                if (MathF.Abs(clamped - MathF.Max(0, target)) > 0.01f) { frozen[k] = true; violated = true; }
            }
            if (!violated) return;
        }
    }

    float ClampFlex(int c, float size, bool row, float mainAvail, in TextStyle ts)
    {
        ref readonly var cs = ref _t[c].Style;
        var max = row ? cs.MaxWidth : cs.MaxHeight;
        if (Spec(max, mainAvail) is { } mx) size = MathF.Min(size, mx);
        return MathF.Max(size, FlexFloor(c, ts, row, mainAvail));
    }
}