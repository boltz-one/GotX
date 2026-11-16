using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using static GotX.Platform.Windows.D3D;

namespace GotX.Platform.Windows;

internal sealed unsafe partial class D3D11Renderer
{
    nint _hwnd;
    bool _forceWarp;

    public static D3D11Renderer? TryCreate(nint hwnd, int width, int height)
    {
        var r = new D3D11Renderer { _hwnd = hwnd };
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
        long tInit = System.Diagnostics.Stopwatch.GetTimestamp();
        static double Ms(long since) => (System.Diagnostics.Stopwatch.GetTimestamp() - since) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        _width = Math.Max(1, width); _height = Math.Max(1, height);
        void* swap = null, dev, ctx;
        int fl, hr;
        if (hwnd == 0) // offscreen: chỉ thiết bị, đích vẽ là texture (dùng cho test parity / chụp ảnh)
        {
            IsWarp = _forceWarp;
            hr = D3D11CreateDevice(null, _forceWarp ? DRIVER_WARP : DRIVER_HARDWARE, 0, CREATE_BGRA_SUPPORT, null, 0, D3D11_SDK_VERSION, &dev, &fl, &ctx);
            if (hr < 0 && !_forceWarp) { IsWarp = true; hr = D3D11CreateDevice(null, DRIVER_WARP, 0, CREATE_BGRA_SUPPORT, null, 0, D3D11_SDK_VERSION, &dev, &fl, &ctx); }
            Check(hr, "D3D11CreateDevice");
        }
        else
        {
            var sd = new DXGI_SWAP_CHAIN_DESC
            {
                Width = (uint)_width, Height = (uint)_height, Format = DXGI_FORMAT_B8G8R8A8_UNORM,
                SampleCount = 1, BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT, BufferCount = 2,
                OutputWindow = hwnd, Windowed = 1, SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD,
            };
            IsWarp = _forceWarp;
            hr = D3D11CreateDeviceAndSwapChain(null, _forceWarp ? DRIVER_WARP : DRIVER_HARDWARE, 0, CREATE_BGRA_SUPPORT, null, 0,
                D3D11_SDK_VERSION, &sd, &swap, &dev, &fl, &ctx);
            if (hr < 0 && !_forceWarp) // không có GPU phù hợp → WARP (rasterizer phần mềm của Windows)
            {
                IsWarp = true;
                hr = D3D11CreateDeviceAndSwapChain(null, DRIVER_WARP, 0, CREATE_BGRA_SUPPORT, null, 0,
                    D3D11_SDK_VERSION, &sd, &swap, &dev, &fl, &ctx);
            }
            Check(hr, "D3D11CreateDeviceAndSwapChain");
        }
        _swap = swap; _device = dev; _ctx = ctx;

        PerfLog.Shared?.Startup("d3d-device", Ms(tInit));
        long tShader = System.Diagnostics.Stopwatch.GetTimestamp();
        CreateTarget();
        CompileShaders();
        PerfLog.Shared?.Startup("d3d-shaders", Ms(tShader));
        long tRest = System.Diagnostics.Stopwatch.GetTimestamp();

        var blend = new BLEND_DESC();
        blend.RenderTarget0 = new RENDER_TARGET_BLEND_DESC
        {
            BlendEnable = 1, SrcBlend = 2 /*ONE*/, DestBlend = 6 /*INV_SRC_ALPHA*/, BlendOp = 1,
            SrcBlendAlpha = 2, DestBlendAlpha = 6, BlendOpAlpha = 1, RenderTargetWriteMask = 0xF,
        };
        void* bs; Check(CreateBlendState(_device, &blend, &bs), "CreateBlendState"); _blend = bs;

        var rd = new RASTERIZER_DESC { FillMode = 3 /*SOLID*/, CullMode = 1 /*NONE*/, DepthClipEnable = 1, ScissorEnable = 1 };
        void* rs; Check(CreateRasterizerState(_device, &rd, &rs), "CreateRasterizerState"); _raster = rs;

        var smp = new SAMPLER_DESC { Filter = 0 /*POINT*/, AddressU = 3, AddressV = 3, AddressW = 3, ComparisonFunc = 1, MaxLOD = float.MaxValue };
        void* ss; Check(CreateSamplerState(_device, &smp, &ss), "CreateSamplerState"); _sampler = ss;
        smp.Filter = 0x15; // MIN_MAG_MIP_LINEAR – cho glyph bị xoay
        void* sl; Check(CreateSamplerState(_device, &smp, &sl), "CreateSamplerState(linear)"); _samplerLinear = sl;

        var cbd = new BUFFER_DESC { ByteWidth = 16, Usage = USAGE_DEFAULT, BindFlags = BIND_CONSTANT_BUFFER };
        void* cb; Check(CreateBuffer(_device, &cbd, null, &cb), "CreateBuffer(cb)"); _cb = cb;

        CreateAtlas(AtlasMin);

        EnsureInstanceCapacity(1024);
        EnsureShapeCapacity(16);
        InitGpuTimer();
        PerfLog.Shared?.Startup("d3d-resources", Ms(tRest));
    }

    // Atlas glyph R8: bắt đầu AtlasMin² (256 KB) và nở gấp đôi khi đầy tới AtlasMax² (4 MB); nở = tạo lại texture, glyph nạp lại theo nhu cầu.
    const int AtlasMin = 512, AtlasMax = 2048;
    int _atlasSide;
    public int AtlasSide => _atlasSide;

    void CreateAtlas(int side)
    {
        Release(ref _atlasSrv); Release(ref _atlas);
        var td = new TEXTURE2D_DESC
        {
            Width = (uint)side, Height = (uint)side, MipLevels = 1, ArraySize = 1, Format = DXGI_FORMAT_R8_UNORM,
            SampleCount = 1, Usage = USAGE_DEFAULT, BindFlags = BIND_SHADER_RESOURCE,
        };
        void* tex; CheckDev(CreateTexture2D(_device, &td, null, &tex), "CreateTexture2D(atlas)"); _atlas = tex;
        void* tsrv; Check(CreateShaderResourceView(_device, _atlas, null, &tsrv), "CreateSRV(atlas)"); _atlasSrv = tsrv;
        _atlasSide = side;
        Array.Fill(_glyphX, -1);
        _shelfX = _shelfY = _shelfH = 0;
    }

    void CreateTarget()
    {
        void* back;
        if (_swap == null) { CreateOffscreenTarget(); return; }
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
        if (_swap == null) { CreateOffscreenTarget(); return; }
        void* none = null;
        OMSetRenderTargets(_ctx, 1, &none, null); // backbuffer còn gắn vào pipeline thì ResizeBuffers báo lỗi
        CheckDev(Inject(ResizeBuffers(_swap, 0, (uint)w, (uint)h, DXGI_FORMAT_UNKNOWN, 0)), "ResizeBuffers");
        CreateTarget();
    }
}
