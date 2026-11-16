using GotX;
using GotX.Widgets;
using GotX.Platform.Windows;
using GotX.Test;

/// <summary>
/// Bộ đếm GPU (draw call, đổi pipeline/texture, byte instance, atlas hit, encode), idle phải miễn phí (không yêu cầu frame → không encode/present),
/// frame ổn định không cấp phát managed, atlas bắt đầu nhỏ và nở đúng (kể cả glyph lớn hơn atlas), buffer có trần.
/// </summary>
static class GpuCountersTests
{
    const int W = GpuParityScenes.W, H = GpuParityScenes.H;

    sealed class BlinkHost : View
    {
        public TextInput Field = null!;
        protected override void OnCreated() => Field = App.New(new TextInput { Text = "abc", BoxClass = "h-8 w-40 px-3 border bg-white text-slate-900", Blink = true, AriaLabel = "field" });
        public override Div Render(Window w) => Fx.Page(w).Child(w.Div().Key("field").Child(w.View(Field)));
    }

    sealed class BigGlyphHost : View
    {
        public override Div Render(Window w) => w.Div().Class("size-full bg-white overflow-hidden").Child(w.Div().Class("text-[700px] text-black leading-none").Child("W"));
    }

    static Scene Named(string name) => GpuParityScenes.All().First(s => s.Name == name).Scene;

