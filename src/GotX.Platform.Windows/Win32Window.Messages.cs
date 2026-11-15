using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using GotX.Raster;
using GotX.Text;
using static GotX.Platform.Windows.Win32;

namespace GotX.Platform.Windows;

internal sealed unsafe partial class Win32Window
{
    nint Handle(uint msg, nint wp, nint lp)
    {
        switch (msg)
        {
            case WM_PAINT:
            {
                PAINTSTRUCT ps;
                nint hdc = BeginPaint(_hwnd, &ps);
                // EndPaint phải luôn chạy: nếu Frame ném exception mà bỏ qua nó, HDC bị rò và Windows gửi WM_PAINT liên tục.
                try
                {
                    Frame?.Invoke();
                    if (_gpu is null) Blit(hdc);
                }
                finally { EndPaint(_hwnd, &ps); }
                return 0;
            }
            case WM_TIMER:
                KillTimer(_hwnd, 1);
                _animTimer = false;
                RequestFrame();
                return 0;
            case WM_MOUSEWHEEL:
            case WM_MOUSEHWHEEL:
            {
                // 120 đơn vị = một nấc; một nấc ≈ 3 dòng ≈ 64 logical px. Touchpad gửi giá trị nhỏ hơn → cuộn mịn.
                float notches = (short)((wp >> 16) & 0xFFFF) / 120f;
                var pt = new POINT { x = (short)(lp & 0xFFFF), y = (short)((lp >> 16) & 0xFFFF) };
                ScreenToClient(_hwnd, &pt); // toạ độ của wheel là toạ độ màn hình
                float s = ScaleFactor, step = 64;
                bool horizontal = msg == WM_MOUSEHWHEEL || (CurrentModifiers() & Modifiers.Shift) != 0;
                var delta = msg == WM_MOUSEHWHEEL ? new Point(notches * step, 0)
                          : horizontal ? new Point(-notches * step, 0) : new Point(0, -notches * step);
                Input?.Invoke(new InputEvent(InputKind.Scroll, new Point(pt.x / s, pt.y / s), Delta: delta, Modifiers: CurrentModifiers()));
                return 0;
            }
            case WM_SETCURSOR:
                if ((lp & 0xFFFF) != HTCLIENT || CursorProvider is null) break;
                SetCursor(CursorProvider() switch
                {
                    CursorKind.Pointer => s_hand,
                    CursorKind.Text => s_ibeam,
                    _ => s_arrow,
                });
                return 1;
            case WM_ERASEBKGND:
                return 1; // tránh nháy: ta tự vẽ toàn bộ
            case WM_SIZE:
                RequestFrame();
                return 0;

            // ---- Chuột ----
            case WM_MOUSEMOVE:
                if (!_trackingLeave)
                {
                    var t = new TRACKMOUSEEVENT { cbSize = (uint)sizeof(TRACKMOUSEEVENT), dwFlags = TME_LEAVE, hwndTrack = _hwnd };
                    TrackMouseEvent(&t);
                    _trackingLeave = true;
                }
                RaiseMouse(InputKind.MouseMove, lp);
                return 0;
            case WM_MOUSELEAVE:
                _trackingLeave = false;
                Input?.Invoke(new InputEvent(InputKind.MouseLeave));
                return 0;
            case WM_LBUTTONDOWN:
                SetCapture(_hwnd);
                RaiseMouse(InputKind.MouseDown, lp);
                return 0;
            case WM_LBUTTONUP:
                ReleaseCapture();
                RaiseMouse(InputKind.MouseUp, lp);
                return 0;
            case WM_RBUTTONDOWN: RaiseMouse(InputKind.MouseDown, lp, MouseButton.Right); return 0;
            case WM_RBUTTONUP: RaiseMouse(InputKind.MouseUp, lp, MouseButton.Right); return 0;
            case WM_MBUTTONDOWN: RaiseMouse(InputKind.MouseDown, lp, MouseButton.Middle); return 0;
            case WM_MBUTTONUP: RaiseMouse(InputKind.MouseUp, lp, MouseButton.Middle); return 0;

            // ---- Bàn phím ----
            case WM_KEYDOWN:
            {
                var key = MapKey((int)wp);
                if (key == Key.None) break;
                Input?.Invoke(new InputEvent(InputKind.KeyDown, Key: key, Modifiers: CurrentModifiers()));
                return 0;
            }
            case WM_CHAR:
            {
                // Unikey/EVKey gửi VK_BACK (đã xử lý ở WM_KEYDOWN) rồi WM_CHAR của ký tự mới.
                char c = (char)wp;
                if (char.IsHighSurrogate(c)) { _highSurrogate = c; return 0; }
                string? text = char.IsLowSurrogate(c)
                    ? (_highSurrogate != 0 ? new string([_highSurrogate, c]) : null)
                    : (c < 0x20 || c == 0x7F ? null : c.ToString());
                _highSurrogate = '\0';
                if (text is not null) Input?.Invoke(new InputEvent(InputKind.TextInput, Text: text));
                return 0;
            }

            // ---- IME (Telex của Windows, bộ gõ CJK…) ----
            case WM_IME_SETCONTEXT:
                // Ta tự vẽ chuỗi đang soạn ngay trong ô nhập → tắt cửa sổ soạn mặc định của hệ thống.
                return DefWindowProcW(_hwnd, msg, wp, (nint)((long)lp & ~ISC_SHOWUICOMPOSITIONWINDOW));
            case WM_IME_STARTCOMPOSITION:
                PositionIme();
                return 0;
            case WM_IME_COMPOSITION:
            {
                nint himc = ImmGetContext(_hwnd);
                if (himc != 0)
                {
                    if ((lp & GCS_RESULTSTR) != 0)
                    {
                        string result = CompositionString(himc, GCS_RESULTSTR);
                        if (result.Length > 0) Input?.Invoke(new InputEvent(InputKind.TextInput, Text: result));
                    }
                    if ((lp & GCS_COMPSTR) != 0 || (lp & GCS_RESULTSTR) != 0)
                        Input?.Invoke(new InputEvent(InputKind.ImeCompose, Text: CompositionString(himc, GCS_COMPSTR)));
                    ImmReleaseContext(_hwnd, himc);
                }
                PositionIme();
                return 0;
            }
            case WM_IME_ENDCOMPOSITION:
                Input?.Invoke(new InputEvent(InputKind.ImeCompose, Text: ""));
                return 0;

            case WM_DPICHANGED:
            {
                var r = (RECT*)lp; // Windows gợi ý kích thước mới theo DPI mới
                SetWindowPos(_hwnd, 0, r->left, r->top, r->right - r->left, r->bottom - r->top, SWP_NOZORDER | SWP_NOACTIVATE);
                RequestFrame();
                return 0;
            }
            case WM_DESTROY:
                _gpu?.Dispose();
                s_windows.Remove(_hwnd);
                Closed?.Invoke();
                if (s_windows.Count == 0) PostQuitMessage(0);
                return 0;
        }
        return DefWindowProcW(_hwnd, msg, wp, lp);
    }

