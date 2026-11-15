using GotX;
using GotX.Test;
using GotX.Text;

/// <summary>
/// Các cache đường nóng của chữ / Tailwind (trang glyph, kerning, ngắt dòng theo tham chiếu chuỗi, bảng glyph→atlas, FontFor)
/// phải cho đúng kết quả như khi tra trực tiếp, và thực sự trúng cache ở frame ổn định.
/// </summary>
static class PerfCacheTests
{
    static byte[]? FontBytes()
    {
        string fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        foreach (string path in new[] { Path.Combine(fonts, "segoeui.ttf"), "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf" })
            if (File.Exists(path)) return File.ReadAllBytes(path);
        return null;
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("perf/cache/glyph-index-and-advance-independent-of-lookup-order", t =>
        {
            var data = FontBytes();
            if (data is null) { t.Notes.Add("không có font hệ thống: bỏ qua"); return; }
            var a = new Font(data, 1, FontWeight.Normal);
            var b = new Font(data, 2, FontWeight.Normal);
            var rng = new Random(7);
            var cps = Enumerable.Range(0, 6000).Select(_ => rng.Next(0, 0x10000)).Concat(Enumerable.Range(0x20, 0x250)).Concat(Enumerable.Range(0x1E00, 0x100)).ToArray();
            // a tra theo thứ tự xuôi, b theo thứ tự ngược và hai lần: trang glyph nhớ lại không được làm lệch kết quả
            var ga = cps.Select(cp => a.GlyphIndex(cp)).ToArray();
            var gb = cps.Reverse().Select(cp => b.GlyphIndex(cp)).Reverse().ToArray();
            t.Assert.True(ga.SequenceEqual(gb), "GlyphIndex giống nhau bất kể thứ tự tra");
            t.Assert.True(cps.Select(cp => a.GlyphIndex(cp)).SequenceEqual(ga), "tra lại lần hai ra cùng kết quả (từ cache)");
            var adv = ga.Distinct().ToArray();
            t.Assert.True(adv.All(g => a.AdvanceWidth(g) == b.AdvanceWidth(g)) && a.AdvanceWidth((ushort)65000) == b.AdvanceWidth((ushort)65000), "AdvanceWidth giống nhau, kể cả glyph ngoài bảng hmtx");
            t.Assert.True(a.GlyphIndex('A') != 0 && a.GlyphIndex('A') != a.GlyphIndex('B') && a.GlyphIndex(0x1EC7) != 0, "ký tự thường và chữ Việt có glyph riêng");
        });

        suite.Add("perf/cache/kerning-cache-survives-slot-collisions", t =>
        {
            var data = FontBytes();
            if (data is null) { t.Notes.Add("không có font hệ thống: bỏ qua"); return; }
            var a = new Font(data, 1, FontWeight.Normal);
            var b = new Font(data, 2, FontWeight.Normal);
            if (!a.HasKerning) { t.Notes.Add("font không có kerning: bỏ qua"); return; }
            var rng = new Random(11);
            var pairs = Enumerable.Range(0, 30000).Select(_ => ((ushort)rng.Next(1, 400), (ushort)rng.Next(1, 400))).ToArray(); // ≫ 4096 ô → va chạm chắc chắn
            var ka = pairs.Select(p => a.Kern(p.Item1, p.Item2)).ToArray();
            var kb = pairs.Reverse().Select(p => b.Kern(p.Item1, p.Item2)).Reverse().ToArray();
            t.Assert.True(ka.SequenceEqual(kb), "Kern giống nhau bất kể thứ tự tra (va chạm ô cache không trả nhầm cặp)");
            t.Assert.True(ka.Any(k => k != 0), "mẫu có cặp kerning khác 0");
            t.Assert.True(pairs.Select(p => a.Kern(p.Item1, p.Item2)).SequenceEqual(ka), "tra lại cho cùng kết quả");
        });

        suite.Add("perf/cache/wrap-reference-cache-matches-content-cache", t =>
        {
            var text = TestFonts.Shared;
            var style = TextStyle.Default with { FontSize = 14, LineHeight = 20 };
            string s = string.Join(' ', Enumerable.Range(0, 60).Select(i => "từ" + i));
            string same = new string(s.AsSpan()); // cùng nội dung, khác đối tượng
            var first = new List<TextLine>();
            text.Wrap(s, style, 240, first);
            var again = new List<TextLine>();
            var other = new List<TextLine>();
            var narrow = new List<TextLine>();
            text.Wrap(s, style, 240, again);        // trúng tầng tham chiếu
            text.Wrap(same, style, 240, other);     // trượt tầng tham chiếu, trúng tầng nội dung
            text.Wrap(s, style, 120, narrow);       // khác bề rộng → không được trả kết quả cũ
            t.Assert.True(first.Count > 3 && first.SequenceEqual(again) && first.SequenceEqual(other), "cùng chuỗi/bề rộng cho cùng các dòng qua cả hai tầng cache");
            t.Assert.True(narrow.Count > first.Count, $"bề rộng hẹp hơn ra nhiều dòng hơn ({narrow.Count} > {first.Count})");
            var bold = style with { Weight = FontWeight.Bold };
            var boldLines = new List<TextLine>();
            text.Wrap(s, bold, 240, boldLines);
            t.Assert.True(boldLines.Count >= first.Count && !boldLines.SequenceEqual(first), "độ đậm khác → ngắt dòng khác (khoá cache có font)");
            var fresh = new TextSystem();
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            if (fresh.TryAddFont(Path.Combine(dir, "segoeui.ttf"), FontWeight.Normal))
            {
                var lines = new List<TextLine>();
                fresh.Wrap(s, style, 240, lines);
                t.Assert.True(lines.SequenceEqual(first), "TextSystem mới (cache rỗng) ngắt dòng y hệt");
            }
        });

        suite.Add("perf/cache/steady-frames-hit-text-and-class-caches", t =>
        {
            t.Open(new CounterHost());
            t.Window.Trace = null;
            t.Frames(5);
            var before = PerfCounters.Snapshot.Now();
            for (int i = 0; i < 30; i++) t.Window.Draw(t.Size, 1);
            var delta = PerfCounters.Snapshot.Now() - before;
            t.Assert.Equal(delta.WrapMiss, 0L, "frame ổn định không ngắt dòng lại chữ nào");
            t.Assert.Equal(delta.ClassMiss, 0L, "frame ổn định không biên dịch class Tailwind mới");
            t.Assert.Equal(delta.GlyphRaster, 0L, "frame ổn định không raster glyph mới");
        });

        suite.Add("perf/cache/font-for-matches-scoring", t =>
        {
            var text = TestFonts.Shared;
            // FontFor nhớ theo (họ, độ đậm, nghiêng): kết quả lần đầu và lần sau giống nhau, độ đậm trung gian vẫn chọn font gần nhất
            foreach (var family in new[] { FontFamily.Sans, FontFamily.Mono })
                foreach (var italic in new[] { false, true })
                    foreach (int w in new[] { 100, 300, 400, 450, 500, 600, 700, 900 })
                    {
                        var a = text.FontFor(family, (FontWeight)w, italic);
                        var b = text.FontFor(family, (FontWeight)w, italic);
                        t.Assert.True(ReferenceEquals(a, b), $"FontFor({family}, {w}, {italic}) ổn định");
                    }
            var normal = text.FontFor(FontFamily.Sans, FontWeight.Normal, false);
            var bold = text.FontFor(FontFamily.Sans, FontWeight.Bold, false);
            t.Assert.True(normal.Id != bold.Id || normal.Weight == bold.Weight, "độ đậm Bold không rơi về cùng font với Normal khi có font đậm");
            t.Assert.True(ReferenceEquals(text.FontFor(FontFamily.Sans, (FontWeight)350, false), text.FontFor(FontFamily.Sans, (FontWeight)350, false)), "độ đậm lẻ (không bội 100) vẫn đúng");
        });
    }
}
