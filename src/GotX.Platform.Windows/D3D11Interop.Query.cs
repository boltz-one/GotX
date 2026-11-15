using System.Runtime.InteropServices;

namespace GotX.Platform.Windows;

internal static unsafe partial class D3D
{
    [StructLayout(LayoutKind.Sequential)]
    public struct QUERY_DESC { public uint Query, MiscFlags; }

    [StructLayout(LayoutKind.Sequential)]
    public struct QUERY_DISJOINT { public ulong Frequency; public int Disjoint; }

    public static int CreateQuery(void* d, QUERY_DESC* desc, void** query) =>
        ((delegate* unmanaged[Stdcall]<void*, QUERY_DESC*, void**, int>)Vt(d)[24])(d, desc, query);
    public static void QueryBegin(void* c, void* q) => ((delegate* unmanaged[Stdcall]<void*, void*, void>)Vt(c)[27])(c, q);
    public static void QueryEnd(void* c, void* q) => ((delegate* unmanaged[Stdcall]<void*, void*, void>)Vt(c)[28])(c, q);
    public static int QueryData(void* c, void* q, void* data, uint size, uint flags) =>
        ((delegate* unmanaged[Stdcall]<void*, void*, void*, uint, uint, int>)Vt(c)[29])(c, q, data, size, flags);
}
