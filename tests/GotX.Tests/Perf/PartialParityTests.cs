using GotX;
using GotX.Components;
using GotX.Test;
using ShadcnShots;

/// <summary>
/// Khung một phần (chỉ vẽ lại view bẩn, thay đoạn scene tại chỗ) phải cho đúng cùng kết quả như dựng đầy đủ:
/// hai cửa sổ cùng trang, cùng chuỗi input ngẫu nhiên (seed cố định) – một bật, một tắt <see cref="Window.PartialFrames"/> –
/// scene (primitive, vùng cắt, gradient), hitbox, hover khớp từng phần tử sau mỗi bước.
/// </summary>
static class PartialParityTests
{
    sealed class Rig
    {
        public readonly App App;
        public readonly Window Window;
        public double Now = 1000;
        public readonly List<string> Events = new();
        public readonly Size Size;

        public Rig(Func<App, View> root, Size size, bool partial)
        {
            Size = size;
            App = new App { Clock = () => Now };
            Window = new Window(App, TestFonts.Shared) { Clock = () => Now, PartialFrames = partial, Trace = e => { if (e.Kind is TraceKind.ViewRender or TraceKind.ViewDirty or TraceKind.Frame) Events.Add(e.ToString()); } };
            Window.Root = root(App);
            Step(); Step();
        }

        public Action<Scene>? OnFrame;

        public void Step()
        {
            Now += 0.016;
            App.RunTimers();
            var scene = Window.Draw(Size, 1);
            App.Flush();
            OnFrame?.Invoke(scene);
        }

        public void Send(in InputEvent e)
        {
            Window.DispatchInput(e);
            App.Flush();
            Step();
        }

        public void Frames(int n) { for (int i = 0; i < n; i++) Step(); }
    }

    sealed class PageHost(Func<App, View> page) : View
    {
        View _page = null!;
        protected override void OnCreated() => _page = page(App);
        public override Div Render(Window w) =>
            w.Div().Class("size-full overflow-y-auto bg-background text-foreground").Key("page").Child(w.View(_page));
    }

    static readonly (string Name, Func<App, View> Root, Size Size)[] Targets =
    [
        ("forms", app => app.New(new PageHost(a => a.New(new FormsPage()))), new Size(1000, 760)),
        ("overlays", app => app.New(new PageHost(a => a.New(new OverlaysPage()))), new Size(1000, 760)),
        ("navigation", app => app.New(new PageHost(a => a.New(new NavigationPage()))), new Size(1000, 760)),
        ("display", app => app.New(new PageHost(a => a.New(new DisplayPage()))), new Size(1000, 760)),
        ("chat", app => app.New(new PageHost(a => a.New(new ChatPage()))), new Size(1000, 760)),
        ("home", app => app.New(new SiteShell()), new Size(1440, 900)),
        ("sidebar-03", app => app.New(new ShotHost(BlockCatalog.All.First(b => b.Id == "sidebar-03"))), new Size(1440, 900)),
        ("login-04", app => app.New(new ShotHost(BlockCatalog.All.First(b => b.Id == "login-04"))), new Size(1440, 900)),
    ];

    /// <summary>Lỗi đầu tiên (mô tả) khi so hai cửa sổ, hoặc null nếu khớp.</summary>
    static string? Diff(Rig a, Rig b)
    {
        var sa = a.Window.LastScene; var sb = b.Window.LastScene;
        var pa = sa.Primitives; var pb = sb.Primitives;
        if (sa.Count != sb.Count)
        {
            int k = 0;
            while (k < pa.Length && k < pb.Length && SameInk(pa[k], pb[k])) k++;
            return $"số primitive {sa.Count} ≠ {sb.Count}; khác đầu tiên #{k}: A {(k < pa.Length ? pa[k].Kind + " " + pa[k].Bounds : "-")} | B {(k < pb.Length ? pb[k].Kind + " " + pb[k].Bounds : "-")}";
        }
        for (int i = 0; i < pa.Length; i++)
            if (!SameInk(pa[i], pb[i])) return $"primitive #{i} khác ở {Fields(pa[i], pb[i])} ({pa[i].Kind})";
        if (sa.ShapeCount != sb.ShapeCount) return "số vùng cắt hình";
        for (int i = 0; i < sa.ShapeCount; i++) if (!sa.ClipShapes[i].Equals(sb.ClipShapes[i])) return $"vùng cắt hình #{i}";
        if (a.Window.HitboxCount != b.Window.HitboxCount) return $"số hitbox {a.Window.HitboxCount} ≠ {b.Window.HitboxCount}";
        for (int i = 0; i < a.Window.HitboxCount; i++)
            if (a.Window.HitboxBounds(i) != b.Window.HitboxBounds(i) || a.Window.HitboxIsClick(i) != b.Window.HitboxIsClick(i)) return $"hitbox #{i}";
        if (a.Window.CursorAtMouse != b.Window.CursorAtMouse) return "con trỏ chuột";
        return null;
    }

