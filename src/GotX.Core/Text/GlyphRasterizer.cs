namespace GotX.Text;

/// <summary>Bitmap alpha của một glyph. Left/Top là offset (device px) so với điểm gốc trên baseline.</summary>
public sealed class GlyphMask
{
    public int Width, Height, Left, Top;
    public byte[] Alpha = Array.Empty<byte>();
}

/// <summary>
/// Rasterizer tích luỹ diện tích có dấu (xem THIRD_PARTY_NOTICES.md): mỗi đoạn thẳng cộng
/// phần diện tích phủ vào buffer, sau đó cộng dồn theo hàng để ra độ phủ anti-alias.
/// </summary>
internal sealed class GlyphRasterizer : IOutlineSink
{
    float[] _acc = Array.Empty<float>();
    int _w, _h, _len;
    float _cx, _cy, _sx, _sy;

    public void Reset(int w, int h)
    {
        _w = w; _h = h; _len = w * h + 4;
        if (_acc.Length < _len) _acc = new float[_len];
        else Array.Clear(_acc, 0, _len);
    }

    public void MoveTo(float x, float y) { _cx = _sx = x; _cy = _sy = y; }
    public void LineTo(float x, float y) { Line(_cx, _cy, x, y); _cx = x; _cy = y; }
    public void Close() { if (_cx != _sx || _cy != _sy) Line(_cx, _cy, _sx, _sy); _cx = _sx; _cy = _sy; }

    public void QuadTo(float x1, float y1, float x2, float y2)
    {
        float x0 = _cx, y0 = _cy;
        float devx = x0 - 2 * x1 + x2, devy = y0 - 2 * y1 + y2;
        float devsq = devx * devx + devy * devy;
        if (devsq < 0.333f) { LineTo(x2, y2); return; }
        int n = 1 + (int)MathF.Sqrt(MathF.Sqrt(3f * devsq));
        for (int i = 1; i <= n; i++)
        {
            float t = (float)i / n, mt = 1 - t;
            LineTo(mt * mt * x0 + 2 * mt * t * x1 + t * t * x2,
                   mt * mt * y0 + 2 * mt * t * y1 + t * t * y2);
        }
    }

    void Add(int i, float v) { if ((uint)i < (uint)_len) _acc[i] += v; }

    void Line(float px0, float py0, float px1, float py1)
    {
        if (py0 == py1) return;
        float dir, ax, ay, bx, by;
        if (py0 < py1) { dir = 1; ax = px0; ay = py0; bx = px1; by = py1; }
        else { dir = -1; ax = px1; ay = py1; bx = px0; by = py0; }
        float dxdy = (bx - ax) / (by - ay);
        float x = ax;
        int yStart = ay < 0 ? 0 : (int)ay;
        if (ay < 0) x -= ay * dxdy;
        int yEnd = Math.Min(_h, (int)MathF.Ceiling(by));
        for (int y = yStart; y < yEnd; y++)
        {
            int ls = y * _w;
            float dy = MathF.Min(y + 1f, by) - MathF.Max(y, ay);
            float xnext = x + dxdy * dy;
            float d = dy * dir;
            float x0 = MathF.Min(x, xnext), x1 = MathF.Max(x, xnext);
            float x0floor = MathF.Floor(x0);
            int x0i = (int)x0floor;
            float x1ceil = MathF.Ceiling(x1);
            int x1i = (int)x1ceil;
            if (x1i <= x0i + 1)
            {
                float xmf = 0.5f * (x + xnext) - x0floor;
                Add(ls + x0i, d - d * xmf);
                Add(ls + x0i + 1, d * xmf);
            }
            else
            {
                float s = 1f / (x1 - x0);
                float x0f = x0 - x0floor;
                float a0 = 0.5f * s * (1 - x0f) * (1 - x0f);
                float x1f = x1 - x1ceil + 1;
                float am = 0.5f * s * x1f * x1f;
                Add(ls + x0i, d * a0);
                if (x1i == x0i + 2) Add(ls + x0i + 1, d * (1 - a0 - am));
                else
                {
                    float a1 = s * (1.5f - x0f);
                    Add(ls + x0i + 1, d * (a1 - a0));
                    for (int xi = x0i + 2; xi < x1i - 1; xi++) Add(ls + xi, d * s);
                    float a2 = a1 + (x1i - x0i - 3) * s;
                    Add(ls + x1i - 1, d * (1 - a2 - am));
                }
                Add(ls + x1i, d * am);
            }
            x = xnext;
        }
    }

    public byte[] ToAlpha()
    {
        var a = new byte[_w * _h];
        float acc = 0;
        for (int i = 0; i < a.Length; i++)
        {
            acc += _acc[i];
            a[i] = (byte)(MathF.Min(MathF.Abs(acc), 1f) * 255f + 0.5f);
        }
        return a;
    }
}
