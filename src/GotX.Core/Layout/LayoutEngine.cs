using System.Buffers;
using GotX.Text;

namespace GotX;

/// <summary>
/// Flexbox rút gọn: block (xếp dọc, kéo giãn ngang), flex row/column, gap, padding, margin,
/// border, width/height (px, rem, %), min/max, grow/shrink/basis, justify-content, align-items.
/// Chữ tự xuống dòng theo độ rộng khả dụng.
/// View được render "lười": chỉ gọi Render() khi không thể dùng lại kết quả của frame trước.
/// Có thêm: flex-wrap, grid, absolute, vùng cuộn. Chưa có: margin auto, align-content.
/// </summary>
internal sealed partial class LayoutEngine
{
    readonly ElementTree _t;
    readonly TextSystem _text;
    readonly Window _w;
    readonly List<TextLine> _lines = new();
    float _rem;

    public LayoutEngine(ElementTree tree, TextSystem text, Window window) { _t = tree; _text = text; _w = window; }

    public void Run(int root, Size viewport, in TextStyle baseText, float rem)
    {
        _rem = rem;
        Arrange(root, new Bounds(0, 0, viewport.Width, viewport.Height), baseText);
    }

    /// <summary>Đo một node gốc trước khi đặt chỗ (overlay neo theo anchor cần biết kích thước nội dung).</summary>
    public Size MeasureNode(int id, in TextStyle text, float availW, float availH, float rem)
    {
        _rem = rem;
        return Measure(id, text, availW, availH);
    }

    /// <summary>Min-content của một node gốc (khung một phần so với giá trị cũ của view).</summary>
    public float MinContentOf(int id, in TextStyle text, float rem) { _rem = rem; return MinContent(id, text); }

    /// <summary>Dựng layout cho các con của node (dùng khi view buộc phải render lúc paint).</summary>
    public void ArrangeChildren(int id) => ArrangeContent(id, _t[id].Bounds, _t[id].InheritedText);

    float? Spec(in Length l, float parent) => l.Resolve(parent, _rem);

    float Clamp(float v, in Length min, in Length max, float parent)
    {
        if (Spec(max, parent) is { } mx) v = MathF.Min(v, mx);
        if (Spec(min, parent) is { } mn) v = MathF.Max(v, mn);
        return MathF.Max(0, v);
    }

    static bool IsRow(in Style s) => s.Display == Display.Flex && s.FlexDirection == FlexDirection.Row;

    /// <summary>Phần tử tham gia luồng flex/block (không ẩn, không absolute).</summary>
    static bool InFlow(in Style s) => s.Display != Display.None && s.Position != Position.Absolute;

    /// <summary>Kích thước border-box mong muốn khi có không gian availW × availH (NaN = không giới hạn).</summary>
    Size Measure(int id, in TextStyle parentText, float availW, float availH)
    {
        ref var n = ref _t[id];
        PerfCounters.Measure++;
        if (n.Memo.TryGet(availW, availH, out var memo)) { PerfCounters.MeasureMemoHit++; return memo; }

        if (n.Kind == NodeKind.View && !n.ViewRendered)
        {
            if (_w.TryCachedMeasure(n.View!, availW, availH, parentText, out var cached))
            {
                n.Memo.Add(availW, availH, cached);
                return cached;
            }
            _w.RenderView(id);
        }

        ref readonly var s = ref n.Style;
        if (s.Display == Display.None) return default;
        var ts = parentText.Refine(s, _rem);
        var pad = s.Padding.Resolve(availW, _rem);
        float extraW = pad.Horizontal + s.BorderBox.Horizontal;
        float extraH = pad.Vertical + s.BorderBox.Vertical;

        float? w = Spec(s.Width, availW), h = Spec(s.Height, availH);
        // Độ rộng đã đặt (w-full, w-96…) vẫn bị min/max bó lại: đo nội dung theo bề rộng sau khi bó, không phải trước.
        if (w is { } fixedW) w = Clamp(fixedW, s.MinWidth, s.MaxWidth, availW);
        if (s.AspectRatio > 0) ApplyAspect(s, availW, availH, ref w, ref h);
        if (w is null || h is null)
        {
            // Độ rộng tự động nhưng có max-width: nội dung phải được đo trong đúng giới hạn đó, nếu không chữ được coi là
            // nằm trên một dòng dài rồi lúc đặt chỗ mới bị bó lại và xuống dòng – hộp sẽ thấp hơn nội dung thật.
            float limitW = availW;
            if (w is null && Spec(s.MaxWidth, availW) is { } maxW) limitW = float.IsNaN(availW) ? maxW : MathF.Min(availW, maxW);
            float innerW = (w ?? limitW) - extraW, innerH = (h ?? availH) - extraH;
            Size content = n.Kind switch
            {
                NodeKind.Text => MeasureText(n.Text!, ts, innerW),
                NodeKind.Input => MeasureInput(n.Input!, ts, innerW),
                _ => MeasureChildren(id, ts, innerW, innerH),
            };
            w ??= content.Width + extraW;
            h ??= content.Height + extraH;
        }

        var result = new Size(Clamp(w.Value, s.MinWidth, s.MaxWidth, availW),
                              Clamp(h.Value, s.MinHeight, s.MaxHeight, availH));
        n.Memo.Add(availW, availH, result);
        return result;
    }

