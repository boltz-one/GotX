using System.Globalization;
using System.Text;
using GotX.Components;
using GotX.Platform.Windows;
using ShadcnShots;

namespace GotX.Footprint;

/// <summary>
/// <c>host &lt;scenario&gt; --renderer d3d|software</c>: one scenario in this process on a real Win32 window. The parent drives input and samples the
/// process from outside; this side only answers one-byte commands on stdin: <c>s</c> counters (frames, GC, allocation, GPU memory), <c>h</c> collect + heap size, <c>q</c> exit.
/// Replies on stdout: <c>FIRST ms</c> once, then <c>S k=v ...</c> / <c>H k=v ...</c>. Costs the host carries: one blocked stdin thread + one 5 ms poller until the first frame.
/// </summary>
static class HostCommand
{
    public static int Run(string[] args)
    {
        string scenario = args[0];
        bool gpu = Args.Get(args, "--renderer") != "software";
        ComponentTheme.Install();
        new Thread(() => Control(gpu)) { IsBackground = true, Name = "fp-control" }.Start();
        IPlatform inner = new WindowsPlatform(gpu);
        if (scenario is "home" or "resize") inner = new GeistPlatform(inner); // the shadcn page ships with Geist
        var (w, h) = Scenes.Size(scenario);
        Application.Run(new CountingPlatform(inner), new WindowOptions("GotX footprint - " + scenario, w, h), app => Scenes.Create(scenario, app));
        return 0;
    }

    static void Control(bool gpu)
    {
        using var stdin = Console.OpenStandardInput();
        using var stdout = Console.OpenStandardOutput();
        new Thread(() =>
        {
            while (CountingPlatform.FirstPresentMs < 0) Thread.Sleep(5);
            Send(stdout, "FIRST " + CountingPlatform.FirstPresentMs);
        }) { IsBackground = true }.Start();
        var buf = new byte[16];
        int n;
        while ((n = stdin.Read(buf, 0, buf.Length)) > 0)
            for (int i = 0; i < n; i++)
            {
                if (buf[i] == (byte)'s') Send(stdout, "S " + Counters(gpu, false));
                else if (buf[i] == (byte)'h') Send(stdout, "H " + Counters(gpu, true));
                else if (buf[i] == (byte)'q') Environment.Exit(0);
            }
    }

    static string Counters(bool gpu, bool collect)
    {
        long heap = GC.GetTotalMemory(collect); // collect=true: full blocking GC first, so the number is live data only
        long alloc = GC.GetTotalAllocatedBytes(true);
        long frames = CountingPlatform.Frames, presents = CountingPlatform.Presents;
        int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
        var (local, shared) = gpu ? GpuMem.Usage() : (-1, -1);
        return string.Create(CultureInfo.InvariantCulture,
            $"frames={frames} presents={presents} alloc={alloc} gc0={g0} gc1={g1} gc2={g2} heap={heap} gpuLocal={local} gpuShared={shared} cycles={DialogView.Opened}");
    }

    static void Send(Stream s, string line)
    {
        var b = Encoding.UTF8.GetBytes(line + "\n");
        lock (s) { s.Write(b, 0, b.Length); s.Flush(); }
    }
}
