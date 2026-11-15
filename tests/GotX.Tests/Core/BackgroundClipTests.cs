using GotX;
using GotX.Test;

/// <summary>bg-clip-border / padding / content: nền (màu và gradient) bị cắt theo hộp tương ứng, viền vẫn ở hộp viền và để lộ phần nằm sau.</summary>
static class BackgroundClipTests
{
    static readonly Rgba Behind = Theme.Colors["sky-300"], Black = Rgba.FromHex(0x000000);

    /// <summary>Nền sky-300 phủ đúng hộp phần tử, phần tử đặt ở (24, 24).</summary>
    static View Sample(string classes) => new Fx(w => Fx.Page(w).Child(
        w.Div().Class("w-24 h-8 bg-sky-300").Child(w.Div().Key("box").Class(classes))));

    public static void Register(TestSuite suite)
    {
        suite.Add("core/bg-clip/padding-box-leaves-border-transparent", t =>
        {
            t.Open(_ => Sample("size-full border border-transparent bg-clip-padding bg-black"));
            t.Assert.Pixel(new Point(24.5f, 40), Behind);   // viền trái: thấy nền phía sau
            t.Assert.Pixel(new Point(119.5f, 40), Behind);  // viền phải
            t.Assert.Pixel(new Point(60, 24.5f), Behind);   // viền trên
            t.Assert.Pixel(new Point(60, 55.5f), Behind);   // viền dưới
            t.Assert.Pixel(new Point(25.5f, 40), Black);    // mép trong của hộp đệm
            t.Assert.Pixel(new Point(118.5f, 40), Black);
            t.Assert.Pixel(new Point(60, 40), Black);
        });

        suite.Add("core/bg-clip/border-box-is-default", t =>
        {
            t.Open(_ => Sample("size-full border border-red-600 bg-black"));
            t.Assert.Pixel(new Point(24.5f, 40), Theme.Colors["red-600"]);
            t.Open(_ => Sample("size-full border border-red-600 bg-clip-border bg-black"));
            t.Assert.Pixel(new Point(24.5f, 40), Theme.Colors["red-600"]);
            t.Assert.Pixel(new Point(60, 40), Black);
        });

        suite.Add("core/bg-clip/transparent-border-lets-fill-show-under-it", t =>
        {
            // mặc định (border-box): viền trong suốt không được xoá nền nằm dưới nó
            t.Open(_ => Sample("size-full border border-transparent bg-black"));
            t.Assert.Pixel(new Point(24.5f, 40), Black);
            t.Assert.Pixel(new Point(60, 24.5f), Black);
            // viền mờ 50 % đỏ đè lên nền đen: ≈ nửa đỏ
            t.Open(_ => Sample("size-full border-2 border-red-600/50 bg-black"));
            var edge = t.PixelAt(new Point(24.5f, 40));
            t.Assert.True(edge.R > 0.35f && edge.R < 0.55f && edge.G < 0.1f, $"viền 50 % đè lên nền đen (R={edge.R:0.00})");
        });

        suite.Add("core/bg-clip/content-box-excludes-padding", t =>
        {
            t.Open(_ => Sample("size-full border border-transparent p-3 bg-clip-content bg-black"));
            t.Assert.Pixel(new Point(30, 40), Behind);      // trong padding
            t.Assert.Pixel(new Point(37.5f, 40), Black);    // hộp nội dung bắt đầu ở 24 + 1 + 12 = 37
            t.Assert.Pixel(new Point(60, 40), Black);
        });

        suite.Add("core/bg-clip/inner-radius-is-outer-minus-border", t =>
        {
            // rounded-2xl (16) + border 1 → bán kính nền 15: góc (25.5, 25.5) nằm ngoài cung tròn nên thấy nền phía sau; tâm cạnh thì đen
            t.Open(_ => Sample("size-full rounded-2xl border border-transparent bg-clip-padding bg-black"));
            t.Assert.Pixel(new Point(25.5f, 25.5f), Behind);
            t.Assert.Pixel(new Point(25.5f, 40), Black);
            t.Assert.Pixel(new Point(40, 25.5f), Black);
        });

        suite.Add("core/bg-clip/gradient-is-clipped-too", t =>
        {
            t.Open(_ => Sample("size-full border-4 border-transparent bg-clip-padding bg-gradient-to-r from-black to-black"));
            t.Assert.Pixel(new Point(25, 40), Behind);
            t.Assert.Pixel(new Point(60, 40), Black);
            t.Assert.Pixel(new Point(60, 26), Behind);
        });

        suite.Add("core/bg-clip/border-still-drawn-at-border-box", t =>
        {
            t.Open(_ => Sample("size-full border-2 border-red-600 bg-clip-padding bg-black"));
            t.Assert.Pixel(new Point(24.5f, 40), Theme.Colors["red-600"]);
            t.Assert.Pixel(new Point(60, 40), Black);
        });
    }
}
