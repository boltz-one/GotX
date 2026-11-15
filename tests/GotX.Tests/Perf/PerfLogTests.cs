using System.Text.Json;
using GotX;
using GotX.Test;

/// <summary>Nhật ký hiệu năng (PerfLog / Window.Log): định dạng bản ghi, nguyên nhân, và chi phí khi bật (không cấp phát ở frame ổn định).</summary>
static class PerfLogTests
{
    static string TempLog() => Path.Combine(Path.GetTempPath(), "gotx-perflog-test-" + Guid.NewGuid().ToString("N") + ".jsonl");

    static List<JsonElement> Read(string path)
    {
        var list = new List<JsonElement>();
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete); // luồng ghi nền còn giữ file
        using var reader = new StreamReader(fs);
        string? line;
        while ((line = reader.ReadLine()) is not null)
            if (line.Length > 0) list.Add(JsonDocument.Parse(line).RootElement.Clone());
        return list;
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("perf/log/one-record-per-frame-with-causes", t =>
        {
            string path = TempLog();
            try
            {
                using var log = new PerfLog(path, PerfLogLevel.Trace);
                t.Open(new CounterHost());
                t.Window.Log = log;
                log.Mark("phase-a");
                int firstFrame = t.Window.LastFrame.Frame + 1;
                t.Click("c-inc");
                t.Window.DarkMode = true;
                t.Frames(3);
                int lastFrame = t.Window.LastFrame.Frame;
                log.Flush();
                var records = Read(path);
                var frames = records.Where(r => r.GetProperty("t").GetString() == "frame").ToList();
                t.Assert.True(frames.Count >= lastFrame - firstFrame + 1, $"mỗi Draw một bản ghi frame ({frames.Count} bản ghi cho {lastFrame - firstFrame + 1} frame)");
                int prev = 0;
                bool ordered = true, shaped = true;
                foreach (var f in frames)
                {
                    int n = f.GetProperty("n").GetInt32();
                    ordered &= n > prev;
                    prev = n;
                    shaped &= f.TryGetProperty("draw", out _) && f.TryGetProperty("layout", out _) && f.TryGetProperty("paint", out _)
                        && f.TryGetProperty("rend", out _) && f.TryGetProperty("repl", out _) && f.TryGetProperty("alloc", out _) && f.TryGetProperty("gc", out var gc) && gc.GetArrayLength() == 3;
                }
                t.Assert.True(ordered, "số frame tăng dần");
                t.Assert.True(shaped, "mỗi bản ghi có draw/layout/paint/rend/repl/alloc/gc");
                t.Assert.True(records.Any(r => r.GetProperty("t").GetString() == "mark" && r.GetProperty("name").GetString() == "phase-a"), "có mark do app đặt");
                t.Assert.True(frames.Any(f => f.TryGetProperty("in", out var inp) && inp.EnumerateArray().Any(e => e.ValueKind == JsonValueKind.String && e.GetString() == "MouseDown")),
                    "input của click được ghi vào frame kế tiếp");
                t.Assert.True(frames.Any(f => f.TryGetProperty("views", out var v) && v.EnumerateArray().Any(e => e.ValueKind == JsonValueKind.String && e.GetString() == "CounterView")),
                    "tên view đã render (CounterView) nằm trong views");
                t.Assert.True(frames.Any(f => f.TryGetProperty("cause", out var c) && c.EnumerateArray().Any(e => e.GetString() == "Notify")), "nguyên nhân bẩn Notify");
                t.Assert.True(records.Any(r => r.GetProperty("t").GetString() == "warn" && r.GetProperty("kind").GetString() == "cache-cleared" && r.GetProperty("msg").GetString()!.Contains("dark")),
                    "bật dark mode → cảnh báo cache-cleared lý do dark");
                t.Assert.Equal(log.Dropped, 0L, "không bản ghi nào bị bỏ");
            }
            finally { File.Delete(path); }
        });

        suite.Add("perf/log/slow-level-writes-only-slow-frames", t =>
        {
            string path = TempLog();
            try
            {
                using var log = new PerfLog(path, PerfLogLevel.Slow, slowFrameMs: 1e9);
                t.Open(new CounterHost());
                t.Window.Log = log;
                t.Frames(10);
                log.Warn("custom", "vẫn ghi cảnh báo");
                log.Flush();
                var records = Read(path);
                t.Assert.Equal(records.Count(r => r.GetProperty("t").GetString() == "frame"), 0, "mức slow: frame nhanh không được ghi");
                t.Assert.Equal(records.Count(r => r.GetProperty("t").GetString() == "warn"), 1, "cảnh báo vẫn được ghi");
            }
            finally { File.Delete(path); }
        });

        suite.Add("perf/log/enabled-costs-no-allocation-on-steady-frames", t =>
        {
            string path = TempLog();
            try
            {
                using var log = new PerfLog(path, PerfLogLevel.Frames);
                t.Open(new CounterHost());
                t.Window.Log = log;
                t.Window.Trace = null; // Trace (khung test) tự cấp phát chuỗi mỗi frame – ở đây chỉ đo chi phí của chính nhật ký
                var size = t.Size;
                for (int i = 0; i < 20; i++) t.Window.Draw(size, 1); // làm nóng: JIT, chuỗi tên view, bộ đệm
                var bytes = new List<double>();
                for (int i = 0; i < 40; i++)
                {
                    long before = GC.GetAllocatedBytesForCurrentThread();
                    t.Window.Draw(size, 1);
                    bytes.Add(GC.GetAllocatedBytesForCurrentThread() - before);
                }
                bytes.Sort();
                t.Assert.True(bytes[bytes.Count / 2] == 0, $"frame ổn định có bật log: trung vị cấp phát {bytes[bytes.Count / 2]} B (cả việc ghi nhật ký)");
                t.Assert.True(bytes[^3] <= 256, $"hầu hết frame bật log không cấp phát đáng kể (p95 {bytes[^3]} B)");
            }
            finally { File.Delete(path); }
        });

        suite.Add("perf/log/disabled-is-free", t =>
        {
            t.Open(new CounterHost());
            t.Window.Log = null;
            t.Window.Trace = null;
            var size = t.Size;
            for (int i = 0; i < 20; i++) t.Window.Draw(size, 1);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 40; i++) t.Window.Draw(size, 1);
            t.Assert.Equal(GC.GetAllocatedBytesForCurrentThread() - before, 0L, "tắt log: 40 frame phát lại không cấp phát byte nào");
        });
    }
}
