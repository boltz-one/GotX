using System.Runtime.InteropServices;

namespace GotX.Platform.Windows;

/// <summary>
/// COM tối thiểu cho D3D11 – gọi thẳng qua bảng vtable bằng con trỏ hàm unmanaged.
/// Không cần ComWrappers hay thư viện binding: chỉ là P/Invoke + chỉ số hàm trong vtable
/// (theo thứ tự khai báo trong d3d11.h / dxgi.h / d3dcommon.h).
/// </summary>
internal static unsafe partial class D3D
{
    public const uint D3D11_SDK_VERSION = 7;
    public const int DRIVER_HARDWARE = 1, DRIVER_WARP = 5;
    public const uint CREATE_BGRA_SUPPORT = 0x20;
    public const uint DXGI_FORMAT_UNKNOWN = 0, DXGI_FORMAT_R8_UNORM = 61, DXGI_FORMAT_B8G8R8A8_UNORM = 87;
    public const uint DXGI_USAGE_RENDER_TARGET_OUTPUT = 0x20, DXGI_SWAP_EFFECT_FLIP_DISCARD = 4;
    public const uint USAGE_DEFAULT = 0, USAGE_DYNAMIC = 2;
    public const uint BIND_CONSTANT_BUFFER = 0x4, BIND_SHADER_RESOURCE = 0x8;
    public const uint CPU_ACCESS_WRITE = 0x10000, MISC_BUFFER_STRUCTURED = 0x40, MAP_WRITE_DISCARD = 4;
    public const uint TOPOLOGY_TRIANGLESTRIP = 5;

    // ---------- Struct ----------

    [StructLayout(LayoutKind.Sequential)]
    public struct DXGI_SWAP_CHAIN_DESC
    {
        public uint Width, Height, RefreshNum, RefreshDen, Format, ScanlineOrdering, Scaling;
        public uint SampleCount, SampleQuality;
        public uint BufferUsage, BufferCount;
        public nint OutputWindow;
        public int Windowed;
        public uint SwapEffect, Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BUFFER_DESC { public uint ByteWidth, Usage, BindFlags, CPUAccessFlags, MiscFlags, StructureByteStride; }

    [StructLayout(LayoutKind.Sequential)]
    public struct TEXTURE2D_DESC
    {
        public uint Width, Height, MipLevels, ArraySize, Format, SampleCount, SampleQuality, Usage, BindFlags, CPUAccessFlags, MiscFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MAPPED_SUBRESOURCE { public void* pData; public uint RowPitch, DepthPitch; }

    [StructLayout(LayoutKind.Sequential)]
    public struct BOX { public uint left, top, front, right, bottom, back; }

    [StructLayout(LayoutKind.Sequential)]
    public struct VIEWPORT { public float TopLeftX, TopLeftY, Width, Height, MinDepth, MaxDepth; }

