using System.Globalization;

namespace GotX;

/// <summary>Gom các đoạn thẳng thành đường gấp khúc; đường cong được chia nhỏ tới sai số <c>tolerance</c> (đơn vị viewBox).</summary>
internal sealed class PathFlattener(float tolerance)
{
    public readonly List<float> Points = new();
    public readonly List<(int Start, int Count, bool Closed)> Contours = new();
    int _start = -1;
    float _x, _y;

    public void MoveTo(float x, float y) { End(); _start = Points.Count / 2; Add(x, y); }
    public void LineTo(float x, float y) { if (_start < 0) MoveTo(_x, _y); Add(x, y); }
    public void Close() { End(closed: true); }

    void Add(float x, float y) { Points.Add(x); Points.Add(y); _x = x; _y = y; }

    public void End(bool closed = false)
    {
        if (_start < 0) return;
        int count = Points.Count / 2 - _start;
        if (count > 0) Contours.Add((_start, count, closed));
        if (count > 0) { _x = Points[2 * _start]; _y = Points[2 * _start + 1]; } // sau Z, điểm hiện tại là đầu đường
        _start = -1;
    }

    public void CubicTo(float x1, float y1, float x2, float y2, float x3, float y3)
    {
        float x0 = _x, y0 = _y;
        // Số đoạn theo độ lệch của các điểm điều khiển so với dây cung
        float dd = MathF.Max(Dist(x0 - 2 * x1 + x2, y0 - 2 * y1 + y2), Dist(x1 - 2 * x2 + x3, y1 - 2 * y2 + y3));
        int n = Math.Clamp((int)MathF.Ceiling(MathF.Sqrt(0.75f * dd / tolerance)), 1, 64);
        for (int i = 1; i <= n; i++)
        {
            float t = (float)i / n, m = 1 - t;
            LineTo(m * m * m * x0 + 3 * m * m * t * x1 + 3 * m * t * t * x2 + t * t * t * x3,
                   m * m * m * y0 + 3 * m * m * t * y1 + 3 * m * t * t * y2 + t * t * t * y3);
        }
    }

    public void QuadTo(float x1, float y1, float x2, float y2) =>
        CubicTo(_x + 2f / 3 * (x1 - _x), _y + 2f / 3 * (y1 - _y), x2 + 2f / 3 * (x1 - x2), y2 + 2f / 3 * (y1 - y2), x2, y2);

    /// <summary>Cung elip kiểu SVG (đổi sang tham số tâm theo phụ lục F.6 của đặc tả SVG).</summary>
    public void ArcTo(float rx, float ry, float rotationDeg, bool large, bool sweep, float x, float y)
    {
        float x0 = _x, y0 = _y;
        rx = MathF.Abs(rx); ry = MathF.Abs(ry);
        if (rx == 0 || ry == 0 || (x0 == x && y0 == y)) { LineTo(x, y); return; }
        float phi = rotationDeg * MathF.PI / 180, cos = MathF.Cos(phi), sin = MathF.Sin(phi);
        float dx = (x0 - x) / 2, dy = (y0 - y) / 2;
        float px = cos * dx + sin * dy, py = -sin * dx + cos * dy;
        float lambda = px * px / (rx * rx) + py * py / (ry * ry);
        if (lambda > 1) { float s = MathF.Sqrt(lambda); rx *= s; ry *= s; }
        float num = rx * rx * ry * ry - rx * rx * py * py - ry * ry * px * px;
        float den = rx * rx * py * py + ry * ry * px * px;
        float co = MathF.Sqrt(MathF.Max(0, num / den)) * (large == sweep ? -1 : 1);
        float cxp = co * rx * py / ry, cyp = -co * ry * px / rx;
        float cx = cos * cxp - sin * cyp + (x0 + x) / 2, cy = sin * cxp + cos * cyp + (y0 + y) / 2;
        float a0 = MathF.Atan2((py - cyp) / ry, (px - cxp) / rx);
        float a1 = MathF.Atan2((-py - cyp) / ry, (-px - cxp) / rx);
        float da = a1 - a0;
        if (sweep && da < 0) da += 2 * MathF.PI;
        else if (!sweep && da > 0) da -= 2 * MathF.PI;
        float r = MathF.Max(rx, ry);
        float step = 2 * MathF.Acos(MathF.Max(0, 1 - MathF.Min(tolerance / r, 1)));
        int n = Math.Clamp((int)MathF.Ceiling(MathF.Abs(da) / MathF.Max(step, 0.01f)), 2, 128);
        for (int i = 1; i < n; i++)
        {
            float a = a0 + da * i / n, ex = rx * MathF.Cos(a), ey = ry * MathF.Sin(a);
            LineTo(cx + cos * ex - sin * ey, cy + sin * ex + cos * ey);
        }
        LineTo(x, y);
    }

    static float Dist(float x, float y) => MathF.Sqrt(x * x + y * y);
}

