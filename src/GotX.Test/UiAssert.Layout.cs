namespace GotX.Test;

/// <summary>Assertion về căn hàng, chuỗi sự kiện và lint.</summary>
public sealed partial class UiAssert
{
    /// <summary>
    /// Toạ độ của một cạnh. <see cref="Edge.Baseline"/>: đường gốc của dòng chữ đầu tiên trong phần tử, tính theo
    /// đúng công thức của Core (dòng cao LineHeight, chữ căn giữa theo ascender + descender).
    /// </summary>
    public float? EdgeOf(UiNode node, Edge edge)
    {
        var s = node.Screen;
        switch (edge)
        {
            case Edge.Left: return s.X;
            case Edge.Right: return s.Right;
            case Edge.Top: return s.Y;
            case Edge.Bottom: return s.Bottom;
            case Edge.CenterX: return s.X + s.Width / 2;
            case Edge.CenterY: return s.Y + s.Height / 2;
        }
        var text = node.Kind == "text" ? node : null;
        if (text is null)
            foreach (var d in _t.Tree.Descendants(node))
                if (d.Kind == "text") { text = d; break; }
        if (text is null) return null;
        var ts = text.TextStyle;
        var font = TestFonts.Shared.FontFor(ts);
        float k = ts.FontSize / font.UnitsPerEm, ascent = font.Ascender * k, descent = -font.Descender * k;
        return text.Screen.Y + (ts.LineHeight - (ascent + descent)) / 2 + ascent;
    }

    /// <summary>Mọi phần tử trong <paramref name="keys"/> có cùng toạ độ cạnh <paramref name="edge"/> (sai khác tối đa <paramref name="tolerance"/> px).</summary>
    public bool Aligned(Edge edge, float tolerance, params string[] keys)
    {
        var nodes = new List<UiNode>();
        var values = new List<float>();
        foreach (string key in keys)
        {
            if (Node(key, "Aligned") is not { } node) return false;
            if (EdgeOf(node, edge) is not { } v) { _t.Fail($"Aligned: #{key} không có chữ nên không có {edge}", node); return false; }
            nodes.Add(node); values.Add(v);
        }
        if (nodes.Count < 2) return true;
        // Phần tử lệch nhất so với trung vị là thủ phạm đáng nghi nhất.
        var sorted = new List<float>(values);
        sorted.Sort();
        float median = sorted[sorted.Count / 2];
        int worst = 0;
        for (int i = 1; i < values.Count; i++)
            if (MathF.Abs(values[i] - median) > MathF.Abs(values[worst] - median)) worst = i;
        float spread = sorted[^1] - sorted[0];
        if (spread <= tolerance) return true;
        var parts = new List<string>();
        for (int i = 0; i < keys.Length; i++) parts.Add($"#{keys[i]}={Fmt.N(values[i])}");
        var others = new List<UiNode>(nodes);
        others.RemoveAt(worst);
        _t.Fail($"Aligned({edge}): lệch nhau {Fmt.N(spread)}px > {Fmt.N(tolerance)} — {string.Join(", ", parts)}; lệch nhất: #{keys[worst]}",
            nodes[worst], others.ToArray());
        return false;
    }

    /// <summary>
    /// Từ mốc <paramref name="mark"/> (<c>t.Trace.Mark()</c>) các sự kiện xảy ra đúng thứ tự <paramref name="steps"/>
    /// (cho phép xen sự kiện khác). Ví dụ: Click → ViewDirty "Counter" → ViewRender "Counter".
    /// </summary>
    public bool Sequence(int mark, params (TraceKind Kind, string? Contains)[] steps)
    {
        int missing = _t.Trace.MatchSequence(mark, steps);
        if (missing < 0) return true;
        var (kind, contains) = steps[missing];
        var f = _t.Fail($"Sequence: không thấy bước {missing + 1}/{steps.Length}: {kind}{(contains is null ? "" : $" \"{contains}\"")}");
        f.Details.Add("các sự kiện từ mốc:");
        int shown = 0;
        foreach (var e in _t.Trace.Since(mark))
        {
            if (++shown > 40) { f.Details.Add("  …"); break; }
            f.Details.Add("  " + e);
        }
        return false;
    }

    /// <summary>Chạy lint trên trạng thái hiện tại; mỗi vấn đề từ mức <paramref name="minimum"/> trở lên thành một lỗi của test.</summary>
    public bool NoLint(LintOptions? options = null, Severity minimum = Severity.Warning)
    {
        bool ok = true;
        foreach (var issue in _t.Lint(options))
        {
            if (issue.Severity < minimum) continue;
            ok = false;
            _t.FailIssue(issue);
        }
        return ok;
    }

    /// <summary>
    /// So các vấn đề lint với baseline của bộ test (nhóm <paramref name="scope"/>): chỉ vấn đề MỚI mới làm hỏng test.
    /// Với <c>--update-baseline</c> thì ghi đè nhóm này bằng danh sách hiện tại.
    /// </summary>
    public bool NoNewLint(string scope, IReadOnlyList<LintIssue> issues)
    {
        var baseline = _t.Options.Baseline ?? throw new InvalidOperationException("Bộ test chưa có baseline lint (TestOptions.Baseline).");
        if (_t.Options.UpdateBaseline)
        {
            baseline.Replace(scope, issues);
            _t.Notes.Add($"baseline [{scope}]: ghi {issues.Count} vấn đề");
            return true;
        }
        var (fresh, gone) = baseline.Compare(scope, issues);
        if (gone > 0) _t.Notes.Add($"baseline [{scope}]: {gone} vấn đề đã hết – chạy --update-baseline để dọn");
        foreach (var issue in fresh) _t.FailIssue(issue, "[mới so với baseline] ");
        return fresh.Count == 0;
    }
}
