using GotX;
using GotX.Raster;

/// <summary>Phần 3: chạy DemoView – cache view, cuộn, hover, dark, responsive – in số liệu từng frame và xuất PNG.</summary>
static class DemoRun
{
    public static void Run(Checker c)
    {
        var sampleUnknown = new List<string>();
        Tailwind.OnUnknownClass = (_, token) => sampleUnknown.Add(token);

        var app = new App();
        var window = new Window(app, c.Text) { Clock = () => c.Now };
        window.Root = app.New(new DemoView());
        var size = new Size(1100, 720);

        void Frame(string label, Size? sz = null)
        {
            c.Now += 0.016;
            window.Draw(sz ?? size, c.Scale);
            Console.WriteLine($"{label,-36} render: {window.LastFrameRenderedViews,2}  phát lại: {window.LastFrameReplayedViews,2}  cấp phát: {window.LastFrameAllocatedBytes,6} B");
        }
        void Save(string name, Size sz)
        {
            var scene = window.Draw(sz, c.Scale);
            var canvas = new Canvas();
            canvas.Resize((int)(sz.Width * c.Scale), (int)(sz.Height * c.Scale));
            SoftwareRenderer.Render(scene, canvas);
            string path = Path.Combine(c.OutDir, name);
            PngWriter.Write(path, canvas);
            Console.WriteLine($"  → {path} ({scene.Count} primitive)");
        }

        Frame("Frame đầu");
        Frame("Frame sau (animation đang chạy)");
        Frame("Frame sau nữa");

        // Cuộn trang 400px: view gallery/hộp thư dời lên – phát lại kèm tịnh tiến hay render lại?
        int page = Enumerable.Range(0, window.HitboxCount).First(window.HitboxIsScroll);
        var pageBox = window.HitboxBounds(page);
        var pagePoint = new Point(pageBox.X + 40, pageBox.Y + 40);
        window.DispatchInput(new InputEvent(InputKind.Scroll, pagePoint, Delta: new Point(0, 420)));
        for (int i = 0; i < 12; i++) Frame(i == 0 ? "Cuộn trang (khung 1)" : i == 11 ? "Cuộn trang (khung 12)" : $"  … khung {i + 1}");
        Save("scrolled.png", size);

        // Rê chuột lên một thẻ gallery: transition nhấc thẻ
        window.DispatchInput(new InputEvent(InputKind.MouseMove, new Point(680, 270)));
        for (int i = 0; i < 8; i++) { c.Now += 0.025; window.Draw(size, c.Scale); }
        Save("hover-card.png", size);

        window.DarkMode = true;
        for (int i = 0; i < 3; i++) Frame("Chế độ tối");
        Save("dark.png", size);

        window.DarkMode = false;
        window.DispatchInput(new InputEvent(InputKind.Scroll, pagePoint, Delta: new Point(0, -2000)));
        for (int i = 0; i < 20; i++) { c.Now += 0.016; window.Draw(new Size(1100, 1900), c.Scale); }
        Save("full.png", new Size(1100, 1900));
        for (int i = 0; i < 3; i++) { c.Now += 0.016; window.Draw(new Size(560, 1000), c.Scale); }
        Save("narrow.png", new Size(560, 1000));

        Console.WriteLine(sampleUnknown.Count == 0 ? "Sample không dùng class nào chưa hỗ trợ." : $"Class chưa hỗ trợ trong sample: {string.Join(", ", sampleUnknown.Distinct())}");
    }
}
