using System.Diagnostics;
using System.Text;

namespace GotX;

/// <summary>Mức chi tiết của nhật ký hiệu năng. <c>GOTX_LOG_LEVEL</c>: slow | frames | trace.</summary>
public enum PerfLogLevel : byte
{
    Off,
    /// <summary>Chỉ frame chậm (&gt; <see cref="PerfLog.SlowFrameMs"/>) hoặc input→frame trễ, cùng khởi động/cảnh báo/sự kiện.</summary>
    Slow,
    /// <summary>Mỗi frame một dòng (top 5 view được render).</summary>
    Frames,
    /// <summary>Như Frames, thêm đủ tên view render và toàn bộ nguyên nhân bẩn.</summary>
    Trace,
}

/// <summary>
/// Nhật ký hiệu năng JSON Lines (mỗi dòng một bản ghi) với chi phí gần bằng 0: khi tắt, Window chỉ so sánh null; khi bật,
/// bản ghi được viết tay thẳng vào bộ đệm byte dựng sẵn (không cấp phát) và một luồng nền ghi ra file theo lô.
/// Bật bằng biến môi trường <c>GOTX_LOG=&lt;đường dẫn&gt;</c> (+ <c>GOTX_LOG_LEVEL</c>) hoặc gán <see cref="Window.Log"/> / <see cref="Shared"/>.
/// Cách đọc: <c>tools/GotX.Perf</c>; định dạng bản ghi: <c>docs/performance.md</c>.
/// </summary>
public sealed class PerfLog : IDisposable
{
    const int BufferSize = 512 << 10, RecordRoom = 64 << 10;
    static PerfLog? _shared;
    static bool _sharedInit;

