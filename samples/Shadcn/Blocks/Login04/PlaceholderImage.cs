using GotX;

/// <summary>
/// Ảnh giữ chỗ cho cột phải của login-04 / signup-04: vẽ lại <c>/placeholder.svg</c> của ui.shadcn.com (1200×1200: nền
/// #EAEAEA, nhóm opacity .5 gồm vòng tròn lớn mờ, bốn đường kẻ mờ dần hai đầu, vòng tròn trắng và icon ảnh #666).
/// Sinh bằng code ở nửa độ phân giải (600×600 – cột ảnh hiện nó ở tỉ lệ ≈ 0.45–0.5) vì Core chỉ giải mã PNG.
/// </summary>
static class PlaceholderImage
{
    const int Size = 600;
    const float Scale = 0.5f; // đơn vị SVG → pixel ảnh

    // Hình học của placeholder.svg (đơn vị SVG). Khai báo trước Light/Dark: khởi tạo tĩnh chạy theo thứ tự trong file.
    const float Cx = 600.709f, Cy = 599.88f, BigR = 136.62f, SmallR = 56.825f, Stroke = 2.418f;
    static readonly (float X0, float Y0, float X1, float Y1)[] Lines =
    [
        (405.98f, 796.59f, 797.43f, 405.16f), (404.846f, 598.671f, 796.572f, 598.671f),
        (599.5f, 795.742f, 599.5f, 404.017f), (795.717f, 796.597f, 404.276f, 405.157f),
    ];
    public static readonly ImageSource Light = Build(1f);

    /// <summary>Bản cho dark mode: <c>dark:brightness-[0.2] dark:grayscale</c> (ảnh vốn xám nên chỉ còn nhân 0.2).</summary>
    public static readonly ImageSource Dark = Build(0.2f);

    /// <summary>
    /// Cột ảnh của card hai cột (<c>relative hidden bg-muted md:block</c> + ảnh <c>absolute inset-0 object-cover</c>).
    /// Core chưa có filter: dùng ảnh đã làm tối sẵn cho dark mode.
    /// </summary>
    public static Div Panel(Window w) => w.Div().Key("auth-image").Class("relative hidden bg-muted md:block")
        .Child(w.Image(w.DarkMode ? Dark : Light).Class("absolute inset-0 size-full object-cover"));


    static ImageSource Build(float brightness)
    {
        var icon = PlaceholderIcon.Polygons();
        var pixels = new byte[Size * Size * 4];
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float u = (x + 0.5f) / Scale, v = (y + 0.5f) / Scale;
                // Lớp của nhóm opacity .5, tô theo thứ tự vẽ của SVG (màu premultiplied: c = màu·α).
                float a = 0, c = 0;
                float r = MathF.Sqrt((u - Cx) * (u - Cx) + (v - Cy) * (v - Cy));
                Over(ref c, ref a, 0xFA, 0.5f * Cover(BigR - r));                      // nhóm con opacity .5: nền
                Over(ref c, ref a, 0xC9, 0.5f * Cover(Stroke / 2 - MathF.Abs(r - BigR))); //                 viền
                foreach (var l in Lines) Over(ref c, ref a, 0xC9, LineCover(u, v, l));
                Over(ref c, ref a, 0xFF, Cover(SmallR - r));
                Over(ref c, ref a, 0x66, PlaceholderIcon.Cover(icon, u, v, 1 / Scale));
                Over(ref c, ref a, 0xC9, Cover(Stroke / 2 - MathF.Abs(r - SmallR)));
                // Nền #EAEAEA rồi cả nhóm với opacity .5.
                float grey = 0xEA * (1 - 0.5f * a) + 0.5f * c;
                byte g = (byte)MathF.Round(grey * brightness);
                int i = (y * Size + x) * 4;
                pixels[i] = pixels[i + 1] = pixels[i + 2] = g;
                pixels[i + 3] = 255;
            }
        return ImageSource.FromRgba(Size, Size, pixels);
    }

    /// <summary>Độ phủ khử răng cưa từ khoảng cách có dấu (đơn vị SVG, dương = bên trong); 1 pixel ảnh = 2 đơn vị.</summary>
    static float Cover(float inside) => Math.Clamp(inside * Scale + 0.5f, 0f, 1f);

    static void Over(ref float c, ref float a, float color, float alpha)
    {
        c = color * alpha + c * (1 - alpha);
        a = alpha + a * (1 - alpha);
    }

    /// <summary>Nét thẳng có gradient trong suốt → C9C9C9 (0–20.8 %) → C9C9C9 (79.2 %) → trong suốt dọc theo nét.</summary>
    static float LineCover(float u, float v, (float X0, float Y0, float X1, float Y1) l)
    {
        float dx = l.X1 - l.X0, dy = l.Y1 - l.Y0, len2 = dx * dx + dy * dy;
        float t = ((u - l.X0) * dx + (v - l.Y0) * dy) / len2;
        if (t < 0 || t > 1) return 0;
        float px = l.X0 + t * dx - u, py = l.Y0 + t * dy - v;
        float cover = Cover(Stroke / 2 - MathF.Sqrt(px * px + py * py));
        float ramp = t < 0.208f ? t / 0.208f : t > 0.792f ? (1 - t) / 0.208f : 1;
        return cover * ramp;
    }
}