    public static void Register(TestSuite suite)
    {
        suite.Add("gpu/counters/draws-switches-bytes-and-atlas-hit-rate", t =>
        {
            using var r = GpuKit.TryRenderer(W, H);
            if (r is null) { t.Notes.Add("SKIP: không tạo được D3D11/WARP"); return; }
            var solid = Named("solid-rounded-alpha");
            r.Render(solid, W, H);
            var g = GpuCounters.Last;
            t.Assert.Equal(g.DrawCalls, 1, "cảnh không ảnh = một lệnh vẽ");
            t.Assert.Equal(g.TextureSwitches, 0, "không đổi texture ảnh");
            t.Assert.True(g.InstanceBytes >= solid.Count * 128 && g.Primitives == solid.Count, $"byte instance {g.InstanceBytes}, primitive {g.Primitives}");
            t.Assert.True(g.EncodeMs > 0, "thời gian encode CPU phải > 0");
            r.Render(Named("images"), W, H); // 6 ảnh xen kẽ hai texture: không được gộp vì sẽ đổi thứ tự vẽ
            t.Assert.Equal(GpuCounters.Last.DrawCalls, 6, "mỗi đoạn ảnh cùng texture một lệnh vẽ");
            t.Assert.Equal(GpuCounters.Last.TextureSwitches, 6, "đổi texture mỗi đoạn");
            long frames = GpuCounters.Frames, draws = GpuCounters.DrawCalls;
            r.Render(solid, W, H);
            t.Assert.Equal(GpuCounters.Frames, frames + 1, "tổng frame");
            t.Assert.Equal(GpuCounters.DrawCalls, draws + 1, "tổng draw call cộng dồn");
        });

        suite.Add("gpu/counters/atlas-misses-then-all-hits-and-starts-small", t =>
        {
            t.Open(new GpuParityTests.TextHost(), new Size(480, 300));
            t.Settle();
            var scene = t.Draw();
            using var r = GpuKit.TryRenderer(480, 300);
            if (r is null) { t.Notes.Add("SKIP: không tạo được D3D11/WARP"); return; }
            r.Render(scene, 480, 300);
            t.Assert.True(GpuCounters.Last.AtlasMisses > 0, "frame đầu phải nạp glyph (miss)");
            r.Render(scene, 480, 300);
            t.Assert.Equal(GpuCounters.Last.AtlasMisses, 0, "frame thứ hai không nạp lại");
            t.Assert.True(GpuCounters.Last.AtlasHitRate == 1.0, "atlas hit 100% khi cảnh đứng yên");
            t.Assert.Equal(r.AtlasSide, 512, "atlas ban đầu 512² (256 KB), chưa phải 2048² (4 MB)");
            t.Notes.Add($"atlas {r.AtlasSide}² = {r.AtlasSide * r.AtlasSide / 1024} KB, GPU ước tính {r.GpuBytes / 1024} KB, VRAM DXGI usage {r.VideoMemory.LocalUsage / 1024} KB");
        });

        suite.Add("gpu/atlas/glyph-larger-than-atlas-grows-it-without-device-loss", t =>
        {
            t.Open(new BigGlyphHost(), new Size(800, 800));
            t.Settle();
            var scene = t.Draw();
            using var r = GpuKit.TryRenderer(800, 800);
            if (r is null) { t.Notes.Add("SKIP: không tạo được D3D11/WARP"); return; }
            var gpu = GpuKit.Gpu(r, scene, 800, 800);
            t.Assert.Equal(r.Recoveries, 0, "mask lớn không được làm mất thiết bị (UpdateSubresource ngoài biên)");
            var d = GpuKit.Compare(GpuKit.Cpu(scene, 800, 800), gpu, 3);
            t.Assert.True(d.Within >= 0.995, $"glyph lớn: {d}");
            t.Notes.Add($"glyph lớn: atlas nở tới {r.AtlasSide}², {d}");
        });

        suite.Add("gpu/idle/no-frame-request-so-no-encode-no-present", t =>
        {
            t.Open(app => app.New(new BlinkHost()), new Size(300, 120));
            t.Settle();
            int requests = 0;
            t.Window.FrameRequested += () => requests++;
            long frames = GpuCounters.Frames, presents = GpuCounters.Presents;
            // Không focus: không blink, không animation → vòng lặp thật (WM_PAINT chỉ sinh từ RequestFrame) không có gì để làm.
            t.Advance(3);
            t.Assert.Equal(requests, 0, "idle không được yêu cầu frame");
            t.Assert.True(!t.Window.WantsAnimationFrame, "idle không đòi animation frame");
            t.Assert.Equal(GpuCounters.Frames, frames, "idle: 0 draw call (không encode)");
            t.Assert.Equal(GpuCounters.Presents, presents, "idle: 0 present");
            // Có focus: con trỏ nháy chỉ đánh thức vòng lặp ở nhịp nháy (≤ 1 lần / 500 ms), không phải mỗi 16 ms.
            t.Click("field");
            requests = 0;
            t.Advance(3);
            t.Notes.Add($"blink: {requests} yêu cầu frame trong 3 s (nhịp 16 ms sẽ là ~187)");
            t.Assert.True(requests is > 0 and <= 8, $"blink phải đánh thức thưa: {requests} yêu cầu/3 s");
            t.Assert.True(!t.Window.WantsAnimationFrame, "blink không được giữ vòng animation 60 Hz");
        });

        suite.Add("gpu/memory/hardware-adapter-vram-report", t =>
        {
            using var r = D3D11Renderer.TryCreateOffscreen(W, H, warp: false);
            if (r is null) { t.Notes.Add("SKIP: không tạo được thiết bị D3D11"); return; }
            r.Render(Named("images"), W, H);
            t.Notes.Add($"adapter {(r.IsWarp ? "WARP" : "phần cứng")}: VRAM DXGI (local/non-local, KB): usage {r.VideoMemory.LocalUsage / 1024}/{r.VideoMemory.NonLocalUsage / 1024} budget {r.VideoMemory.LocalBudget / 1024}/{r.VideoMemory.NonLocalBudget / 1024}, ước tính tài nguyên {r.GpuBytes / 1024} KB");
        });

        suite.Add("gpu/memory/gallery-idle-footprint", t =>
        {
            foreach (var (name, page) in new (string, Func<App, View>)[] { ("display", a => a.New(new DisplayPage())), ("chat", a => a.New(new ChatPage())) })
            {
                t.Open(app => app.New(new GpuParityTests.PageHost(page)), new Size(1000, 700));
                t.Settle();
                var scene = t.Draw();
                using var r = GpuKit.TryRenderer(1000, 700);
                if (r is null) { t.Notes.Add("SKIP: không tạo được D3D11/WARP"); return; }
                for (int i = 0; i < 3; i++) r.Render(scene, 1000, 700);
                long target = 1000L * 700 * 4;
                t.Notes.Add($"gallery-{name} @1000×700: {scene.Count} primitive, atlas {r.AtlasSide}² ; tài nguyên GPU ước tính {r.GpuBytes / 1024} KB (gồm đích {target / 1024} KB); " +
                            $"atlas CPU {scene.Atlas.Generation} gen; bộ nhớ managed {GC.GetTotalMemory(false) / 1024 / 1024} MB");
                t.Assert.True(r.GpuBytes < 24L << 20, $"gallery-{name}: tài nguyên GPU {r.GpuBytes / 1024} KB vượt 24 MB");
            }
        });

        suite.Add("gpu/alloc/steady-state-renderer-frame-allocates-zero-managed-bytes", t =>
        {
            t.Open(new GpuParityTests.TextHost(), new Size(480, 300));
            t.Settle();
            var scenes = new (string, Scene, int, int)[] { ("solid", Named("solid-rounded-alpha"), W, H), ("images", Named("images"), W, H), ("text", t.Draw(), 480, 300) };
            foreach (var (name, scene, w, h) in scenes)
            {
                using var r = GpuKit.TryRenderer(w, h);
                if (r is null) { t.Notes.Add("SKIP: không tạo được D3D11/WARP"); return; }
                for (int i = 0; i < 5; i++) r.Render(scene, w, h);
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 100; i++) r.Render(scene, w, h);
                long per = (GC.GetAllocatedBytesForCurrentThread() - before) / 100;
                t.Notes.Add($"{name}: {per} B/frame managed trong renderer");
                t.Assert.Equal(per, 0L, $"{name}: renderer cấp phát {per} B/frame ở trạng thái ổn định");
            }
        });
    }
}