    [StructLayout(LayoutKind.Sequential)]
    public struct RENDER_TARGET_BLEND_DESC
    {
        public int BlendEnable;
        public uint SrcBlend, DestBlend, BlendOp, SrcBlendAlpha, DestBlendAlpha, BlendOpAlpha;
        public byte RenderTargetWriteMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BLEND_DESC
    {
        public int AlphaToCoverageEnable, IndependentBlendEnable;
        public RENDER_TARGET_BLEND_DESC RenderTarget0;
        public fixed byte RenderTargetRest[7 * 32];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RASTERIZER_DESC
    {
        public uint FillMode, CullMode;
        public int FrontCounterClockwise, DepthBias;
        public float DepthBiasClamp, SlopeScaledDepthBias;
        public int DepthClipEnable, ScissorEnable, MultisampleEnable, AntialiasedLineEnable;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SAMPLER_DESC
    {
        public uint Filter, AddressU, AddressV, AddressW;
        public float MipLODBias;
        public uint MaxAnisotropy, ComparisonFunc;
        public float Border0, Border1, Border2, Border3, MinLOD, MaxLOD;
    }

    public static readonly Guid IID_ID3D11Texture2D = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

    // ---------- Hàm export ----------

    [LibraryImport("d3d11.dll")]
    public static partial int D3D11CreateDeviceAndSwapChain(void* adapter, int driverType, nint software, uint flags,
        void* featureLevels, uint numLevels, uint sdkVersion, DXGI_SWAP_CHAIN_DESC* desc,
        void** swapChain, void** device, int* featureLevel, void** context);

    [LibraryImport("d3dcompiler_47.dll")]
    public static partial int D3DCompile(void* src, nuint srcSize, byte* sourceName, void* defines, void* include,
        byte* entry, byte* target, uint flags1, uint flags2, void** code, void** errors);

    // ---------- vtable ----------

    static void** Vt(void* o) => *(void***)o;

    public static void Release(ref void* o)
    {
        if (o == null) return;
        ((delegate* unmanaged[Stdcall]<void*, uint>)Vt(o)[2])(o);
        o = null;
    }

    public static void Check(int hr, string what)
    {
        if (hr < 0) throw new InvalidOperationException($"{what} thất bại: HRESULT 0x{hr:X8}");
    }

    // ID3DBlob
    public static void* BlobPtr(void* b) => ((delegate* unmanaged[Stdcall]<void*, void*>)Vt(b)[3])(b);
    public static nuint BlobSize(void* b) => ((delegate* unmanaged[Stdcall]<void*, nuint>)Vt(b)[4])(b);

    // IDXGISwapChain
    public static int Present(void* sc, uint sync, uint flags) =>
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, int>)Vt(sc)[8])(sc, sync, flags);
    public static int GetBuffer(void* sc, uint i, Guid* iid, void** buf) =>
        ((delegate* unmanaged[Stdcall]<void*, uint, Guid*, void**, int>)Vt(sc)[9])(sc, i, iid, buf);
    public static int ResizeBuffers(void* sc, uint count, uint w, uint h, uint fmt, uint flags) =>
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, uint, uint, uint, int>)Vt(sc)[13])(sc, count, w, h, fmt, flags);

    // ID3D11Device
    public static int CreateBuffer(void* d, BUFFER_DESC* desc, void* init, void** buf) =>
        ((delegate* unmanaged[Stdcall]<void*, BUFFER_DESC*, void*, void**, int>)Vt(d)[3])(d, desc, init, buf);
    public static int CreateTexture2D(void* d, TEXTURE2D_DESC* desc, void* init, void** tex) =>
        ((delegate* unmanaged[Stdcall]<void*, TEXTURE2D_DESC*, void*, void**, int>)Vt(d)[5])(d, desc, init, tex);
    public static int CreateShaderResourceView(void* d, void* res, void* desc, void** srv) =>
        ((delegate* unmanaged[Stdcall]<void*, void*, void*, void**, int>)Vt(d)[7])(d, res, desc, srv);
    public static int CreateRenderTargetView(void* d, void* res, void* desc, void** rtv) =>
        ((delegate* unmanaged[Stdcall]<void*, void*, void*, void**, int>)Vt(d)[9])(d, res, desc, rtv);
    public static int CreateVertexShader(void* d, void* code, nuint len, void* linkage, void** vs) =>
        ((delegate* unmanaged[Stdcall]<void*, void*, nuint, void*, void**, int>)Vt(d)[12])(d, code, len, linkage, vs);
    public static int CreatePixelShader(void* d, void* code, nuint len, void* linkage, void** ps) =>
        ((delegate* unmanaged[Stdcall]<void*, void*, nuint, void*, void**, int>)Vt(d)[15])(d, code, len, linkage, ps);
    public static int CreateBlendState(void* d, BLEND_DESC* desc, void** state) =>
        ((delegate* unmanaged[Stdcall]<void*, BLEND_DESC*, void**, int>)Vt(d)[20])(d, desc, state);
    public static int CreateRasterizerState(void* d, RASTERIZER_DESC* desc, void** state) =>
        ((delegate* unmanaged[Stdcall]<void*, RASTERIZER_DESC*, void**, int>)Vt(d)[22])(d, desc, state);
    public static int CreateSamplerState(void* d, SAMPLER_DESC* desc, void** state) =>
        ((delegate* unmanaged[Stdcall]<void*, SAMPLER_DESC*, void**, int>)Vt(d)[23])(d, desc, state);

