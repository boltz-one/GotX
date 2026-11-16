using static GotX.Platform.Windows.Win32;

namespace GotX.Platform.Windows;

/// <summary>Móc trợ năng của cửa sổ Win32: bridge UI Automation chỉ được tạo khi có WM_GETOBJECT đầu tiên.</summary>
internal sealed partial class Win32Window : IAccessibleWindow
{
    UiaBridge? _uia;
    IAccessibilityHost? _accessibility;

    public IAccessibilityHost? Accessibility
    {
        set
        {
            if (_accessibility is null) Frame += UiaAfterFrame; // chạy sau handler vẽ của Application; chỉ tốn một phép kiểm null khi không có AT
            _accessibility = value;
        }
    }

    void UiaAfterFrame() => _uia?.OnFrame();

    /// <summary>WM_GETOBJECT (chỉ UiaRootObjectId) và WM_GOTX_UIA (hành động / dựng lại ảnh chụp từ luồng UIA).</summary>
    nint HandleUia(uint msg, nint wp, nint lp)
    {
        if (msg == WM_GOTX_UIA) { _uia?.Drain(); return 0; }
        if ((int)lp != UiaRootObjectId || _accessibility is not { } host) return DefWindowProcW(_hwnd, msg, wp, lp);
        _uia ??= new UiaBridge(_hwnd, host);
        return _uia.GetObject(wp, lp);
    }

    void UiaDestroy()
    {
        _uia?.Dispose();
        _uia = null;
    }
}
