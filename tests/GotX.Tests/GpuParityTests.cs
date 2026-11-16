using GotX;
using GotX.Raster;
using GotX.Test;

/// <summary>
/// Parity CPU↔GPU: cùng một Scene vẽ bằng SoftwareRenderer và bằng D3D11 (WARP, đích offscreen + đọc lại) rồi so từng pixel.
/// Ngưỡng: lệch kênh ≤ 3 trên ≥ 99.5% pixel. Báo ô 32×32 xấu nhất khi hỏng (và lưu ảnh cpu/gpu cạnh nhau).
/// Bỏ qua kèm ghi chú rõ chỉ khi D3D11/WARP không tạo được.
/// </summary>
static class GpuParityTests
{
    const int Tol = 3;
    const double MinWithin = 0.995;

    internal sealed class PageHost(Func<App, View> page) : View
    {
        View _page = null!;
        protected override void OnCreated() => _page = page(App);
        public override Div Render(Window w) =>
            w.Div().Class("size-full overflow-y-auto bg-background text-foreground").Key("gallery-page").Child(w.View(_page));
    }

    internal sealed class TextHost : View
    {
        static Div T(Window w, string cls, string text) => w.Div().Class(cls).Child(text);

        public override Div Render(Window w) => w.Div().Class("size-full bg-white p-4 flex flex-col gap-2")
            .Child(T(w, "text-xs text-zinc-900", "The quick brown fox jumps over the lazy dog 0123456789"))
            .Child(T(w, "text-sm text-blue-700 font-medium", "Glyph text parity: hinting, subpixel, kerning (AV To Wa)"))
            .Child(T(w, "text-base text-zinc-700", "Rendering the same atlas masks on CPU and GPU"))
            .Child(T(w, "text-xl font-semibold text-red-600", "Heading 20px semibold"))
            .Child(T(w, "text-3xl font-bold text-emerald-700 rotate-3", "Rotated 3deg"))
            .Child(T(w, "rounded-xl border border-black/20 bg-white/50 p-3 shadow-lg text-sm", "Card with translucent border and shadow"));
    }

    static bool Compare(UiTest t, string name, Scene scene, int w, int h)
    {
        using var gpu = GpuKit.TryRenderer(w, h);
        if (gpu is null) { t.Notes.Add($"SKIP {name}: không tạo được D3D11/WARP trên máy này"); return false; }
        var a = GpuKit.Cpu(scene, w, h);
        var b = GpuKit.Gpu(gpu, scene, w, h);
        var d = GpuKit.Compare(a, b, Tol);
        bool ok = t.Assert.True(d.Within >= MinWithin, $"{name}: {d}");
        if (!ok)
        {
            PngWriter.Write(t.ArtifactPath(name + ".cpu.png"), a);
            PngWriter.Write(t.ArtifactPath(name + ".gpu.png"), b);
        }
        t.Notes.Add($"{name}: {d}");
        return true;
    }

    public static void Register(TestSuite suite)
    {
        foreach (var (name, scene) in GpuParityScenes.All())
        {
            string n = name;
            var sc = scene;
            suite.Add($"gpu/parity/{n}", t => Compare(t, n, sc, GpuParityScenes.W, GpuParityScenes.H));
        }

        suite.Add("gpu/parity/glyph-text", t =>
        {
            t.Open(new TextHost(), new Size(480, 300));
            t.Settle();
            Compare(t, "glyph-text", t.Draw(), 480, 300);
        });

        foreach (var (name, page) in new (string, Func<App, View>)[]
        {
            ("display", app => app.New(new DisplayPage())), ("forms", app => app.New(new FormsPage())),
            ("overlays", app => app.New(new OverlaysPage())), ("navigation", app => app.New(new NavigationPage())),
            ("chat", app => app.New(new ChatPage())), ("data", app => app.New(new DataPage())),
        })
        {
            string n = name;
            var pg = page;
            suite.Add($"gpu/parity/gallery-{n}", t =>
            {
                t.Open(app => app.New(new PageHost(pg)), new Size(900, 700), scale: 1);
                t.Settle();
                Compare(t, "gallery-" + n, t.Draw(), 900, 700);
            });
        }

        suite.Add("gpu/parity/gallery-forms-dark-1.5x", t =>
        {
            t.Open(app => app.New(new PageHost(a => a.New(new FormsPage()))), new Size(600, 500), scale: 1.5f, dark: true);
            t.Settle();
            Compare(t, "gallery-forms-dark-1.5x", t.Draw(), 900, 750);
        });
    }
}
