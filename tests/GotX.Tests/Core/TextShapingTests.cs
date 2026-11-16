using GotX;
using GotX.Test;
using GotX.Text;

/// <summary>
/// Định hình chữ: font dự phòng theo codepoint, .notdef, dấu kết hợp (NFC = NFD), ligature + cụm (caret).
/// Dùng font Windows thật; thiếu font thì ghi chú SKIP rõ ràng (không báo đạt giả).
/// </summary>
static class TextShapingTests
{
    static readonly string Fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

    /// <summary>Segoe UI (+ Segoe UI Symbol / Emoji làm dự phòng nếu fallback); null nếu máy không có.</summary>
    internal static TextSystem? Make(bool fallback)
    {
        var ts = new TextSystem();
        if (!ts.TryAddFont(Path.Combine(Fonts, "segoeui.ttf"), FontWeight.Normal)) return null;
        if (fallback)
        {
            ts.TryAddFallbackFont(Path.Combine(Fonts, "seguisym.ttf"));
            ts.TryAddFallbackFont(Path.Combine(Fonts, "seguiemj.ttf"));
        }
        return ts;
    }

    /// <summary>Hộp mực (px) thật của chuỗi: bitmap glyph trong atlas đặt theo kết quả Shape, như lúc vẽ.</summary>
    internal static (float X0, float Y0, float X1, float Y1) Ink(TextSystem ts, string s, float size = 16)
    {
        var style = TextStyle.Default with { FontSize = size };
        int n = ts.Shape(s, 0, s.Length, style, true);
        float dx = 0, x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        for (int i = 0; i < n; i++)
        {
            var g = ts.Glyphs[i];
            float k = size / g.Font.UnitsPerEm;
            dx += g.Pre * k;
            var mask = ts.Atlas[ts.Atlas.Face(g.Font, size).Get(g.Id)];
            if (mask.Width > 0)
            {
                float left = MathF.Round(dx + g.XOff * k) + mask.Left, top = -MathF.Round(g.YOff * k) + mask.Top;
                x0 = MathF.Min(x0, left); x1 = MathF.Max(x1, left + mask.Width);
                y0 = MathF.Min(y0, top); y1 = MathF.Max(y1, top + mask.Height);
            }
            dx += g.Advance * k;
        }
        return (x0, y0, x1, y1);
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("core/text/fallback-font-per-codepoint", t =>
        {
            var ts = Make(true);
            if (ts is null) { t.Notes.Add("SKIP: không có segoeui.ttf"); return; }
            var style = TextStyle.Default;
            var primary = ts.FontFor(style);
            int cp = new[] { 0x2603, 0x2605, 0x2764, 0x2713, 0x2708, 0x265B }.FirstOrDefault(c => primary.GlyphIndex(c) == 0 && ts.CanRender(c, style));
            if (cp == 0) { t.Notes.Add("SKIP: không tìm được ký hiệu mà Segoe UI thiếu nhưng Segoe UI Symbol/Emoji có"); return; }
            string sym = char.ConvertFromUtf32(cp), s = "a" + sym + "b";
            int n = ts.Shape(s, 0, s.Length, style, false);
            t.Assert.Equal(n, 3, "3 glyph: a, ký hiệu, b");
            t.Assert.True(ts.Glyphs[0].Font == primary && ts.Glyphs[2].Font == primary, "chữ Latin vẫn dùng font chính");
            t.Assert.True(ts.Glyphs[1].Font != primary && ts.Glyphs[1].Font.Id >= TextSystem.FallbackIdBase && ts.Glyphs[1].Id != 0, "ký hiệu lấy từ font dự phòng (id riêng cho khoá atlas)");
            t.Assert.Near(ts.Measure(s, style), ts.Measure("a", style) + ts.Measure(sym, style) + ts.Measure("b", style), 0.01f, "bề rộng = tổng từng run (kerning chỉ trong run)");
            var ink = Ink(ts, sym);
            t.Assert.True(ink.X1 > ink.X0 && ink.Y1 > ink.Y0, "glyph dự phòng có bitmap thật");
            var a = ts.Atlas.Face(primary, 16); var b = ts.Atlas.Face(ts.Glyphs[1].Font, 16);
            t.Assert.True(!ReferenceEquals(a, b), "mặt chữ atlas tách theo font (khoá có id font dự phòng)");
        });

        suite.Add("core/text/notdef-tofu-without-fallback", t =>
        {
            var ts = Make(false);
            if (ts is null) { t.Notes.Add("SKIP: không có segoeui.ttf"); return; }
            var style = TextStyle.Default;
            var primary = ts.FontFor(style);
            string s = "☃";
            if (primary.GlyphIndex(0x2603) != 0) { s = "\U0001FAE0"; }
            t.Assert.True(primary.GlyphIndex(char.ConvertToUtf32(s, 0)) == 0, "ký tự thử không có trong font chính");
            int n = ts.Shape(s, 0, s.Length, style, false);
            t.Assert.True(n == 1 && ts.Glyphs[0].Id == 0 && ts.Glyphs[0].Font == primary, "không có dự phòng: vẽ .notdef của font chính");
            t.Assert.Near(ts.Measure(s, style), primary.AdvanceWidth(0) * 16f / primary.UnitsPerEm, 0.01f, "bề rộng = advance .notdef");
            t.Assert.True(!ts.CanRender(char.ConvertToUtf32(s, 0), style), "CanRender báo thiếu glyph");
        });

        suite.Add("core/text/fonts-load-lazily", t =>
        {
            var ts = Make(true);
            if (ts is null) { t.Notes.Add("SKIP: không có segoeui.ttf"); return; }
            t.Assert.Equal(ts.ResidentFontCount, 0, "đăng ký font chưa nạp file vào RAM");
            ts.Measure("hello", TextStyle.Default);
            t.Assert.Equal(ts.ResidentFontCount, 1, "đo chữ Latin chỉ nạp font chính, không nạp dự phòng");
            t.Notes.Add($"font nạp {ts.ResidentFontCount}/{1 + ts.FallbackCount}, trang cache dự phòng {ts.FallbackPageCount}");
        });

        suite.Add("core/text/vietnamese-nfc-equals-nfd", t =>
        {
            var ts = Make(false);
            if (ts is null) { t.Notes.Add("SKIP: không có segoeui.ttf"); return; }
            (string Nfc, string Nfd)[] pairs =
            [
                ("Việt", "Việt"), ("Tiếng", "Tiếng"), ("ở", "ở"), ("Ố", "Ố"), ("ặ", "ặ"),
            ];
            foreach (var (nfc, nfd) in pairs)
            {
                var a = Ink(ts, nfc); var b = Ink(ts, nfd);
                bool same = MathF.Abs(a.X0 - b.X0) <= 1 && MathF.Abs(a.X1 - b.X1) <= 1 && MathF.Abs(a.Y0 - b.Y0) <= 1 && MathF.Abs(a.Y1 - b.Y1) <= 1;
                t.Assert.True(same, $"'{nfc}': NFC [{a.X0},{a.Y0},{a.X1},{a.Y1}] ≈ NFD [{b.X0},{b.Y0},{b.X1},{b.Y1}] trong 1px");
                t.Assert.Near(ts.Measure(nfc, TextStyle.Default), ts.Measure(nfd, TextStyle.Default), 0.5f, $"'{nfc}': bề rộng NFC = NFD");
            }
        });

        suite.Add("core/text/mark-attaches-by-gpos-when-no-precomposed", t =>
        {
            var ts = Make(false);
            if (ts is null) { t.Notes.Add("SKIP: không có segoeui.ttf"); return; }
            var f = ts.FontFor(TextStyle.Default);
            if (!f.HasMarkPositioning) { t.Notes.Add("SKIP: Segoe UI không có GPOS mark"); return; }
            string s = "x́"; // không có "x" dấu sắc dựng sẵn: dấu phải được đặt bằng neo
            int n = ts.Shape(s, 0, 2, TextStyle.Default, false);
            t.Assert.True(n == 2 && ts.Glyphs[1].IsMark && ts.Glyphs[1].Advance == 0, "dấu là glyph riêng, advance 0");
            var bare = Ink(ts, "x"); var all = Ink(ts, s);
            t.Assert.True(all.X1 <= bare.X1 + 2 && all.Y0 < bare.Y0, $"dấu nằm trên chữ, không chiếm chỗ ngang (x [{bare.X0},{bare.X1}] → [{all.X0},{all.X1}], y0 {bare.Y0} → {all.Y0})");
        });
    }
}
