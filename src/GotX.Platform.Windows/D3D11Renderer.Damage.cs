using GotX.Raster;
using static GotX.Platform.Windows.D3D;

namespace GotX.Platform.Windows;

/// <summary>
/// Vùng hỏng cho D3D11 (prototype, mặc định TẮT: gán <see cref="Damage"/> để bật). Frame không đổi → Render trả về ngay (không encode, không Present).
/// Frame đổi một phần → chỉ primitive chạm hộp bao các ô hỏng được dựng instance và vẽ, có scissor, vào texture đích giữ lại.
/// Ràng buộc flip-model: swap chain FLIP_DISCARD xoay nhiều back buffer và nội dung sau Present là không xác định, nên không thể "chỉ vẽ phần đổi"
/// thẳng vào back buffer; phải vẽ vào texture riêng (giữ nguyên giữa các frame) rồi CopyResource cả texture sang back buffer mỗi lần Present.
/// Chế độ offscreen không có flip nên đích vẽ chính là đích giữ lại (không cần chép).
/// </summary>
internal sealed unsafe partial class D3D11Renderer
{
    /// <summary>Bộ theo dõi vùng hỏng; null = tắt (vẽ toàn frame như cũ).</summary>
    public SceneDamage? Damage { get; set; }

    bool _useDamage;
    Bounds _dmgRect;
    void* _retTex, _retRtv;
    int _retW, _retH;
    Primitive[] _vis = new Primitive[256];

    /// <summary>false nếu frame này giống hệt frame trước (bỏ qua hoàn toàn). Đặt <c>_useDamage</c>/<c>_dmgRect</c> cho RenderCore.</summary>
    bool PlanDamage(Scene scene, int width, int height)
    {
        _useDamage = false;
        if (Damage is not { } d) return true;
        var r = d.Update(scene, width, height);
        if (r.Empty && _frame > 0) { GpuCounters.Last = default; GpuCounters.Skipped++; return false; }
        if (!r.Full && r.Coverage <= 0.75) { _useDamage = true; _dmgRect = r.BoundingRect(width, height); }
        return true;
    }

    /// <summary>Đích RenderCore vẽ vào: back buffer / texture offscreen, hoặc texture giữ lại khi bật vùng hỏng trên swap chain.</summary>
    void* DrawTarget(int width, int height)
    {
        if (Damage == null || _swap == null) return _rtv;
        if (_retTex == null || _retW != width || _retH != height)
        {
            ReleaseRetained();
            var td = new TEXTURE2D_DESC { Width = (uint)width, Height = (uint)height, MipLevels = 1, ArraySize = 1, Format = DXGI_FORMAT_B8G8R8A8_UNORM, SampleCount = 1, Usage = USAGE_DEFAULT, BindFlags = BIND_RENDER_TARGET };
            void* tex; CheckDev(CreateTexture2D(_device, &td, null, &tex), "CreateTexture2D(retained)"); _retTex = tex;
            void* rtv; CheckDev(CreateRenderTargetView(_device, _retTex, null, &rtv), "CreateRTV(retained)"); _retRtv = rtv;
            _retW = width; _retH = height;
            Damage.Invalidate(); // nội dung mới toanh: lần sau phải vẽ cả frame
            _useDamage = false;
        }
        return _retRtv;
    }

    void CopyRetainedToBackbuffer()
    {
        void* back;
        Guid iid = IID_ID3D11Texture2D;
        CheckDev(GetBuffer(_swap, 0, &iid, &back), "GetBuffer(copy)");
        CopyResource(_ctx, back, _retTex);
        Release(ref back);
    }

    void ReleaseRetained() { Release(ref _retRtv); Release(ref _retTex); _retW = _retH = 0; }

    /// <summary>Quad xoá nền phủ hộp hỏng, rồi mọi primitive chạm hộp đó (giữ nguyên thứ tự). Hộp là bội của ô 64 px nên quad phủ đúng từng pixel.</summary>
    ReadOnlySpan<Primitive> SelectDamaged(Scene scene, ReadOnlySpan<Primitive> all, out int sx, out int sy, out int sw, out int sh)
    {
        var rect = _dmgRect;
        sx = (int)rect.X; sy = (int)rect.Y; sw = (int)rect.Width; sh = (int)rect.Height;
        if (_vis.Length < all.Length + 1) _vis = new Primitive[Math.Max(all.Length + 1, _vis.Length * 2)];
        _vis[0] = new Primitive { Kind = PrimitiveKind.Quad, Bounds = rect, Clip = rect, Color = scene.ClearColor };
        int n = 1;
        for (int i = 0; i < all.Length; i++)
            if (!SceneDamageMath.Visual(scene, all[i]).Intersect(rect).IsEmpty) _vis[n++] = all[i];
        return new ReadOnlySpan<Primitive>(_vis, 0, n);
    }
}
