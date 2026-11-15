using GotX;
using GotX.Raster;
using GotX.Test;

/// <summary>ImageSource / PNG: ghi rồi đọc lại, object-cover so với object-contain, bo góc cắt ảnh.</summary>
static class ImageTests
{
    /// <summary>Ảnh 80×40: nửa trái đỏ, nửa phải xanh dương, dải 4 px trên cùng màu lục – đủ để biết phần nào của ảnh đang hiện.</summary>
    static byte[] Pattern(out int width, out int height)
    {
        width = 80; height = 40;
        var rgba = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                bool top = y < 4, left = x < 40;
                rgba[i] = (byte)(top ? 0 : left ? 255 : 0);
                rgba[i + 1] = (byte)(top ? 200 : 0);
                rgba[i + 2] = (byte)(top ? 0 : left ? 0 : 255);
                rgba[i + 3] = 255;
            }
        return rgba;
    }

    static ImageSource Image() => ImageSource.FromRgba(80, 40, Pattern(out _, out _));

    static bool Red(Rgba c) => c is { R: > 0.9f, G: < 0.1f, B: < 0.1f };
    static bool Blue(Rgba c) => c is { R: < 0.1f, G: < 0.1f, B: > 0.9f };
    static bool Green(Rgba c) => c is { R: < 0.1f, G: > 0.7f, B: < 0.1f };
    static bool Gray(Rgba c) => MathF.Abs(c.R - c.G) < 0.02f && c.R is > 0.8f and < 0.99f;

    public static void Register(TestSuite suite)
    {
        suite.Add("core/image/png-roundtrip", t =>
        {
            var rgba = Pattern(out int w, out int h);
            // Thêm một vùng bán trong suốt để thử cả kênh alpha.
            for (int i = 0; i < 20; i++) { rgba[(30 * w + i) * 4] = 100; rgba[(30 * w + i) * 4 + 3] = 128; }
            var canvas = new Canvas();
            canvas.Resize(w, h);
            var source = ImageSource.FromRgba(w, h, rgba);
            for (int i = 0; i < w * h; i++) // pixel của ImageSource (BGRA premultiplied) → Canvas
                canvas.Pixels[i] = (uint)(source.Pixels[i * 4] | source.Pixels[i * 4 + 1] << 8 | source.Pixels[i * 4 + 2] << 16 | source.Pixels[i * 4 + 3] << 24);
            using var png = new MemoryStream();
            PngWriter.Write(png, canvas);
            var decoded = PngDecoder.Decode(png.ToArray(), out int dw, out int dh);
            t.Assert.Equal((dw, dh), (w, h), "kích thước sau khi đọc lại");
            int worst = 0;
            for (int i = 0; i < rgba.Length; i++) worst = Math.Max(worst, Math.Abs(decoded[i] - rgba[i]));
            t.Assert.True(worst <= 1, $"PngWriter → PngDecoder trả lại đúng pixel (sai lệch lớn nhất {worst}; ≤ 1 do làm tròn premultiply)");
            var again = ImageSource.FromPng(png.ToArray());
            t.Assert.True(again.Width == w && again.Height == h && again.Id != source.Id && ImageSource.Find(again.Id) == again, "FromPng tạo ảnh mới, tra lại được bằng Id");
            bool threw = false;
            try { ImageSource.FromPng([1, 2, 3, 4, 5, 6, 7, 8, 9]); } catch (NotSupportedException) { threw = true; }
            t.Assert.True(threw, "dữ liệu không phải PNG → NotSupportedException");
        });

        suite.Add("core/image/object-cover-vs-contain", t =>
        {
            var img = Image();
            t.Open(new Fx(w => w.Div().Class("size-full p-4 flex flex-col items-start gap-4 bg-white")
                .Child(w.Image(img).Key("natural"))
                .Child(w.Image(img).Key("cover").Class("size-20"))
                .Child(w.Image(img).Key("contain").Class("size-20 object-contain bg-neutral-200"))
                .Child(w.Image(img).Key("fill").Class("size-20 object-fill"))), new Size(200, 400));
            t.Assert.Size("natural", 80, 40); // mặc định bằng kích thước ảnh
            t.Assert.Equal(t.Get("natural").Kind, "image", "Inspect: loại node là image");
            var n = t.Box("natural");
            t.Assert.True(Red(t.PixelAt(new Point(n.X + 10, n.Y + 20))) && Blue(t.PixelAt(new Point(n.Right - 10, n.Y + 20))) && Green(t.PixelAt(new Point(n.X + 40, n.Y + 1))), "kích thước gốc: trái đỏ, phải xanh, dải lục trên cùng");

            // cover (mặc định): ảnh 2:1 trong hộp vuông → phóng theo chiều cao, cắt hai bên, vẫn chạm đỉnh/đáy.
            var c = t.Box("cover");
            t.Assert.True(Red(t.PixelAt(new Point(c.X + 2, c.Y + 40))) && Blue(t.PixelAt(new Point(c.Right - 2, c.Y + 40))), "cover: kín hộp, ranh đỏ/xanh ở giữa");
            t.Assert.True(Red(t.PixelAt(new Point(c.X + 38, c.Y + 40))) && Blue(t.PixelAt(new Point(c.X + 42, c.Y + 40))), "cover: ranh giới nằm đúng giữa hộp");
            t.Assert.True(Green(t.PixelAt(new Point(c.X + 40, c.Y + 3))) && Red(t.PixelAt(new Point(c.X + 20, c.Bottom - 2))), "cover: dải lục cao gấp đôi (8 px) ở đỉnh, đáy là ảnh");

            // contain: ảnh nằm trọn, thu còn 80×40 ở giữa, trên dưới lộ nền của phần tử.
            var k = t.Box("contain");
            t.Assert.True(Gray(t.PixelAt(new Point(k.X + 40, k.Y + 8))) && Gray(t.PixelAt(new Point(k.X + 40, k.Bottom - 8))), "contain: dải nền phía trên và dưới ảnh");
            t.Assert.True(Red(t.PixelAt(new Point(k.X + 5, k.Y + 40))) && Blue(t.PixelAt(new Point(k.Right - 5, k.Y + 40))) && Green(t.PixelAt(new Point(k.X + 40, k.Y + 21))), "contain: thấy toàn bộ ảnh, không cắt");

            // fill: kéo giãn méo cho kín hộp – cả chiều ngang ảnh đều hiện và dải lục giãn theo chiều dọc.
            var f = t.Box("fill");
            t.Assert.True(Red(t.PixelAt(new Point(f.X + 5, f.Y + 40))) && Blue(t.PixelAt(new Point(f.Right - 5, f.Y + 40))) && Green(t.PixelAt(new Point(f.X + 40, f.Y + 6))) && Red(t.PixelAt(new Point(f.X + 20, f.Y + 10))), "fill: kéo giãn kín hộp");
        });

        suite.Add("core/image/rounded-clips-corners", t =>
        {
            var img = Image();
            t.Open(new Fx(w => w.Div().Class("size-full p-4 flex gap-4 bg-white")
                .Child(w.Image(img).Key("square").Class("size-20"))
                .Child(w.Image(img).Key("round").Class("size-20 rounded-full"))
                .Child(w.Image(img).Key("faded").Class("size-20 opacity-50"))), new Size(320, 120));
            var s = t.Box("square"); var r = t.Box("round");
            t.Assert.True(Green(t.PixelAt(new Point(s.X + 2, s.Y + 2))), "không bo: góc vẫn là ảnh");
            foreach (var corner in new[] { new Point(r.X + 2, r.Y + 2), new Point(r.Right - 3, r.Y + 2), new Point(r.X + 2, r.Bottom - 3), new Point(r.Right - 3, r.Bottom - 3) })
                t.Assert.Pixel(corner, Rgba.White, 2); // rounded-full: bốn góc bị cắt, lộ nền trang
            t.Assert.True(Red(t.PixelAt(new Point(r.X + 20, r.Y + 40))) && Blue(t.PixelAt(new Point(r.Right - 20, r.Y + 40))) && Green(t.PixelAt(new Point(r.X + 40, r.Y + 3))), "bên trong hình tròn vẫn là ảnh");
            bool soft = false; // dọc đường chéo phải có pixel pha giữa ảnh (đỏ) và nền (trắng): mép tròn có anti-alias
            for (float d = 6; d < 18; d++) soft |= t.PixelAt(new Point(r.X + d, r.Y + d)).G is > 0.1f and < 0.9f;
            t.Assert.True(soft, "mép tròn có anti-alias, không lởm chởm");
            var faded = t.PixelAt(new Point(t.Box("faded").X + 10, t.Box("faded").Y + 40));
            t.Assert.True(faded.R > 0.95f && MathF.Abs(faded.G - 0.5f) < 0.05f, $"opacity-50: ảnh pha với nền trắng ({Fmt.C(faded)})");
            Kit.ExpectClean(t, t.Lint(), "trang ảnh");
        });
    }
}
