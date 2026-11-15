using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using static GotX.Platform.Windows.D3D;

namespace GotX.Platform.Windows;

internal sealed unsafe partial class D3D11Renderer
{
    [StructLayout(LayoutKind.Sequential)]
    struct Instance
    {
        public Vector4 Bounds;  // x, y, w, h (device px, trước khi xoay)
        public Vector4 Clip;    // x0, y0, x1, y1
        public Vector4 Color;   // rgba không premultiply
        public Vector4 BorderT, BorderR, BorderB, BorderL; // màu viền từng cạnh
        public Vector4 Widths;  // độ rộng viền: top, right, bottom, left
        public Vector4 Corners; // tl, tr, br, bl
        public Vector4 Misc;    // kind, 1/rộng ảnh, blur, 1/cao ảnh
        public Vector4 Uv;      // glyph / ảnh: u, v, du, dv
        public Vector4 Xform;   // M11, M12, M21, M22 (biến đổi quanh tâm; đơn vị nếu không có)
        public Vector4 Extra;   // kiểu viền, chỉ số vùng cắt theo hình (1-based), chỉ số gradient (1-based), 0
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ShapeData
    {
        public Vector4 Rect;    // x, y, w, h (device px, trước biến đổi)
        public Vector4 Corners;
        public Vector4 Xform;
    }

