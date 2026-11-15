using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using static GotX.Platform.Windows.D3D;

namespace GotX.Platform.Windows;

internal sealed unsafe partial class D3D11Renderer
{
    public static D3D11Renderer? TryCreate(nint hwnd, int width, int height)
    {
        var r = new D3D11Renderer();
        try { r.Init(hwnd, width, height); return r; }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Không khởi tạo được D3D11, dùng renderer CPU: {ex.Message}");
            r.Dispose();
            return null;
        }
    }

    void Init(nint hwnd, int width, int height)
    {
        _width = Math.Max(1, width); _height = Math.Max(1, height);
        var sd = new DXGI_SWAP_CHAIN_DESC
        {
            Width = (uint)_width, Height = (uint)_height, Format = DXGI_FORMAT_B8G8R8A8_UNORM,
            SampleCount = 1, BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT, BufferCount = 2,
            OutputWindow = hwnd, Windowed = 1, SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD,
        };
        void* swap, dev, ctx;
        int fl;
        int hr = D3D11CreateDeviceAndSwapChain(null, DRIVER_HARDWARE, 0, CREATE_BGRA_SUPPORT, null, 0,
            D3D11_SDK_VERSION, &sd, &swap, &dev, &fl, &ctx);
        if (hr < 0) // không có GPU phù hợp → WARP (rasterizer phần mềm của Windows)
            hr = D3D11CreateDeviceAndSwapChain(null, DRIVER_WARP, 0, CREATE_BGRA_SUPPORT, null, 0,
                D3D11_SDK_VERSION, &sd, &swap, &dev, &fl, &ctx);
        Check(hr, "D3D11CreateDeviceAndSwapChain");
        _swap = swap; _device = dev; _ctx = ctx;

        CreateTarget();
        CompileShaders();

        var blend = new BLEND_DESC();
        blend.RenderTarget0 = new RENDER_TARGET_BLEND_DESC
        {
            BlendEnable = 1, SrcBlend = 2 /*ONE*/, DestBlend = 6 /*INV_SRC_ALPHA*/, BlendOp = 1,
            SrcBlendAlpha = 2, DestBlendAlpha = 6, BlendOpAlpha = 1, RenderTargetWriteMask = 0xF,
        };
        void* bs; Check(CreateBlendState(_device, &blend, &bs), "CreateBlendState"); _blend = bs;

        var rd = new RASTERIZER_DESC { FillMode = 3 /*SOLID*/, CullMode = 1 /*NONE*/, DepthClipEnable = 1 };
        void* rs; Check(CreateRasterizerState(_device, &rd, &rs), "CreateRasterizerState"); _raster = rs;

        var smp = new SAMPLER_DESC { Filter = 0 /*POINT*/, AddressU = 3, AddressV = 3, AddressW = 3, ComparisonFunc = 1, MaxLOD = float.MaxValue };
        void* ss; Check(CreateSamplerState(_device, &smp, &ss), "CreateSamplerState"); _sampler = ss;
        smp.Filter = 0x15; // MIN_MAG_MIP_LINEAR – cho glyph bị xoay
        void* sl; Check(CreateSamplerState(_device, &smp, &sl), "CreateSamplerState(linear)"); _samplerLinear = sl;

        var cbd = new BUFFER_DESC { ByteWidth = 16, Usage = USAGE_DEFAULT, BindFlags = BIND_CONSTANT_BUFFER };
        void* cb; Check(CreateBuffer(_device, &cbd, null, &cb), "CreateBuffer(cb)"); _cb = cb;

        var td = new TEXTURE2D_DESC
        {
            Width = AtlasSize, Height = AtlasSize, MipLevels = 1, ArraySize = 1, Format = DXGI_FORMAT_R8_UNORM,
            SampleCount = 1, Usage = USAGE_DEFAULT, BindFlags = BIND_SHADER_RESOURCE,
        };
        void* tex; Check(CreateTexture2D(_device, &td, null, &tex), "CreateTexture2D(atlas)"); _atlas = tex;
        void* tsrv; Check(CreateShaderResourceView(_device, _atlas, null, &tsrv), "CreateSRV(atlas)"); _atlasSrv = tsrv;

        EnsureInstanceCapacity(1024);
        EnsureShapeCapacity(16);
        InitGpuTimer();
    }

    void CreateTarget()
    {
        void* back;
        Guid iid = IID_ID3D11Texture2D;
        Check(GetBuffer(_swap, 0, &iid, &back), "GetBuffer");
        void* rtv;
        int hr = CreateRenderTargetView(_device, back, null, &rtv);
        Release(ref back);
        Check(hr, "CreateRenderTargetView");
        _rtv = rtv;
    }

    static Vector4 M(Mat2 m) { var x = m.OrIdentity; return new(x.M11, x.M12, x.M21, x.M22); }

    void Resize(int w, int h)
    {
        _width = w; _height = h;
        Release(ref _rtv);
        Check(ResizeBuffers(_swap, 0, (uint)w, (uint)h, DXGI_FORMAT_UNKNOWN, 0), "ResizeBuffers");
        CreateTarget();
    }
}