    Size MeasureText(string text, in TextStyle ts, float maxWidth)
    {
        text = _text.ApplyCase(text, ts.Case);
        _text.WrapStyled(text, ts, ts.NoWrap ? float.NaN : maxWidth, _lines);
        int shown = ts.LineClamp > 0 ? Math.Min(_lines.Count, ts.LineClamp) : _lines.Count; // line-clamp: cao đúng N dòng
        float w = 0;
        for (int i = 0; i < shown; i++) w = MathF.Max(w, _lines[i].Width);
        return new Size(MathF.Ceiling(w), shown * ts.LineHeight);
    }

    Size MeasureInput(Widgets.TextInput input, in TextStyle ts, float maxWidth)
    {
        // ô rỗng: chiều cao theo placeholder (nó xuống dòng cùng bề rộng như chữ thật)
        string shown = input.Multiline && input.DisplayText.Length == 0 && input.Placeholder.Length > 0 ? input.Placeholder : input.DisplayText;
        _text.Wrap(shown, ts, input.Multiline ? maxWidth : float.NaN, _lines); // một dòng: không bao giờ xuống dòng
        float w = float.IsNaN(maxWidth) ? 160 : maxWidth;
        return new Size(w, Math.Max(1, _lines.Count) * ts.LineHeight);
    }

    Size MeasureChildren(int id, in TextStyle ts, float innerW, float innerH)
    {
        ref readonly var s = ref _t[id].Style;
        if (s.Display == Display.Grid) return GridLayout(id, ts, innerW, innerH, arrange: false, default);

        bool flex = s.Display == Display.Flex, row = IsRow(s);
        float gap = flex ? Spec(row ? s.ColumnGap : s.RowGap, row ? innerW : innerH) ?? 0 : 0;
        bool wrap = flex && row && s.FlexWrap && !float.IsNaN(innerW);
        float crossGap = wrap ? Spec(s.RowGap, innerH) ?? 0 : 0;
        if (wrap && Environment.GetEnvironmentVariable("GOTX_NO_WRAPROW") is null) return MeasureWrapRow(id, ts, innerW, innerH, gap, crossGap);
        float main = 0, cross = 0, lineMain = 0, lineCross = 0;
        int inLine = 0, lines = 0;
        float maxAsc = 0, maxDesc = 0; // align baseline: phần trên / dưới đường cơ sở lớn nhất
        for (int c = _t[id].FirstChild; c >= 0; c = _t[c].NextSibling)
        {
            ref readonly var cs = ref _t[c].Style;
            if (!InFlow(cs)) continue;
            var m = cs.Margin.Resolve(innerW, _rem);
            var sz = Measure(c, ts, innerW, row ? innerH : float.NaN);
            float oMain = row ? sz.Width + m.Horizontal : sz.Height + m.Vertical;
            float oCross = row ? sz.Height + m.Vertical : sz.Width + m.Horizontal;
            if (row && flex && (cs.AlignSelf ?? s.AlignItems) == Align.Baseline)
            {
                float asc = m.Top + FirstBaseline(c, ts);
                maxAsc = MathF.Max(maxAsc, asc); maxDesc = MathF.Max(maxDesc, oCross - asc);
            }
            if (wrap && inLine > 0 && lineMain + gap + oMain > innerW)
            {
                main = MathF.Max(main, lineMain); cross += lineCross + (lines > 0 ? crossGap : 0);
                lines++; lineMain = lineCross = 0; inLine = 0;
            }
            lineMain += (inLine > 0 ? gap : 0) + oMain;
            lineCross = MathF.Max(lineCross, oCross);
            inLine++;
        }
        lineCross = MathF.Max(lineCross, maxAsc + maxDesc);
        if (inLine > 0) { main = MathF.Max(main, lineMain); cross += lineCross + (lines > 0 ? crossGap : 0); }
        if (flex && row && !wrap && inLine > 1 && !float.IsNaN(innerW) && !s.ScrollX
            && RowCrossAfterFlex(id, ts, innerW, innerH, gap) is var flexed && !float.IsNaN(flexed)) cross = MathF.Max(flexed, maxAsc + maxDesc);
        return row ? new Size(main, cross) : new Size(cross, main);
    }

    /// <summary>Độ rộng cơ sở của một con trong hàng flex: flex-basis, rồi width, rồi độ rộng tự nhiên.</summary>
    float RowBasis(int c, in TextStyle ts, float innerW, float innerH)
    {
        ref readonly var cs = ref _t[c].Style;
        return Spec(cs.FlexBasis, innerW) ?? Spec(cs.Width, innerW) ?? MathF.Max(Measure(c, ts, innerW, innerH).Width, CanProbe(c) ? Measure(c, ts, float.NaN, innerH).Width : 0); // basis auto = max-content (khi chữ bị xuống dòng ở chỗ còn lại)
    }