    static Key MapKey(int vk) => vk switch
    {
        VK_BACK => Key.Backspace, VK_DELETE => Key.Delete, VK_LEFT => Key.Left, VK_RIGHT => Key.Right,
        VK_UP => Key.Up, VK_DOWN => Key.Down, VK_HOME => Key.Home, VK_END => Key.End,
        VK_RETURN => Key.Enter, VK_TAB => Key.Tab, VK_ESCAPE => Key.Escape,
        VK_A => Key.A, VK_C => Key.C, VK_V => Key.V, VK_X => Key.X, VK_Y => Key.Y, VK_Z => Key.Z,
        0x20 => Key.Space, 0x21 => Key.PageUp, 0x22 => Key.PageDown, // VK_SPACE, VK_PRIOR, VK_NEXT
        _ => Key.None,
    };

    static Modifiers CurrentModifiers()
    {
        var m = Modifiers.None;
        if (GetKeyState(VK_SHIFT) < 0) m |= Modifiers.Shift;
        if (GetKeyState(VK_CONTROL) < 0) m |= Modifiers.Control;
        if (GetKeyState(VK_MENU) < 0) m |= Modifiers.Alt;
        return m;
    }

    void RaiseMouse(InputKind kind, nint lp, MouseButton button = MouseButton.Left)
    {
        float s = ScaleFactor;
        int x = (short)(lp & 0xFFFF), y = (short)((lp >> 16) & 0xFFFF);
        Input?.Invoke(new InputEvent(kind, new Point(x / s, y / s), button, Modifiers: CurrentModifiers()));
    }
}
