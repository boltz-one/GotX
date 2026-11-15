using System.Text;

namespace GotX.Test;

/// <summary>
/// Định dạng kết quả cho agent đọc trên terminal: test đạt thì im lặng (trừ --verbose), mỗi lỗi một khối ngắn gồm
/// thông báo, đường dẫn phần tử + hộp, file bằng chứng và các sự kiện ngay trước đó. Kèm report.json viết tay.
/// </summary>
public static class TestReport
{
    const int MaxTraceLines = 12, MaxStackLines = 8, MaxFailuresShown = 12;

    public static string PassLine(TestResult r)
    {
        var sb = new StringBuilder($"ok    {r.Name} ({Fmt.N(r.Milliseconds)} ms)\n");
        foreach (string note in r.Notes) sb.Append("      · ").Append(note).Append('\n');
        return sb.ToString();
    }

    public static string FailureBlock(TestResult r)
    {
        var sb = new StringBuilder($"FAIL  {r.Name} ({Fmt.N(r.Milliseconds)} ms) — {r.Failures.Count} lỗi\n");
        int shown = 0;
        foreach (var f in r.Failures)
        {
            if (++shown > MaxFailuresShown) { sb.Append($"  … và {r.Failures.Count - MaxFailuresShown} lỗi nữa (xem report.json)\n"); break; }
            sb.Append("  ✗ ").Append(f.Message).Append('\n');
            if (f.Path is not null) sb.Append("    tại:  ").Append(f.Path).Append(f.Bounds is { } b ? " " + Fmt.B(b) : "").Append('\n');
            else if (f.Bounds is { } area) sb.Append("    vùng: ").Append(Fmt.B(area)).Append('\n');
            foreach (string d in f.Details) sb.Append("    ").Append(d).Append('\n');
            foreach (string a in f.Artifacts) sb.Append("    file: ").Append(a).Append('\n');
            if (f.Stack is { } stack)
            {
                var lines = stack.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                for (int i = 0; i < Math.Min(lines.Length, MaxStackLines); i++) sb.Append("    ").Append(lines[i]).Append('\n');
            }
            // Nhật ký giống nhau giữa các lỗi liền nhau của cùng một test: chỉ in ở lỗi đầu để khối không phình ra.
            if (shown == 1 && f.Trace.Count > 0)
            {
                sb.Append("    trước đó:\n");
                for (int i = Math.Max(0, f.Trace.Count - MaxTraceLines); i < f.Trace.Count; i++) sb.Append("      ").Append(f.Trace[i]).Append('\n');
            }
        }
        foreach (string note in r.Notes) sb.Append("  · ").Append(note).Append('\n');
        return sb.ToString();
    }

    public static string Summary(string title, List<TestResult> results, double ms, string reportPath, int filteredOut)
    {
        int passed = results.FindAll(r => r.Passed).Count, failed = results.Count - passed, notes = 0;
        foreach (var r in results) notes += r.Notes.Count;
        return $"{title}: {passed} đạt, {failed} hỏng" + (filteredOut > 0 ? $", {filteredOut} bị lọc" : "") +
               (notes > 0 ? $", {notes} ghi chú (--verbose)" : "") + $" trong {Fmt.N(ms / 1000)} s — {reportPath}";
    }

    public static string Json(string title, List<TestResult> results, double ms)
    {
        var j = new JsonWriter();
        j.BeginObject();
        j.Name("suite").Value(title);
        j.Name("passed").Value(results.FindAll(r => r.Passed).Count);
        j.Name("failed").Value(results.FindAll(r => !r.Passed).Count);
        j.Name("milliseconds").Value(ms);
        j.Name("tests").BeginArray();
        foreach (var r in results)
        {
            j.BeginObject();
            j.Name("name").Value(r.Name).Name("status").Value(r.Passed ? "passed" : "failed").Name("milliseconds").Value(r.Milliseconds);
            if (r.Notes.Count > 0) j.Name("notes").Strings(r.Notes);
            if (r.Failures.Count > 0)
            {
                j.Name("failures").BeginArray();
                foreach (var f in r.Failures)
                {
                    j.BeginObject().Name("message").Value(f.Message);
                    if (f.Path is not null) j.Name("path").Value(f.Path);
                    if (f.Bounds is { } b) j.Name("bounds").Bounds(b);
                    if (f.Details.Count > 0) j.Name("details").Strings(f.Details);
                    if (f.Artifacts.Count > 0) j.Name("artifacts").Strings(f.Artifacts);
                    if (f.Trace.Count > 0) j.Name("trace").Strings(f.Trace);
                    if (f.Stack is not null) j.Name("stack").Value(f.Stack);
                    j.EndObject();
                }
                j.EndArray();
            }
            j.EndObject();
        }
        j.EndArray().EndObject();
        return j.ToString();
    }
}
