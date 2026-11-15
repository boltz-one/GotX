namespace GotX.Test;

/// <summary>
/// Nhật ký <see cref="TraceEvent"/> của một test. Khi có lỗi, vài chục sự kiện cuối cho biết "chuyện gì vừa xảy ra":
/// input nào, click trúng đâu, view nào bị đánh dấu bẩn, view nào render lại.
/// </summary>
public sealed class TraceLog
{
    const int Capacity = 20_000;
    readonly List<TraceEvent> _events = new();
    int _dropped;

    /// <summary>Tạm ngừng ghi – dùng khi harness tự vẽ frame soi cây (Inspect) để nhật ký chỉ chứa frame "thật".</summary>
    public bool Paused { get; set; }

    public IReadOnlyList<TraceEvent> Events => _events;

    internal void Add(TraceEvent e)
    {
        if (Paused) return;
        if (_events.Count >= Capacity) { _events.RemoveRange(0, Capacity / 2); _dropped += Capacity / 2; }
        _events.Add(e);
    }

    /// <summary>Mốc hiện tại; truyền lại cho <see cref="Since"/> / <see cref="Count"/> để chỉ xét các sự kiện sau mốc.</summary>
    public int Mark() => _dropped + _events.Count;

    public IEnumerable<TraceEvent> Since(int mark)
    {
        for (int i = Math.Max(0, mark - _dropped); i < _events.Count; i++) yield return _events[i];
    }

    public int Count(TraceKind kind, int mark = 0, string? contains = null)
    {
        int n = 0;
        foreach (var e in Since(mark))
            if (e.Kind == kind && (contains is null || e.Message.Contains(contains, StringComparison.Ordinal))) n++;
        return n;
    }

    /// <summary>Tên (Message) của các sự kiện loại <paramref name="kind"/> sau mốc, gộp trùng thành <c>Tên ×n</c>.</summary>
    public List<string> Names(TraceKind kind, int mark = 0)
    {
        var counts = new Dictionary<string, int>();
        var order = new List<string>();
        foreach (var e in Since(mark))
        {
            if (e.Kind != kind) continue;
            if (!counts.TryGetValue(e.Message, out int c)) order.Add(e.Message);
            counts[e.Message] = c + 1;
        }
        var result = new List<string>();
        foreach (string name in order) result.Add(counts[name] > 1 ? $"{name} ×{counts[name]}" : name);
        return result;
    }

    /// <summary>
    /// Các sự kiện sau mốc có chứa lần lượt các bước <paramref name="steps"/> (đúng thứ tự, cho phép xen sự kiện khác).
    /// Mỗi bước khớp theo loại và, nếu có, chuỗi con của Message. Trả về chỉ số bước đầu tiên không tìm thấy, hoặc -1 nếu đủ.
    /// </summary>
    public int MatchSequence(int mark, params (TraceKind Kind, string? Contains)[] steps)
    {
        int at = 0;
        foreach (var e in Since(mark))
        {
            if (at == steps.Length) break;
            var (kind, contains) = steps[at];
            if (e.Kind == kind && (contains is null || e.Message.Contains(contains, StringComparison.Ordinal))) at++;
        }
        return at == steps.Length ? -1 : at;
    }

    /// <summary>
    /// <paramref name="max"/> sự kiện cuối dạng văn bản. Frame không render view nào bị lược bỏ: chúng chiếm phần lớn
    /// nhật ký mà không nói lên điều gì.
    /// </summary>
    public List<string> Tail(int max = 30)
    {
        var lines = new List<string>();
        for (int i = _events.Count - 1; i >= 0 && lines.Count < max; i--)
        {
            var e = _events[i];
            if (e.Kind == TraceKind.Frame && e.Message.StartsWith("render 0,", StringComparison.Ordinal)) continue;
            lines.Add(e.ToString());
        }
        lines.Reverse();
        return lines;
    }

    public void Clear() { _events.Clear(); _dropped = 0; }
}
