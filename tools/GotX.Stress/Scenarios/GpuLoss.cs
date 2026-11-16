using GotX.Platform.Windows;

namespace GotX.Stress;

/// <summary>
/// D3D11 (WARP, offscreen) device loss under load: the mixed page is driven by seeded input while DEVICE_REMOVED / RESET / HUNG / DRIVER_INTERNAL_ERROR
/// are injected into the renderer every 25 frames (the only simulated part is the HRESULT, via the internal InjectHr seam). Every injection must be recovered
/// by rebuilding device + shaders + buffers + atlas; <c>lossErrors</c> counts injections not recovered, failed renders and a Failed renderer (must be 0).
/// </summary>
static class GpuLossScenario
{
    public static Dictionary<string, double>? Run(GotX.Text.TextSystem text)
    {
        var size = Scenarios.Desktop;
        using var gpu = D3D11Renderer.TryCreateOffscreen((int)size.Width, (int)size.Height, warp: true);
        if (gpu is null) { Console.WriteLine("  gpu-loss: SKIPPED - no D3D11/WARP device on this machine"); return null; }
        gpu.Damage = new SceneDamage();
        int fails = 0;
        var rig = new Rig(text);
        rig.Present = (scene, w) => { if (!gpu.Render(scene, (int)(w.Size.Width * w.Scale), (int)(w.Size.Height * w.Scale))) fails++; };
        var win = rig.Add(a => a.New(new MixedPage()), size);
        var m = Scenarios.Begin(rig, win);
        int[] losses = [D3D.DXGI_ERROR_DEVICE_REMOVED, D3D.DXGI_ERROR_DEVICE_RESET, D3D.DXGI_ERROR_DEVICE_HUNG, D3D.DXGI_ERROR_DRIVER_INTERNAL_ERROR];
        var rng = new Random(77);
        int injected = 0, rec0 = gpu.Recoveries;
        for (int f = 0; f < 400; f++)
        {
            if (f % 25 == 10 && gpu.InjectCount == 0) { gpu.InjectHr = losses[f / 25 % losses.Length]; gpu.InjectCount = 1; injected++; }
            m.Add(rig.Send(win, InputScenario.Next(rng, win.Size)));
        }
        int recovered = gpu.Recoveries - rec0, pending = gpu.InjectCount;
        int errors = fails + (gpu.Failed ? 1 : 0) + Math.Max(0, injected - pending - recovered);
        m.Set("injected", injected); m.Set("recovered", recovered); m.Set("lossErrors", errors);
        Console.WriteLine($"\n  gpu-loss: injected {injected}, recovered {recovered}, failed renders {fails}, renderer {(gpu.Failed ? "FAILED" : "alive")}" + (errors == 0 ? "" : "  <-- ERRORS"));
        return m.Finish();
    }
}
