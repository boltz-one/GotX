using System.Runtime.InteropServices;

namespace GotX.Platform.Windows;

/// <summary>
/// Khai báo Win32 tối thiểu. Mọi tham số đều blittable (con trỏ, số nguyên) nên LibraryImport
/// không phải sinh marshalling – gọi thẳng, tương thích Native AOT.
/// </summary>
internal static unsafe partial class Win32
{
    public const uint WM_DESTROY = 0x0002, WM_SIZE = 0x0005, WM_PAINT = 0x000F, WM_ERASEBKGND = 0x0014,
        WM_MOUSEMOVE = 0x0200, WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202,
        WM_RBUTTONDOWN = 0x0204, WM_RBUTTONUP = 0x0205, WM_MBUTTONDOWN = 0x0207, WM_MBUTTONUP = 0x0208,
        WM_MOUSELEAVE = 0x02A3, WM_DPICHANGED = 0x02E0, WM_APP = 0x8000,
        WM_KEYDOWN = 0x0100, WM_CHAR = 0x0102, WM_SYSKEYDOWN = 0x0104,
        WM_IME_STARTCOMPOSITION = 0x010D, WM_IME_ENDCOMPOSITION = 0x010E, WM_IME_COMPOSITION = 0x010F,
        WM_IME_SETCONTEXT = 0x0281, WM_SETCURSOR = 0x0020,
        WM_MOUSEWHEEL = 0x020A, WM_MOUSEHWHEEL = 0x020E, WM_TIMER = 0x0113;
    public const int IDC_HAND = 32649, IDC_IBEAM = 32513, HTCLIENT = 1;
    public const uint GCS_COMPSTR = 0x0008, GCS_RESULTSTR = 0x0800, CFS_POINT = 0x0002, CFS_EXCLUDE = 0x0080;
    public const long ISC_SHOWUICOMPOSITIONWINDOW = 0x80000000L;
    public const int VK_BACK = 0x08, VK_TAB = 0x09, VK_RETURN = 0x0D, VK_SHIFT = 0x10, VK_CONTROL = 0x11, VK_MENU = 0x12,
        VK_ESCAPE = 0x1B, VK_END = 0x23, VK_HOME = 0x24, VK_LEFT = 0x25, VK_UP = 0x26, VK_RIGHT = 0x27,
        VK_DOWN = 0x28, VK_DELETE = 0x2E, VK_A = 0x41, VK_C = 0x43, VK_V = 0x56, VK_X = 0x58;
    public const uint CF_UNICODETEXT = 13, GMEM_MOVEABLE = 0x0002;
    public const uint WS_OVERLAPPEDWINDOW = 0x00CF0000, CS_HREDRAW = 0x2, CS_VREDRAW = 0x1;
    public const int CW_USEDEFAULT = unchecked((int)0x80000000), SW_SHOW = 5, IDC_ARROW = 32512;
    public const uint SWP_NOMOVE = 0x2, SWP_NOZORDER = 0x4, SWP_NOACTIVATE = 0x10, TME_LEAVE = 0x2;
    public static readonly nint DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4;

