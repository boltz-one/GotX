using System.Numerics;
using static GotX.Platform.Windows.D3D;

namespace GotX.Platform.Windows;

internal sealed unsafe partial class D3D11Renderer
{
    void RenderCore(Scene scene, int width, int height)
    {
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
        GpuCounters.Last = default; _boundImage = 0;
        var prims = scene.Primitives;
        int scX = 0, scY = 0, scW = width, scH = height;
        bool damaged = _useDamage;
        if (damaged) prims = SelectDamaged(scene, prims, out scX, out scY, out scW, out scH);
        EnsureInstanceCapacity(Math.Max(1, prims.Length));
        // Nếu atlas GPU bị xếp lại giữa chừng, glyph đã dựng trước đó trỏ sai chỗ: dựng lại cả frame một lần.
        // (Frame có nhiều glyph hơn sức chứa atlas thì lần hai vẫn tràn – khi đó chấp nhận như trước.)
        for (int attempt = 0; attempt < 4; attempt++) // atlas nở 512→1024→2048 có thể cần vài lượt
        {
            _atlasWrapped = false;
            BuildInstances(scene, prims);
            if (!_atlasWrapped) break;
        }

        long tInst = System.Diagnostics.Stopwatch.GetTimestamp();
        if (prims.Length > 0)
        {
            MAPPED_SUBRESOURCE m;
            CheckDev(Map(_ctx, _instBuf, 0, MAP_WRITE_DISCARD, 0, &m), "Map");
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
            CheckDev(Map(_ctx, _shapeBuf, 0, MAP_WRITE_DISCARD, 0, &ms), "Map(shapes)");
            fixed (ShapeData* src = _shapeStaging)
                Buffer.MemoryCopy(src, ms.pData, (long)_shapeCap * sizeof(ShapeData), (long)shapes.Length * sizeof(ShapeData));
            Unmap(_ctx, _shapeBuf, 0);
        }

        UploadGradients(scene);
        long tBuilt = System.Diagnostics.Stopwatch.GetTimestamp();

        GpuBegin();
        void* rtv = DrawTarget(width, height);
        OMSetRenderTargets(_ctx, 1, &rtv, null);
        var cc = scene.ClearColor;
        float* clear = stackalloc float[4] { cc.R * cc.A, cc.G * cc.A, cc.B * cc.A, cc.A };
        if (!damaged) ClearRenderTargetView(_ctx, rtv, clear); // vùng hỏng được xoá bằng một quad màu nền (ClearRenderTargetView bỏ qua scissor)

        if (prims.Length > 0)
        {
            var vp = new VIEWPORT { Width = width, Height = height, MaxDepth = 1 };
            RSSetViewports(_ctx, 1, &vp);
            RSSetState(_ctx, _raster);
            int* scissor = stackalloc int[4] { scX, scY, scX + scW, scY + scH };
            RSSetScissorRects(_ctx, 1, scissor);
            GpuCounters.Last.PipelineSwitches += 7; // vs, ps, blend, raster, sampler, buffer, SRV – gắn một lần cho cả frame
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
        if (_swap != null && Damage != null) CopyRetainedToBackbuffer();
        _frame++;
        EvictImages();
        EvictOverBudget();

        GpuEnd();
        long tSubmitted = System.Diagnostics.Stopwatch.GetTimestamp();
        CheckDev(Inject(_swap != null ? Present(_swap, 1, 0) : 0), "Present"); // chờ vsync; offscreen không có Present
        int lost = GetDeviceRemovedReason(_device); // draw/copy không trả HRESULT: hỏi lý do để bắt mất thiết bị ngay ở frame gây ra (chuỗi mô tả chỉ dựng khi lỗi: 0 B/frame)
        if (lost < 0) throw new DeviceLostException(lost, $"post-frame ({prims.Length} prims, {shapes.Length} shapes, {scene.Gradients.Length} grads, atlas {_atlasSide})", lost);
        long tPresented = System.Diagnostics.Stopwatch.GetTimestamp();
        double toMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        InstMs = (tInst - tBegin) * toMs; MapMs = (tBuilt - tInst) * toMs;
        TrimBuffers(prims.Length, shapes.Length);
        var gc = GpuCounters.Last;
        gc.Primitives = prims.Length; gc.EncodeMs = (tSubmitted - tBegin) * toMs; gc.GpuMs = GpuMs;
        gc.InstanceBytes = (long)prims.Length * sizeof(Instance) + (long)shapes.Length * sizeof(ShapeData) + (long)scene.Gradients.Length * sizeof(GradData);
        GpuCounters.Commit(gc, _swap != null);
        BuildMs = (tBuilt - tBegin) * toMs; SubmitMs = (tSubmitted - tBuilt) * toMs; PresentMs = (tPresented - tSubmitted) * toMs;
    }

    int _boundImage;

    void DrawRun(int start, int count, int imageId, int width, int height)
    {
        Vector4 globals = new(width, height, start, 0);
        UpdateSubresource(_ctx, _cb, 0, null, &globals, 0, 0);
        void* view = imageId != 0 && _images.TryGetValue(imageId, out var entry) ? (void*)entry.View : null;
        if (imageId != _boundImage) { _boundImage = imageId; GpuCounters.Last.TextureSwitches++; }
        PSSetShaderResources(_ctx, 3, 1, &view);
        DrawInstanced(_ctx, 4, (uint)count, 0, 0);
        GpuCounters.Last.DrawCalls++;
    }
}
