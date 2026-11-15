using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using static GotX.Platform.Windows.D3D;

namespace GotX.Platform.Windows;

/// <summary>
/// Vẽ Scene bằng D3D11: mọi primitive thành một instance trong StructuredBuffer, một lệnh
/// DrawInstanced cho cả frame (giữ đúng thứ tự vẽ). Pixel shader dùng cùng công thức SDF/erf
/// với renderer CPU nên hai backend cho kết quả gần như trùng nhau.
/// Glyph được xếp vào một atlas R8 2048×2048 theo kiểu "kệ" (shelf packing).
/// </summary>
internal sealed unsafe partial class D3D11Renderer : IDisposable
{
    const int AtlasSize = 2048;

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
    public double BuildMs, SubmitMs, PresentMs;
    public long UploadBytes;

    public void Render(Scene scene, int width, int height)
    {
        if (width <= 0 || height <= 0) return;
        if (width != _width || height != _height) Resize(width, height);

        // GlyphAtlas đã bị xoá và đánh số lại → vị trí trong atlas GPU không còn đúng, xếp lại từ đầu.
        if (scene.Atlas.Generation != _atlasGeneration)
        {
            _atlasGeneration = scene.Atlas.Generation;
            _glyphX.AsSpan().Fill(-1);
            _shelfX = _shelfY = _shelfH = 0;
        }

        long tBegin = System.Diagnostics.Stopwatch.GetTimestamp();
        UploadBytes = 0;
        var prims = scene.Primitives;
        EnsureInstanceCapacity(Math.Max(1, prims.Length));
        // Nếu atlas GPU bị xếp lại giữa chừng, glyph đã dựng trước đó trỏ sai chỗ: dựng lại cả frame một lần.
        // (Frame có nhiều glyph hơn sức chứa atlas thì lần hai vẫn tràn – khi đó chấp nhận như trước.)
        for (int attempt = 0; attempt < 2; attempt++)
        {
            _atlasWrapped = false;
            BuildInstances(scene, prims);
            if (!_atlasWrapped) break;
        }

        if (prims.Length > 0)
        {
            MAPPED_SUBRESOURCE m;
            Check(Map(_ctx, _instBuf, 0, MAP_WRITE_DISCARD, 0, &m), "Map");
            fixed (Instance* src = _staging)
                Buffer.MemoryCopy(src, m.pData, (long)_instCap * sizeof(Instance), (long)prims.Length * sizeof(Instance));
            Unmap(_ctx, _instBuf, 0);
        }

        // Vùng cắt theo hình (bo góc, xoay) – mảng riêng, primitive tham chiếu bằng chỉ số
        var shapes = scene.ClipShapes;
        EnsureShapeCapacity(Math.Max(1, shapes.Length));
        for (int i = 0; i < shapes.Length; i++)
        {
            var sh = shapes[i];
            _shapeStaging[i] = new ShapeData
            {
                Rect = new Vector4(sh.Rect.X, sh.Rect.Y, sh.Rect.Width, sh.Rect.Height),
                Corners = new Vector4(sh.Corners.TopLeft, sh.Corners.TopRight, sh.Corners.BottomRight, sh.Corners.BottomLeft),
                Xform = M(sh.Transform),
            };
        }
        if (shapes.Length > 0)
        {
            MAPPED_SUBRESOURCE ms;
            Check(Map(_ctx, _shapeBuf, 0, MAP_WRITE_DISCARD, 0, &ms), "Map(shapes)");
            fixed (ShapeData* src = _shapeStaging)
                Buffer.MemoryCopy(src, ms.pData, (long)_shapeCap * sizeof(ShapeData), (long)shapes.Length * sizeof(ShapeData));
            Unmap(_ctx, _shapeBuf, 0);
        }

        UploadGradients(scene);
        long tBuilt = System.Diagnostics.Stopwatch.GetTimestamp();

        GpuBegin();
        void* rtv = _rtv;
        OMSetRenderTargets(_ctx, 1, &rtv, null);
        var cc = scene.ClearColor;
        float* clear = stackalloc float[4] { cc.R * cc.A, cc.G * cc.A, cc.B * cc.A, cc.A };
        ClearRenderTargetView(_ctx, _rtv, clear);

        if (prims.Length > 0)
        {
            var vp = new VIEWPORT { Width = width, Height = height, MaxDepth = 1 };
            RSSetViewports(_ctx, 1, &vp);
            RSSetState(_ctx, _raster);
            IASetInputLayout(_ctx, null);
            IASetPrimitiveTopology(_ctx, TOPOLOGY_TRIANGLESTRIP);
            VSSetShader(_ctx, _vs);
            PSSetShader(_ctx, _ps);
            void* cb = _cb;
            VSSetConstantBuffers(_ctx, 0, 1, &cb);
            PSSetConstantBuffers(_ctx, 0, 1, &cb);
            void* instSrv = _instSrv;
            VSSetShaderResources(_ctx, 0, 1, &instSrv);
            void** psViews = stackalloc void*[3] { _instSrv, _atlasSrv, _shapeSrv };
            PSSetShaderResources(_ctx, 0, 3, psViews);
            void* gradSrv = _gradSrv;
            PSSetShaderResources(_ctx, 4, 1, &gradSrv);
            void** samplers = stackalloc void*[2] { _sampler, _samplerLinear };
            PSSetSamplers(_ctx, 0, 2, samplers);
            OMSetBlendState(_ctx, _blend, null, 0xFFFFFFFF);

            // Một lượt vẽ cho mỗi đoạn primitive dùng cùng một texture ảnh (không có ảnh thì cả frame là một lượt).
            int runStart = 0, runImage = 0;
            for (int i = 0; i < prims.Length; i++)
            {
                if (prims[i].Kind != PrimitiveKind.Image || prims[i].Glyph == runImage) continue;
                if (runImage != 0) { DrawRun(runStart, i - runStart, runImage, width, height); runStart = i; }
                runImage = prims[i].Glyph;
            }
            DrawRun(runStart, prims.Length - runStart, runImage, width, height);
        }
        _frame++;
        EvictImages();

        GpuEnd();
        long tSubmitted = System.Diagnostics.Stopwatch.GetTimestamp();
        Present(_swap, 1, 0); // chờ vsync
        long tPresented = System.Diagnostics.Stopwatch.GetTimestamp();
        double toMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        BuildMs = (tBuilt - tBegin) * toMs; SubmitMs = (tSubmitted - tBuilt) * toMs; PresentMs = (tPresented - tSubmitted) * toMs;
    }

    void DrawRun(int start, int count, int imageId, int width, int height)
    {
        Vector4 globals = new(width, height, start, 0);
        UpdateSubresource(_ctx, _cb, 0, null, &globals, 0, 0);
        void* view = imageId != 0 && _images.TryGetValue(imageId, out var entry) ? (void*)entry.View : null;
        PSSetShaderResources(_ctx, 3, 1, &view);
        DrawInstanced(_ctx, 4, (uint)count, 0, 0);
    }

    public void Dispose()
    {
        ReleaseGpuTimer();
        ReleaseImages();
        Release(ref _instSrv); Release(ref _instBuf); Release(ref _atlasSrv); Release(ref _atlas);
        Release(ref _shapeSrv); Release(ref _shapeBuf); ReleaseGradients();
        Release(ref _cb); Release(ref _sampler); Release(ref _samplerLinear); Release(ref _raster); Release(ref _blend);
        Release(ref _ps); Release(ref _vs); Release(ref _rtv);
        Release(ref _ctx); Release(ref _swap); Release(ref _device);
    }
}
