using GotX.Text;

namespace GotX;

public readonly record struct WindowOptions(string Title, float Width, float Height);

/// <summary>Clipboard dạng văn bản (Ctrl+C / Ctrl+X / Ctrl+V trong ô nhập).</summary>
public interface IClipboard
{
    string? GetText();
    void SetText(string text);
}

/// <summary>Clipboard trong bộ nhớ của tiến trình: dùng khi chạy không có platform (headless).</summary>
public sealed class MemoryClipboard : IClipboard
{
    string? _text;
    public string? GetText() => _text;
    public void SetText(string text) => _text = text;
}

/// <summary>Ranh giới core ↔ hệ điều hành. Windows hôm nay, macOS sau.</summary>
public interface IPlatform
{
    void Run(Action onLaunch);
    IPlatformWindow OpenWindow(WindowOptions options);
    void PostToMain(Action action);
    void RegisterDefaultFonts(TextSystem text);
    IClipboard Clipboard { get; }
}

public interface IPlatformWindow
{
    Size LogicalSize { get; }
    float ScaleFactor { get; }
    void RequestFrame();

    /// <summary>Hẹn frame tiếp theo cho animation (~16ms, theo nhịp màn hình), không vẽ dồn dập.</summary>
    void RequestAnimationFrame();
    event Action<InputEvent>? Input;
    event Action? Frame;

    /// <summary>Cửa sổ đã bị đóng: không còn Frame/Input nào nữa.</summary>
    event Action? Closed;

    /// <summary>Renderer của platform (D3D11 hoặc CPU trên Windows, Metal sau này) vẽ Scene.</summary>
    void Draw(Scene scene);

    /// <summary>Trả về vị trí con trỏ nhập liệu (logical px) để đặt cửa sổ gợi ý của IME.</summary>
    Func<Bounds?>? ImeCaret { set; }

    /// <summary>Con trỏ chuột cần hiển thị (cursor-pointer, cursor-text…).</summary>
    Func<CursorKind>? CursorProvider { set; }
}

/// <summary>Tuỳ chọn: cửa sổ platform báo thời gian của lần <see cref="IPlatformWindow.Draw"/> gần nhất cho nhật ký hiệu năng (ms).</summary>
public interface IFrameTimings
{
    /// <summary>CPU: raster Scene. GPU: dựng instance + tải atlas/ảnh + map buffer.</summary>
    double BuildMs { get; }
    /// <summary>GPU: ghi lệnh vẽ (CPU side).</summary>
    double SubmitMs { get; }
    /// <summary>GPU: Present (chờ vsync).</summary>
    double PresentMs { get; }
    /// <summary>GPU: thời gian thực thi trên GPU của frame trước (timestamp query), 0 nếu không đo được.</summary>
    double GpuMs { get; }
    long UploadBytes { get; }
}

/// <summary>Nối platform với Window: frame → Draw → renderer; input → dispatch → effect → vẽ lại.</summary>
public static class Application
{
    public static void Run(IPlatform platform, WindowOptions options, Func<App, View> root)
    {
        platform.Run(() =>
        {
            var log = PerfLog.Shared;
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            if (log is not null)
                log.Startup("process-to-launch", (DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalMilliseconds);
            var app = new App(platform);
            var text = new TextSystem();
            platform.RegisterDefaultFonts(text);
            log?.Startup("fonts", (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
            app.Text = text;
            OpenWindow(app, options, root(app));
        });
    }

    /// <summary>Mở một cửa sổ của platform và nối nó với một <see cref="Window"/> mới. Các cửa sổ dùng chung TextSystem của app.</summary>
    internal static Window OpenWindow(App app, WindowOptions options, View root)
    {
        var platform = app.Platform ?? throw new InvalidOperationException("App không có platform: không mở được cửa sổ.");
        var text = app.Text ?? throw new InvalidOperationException("App.Text chưa được gán (hãy chạy qua Application.Run).");
        var pw = platform.OpenWindow(options);
        var window = new Window(app, text) { Root = root };
        window.FrameRequested += pw.RequestFrame;
        pw.ImeCaret = () => window.ImeCaretRect;
        pw.CursorProvider = () => window.CursorAtMouse;
        pw.Frame += () =>
        {
            var scene = window.Draw(pw.LogicalSize, pw.ScaleFactor);
            long t0 = window.Log is null ? 0 : System.Diagnostics.Stopwatch.GetTimestamp();
            pw.Draw(scene);
            if (window.Log is not null)
            {
                double ms = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
                if (pw is IFrameTimings ft) window.LogPresent(ms, ft.BuildMs, ft.SubmitMs, ft.PresentMs, ft.GpuMs, ft.UploadBytes);
                else window.LogPresent(ms);
            }
            if (window.WantsAnimationFrame) pw.RequestAnimationFrame();
        };
        pw.Input += e => window.DispatchInput(e);
        pw.Closed += () => app.RemoveWindow(window);
        pw.RequestFrame();
        return window;
    }
}
