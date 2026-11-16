using System.Diagnostics;
using System.Runtime.InteropServices;
using GotX;
using GotX.Platform.Windows;
using GotX.Raster;
using GotX.Test;

/// <summary>
/// Mất thiết bị D3D11: Render bắt DXGI_ERROR_DEVICE_REMOVED/RESET ở Present và ResizeBuffers, dựng lại thiết bị + swap chain + shader + buffer + atlas
/// (thật, bằng WARP) rồi vẽ lại đúng frame; quá <see cref="D3D11Renderer.MaxConsecutiveFailures"/> lần liên tiếp thì <c>Failed</c> (cửa sổ chuyển sang CPU).
/// Phần giả lập duy nhất là HRESULT báo lỗi (test seam InjectHr/InjectCount): driver thật không thể bị rút theo yêu cầu.
/// Cũng kiểm tra rò rỉ qua 200 chu kỳ đổi kích thước / DPI / thu nhỏ-khôi phục.
/// </summary>
static class GpuRobustnessTests
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "CreateWindowExW")]
    static extern nint CreateWindowEx(uint ex, string cls, string title, uint style, int x, int y, int w, int h, nint parent, nint menu, nint inst, nint param);
    [DllImport("user32.dll")] static extern int DestroyWindow(nint hwnd);

    const int W = GpuParityScenes.W, H = GpuParityScenes.H;

    sealed class Window_ : IDisposable
    {
        public readonly nint Hwnd = CreateWindowEx(0, "STATIC", "gotx-gpu-test", 0x80000000 /*WS_POPUP*/, 0, 0, W, H, 0, 0, 0, 0);
        public void Dispose() { if (Hwnd != 0) DestroyWindow(Hwnd); }
    }

    static IEnumerable<(string, Scene)> Scenes() => GpuParityScenes.All().Where(s => s.Name is "images" or "gradients" or "shadows");

    static void Frames(D3D11Renderer r, Scene s, int n) { for (int i = 0; i < n; i++) r.Render(s, W, H); }

    public static void Register(TestSuite suite)
    {
        suite.Add("gpu/device-loss/present-removed-recovers-and-renders-same-pixels", t =>
        {
            using var r = GpuKit.TryRenderer(W, H);
            if (r is null) { t.Notes.Add("SKIP: không tạo được D3D11/WARP"); return; }
            foreach (var (name, scene) in Scenes())
            {
                var before = GpuKit.Gpu(r, scene, W, H);
                int rec = r.Recoveries;
                r.InjectHr = D3D.DXGI_ERROR_DEVICE_REMOVED; r.InjectCount = 1;
                bool ok = r.Render(scene, W, H);
                t.Assert.True(ok, $"{name}: Render báo thất bại dù dựng lại được");
                t.Assert.Equal(r.Recoveries, rec + 1, $"{name}: số lần dựng lại");
                var after = new Canvas(); r.ReadPixels(after);
                t.Assert.True(GpuKit.Compare(before, after, 0).Within == 1.0, $"{name}: pixel sau khi dựng lại khác trước ({GpuKit.Compare(before, after, 0)})");
                var next = GpuKit.Gpu(r, scene, W, H); // frame kế tiếp cũng đúng
                t.Assert.True(GpuKit.Compare(before, next, 0).Within == 1.0, $"{name}: frame kế tiếp sau dựng lại sai");
            }
        });

        suite.Add("gpu/device-loss/reset-hung-and-atlas-reuploaded", t =>
        {
            using var r = GpuKit.TryRenderer(480, 300);
            if (r is null) { t.Notes.Add("SKIP: không tạo được D3D11/WARP"); return; }
            t.Open(new GpuParityTests.TextHost(), new Size(480, 300));
            t.Settle();
            var scene = t.Draw();
            var before = GpuKit.Gpu(r, scene, 480, 300);
            foreach (int hr in new[] { D3D.DXGI_ERROR_DEVICE_RESET, D3D.DXGI_ERROR_DEVICE_HUNG, D3D.DXGI_ERROR_DRIVER_INTERNAL_ERROR })
            {
                r.InjectHr = hr; r.InjectCount = 1;
                t.Assert.True(r.Render(scene, 480, 300), $"0x{hr:X8}: Render thất bại");
                var c = new Canvas(); r.ReadPixels(c);
                t.Assert.True(GpuKit.Compare(before, c, 0).Within == 1.0, $"0x{hr:X8}: glyph sau dựng lại khác ({GpuKit.Compare(before, c, 0)})");
            }
            t.Assert.True(r.AtlasSide <= 2048 && r.AtlasSide >= 512, "atlas phải nằm trong [512, 2048]");
        });

        suite.Add("gpu/device-loss/swapchain-present-and-resize-removed", t =>
        {
            using var host = new Window_();
            using var r = D3D11Renderer.TryCreate(host.Hwnd, W, H);
            if (r is null) { t.Notes.Add("SKIP: không tạo được D3D11 trên cửa sổ (swap chain)"); return; }
            var scene = GpuParityScenes.All().First(s => s.Name == "shadows").Scene;
            t.Assert.True(r.Render(scene, W, H), "frame đầu");
            r.InjectHr = D3D.DXGI_ERROR_DEVICE_REMOVED; r.InjectCount = 1; // Present
            t.Assert.True(r.Render(scene, W, H), "Present removed phải dựng lại và vẽ được");
            r.InjectHr = D3D.DXGI_ERROR_DEVICE_RESET; r.InjectCount = 1;   // ResizeBuffers
            t.Assert.True(r.Render(scene, W + 40, H + 20), "ResizeBuffers reset phải dựng lại và vẽ được");
            t.Assert.Equal(r.Recoveries, 2, "số lần dựng lại");
            t.Assert.True(!r.Failed, "không được Failed khi mọi lần dựng lại đều thành công");
            t.Assert.True(r.Render(scene, W, H), "vẽ tiếp sau dựng lại");
        });

        suite.Add("gpu/device-loss/consecutive-failures-fall-back-to-software", t =>
        {
            using var r = GpuKit.TryRenderer(W, H);
            if (r is null) { t.Notes.Add("SKIP: không tạo được D3D11/WARP"); return; }
            var scene = GpuParityScenes.All().First(s => s.Name == "solid-rounded-alpha").Scene;
            r.InjectHr = D3D.DXGI_ERROR_DEVICE_REMOVED; r.InjectCount = 1000;
            int calls = 0;
            while (!r.Failed && calls < 10) { r.Render(scene, W, H); calls++; }
            t.Assert.True(r.Failed, "phải Failed sau các lần dựng lại liên tiếp thất bại");
            t.Assert.True(calls <= D3D11Renderer.MaxConsecutiveFailures, $"bỏ cuộc muộn: {calls} lần Render");
            t.Assert.True(!r.Render(scene, W, H), "đã Failed thì Render phải trả false (người gọi dùng CPU)");
            // Một frame thành công xen giữa phải xoá bộ đếm: lỗi rời rạc không bao giờ dẫn tới Failed.
            using var r2 = GpuKit.TryRenderer(W, H)!;
            for (int i = 0; i < 20; i++) { r2.InjectHr = D3D.DXGI_ERROR_DEVICE_REMOVED; r2.InjectCount = 1; t.Assert.True(r2.Render(scene, W, H), $"lỗi rời rạc #{i}"); }
            t.Assert.True(!r2.Failed, "lỗi rời rạc không được dẫn tới Failed");
        });

        suite.Add("gpu/leak/200-cycles-resize-dpi-minimize-restore", t =>
        {
            using var host = new Window_();
            using var r = D3D11Renderer.TryCreate(host.Hwnd, W, H);
            if (r is null) { t.Notes.Add("SKIP: không tạo được D3D11 trên cửa sổ (swap chain)"); return; }
            var scene = GpuParityScenes.All().First(s => s.Name == "images").Scene;
            var sizes = new (int, int)[] { (W, H), (W + 97, H + 31), (W * 3 / 2, H * 3 / 2) /* DPI 1.5x */, (0, 0) /* thu nhỏ */, (W, H) };
            void Cycle() { foreach (var (w, h) in sizes) r.Render(scene, w, h); }
            for (int i = 0; i < 30; i++) Cycle(); // khởi động: JIT, shader, driver
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            var p = Process.GetCurrentProcess(); p.Refresh();
            long mem0 = p.PrivateMemorySize64; int h0 = p.HandleCount;
            for (int i = 0; i < 200; i++) Cycle();
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            p.Refresh();
            long grow = p.PrivateMemorySize64 - mem0; int hgrow = p.HandleCount - h0;
            t.Notes.Add($"200 chu kỳ: private bytes +{grow / 1024} KB, handle +{hgrow}, atlas {r.AtlasSide}px, GPU ước tính {r.GpuBytes / 1024} KB");
            t.Assert.True(grow < 6L * 1024 * 1024, $"bộ nhớ riêng tăng {grow / 1024} KB sau 200 chu kỳ (rò GPU/CPU?)");
            // Handle của tiến trình dao động theo luồng nền (GC, JIT, threadpool): rò thật (≥ 1 handle/chu kỳ) cho +200 nên ngưỡng 64 vẫn bắt được.
            t.Assert.True(hgrow <= 64, $"handle tăng {hgrow} sau 200 chu kỳ");
        });
    }
}
