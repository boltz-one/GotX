namespace GotX.Raster;

public static partial class SoftwareRenderer
{
    /// <summary>
    /// Vẽ lại chỉ các ô hỏng vào canvas đã giữ từ frame trước (prototype, xem <see cref="SceneDamage"/>): mỗi dải ô liền nhau trên một hàng
    /// được xoá về màu nền rồi vẽ lại mọi primitive chạm vào dải đó, cắt theo dải. Kết quả trùng từng pixel với <see cref="Render"/>.
    /// Trả số pixel đã vẽ lại. Full → vẽ cả frame.
    /// </summary>
    [ThreadStatic] static DamageScratch? t_scratch;

    sealed class DamageScratch
    {
        public Bounds[] Rects = new Bounds[64];
        public int[] Ty = new int[64];       // hàng ô đầu của hình chữ nhật
        public int[] RowHead = new int[32];  // theo hàng ô: chỉ số đầu danh sách hình chữ nhật chạm hàng đó
        public int[] Next = new int[256];    // danh sách liên kết (hình chữ nhật, hàng) → mục kế
        public int[] RectOf = new int[256];
        public int[] OpenPrev = new int[64], OpenCur = new int[64];
    }

    /// <summary>
    /// Vẽ lại chỉ các ô hỏng vào canvas đã giữ từ frame trước (xem <see cref="SceneDamage"/>): dải ô liền nhau trên một hàng, các dải giống nhau
    /// ở hàng kế được gộp thành một hình chữ nhật; mọi hình chữ nhật được xoá về màu nền, rồi MỘT lượt qua primitive vẽ mỗi primitive vào từng hình
    /// chữ nhật nó chạm (cắt theo hình chữ nhật; chúng rời nhau nên thứ tự vẽ trong từng hình giữ nguyên). Trùng từng pixel với <see cref="Render"/>.
    /// Trả số pixel đã vẽ lại. Full → vẽ cả frame.
    /// </summary>
    public static long RenderDamaged(Scene scene, Canvas canvas, in SceneDamage.Result damage)
    {
        if (damage.Empty) return 0;
        if (damage.Full || damage.Dirty is null)
        {
            Render(scene, canvas);
            return (long)canvas.Width * canvas.Height;
        }
        if (damage.HasShift) ApplyShift(canvas, damage);
        var s = t_scratch ??= new DamageScratch();
        int count = BuildRects(s, damage, canvas.Width, canvas.Height, out long pixels);
        for (int r = 0; r < count; r++)
            FillRegion(canvas, scene.ClearColor, (int)s.Rects[r].X, (int)s.Rects[r].Y, (int)s.Rects[r].Right, (int)s.Rects[r].Bottom);
        int rows = damage.TilesY, tile = SceneDamage.Tile;
        var prims = scene.Primitives;
        var boxes = damage.Boxes is { } bx && bx.Length >= prims.Length ? bx : null;
        for (int i = 0; i < prims.Length; i++)
        {
            ref readonly var p = ref prims[i];
            var box = boxes is not null ? boxes[i] : SceneDamageMath.Visual(scene, p);
            if (box.IsEmpty) continue;
            int r0 = Math.Max(0, (int)MathF.Floor(box.Y / tile)), r1 = Math.Min(rows - 1, (int)MathF.Floor((box.Bottom - 0.001f) / tile));
            for (int row = r0; row <= r1; row++)
                for (int e = s.RowHead[row]; e >= 0; e = s.Next[e])
                {
                    int r = s.RectOf[e];
                    if (row != Math.Max(r0, s.Ty[r])) continue; // hình chữ nhật trải nhiều hàng chỉ xử lý ở hàng đầu mà primitive chạm
                    var region = s.Rects[r];
                    var clip = p.Clip.Intersect(region);
                    if (box.Intersect(region).IsEmpty) continue;
                    var q = p;
                    q.Clip = clip;
                    DrawOne(canvas, scene, q);
                }
        }
        return pixels;
    }

