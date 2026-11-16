using System.Diagnostics.Tracing;

namespace GotX.Stress;

/// <summary>
/// In-process runtime event listener (<c>GCAllocationTick</c>, one event per ~100 KB allocated, tagged with the type being allocated):
/// tells which types the allocation volume of a scenario is made of. Used with STRESS_ALLOC=1; sampling, so only the
/// big contributors are reliable.
/// </summary>
sealed class AllocListener : EventListener
{
    readonly Dictionary<string, long> _bytes = new();
    long _total;

    protected override void OnEventSourceCreated(EventSource source)
    {
        // Keyword 0x1 = GC, level Verbose enables GCAllocationTick.
        if (source.Name == "Microsoft-Windows-DotNETRuntime") EnableEvents(source, EventLevel.Verbose, (EventKeywords)0x1);
    }

    protected override void OnEventWritten(EventWrittenEventArgs e)
    {
        if (e.EventName is null || !e.EventName.StartsWith("GCAllocationTick", StringComparison.Ordinal) || e.Payload is null) return;
        int nameIndex = e.PayloadNames?.IndexOf("TypeName") ?? -1;
        int sizeIndex = e.PayloadNames?.IndexOf("AllocationAmount64") ?? -1;
        string type = nameIndex >= 0 ? e.Payload[nameIndex]?.ToString() ?? "?" : "?";
        long amount = sizeIndex >= 0 && e.Payload[sizeIndex] is ulong u ? (long)u : 100_000;
        lock (_bytes) { _bytes[type] = _bytes.GetValueOrDefault(type) + amount; _total += amount; }
    }

    /// <summary>Resets the counters and returns what had been collected since the previous call.</summary>
    public string Drain(int top = 12)
    {
        lock (_bytes)
        {
            var lines = _bytes.OrderByDescending(x => x.Value).Take(top).Select(x => $"    {x.Value / 1024,9:N0} KB  {x.Key}");
            string s = $"  allocated ~{_total / 1024:N0} KB (sampled)\n" + string.Join('\n', lines);
            _bytes.Clear(); _total = 0;
            return s;
        }
    }
}
