using System.Globalization;

/// <summary>
/// Icon ảnh #666 ở giữa placeholder.svg (path fill-rule evenodd, cắt theo ô 38.689). Chỉ dùng lúc dựng ảnh một lần:
/// đọc path SVG (M/L/H/V/A/Z, tương đối và tuyệt đối) thành các đa giác, cung tròn chia thành đoạn thẳng.
/// </summary>
static class PlaceholderIcon
{
    const string D =
        "M616.426 586.58h-31.434v16.176l3.553-3.554.531-.531h9.068l.074-.074 8.463-8.463h2.565l7.18 7.181V586.58Z" +
        "m-15.715 14.654 3.698 3.699 1.283 1.282-2.565 2.565-1.282-1.283-5.2-5.199h-6.066l-5.514 5.514-.073.073v2.876" +
        "a2.418 2.418 0 0 0 2.418 2.418h26.598a2.418 2.418 0 0 0 2.418-2.418v-8.317l-8.463-8.463-7.181 7.181-.071.072Z" +
        "m-19.347 5.442v4.085a6.045 6.045 0 0 0 6.046 6.045h26.598a6.044 6.044 0 0 0 6.045-6.045v-7.108l1.356-1.355" +
        "-1.282-1.283-.074-.073v-17.989h-38.689v23.43l-.146.146.146.147Z";

    // clip-path của icon.
    const float ClipX = 581.364f, ClipY = 580.535f, ClipSize = 38.689f;

    public static List<List<(float X, float Y)>> Polygons()
    {
        var polys = new List<List<(float X, float Y)>>();
        List<(float X, float Y)>? cur = null;
        float x = 0, y = 0, sx = 0, sy = 0;
        char cmd = 'M';
        int i = 0;
        while (i < D.Length)
        {
            if (char.IsLetter(D[i])) cmd = D[i++];
            if (cmd is 'Z' or 'z') { x = sx; y = sy; cur = null; continue; }
            bool rel = char.IsLower(cmd);
            switch (char.ToUpperInvariant(cmd))
            {
                case 'M':
                    x = (rel ? x : 0) + Num(ref i); y = (rel ? y : 0) + Num(ref i);
                    sx = x; sy = y; polys.Add(cur = [(x, y)]);
                    cmd = rel ? 'l' : 'L'; // cặp số tiếp theo sau M là lệnh L
                    break;
                case 'L': x = (rel ? x : 0) + Num(ref i); y = (rel ? y : 0) + Num(ref i); cur!.Add((x, y)); break;
                case 'H': x = (rel ? x : 0) + Num(ref i); cur!.Add((x, y)); break;
                case 'V': y = (rel ? y : 0) + Num(ref i); cur!.Add((x, y)); break;
                case 'A':
                    float r = Num(ref i); Num(ref i); Num(ref i); Num(ref i); bool sweep = Num(ref i) != 0;
                    float ex = (rel ? x : 0) + Num(ref i), ey = (rel ? y : 0) + Num(ref i);
                    Arc(cur!, x, y, ex, ey, r, sweep);
                    x = ex; y = ey;
                    break;
            }
            SkipSeparators(ref i);
        }
        return polys;
    }

    /// <summary>Cung phần tư (mọi cung của icon đều là góc bo 90°): tâm là một trong hai góc của hình vuông P0–P1.</summary>
    static void Arc(List<(float X, float Y)> poly, float x0, float y0, float x1, float y1, float r, bool sweep)
    {
        // y hướng xuống: sweep=1 là chiều kim đồng hồ trên màn hình ⇔ tích chéo (P0−C)×(P1−C) dương.
        float cx = x0, cy = y1;
        if ((((x0 - cx) * (y1 - cy) - (y0 - cy) * (x1 - cx)) > 0) != sweep) { cx = x1; cy = y0; }
        float a0 = MathF.Atan2(y0 - cy, x0 - cx), a1 = MathF.Atan2(y1 - cy, x1 - cx);
        float da = a1 - a0;
        if (sweep && da < 0) da += 2 * MathF.PI;
        if (!sweep && da > 0) da -= 2 * MathF.PI;
        for (int k = 1; k <= 8; k++)
        {
            float a = a0 + da * k / 8;
            poly.Add((cx + r * MathF.Cos(a), cy + r * MathF.Sin(a)));
        }
    }

    static float Num(ref int i)
    {
        SkipSeparators(ref i);
        int start = i;
        if (D[i] is '-' or '+') i++;
        bool dot = false;
        while (i < D.Length && (char.IsDigit(D[i]) || (D[i] == '.' && !dot)))
        {
            if (D[i] == '.') dot = true;
            i++;
        }
        return float.Parse(D.AsSpan(start, i - start), CultureInfo.InvariantCulture);
    }

    static void SkipSeparators(ref int i)
    {
        while (i < D.Length && (D[i] == ' ' || D[i] == ',')) i++;
    }

    /// <summary>Độ phủ của icon trong pixel tâm (u, v) cạnh <paramref name="pixel"/> (đơn vị SVG): 4×4 mẫu, luật evenodd.</summary>
    public static float Cover(List<List<(float X, float Y)>> polys, float u, float v, float pixel)
    {
        float half = pixel / 2;
        if (u + half < ClipX || u - half > ClipX + ClipSize || v + half < ClipY || v - half > ClipY + ClipSize) return 0;
        int hits = 0;
        for (int sy = 0; sy < 4; sy++)
            for (int sx = 0; sx < 4; sx++)
            {
                float px = u - half + (sx + 0.5f) * pixel / 4, py = v - half + (sy + 0.5f) * pixel / 4;
                if (px < ClipX || px > ClipX + ClipSize || py < ClipY || py > ClipY + ClipSize) continue;
                bool inside = false;
                foreach (var poly in polys)
                    for (int a = 0, b = poly.Count - 1; a < poly.Count; b = a++)
                        if ((poly[a].Y > py) != (poly[b].Y > py) &&
                            px < (poly[b].X - poly[a].X) * (py - poly[a].Y) / (poly[b].Y - poly[a].Y) + poly[a].X)
                            inside = !inside;
                if (inside) hits++;
            }
        return hits / 16f;
    }
}