    static bool SameInk(Primitive x, Primitive y)
    {
        if (x.Kind == PrimitiveKind.Glyph) { x.Glyph = y.Glyph; } // chỉ số atlas phụ thuộc thứ tự raster glyph (atlas dùng chung hai cửa sổ)
        return x.Equals(y);
    }

    static string Fields(Primitive x, Primitive y)
    {
        object bx = x, by = y;
        var diff = new List<string>();
        foreach (var fi in typeof(Primitive).GetFields())
            if (!Equals(fi.GetValue(bx), fi.GetValue(by))) diff.Add($"{fi.Name}: {fi.GetValue(bx)} ↔ {fi.GetValue(by)}");
        return string.Join("; ", diff);
    }

    static void RandomSequence(UiTest t, string name, Func<App, View> root, Size size, int seed, int steps)
    {
        var a = new Rig(root, size, partial: Environment.GetEnvironmentVariable("GOTX_PARITY_BASE") is null);
        var b = new Rig(root, size, partial: false);
        var rng = new Random(seed);
        string chars = "abcdXYZ 0123";
        for (int i = 0; i < steps; i++)
        {
            InputEvent e;
            var w = a.Window;
            a.Events.Clear(); b.Events.Clear();
            switch (rng.Next(10))
            {
                case 0 or 1 or 2 or 3 when w.HitboxCount > 0:
                {
                    var bx = w.HitboxBounds(rng.Next(w.HitboxCount));
                    var p = bx.IsEmpty ? new Point(rng.Next((int)size.Width), rng.Next((int)size.Height)) : new Point(bx.X + bx.Width / 2, bx.Y + bx.Height / 2);
                    e = new InputEvent(InputKind.MouseMove, p);
                    break;
                }
                case 4:
                {
                    var p = new Point(rng.Next((int)size.Width), rng.Next((int)size.Height));
                    foreach (var k in new[] { InputKind.MouseMove, InputKind.MouseDown }) { a.Send(new InputEvent(k, p)); b.Send(new InputEvent(k, p)); }
                    e = new InputEvent(InputKind.MouseUp, p);
                    break;
                }
                case 5: e = new InputEvent(InputKind.TextInput, Text: chars[rng.Next(chars.Length)].ToString()); break;
                case 6: e = new InputEvent(InputKind.Scroll, new Point(size.Width / 2, size.Height / 2), Delta: new Point(0, rng.Next(-120, 120))); break;
                case 7: e = new InputEvent(InputKind.KeyDown, Key: new[] { Key.Tab, Key.Escape, Key.Backspace, Key.Left, Key.Right, Key.Down, Key.Enter }[rng.Next(7)]); break;
                case 8: { a.Frames(3); b.Frames(3); e = new InputEvent(InputKind.MouseMove, new Point(rng.Next((int)size.Width), rng.Next((int)size.Height))); break; }
                default: e = new InputEvent(InputKind.MouseMove, new Point(rng.Next((int)size.Width), rng.Next((int)size.Height))); break;
            }
            a.Send(e); b.Send(e);
            if (rng.Next(3) == 0) { int n = rng.Next(1, 40); a.Frames(n); b.Frames(n); }
            string? diff = Diff(a, b);
            if (diff is not null)
            {
                t.Notes.Add($"anim A={a.Window.WantsAnimationFrame} B={b.Window.WantsAnimationFrame} focusA={a.Window.Focused?.Id.Value} focusB={b.Window.Focused?.Id.Value}"); t.Notes.Add("A: " + string.Join(" | ", a.Events.Where(x => !x.Contains("Frame:")).Take(40))); t.Notes.Add("B: " + string.Join(" | ", b.Events.Where(x => !x.Contains("Frame:")).Take(40)));
                t.Fail($"{name} seed {seed} bước {i} ({e.Kind}): {diff} (khung một phần: {a.Window.PartialFrameCount}, lùi: {a.Window.PartialFallbackCount})");
                return;
            }
        }
        t.Notes.Add($"{name} seed {seed}: {steps} bước khớp; khung một phần {a.Window.PartialFrameCount}, lùi {a.Window.PartialFallbackCount}");
    }

