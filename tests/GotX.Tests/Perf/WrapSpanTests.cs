using GotX;
using GotX.Test;
using GotX.Text;

/// <summary>Cache ngắt dòng theo khoảng bề rộng: mọi bề rộng phải cho đúng các dòng như tính trực tiếp (cache tắt), kể cả từ dài, '\n', CJK, và không cấp phát khi kéo giãn.</summary>
static class WrapSpanTests
{
    public static void Register(TestSuite suite)
    {
        suite.Add("perf/wrap/span-cache-equals-direct-wrap-at-any-width", t =>
        {
            var text = TestFonts.Shared;
            var rng = new Random(5);
            string[] words = ["a", "xin", "chào", "GotX", "lorem", "ipsum", "dolor", "sit", "amet", "supercalifragilisticexpialidocious", "日本語のテキスト", "x\ny", "", "  "];
            var cached = new List<TextLine>(); var direct = new List<TextLine>();
            int checkedWidths = 0;
            for (int round = 0; round < 40; round++)
            {
                string s = string.Join(' ', Enumerable.Range(0, rng.Next(1, 30)).Select(_ => words[rng.Next(words.Length)]));
                var style = TextStyle.Default with { FontSize = 12 + rng.Next(8), LineHeight = 20 };
                for (int k = 0; k < 60; k++)
                {
                    float w = k % 3 == 0 ? rng.Next(5, 600) : 200 + (k % 7) * 3 + (float)rng.NextDouble();
                    if (k == 5) w = float.PositiveInfinity;
                    text.Wrap(s, style, w, cached);
                    int old = text.MaxCachedWraps; text.MaxCachedWraps = 0;
                    text.Wrap(new string(s), style, w, direct); // cache tắt và chuỗi khác tham chiếu: tính thẳng
                    text.MaxCachedWraps = old;
                    checkedWidths++;
                    if (!cached.SequenceEqual(direct)) { t.Fail($"wrap lệch: \"{s}\" bề rộng {w} cỡ {style.FontSize}: {cached.Count} dòng ≠ {direct.Count}"); return; }
                }
            }
            t.Notes.Add($"{checkedWidths} cặp (chuỗi, bề rộng) khớp");
        });

        suite.Add("perf/wrap/resize-drag-hits-span-cache", t =>
        {
            var text = TestFonts.Shared;
            var style = TextStyle.Default with { FontSize = 14, LineHeight = 20 };
            string s = "Một đoạn chữ vừa phải để thử kéo giãn cửa sổ từng pixel một";
            var lines = new List<TextLine>();
            text.Wrap(s, style, 400, lines);
            var before = PerfCounters.Snapshot.Now();
            long alloc0 = GC.GetAllocatedBytesForCurrentThread();
            for (float w = 400; w < 440; w += 0.5f) text.Wrap(s, style, w, lines);
            long alloc = GC.GetAllocatedBytesForCurrentThread() - alloc0;
            var d = PerfCounters.Snapshot.Now() - before;
            t.Assert.Equal(d.WrapMiss, 0L, "kéo giãn trong khoảng cùng kết quả không ngắt dòng lại");
            t.Assert.True(alloc <= 256, $"kéo giãn không cấp phát: {alloc} B");
        });
    }
}
