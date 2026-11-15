using System.Buffers;

namespace GotX.Raster;

public static partial class SoftwareRenderer
{
    /// <summary>
    /// Source-over của MỘT màu premultiplied lặp lại trên nhiều pixel (ruột bóng đổ, nền trong suốt): mỗi kênh đích chỉ nhận
    /// 256 giá trị nên kết quả mỗi kênh được tính sẵn thành bảng. Công thức từng kênh y hệt <see cref="Blend"/> nên đúng từng bit.
    /// Chỉ dựng bảng khi cùng một màu đã lặp lại vài chục lần liên tiếp (dựng tốn ~1000 phép tính).
    /// </summary>
    ref struct BlendRun
    {
        const int BuildAfter = 48;
        uint[]? _lut; // [0..255] B, [256..511] G<<8, [512..767] R<<16, [768..1023] A<<24
        float _r, _g, _b, _a;
        int _repeat;

        public void Blend(ref uint dst, float r, float g, float b, float a)
        {
            if (a <= 0) return;
            if (a >= 0.999f) { dst = Canvas.Pack(r, g, b, 1); return; }
            if (_lut is not null && a == _a && r == _r && g == _g && b == _b)
            {
                uint d = dst;
                dst = _lut[768 + (int)(d >> 24)] | _lut[512 + (int)((d >> 16) & 0xFF)] | _lut[256 + (int)((d >> 8) & 0xFF)] | _lut[(int)(d & 0xFF)];
                return;
            }
            if (a == _a && r == _r && g == _g && b == _b)
            {
                if (++_repeat == BuildAfter) { Build(r, g, b, a); }
            }
            else { _r = r; _g = g; _b = b; _a = a; _repeat = 0; if (_lut is not null) { ArrayPool<uint>.Shared.Return(_lut); _lut = null; } }
            SoftwareRenderer.Blend(ref dst, r, g, b, a);
        }

        void Build(float r, float g, float b, float a)
        {
            var lut = _lut ??= ArrayPool<uint>.Shared.Rent(1024);
            float inv = 1 - a;
            for (int v = 0; v < 256; v++)
            {
                lut[v] = Canvas.Pack(0, 0, b + v / 255f * inv, 0) & 0xFFu;
                lut[256 + v] = Canvas.Pack(0, g + v / 255f * inv, 0, 0) & 0xFF00u;
                lut[512 + v] = Canvas.Pack(r + v / 255f * inv, 0, 0, 0) & 0xFF0000u;
                lut[768 + v] = Canvas.Pack(0, 0, 0, a + v / 255f * inv) & 0xFF000000u;
            }
        }

        public void Dispose()
        {
            if (_lut is not null) { ArrayPool<uint>.Shared.Return(_lut); _lut = null; }
        }
    }
}
