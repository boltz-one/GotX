using System.Diagnostics;
using GotX;
using GotX.Raster;
using GotX.Test;
using GotX.Widgets;

/// <summary>
/// Vùng hỏng (prototype, mặc định tắt): trên chuỗi hover / con trỏ nháy / cuộn / animation, ảnh vẽ theo vùng hỏng phải GIỐNG TỪNG PIXEL ảnh vẽ cả frame,
/// với renderer CPU (canvas giữ lại, vẽ lại ô hỏng) và D3D11 WARP (đích giữ lại + scissor). Đo tiết kiệm trên cảnh rảnh có con trỏ nháy.
/// </summary>
static class GpuDamageTests
{
    const int W = 600, H = 420;

    sealed class DamageHost(bool pulse = true) : View
    {
        public TextInput Field = null!;
        protected override void OnCreated() => Field = App.New(new TextInput { Text = "hello", BoxClass = "h-8 w-56 px-3 border bg-white text-slate-900", Blink = true, AriaLabel = "field" });

        public override Div Render(Window w)
        {
            var row = w.Div().Class("flex gap-3");
            for (int i = 0; i < 5; i++)
                row.Child(w.Div().Key("b" + i).Class("w-24 h-12 rounded-lg border bg-slate-100 hover:bg-blue-200 hover:shadow-lg flex items-center justify-center text-sm").Child("Btn " + i));
            var list = w.Div().Key("list").Class("w-full h-48 overflow-y-auto border rounded-md flex flex-col");
            for (int i = 0; i < 60; i++) list.Child(w.Div().Class("px-3 py-1 text-sm border-b").Child($"Row {i} lorem ipsum dolor sit amet"));
            return w.Div().Class("size-full bg-white p-4 flex flex-col gap-3 text-slate-900")
                .Child(row).Child(w.Div().Key("field").Child(w.View(Field))).Child(w.Div().Class("size-6 rounded-full border-2 border-blue-500 bg-blue-100" + (pulse ? " animate-pulse" : ""))).Child(list);
        }
    }

    static bool Same(Canvas a, Canvas b) => a.Pixels.AsSpan(0, a.Width * a.Height).SequenceEqual(b.Pixels.AsSpan(0, b.Width * b.Height));

    static void Sequence(UiTest t, Action<Action> run, out int frames, out int empty, out double coverage, out long cpuFullUs, out long cpuDamUs)
    {
        var damage = new SceneDamage();
        var retained = new Canvas(); retained.Resize(W, H);
        var full = new Canvas(); full.Resize(W, H);
        using var gpuDamaged = GpuKit.TryRenderer(W, H);
        using var gpuFull = GpuKit.TryRenderer(W, H);
        if (gpuDamaged is not null) gpuDamaged.Damage = new SceneDamage();
        int f = 0, e = 0, bad = 0, gbad = 0; double cov = 0; long usFull = 0, usDam = 0;
        var gc = new Canvas(); var gf = new Canvas();
        run(() =>
        {
            var scene = t.Draw();
            long t0 = Stopwatch.GetTimestamp();
            SoftwareRenderer.Render(scene, full);
            long t1 = Stopwatch.GetTimestamp();
            var r = damage.Update(scene, W, H);
            SoftwareRenderer.RenderDamaged(scene, retained, r);
            long t2 = Stopwatch.GetTimestamp();
            usFull += (t1 - t0) * 1_000_000 / Stopwatch.Frequency; usDam += (t2 - t1) * 1_000_000 / Stopwatch.Frequency;
            f++; if (r.Empty) e++; cov += r.Coverage;
            if (!Same(full, retained)) bad++;
            if (gpuDamaged is not null && gpuFull is not null)
            {
                gpuDamaged.Render(scene, W, H); gpuDamaged.ReadPixels(gc);
                gpuFull.Render(scene, W, H); gpuFull.ReadPixels(gf);
                if (!Same(gc, gf)) gbad++;
            }
        });
        t.Assert.Equal(bad, 0, $"CPU: số frame vẽ theo vùng hỏng khác vẽ cả frame (trên {f})");
        t.Assert.Equal(gbad, 0, $"GPU: số frame vẽ theo vùng hỏng khác vẽ cả frame (trên {f})");
        frames = f; empty = e; coverage = cov / Math.Max(1, f); cpuFullUs = usFull; cpuDamUs = usDam;
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("gpu/damage/hover-blink-scroll-animation-sequences-are-pixel-exact", t =>
        {
            t.Open(app => app.New(new DamageHost()), new Size(W, H));
            t.Settle();
            Sequence(t, step =>
            {
                step();
                foreach (var key in new[] { "b0", "b2", "b4", "b1" }) { t.Hover(key); step(); step(); }   // hover: đổi nền + bóng
                t.MouseLeave(); step();
                t.Click(new Point(60, 92)); for (int i = 0; i < 8; i++) { t.Advance(0.5); step(); }               // con trỏ nháy
                for (int i = 0; i < 6; i++) { t.Scroll("list", 40); step(); }                           // cuộn
                for (int i = 0; i < 20; i++) { t.Advance(0.05); step(); }                               // animate-pulse
                t.Type("x"); step();                                                                      // đổi chữ
            }, out int frames, out int empty, out double cov, out _, out _);
            t.Notes.Add($"{frames} frame, {empty} frame rỗng (không vẽ), trung bình {cov * 100:F1}% số ô bị vẽ lại");
        });

        suite.Add("gpu/damage/idle-blink-saves-raster-and-upload", t =>
        {
            t.Open(app => app.New(new DamageHost(pulse: false)), new Size(W, H));
            t.Settle();
            t.Click(new Point(60, 92));
            using var gpu = GpuKit.TryRenderer(W, H);
            if (gpu is not null) gpu.Damage = new SceneDamage();
            Sequence(t, step =>
            {
                for (int i = 0; i < 40; i++) { t.Advance(0.55); step(); } // ~22 s rảnh, con trỏ nháy (bước lệch pha để bắt cả hai trạng thái)
            }, out int frames, out int empty, out double cov, out long full, out long dam);
            t.Notes.Add($"rảnh + nháy: {frames} frame; CPU raster {full / frames} µs → {dam / frames} µs/frame ({100.0 * dam / Math.Max(1, full):F0}%), ô vẽ lại {cov * 100:F1}%");
            t.Assert.True(dam < full, $"vẽ theo vùng hỏng phải rẻ hơn vẽ cả frame trên cảnh rảnh ({dam} vs {full} µs tổng)");
            if (gpu is null) return;
            var scene = t.Draw();
            gpu.Render(scene, W, H);
            long fullBytes = GpuCounters.Last.InstanceBytes;
            var half = (bytes: 0L, prims: 0);
            for (int i = 0; i < 6 && half.prims == 0; i++) { t.Advance(0.55); scene = t.Draw(); gpu.Render(scene, W, H); half = (GpuCounters.Last.InstanceBytes, GpuCounters.Last.Primitives); }
            t.Notes.Add($"GPU: frame nháy có thay đổi gửi {half.prims} primitive / {half.bytes} B instance (cả frame {scene.Count} primitive / {fullBytes} B); frame rảnh bị bỏ qua hoàn toàn: {GpuCounters.Skipped} lần");
            t.Assert.True(half.prims > 0 && half.prims < scene.Count / 4, $"vùng hỏng của frame nháy phải gửi ít primitive hơn nhiều: {half.prims}/{scene.Count}");
        });
    }
}
