using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using GotX.Raster;
using GotX.Text;
using static GotX.Platform.Windows.Win32;

namespace GotX.Platform.Windows;

/// <summary>
/// Cửa sổ Win32: nhận message (chuột, phím, IME), vẽ Scene bằng D3D11 hoặc CPU.
/// </summary>
internal sealed unsafe partial class Win32Window : IPlatformWindow, IFrameTimings
{
    public double BuildMs { get; private set; }
    public double SubmitMs { get; private set; }
    public double PresentMs { get; private set; }
    public double GpuMs => _gpu?.GpuMs ?? 0;
    public long UploadBytes { get; private set; }
    public double InstMs { get; private set; }
    public double MapMs { get; private set; }

    static readonly Dictionary<nint, Win32Window> s_windows = new();
    static nint s_className;

    readonly nint _hwnd;
    readonly Canvas _canvas = new();
    D3D11Renderer? _gpu;
    // Vùng hỏng (prototype, mặc định tắt): GOTX_DAMAGE=1 bật cho cả renderer GPU lẫn CPU. Xem SceneDamage.
    static readonly bool s_damageOn = Environment.GetEnvironmentVariable("GOTX_DAMAGE") == "1";
    SceneDamage? _damage;
    bool _trackingLeave;
    char _highSurrogate;

    public event Action<InputEvent>? Input;
    public event Action? Frame;
    public event Action? Closed;
    public Func<Bounds?>? ImeCaret { private get; set; }
    public Func<CursorKind>? CursorProvider { private get; set; }
    static readonly nint s_arrow = LoadCursorW(0, IDC_ARROW), s_hand = LoadCursorW(0, IDC_HAND), s_ibeam = LoadCursorW(0, IDC_IBEAM);

    public Win32Window(WindowOptions o, bool preferGpu)
    {
        EnsureClass();
        nint inst = GetModuleHandle(null);
        fixed (char* title = o.Title)
            _hwnd = CreateWindowExW(0, (char*)s_className, title, WS_OVERLAPPEDWINDOW,
                CW_USEDEFAULT, CW_USEDEFAULT, 100, 100, 0, 0, inst, 0);
        if (_hwnd == 0) throw new InvalidOperationException("CreateWindowExW thất bại.");
        s_windows[_hwnd] = this;

        // Kích thước vùng client = logical px × DPI của màn hình chứa cửa sổ.
        uint dpi = GetDpiForWindow(_hwnd);
        var r = new RECT { right = (int)(o.Width * dpi / 96f), bottom = (int)(o.Height * dpi / 96f) };
        AdjustWindowRectExForDpi(&r, WS_OVERLAPPEDWINDOW, 0, 0, dpi);
        SetWindowPos(_hwnd, 0, 0, 0, r.right - r.left, r.bottom - r.top, SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);

        if (preferGpu)
        {
            RECT c;
            GetClientRect(_hwnd, &c);
            _gpu = D3D11Renderer.TryCreate(_hwnd, c.right, c.bottom);
            if (s_damageOn && _gpu is not null) _gpu.Damage = new SceneDamage();
            PerfLog.Shared?.Mark("renderer", _gpu is null ? "software" : _gpu.IsWarp ? "d3d11-warp" : "d3d11-hardware");
        }
        ShowWindow(_hwnd, SW_SHOW);
    }

    public bool UsesGpu => _gpu is not null;

    static void EnsureClass()
    {
        if (s_className != 0) return;
        s_className = Marshal.StringToHGlobalUni("GotXWindow");
        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)sizeof(WNDCLASSEXW),
            style = CS_HREDRAW | CS_VREDRAW,
            lpfnWndProc = (nint)(delegate* unmanaged<nint, uint, nint, nint, nint>)&WndProc,
            hInstance = GetModuleHandle(null),
            hCursor = LoadCursorW(0, IDC_ARROW),
            lpszClassName = s_className,
        };
        if (RegisterClassExW(&wc) == 0) throw new InvalidOperationException("RegisterClassExW thất bại.");
    }

    public float ScaleFactor => GetDpiForWindow(_hwnd) / 96f;

    public Size LogicalSize
    {
        get
        {
            RECT r;
            GetClientRect(_hwnd, &r);
            float s = ScaleFactor;
            return new Size(r.right / s, r.bottom / s);
        }
    }

    public void RequestFrame() => InvalidateRect(_hwnd, null, 0);

    bool _animTimer;

    /// <summary>Hẹn frame sau ~16ms bằng WM_TIMER (renderer D3D11 còn chờ vsync khi Present).</summary>
    public void RequestAnimationFrame()
    {
        if (_animTimer) return;
        _animTimer = true;
        SetTimer(_hwnd, 1, 15, 0);
    }

    public void Draw(Scene scene)
    {
        RECT r;
        GetClientRect(_hwnd, &r);
        if (r.right <= 0 || r.bottom <= 0) return;
        if (_gpu is not null)
        {
            if (!_gpu.Render(scene, r.right, r.bottom) && _gpu.Failed)
            {
                // Dựng lại thiết bị thất bại nhiều lần liên tiếp: bỏ GPU, vẽ bằng CPU từ frame này (cửa sổ vẫn sống).
                _gpu.Dispose(); _gpu = null;
                GpuCounters.SoftwareFallbacks++; GpuCounters.Last = default;
                PerfLog.Shared?.Mark("renderer", "software-fallback");
                InvalidateRect(_hwnd, null, 0);
                return;
            }
            InstMs = _gpu.InstMs; MapMs = _gpu.MapMs; BuildMs = _gpu.BuildMs; SubmitMs = _gpu.SubmitMs; PresentMs = _gpu.PresentMs; UploadBytes = _gpu.UploadBytes;
            return;
        }
        long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        _canvas.Resize(r.right, r.bottom);
        if (s_damageOn) SoftwareRenderer.RenderDamaged(scene, _canvas, (_damage ??= new SceneDamage()).Update(scene, r.right, r.bottom));
        else SoftwareRenderer.Render(scene, _canvas);
        BuildMs = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        SubmitMs = PresentMs = 0; UploadBytes = 0;
    }

    [UnmanagedCallersOnly]
    static nint WndProc(nint hwnd, uint msg, nint wp, nint lp)
    {
        try
        {
            if (s_windows.TryGetValue(hwnd, out var w)) return w.Handle(msg, wp, lp);
        }
        catch (Exception ex)
        {
            // Exception không được thoát ra khỏi callback native, nếu không process sẽ bị huỷ.
            WindowsPlatform.ReportException(ex);
        }
        return DefWindowProcW(hwnd, msg, wp, lp);
    }

    void Blit(nint hdc)
    {
        if (_canvas.Width == 0) return;
        var bi = new BITMAPINFOHEADER
        {
            biSize = (uint)sizeof(BITMAPINFOHEADER),
            biWidth = _canvas.Width,
            biHeight = -_canvas.Height, // âm = top-down
            biPlanes = 1,
            biBitCount = 32,
        };
        fixed (uint* bits = _canvas.Pixels)
            SetDIBitsToDevice(hdc, 0, 0, (uint)_canvas.Width, (uint)_canvas.Height, 0, 0, 0,
                (uint)_canvas.Height, bits, &bi, 0);
    }
}
