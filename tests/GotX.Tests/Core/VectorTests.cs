using GotX;
using GotX.Test;

/// <summary>VectorPath: cú pháp path SVG (cờ cung viết liền, lệnh tương đối), dạng tắt, và pixel thật sự được vẽ ở đúng chỗ.</summary>
static class VectorTests
{
    /// <summary>Vẽ một path vào ô 96×96 (viewBox 24 → mỗi đơn vị 4 px) màu đen trên nền trắng; trả về hàm đo độ đậm tại toạ độ viewBox.</summary>
    static Func<float, float, float> Draw(UiTest t, VectorPath path, string classes = "size-24")
    {
        t.Open(new Fx(w => w.Div().Class("size-full bg-white text-black").Child(w.Path(path).Key("p").Class(classes))), new Size(120, 120));
        var box = t.Box("p");
        float unit = box.Width / 24;
        return (x, y) => 1 - t.PixelAt(new Point(box.X + x * unit, box.Y + y * unit)).R; // 0 = trắng, 1 = mực đen
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("core/vector/absolute-lines", t =>
        {
            var ink = Draw(t, VectorPath.Icon("M4 12 L20 12"));
            t.Assert.True(ink(12, 12) > 0.9f && ink(6, 12) > 0.9f && ink(18, 12) > 0.9f, "nét ngang đi qua giữa");
            t.Assert.True(ink(12, 9) < 0.1f && ink(12, 15) < 0.1f, "ngoài độ dày nét (2 đơn vị) thì trắng");
            t.Assert.True(ink(3.5f, 12) > 0.5f && ink(2, 12) < 0.1f, "đầu nét bo tròn: lấn ra 1 đơn vị, không hơn");
            t.Assert.Equal(t.Get("p").Kind, "path", "Inspect: loại node là path");
        });

        suite.Add("core/vector/relative-commands", t =>
        {
            // Chữ L vẽ bằng lệnh tương đối + h/v, phải trùng với bản toạ độ tuyệt đối.
            var rel = Draw(t, VectorPath.Icon("m6 4v14h12"));
            float[] a = [rel(6, 6), rel(6, 16), rel(10, 18), rel(17, 18), rel(12, 10)];
            var abs = Draw(t, VectorPath.Icon("M6 4L6 18L18 18"));
            float[] b = [abs(6, 6), abs(6, 16), abs(10, 18), abs(17, 18), abs(12, 10)];
            t.Assert.True(a[0] > 0.9f && a[1] > 0.9f && a[2] > 0.9f && a[3] > 0.9f && a[4] < 0.1f, $"nét dọc rồi nét ngang, bên trong trống ({string.Join(" ", a)})");
            for (int i = 0; i < a.Length; i++) t.Assert.Near(a[i], b[i], 0.02f, $"điểm {i}: tương đối = tuyệt đối");
        });

        suite.Add("core/vector/compact-arc-flags", t =>
        {
            // "a2 2 0 012 2": hai cờ viết liền với toạ độ (0, 1, rồi 2 2) – kiểu nén của Lucide. Là góc bo một phần tư đường tròn.
            var compact = Draw(t, VectorPath.Icon("M6 6h8a2 2 0 012 2v10"));
            var spaced = Draw(t, VectorPath.Icon("M6 6h8a2 2 0 0 1 2 2v10"));
            (float X, float Y)[] probes = [(8, 6), (13, 6), (16, 10), (16, 17), (15.4f, 6.6f), (16, 6), (12, 12)];
            var got = new List<string>();
            foreach (var (x, y) in probes)
            {
                t.Assert.Near(compact(x, y), spaced(x, y), 0.02f, $"({x},{y}): cờ viết liền = cờ viết rời");
                got.Add(Fmt.N(compact(x, y)));
            }
            t.Assert.True(compact(8, 6) > 0.9f && compact(16, 17) > 0.9f, "hai đoạn thẳng của hình");
            t.Assert.True(compact(15.4f, 6.6f) > 0.9f, $"cung đi qua điểm giữa góc bo ({string.Join(" ", got)})");
            t.Assert.True(compact(12, 12) < 0.1f, "bên trong góc không có mực");
        });

        suite.Add("core/vector/shorthand-shapes", t =>
        {
            var circle = Draw(t, VectorPath.Icon("circle 12 12 8"));
            t.Assert.True(circle(4, 12) > 0.9f && circle(20, 12) > 0.9f && circle(12, 4) > 0.9f && circle(12, 20) > 0.9f, "circle: bốn điểm trên đường tròn");
            t.Assert.True(circle(12, 12) < 0.1f && circle(1.5f, 12) < 0.1f, "circle: tâm và bên ngoài trống");
            var line = Draw(t, VectorPath.Icon("line 4 4 20 20"));
            t.Assert.True(line(12, 12) > 0.9f && line(6, 6) > 0.9f && line(18, 6) < 0.1f, "line: đường chéo");
            var rect = Draw(t, VectorPath.Icon("rect 4 6 16 12 3"));
            t.Assert.True(rect(12, 6) > 0.9f && rect(4, 12) > 0.9f && rect(20, 12) > 0.9f && rect(12, 18) > 0.9f, "rect: bốn cạnh");
            t.Assert.True(rect(12, 12) < 0.1f && rect(4.2f, 6.2f) < 0.5f, "rect: lòng trống, góc bo (rx=3) không có mực ở đỉnh nhọn");
            var poly = Draw(t, VectorPath.Icon("polyline 4,18 12,6 20,18"));
            t.Assert.True(poly(8, 12) > 0.8f && poly(16, 12) > 0.8f && poly(12, 6) > 0.8f && poly(12, 18) < 0.1f, "polyline: hai cạnh chữ V ngược, đáy hở");
            var filled = Draw(t, VectorPath.FilledIcon("polygon 4,20 12,4 20,20"));
            t.Assert.True(filled(12, 14) > 0.9f && filled(5, 6) < 0.1f, "FilledIcon polygon: tô kín bên trong");
        });

        suite.Add("core/vector/color-size-and-fill", t =>
        {
            // Lấy màu chữ hiện tại; giữ tỉ lệ trong hộp không vuông (căn giữa); object-fill thì kéo giãn.
            var line = VectorPath.Polyline([new(0, 50), new(200, 50)], 200, 100, 4);
            t.Open(new Fx(w => w.Div().Class("size-full flex flex-col gap-2 bg-white text-rose-600")
                .Child(w.Path(VectorPath.FilledIcon("rect 0 0 24 24")).Key("fit").Class("w-24 h-8"))
                .Child(w.Path(VectorPath.FilledIcon("rect 0 0 24 24")).Key("fill").Class("w-24 h-8 object-fill"))
                .Child(w.Path(line).Key("chart").Class("w-[100px] h-[50px] text-sky-600"))), new Size(160, 160));
            var fit = t.Box("fit");
            t.Assert.True(t.PixelAt(Fmt.Center(fit)) is { R: > 0.8f, G: < 0.3f }, "icon lấy màu chữ (text-rose-600)");
            t.Assert.Pixel(new Point(fit.X + 8, fit.Y + 16), Rgba.White, 4); // 24:24 trong hộp 96×32 → hình vuông 32 px ở giữa
            var fill = t.Box("fill");
            t.Assert.True(t.PixelAt(new Point(fill.X + 8, fill.Y + 16)).G < 0.3f, "object-fill: kéo giãn kín hộp");
            var chart = t.Box("chart");
            t.Assert.True(t.PixelAt(new Point(chart.X + 50, chart.Y + 25)).B > 0.7f && t.PixelAt(new Point(chart.X + 50, chart.Y + 25)).R < 0.2f, "Polyline: đường biểu đồ màu riêng");
            t.Assert.Pixel(new Point(chart.X + 50, chart.Y + 10), Rgba.White, 4);
            t.Assert.Size("chart", 100, 50);
        });
    }
}