    /// <summary>Cuộn bằng blit: chép các hàng của vùng cuộn lên/xuống <c>ShiftDy</c> hàng trong canvas giữ lại (hàng đích sau hàng nguồn khi đi xuống, và ngược lại, nên không đè nhầm).</summary>
    static void ApplyShift(Canvas c, in SceneDamage.Result d)
    {
        int x0 = Math.Max(0, (int)d.ShiftRect.X), x1 = Math.Min(c.Width, (int)(d.ShiftRect.X + d.ShiftRect.Width));
        int y0 = Math.Max(0, (int)d.ShiftRect.Y), y1 = Math.Min(c.Height, (int)(d.ShiftRect.Y + d.ShiftRect.Height)), dy = d.ShiftDy, w = x1 - x0, W = c.Width;
        if (w <= 0 || Math.Abs(dy) >= y1 - y0) return;
        var px = c.Pixels;
        if (w == W) // cả bề ngang: một lần memmove (Span.CopyTo an toàn khi chồng lấn)
        {
            if (dy > 0) px.AsSpan(y0 * W, (y1 - dy - y0) * W).CopyTo(px.AsSpan((y0 + dy) * W));
            else px.AsSpan((y0 - dy) * W, (y1 + dy - y0) * W).CopyTo(px.AsSpan(y0 * W));
            return;
        }
        if (dy > 0) for (int y = y1 - 1 - dy; y >= y0; y--) px.AsSpan(y * W + x0, w).CopyTo(px.AsSpan((y + dy) * W + x0));
        else for (int y = y0 - dy; y < y1; y++) px.AsSpan(y * W + x0, w).CopyTo(px.AsSpan((y + dy) * W + x0));
    }
    /// <summary>Gộp lưới ô hỏng thành hình chữ nhật (dải ngang rồi nối dọc nếu cùng cột đầu/cuối); ghi danh sách theo hàng vào <paramref name="s"/>.</summary>
    static int BuildRects(DamageScratch s, in SceneDamage.Result d, int width, int height, out long pixels)
    {
        int tile = SceneDamage.Tile, tx = d.TilesX, ty = d.TilesY, n = 0, links = 0, open = 0, prevOpen = 0;
        pixels = 0;
        if (s.RowHead.Length < ty) s.RowHead = new int[ty * 2];
        Array.Fill(s.RowHead, -1, 0, ty);
        for (int y = 0; y < ty; y++)
        {
            float y0 = y * tile, y1 = Math.Min(height, (y + 1) * tile);
            (s.OpenPrev, s.OpenCur) = (s.OpenCur, s.OpenPrev);
            prevOpen = open; open = 0;
            for (int x = 0; x < tx; x++)
            {
                if (!d.Dirty![y * tx + x]) continue;
                int run = x;
                while (run + 1 < tx && d.Dirty[y * tx + run + 1]) run++;
                float x0 = x * tile, x1 = Math.Min(width, (run + 1) * tile);
                int rect = -1;
                for (int k = 0; k < prevOpen; k++)
                {
                    ref var c = ref s.Rects[s.OpenPrev[k]];
                    if (c.X == x0 && c.Right == x1 && c.Bottom == y0) { rect = s.OpenPrev[k]; break; }
                }
                if (rect >= 0) s.Rects[rect] = new Bounds(x0, s.Rects[rect].Y, x1 - x0, y1 - s.Rects[rect].Y); // nối dọc
                else
                {
                    if (n == s.Rects.Length) { Array.Resize(ref s.Rects, n * 2); Array.Resize(ref s.Ty, n * 2); }
                    rect = n++;
                    s.Rects[rect] = new Bounds(x0, y0, x1 - x0, y1 - y0);
                    s.Ty[rect] = y;
                }
                if (open == s.OpenCur.Length) Array.Resize(ref s.OpenCur, open * 2);
                s.OpenCur[open++] = rect;
                Link(s, y, rect, ref links);
                pixels += (long)((x1 - x0) * (y1 - y0));
                x = run;
            }
        }
        return n;
    }

    static void Link(DamageScratch s, int row, int rect, ref int links)
    {
        if (links == s.Next.Length) { Array.Resize(ref s.Next, links * 2); Array.Resize(ref s.RectOf, links * 2); }
        s.RectOf[links] = rect; s.Next[links] = s.RowHead[row]; s.RowHead[row] = links++;
    }

    static void FillRegion(Canvas c, Rgba clear, int x0, int y0, int x1, int y1)
    {
        uint v = Canvas.Pack(clear.R * clear.A, clear.G * clear.A, clear.B * clear.A, clear.A);
        for (int y = y0; y < y1; y++) c.Pixels.AsSpan(y * c.Width + x0, x1 - x0).Fill(v);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    static void DrawOne(Canvas canvas, Scene scene, in Primitive p)
    {
        var clip = Shape.Of(scene, p.ClipShape);
        switch (p.Kind)
        {
            case PrimitiveKind.Quad: Quad(canvas, scene, p, clip); break;
            case PrimitiveKind.Shadow: Shadow(canvas, p, clip); break;
            case PrimitiveKind.Glyph: Glyph(canvas, scene, p, clip); break;
            case PrimitiveKind.Image: Image(canvas, p, clip); break;
        }
    }
}
