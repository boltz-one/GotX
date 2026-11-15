using System.Text.Json;
using GotX;
using GotX.Test;

/// <summary>Runner: bắt exception, tính giờ, khối lỗi cho agent đọc, report.json, nhật ký trước lỗi.</summary>
static class RunnerTests
{
    public static void Register(TestSuite suite)
    {
        suite.Add("runner/exception-keeps-stack", t =>
        {
            var result = TestSuite.RunOne("nhóm/ném", p => { p.Open(new CounterHost()); p.Click("c-inc"); Throwing(); }, new TestOptions());
            t.Assert.Equal(result.Failures.Count, 1, "exception thành một lỗi");
            if (result.Failures.Count != 1) return;
            var f = result.Failures[0];
            t.Assert.Equal(f.Message, "ném InvalidOperationException: hỏng có chủ ý", "thông báo exception");
            t.Assert.True(f.Stack?.Contains("RunnerTests.Throwing") == true, "giữ stack trace");
            t.Assert.True(f.Trace.Exists(e => e.Contains("Click: CounterView")), "kèm các sự kiện trước lỗi");
            t.Assert.True(result.Milliseconds > 0, "có tính giờ");
        });

        suite.Add("runner/abort-stops-only-that-test", t =>
        {
            var result = TestSuite.RunOne("nhóm/thiếu", p => { p.Open(new CounterHost()); p.Click("khong-co"); p.Assert.True(false, "không tới đây"); }, new TestOptions());
            t.Assert.True(result.Failures.Count == 1 && result.Failures[0].Stack is null, "thiếu phần tử: một lỗi gọn, không stack");
        });

        suite.Add("runner/failure-block-and-json", t =>
        {
            string dir = Kit.TempDir("runner");
            var options = new TestOptions { OutDir = dir };
            var bad = TestSuite.RunOne("nhom/hong", p =>
            {
                p.Open(new CounterHost());
                p.Click("c-inc");
                p.Assert.Text("c-value", "Giá trị: 5");
                p.Assert.Size("c-inc", 10, 10);
            }, options);
            var good = TestSuite.RunOne("nhom/dat", p => { p.Open(new CounterHost()); p.Assert.Exists("c-inc"); p.Notes.Add("một ghi chú"); }, options);

            string block = TestReport.FailureBlock(bad);
            string[] expected =
            [
                "FAIL  nhom/hong (", ") — 2 lỗi",
                "  ✗ Text: #c-value mong đợi \"Giá trị: 5\", thực tế \"Giá trị: 1\"",
                "    tại:  CounterHost >> CounterView > div[1] > div#c-value .text-sm [",
                "    file: " + Path.Combine(dir, "nhom_hong.fail1.png"),
                "    file: " + Path.Combine(dir, "nhom_hong.fail1.tree.txt"),
                "    trước đó:", "Click: CounterView", "ViewDirty: CounterView", "ViewRender: CounterView",
                "  ✗ Size: #c-inc mong đợi 10×10",
            ];
            foreach (string part in expected) t.Assert.True(block.Contains(part), $"khối lỗi phải chứa «{part}»:\n{block}");
            t.Assert.Equal(block.Split("trước đó:").Length, 2, "nhật ký chỉ in một lần cho cả khối");
            t.Assert.True(block.Split('\n').Length < 40, "khối lỗi ngắn gọn");
            t.Assert.True(TestReport.PassLine(good).StartsWith("ok    nhom/dat (") && TestReport.PassLine(good).Contains("· một ghi chú"), "dòng đạt (--verbose) kèm ghi chú");
            string summary = TestReport.Summary("Bộ thử", [bad, good], 1234, "r.json", 3);
            t.Assert.Equal(summary, "Bộ thử: 1 đạt, 1 hỏng, 3 bị lọc, 1 ghi chú (--verbose) trong 1.23 s — r.json", "dòng tổng kết");

            using var json = JsonDocument.Parse(TestReport.Json("Bộ thử", [bad, good], 1234));
            var root = json.RootElement;
            t.Assert.True(root.GetProperty("passed").GetInt32() == 1 && root.GetProperty("failed").GetInt32() == 1, "report.json: số đạt/hỏng");
            var first = root.GetProperty("tests")[0];
            t.Assert.Equal(first.GetProperty("status").GetString(), "failed", "report.json: trạng thái");
            var failure = first.GetProperty("failures")[0];
            t.Assert.True(failure.GetProperty("message").GetString()!.Contains("\"Giá trị: 5\"") && failure.GetProperty("bounds").GetArrayLength() == 4
                && failure.GetProperty("artifacts").GetArrayLength() == 2 && failure.GetProperty("trace").GetArrayLength() > 3, "report.json: lỗi có message (đã escape), bounds, artifacts, trace");
            t.Assert.Equal(root.GetProperty("tests")[1].GetProperty("notes")[0].GetString(), "một ghi chú", "report.json: ghi chú");
        });

        suite.Add("runner/suite-cli", t =>
        {
            string dir = Kit.TempDir("suite");
            var inner = new TestSuite { Title = "Con", BaselinePath = Path.Combine(dir, "baseline.txt"), SnapshotDir = dir };
            int ran = 0;
            inner.Add("a/một", p => { ran++; p.Open(new CounterHost()); });
            inner.Add("a/hai", p => { ran++; p.Open(new CounterHost()); p.Assert.Exists("zzz"); });
            inner.Add("b/ba", p => { ran++; });
            var console = Console.Out;
            var captured = new StringWriter();
            Console.SetOut(captured);
            int listCode, passCode, failCode, badCode;
            try
            {
                listCode = inner.Run(["--list", "--filter", "a/"]);
                passCode = inner.Run(["--filter", "một", "--out", dir, "--verbose"]);
                failCode = inner.Run(["--out", dir]);
                var err = Console.Error;
                Console.SetError(captured);
                badCode = inner.Run(["--nope"]);
                Console.SetError(err);
            }
            finally { Console.SetOut(console); }
            string output = captured.ToString();
            t.Assert.True(listCode == 0 && output.StartsWith("a/một" + Environment.NewLine + "a/hai" + Environment.NewLine), "--list --filter chỉ in tên test khớp");
            t.Assert.True(passCode == 0 && output.Contains("ok    a/một (") && output.Contains("Con: 1 đạt, 0 hỏng, 2 bị lọc"), "--filter + --verbose: mã 0");
            t.Assert.True(failCode == 1 && output.Contains("FAIL  a/hai") && output.Contains("Con: 2 đạt, 1 hỏng trong"), "có test hỏng: mã 1, test đạt không in dòng nào");
            t.Assert.True(badCode == 2 && output.Contains("Tham số không hiểu: --nope"), "tham số lạ: mã 2");
            t.Assert.Equal(ran, 4, "đúng số test đã chạy (1 + 3)");
            using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "report.json")));
            t.Assert.Equal(json.RootElement.GetProperty("tests").GetArrayLength(), 3, "report.json ghi vào thư mục --out");
        });
    }

    static void Throwing() => throw new InvalidOperationException("hỏng có chủ ý");
}
