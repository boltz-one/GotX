using GotX.Raster;
using static GotX.Platform.Windows.D3D;

namespace GotX.Platform.Windows;

/// <summary>
/// Chế độ không cửa sổ: đích vẽ là một texture, đọc lại bằng staging texture. Dùng cho test parity CPU↔GPU và chụp ảnh;
/// cùng đường vẽ với chế độ swap chain (chỉ khác bước Present).
/// </summary>
internal sealed unsafe partial class D3D11Renderer
{
    void* _offTex, _stage;
    int _stageW, _stageH;

    /// <summary>Tạo renderer offscreen. warp = true ép WARP (kết quả xác định, chạy được không cần GPU).</summary>
    public static D3D11Renderer? TryCreateOffscreen(int width, int height, bool warp)
    {
        var r = new D3D11Renderer { _forceWarp = warp };
        try { r.Init(0, width, height); return r; }
        catch (Exception ex) { Console.Error.WriteLine($"D3D11 offscreen: {ex.Message}"); r.Dispose(); return null; }
    }

    void CreateOffscreenTarget()
    {
        Release(ref _offTex); Release(ref _stage);
        var td = new TEXTURE2D_DESC
        {
            Width = (uint)_width, Height = (uint)_height, MipLevels = 1, ArraySize = 1, Format = DXGI_FORMAT_B8G8R8A8_UNORM,
            SampleCount = 1, Usage = USAGE_DEFAULT, BindFlags = BIND_RENDER_TARGET | BIND_SHADER_RESOURCE,
        };
        void* tex; Check(CreateTexture2D(_device, &td, null, &tex), "CreateTexture2D(offscreen)"); _offTex = tex;
        void* rtv; Check(CreateRenderTargetView(_device, _offTex, null, &rtv), "CreateRenderTargetView(offscreen)"); _rtv = rtv;
    }

    void ReleaseOffscreen() { Release(ref _offTex); Release(ref _stage); }

    /// <summary>Chép đích vẽ về canvas (BGRA premultiplied, đúng định dạng Canvas). Chỉ cho renderer offscreen.</summary>
    public void ReadPixels(Canvas canvas)
    {
        if (_offTex == null) throw new InvalidOperationException("ReadPixels chỉ dùng cho renderer offscreen.");
        if (_stage == null || _stageW != _width || _stageH != _height)
        {
            Release(ref _stage);
            var td = new TEXTURE2D_DESC
            {
                Width = (uint)_width, Height = (uint)_height, MipLevels = 1, ArraySize = 1, Format = DXGI_FORMAT_B8G8R8A8_UNORM,
                SampleCount = 1, Usage = USAGE_STAGING, CPUAccessFlags = CPU_ACCESS_READ,
            };
            void* st; Check(CreateTexture2D(_device, &td, null, &st), "CreateTexture2D(staging)"); _stage = st;
            _stageW = _width; _stageH = _height;
        }
        CopyResource(_ctx, _stage, _offTex);
        MAPPED_SUBRESOURCE m;
        CheckDev(Map(_ctx, _stage, 0, MAP_READ, 0, &m), "Map(staging)");
        canvas.Resize(_width, _height);
        for (int y = 0; y < _height; y++)
            new ReadOnlySpan<uint>((byte*)m.pData + (long)y * m.RowPitch, _width).CopyTo(canvas.Pixels.AsSpan(y * _width, _width));
        Unmap(_ctx, _stage, 0);
    }
}
