using System.Runtime.InteropServices;

namespace GotX.Platform.Windows;

/// <summary>Phần interop cho thiết bị không cửa sổ (offscreen), đọc lại pixel và phát hiện mất thiết bị.</summary>
internal static unsafe partial class D3D
{
    public const int DXGI_ERROR_DEVICE_HUNG = unchecked((int)0x887A0006), DXGI_ERROR_DEVICE_REMOVED = unchecked((int)0x887A0005),
        DXGI_ERROR_DEVICE_RESET = unchecked((int)0x887A0007), DXGI_ERROR_DRIVER_INTERNAL_ERROR = unchecked((int)0x887A0020),
        DXGI_STATUS_OCCLUDED = 0x087A0001;
    public const uint USAGE_STAGING = 3, BIND_RENDER_TARGET = 0x20, CPU_ACCESS_READ = 0x20000, MAP_READ = 1;

    public static bool IsDeviceLost(int hr) =>
        hr is DXGI_ERROR_DEVICE_REMOVED or DXGI_ERROR_DEVICE_RESET or DXGI_ERROR_DEVICE_HUNG or DXGI_ERROR_DRIVER_INTERNAL_ERROR;

    [LibraryImport("d3d11.dll")]
    public static partial int D3D11CreateDevice(void* adapter, int driverType, nint software, uint flags,
        void* featureLevels, uint numLevels, uint sdkVersion, void** device, int* featureLevel, void** context);

    // ID3D11Device::GetDeviceRemovedReason
    public static int GetDeviceRemovedReason(void* d) => ((delegate* unmanaged[Stdcall]<void*, int>)Vt(d)[39])(d);

    // ID3D11DeviceContext
    public static void CopyResource(void* c, void* dst, void* src) =>
        ((delegate* unmanaged[Stdcall]<void*, void*, void*, void>)Vt(c)[47])(c, dst, src);
    public static void RSSetScissorRects(void* c, uint n, int* rects) =>
        ((delegate* unmanaged[Stdcall]<void*, uint, int*, void>)Vt(c)[45])(c, n, rects);
    public static void ClearState(void* c) => ((delegate* unmanaged[Stdcall]<void*, void>)Vt(c)[110])(c);
    public static void Flush(void* c) => ((delegate* unmanaged[Stdcall]<void*, void>)Vt(c)[111])(c);

    [StructLayout(LayoutKind.Sequential)]
    public struct VIDEO_MEMORY_INFO { public ulong Budget, CurrentUsage, AvailableForReservation, CurrentReservation; }

    public static int QueryInterface(void* o, Guid* iid, void** result) =>
        ((delegate* unmanaged[Stdcall]<void*, Guid*, void**, int>)Vt(o)[0])(o, iid, result);
    // IDXGIDevice::GetAdapter
    public static int GetAdapter(void* dxgiDevice, void** adapter) =>
        ((delegate* unmanaged[Stdcall]<void*, void**, int>)Vt(dxgiDevice)[7])(dxgiDevice, adapter);
    // IDXGIAdapter3::QueryVideoMemoryInfo(node, segmentGroup, info)
    public static int QueryVideoMemoryInfo(void* adapter3, uint node, uint group, VIDEO_MEMORY_INFO* info) =>
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, VIDEO_MEMORY_INFO*, int>)Vt(adapter3)[14])(adapter3, node, group, info);
}