    const string Hlsl = """
        cbuffer Globals : register(b0) { float2 viewport; float baseInstance; float pad; };
        struct Inst {
            float4 bounds; float4 clip; float4 color;
            float4 bt; float4 br; float4 bb; float4 bl; float4 widths;
            float4 corners; float4 misc; float4 uv; float4 xform; float4 extra;
        };
        struct Shape { float4 rect; float4 corners; float4 xform; };
        StructuredBuffer<Inst> insts : register(t0);
        Texture2D<float> atlas : register(t1);
        StructuredBuffer<Shape> shapes : register(t2);
        Texture2D<float4> image : register(t3);
        SamplerState sampPoint : register(s0);
        SamplerState sampLinear : register(s1);

        // local: toạ độ trước biến đổi (để tính SDF); device: toạ độ màn hình (để cắt)
        struct VOut { float4 pos : SV_Position; nointerpolation uint id : INST; float2 local : LOCAL; float2 device : DEVICE; float2 uv : UV; };

        float2 mul2(float4 m, float2 v) { return float2(m.x * v.x + m.y * v.y, m.z * v.x + m.w * v.y); }
        float4 inv2(float4 m) { float d = m.x * m.w - m.y * m.z; return float4(m.w, -m.y, -m.z, m.x) / d; }

        VOut vs(uint vid : SV_VertexID, uint iid : SV_InstanceID)
        {
            // SV_InstanceID luôn đếm từ 0 trong mỗi lệnh vẽ → cộng vị trí bắt đầu của lượt vẽ
            uint id = iid + (uint)baseInstance;
            Inst it = insts[id];
            float2 corner = float2(vid & 1, vid >> 1);
            float4 b = it.bounds;
            if (it.misc.x == 1) { float m = it.misc.z * 1.5 + 1; b = float4(b.xy - m, b.zw + 2 * m); }
            else if (it.misc.x == 0 || it.misc.x == 3) { b = float4(b.xy - 1, b.zw + 2); }
            float2 p = b.xy + corner * b.zw;
            float2 center = it.bounds.xy + it.bounds.zw * 0.5;
            float2 d = center + mul2(it.xform, p - center);
            VOut o;
            o.pos = float4(d / viewport * float2(2, -2) + float2(-1, 1), 0, 1);
            o.id = id;
            o.local = p;
            o.device = d;
            o.uv = it.uv.xy + corner * it.uv.zw;
            return o;
        }

        float sdf(float2 p, float2 hs, float r)
        {
            float2 q = abs(p) - hs + r;
            return length(max(q, 0)) + min(max(q.x, q.y), 0) - r;
        }

        float pick(float4 c, float2 p) { return p.x < 0 ? (p.y < 0 ? c.x : c.w) : (p.y < 0 ? c.y : c.z); }

        float erf_(float x)
        {
            float s = sign(x); x = abs(x);
            float t = 1 / (1 + 0.3275911 * x);
            float y = 1 - ((((1.061405429 * t - 1.453152027) * t + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * exp(-x * x);
            return s * y;
        }

        float4 premul(float4 c) { return float4(c.rgb * c.a, c.a); }

        // Độ phủ của vùng cắt theo hình (bo góc, xoay) tại điểm màn hình pd
        float clipShape(uint idx, float2 pd)
        {
            if (idx == 0) return 1;
            Shape sh = shapes[idx - 1];
            float2 hs = sh.rect.zw * 0.5;
            float2 lp = mul2(inv2(sh.xform), pd - (sh.rect.xy + hs));
            float4 cr = min(sh.corners, min(hs.x, hs.y));
            return saturate(0.5 - sdf(lp, hs, pick(cr, lp)));
        }

        // 0 trên, 1 phải, 2 dưới, 3 trái – cạnh có khoảng cách chuẩn hoá nhỏ nhất (góc viền chéo như CSS)
        int sideOf(float2 lp, float2 hs, float4 w)
        {
            float big = 1e9;
            float dt = w.x > 0 ? (lp.y + hs.y) / w.x : big;
            float dr = w.y > 0 ? (hs.x - lp.x) / w.y : big;
            float db = w.z > 0 ? (hs.y - lp.y) / w.z : big;
            float dl = w.w > 0 ? (lp.x + hs.x) / w.w : big;
            float m = min(min(dt, dr), min(db, dl));
            return m == dt ? 0 : (m == dr ? 1 : (m == db ? 2 : 3));
        }

        // Viền gạch (1) / chấm (2): tham số theo chiều dài cạnh, chia đều, luôn có gạch/chấm ở góc
        float pattern(float style, int side, float2 lp, float2 hs, float4 w, float band)
        {
            float t, len, across, wd;
            if (side == 0) { t = lp.x + hs.x; len = 2 * hs.x; across = lp.y + hs.y; wd = w.x; }
            else if (side == 1) { t = lp.y + hs.y; len = 2 * hs.y; across = hs.x - lp.x; wd = w.y; }
            else if (side == 2) { t = lp.x + hs.x; len = 2 * hs.x; across = hs.y - lp.y; wd = w.z; }
            else { t = lp.y + hs.y; len = 2 * hs.y; across = lp.x + hs.x; wd = w.w; }
            if (wd <= 0) return 0;
            if (style == 1)
            {
                float period = len / max(1, round(len / (4 * wd)));
                float u = t - round(t / period) * period;
                return band * saturate(period / 4 - abs(u) + 0.5);
            }
            float period2 = len / max(1, round(len / (2 * wd)));
            float u2 = t - round(t / period2) * period2, v2 = across - wd / 2;
            return saturate(wd / 2 - sqrt(u2 * u2 + v2 * v2) + 0.5);
        }

        float4 ps(VOut i) : SV_Target
        {
            Inst it = insts[i.id];
            float2 pd = i.device;
            if (pd.x < it.clip.x || pd.y < it.clip.y || pd.x >= it.clip.z || pd.y >= it.clip.w) discard;
            float clipCov = clipShape((uint)it.extra.y, pd);
            if (clipCov <= 0) discard;
            float kind = it.misc.x;
            bool transformed = any(it.xform != float4(1, 0, 0, 1));
            if (kind == 2)
            {
                float m = transformed ? atlas.SampleLevel(sampLinear, i.uv, 0) : atlas.SampleLevel(sampPoint, i.uv, 0);
                float a = m * it.color.a * clipCov;
                return float4(it.color.rgb * a, a);
            }
            float2 hs = it.bounds.zw * 0.5;
            float2 lp = i.local - (it.bounds.xy + hs);
            float4 cr = min(it.corners, max(min(hs.x, hs.y), 0));
            float r = pick(cr, lp);
            float d = sdf(lp, hs, r);
            if (kind == 1)
            {
                float sigma = it.misc.z * 0.5;
                float a = sigma > 0 ? 0.5 * (1 - erf_(d / (sigma * 1.41421356))) : saturate(0.5 - d);
                a *= it.color.a * clipCov;
                return float4(it.color.rgb * a, a);
            }
            float cov = saturate(0.5 - d) * clipCov;
            if (kind == 3)
            {
                // Ảnh: vùng nguồn kéo giãn vào bounds; kẹp nửa texel để mép không lấy lẫn phần bị cắt
                float2 t = (i.local - it.bounds.xy) / it.bounds.zw;
                float2 halfTexel = float2(it.misc.y, it.misc.w) * 0.5;
                float2 uv = clamp(it.uv.xy + t * it.uv.zw, it.uv.xy + halfTexel, it.uv.xy + it.uv.zw - halfTexel);
                return image.SampleLevel(sampLinear, uv, 0) * (cov * it.color.a);
            }
            float4 col = premul(it.color);
            if (it.extra.z > 0) { float4 g = gradColor((uint)it.extra.z, lp, hs); col = g + col * (1 - g.a); } // background-image trên màu nền
            float4 w = min(it.widths, float4(it.bounds.w, it.bounds.z, it.bounds.w, it.bounds.z)); // t, r, b, l
            float style = it.extra.x;
            if (any(w > 0) && style != 3)
            {
                // hộp bên trong lệch tâm theo độ dày từng cạnh; bán kính trong = bán kính ngoài - cạnh dày hơn
                float2 i0 = float2(-hs.x + w.w, -hs.y + w.x), i1 = float2(hs.x - w.y, hs.y - w.z);
                float2 ic = (i0 + i1) * 0.5, ihs = (i1 - i0) * 0.5;
                float4 ir = max(cr - float4(max(w.w, w.x), max(w.y, w.x), max(w.y, w.z), max(w.w, w.z)), 0);
                float2 e = lp - ic;
                float din = (ihs.x <= 0 || ihs.y <= 0) ? 1 : sdf(e, ihs, pick(ir, e));
                float band = 1 - saturate(0.5 - din);
                int side = sideOf(lp, hs, w);
                float amount = style == 0 ? band : pattern(style, side, lp, hs, w, band);
                float4 sc = side == 0 ? it.bt : (side == 1 ? it.br : (side == 2 ? it.bb : it.bl));
                col = lerp(col, premul(sc), amount);
            }
            return col * cov;
        }
        """;
}
