using System.Runtime.InteropServices;

namespace GotX.Platform.Windows;

/// <summary>Khai báo UI Automation / OLE tối thiểu cho cầu nối trợ năng. Mọi tham số blittable: tương thích Native AOT.</summary>
internal static unsafe partial class Win32
{
    public const uint WM_GETOBJECT = 0x003D, WM_GOTX_UIA = WM_APP + 0x41;
    public const int UiaRootObjectId = -25;

    /// <summary>VARIANT 16 byte của x64: kiểu ở đầu, dữ liệu từ byte 8.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct VARIANT
    {
        public ushort vt, r1, r2, r3;
        public long data, data2;

        public static VARIANT Int(int v) => new() { vt = 3, data = v };
        public static VARIANT Bool(bool v) => new() { vt = 11, data = v ? -1 : 0 };
        public static VARIANT Double(double v) => new() { vt = 5, data = BitConverter.DoubleToInt64Bits(v) };
        public static VARIANT Str(string s) { fixed (char* p = s) return new() { vt = 8, data = SysAllocString(p) }; }
    }

    [LibraryImport("oleaut32.dll")] public static partial nint SysAllocString(char* s);
    [LibraryImport("oleaut32.dll")] public static partial void SysFreeString(char* s);
    [LibraryImport("oleaut32.dll")] public static partial nint SafeArrayCreateVector(ushort vt, int lbound, uint count);
    [LibraryImport("oleaut32.dll")] public static partial int SafeArrayAccessData(nint array, void** data);
    [LibraryImport("oleaut32.dll")] public static partial int SafeArrayUnaccessData(nint array);

    [LibraryImport("UIAutomationCore.dll")] public static partial nint UiaReturnRawElementProvider(nint hwnd, nint wParam, nint lParam, nint element);
    [LibraryImport("UIAutomationCore.dll")] public static partial int UiaHostProviderFromHwnd(nint hwnd, nint* provider);
    [LibraryImport("UIAutomationCore.dll")] public static partial int UiaClientsAreListening();
    [LibraryImport("UIAutomationCore.dll")] public static partial int UiaRaiseAutomationEvent(nint provider, int eventId);
    [LibraryImport("UIAutomationCore.dll")] public static partial int UiaRaiseAutomationPropertyChangedEvent(nint provider, int propertyId, VARIANT oldValue, VARIANT newValue);
    [LibraryImport("UIAutomationCore.dll")] public static partial int UiaDisconnectProvider(nint provider);

    [LibraryImport("user32.dll")] public static partial int ClientToScreen(nint hwnd, POINT* pt);
    [LibraryImport("user32.dll")] public static partial int PostMessageW(nint hwnd, uint msg, nint wParam, nint lParam);
}
