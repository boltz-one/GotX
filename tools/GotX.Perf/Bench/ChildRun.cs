using System.Diagnostics;

namespace GotX.Perf;

/// <summary>
/// <c>run-target &lt;target&gt; --log &lt;file&gt; [--cold] [--no-raster] [--scenarios a,b] [--alloc-types] [--trace]</c>: the in-process half of the bench (one fresh process per
/// target and repeat, so JIT, caches and the glyph atlas start cold). Writes only the perf log; the parent reads it back.
/// </summary>
public static class ChildRun
{
    public static int Run(string[] args)
    {
        string name = args[0];
        string logPath = Arg(args, "--log") ?? throw new ArgumentException("--log required");
        bool cold = args.Contains("--cold"), raster = !args.Contains("--no-raster");
        var target = Targets.All().FirstOrDefault(t => t.Name == name) ?? throw new ArgumentException($"unknown target '{name}'");

        using var log = new PerfLog(logPath, args.Contains("--trace") ? PerfLogLevel.Trace : PerfLogLevel.Frames);
        PerfLog.Shared = log;
        double Ms(long since) => (Stopwatch.GetTimestamp() - since) * 1000.0 / Stopwatch.Frequency;
        long t0 = Stopwatch.GetTimestamp();
        log.Startup("process-to-main", (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds);
        var fonts = Targets.Fonts();
        log.Startup("fonts", Ms(t0));
        long t1 = Stopwatch.GetTimestamp();
        var session = new PerfSession(fonts, target.Root, log, target.Size, raster, args.Contains("--gpu"));
        log.Startup("create", Ms(t1));
        if (cold)
        {
            Scenarios.Cold(session);
            log.Startup("cold-total", Ms(t0));
            log.Startup("cold-alloc", GC.GetTotalAllocatedBytes());
            return 0;
        }

        log.Startup("calibrate", Calibrate());
        PartialWarmup.Run(session.Window.Text);

        // Warm-up (not measured): settle, then enough idle frames for the JIT and every cache to reach steady state.
        session.Mark("warmup");
        session.Settle();
        session.Steps(20);
        // Khung một phần chỉ chạy khi có view bẩn: rê chuột qua vài phần tử để JIT (TieredCompilation=0) biên dịch đường đó trước khi đo.
        var wsz = session.Size;
        for (int i = 0; i < 8; i++)
        {
            session.Send(new GotX.InputEvent(GotX.InputKind.MouseMove, new GotX.Point(wsz.Width * (0.1f + 0.1f * i), wsz.Height * (0.15f + 0.1f * (i % 5)))));
            session.Steps(2);
        }
        session.Send(new GotX.InputEvent(GotX.InputKind.MouseMove, new GotX.Point(-10, -10)));
        session.Settle();
        // Đổi giao diện sáng/tối một vòng: frame đầu sau đổi theme chạy lần đầu các đường transition và vẽ lại toàn bộ (JIT với TieredCompilation=0, ~1 ms):
        // đó là chi phí một lần mỗi tiến trình (cold lo phần khởi động), không phải chi phí ổn định của kịch bản "dark".
        session.Window.DarkMode = true; session.Steps(6); session.Window.DarkMode = false; session.Steps(6);
        session.Settle();
        var wanted = Arg(args, "--scenarios")?.Split(',') ?? Scenarios.Names;
        using var alloc = args.Contains("--alloc-types") ? new AllocListener() : null;
        alloc?.Drain();
        foreach (string scenario in wanted.Where(w => w is "typelong" or "resizelong").Concat(Scenarios.Names.Where(n => wanted.Contains(n)))) // typelong: chỉ khi được chọn
        {
            Scenarios.Run(session, scenario);
            if (alloc is not null) Console.WriteLine(scenario + ":" + Environment.NewLine + alloc.Drain());
        }
        log.Flush();
        return 0;
    }

    /// <summary>
    /// Fixed CPU workload (integer hashing, float math, dictionary traffic), best of 7: the bench divides baseline times by the ratio of this
    /// number to the one stored in the baseline, so a slower / busier machine does not read as a regression.
    /// </summary>
    static double Calibrate()
    {
        double best = double.MaxValue;
        for (int rep = 0; rep < 7; rep++)
        {
            long t = Stopwatch.GetTimestamp();
            ulong h = 14695981039346656037UL;
            float f = 0;
            var dict = new Dictionary<int, int>();
            for (int i = 0; i < 300_000; i++)
            {
                h = (h ^ (ulong)i) * 1099511628211UL;
                f += MathF.Sqrt(i);
                dict[i & 1023] = i;
            }
            GC.KeepAlive(dict);
            if (h == 0 || f < 0) Console.Write(' '); // keep the loop observable
            best = Math.Min(best, (Stopwatch.GetTimestamp() - t) * 1000.0 / Stopwatch.Frequency);
        }
        return best;
    }

    static string? Arg(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}

/// <summary>Chạy trước khi đo (TieredCompilation=0 biên dịch đầy đủ ở lần gọi đầu): một cửa sổ nhỏ có view con bị Notify để đường khung một phần + ghép đoạn được JIT.</summary>
static class PartialWarmup
{
    sealed class Leaf : View
    {
        public int N;
        public override Div Render(Window w) =>
            w.Div().Class("p-2 flex gap-2 hover:bg-slate-100").OnClick(this, static (v, _) => v.Notify()).Child(w.Div().Class("size-4 rounded bg-blue-500")).Child("n" + N);
    }

    sealed class Root : View
    {
        public Leaf A = null!, B = null!;
        public GotX.Widgets.TextInput Field = null!;
        protected override void OnCreated() { A = App.New(new Leaf()); B = App.New(new Leaf()); Field = App.New(new GotX.Widgets.TextInput { Multiline = false, Text = "x", BoxClass = "h-8 w-40 px-3 border rounded-md focus:ring-2" }); }
        public override Div Render(Window w)
        {
            var list = w.Div().Key("wl").Class("h-24 overflow-y-auto flex flex-col");
            for (int i = 0; i < 12; i++) list.Child(w.View(i % 2 == 0 ? A : B));
            return w.Div().Class("size-full flex flex-col gap-2 overflow-hidden rounded").Child(w.View(A)).Child(w.View(B)).Child(w.Div().Key("f").Child(w.View(Field))).Child(list).Child("static");
        }
    }

    public static void Run(GotX.Text.TextSystem text)
    {
        var app = new App();
        var win = new Window(app, text) { Log = null };
        win.Root = app.New(new Root());
        var size = new Size(300, 200);
        for (int i = 0; i < 3; i++) win.Draw(size, 1);
        var root = (Root)win.Root!;
        win.DispatchInput(new InputEvent(InputKind.MouseDown, new Point(40, 90)));
        win.DispatchInput(new InputEvent(InputKind.MouseUp, new Point(40, 90)));
        for (int i = 0; i < 4; i++) { win.DispatchInput(new InputEvent(InputKind.TextInput, Text: "ab"[i & 1].ToString())); app.Flush(); win.Draw(size, 1); }
        for (int i = 0; i < 4; i++) { win.DispatchInput(new InputEvent(InputKind.Scroll, new Point(60, 150), Delta: new Point(0, 40))); app.Flush(); win.Draw(size, 1); win.Draw(size, 1); }
        for (int i = 0; i < 4; i++)
        {
            root.A.N++; root.A.Notify(); root.B.Notify();
            app.Flush();
            win.Draw(size, 1);
            win.DispatchInput(new InputEvent(InputKind.MouseMove, new Point(20 + i, 20)));
            win.Draw(size, 1);
        }
    }
}