    /// <summary>
    /// Chiều cao của một hàng flex sau khi chia chỗ theo grow/shrink. Lúc đo, mỗi con được hỏi với cả bề rộng của hàng;
    /// nhưng khi đặt chỗ nó có thể hẹp hơn (bị co, hoặc <c>flex-1</c> chỉ nhận phần còn lại) và chữ xuống thêm dòng.
    /// Tính lại theo đúng cách <see cref="ArrangeFlow"/> chia để hàng đủ cao. Trả về NaN nếu không con nào đổi bề rộng.
    /// </summary>
    float RowCrossAfterFlex(int id, in TextStyle ts, float innerW, float innerH, float gap)
    {
        int count = 0;
        for (int c = _t[id].FirstChild; c >= 0; c = _t[c].NextSibling) if (InFlow(_t[c].Style)) count++;
        var ids = ArrayPool<int>.Shared.Rent(count);
        var main = ArrayPool<float>.Shared.Rent(count);
        try
        {
            float used = 0;
            int k = 0;
            bool changed = false;
            for (int c = _t[id].FirstChild; c >= 0; c = _t[c].NextSibling)
            {
                ref readonly var cs = ref _t[c].Style;
                if (!InFlow(cs)) continue;
                ids[k] = c;
                main[k] = RowBasis(c, ts, innerW, innerH);
                used += main[k] + cs.Margin.Resolve(innerW, _rem).Horizontal + (k > 0 ? gap : 0);
                k++;
            }
            var before = ArrayPool<float>.Shared.Rent(count);
            try
            {
                main.AsSpan(0, count).CopyTo(before);
                ResolveFlexible(ids.AsSpan(0, count), main.AsSpan(0, count), innerW - used, true, innerW, ts);
                for (k = 0; k < count; k++) if (MathF.Abs(main[k] - before[k]) > 0.01f) changed = true;
                if (!changed) return float.NaN;
                float cross = 0;
                for (k = 0; k < count; k++)
                {
                    ref readonly var cs = ref _t[ids[k]].Style;
                    // Chỉ đo lại phần tử đổi bề rộng: đo ở một bề rộng mới buộc view con render lại (không có trong bộ nhớ đo).
                    float height = Spec(cs.Height, innerH)
                        ?? (MathF.Abs(main[k] - before[k]) > 0.01f ? Measure(ids[k], ts, main[k], innerH) : Measure(ids[k], ts, innerW, innerH)).Height;
                    cross = MathF.Max(cross, height + cs.Margin.Resolve(innerW, _rem).Vertical);
                }
                return cross;
            }
            finally { ArrayPool<float>.Shared.Return(before); }
        }
        finally { ArrayPool<int>.Shared.Return(ids); ArrayPool<float>.Shared.Return(main); }
    }

    void Arrange(int id, Bounds b, in TextStyle parentText)
    {
        ref var n = ref _t[id];
        n.Bounds = b;
        n.InheritedText = parentText;

        if (n.Kind == NodeKind.View && !n.ViewRendered)
        {
            n.Replay = false;
            if (_w.CanReplay(n.View!, b, parentText)) { n.Replay = true; return; }
            _w.RenderView(id);
        }
        if (n.Kind is NodeKind.Text or NodeKind.Input || n.FirstChild < 0) return;
        ArrangeContent(id, b, parentText);
    }

    void ArrangeContent(int id, Bounds b, in TextStyle parentText)
    {
        ref var n = ref _t[id];
        ref readonly var s = ref n.Style;
        var ts = parentText.Refine(s, _rem);
        var pad = s.Padding.Resolve(b.Width, _rem);
        var bw = s.BorderBox;
        var content = new Bounds(b.X + pad.Left + bw.Left, b.Y + pad.Top + bw.Top,
            MathF.Max(0, b.Width - pad.Horizontal - bw.Horizontal),
            MathF.Max(0, b.Height - pad.Vertical - bw.Vertical));

        bool hasAbsolute = false;
        for (int c = n.FirstChild; c >= 0; c = _t[c].NextSibling)
            if (_t[c].Style.Position == Position.Absolute && _t[c].Style.Display != Display.None) hasAbsolute = true;

        if (s.Display == Display.Grid) GridLayout(id, ts, content.Width, content.Height, arrange: true, content);
        else ArrangeFlow(id, ts, content);

        if (hasAbsolute) ArrangeAbsolute(id, b, ts);
        if (s.ScrollX || s.ScrollY) ComputeScrollExtent(id, b, pad, bw);
    }

    float MarginMain(int c, bool row, float parentW)
    {
        var m = _t[c].Style.Margin.Resolve(parentW, _rem);
        return row ? m.Horizontal : m.Vertical;
    }

    float MarginCross(int c, bool row, float parentW)
    {
        var m = _t[c].Style.Margin.Resolve(parentW, _rem);
        return row ? m.Vertical : m.Horizontal;
    }
}