/// <summary>Đọc thuộc tính <c>d</c> của SVG: M L H V C S Q T A Z, cả dạng tuyệt đối lẫn tương đối.</summary>
internal static class PathParser
{
    public static void Parse(string d, PathFlattener o)
    {
        int i = 0;
        char cmd = ' ';
        float x = 0, y = 0, sx = 0, sy = 0, lcx = 0, lcy = 0; // điểm hiện tại, đầu đường, điểm điều khiển trước
        char prev = ' ';
        while (true)
        {
            SkipSeparators(d, ref i);
            if (i >= d.Length) break;
            if (char.IsAsciiLetter(d[i])) cmd = d[i++];
            else if (cmd == ' ') throw new FormatException($"Path SVG không bắt đầu bằng lệnh: '{d}'");
            else if (cmd == 'M') cmd = 'L';
            else if (cmd == 'm') cmd = 'l';
            bool rel = char.IsAsciiLetterLower(cmd);
            float ox = rel ? x : 0, oy = rel ? y : 0;
            switch (char.ToUpperInvariant(cmd))
            {
                case 'M': x = ox + Num(d, ref i); y = oy + Num(d, ref i); sx = x; sy = y; o.MoveTo(x, y); break;
                case 'L': x = ox + Num(d, ref i); y = oy + Num(d, ref i); o.LineTo(x, y); break;
                case 'H': x = ox + Num(d, ref i); o.LineTo(x, y); break;
                case 'V': y = oy + Num(d, ref i); o.LineTo(x, y); break;
                case 'C':
                {
                    float x1 = ox + Num(d, ref i), y1 = oy + Num(d, ref i);
                    lcx = ox + Num(d, ref i); lcy = oy + Num(d, ref i);
                    x = ox + Num(d, ref i); y = oy + Num(d, ref i);
                    o.CubicTo(x1, y1, lcx, lcy, x, y);
                    break;
                }
                case 'S':
                {
                    bool smooth = prev is 'C' or 'S';
                    float x1 = smooth ? 2 * x - lcx : x, y1 = smooth ? 2 * y - lcy : y;
                    lcx = ox + Num(d, ref i); lcy = oy + Num(d, ref i);
                    x = ox + Num(d, ref i); y = oy + Num(d, ref i);
                    o.CubicTo(x1, y1, lcx, lcy, x, y);
                    break;
                }
                case 'Q':
                    lcx = ox + Num(d, ref i); lcy = oy + Num(d, ref i);
                    x = ox + Num(d, ref i); y = oy + Num(d, ref i);
                    o.QuadTo(lcx, lcy, x, y);
                    break;
                case 'T':
                {
                    bool smooth = prev is 'Q' or 'T';
                    lcx = smooth ? 2 * x - lcx : x; lcy = smooth ? 2 * y - lcy : y;
                    x = ox + Num(d, ref i); y = oy + Num(d, ref i);
                    o.QuadTo(lcx, lcy, x, y);
                    break;
                }
                case 'A':
                {
                    float rx = Num(d, ref i), ry = Num(d, ref i), rot = Num(d, ref i);
                    bool large = Flag(d, ref i), sweep = Flag(d, ref i);
                    x = ox + Num(d, ref i); y = oy + Num(d, ref i);
                    o.ArcTo(rx, ry, rot, large, sweep, x, y);
                    break;
                }
                case 'Z': o.Close(); x = sx; y = sy; break;
                default: throw new FormatException($"Lệnh path SVG không hỗ trợ: '{cmd}'");
            }
            prev = char.ToUpperInvariant(cmd);
        }
    }

    static void SkipSeparators(string d, ref int i)
    {
        while (i < d.Length && (char.IsWhiteSpace(d[i]) || d[i] == ',')) i++;
    }

    /// <summary>Cờ của lệnh cung: một ký tự 0/1, có thể viết dính liền số kế tiếp ("a2 2 0 012 2").</summary>
    static bool Flag(string d, ref int i)
    {
        SkipSeparators(d, ref i);
        if (i >= d.Length || d[i] is not ('0' or '1')) throw new FormatException($"Thiếu cờ cung trong path SVG: '{d}'");
        return d[i++] == '1';
    }

    /// <summary>Số kiểu SVG: dấu, phần thập phân, mũ; "1.5.5" là hai số 1.5 và .5.</summary>
    static float Num(string d, ref int i)
    {
        SkipSeparators(d, ref i);
        int start = i;
        if (i < d.Length && d[i] is '-' or '+') i++;
        bool dot = false;
        while (i < d.Length)
        {
            char c = d[i];
            if (char.IsAsciiDigit(c)) i++;
            else if (c == '.' && !dot) { dot = true; i++; }
            else if (c is 'e' or 'E' && i + 1 < d.Length && (char.IsAsciiDigit(d[i + 1]) || d[i + 1] is '-' or '+')) { i += 2; dot = true; }
            else break;
        }
        if (i == start) throw new FormatException($"Thiếu số trong path SVG tại vị trí {start}: '{d}'");
        return float.Parse(d.AsSpan(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
