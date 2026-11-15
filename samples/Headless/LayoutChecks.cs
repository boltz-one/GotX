using GotX;
using GotX.Raster;

/// <summary>Phần 2: layout, cuộn, transition, viền, xoay, vùng cắt – đo hộp và pixel trên các view thử trong Probes.cs.</summary>
static class LayoutChecks
{
    public static void Run(Checker c)
    {
        var app0 = new App();
        var win0 = new Window(app0, c.Text) { Clock = () => c.Now, Root = app0.New(new LayoutProbe()) };
        var sz0 = new Size(800, 600);
        win0.Draw(sz0, 1);
        Bounds B(string k) => win0.FindBounds(k) ?? throw new Exception("thiếu " + k);
        // flex-wrap: 5 ô rộng 100 trong khung 300, gap 8 → 2 ô/dòng
        var w0 = B("w0");
        c.Check("wrap: ô 3 xuống dòng 2", Checker.Near(B("w2").X, w0.X) && Checker.Near(B("w2").Y - w0.Y, 28) && Checker.Near(B("w1").X - w0.X, 108));
        c.Check("wrap: ô 5 ở dòng 3", Checker.Near(B("w4").Y - w0.Y, 56));
        var btn = B("fade"); // lấy trước: frame sau view được phát lại từ cache, node không còn trong arena
        // grid-cols-4 gap-4 rộng 400: cột = 88; ô đầu col-span-2 = 192; ô 2 bắt đầu ở 208
        c.Check("grid: col-span-2 = 192px", Checker.Near(B("g0").Width, 192) && Checker.Near(B("g1").X - B("g0").X, 208));
        c.Check("grid: hàng 2", Checker.Near(B("g3").X - B("g0").X, 0) && B("g3").Y > B("g0").Y);
        c.Check("grid-cols-[100px_1fr]: cột cố định", Checker.Near(B("t0").Width, 100) && Checker.Near(B("t1").Width, 300));
        c.Check("absolute inset-0 phủ kín cha", B("abs") == B("absParent"));

        // Cuộn: 10 dòng × 30px trong khung cao 100 → cuộn được 200
        int scrollHb = Enumerable.Range(0, win0.HitboxCount).First(win0.HitboxIsScroll);
        var area = win0.HitboxBounds(scrollHb);
        var mid = new Point(area.X + 10, area.Y + 10);
        win0.DispatchInput(new InputEvent(InputKind.Scroll, mid, Delta: new Point(0, 120)));
        for (int i = 0; i < 40; i++) { c.Now += 0.016; win0.Draw(sz0, 1); }
        c.Check("cuộn mượt tới đích 120", Checker.Near(win0.ScrollOffset(scrollHb).Y, 120));
        win0.DispatchInput(new InputEvent(InputKind.Scroll, mid, Delta: new Point(0, 500)));
        for (int i = 0; i < 30; i++) { c.Now += 0.016; win0.Draw(sz0, 1); }
        c.Check("cuộn bị chặn ở cuối (200)", Checker.Near(win0.ScrollOffset(scrollHb).Y, 200));
        c.Check("hết chuyển động → không xin frame", !win0.WantsAnimationFrame);

        // Transition: nền trắng → đen, 200ms tuyến tính, đo pixel giữa nút
        var center = new Point(btn.X + btn.Width / 2, btn.Y + btn.Height / 2);
        uint Pixel()
        {
            var c = new Canvas();
            c.Resize((int)sz0.Width, (int)sz0.Height);
            SoftwareRenderer.Render(win0.Draw(sz0, 1), c);
            return c.Pixels[(int)center.Y * c.Width + (int)center.X];
        }
        win0.DispatchInput(new InputEvent(InputKind.MouseMove, center));
        uint p0 = Pixel(); c.Now += 0.1; uint p1 = Pixel(); c.Now += 0.15; uint p2 = Pixel();
        c.Check($"transition: đầu trắng ({p0 & 0xFF})", (p0 & 0xFF) > 240);
        c.Check($"transition: giữa xám ({p1 & 0xFF})", (p1 & 0xFF) is > 100 and < 160);
        c.Check($"transition: cuối đen ({p2 & 0xFF})", (p2 & 0xFF) < 10);

        // ---- Viền từng cạnh và xoay: đo pixel ----
        var probe2 = app0.New(new EffectProbe());
        win0.Root = probe2;
        c.Now += 1;
        var canvas2 = new Canvas();
        canvas2.Resize((int)sz0.Width, (int)sz0.Height);
        SoftwareRenderer.Render(win0.Draw(sz0, 1), canvas2);
        uint Px(float x, float y) => canvas2.Pixels[(int)y * canvas2.Width + (int)x];
        static (int r, int g, int b) Rgb(uint p) => ((int)(p >> 16) & 255, (int)(p >> 8) & 255, (int)p & 255);
        var callout = B("callout");
        var leftEdge = Rgb(Px(callout.X + 2, callout.Y + callout.Height / 2));
        var topEdge = Rgb(Px(callout.X + callout.Width / 2, callout.Y + 1));
        c.Check($"border-l-4 đỏ ở cạnh trái {leftEdge}", leftEdge.r > 200 && leftEdge.g < 80);
        c.Check($"cạnh trên không có viền {topEdge}", topEdge.r > 240 && topEdge.g > 240);
        var bar = B("bar"); // thanh 60×8 xoay 90° → thành thanh dọc
        float bcx = bar.X + bar.Width / 2, bcy = bar.Y + bar.Height / 2;
        c.Check("rotate-90: phía trên tâm là đen", Rgb(Px(bcx, bcy - 20)).r < 30);
        c.Check("rotate-90: bên phải tâm là trắng", Rgb(Px(bcx + 20, bcy)).r > 240);
        // Hit-test theo hình đã xoay: góc của hộp bao (ngoài hình thoi) không bấm được
        var dia = B("diamond");
        float dcx = dia.X + dia.Width / 2, dcy = dia.Y + dia.Height / 2;
        void Click(float x, float y)
        {
            win0.DispatchInput(new InputEvent(InputKind.MouseDown, new Point(x, y)));
            win0.DispatchInput(new InputEvent(InputKind.MouseUp, new Point(x, y)));
        }
        Click(dcx - dia.Width / 2 + 3, dcy - dia.Height / 2 + 3); // góc trên-trái của hộp chưa xoay
        Click(dcx, dcy);
        c.Check($"rotate-45: chỉ cú bấm trong hình thoi được nhận ({probe2.Clicks})", probe2.Clicks == 1);
        // animate-spin: sau 0.25s quay 90°
        var spin = B("spin");
        c.Now = Math.Floor(c.Now) + 0.25;
        SoftwareRenderer.Render(win0.Draw(sz0, 1), canvas2);
        float scx = spin.X + spin.Width / 2, scy = spin.Y + spin.Height / 2;
        PngWriter.Write(Path.Combine(c.OutDir, "probe.png"), canvas2);
        c.Check("animate-spin: sau 0.25s cạnh màu (trên) đã quay sang phải", Rgb(Px(scx + spin.Width / 2 - 2, scy)).r < 100 && Rgb(Px(scx, scy - spin.Height / 2 + 2)).r > 200);

        // ---- Kiểu viền, vùng cắt theo hình, skew, origin, outline: đo pixel ----
        win0.Root = app0.New(new ShapeProbe());
        SoftwareRenderer.Render(win0.Draw(sz0, 1), canvas2);
        PngWriter.Write(Path.Combine(c.OutDir, "probe2.png"), canvas2);
        int Transitions(Bounds b, float y)
        {
            int n = 0; bool prev = false;
            for (float x = b.X + 1; x < b.Right - 1; x++) { bool dark = Rgb(Px(x, y)).r < 128; if (x > b.X + 1 && dark != prev) n++; prev = dark; }
            return n;
        }
        var dashed = B("dashed"); var dotted = B("dotted"); var solidB = B("solid");
        c.Check($"border-dashed: cạnh trên đứt quãng ({Transitions(dashed, dashed.Y + 1)} lần đổi)", Transitions(dashed, dashed.Y + 1) >= 6);
        c.Check($"border-dotted: cạnh trên thành chấm ({Transitions(dotted, dotted.Y + 1)} lần đổi)", Transitions(dotted, dotted.Y + 1) >= 10);
        c.Check("border solid: cạnh trên liền", Transitions(solidB, solidB.Y + 1) == 0);
        var round = B("roundclip");
        c.Check("overflow-hidden + rounded-full: góc không bị con tràn ra", Rgb(Px(round.X + 3, round.Y + 3)).r > 240 && Rgb(Px(round.X + 3, round.Y + 3)).g > 240);
        c.Check("overflow-hidden + rounded-full: tâm là màu của con", Rgb(Px(round.X + round.Width / 2, round.Y + round.Height / 2)) is { r: > 200, g: < 80 });
        var rclip = B("rotclip");
        c.Check("overflow-hidden khi xoay 45°: góc hộp bao không có màu con", Rgb(Px(rclip.X + 4, rclip.Y + 4)).g > 240);
        var skew = B("skew"); // skew-x-[30deg]: đáy lệch sang phải, đỉnh lệch sang trái
        c.Check("skew-x: đáy vượt quá mép phải", Rgb(Px(skew.Right + 6, skew.Bottom - 2)).r < 30);
        c.Check("skew-x: đỉnh lùi khỏi mép phải", Rgb(Px(skew.Right - 6, skew.Y + 2)).r > 240);
        var og = B("origin"); // origin-top-left rotate-90: thanh quay xuống dưới, nằm bên trái gốc
        c.Check("origin-top-left rotate-90: thanh nằm dưới-trái gốc", Rgb(Px(og.X - 8, og.Y + 40)).r < 30 && Rgb(Px(og.X + 40, og.Y + 8)).r > 240);
        var outl = B("outline"); // outline-2 outline-offset-2: vòng đỏ cách 2px, dày 2px
        c.Check("outline: vòng ngoài đỏ, khe offset trắng", Rgb(Px(outl.X - 3, outl.Y + outl.Height / 2)) is { r: > 200, g: < 80 }
              && Rgb(Px(outl.X - 1, outl.Y + outl.Height / 2)).g > 240);
    }
}
