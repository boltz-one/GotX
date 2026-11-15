using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GotX.Test;

/// <summary>Kết quả của một test sau khi chạy.</summary>
public sealed class TestResult
{
    public required string Name { get; init; }
    public double Milliseconds { get; init; }
    public required List<Failure> Failures { get; init; }
    public required List<string> Notes { get; init; }
    public bool Passed => Failures.Count == 0;
}

/// <summary>
/// Bộ test đăng ký tường minh (không dò bằng reflection – giữ AOT-compatible):
/// <code>
/// var suite = new TestSuite();
/// suite.Add("button/click", t => { t.Open(new MyView()); t.Click("save"); t.Assert.Text("status", "Đã lưu"); });
/// return suite.Run(args);
/// </code>
/// Dòng lệnh: <c>[--filter chuỗi] [--out thư-mục] [--update-snapshots] [--update-baseline] [--list] [--verbose]</c>.
/// </summary>
public sealed class TestSuite
{
    readonly List<(string Name, Action<UiTest> Body)> _tests = new();

    /// <param name="sourceFile">Trình biên dịch tự điền: thư mục chứa file gọi được coi là thư mục dự án test.</param>
    public TestSuite([CallerFilePath] string sourceFile = "")
    {
        ProjectDir = Path.GetDirectoryName(sourceFile) ?? Directory.GetCurrentDirectory();
        SnapshotDir = Path.Combine(ProjectDir, "__snapshots__");
        BaselinePath = Path.Combine(ProjectDir, "lint-baseline.txt");
    }

    /// <summary>Thư mục mã nguồn của dự án test: nơi đặt <c>__snapshots__/</c> và <c>lint-baseline.txt</c>.</summary>
    public string ProjectDir { get; set; }
    public string SnapshotDir { get; set; }
    public string BaselinePath { get; set; }
    /// <summary>Tên hiện ở dòng tổng kết.</summary>
    public string Title { get; set; } = "GotX.Tests";
    public IReadOnlyList<(string Name, Action<UiTest> Body)> Tests => _tests;

    /// <summary>Đăng ký một test. Tên nên có dạng <c>nhóm/tên</c> để lọc bằng <c>--filter</c>.</summary>
    public void Add(string name, Action<UiTest> body) => _tests.Add((name, body));

    /// <summary>Chạy theo tham số dòng lệnh; trả về mã thoát (0 = mọi test đạt).</summary>
    public int Run(string[] args)
    {
        string? filter = null, outDir = null;
        bool list = false, verbose = false, updateSnapshots = false, updateBaseline = false;
        for (int i = 0; i < args.Length; i++)
            switch (args[i])
            {
                case "--filter" when i + 1 < args.Length: filter = args[++i]; break;
                case "--out" when i + 1 < args.Length: outDir = args[++i]; break;
                case "--list": list = true; break;
                case "--verbose": verbose = true; break;
                case "--update-snapshots": updateSnapshots = true; break;
                case "--update-baseline": updateBaseline = true; break;
                default:
                    Console.Error.WriteLine($"Tham số không hiểu: {args[i]}");
                    Console.Error.WriteLine("Dùng: [--filter chuỗi] [--out thư-mục] [--update-snapshots] [--update-baseline] [--list] [--verbose]");
                    return 2;
            }
        try { Console.OutputEncoding = System.Text.Encoding.UTF8; } catch (IOException) { }

        var selected = _tests.FindAll(t => filter is null || t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
        if (list)
        {
            foreach (var (name, _) in selected) Console.WriteLine(name);
            return 0;
        }
        outDir = Path.GetFullPath(outDir ?? Path.Combine(AppContext.BaseDirectory, "test-out"));
        Directory.CreateDirectory(outDir);
        var options = new TestOptions
        {
            OutDir = outDir, SnapshotDir = SnapshotDir, UpdateSnapshots = updateSnapshots, UpdateBaseline = updateBaseline,
            Verbose = verbose, Baseline = LintBaseline.Load(BaselinePath),
        };

        var results = new List<TestResult>();
        var clock = Stopwatch.StartNew();
        foreach (var (name, body) in selected)
        {
            var result = RunOne(name, body, options);
            results.Add(result);
            if (!result.Passed) Console.Write(TestReport.FailureBlock(result));
            else if (verbose) Console.Write(TestReport.PassLine(result));
        }
        if (options.Baseline.Dirty)
        {
            options.Baseline.Save();
            Console.WriteLine($"baseline lint đã ghi: {BaselinePath}");
        }
        string reportPath = Path.Combine(outDir, "report.json");
        File.WriteAllText(reportPath, TestReport.Json(Title, results, clock.Elapsed.TotalMilliseconds));
        Console.WriteLine(TestReport.Summary(Title, results, clock.Elapsed.TotalMilliseconds, reportPath, _tests.Count - selected.Count));
        return results.TrueForAll(r => r.Passed) ? 0 : 1;
    }

    /// <summary>Chạy một test: exception thoát ra được ghi thành lỗi (giữ stack), không làm dừng các test khác.</summary>
    public static TestResult RunOne(string name, Action<UiTest> body, TestOptions options)
    {
        var test = new UiTest(name, options);
        long start = Stopwatch.GetTimestamp();
        try { body(test); }
        catch (TestAbortException) { } // lỗi đã được ghi trước khi ném
        catch (Exception ex) { test.FailException(ex); }
        return new TestResult
        {
            Name = name, Failures = test.Failures, Notes = test.Notes,
            Milliseconds = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency,
        };
    }
}
