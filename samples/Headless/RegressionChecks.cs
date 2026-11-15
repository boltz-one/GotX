using GotX;
using GotX.Raster;

/// <summary>Phần 2b: hồi quy – effect, vòng đời entity, ô nhập, cache class, atlas glyph, state qua các frame.</summary>
static class RegressionChecks
{
    public static void Run(Checker c)
    {
        double rgStartTime = c.Now;
        var rgApp = new App { MaxEffectsPerFlush = 1000 };

        // Hai entity Notify lẫn nhau: Flush phải dừng bằng exception thay vì treo
        var rgA = rgApp.New(new RegressionModel());
        var rgB = rgApp.New(new RegressionModel());
        var rgSubA = rgApp.Observe(rgA, () => rgB.Notify());
        var rgSubB = rgApp.Observe(rgB, () => rgA.Notify());
        bool rgThrew = false;
        rgA.Notify();
        try { rgApp.Flush(); } catch (InvalidOperationException) { rgThrew = true; }
        c.Check("vòng lặp effect bị chặn bằng exception", rgThrew);
        rgSubA.Dispose(); rgSubB.Dispose(); rgSubB.Dispose(); // Dispose hai lần phải an toàn

        // Huỷ đăng ký ngay trong callback không được làm sót observer đứng sau
        var rgC = rgApp.New(new RegressionModel());
        int rgSecond = 0, rgThird = 0;
        Subscription? rgSelf = null;
        rgSelf = rgApp.Observe(rgC, () => rgSelf!.Dispose());
        rgApp.Observe(rgC, () => rgSecond++);
        rgApp.Observe(rgC, () => rgThird++);
        rgC.Notify(); rgApp.Flush();
        rgC.Notify(); rgApp.Flush();
        c.Check($"Dispose trong callback không làm sót observer ({rgSecond}, {rgThird})", rgSecond == 2 && rgThird == 2);

        // Release: gỡ observer, Notify sau đó vô hại
        rgApp.Release(rgC);
        rgC.Notify(); rgApp.Flush();
        c.Check("Release gỡ entity và observer của nó", !rgC.IsAttached && rgSecond == 2 && rgThird == 2);

        // Backspace: từng grapheme, qua cả dấu xuống dòng
        var rgInput = rgApp.New(new GotX.Widgets.TextInput());
        rgInput.Text = "ab\ncd";
        var rgSteps = new List<string>();
        for (int i = 0; i < 4; i++) { rgInput.HandleKey(Key.Backspace, Modifiers.None); rgSteps.Add(rgInput.Text); }
        c.Check("Backspace lùi đúng qua '\\n'", rgSteps.SequenceEqual(["ab\nc", "ab\n", "ab", "a"]));

        // Cache class có giới hạn: chạm ngưỡng thì xoá, kết quả biên dịch vẫn đúng
        int rgLimit = Tailwind.MaxCachedClassLists;
        Tailwind.MaxCachedClassLists = 8;
        for (int i = 0; i < 50; i++) Tailwind.Get($"w-[{i}px]");
        c.Check("cache class bị giới hạn vẫn biên dịch đúng", Checker.R("w-[49px]").Width == Length.Px(49) && Checker.R("p-4").Padding.Top == Length.Rems(1));
        Tailwind.MaxCachedClassLists = rgLimit;

        // Atlas glyph vượt ngân sách: xoá ở đầu frame, không phát lại scene cũ, frame sau cache chạy lại
        var rgWin = new Window(new App(), c.Text) { Clock = () => c.Now };
        rgWin.Root = rgWin.App.New(new DemoView());
        var rgSize = new Size(1100, 720);
        var rgCanvas = new Canvas();
        rgCanvas.Resize(1100, 720);
        rgWin.Draw(rgSize, 1); rgWin.Draw(rgSize, 1);
        int rgGen = c.Text.Atlas.Generation;
        long rgBudget = c.Text.Atlas.MaxBytes;
        c.Text.Atlas.MaxBytes = -1;
        SoftwareRenderer.Render(rgWin.Draw(rgSize, 1), rgCanvas);
        c.Check("atlas vượt ngân sách → xoá, không phát lại", c.Text.Atlas.Generation == rgGen + 1 && rgWin.LastFrameReplayedViews == 0);
        c.Text.Atlas.MaxBytes = rgBudget;
        SoftwareRenderer.Render(rgWin.Draw(rgSize, 1), rgCanvas);
        c.Check("sau khi xoá atlas: cache view hoạt động lại",
            c.Text.Atlas.Generation == rgGen + 1 && rgWin.LastFrameReplayedViews > 0 && c.Text.Atlas.UsedBytes > 0);

        // Subscription bị callback đứng trước huỷ: không được gọi nữa ngay trong lượt Flush đó
        var rgD = rgApp.New(new RegressionModel());
        int rgLate = 0;
        Subscription? rgVictim = null;
        rgApp.Observe(rgD, () => rgVictim!.Dispose());
        rgVictim = rgApp.Observe(rgD, () => rgLate++);
        rgD.Notify(); rgApp.Flush();
        c.Check($"callback đã bị huỷ không được gọi thêm ({rgLate})", rgLate == 0);

        // Clipboard: Ctrl+C / Ctrl+V / Ctrl+X trong ô nhập (không có platform → clipboard trong bộ nhớ)
        rgInput.Text = "xin chào";
        rgInput.SelectAll();
        rgInput.HandleKey(Key.C, Modifiers.Control);
        rgInput.HandleKey(Key.Right, Modifiers.None);
        rgInput.HandleKey(Key.V, Modifiers.Control);
        c.Check("Ctrl+C rồi Ctrl+V chèn bản sao", rgInput.Text == "xin chàoxin chào" && rgApp.Clipboard.GetText() == "xin chào");
        rgInput.SelectAll();
        rgInput.HandleKey(Key.X, Modifiers.Control);
        c.Check("Ctrl+X cắt vùng chọn", rgInput.Text == "" && rgApp.Clipboard.GetText() == "xin chàoxin chào");

        // ---- State qua các frame: nút phải, view tạm ẩn, dọn bảng khoá ----
        var rgApp2 = new App();
        var rgHost = rgApp2.New(new HostProbe());
        var rgBlank = rgApp2.New(new BlankView());
        var rgWin2 = new Window(rgApp2, c.Text) { Clock = () => c.Now, Root = rgHost };
        var rgSz = new Size(400, 400);
        void RgFrames(int n) { for (int i = 0; i < n; i++) { c.Now += 0.016; rgWin2.Draw(rgSz, 1); } }
        float RgOffset() => rgWin2.ScrollOffset(Enumerable.Range(0, rgWin2.HitboxCount).First(rgWin2.HitboxIsScroll)).Y;
        void RgPress(Point p, MouseButton down, MouseButton up)
        {
            rgWin2.DispatchInput(new InputEvent(InputKind.MouseDown, p, down));
            rgWin2.DispatchInput(new InputEvent(InputKind.MouseUp, p, up));
        }
        RgFrames(1);
        var rgBoxB = rgWin2.FindBounds("box") ?? throw new Exception("thiếu box");
        var rgPt = new Point(rgBoxB.X + 5, rgBoxB.Y + 5);
        var rgScroll = rgHost.Scroll;

        RgPress(rgPt, MouseButton.Right, MouseButton.Right);
        c.Check("nút phải gọi OnAuxClick, không gọi OnClick", rgScroll is { Aux: 1, Clicks: 0, LastButton: MouseButton.Right });
        RgPress(rgPt, MouseButton.Left, MouseButton.Left);
        c.Check("nút trái gọi OnClick, không gọi OnAuxClick", rgScroll is { Aux: 1, Clicks: 1 });
        RgPress(rgPt, MouseButton.Middle, MouseButton.Right);
        c.Check("nhấn nút giữa, thả nút phải: không tính là click", rgScroll is { Aux: 1, Clicks: 1 });
        RgPress(rgPt, MouseButton.Middle, MouseButton.Middle);
        c.Check("nút giữa gọi OnAuxClick", rgScroll is { Aux: 2, LastButton: MouseButton.Middle });

        var rgArea = rgWin2.HitboxBounds(Enumerable.Range(0, rgWin2.HitboxCount).First(rgWin2.HitboxIsScroll));
        rgWin2.DispatchInput(new InputEvent(InputKind.Scroll, new Point(rgArea.X + 10, rgArea.Y + 10), Delta: new Point(0, 90)));
        RgFrames(40);
        c.Check($"vùng cuộn thử nghiệm tới 90 ({RgOffset()})", Checker.Near(RgOffset(), 90));

        rgWin2.Root = rgBlank; RgFrames(3);
        rgWin2.Root = rgHost; RgFrames(2);
        c.Check($"view tạm ẩn rồi hiện lại vẫn giữ vị trí cuộn ({RgOffset()})", Checker.Near(RgOffset(), 90));

        // 40 frame × 300 khoá mới = 12 000 khoá (ngưỡng dọn 4096). View chứa vùng cuộn được phát lại từ cache,
        // nên khoá "keep" của nó không được dùng trong các frame đó – nó phải sống sót nhờ còn state.
        rgHost.Spam.Count = 300;
        for (int i = 0; i < 40; i++) { rgHost.Spam.Generation++; rgHost.Spam.Notify(); rgApp2.Flush(); RgFrames(1); }
        bool rgReplayed = rgWin2.LastFrameReplayedViews > 0;
        rgScroll.Notify(); rgApp2.Flush(); RgFrames(1); // render lại: .Key("keep") phải nhận đúng số cũ
        c.Check($"dọn bảng khoá không làm mất state của khoá còn dùng ({RgOffset()})", rgReplayed && Checker.Near(RgOffset(), 90));
        c.Now = rgStartTime; // phần demo bên dưới xuất PNG theo đồng hồ: không để các kiểm tra này làm lệch pha animation
    }
}
