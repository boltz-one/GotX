using System.Runtime.InteropServices;

namespace GotX.Footprint;

/// <summary>DXGI QueryVideoMemoryInfo of the default adapter (the one D3D11 picks): usage of THIS process only, so it must run inside the host.</summary>
static unsafe class GpuMem
{
    [DllImport("dxgi.dll")] static extern int CreateDXGIFactory1(Guid* riid, void** factory);

    static void* s_adapter;
    static bool s_tried;

    /// <summary>(local, non-local) bytes, or (-1, -1) when the adapter does not expose IDXGIAdapter3.</summary>
    public static (long Local, long NonLocal) Usage()
    {
        if (!s_tried) { s_tried = true; Init(); }
        if (s_adapter is null) return (-1, -1);
        var vt = *(void***)s_adapter;
        var query = (delegate* unmanaged[Stdcall]<void*, uint, int, ulong*, int>)vt[14]; // IDXGIAdapter3::QueryVideoMemoryInfo (IUnknown 3 + IDXGIObject 4 + Adapter 3 + Adapter1 1 + Adapter2 1 + 2 teardown-event methods)
        ulong* info = stackalloc ulong[4]; // Budget, CurrentUsage, AvailableForReservation, CurrentReservation
        long local = query(s_adapter, 0, 0, info) >= 0 ? (long)info[1] : -1;
        long shared = query(s_adapter, 0, 1, info) >= 0 ? (long)info[1] : -1;
        return (local, shared);
    }

    static void Init()
    {
        try
        {
            var iidFactory = new Guid("770aae78-f26f-4dba-a829-253c83d1b387");
            void* factory;
            if (CreateDXGIFactory1(&iidFactory, &factory) < 0) return;
            void* adapter;
            var enumAdapters1 = (delegate* unmanaged[Stdcall]<void*, uint, void**, int>)(*(void***)factory)[12];
            if (enumAdapters1(factory, 0, &adapter) < 0) return;
            var iid3 = new Guid("645967A4-1392-4310-A798-8053CE3E93FD");
            void* adapter3;
            var qi = (delegate* unmanaged[Stdcall]<void*, Guid*, void**, int>)(*(void***)adapter)[0];
            if (qi(adapter, &iid3, &adapter3) >= 0) s_adapter = adapter3;
        }
        catch (Exception) { /* no dxgi: leave unavailable */ }
    }
}
