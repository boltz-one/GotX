using System.Runtime.InteropServices;
using System.Text;

namespace GotX.Footprint;

/// <summary>Win32 used by the parent: find the host window, post input, resize, GUI-object counts, PrintWindow capture (same approach as tools/GotX.Perf).</summary>
static unsafe class Native
{
    public const uint WM_CLOSE = 0x10, WM_KEYDOWN = 0x100, WM_KEYUP = 0x101, WM_MOUSEMOVE = 0x200, WM_LBUTTONDOWN = 0x201, WM_LBUTTONUP = 0x202, WM_MOUSEWHEEL = 0x20A;

    delegate bool EnumProc(nint hwnd, nint lp);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, nint lp);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(nint hwnd, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassNameW(nint hwnd, StringBuilder sb, int max);
    [DllImport("user32.dll")] static extern bool GetClientRect(nint hwnd, out RECT r);
    [DllImport("user32.dll")] static extern bool GetWindowRect(nint hwnd, out RECT r);
    [DllImport("user32.dll")] static extern bool ClientToScreen(nint hwnd, ref POINT p);
    [DllImport("user32.dll")] static extern bool PostMessageW(nint hwnd, uint msg, nint wp, nint lp);
    [DllImport("user32.dll")] static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] static extern uint GetGuiResources(nint process, uint flags);
    [DllImport("user32.dll")] static extern bool SetProcessDpiAwarenessContext(nint ctx);
    [DllImport("user32.dll")] static extern bool PrintWindow(nint hwnd, nint hdc, uint flags);
    [DllImport("user32.dll")] static extern nint GetDC(nint hwnd);
    [DllImport("user32.dll")] static extern int ReleaseDC(nint hwnd, nint hdc);
    [DllImport("gdi32.dll")] static extern nint CreateCompatibleDC(nint hdc);
    [DllImport("gdi32.dll")] static extern nint CreateDIBSection(nint hdc, ref BITMAPINFO bi, uint usage, out nint bits, nint section, uint offset);
    [DllImport("gdi32.dll")] static extern nint SelectObject(nint hdc, nint obj);
    [DllImport("gdi32.dll")] static extern bool DeleteObject(nint obj);
    [DllImport("gdi32.dll")] static extern bool DeleteDC(nint hdc);

    [StructLayout(LayoutKind.Sequential)] struct RECT { public int left, top, right, bottom; }
    [StructLayout(LayoutKind.Sequential)] struct POINT { public int x, y; }
    [StructLayout(LayoutKind.Sequential)]
    struct BITMAPINFO { public uint biSize; public int biWidth, biHeight; public ushort biPlanes, biBitCount; public uint biCompression, biSizeImage; public int x, y; public uint cu, ci; }

    public static void DpiAware() => SetProcessDpiAwarenessContext(-4); // per-monitor v2, so window sizes mean physical pixels like the host's

    public static nint FindWindow(uint pid)
    {
        nint found = 0;
        EnumWindows((h, _) =>
        {
            GetWindowThreadProcessId(h, out uint p);
            if (p != pid) return true;
            var cls = new StringBuilder(128);
            GetClassNameW(h, cls, 128);
            string c = cls.ToString();
            if (c.Contains("Console", StringComparison.OrdinalIgnoreCase) || c.StartsWith("IME", StringComparison.Ordinal) || c.StartsWith("MSCTFIME", StringComparison.Ordinal)) return true;
            if (!GetClientRect(h, out var r) || r.right < 200) return true;
            found = h;
            return false;
        }, 0);
        return found;
    }

    public static (int W, int H) ClientSize(nint hwnd) => GetClientRect(hwnd, out var r) ? (r.right, r.bottom) : (0, 0);
    public static (int W, int H) OuterSize(nint hwnd) => GetWindowRect(hwnd, out var r) ? (r.right - r.left, r.bottom - r.top) : (0, 0);

    /// <summary>Synchronous (the call returns once the window's thread handled WM_SIZE), like a drag step.</summary>
    public static void Resize(nint hwnd, int w, int h) => SetWindowPos(hwnd, 0, 0, 0, w, h, 0x0002 | 0x0004 | 0x0010); // NOMOVE | NOZORDER | NOACTIVATE

    public static (uint Gdi, uint User) GuiObjects(nint process) => (GetGuiResources(process, 0), GetGuiResources(process, 1));

    public static void Post(nint hwnd, uint msg, nint wp, nint lp) => PostMessageW(hwnd, msg, wp, lp);
    public static void Move(nint hwnd, int x, int y) => Post(hwnd, WM_MOUSEMOVE, 0, Pack(x, y));
    public static void Click(nint hwnd, int x, int y) { Move(hwnd, x, y); Post(hwnd, WM_LBUTTONDOWN, 1, Pack(x, y)); Post(hwnd, WM_LBUTTONUP, 0, Pack(x, y)); }
    public static void Key(nint hwnd, int vk) { Post(hwnd, WM_KEYDOWN, vk, 0); Post(hwnd, WM_KEYUP, vk, unchecked((nint)0xC0000000)); }

    public static void Wheel(nint hwnd, int x, int y, int notches)
    {
        var pt = new POINT { x = x, y = y };
        ClientToScreen(hwnd, ref pt); // the window handler expects screen coordinates in lParam
        Post(hwnd, WM_MOUSEWHEEL, (nint)(((short)(notches * 120) & 0xFFFF) << 16), Pack(pt.x, pt.y));
    }

    static nint Pack(int x, int y) => (nint)(((y & 0xFFFF) << 16) | (x & 0xFFFF));

    /// <summary>PrintWindow capture of the client area (works with D3D11 flip-model windows). BGRA, top-down.</summary>
    public static uint[]? Capture(nint hwnd, out int w, out int h)
    {
        (w, h) = ClientSize(hwnd);
        if (w <= 0 || h <= 0) return null;
        nint screen = GetDC(0), dc = CreateCompatibleDC(screen);
        var bi = new BITMAPINFO { biSize = 40, biWidth = w, biHeight = -h, biPlanes = 1, biBitCount = 32 };
        nint bmp = CreateDIBSection(screen, ref bi, 0, out nint bits, 0, 0);
        SelectObject(dc, bmp);
        bool ok = PrintWindow(hwnd, dc, 3); // CLIENTONLY | RENDERFULLCONTENT
        uint[]? pixels = null;
        if (ok && bits != 0) { pixels = new uint[w * h]; new ReadOnlySpan<uint>((void*)bits, w * h).CopyTo(pixels); }
        DeleteObject(bmp); DeleteDC(dc); ReleaseDC(0, screen);
        return pixels;
    }
}
