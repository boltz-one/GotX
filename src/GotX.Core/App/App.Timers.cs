using System.Diagnostics;

namespace GotX;

/// <summary>
/// Hẹn giờ của app: callback luôn chạy trên main thread, sau đó effect được xử lý như sau một sự kiện input.
/// Có platform thì app tự đánh thức đúng lúc; không có platform (headless, test) thì gọi <see cref="RunTimers"/>
/// sau khi đẩy <see cref="Clock"/> – nhờ vậy test điều khiển được thời gian một cách tất định.
/// </summary>
public sealed partial class App
{
    sealed class TimerEntry(Action callback) : IDisposable
    {
        public readonly Action Callback = callback;
        public double Due, Period;
        public bool Cancelled;
        public void Dispose() => Cancelled = true;
    }

    readonly List<TimerEntry> _timers = new();
    readonly List<TimerEntry> _due = new();
    Timer? _wake;
    bool _runningTimers;

    /// <summary>Đồng hồ (giây) của hẹn giờ. Test có thể thay bằng đồng hồ giả.</summary>
    public Func<double> Clock { get; set; } = static () => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;

    /// <summary>Số hẹn giờ đang chờ.</summary>
    public int PendingTimers
    {
        get { int n = 0; foreach (var t in _timers) if (!t.Cancelled) n++; return n; }
    }

    /// <summary>Gọi <paramref name="callback"/> một lần sau <paramref name="seconds"/> giây. Dispose kết quả để huỷ.</summary>
    public IDisposable SetTimeout(double seconds, Action callback) => AddTimer(seconds, 0, callback);

    /// <summary>Gọi <paramref name="callback"/> lặp lại mỗi <paramref name="seconds"/> giây cho tới khi Dispose.</summary>
    public IDisposable SetInterval(double seconds, Action callback) => AddTimer(seconds, Math.Max(seconds, 0.001), callback);

    TimerEntry AddTimer(double seconds, double period, Action callback)
    {
        var entry = new TimerEntry(callback) { Due = Clock() + Math.Max(0, seconds), Period = period };
        _timers.Add(entry);
        ScheduleWake();
        return entry;
    }

    /// <summary>
    /// Chạy các hẹn giờ đã tới hạn theo <see cref="Clock"/> rồi xử lý effect. Trả về số callback đã gọi.
    /// Platform gọi tự động; code headless/test gọi sau khi đẩy đồng hồ.
    /// </summary>
    public int RunTimers()
    {
        if (_runningTimers) return 0; // callback gọi lại RunTimers: lượt ngoài đang duyệt danh sách tới hạn
        _runningTimers = true;
        double now = Clock();
        _due.Clear();
        for (int i = _timers.Count - 1; i >= 0; i--)
        {
            var t = _timers[i];
            if (t.Cancelled) { _timers.RemoveAt(i); continue; }
            if (t.Due > now) continue;
            _due.Add(t);
            if (t.Period > 0) t.Due = Math.Max(t.Due + t.Period, now); // trễ nhiều chu kỳ thì không gọi dồn
            else _timers.RemoveAt(i);
        }
        _due.Sort(static (a, b) => a.Due.CompareTo(b.Due));
        int fired = 0;
        try
        {
            foreach (var t in _due)
                if (!t.Cancelled) { t.Callback(); fired++; }
        }
        finally
        {
            _runningTimers = false;
            Flush();
            ScheduleWake();
        }
        return fired;
    }

    /// <summary>Đặt lại bộ đánh thức của hệ điều hành theo hẹn giờ gần nhất (chỉ khi có platform).</summary>
    void ScheduleWake()
    {
        if (Platform is null) return;
        double next = double.PositiveInfinity;
        foreach (var t in _timers) if (!t.Cancelled) next = Math.Min(next, t.Due);
        if (double.IsPositiveInfinity(next)) { _wake?.Change(Timeout.Infinite, Timeout.Infinite); return; }
        _wake ??= new Timer(_ => Platform.PostToMain(() => RunTimers()));
        _wake.Change((int)Math.Clamp((next - Clock()) * 1000 + 1, 1, int.MaxValue / 2), Timeout.Infinite);
    }
}