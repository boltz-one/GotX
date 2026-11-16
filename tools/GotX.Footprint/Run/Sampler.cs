using System.Diagnostics;

namespace GotX.Footprint;

/// <summary>Working set / private bytes of the host from outside, every 250 ms during the measured phase: start, peak, end.</summary>
sealed class Sampler(Process proc)
{
    long _next;
    bool _on;
    public long WsStart, WsPeak, WsEnd, PrivStart, PrivPeak, PrivEnd;

    public void Begin()
    {
        proc.Refresh();
        WsStart = WsPeak = WsEnd = proc.WorkingSet64;
        PrivStart = PrivPeak = PrivEnd = proc.PrivateMemorySize64;
        _on = true;
        _next = Environment.TickCount64 + 250;
    }

    public void Poll() => Poll(false);

    public void Poll(bool force)
    {
        if (!_on || (!force && Environment.TickCount64 < _next)) return;
        _next = Environment.TickCount64 + 250;
        proc.Refresh();
        WsEnd = proc.WorkingSet64; PrivEnd = proc.PrivateMemorySize64;
        WsPeak = Math.Max(WsPeak, WsEnd); PrivPeak = Math.Max(PrivPeak, PrivEnd);
    }
}
