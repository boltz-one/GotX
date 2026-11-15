using GotX;
using GotX.Test;

/// <summary>So ảnh chuẩn, giải thích pixel, ảnh khoanh vùng và file bằng chứng khi có lỗi.</summary>
static class VisualTests
{
    /// <summary>Các hộp màu không có chữ: ảnh không phụ thuộc font của máy nên dùng làm ảnh chuẩn lưu trong repo được.</summary>
    sealed class Boxes : View
    {
        public string Accent = "bg-sky-500";
        public override Div Render(Window w) => Fx.Page(w)
            .Child(w.Div().Key("card").Class("w-40 p-3 flex flex-col gap-2 rounded-lg border border-slate-300 bg-slate-50")
                .Child(Fx.Box(w, "head", "h-4 rounded bg-slate-300"))
                .Child(Fx.Box(w, "accent", "h-6 rounded " + Accent)))
            .Child(Fx.Box(w, "pill", "w-24 h-6 rounded-full bg-emerald-500"));
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("visual/snapshot-committed-baseline", t =>
        {
            // Ảnh chuẩn thật nằm ở tests/GotX.Tests/__snapshots__/boxes.png.
            t.Open(new Boxes(), new Size(240, 200));
            t.Snapshot("boxes");
        });

        suite.Add("visual/snapshot-lifecycle", t =>
        {
            string snaps = Kit.TempDir("snapshots"), outDir = Kit.TempDir("snapshots-out");
            var created = Kit.Probe(p => { p.Open(new Boxes(), new Size(240, 200)); p.Snapshot("card"); }, outDir, snaps);
            t.Assert.True(created.Passed && created.Notes.Exists(n => n.StartsWith("snapshot created")) && File.Exists(Path.Combine(snaps, "card.png")), "thiếu ảnh chuẩn → tạo mới và ghi chú 'created'");
            var same = Kit.Probe(p => { p.Open(new Boxes(), new Size(240, 200)); p.Snapshot("card"); }, outDir, snaps);
            t.Assert.True(same.Passed && same.Notes.Count == 0, "ảnh giống ảnh chuẩn → đạt, không ghi chú");

            var changed = Kit.Probe(p => { p.Open(new Boxes { Accent = "bg-rose-500" }, new Size(240, 200)); p.Snapshot("card"); }, outDir, snaps);
            var f = Kit.ExpectOneFailure(t, changed, "đổi màu một hộp", "Snapshot \"card\"", "pixel khác ảnh chuẩn", "trong 1 vùng");
            if (f is null) return;
            t.Assert.True(f.Details.Count == 1 && f.Details[0].Contains("div#accent") && f.Details[0].Contains("[37,61 134×24]"), $"vùng khác được quy về đúng phần tử: {string.Join(" | ", f.Details)}");
            t.Assert.True(f.Artifacts.Count == 3 && f.Artifacts[0].EndsWith("card.diff.png") && f.Artifacts[1].EndsWith("card.actual.png") && f.Artifacts.TrueForAll(File.Exists), "ghi diff + actual, kèm đường dẫn ảnh chuẩn");
            // Ảnh diff: pixel khác tô đỏ tươi, phần còn lại là ảnh gốc làm mờ.
            var diff = PngDecoder.Decode(File.ReadAllBytes(f.Artifacts[0]), out int dw, out _);
            int at = (70 * dw + 100) * 4, corner = (5 * dw + 5) * 4;
            t.Assert.True(diff[at] == 255 && diff[at + 1] == 0 && diff[at + 2] == 200, "pixel khác được tô đỏ tươi trong ảnh diff");
            t.Assert.True(diff[corner] == diff[corner + 1] && diff[corner] > 200, "pixel giống được làm mờ về xám nhạt");

            var tolerant = Kit.Probe(p => { p.Open(new Boxes { Accent = "bg-rose-500" }, new Size(240, 200)); p.Snapshot("card", tolerance: 255); }, outDir, snaps);
            t.Assert.True(tolerant.Passed, "dung sai kênh đủ lớn thì coi như giống");
            var otherSize = Kit.Probe(p => { p.Open(new Boxes(), new Size(260, 200)); p.Snapshot("card"); }, outDir, snaps);
            Kit.ExpectOneFailure(t, otherSize, "khác kích thước", "kích thước ảnh 260×200 khác ảnh chuẩn 240×200");
            var updated = Kit.Probe(p => { p.Open(new Boxes { Accent = "bg-rose-500" }, new Size(240, 200)); p.Snapshot("card"); }, outDir, snaps, updateSnapshots: true);
            t.Assert.True(updated.Passed && updated.Notes.Exists(n => n.StartsWith("snapshot updated")), "--update-snapshots ghi đè ảnh chuẩn");
            var after = Kit.Probe(p => { p.Open(new Boxes { Accent = "bg-rose-500" }, new Size(240, 200)); p.Snapshot("card"); }, outDir, snaps);
            t.Assert.True(after.Passed, "sau khi cập nhật, ảnh mới là ảnh chuẩn");
        });

