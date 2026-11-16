using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace GotX.Footprint;

/// <summary>A running host child: stdout lines go to a queue, <see cref="Ask"/> sends a one-byte command and waits for its reply.</summary>
sealed class HostProcess : IDisposable
{
    readonly BlockingCollection<string> _lines = new();
    public Process Proc { get; }
    public nint Hwnd { get; private set; }
    public double FirstFrameMs { get; private set; } = -1;

    public HostProcess(string exe, string[] baseArgs, string scenario, string renderer)
    {
        var psi = new ProcessStartInfo(exe) { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, WorkingDirectory = Path.GetDirectoryName(exe)! };
        foreach (var a in baseArgs) psi.ArgumentList.Add(a);
        psi.ArgumentList.Add("host"); psi.ArgumentList.Add(scenario);
        psi.ArgumentList.Add("--renderer"); psi.ArgumentList.Add(renderer);
        Proc = Process.Start(psi)!;
        new Thread(() => { string? l; while ((l = Proc.StandardOutput.ReadLine()) is not null) _lines.Add(l); _lines.CompleteAdding(); }) { IsBackground = true }.Start();
    }

    /// <summary>Waits for the window and the first present; false when the host never got there.</summary>
    public bool WaitReady(int timeoutMs)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs && !Proc.HasExited)
        {
            if (Hwnd == 0) Hwnd = Native.FindWindow((uint)Proc.Id);
            if (_lines.TryTake(out var l, 20) && l.StartsWith("FIRST ", StringComparison.Ordinal))
            {
                FirstFrameMs = double.Parse(l.AsSpan(6), CultureInfo.InvariantCulture);
                if (Hwnd == 0) Hwnd = Native.FindWindow((uint)Proc.Id);
                return Hwnd != 0;
            }
        }
        return false;
    }

    /// <summary>'s' = counters, 'h' = collect + counters. Returns key=value pairs.</summary>
    public Dictionary<string, double> Ask(char cmd)
    {
        Proc.StandardInput.BaseStream.WriteByte((byte)cmd);
        Proc.StandardInput.BaseStream.Flush();
        string tag = cmd == 'h' ? "H " : "S ";
        while (_lines.TryTake(out var l, 20000))
        {
            if (!l.StartsWith(tag, StringComparison.Ordinal)) continue;
            var d = new Dictionary<string, double>();
            foreach (var kv in l[2..].Split(' ')) { int e = kv.IndexOf('='); d[kv[..e]] = double.Parse(kv.AsSpan(e + 1), CultureInfo.InvariantCulture); }
            return d;
        }
        throw new TimeoutException("host did not answer '" + cmd + "'");
    }

    public void Dispose()
    {
        try { if (Hwnd != 0) Native.Post(Hwnd, Native.WM_CLOSE, 0, 0); if (!Proc.WaitForExit(3000)) Proc.Kill(true); }
        catch (InvalidOperationException) { }
        Proc.Dispose();
    }
}