    [StructLayout(LayoutKind.Sequential)]
    public struct WNDCLASSEXW
    {
        public uint cbSize, style;
        public nint lpfnWndProc;
        public int cbClsExtra, cbWndExtra;
        public nint hInstance, hIcon, hCursor, hbrBackground, lpszMenuName, lpszClassName, hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)] public struct POINT { public int x, y; }
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int left, top, right, bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam, lParam;
        public uint time;
        public POINT pt;
        public uint lPrivate;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PAINTSTRUCT
    {
        public nint hdc;
        public int fErase;
        public RECT rcPaint;
        public int fRestore, fIncUpdate;
        public fixed byte rgbReserved[32];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth, biHeight;
        public ushort biPlanes, biBitCount;
        public uint biCompression, biSizeImage;
        public int biXPelsPerMeter, biYPelsPerMeter;
        public uint biClrUsed, biClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TRACKMOUSEEVENT { public uint cbSize, dwFlags; public nint hwndTrack; public uint dwHoverTime; }

    [StructLayout(LayoutKind.Sequential)]
    public struct COMPOSITIONFORM { public uint dwStyle; public POINT ptCurrentPos; public RECT rcArea; }

    [StructLayout(LayoutKind.Sequential)]
    public struct CANDIDATEFORM { public uint dwIndex, dwStyle; public POINT ptCurrentPos; public RECT rcArea; }

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW")] public static partial nint GetModuleHandle(char* name);
    [LibraryImport("kernel32.dll")] public static partial uint GetCurrentThreadId();

    [LibraryImport("user32.dll")] public static partial ushort RegisterClassExW(WNDCLASSEXW* wc);
    [LibraryImport("user32.dll")]
    public static partial nint CreateWindowExW(uint exStyle, char* cls, char* title, uint style,
        int x, int y, int w, int h, nint parent, nint menu, nint instance, nint param);
    [LibraryImport("user32.dll")] public static partial int ShowWindow(nint hwnd, int cmd);
    [LibraryImport("user32.dll")] public static partial int GetMessageW(MSG* msg, nint hwnd, uint min, uint max);
    [LibraryImport("user32.dll")] public static partial int TranslateMessage(MSG* msg);
    [LibraryImport("user32.dll")] public static partial nint DispatchMessageW(MSG* msg);
    [LibraryImport("user32.dll")] public static partial nint DefWindowProcW(nint hwnd, uint msg, nint wp, nint lp);
    [LibraryImport("user32.dll")] public static partial void PostQuitMessage(int code);
    [LibraryImport("user32.dll")] public static partial int PostThreadMessageW(uint thread, uint msg, nint wp, nint lp);
    [LibraryImport("user32.dll")] public static partial nint BeginPaint(nint hwnd, PAINTSTRUCT* ps);
    [LibraryImport("user32.dll")] public static partial int EndPaint(nint hwnd, PAINTSTRUCT* ps);
    [LibraryImport("user32.dll")] public static partial int InvalidateRect(nint hwnd, RECT* rect, int erase);
    [LibraryImport("user32.dll")] public static partial int GetClientRect(nint hwnd, RECT* rect);
    [LibraryImport("user32.dll")] public static partial int SetProcessDpiAwarenessContext(nint ctx);
    [LibraryImport("user32.dll")] public static partial uint GetDpiForWindow(nint hwnd);
    [LibraryImport("user32.dll")] public static partial int AdjustWindowRectExForDpi(RECT* rect, uint style, int menu, uint exStyle, uint dpi);
    [LibraryImport("user32.dll")] public static partial int SetWindowPos(nint hwnd, nint after, int x, int y, int cx, int cy, uint flags);
    [LibraryImport("user32.dll")] public static partial nint LoadCursorW(nint instance, nint name);
    [LibraryImport("user32.dll")] public static partial int TrackMouseEvent(TRACKMOUSEEVENT* tme);
    [LibraryImport("user32.dll")] public static partial nint SetCapture(nint hwnd);
    [LibraryImport("user32.dll")] public static partial int ReleaseCapture();

    [LibraryImport("user32.dll")] public static partial short GetKeyState(int vk);
    [LibraryImport("user32.dll")] public static partial nint SetCursor(nint cursor);
    [LibraryImport("user32.dll")] public static partial nuint SetTimer(nint hwnd, nuint id, uint ms, nint proc);
    [LibraryImport("user32.dll")] public static partial int KillTimer(nint hwnd, nuint id);
    [LibraryImport("user32.dll")] public static partial int ScreenToClient(nint hwnd, POINT* pt);

    [LibraryImport("user32.dll")] public static partial int OpenClipboard(nint owner);
    [LibraryImport("user32.dll")] public static partial int CloseClipboard();
    [LibraryImport("user32.dll")] public static partial int EmptyClipboard();
    [LibraryImport("user32.dll")] public static partial nint GetClipboardData(uint format);
    [LibraryImport("user32.dll")] public static partial nint SetClipboardData(uint format, nint handle);
    [LibraryImport("kernel32.dll")] public static partial nint GlobalAlloc(uint flags, nuint bytes);
    [LibraryImport("kernel32.dll")] public static partial nint GlobalFree(nint handle);
    [LibraryImport("kernel32.dll")] public static partial void* GlobalLock(nint handle);
    [LibraryImport("kernel32.dll")] public static partial int GlobalUnlock(nint handle);
    [LibraryImport("kernel32.dll")] public static partial nuint GlobalSize(nint handle);

    [LibraryImport("imm32.dll")] public static partial nint ImmGetContext(nint hwnd);
    [LibraryImport("imm32.dll")] public static partial int ImmReleaseContext(nint hwnd, nint himc);
    [LibraryImport("imm32.dll")] public static partial int ImmGetCompositionStringW(nint himc, uint index, void* buf, uint bufLen);
    [LibraryImport("imm32.dll")] public static partial int ImmSetCompositionWindow(nint himc, COMPOSITIONFORM* form);
    [LibraryImport("imm32.dll")] public static partial int ImmSetCandidateWindow(nint himc, CANDIDATEFORM* form);

    [LibraryImport("gdi32.dll")]
    public static partial int SetDIBitsToDevice(nint hdc, int xDest, int yDest, uint w, uint h,
        int xSrc, int ySrc, uint startScan, uint lines, void* bits, BITMAPINFOHEADER* bmi, uint usage);
}
