using GotX;
using GotX.Test;
using GotX.Text;

/// <summary>Ligature + cụm (caret), ngắt dòng UAX#14, bidi (thứ tự hiển thị + caret) và cấp phát mỗi frame.</summary>
static class TextLayoutTests
{
    static readonly string[] LigFonts = ["calibri.ttf", "cambria.ttc", "georgia.ttf", "constan.ttf", "corbel.ttf", "candara.ttf", "segoeui.ttf", "arial.ttf", "times.ttf", "tahoma.ttf"];

    /// <summary>Font Windows đầu tiên mà "fi" thành một glyph (ligature thật); null nếu không có.</summary>
    static TextSystem? LigatureSystem(out string name)
    {
        foreach (var f in LigFonts)
        {
            var ts = new TextSystem();
            if (!ts.TryAddFont(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), f), FontWeight.Normal)) continue;
            foreach (var pair in new[] { "fi", "fl", "ff", "ffi" })
                if (ts.Shape(pair, 0, pair.Length, TextStyle.Default, false) == 1) { name = $"{f}/{pair}"; return ts; }
        }
        name = "";
        return null;
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("core/text/ligature-cluster-caret", t =>
        {
            var ts = LigatureSystem(out string name);
            if (ts is null) { t.Notes.Add("SKIP: không font Windows nào có ligature fi/fl/ff trong GSUB liga"); return; }
            t.Notes.Add($"ligature thật: {name}");
            string s = name[(name.IndexOf('/') + 1)..];
            var st = TextStyle.Default;
            float whole = ts.Measure(s, st);
            int n = ts.Shape(s, 0, s.Length, st, false);
            t.Assert.True(n == 1 && ts.Glyphs[0].ClusterEnd - ts.Glyphs[0].Cluster == s.Length, $"{name}: một glyph phủ {s.Length} ký tự");
            float half = ts.MeasureRange(s, 0, 1, st);
            t.Assert.Near(half, whole / s.Length, 0.01f, "caret giữa ligature chia đều theo ký tự");
            t.Assert.Near(ts.CaretX(s, 0, s.Length, 1, st), half, 0.01f, "CaretX khớp MeasureRange");
            t.Assert.Equal(ts.IndexAtX(s, 0, s.Length, whole * 0.9f, st), s.Length, "bấm gần mép phải: cuối cụm");
            t.Assert.Equal(ts.IndexAtX(s, 0, s.Length, 0.5f, st), 0, "bấm gần mép trái: đầu cụm");
            int spaced = ts.Shape(s, 0, s.Length, st with { Tracking = 0.1f }, false);
            t.Assert.Equal(spaced, s.Length, "letter-spacing tắt ligature (như CSS)");
        });

        suite.Add("core/text/linebreak-cjk-and-latin", t =>
        {
            var ts = TextShapingTests.Make(false);
            if (ts is null) { t.Notes.Add("SKIP: không có segoeui.ttf"); return; }
            var st = TextStyle.Default;
            string cjk = "你好世界，这是一个测试。再来一句";
            float em = ts.Measure("你", st);
            var lines = new List<TextLine>();
            ts.Wrap(cjk, st, em * 3.5f, lines);
            t.Assert.True(lines.Count >= 4, $"CJK ngắt giữa các chữ biểu ý (được {lines.Count} dòng)");
            bool badStart = false;
            foreach (var l in lines) if (l.Length > 0 && cjk[l.Start] is '，' or '。') badStart = true;
            t.Assert.True(!badStart, "không dòng nào bắt đầu bằng dấu câu đóng (，。)");
            t.Assert.Equal(lines.Sum(l => l.Length), cjk.Length, "các dòng phủ kín chuỗi");

            ts.Wrap("alpha beta gamma delta", st, ts.Measure("alpha beta", st) + 1, lines);
            t.Assert.True(lines.Count == 3 && lines[0].Length == "alpha beta ".Length, $"Latin chỉ ngắt ở khoảng trắng (dòng đầu {lines[0].Length} ký tự)");
            ts.Wrap("superlongwordwithoutspaces", st, ts.Measure("superlong", st) + 1, lines);
            t.Assert.True(lines.Count >= 3 && lines[0].Start == 0, "từ dài hơn một dòng vẫn ngắt theo ký tự");
        });

        suite.Add("core/text/bidi-visual-order-and-caret", t =>
        {
            var ts = TextShapingTests.Make(false);
            if (ts is null || !ts.CanRender(0x5D0, TextStyle.Default)) { t.Notes.Add("SKIP: không có font có chữ Hebrew"); return; }
            var st = TextStyle.Default;
            string s = "abc אבג def"; // Hebrew 3 chữ ở giữa đoạn LTR
            int n = ts.Shape(s, 0, s.Length, st, true);
            var order = new List<int>();
            for (int i = 0; i < n; i++) order.Add(ts.Glyphs[i].Cluster);
            t.Assert.True(order.IndexOf(6) < order.IndexOf(5) && order.IndexOf(5) < order.IndexOf(4), $"chữ Hebrew hiển thị đảo (thứ tự cụm: {string.Join(",", order)})");
            t.Assert.True(order.IndexOf(2) < order.IndexOf(6) && order.IndexOf(4) < order.IndexOf(8), "chữ Latin hai bên giữ thứ tự");
            foreach (int p in new[] { 0, 1, 2, 5, 6, 8, 9, 10 })
            {
                float x = ts.CaretX(s, 0, s.Length, p, st);
                t.Assert.Equal(ts.IndexAtX(s, 0, s.Length, x, st), p, $"vòng tròn caret ↔ hit-test tại vị trí {p} (x={x:F1})");
            }
            t.Assert.True(ts.CaretX(s, 0, s.Length, 5, st) < ts.CaretX(s, 0, s.Length, 4, st), "trong chữ Hebrew, vị trí logic sau nằm bên trái vị trí logic trước");
            string pure = "abc def";
            t.Assert.Near(ts.CaretX(pure, 0, pure.Length, 3, st), ts.MeasureRange(pure, 0, 3, st), 0.01f, "chữ Latin: CaretX = MeasureRange");
        });

        suite.Add("core/text/window-draw-static-text-bytes-per-frame", t =>
        {
            var ts = TextShapingTests.Make(true);
            if (ts is null) { t.Notes.Add("SKIP: không có segoeui.ttf"); return; }
            long Run(bool cache)
            {
                var app = new App();
                var window = new Window(app, ts) { CacheEnabled = cache };
                window.Root = app.New(new Fx(w => Fx.Page(w).Child(w.Div().Class("w-64 text-sm").Child("The quick brown fox jumps over the lazy dog, Việt Nam abc אבג def 你好世界 ☃"))));
                var size = new Size(400, 300);
                for (int i = 0; i < 50; i++) window.Draw(size, 1);
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++) window.Draw(size, 1);
                return (GC.GetAllocatedBytesForCurrentThread() - before) / 1000;
            }
            long cached = Run(true), full = Run(false);
            t.Assert.Equal(cached, 0L, "frame không đổi (phát lại cache): 0 B");
            t.Notes.Add($"Window.Draw chữ tĩnh: {cached} B/frame (cache), {full} B/frame (render lại toàn bộ, cache tắt)");
        });

        suite.Add("core/text/steady-state-allocates-nothing", t =>
        {
            var ts = TextShapingTests.Make(true);
            if (ts is null) { t.Notes.Add("SKIP: không có segoeui.ttf"); return; }
            var st = TextStyle.Default;
            string[] texts = ["The quick brown fox jumps over the lazy dog", "Việt Nam Tiếng Việt", "abc אבג def", "你好世界，这是一个测试 ☃ end"];
            var lines = new List<TextLine>();
            void Frame()
            {
                foreach (var s in texts)
                {
                    ts.Wrap(s, st, 120, lines);
                    ts.Measure(s, st);
                    ts.Shape(s, 0, s.Length, st, true);
                    ts.FitIndex(s, 0, s.Length, 80, st);
                    ts.IndexAtX(s, 0, s.Length, 50, st);
                    ts.CaretX(s, 0, s.Length, s.Length / 2, st);
                }
            }
            for (int i = 0; i < 50; i++) Frame();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) Frame();
            long perFrame = (GC.GetAllocatedBytesForCurrentThread() - before) / 1000;
            t.Assert.Equal(perFrame, 0L, "đo/ngắt dòng/định hình/hit-test chữ tĩnh: 0 B mỗi frame");
            t.Notes.Add($"chữ tĩnh: {perFrame} B/frame; cache wrap {ts.WrapCacheCount}, trang dự phòng {ts.FallbackPageCount}, font nạp {ts.ResidentFontCount}/{1 + ts.FallbackCount}");
        });
    }
}
