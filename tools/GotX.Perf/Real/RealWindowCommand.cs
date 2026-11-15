using System.Diagnostics;
using GotX.Raster;

namespace GotX.Perf;

/// <summary>
/// <c>real &lt;shadcn|gallery|exe&gt; [--seconds N] [--out dir]</c>: starts a sample on a real D3D11 window with <c>GOTX_LOG</c> set,
/// drives it with posted input messages (idle, wheel scroll, pointer sweep, clicks + Escape, typing), captures the window with
/// PrintWindow (PNG next to the log), closes it and prints the report.
/// </summary>
public static class RealWindowCommand
{
    public static int Run(string[] args)
    {
        if (args.Length < 1) { Console.Error.WriteLine("usage: real <shadcn|gallery|path.exe> [--seconds N] [--out dir]"); return 2; }
        double seconds = ReportCommand.ArgInt(args, "--seconds", 10);
        string outDir = Path.GetFullPath(Arg(args, "--out") ?? Path.Combine(Path.GetTempPath(), "gotx-perf-real"));
        Directory.CreateDirectory(outDir);
        string? exe = Resolve(args[0]);
        if (exe is null) { Console.Error.WriteLine($"cannot find executable for '{args[0]}' (build the sample or pass a path)"); return 2; }
        string name = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
        string log = Path.Combine(outDir, $"real-{name}.jsonl");

        var psi = new ProcessStartInfo(exe) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(exe)! };
        psi.Environment["GOTX_LOG"] = log;
        psi.Environment["GOTX_LOG_LEVEL"] = "frames";
        using var proc = Process.Start(psi)!;
        nint hwnd = 0;
        for (int i = 0; i < 400 && hwnd == 0 && !proc.HasExited; i++) { Thread.Sleep(50); hwnd = Native.FindWindow((uint)proc.Id); }
        if (hwnd == 0) { Console.Error.WriteLine("window did not appear"); Kill(proc); return 3; }
        Thread.Sleep(1500); // first frames, fonts, atlas warm-up

        Drive(hwnd, seconds);

        Thread.Sleep(400);
        var pixels = Native.Capture(hwnd, out int w, out int h);
        if (pixels is not null)
        {
            var canvas = new Canvas();
            canvas.Resize(w, h);
            for (int i = 0; i < w * h; i++) canvas.Pixels[i] = pixels[i] | 0xFF000000u;
            PngWriter.Write(Path.Combine(outDir, $"real-{name}.png"), canvas);
        }
        Native.Post(hwnd, Native.WM_CLOSE, 0, 0);
        if (!proc.WaitForExit(4000)) Kill(proc);
        Thread.Sleep(300);
        Console.WriteLine($"window {w}x{h}, log {log}" + (pixels is not null ? $", capture {Path.Combine(outDir, $"real-{name}.png")}" : ", capture failed"));
        Console.Write(ReportCommand.Render(LogData.Read(log), 8, phases: false));
        return 0;
    }

    // ~60 messages/s timeline, split in thirds: wheel (down/up), pointer sweep, clicks and typing.
    static void Drive(nint hwnd, double seconds)
    {
        var (w, h) = Native.ClientSize(hwnd);
        var clock = Stopwatch.StartNew();
        int step = 0;
        while (clock.Elapsed.TotalSeconds < seconds)
        {
            double t = clock.Elapsed.TotalSeconds / seconds;
            int cx = w / 2 + (w > 800 ? w / 8 : 0), cy = h / 2;
            if (t < 0.10) { /* idle */ }
            else if (t < 0.40)
            {
                if (step % 3 == 0) Native.Wheel(hwnd, cx, cy, t < 0.28 ? -1 : 1);
                else Native.Move(hwnd, cx, cy);
            }
            else if (t < 0.70)
            {
                double u = (t - 0.40) / 0.30 * 6;
                int x = (int)(w * (0.1 + 0.8 * Math.Abs(u % 2 - 1))), y = (int)(h * (0.08 + 0.84 * ((u * 3) % 1)));
                Native.Move(hwnd, x, y);
            }
            else if (t < 0.92)
            {
                if (step % 20 == 0) Native.Click(hwnd, (int)(w * (0.15 + 0.7 * ((step / 20 * 0.37) % 1))), (int)(h * (0.1 + 0.8 * ((step / 20 * 0.61) % 1))));
                else if (step % 20 == 10) Native.Key(hwnd, 0x1B);
                else if (step % 20 == 15) foreach (char c in "gotx") Native.Post(hwnd, Native.WM_CHAR, c, 0);
            }
            else if (step % 4 == 0) Native.Move(hwnd, cx, cy);
            step++;
            Thread.Sleep(16);
        }
    }

    static void Kill(Process p) { try { p.Kill(true); } catch (InvalidOperationException) { } }

    /// <summary>Sample executables sit beside this tool's output (artifacts layout bin/Name/debug) or in their own bin folders.</summary>
    static string? Resolve(string what)
    {
        if (File.Exists(what)) return Path.GetFullPath(what);
        string sample = what switch { "shadcn" => "Shadcn", "gallery" => "Components", _ => what };
        string @base = AppContext.BaseDirectory;
        string[] candidates =
        [
            Path.Combine(@base, "..", sample, Path.GetFileName(Path.TrimEndingDirectorySeparator(@base)), sample + ".exe"),
            Path.Combine(@base, "..", "..", sample, "debug", sample + ".exe"),
            Path.Combine(@base, "..", "..", sample, "release", sample + ".exe"),
        ];
        return candidates.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
    }

    static string? Arg(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
