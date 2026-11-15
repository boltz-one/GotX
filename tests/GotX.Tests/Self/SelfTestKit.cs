using GotX;
using GotX.Test;

/// <summary>Đồ nghề cho các test tự kiểm tra khung: chạy một "test bên trong" để xem nó ghi lỗi gì, lint một fixture.</summary>
static class Kit
{
    /// <summary>
    /// Chạy <paramref name="body"/> trong một UiTest riêng và trả nó về – cách kiểm tra rằng một assertion HỎNG đúng lúc
    /// và ghi đúng thông tin, mà không làm hỏng test bên ngoài. <paramref name="outDir"/> khác null thì test trong ghi cả file bằng chứng.
    /// </summary>
    public static UiTest Probe(Action<UiTest> body, string? outDir = null, string? snapshotDir = null, bool updateSnapshots = false)
    {
        var options = new TestOptions { OutDir = outDir, SnapshotDir = snapshotDir, UpdateSnapshots = updateSnapshots };
        var inner = new UiTest("probe", options);
        try { body(inner); }
        catch (TestAbortException) { }
        return inner;
    }

    /// <summary>Test trong phải có đúng một lỗi và thông báo chứa mọi chuỗi trong <paramref name="parts"/>.</summary>
    public static Failure? ExpectOneFailure(UiTest t, UiTest inner, string what, params string[] parts)
    {
        if (!t.Assert.Equal(inner.Failures.Count, 1, $"{what}: số lỗi được ghi")) return inner.Failures.Count > 0 ? inner.Failures[0] : null;
        var f = inner.Failures[0];
        foreach (string part in parts)
            t.Assert.True(f.Message.Contains(part, StringComparison.Ordinal), $"{what}: thông báo phải chứa \"{part}\" — thực tế: {f.Message}");
        return f;
    }

    /// <summary>Mở fixture dựng từ lambda và chạy lint.</summary>
    public static List<LintIssue> Lint(UiTest t, Func<Window, Div> render, float scale = 1, Size? size = null, LintOptions? options = null)
    {
        t.Open(new Fx(render), size ?? new Size(640, 400), scale);
        return t.Lint(options);
    }

    /// <summary>
    /// Kết quả lint phải gồm ĐÚNG <paramref name="count"/> vấn đề, tất cả thuộc luật <paramref name="rule"/> và nằm trên phần
    /// tử có đường dẫn chứa <paramref name="pathPart"/>; không luật nào khác được lên tiếng (kể cả mức info).
    /// </summary>
    public static LintIssue? ExpectOnly(UiTest t, List<LintIssue> issues, string rule, string pathPart, int count = 1, Severity? severity = null)
    {
        LintIssue? hit = null;
        int matched = 0;
        foreach (var i in issues)
        {
            if (i.Rule == rule && i.Path.Contains(pathPart, StringComparison.Ordinal)) { matched++; hit ??= i; }
            else t.Fail($"lint báo thừa (mong đợi chỉ {rule} trên \"{pathPart}\"): {i}");
        }
        t.Assert.Equal(matched, count, $"số vấn đề {rule} trên \"{pathPart}\"");
        if (hit is not null && severity is { } s) t.Assert.Equal(hit.Severity, s, $"mức của {rule}");
        return hit;
    }

    public static void ExpectClean(UiTest t, List<LintIssue> issues, string what)
    {
        foreach (var i in issues) t.Fail($"{what}: lint phải im lặng nhưng báo: {i}");
    }

    /// <summary>Thư mục tạm riêng cho một test (xoá sạch trước khi dùng).</summary>
    public static string TempDir(string name)
    {
        string dir = Path.Combine(Path.GetTempPath(), "gotx-tests", name);
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        Directory.CreateDirectory(dir);
        return dir;
    }
}