    // ID3D11DeviceContext
    public static void VSSetConstantBuffers(void* c, uint slot, uint n, void** bufs) =>
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)Vt(c)[7])(c, slot, n, bufs);
    public static void PSSetShaderResources(void* c, uint slot, uint n, void** views) =>
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)Vt(c)[8])(c, slot, n, views);
    public static void PSSetShader(void* c, void* ps) =>
        ((delegate* unmanaged[Stdcall]<void*, void*, void**, uint, void>)Vt(c)[9])(c, ps, null, 0);
    public static void PSSetSamplers(void* c, uint slot, uint n, void** samplers) =>
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)Vt(c)[10])(c, slot, n, samplers);
    public static void VSSetShader(void* c, void* vs) =>
        ((delegate* unmanaged[Stdcall]<void*, void*, void**, uint, void>)Vt(c)[11])(c, vs, null, 0);
    public static int Map(void* c, void* res, uint sub, uint type, uint flags, MAPPED_SUBRESOURCE* m) =>
        ((delegate* unmanaged[Stdcall]<void*, void*, uint, uint, uint, MAPPED_SUBRESOURCE*, int>)Vt(c)[14])(c, res, sub, type, flags, m);
    public static void Unmap(void* c, void* res, uint sub) =>
        ((delegate* unmanaged[Stdcall]<void*, void*, uint, void>)Vt(c)[15])(c, res, sub);
    public static void PSSetConstantBuffers(void* c, uint slot, uint n, void** bufs) =>
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)Vt(c)[16])(c, slot, n, bufs);
    public static void IASetInputLayout(void* c, void* layout) =>
        ((delegate* unmanaged[Stdcall]<void*, void*, void>)Vt(c)[17])(c, layout);
    public static void DrawInstanced(void* c, uint verts, uint instances, uint startVert, uint startInst) =>
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, uint, uint, void>)Vt(c)[21])(c, verts, instances, startVert, startInst);
    public static void IASetPrimitiveTopology(void* c, uint topology) =>
        ((delegate* unmanaged[Stdcall]<void*, uint, void>)Vt(c)[24])(c, topology);
    public static void VSSetShaderResources(void* c, uint slot, uint n, void** views) =>
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)Vt(c)[25])(c, slot, n, views);
    public static void OMSetRenderTargets(void* c, uint n, void** rtvs, void* dsv) =>
        ((delegate* unmanaged[Stdcall]<void*, uint, void**, void*, void>)Vt(c)[33])(c, n, rtvs, dsv);
    public static void OMSetBlendState(void* c, void* state, float* factor, uint mask) =>
        ((delegate* unmanaged[Stdcall]<void*, void*, float*, uint, void>)Vt(c)[35])(c, state, factor, mask);
    public static void RSSetState(void* c, void* state) =>
        ((delegate* unmanaged[Stdcall]<void*, void*, void>)Vt(c)[43])(c, state);
    public static void RSSetViewports(void* c, uint n, VIEWPORT* vps) =>
        ((delegate* unmanaged[Stdcall]<void*, uint, VIEWPORT*, void>)Vt(c)[44])(c, n, vps);
    public static void UpdateSubresource(void* c, void* res, uint sub, BOX* box, void* data, uint rowPitch, uint depthPitch) =>
        ((delegate* unmanaged[Stdcall]<void*, void*, uint, BOX*, void*, uint, uint, void>)Vt(c)[48])(c, res, sub, box, data, rowPitch, depthPitch);
    public static void ClearRenderTargetView(void* c, void* rtv, float* rgba) =>
        ((delegate* unmanaged[Stdcall]<void*, void*, float*, void>)Vt(c)[50])(c, rtv, rgba);
}
