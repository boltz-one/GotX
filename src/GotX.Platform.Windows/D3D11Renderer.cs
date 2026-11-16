using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using static GotX.Platform.Windows.D3D;

namespace GotX.Platform.Windows;

/// <summary>
/// Vẽ Scene bằng D3D11: mọi primitive thành một instance trong StructuredBuffer, một lệnh
/// DrawInstanced cho cả frame (giữ đúng thứ tự vẽ). Pixel shader dùng cùng công thức SDF/erf
/// với renderer CPU nên hai backend cho kết quả gần như trùng nhau.
/// Glyph được xếp vào một atlas R8 theo kiểu "kệ" (shelf packing); atlas bắt đầu nhỏ và nở dần tới AtlasMax.
/// Mất thiết bị (DXGI_ERROR_DEVICE_REMOVED/RESET) được xử lý ở <see cref="Render"/>: dựng lại mọi thứ, quá nhiều lần liên tiếp thì <see cref="Failed"/>.
/// </summary>
internal sealed unsafe partial class D3D11Renderer : IDisposable
{
    void* _device, _ctx, _swap, _rtv, _vs, _ps, _blend, _raster, _sampler, _samplerLinear, _cb;
    void* _instBuf, _instSrv, _atlas, _atlasSrv, _shapeBuf, _shapeSrv;
    int _instCap, _shapeCap, _width, _height;
    ShapeData[] _shapeStaging = new ShapeData[16];
    Instance[] _staging = new Instance[1024];

    // Atlas: vị trí của từng glyph (theo index trong GlyphAtlas), -1 = chưa tải lên GPU
    int[] _glyphX = Array.Empty<int>(), _glyphY = Array.Empty<int>();
    int _shelfX, _shelfY, _shelfH, _atlasGeneration;
    bool _atlasWrapped;

    D3D11Renderer() { }

    /// <summary>Thời gian CPU của Render gần nhất (ms): dựng instance + tải atlas + map buffer / ghi lệnh vẽ / Present (chờ vsync). UploadBytes = byte atlas+ảnh tải lên.</summary>
    public double BuildMs, SubmitMs, PresentMs, InstMs, MapMs;
    public long UploadBytes;
    public bool IsWarp;

    /// <summary>
    /// Vẽ một frame. Trả false nếu frame không được vẽ (kích thước 0, hoặc <see cref="Failed"/>). Mất thiết bị → dựng lại
    /// thiết bị rồi vẽ lại đúng Scene này; chủ ý không ném exception ra ngoài để cửa sổ vẫn sống.
    /// </summary>
    public bool Render(Scene scene, int width, int height)
    {
        if (Failed || width <= 0 || height <= 0) return false;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                if (_device == null && !Recover(0)) return false;
                if (attempt == 0 && !PlanDamage(scene, width, height)) return true; // không có gì đổi: không encode, không present
                RenderCore(scene, width, height);
                _failures = 0;
                return true;
            }
            catch (DeviceLostException ex) { if (!Recover(ex.Hr, ex.Message)) return false; }
        }
        return false;
    }

    public void Dispose() => ReleaseAll();

    void ReleaseAll()
    {
        ReleaseGpuTimer();
        ReleaseImages();
        ReleaseOffscreen(); ReleaseRetained();
        _useDamage = false; Damage?.Invalidate(); // đích giữ lại đã mất theo thiết bị
        Release(ref _instSrv); Release(ref _instBuf); Release(ref _atlasSrv); Release(ref _atlas);
        Release(ref _shapeSrv); Release(ref _shapeBuf); ReleaseGradients();
        Release(ref _cb); Release(ref _sampler); Release(ref _samplerLinear); Release(ref _raster); Release(ref _blend);
        Release(ref _ps); Release(ref _vs); Release(ref _rtv);
        if (_ctx != null) ClearState(_ctx);
        Release(ref _ctx); Release(ref _swap); Release(ref _device);
        _instCap = _shapeCap = _gradCap = 0;
        Array.Fill(_glyphX, -1);
        _shelfX = _shelfY = _shelfH = 0;
        _atlasGeneration = -1; _atlasSide = 0;
    }
}
