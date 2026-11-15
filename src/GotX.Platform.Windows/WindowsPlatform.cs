using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using GotX.Raster;
using GotX.Text;
using static GotX.Platform.Windows.Win32;

namespace GotX.Platform.Windows;

public sealed unsafe class WindowsPlatform : IPlatform
{
    readonly ConcurrentQueue<Action> _mainQueue = new();
    readonly bool _preferGpu;
    uint _mainThread;

    /// <param name="preferGpu">D3D11 nếu được (mặc định). Biến môi trường GOTX_RENDERER=software ép dùng CPU.</param>
    public WindowsPlatform(bool preferGpu = true)
    {
        _preferGpu = preferGpu && !string.Equals(Environment.GetEnvironmentVariable("GOTX_RENDERER"), "software", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Nhận exception ném ra trong lúc xử lý message (input, vẽ, effect). Chưa gán thì ghi ra stderr –
    /// app WinExe không có console nên hãy gán để ghi log hoặc hiện hộp thoại, tránh nuốt lỗi im lặng.
    /// </summary>
    public static Action<Exception>? UnhandledException { get; set; }

    internal static void ReportException(Exception ex)
    {
        if (UnhandledException is { } handler)
        {
            try { handler(ex); return; }
            catch (Exception inner) { Console.Error.WriteLine(inner); }
        }
        Console.Error.WriteLine(ex);
    }

    public void Run(Action onLaunch)
    {
        SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        _mainThread = GetCurrentThreadId();
        onLaunch();

        MSG msg;
        while (GetMessageW(&msg, 0, 0, 0) > 0)
        {
            if (msg.hwnd == 0 && msg.message == WM_APP) { Drain(); continue; }
            TranslateMessage(&msg); // sinh WM_CHAR từ WM_KEYDOWN
            DispatchMessageW(&msg);
        }
    }

    void Drain()
    {
        while (_mainQueue.TryDequeue(out var a))
        {
            // Một việc Post lỗi không được làm sập vòng lặp message hay bỏ rơi các việc còn lại.
            try { a(); }
            catch (Exception ex) { ReportException(ex); }
        }
    }

    public IClipboard Clipboard { get; } = new WindowsClipboard();

    public void PostToMain(Action action)
    {
        _mainQueue.Enqueue(action);
        PostThreadMessageW(_mainThread, WM_APP, 0, 0);
    }

    public IPlatformWindow OpenWindow(WindowOptions options) => new Win32Window(options, _preferGpu);

    public void RegisterDefaultFonts(TextSystem text)
    {
        string dir = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        text.TryAddFont(Path.Combine(dir, "segoeui.ttf"), FontWeight.Normal);
        text.TryAddFont(Path.Combine(dir, "seguisb.ttf"), FontWeight.Semibold);
        text.TryAddFont(Path.Combine(dir, "segoeuib.ttf"), FontWeight.Bold);
        if (!text.HasFonts) text.TryAddFont(Path.Combine(dir, "arial.ttf"), FontWeight.Normal);
        text.TryAddFont(Path.Combine(dir, "segoeuii.ttf"), FontWeight.Normal, FontFamily.Sans, italic: true);
        text.TryAddFont(Path.Combine(dir, "segoeuiz.ttf"), FontWeight.Bold, FontFamily.Sans, italic: true);
        text.TryAddFont(Path.Combine(dir, "consola.ttf"), FontWeight.Normal, FontFamily.Mono);
        text.TryAddFont(Path.Combine(dir, "consolab.ttf"), FontWeight.Bold, FontFamily.Mono);
        text.TryAddFont(Path.Combine(dir, "consolai.ttf"), FontWeight.Normal, FontFamily.Mono, italic: true);
    }
}
