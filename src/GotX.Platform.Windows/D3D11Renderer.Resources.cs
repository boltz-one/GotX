using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using static GotX.Platform.Windows.D3D;

namespace GotX.Platform.Windows;

internal sealed unsafe partial class D3D11Renderer
{
    void EnsureInstanceCapacity(int count)
    {
        if (count <= _instCap) return;
        int cap = Math.Max(count, _instCap * 2);
        Release(ref _instSrv); Release(ref _instBuf);
        var bd = new BUFFER_DESC
        {
            ByteWidth = (uint)(cap * sizeof(Instance)), Usage = USAGE_DYNAMIC, BindFlags = BIND_SHADER_RESOURCE,
            CPUAccessFlags = CPU_ACCESS_WRITE, MiscFlags = MISC_BUFFER_STRUCTURED, StructureByteStride = (uint)sizeof(Instance),
        };
        void* buf; CheckDev(CreateBuffer(_device, &bd, null, &buf), "CreateBuffer(instances)"); _instBuf = buf;
        void* srv; CheckDev(CreateShaderResourceView(_device, _instBuf, null, &srv), "CreateSRV(instances)"); _instSrv = srv;
        _instCap = cap;
        if (_staging.Length < cap) _staging = new Instance[cap];
    }

    void EnsureShapeCapacity(int count)
    {
        if (count <= _shapeCap) return;
        int cap = Math.Max(count, Math.Max(16, _shapeCap * 2));
        Release(ref _shapeSrv); Release(ref _shapeBuf);
        var bd = new BUFFER_DESC
        {
            ByteWidth = (uint)(cap * sizeof(ShapeData)), Usage = USAGE_DYNAMIC, BindFlags = BIND_SHADER_RESOURCE,
            CPUAccessFlags = CPU_ACCESS_WRITE, MiscFlags = MISC_BUFFER_STRUCTURED, StructureByteStride = (uint)sizeof(ShapeData),
        };
        void* buf; CheckDev(CreateBuffer(_device, &bd, null, &buf), "CreateBuffer(shapes)"); _shapeBuf = buf;
        void* srv; CheckDev(CreateShaderResourceView(_device, _shapeBuf, null, &srv), "CreateSRV(shapes)"); _shapeSrv = srv;
        _shapeCap = cap;
        if (_shapeStaging.Length < cap) _shapeStaging = new ShapeData[cap];
    }

    /// <summary>Vị trí glyph trong atlas GPU; tải mask lên nếu chưa có.</summary>
    (int x, int y) GlyphSlot(Text.GlyphAtlas atlas, int index)
    {
        if (index >= _glyphX.Length)
        {
            int n = Math.Max(index + 1, _glyphX.Length * 2);
            int old = _glyphX.Length;
            Array.Resize(ref _glyphX, n); Array.Resize(ref _glyphY, n);
            _glyphX.AsSpan(old).Fill(-1);
        }
        if (_glyphX[index] >= 0) { GpuCounters.Last.AtlasHits++; return (_glyphX[index], _glyphY[index]); }

        var m = atlas[index];
        // Mask lớn hơn atlas: UpdateSubresource ngoài biên là hành vi không xác định (WARP báo mất thiết bị) → nở atlas cho vừa; quá AtlasMax thì bỏ qua glyph.
        if (m.Width + 1 > AtlasMax || m.Height + 1 > AtlasMax) return (-1, -1);
        while (m.Width + 1 > _atlasSide || m.Height + 1 > _atlasSide) { CreateAtlas(_atlasSide * 2); _atlasWrapped = true; }
        if (_shelfX + m.Width + 1 > _atlasSide) { _shelfY += _shelfH + 1; _shelfX = 0; _shelfH = 0; }
        if (_shelfY + m.Height + 1 > _atlasSide)
        {
            // Atlas đầy: nở gấp đôi tới AtlasMax; ở mức tối đa thì xoá hết và xếp lại (đơn giản; đủ cho UI thông thường).
            // Các instance đã dựng trong frame này còn trỏ vào vị trí cũ → Render phải dựng lại.
            if (_atlasSide < AtlasMax) CreateAtlas(_atlasSide * 2);
            else { _glyphX.AsSpan().Fill(-1); _shelfX = _shelfY = _shelfH = 0; }
            _atlasWrapped = true;
        }
        GpuCounters.Last.AtlasMisses++;
        int x = _shelfX, y = _shelfY;
        var box = new BOX { left = (uint)x, top = (uint)y, front = 0, right = (uint)(x + m.Width), bottom = (uint)(y + m.Height), back = 1 };
        fixed (byte* p = m.Alpha) UpdateSubresource(_ctx, _atlas, 0, &box, p, (uint)m.Width, 0);
        UploadBytes += m.Alpha.Length;
        PerfCounters.AtlasUpload++; PerfCounters.AtlasUploadBytes += m.Alpha.Length;
        _shelfX += m.Width + 1;
        _shelfH = Math.Max(_shelfH, m.Height);
        _glyphX[index] = x; _glyphY[index] = y;
        return (x, y);
    }

    static Vector4 V(Rgba c) => new(c.R, c.G, c.B, c.A);