        suite.Add("visual/explain-pixel", t =>
        {
            t.Open(new Boxes(), new Size(240, 200));
            var e = t.Explain(Fmt.Center(t.Box("accent")));
            t.Assert.Equal(e.Layers.Count, 3, "ba lớp phủ: nền trang, nền thẻ, hộp accent\n" + e);
            if (e.Layers.Count != 3) return;
            t.Assert.True(e.Layers[0].Path!.StartsWith("Boxes > div[1] .size-full") && e.Layers[1].Path!.Contains("div#card") && e.Layers[2].Path!.Contains("div#accent"), "mỗi lớp mang đường dẫn node sở hữu, theo thứ tự vẽ\n" + e);
            t.Assert.True(Pixels.Delta(e.Final, e.Layers[2].Color) <= 1 && e.Layers[2].Coverage == 1, "màu cuối là màu lớp trên cùng");
            t.Assert.True(e.ToString().Contains("3 lớp phủ") && e.ToString().Contains("Quad"), "bản in cho agent đọc");
            // Trên đường viền của thẻ: lớp của thẻ được ghi là viền, với màu viền.
            var card = t.Box("card");
            var border = t.Explain(new Point(card.X + 60, card.Y + 0.5f));
            t.Assert.True(border.Layers.Count == 2 && border.Layers[1].Note.Contains("viền") && Pixels.Delta(border.Final, border.Layers[1].Color) <= 1, "pixel trên viền: nhận ra là viền và đúng màu\n" + border);
            // Trong góc bo: hộp bao chứa pixel nhưng hình bo góc thì không → không tính là phủ.
            var corner = t.Explain(new Point(card.X + 0.5f, card.Y + 0.5f));
            t.Assert.Equal(corner.Layers.Count, 1, "ngoài cung bo góc chỉ còn nền trang\n" + corner);
            // Chữ: lớp Glyph với độ phủ lấy từ mask.
            t.Open(new CleanView(), new Size(640, 480));
            var ink = FindInk(t, t.Get("save").Screen);
            var glyph = t.Explain(ink);
            t.Assert.True(glyph.Layers.Count >= 3 && glyph.Layers[^1] is { Kind: PrimitiveKind.Glyph, Coverage: > 0.5f } top && top.Path!.EndsWith("text \"Lưu\""), "pixel nằm trên nét chữ → lớp trên cùng là Glyph của node chữ\n" + glyph);
        });

        suite.Add("visual/annotated-screenshot", t =>
        {
            string dir = Kit.TempDir("annotate");
            var inner = Kit.Probe(p =>
            {
                p.Open(new Boxes(), new Size(240, 200));
                p.Save("plain");
                p.Save("marked", p.Get("pill"));
                p.Assert.Size("pill", 100, 24);
            }, dir);
            string plain = Path.Combine(dir, "probe.plain.png"), marked = Path.Combine(dir, "probe.marked.png");
            t.Assert.True(File.Exists(plain) && File.Exists(marked), "Save ghi PNG vào thư mục ra, tên có tiền tố test");
            var a = PngDecoder.Decode(File.ReadAllBytes(plain), out int w, out int h);
            var b = PngDecoder.Decode(File.ReadAllBytes(marked), out _, out _);
            t.Assert.Equal((w, h), (240, 200), "kích thước ảnh");
            var pill = inner.Get("pill").Screen;
            int outside = ((int)(pill.Y - 1) * w + (int)(pill.X + 20)) * 4, middle = ((int)(pill.Y + 12) * w + (int)(pill.X + 40)) * 4;
            t.Assert.True(b[outside] == 0xE1 && b[outside + 1] == 0x1D && b[outside + 2] == 0x48 && a[outside] == 255, "khung đỏ vẽ sát ngoài mép hộp");
            t.Assert.True(a[middle] == b[middle] && a[middle + 1] == b[middle + 1], "bên trong hộp không bị vẽ đè");
            // Lỗi của test tự kèm ảnh khoanh vùng + bản in cây có đánh dấu.
            var f = Kit.ExpectOneFailure(t, inner, "lỗi kèm bằng chứng", "Size: #pill");
            if (f is null) return;
            t.Assert.True(f.Artifacts.Count == 2 && f.Artifacts[0].EndsWith("probe.fail1.png") && f.Artifacts[1].EndsWith("probe.fail1.tree.txt"), "ảnh + cây");
            var shot = PngDecoder.Decode(File.ReadAllBytes(f.Artifacts[0]), out _, out _);
            t.Assert.True(shot[outside] == 0xE1, "ảnh lỗi khoanh đúng phần tử");
            t.Assert.True(File.ReadAllText(f.Artifacts[1]).Contains(">>    div#pill"), "bản in cây đánh dấu node lỗi");
        });
    }

    /// <summary>Tìm một pixel tối (nét chữ trắng trên nền tối thì ngược lại: pixel sáng) bên trong hộp – điểm chắc chắn nằm trên glyph.</summary>
    static Point FindInk(UiTest t, Bounds box)
    {
        for (float y = box.Y + 4; y < box.Bottom - 4; y++)
            for (float x = box.X + 4; x < box.Right - 4; x++)
                if (t.PixelAt(new Point(x, y)) is { R: > 0.9f, G: > 0.9f, B: > 0.9f }) return new Point(x, y);
        throw new InvalidOperationException("không tìm thấy pixel chữ trong nút");
    }
}