    /// <summary>Vùng hỏng từ hint của Window: canvas giữ lại vẽ theo hint == vẽ cả frame, từng pixel, ở mọi frame (có lúc renderer bỏ qua frame); hint phủ kín ô hỏng của so hash.</summary>
    static void DamageSequence(UiTest t, string name, Func<App, View> root, Size size, int seed, int steps)
    {
        var rig = new Rig(root, size, partial: true);
        int w = (int)size.Width, h = (int)size.Height;
        var hint = new SceneDamage(); var hash = new SceneDamage { UseHint = false };
        var retained = new GotX.Raster.Canvas(); retained.Resize(w, h);
        var full = new GotX.Raster.Canvas(); full.Resize(w, h);
        var rng = new Random(seed ^ 99);
        int frames = 0, bad = 0, uncovered = 0, empty = 0, fulls = 0; double cov = 0;
        string? first = null;
        rig.OnFrame = scene =>
        {
            frames++;
            var hr = hash.Update(scene, w, h);
            if (rng.Next(7) == 0) return; // renderer bỏ qua frame này: hint phải tích luỹ cho lần vẽ sau
            var r = hint.Update(scene, w, h);
            GotX.Raster.SoftwareRenderer.RenderDamaged(scene, retained, r);
            GotX.Raster.SoftwareRenderer.Render(scene, full);
            if (r.Empty) empty++;
            if (r.Full) fulls++;
            cov += r.Coverage;
            if (!retained.Pixels.AsSpan(0, w * h).SequenceEqual(full.Pixels.AsSpan(0, w * h))) { bad++; first ??= $"frame {frames}: ảnh vẽ theo hint khác vẽ cả frame"; }
            if (!r.Full && !hr.Full && hr.Dirty is { } hd && r.Dirty is { } rd)
                for (int i = 0; i < hd.Length && i < rd.Length; i++) if (hd[i] && !rd[i]) { uncovered++; break; }
        };
        Drive(rig, rng, size, steps);
        if (bad > 0 || uncovered > 0) t.Fail($"{name} seed {seed}: {bad}/{frames} frame lệch pixel ({first}), {uncovered} frame hint không phủ ô hỏng của so hash");
        else t.Notes.Add($"{name} seed {seed}: {frames} frame khớp pixel, {empty} rỗng, {fulls} vẽ cả frame, trung bình {cov / Math.Max(1, frames - empty) * 100:F1}% ô vẽ lại");
    }

    static void Drive(Rig a, Random rng, Size size, int steps)
    {
        string chars = "abcdXYZ 0123";
        for (int i = 0; i < steps; i++)
        {
            var w = a.Window;
            switch (rng.Next(9))
            {
                case 0 or 1 or 2 when w.HitboxCount > 0:
                {
                    var bx = w.HitboxBounds(rng.Next(w.HitboxCount));
                    a.Send(new InputEvent(InputKind.MouseMove, bx.IsEmpty ? new Point(rng.Next((int)size.Width), rng.Next((int)size.Height)) : new Point(bx.X + bx.Width / 2, bx.Y + bx.Height / 2)));
                    break;
                }
                case 3: { var p = new Point(rng.Next((int)size.Width), rng.Next((int)size.Height)); foreach (var k in new[] { InputKind.MouseMove, InputKind.MouseDown, InputKind.MouseUp }) a.Send(new InputEvent(k, p)); break; }
                case 4: a.Send(new InputEvent(InputKind.TextInput, Text: chars[rng.Next(chars.Length)].ToString())); break;
                case 5: a.Send(new InputEvent(InputKind.Scroll, new Point(size.Width / 2, size.Height / 2), Delta: new Point(0, rng.Next(-120, 120)))); break;
                case 6: a.Send(new InputEvent(InputKind.KeyDown, Key: new[] { Key.Tab, Key.Escape, Key.Backspace, Key.Left, Key.Right, Key.Down, Key.Enter }[rng.Next(7)])); break;
                default: a.Send(new InputEvent(InputKind.MouseMove, new Point(rng.Next((int)size.Width), rng.Next((int)size.Height)))); break;
            }
            if (rng.Next(3) == 0) a.Frames(rng.Next(1, 40));
        }
    }

    public static void Register(TestSuite suite)
    {
        // Mặc định chạy ngắn (mỗi bước so cả khung với dựng đầy đủ nên đắt: bản dài mất ~17 phút); GOTX_PARITY_FULL=1 chạy đủ.
        bool full = Environment.GetEnvironmentVariable("GOTX_PARITY_FULL") == "1";
        int damageSteps = full ? 50 : 10, partialSteps = full ? 90 : 20;
        foreach (var (name, root, size) in Targets)
            suite.Add($"perf/damage/parity-random-{name}", t =>
            {
                DamageSequence(t, name, root, size, 3, damageSteps);
                if (full && size.Width < 1400) DamageSequence(t, name, root, size, 4321, damageSteps);
            });
        foreach (var (name, root, size) in Targets)
            suite.Add($"perf/partial/parity-random-{name}", t =>
            {
                RandomSequence(t, name, root, size, 7, partialSteps);
                if (full) RandomSequence(t, name, root, size, 1234, partialSteps);
            });
    }
}
