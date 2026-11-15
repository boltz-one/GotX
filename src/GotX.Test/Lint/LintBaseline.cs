using System.Text;

namespace GotX.Test;

/// <summary>
/// Danh sách các vấn đề lint ĐÃ BIẾT, lưu thành file văn bản người đọc được. Test so kết quả lint với file này và chỉ
/// hỏng khi có vấn đề mới – nhờ vậy bật lint lên một giao diện đang có sẵn lỗi mà không phải sửa hết ngay.
/// Mỗi dòng: <c>mức | luật | ×số lượng | đường dẫn phần tử  ## ghi chú</c>, gom theo nhóm <c>[tên]</c>.
/// Khoá so khớp là (luật, đường dẫn); phần sau <c>##</c> chỉ để người đọc.
/// </summary>
public sealed class LintBaseline
{
    sealed record Entry(string Severity, string Rule, int Count, string Path, string Note)
    {
        public string Key => Rule + " | " + Path;
    }

    readonly Dictionary<string, List<Entry>> _scopes = new(StringComparer.Ordinal);

    public LintBaseline(string filePath) { FilePath = filePath; }

    public string FilePath { get; }
    /// <summary>Đã bị <see cref="Replace"/> sửa và chưa lưu.</summary>
    public bool Dirty { get; private set; }

    public static LintBaseline Load(string filePath)
    {
        var baseline = new LintBaseline(filePath);
        if (!File.Exists(filePath)) return baseline;
        List<Entry>? current = null;
        foreach (string raw in File.ReadAllLines(filePath))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            if (line[0] == '[' && line[^1] == ']') { baseline._scopes[line[1..^1]] = current = new List<Entry>(); continue; }
            if (current is null) continue;
            int noteAt = line.IndexOf("  ## ", StringComparison.Ordinal);
            string note = noteAt >= 0 ? line[(noteAt + 5)..] : "";
            var parts = (noteAt >= 0 ? line[..noteAt] : line).Split(" | ", 4);
            if (parts.Length < 4) continue;
            int.TryParse(parts[2].Trim().TrimStart('×'), out int count);
            current.Add(new Entry(parts[0].Trim(), parts[1].Trim(), Math.Max(1, count), parts[3].Trim(), note));
        }
        return baseline;
    }

    public int Count(string scope) => _scopes.TryGetValue(scope, out var list) ? list.Count : 0;

    /// <summary>Thay toàn bộ nhóm <paramref name="scope"/> bằng danh sách hiện tại (dùng cho <c>--update-baseline</c>).</summary>
    public void Replace(string scope, IReadOnlyList<LintIssue> issues)
    {
        var merged = new Dictionary<string, Entry>(StringComparer.Ordinal);
        foreach (var i in issues)
        {
            string note = i.Message + (i.Config is null ? "" : $"  [{i.Config}]");
            merged[i.Key] = merged.TryGetValue(i.Key, out var e)
                ? e with { Count = e.Count + i.Count }
                : new Entry(i.Severity.ToString().ToLowerInvariant(), i.Rule, i.Count, i.Path, note.Replace('\n', ' '));
        }
        var list = new List<Entry>(merged.Values);
        list.Sort(static (a, b) => string.CompareOrdinal(a.Key, b.Key));
        _scopes[scope] = list;
        Dirty = true;
    }

    /// <summary>
    /// So với nhóm <paramref name="scope"/>. <c>Fresh</c>: vấn đề chưa có trong baseline (hoặc xuất hiện nhiều lần hơn);
    /// <c>Gone</c>: số vấn đề trong baseline nay không còn.
    /// </summary>
    public (List<LintIssue> Fresh, int Gone) Compare(string scope, IReadOnlyList<LintIssue> issues)
    {
        var known = new Dictionary<string, int>(StringComparer.Ordinal);
        if (_scopes.TryGetValue(scope, out var list))
            foreach (var e in list) known[e.Key] = known.GetValueOrDefault(e.Key) + e.Count;
        var fresh = new List<LintIssue>();
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var i in issues)
        {
            int total = seen[i.Key] = seen.GetValueOrDefault(i.Key) + i.Count;
            if (total > known.GetValueOrDefault(i.Key)) fresh.Add(i);
        }
        int gone = 0;
        foreach (var kv in known) gone += Math.Max(0, kv.Value - seen.GetValueOrDefault(kv.Key));
        return (fresh, gone);
    }

    public void Save()
    {
        var sb = new StringBuilder();
        sb.Append("# Baseline lint của GotX.Tests – các vấn đề ĐÃ BIẾT. Chỉ vấn đề không có ở đây mới làm hỏng test.\n");
        sb.Append("# Dòng: mức | luật | ×số phần tử | đường dẫn phần tử  ## số đo lúc ghi [cấu hình phát hiện]\n");
        sb.Append("# Sửa xong một lỗi thì xoá dòng đó, hoặc chạy:  dotnet run --project tests/GotX.Tests -- --update-baseline\n");
        var names = new List<string>(_scopes.Keys);
        names.Sort(StringComparer.Ordinal);
        foreach (string name in names)
        {
            sb.Append("\n[").Append(name).Append("]\n");
            foreach (var e in _scopes[name])
                sb.Append($"{e.Severity,-7} | {e.Rule,-13} | ×{e.Count} | {e.Path}  ## {e.Note}\n");
        }
        File.WriteAllText(FilePath, sb.ToString());
        Dirty = false;
    }
}