    /// <summary>Nhật ký chung của tiến trình: tạo lười từ <c>GOTX_LOG</c> (null nếu biến không đặt). Có thể gán lại bằng API.</summary>
    public static PerfLog? Shared
    {
        get
        {
            if (!_sharedInit)
            {
                _sharedInit = true;
                string? path = Environment.GetEnvironmentVariable("GOTX_LOG");
                if (!string.IsNullOrWhiteSpace(path))
                {
                    var level = Environment.GetEnvironmentVariable("GOTX_LOG_LEVEL")?.Trim().ToLowerInvariant() switch
                    {
                        "slow" => PerfLogLevel.Slow, "trace" => PerfLogLevel.Trace, _ => PerfLogLevel.Frames,
                    };
                    try { _shared = new PerfLog(path, level); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
            }
            return _shared;
        }
        set { _sharedInit = true; _shared = value; }
    }

    readonly FileStream _file;
    readonly Thread _writer;
    readonly object _gate = new();
    readonly long _start = Stopwatch.GetTimestamp();
    readonly AutoResetEvent _wake = new(false);
    byte[] _front = new byte[BufferSize];
    byte[]? _pending, _spare = new byte[BufferSize];
    int _len, _pendingLen;
    volatile bool _stop;
    long _dropped;
    bool _first = true;

    public PerfLog(string path, PerfLogLevel level = PerfLogLevel.Frames, double slowFrameMs = 1000.0 / 60 + 0.1)
    {
        Level = level;
        SlowFrameMs = slowFrameMs;
        Path = System.IO.Path.GetFullPath(path);
        if (System.IO.Path.GetDirectoryName(Path) is { Length: > 0 } dir) Directory.CreateDirectory(dir);
        _file = new FileStream(Path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete, 1 << 16);
        _writer = new Thread(WriterLoop) { IsBackground = true, Name = "GotX.PerfLog" };
        _writer.Start();
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Dispose();
    }

    /// <summary><see cref="PerfLogLevel.Off"/> = không ghi frame.</summary>
    public PerfLogLevel Level { get; set; }
    /// <summary>Frame lâu hơn ngưỡng này (ms) bị đánh dấu chậm.</summary>
    public double SlowFrameMs { get; set; }
    /// <summary>Input→frame lâu hơn ngưỡng này (ms) bị đánh dấu trễ.</summary>
    public double SlowLatencyMs { get; set; } = 50;
    public string Path { get; }
    /// <summary>Số bản ghi bị bỏ vì luồng ghi không theo kịp (không bao giờ chặn luồng UI).</summary>
    public long Dropped => _dropped;

    /// <summary>Giây kể từ lúc mở nhật ký.</summary>
    public double Wall => (Stopwatch.GetTimestamp() - _start) / (double)Stopwatch.Frequency;

    // ---- Bản ghi rời rạc ----

    /// <summary>Mốc khởi động (nạp font, frame đầu…): <c>{"t":"start","what":…,"ms":…}</c>.</summary>
    public void Startup(string what, double ms)
    {
        Begin("start"); Str("what", what); Dbl("ms", ms); End();
    }

    /// <summary>Cảnh báo (atlas reset, cache bị xoá và vì sao, effect-loop guard…): <c>{"t":"warn","kind":…,"msg":…}</c>.</summary>
    public void Warn(string kind, string message, int frame = 0)
    {
        Begin("warn"); Str("kind", kind); if (frame > 0) Int("n", frame); Str("msg", message); End();
    }

    /// <summary>Đánh dấu tuỳ ý do app/harness đặt (pha của kịch bản…): <c>{"t":"mark","name":…}</c>.</summary>
    public void Mark(string name, string? detail = null)
    {
        Begin("mark"); Str("name", name); if (detail is not null) Str("detail", detail); End();
    }

    // ---- Bộ viết bản ghi: Begin … End trên cùng luồng; Begin giữ khoá tới End ----

    internal void Begin(string type)
    {
        Monitor.Enter(_gate);
        if (_len + RecordRoom > _front.Length)
        {
            Swap();
            // Luồng ghi chưa trả bộ đệm: bỏ bản ghi thay vì chặn luồng UI.
            if (_len + RecordRoom > _front.Length) { _len = 0; _dropped++; }
        }
        _first = true;
        Raw("{\"t\":\""); Raw(type); Raw("\",\"wall\":"); DblRaw(Wall);
        _first = false;
    }

    internal void End()
    {
        try
        {
            Put((byte)'}'); Put((byte)'\n');
            if (_len > BufferSize - 2 * RecordRoom) Swap();
        }
        finally { Monitor.Exit(_gate); }
    }

    void Swap()
    {
        if (_len == 0 || _pending is not null || _spare is null) return; // luồng ghi đang bận → thử lại lần sau
        _pending = _front; _pendingLen = _len;
        _front = _spare; _spare = null; _len = 0;
        _wake.Set();
    }

    void WriterLoop()
    {
        while (true)
        {
            bool stopping = _stop;
            byte[]? buf; int len;
            lock (_gate)
            {
                // Hết hạn chờ mà bộ đệm chưa đầy: lấy luôn phần đang ghi dở để dòng mới không bị giữ lại.
                if (_pending is null && _len > 0) Swap();
                buf = _pending; len = _pendingLen;
            }
            if (buf is not null)
            {
                try { _file.Write(buf, 0, len); } catch (IOException) { }
                lock (_gate) { _spare = buf; _pending = null; }
            }
            else
            {
                try { _file.Flush(); } catch (IOException) { }
                if (stopping) return;
                _wake.WaitOne(250);
            }
        }
    }

    /// <summary>Ghi hết bản ghi còn trong bộ đệm ra file (chặn luồng gọi tới khi xong, tối đa ~2 s).</summary>
    public void Flush()
    {
        long deadline = Stopwatch.GetTimestamp() + 2 * Stopwatch.Frequency;
        while (Stopwatch.GetTimestamp() < deadline && !_stop)
        {
            lock (_gate) { if (_len == 0 && _pending is null) break; Swap(); }
            _wake.Set();
            Thread.Sleep(2);
        }
    }

    public void Dispose()
    {
        if (_stop) return;
        Flush();
        _stop = true;
        _wake.Set();
        _writer.Join(1000);
        try { _file.Dispose(); } catch (IOException) { }
        if (ReferenceEquals(_shared, this)) _shared = null;
    }

    // ---- Phần tử JSON ----

    void Put(byte b) => _front[_len++] = b;

    void Raw(string s) { foreach (char c in s) _front[_len++] = (byte)c; }

    void Key(string key)
    {
        if (!_first) Put((byte)',');
        _first = false;
        Put((byte)'"'); Raw(key); Put((byte)'"'); Put((byte)':');
    }

    internal void Int(string key, long v) { Key(key); IntRaw(v); }
    internal void Dbl(string key, double v) { Key(key); DblRaw(v); }
    internal void Bool(string key, bool v) { Key(key); Raw(v ? "true" : "false"); }
    internal void Str(string key, string value) { Key(key); StrRaw(value); }

    /// <summary>Mở mảng <c>"key":[</c>; thêm phần tử bằng <see cref="ItemStr"/>/<see cref="ItemInt"/>, đóng bằng <see cref="EndArray"/>.</summary>
    internal void BeginArray(string key) { Key(key); Put((byte)'['); _first = true; }
    internal void EndArray() { Put((byte)']'); _first = false; }
    internal void ItemStr(string v) { if (!_first) Put((byte)','); _first = false; StrRaw(v); }
    internal void ItemInt(long v) { if (!_first) Put((byte)','); _first = false; IntRaw(v); }

    void IntRaw(long v)
    {
        if (v < 0) { Put((byte)'-'); v = -v; }
        Span<byte> tmp = stackalloc byte[20];
        int n = 0;
        do { tmp[n++] = (byte)('0' + v % 10); v /= 10; } while (v > 0);
        while (n > 0) Put(tmp[--n]);
    }

    void DblRaw(double d)
    {
        if (double.IsNaN(d) || double.IsInfinity(d)) { Put((byte)'0'); return; }
        long scaled = (long)Math.Round(d * 1000);
        if (scaled < 0) { Put((byte)'-'); scaled = -scaled; }
        IntRaw(scaled / 1000);
        long f = scaled % 1000;
        if (f == 0) return;
        Put((byte)'.');
        Put((byte)('0' + f / 100)); Put((byte)('0' + f / 10 % 10)); Put((byte)('0' + f % 10));
    }

    void StrRaw(string s)
    {
        Put((byte)'"');
        if (s.Length > 160) s = s[..160];
        foreach (char c in s)
        {
            if (c >= 0x80) _len += Encoding.UTF8.GetBytes(new ReadOnlySpan<char>(in c), _front.AsSpan(_len));
            else if (c == '"' || c == '\\') { Put((byte)'\\'); Put((byte)c); }
            else Put(c < 0x20 ? (byte)' ' : (byte)c);
        }
        Put((byte)'"');
    }
}
