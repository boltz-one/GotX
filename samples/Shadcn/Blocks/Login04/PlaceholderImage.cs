using GotX;

/// <summary>
/// Ảnh giữ chỗ cho cột phải của login-04 / signup-04, vẽ lại theo <c>/placeholder.svg</c> của shadcn (nền xám nhạt, nét mảnh: hai
/// đường chéo, chữ thập và hai vòng tròn). Sinh bằng code vì ví dụ không kèm file ảnh và Core chỉ giải mã PNG.
/// </summary>
static class PlaceholderImage
{
    const int Size = 480;

    public static readonly ImageSource Light = Build(1f);

    /// <summary>Bản cho dark mode: xám và tối bằng 20% – tương đương <c>dark:brightness-[0.2] dark:grayscale</c>.</summary>
    public static readonly ImageSource Dark = Build(0.2f);

    /// <summary>
    /// Cột ảnh của card hai cột (<c>relative hidden bg-muted md:block</c> + ảnh <c>absolute inset-0 object-cover</c>).
    /// Bản gốc làm tối ảnh bằng dark:brightness-[0.2] dark:grayscale (Core chưa có filter): dùng ảnh đã làm tối sẵn.
    /// </summary>
    public static Div Panel(Window w) => w.Div().Key("auth-image").Class("relative hidden bg-muted md:block")
        .Child(w.Image(w.DarkMode ? Dark : Light).Class("absolute inset-0 size-full object-cover"));

    static ImageSource Build(float brightness)
    {
        const float background = 0xEA, stroke = 0xC4, half = Size / 2f;
        var pixels = new byte[Size * Size * 4];
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float u = x + 0.5f - half, v = y + 0.5f - half, r = MathF.Sqrt(u * u + v * v);
                // Khoảng cách tới nét gần nhất; độ phủ giảm tuyến tính quanh mép nét để nét không răng cưa.
                float d = MathF.Min(MathF.Min(MathF.Abs(u - v), MathF.Abs(u + v)) * 0.7071f, MathF.Min(MathF.Abs(u), MathF.Abs(v)));
                d = MathF.Min(d, MathF.Min(MathF.Abs(r - Size * 0.3f), MathF.Abs(r - Size * 0.15f)));
                float cover = Math.Clamp(1.25f - d, 0f, 1f);
                byte grey = (byte)MathF.Round((background + (stroke - background) * cover) * brightness);
                int i = (y * Size + x) * 4;
                pixels[i] = pixels[i + 1] = pixels[i + 2] = grey;
                pixels[i + 3] = 255;
            }
        return ImageSource.FromRgba(Size, Size, pixels);
    }
}