    void BuildInstances(Scene scene, ReadOnlySpan<Primitive> prims)
    {
        float inv = 1f / _atlasSide;
        for (int i = 0; i < prims.Length; i++)
        {
            ref readonly var p = ref prims[i];
            ref var it = ref _staging[i];
            it.Bounds = new Vector4(p.Bounds.X, p.Bounds.Y, p.Bounds.Width, p.Bounds.Height);
            it.Clip = new Vector4(p.Clip.X, p.Clip.Y, p.Clip.Right, p.Clip.Bottom);
            it.Color = V(p.Color);
            it.BorderT = V(p.BorderTop); it.BorderR = V(p.BorderRight); it.BorderB = V(p.BorderBottom); it.BorderL = V(p.BorderLeft);
            it.Widths = new Vector4(p.BorderWidths.Top, p.BorderWidths.Right, p.BorderWidths.Bottom, p.BorderWidths.Left);
            it.Corners = new Vector4(p.Corners.TopLeft, p.Corners.TopRight, p.Corners.BottomRight, p.Corners.BottomLeft);
            it.Misc = new Vector4((float)p.Kind, 0, p.Blur, 0);
            it.Xform = M(p.Transform);
            it.Extra = new Vector4((float)p.BorderStyle, p.ClipShape, p.Gradient, 0);
            if (p.Kind == PrimitiveKind.Glyph)
            {
                var (gx, gy) = GlyphSlot(scene.Atlas, p.Glyph);
                if (gx < 0) { it.Bounds = default; it.Uv = default; continue; }
                it.Uv = new Vector4(gx * inv, gy * inv, p.Bounds.Width * inv, p.Bounds.Height * inv);
            }
            else if (p.Kind == PrimitiveKind.Image) BuildImage(p, ref it);
            else if (p.HasExclusion) it.Uv = new Vector4(p.Source.X, p.Source.Y, p.Source.Width, p.Source.Height);
            else it.Uv = default;
        }
    }

    // Texture của từng ImageSource đang được vẽ (theo Id), kèm frame dùng gần nhất để dọn ảnh không còn hiển thị.
    struct ImageTexture { public nint Texture, View; public int Frame; public long Bytes; }
    readonly Dictionary<int, ImageTexture> _images = new();
    readonly List<int> _stale = new();
    int _frame;

    /// <summary>Điền instance của một ảnh; ảnh đã bị thu hồi thì thành hình rỗng (không vẽ gì).</summary>
    void BuildImage(in Primitive p, ref Instance it)
    {
        if (ImageSource.Find(p.Glyph) is not { } image) { it.Bounds = default; it.Uv = default; return; }
        ImageView(image);
        float iw = 1f / image.Width, ih = 1f / image.Height;
        it.Misc = new Vector4((float)p.Kind, iw, 0, ih);
        it.Uv = new Vector4(p.Source.X * iw, p.Source.Y * ih, p.Source.Width * iw, p.Source.Height * ih);
    }

    /// <summary>Shader resource view của ảnh; tạo texture BGRA và tải pixel lên ở lần đầu.</summary>
    void* ImageView(ImageSource image)
    {
        if (_images.TryGetValue(image.Id, out var entry))
        {
            if (entry.Frame != _frame) { entry.Frame = _frame; _images[image.Id] = entry; }
            return (void*)entry.View;
        }
        var td = new TEXTURE2D_DESC
        {
            Width = (uint)image.Width, Height = (uint)image.Height, MipLevels = 1, ArraySize = 1,
            Format = DXGI_FORMAT_B8G8R8A8_UNORM, SampleCount = 1, Usage = USAGE_DEFAULT, BindFlags = BIND_SHADER_RESOURCE,
        };
        void* tex; CheckDev(CreateTexture2D(_device, &td, null, &tex), "CreateTexture2D(image)");
        void* srv;
        int hr = CreateShaderResourceView(_device, tex, null, &srv);
        if (hr < 0) { Release(ref tex); CheckDev(hr, "CreateSRV(image)"); }
        fixed (byte* px = image.Pixels) UpdateSubresource(_ctx, tex, 0, null, px, (uint)(image.Width * 4), 0);
        UploadBytes += image.Pixels.Length;
        _images[image.Id] = new ImageTexture { Texture = (nint)tex, View = (nint)srv, Frame = _frame, Bytes = image.Pixels.Length };
        _imageBytes += image.Pixels.Length;
        return srv;
    }

    /// <summary>Giải phóng texture của ảnh không được vẽ trong 600 frame gần nhất (~10 giây khi đang vẽ liên tục; cửa sổ đứng yên thì không tính).</summary>
    void EvictImages()
    {
        if (_images.Count == 0) return;
        _stale.Clear();
        foreach (var kv in _images)
            if (_frame - kv.Value.Frame > 600) _stale.Add(kv.Key);
        foreach (int id in _stale) ReleaseImage(id);
    }

    void ReleaseImage(int id)
    {
        if (!_images.Remove(id, out var entry)) return;
        _imageBytes -= entry.Bytes;
        void* view = (void*)entry.View, tex = (void*)entry.Texture;
        Release(ref view); Release(ref tex);
    }

    void ReleaseImages()
    {
        _stale.Clear();
        _stale.AddRange(_images.Keys);
        foreach (int id in _stale) ReleaseImage(id);
    }}
