using System.Numerics;
using System.Runtime.InteropServices;
using static GotX.Platform.Windows.D3D;

namespace GotX.Platform.Windows;

/// <summary>
/// Gradient của Quad: mảng riêng (t4) giống vùng cắt theo hình, instance trỏ tới bằng Extra.z (1-based) nên Instance
/// không phình ra. Điểm dừng đổi sang dạng nội suy trên CPU bằng chính <see cref="GradientMath.Prepare"/>;
/// shader chỉ làm phần theo pixel và chép đúng công thức của <see cref="GradientSampler"/>.
/// </summary>
internal sealed unsafe partial class D3D11Renderer
{
    [StructLayout(LayoutKind.Sequential)]
    struct GradData
    {
        public Vector4 C0, C1, C2; // điểm dừng đã premultiply (Oklab hoặc sRGB) + alpha
        public Vector4 Pos;        // p0, p1, p2, số điểm dừng
        public Vector4 Geom;       // loại (1 linear, 2 radial), hướng x, y, cờ (1 = góc đích, +2 = oklab)
    }

    void* _gradBuf, _gradSrv;
    int _gradCap;
    GradData[] _gradStaging = new GradData[8];

    static Vector4 P((float X, float Y, float Z, float A) c) => new(c.X, c.Y, c.Z, c.A);

    void UploadGradients(Scene scene)
    {
        var grads = scene.Gradients;
        EnsureGradientCapacity(Math.Max(1, grads.Length));
        for (int i = 0; i < grads.Length; i++)
        {
            var g = grads[i];
            _gradStaging[i] = new GradData
            {
                C0 = P(GradientMath.Prepare(g.C0, g.Oklab)), C1 = P(GradientMath.Prepare(g.C1, g.Oklab)), C2 = P(GradientMath.Prepare(g.C2, g.Oklab)),
                Pos = new Vector4(g.P0, g.P1, g.P2, g.Count),
                Geom = new Vector4((float)g.Kind, g.DirX, g.DirY, (g.Corner ? 1 : 0) + (g.Oklab ? 2 : 0)),
            };
        }
        if (grads.Length == 0) return;
        MAPPED_SUBRESOURCE m;
        CheckDev(Map(_ctx, _gradBuf, 0, MAP_WRITE_DISCARD, 0, &m), "Map(gradients)");
        fixed (GradData* src = _gradStaging)
            Buffer.MemoryCopy(src, m.pData, (long)_gradCap * sizeof(GradData), (long)grads.Length * sizeof(GradData));
        Unmap(_ctx, _gradBuf, 0);
    }

    void EnsureGradientCapacity(int count)
    {
        if (count <= _gradCap) return;
        int cap = Math.Max(count, Math.Max(8, _gradCap * 2));
        Release(ref _gradSrv); Release(ref _gradBuf);
        var bd = new BUFFER_DESC
        {
            ByteWidth = (uint)(cap * sizeof(GradData)), Usage = USAGE_DYNAMIC, BindFlags = BIND_SHADER_RESOURCE,
            CPUAccessFlags = CPU_ACCESS_WRITE, MiscFlags = MISC_BUFFER_STRUCTURED, StructureByteStride = (uint)sizeof(GradData),
        };
        void* buf; CheckDev(CreateBuffer(_device, &bd, null, &buf), "CreateBuffer(gradients)"); _gradBuf = buf;
        void* srv; CheckDev(CreateShaderResourceView(_device, _gradBuf, null, &srv), "CreateSRV(gradients)"); _gradSrv = srv;
        _gradCap = cap;
        if (_gradStaging.Length < cap) _gradStaging = new GradData[cap];
    }

    void ReleaseGradients() { Release(ref _gradSrv); Release(ref _gradBuf); }

    // Ghép trước Hlsl chính: ps gọi gradColor(chỉ số, toạ độ cục bộ, nửa kích thước) → sRGB premultiplied.
    const string GradientHlsl = """
        struct Grad { float4 c0; float4 c1; float4 c2; float4 pos; float4 geom; };
        StructuredBuffer<Grad> grads : register(t4);

        float gradT(Grad g, float2 lp, float2 hs)
        {
            if (g.geom.x == 2) { float2 r = max(hs, 1e-4) * 1.41421356; return length(lp / r); }
            float2 d = g.geom.yz;
            if (g.geom.w == 1 || g.geom.w == 3) { d = float2(d.x * hs.y, d.y * hs.x); float n = length(d); if (n > 0) d /= n; }
            float len = 2 * (abs(hs.x * d.x) + abs(hs.y * d.y));
            return len > 0 ? dot(lp, d) / len + 0.5 : 0.5;
        }

        float4 gradMix(float4 a, float4 b, float p0, float p1, float t)
        {
            float f = p1 > p0 ? (t - p0) / (p1 - p0) : 1;
            return a + (b - a) * f;
        }

        float gradSrgb(float c) { c = saturate(c); return c <= 0.0031308 ? 12.92 * c : 1.055 * pow(c, 1 / 2.4) - 0.055; }

        float4 gradColor(uint idx, float2 lp, float2 hs)
        {
            Grad g = grads[idx - 1];
            float t = gradT(g, lp, hs);
            bool three = g.pos.w == 3;
            float4 last = three ? g.c2 : g.c1;
            float pl = three ? g.pos.z : g.pos.y;
            float4 c;
            if (t <= g.pos.x) c = g.c0;
            else if (t >= pl) c = last;
            else if (t < g.pos.y || !three) c = gradMix(g.c0, g.c1, g.pos.x, g.pos.y, t);
            else c = gradMix(g.c1, g.c2, g.pos.y, g.pos.z, t);
            if (c.a <= 0) return float4(0, 0, 0, 0);
            if (g.geom.w < 2) return float4(clamp(c.rgb, 0, c.a), c.a);
            float3 lab = c.xyz / c.a;
            float l = lab.x + 0.3963377774 * lab.y + 0.2158037573 * lab.z;
            float m = lab.x - 0.1055613458 * lab.y - 0.0638541728 * lab.z;
            float s = lab.x - 0.0894841775 * lab.y - 1.2914855480 * lab.z;
            l = l * l * l; m = m * m * m; s = s * s * s;
            float3 rgb = float3(
                gradSrgb(4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s),
                gradSrgb(-1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s),
                gradSrgb(-0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s));
            return float4(rgb * c.a, c.a);
        }

        """;
}
