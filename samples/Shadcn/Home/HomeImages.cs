using System.Runtime.CompilerServices;
using GotX;

/// <summary>
/// Ảnh chụp bảng demo mà trang gốc hiện dưới md thay cho các thẻ (page.tsx: <c>section -mx-4 w-[140vw] md:hidden</c>,
/// ảnh <c>/images/full-light.png</c> / <c>full-dark.png</c> của repo shadcn-ui, MIT). Ảnh gốc 2560×2764 được thu nhỏ sẵn
/// (bicubic) xuống 896 px – đúng bề rộng hiển thị ở khung 640 – vì renderer lấy mẫu song tuyến, không có mipmap (thu nhỏ
/// nhiều lần sẽ răng cưa); khung khác 640 thì ảnh được co giãn nhẹ.
/// </summary>
static class HomeImages
{
    static readonly ImageSource Light = Load("full-light-896.png");
    static readonly ImageSource Dark = Load("full-dark-896.png");

    // Ảnh nằm cạnh file nguồn này (samples/Shadcn/Home/Assets), hoặc cạnh file chạy nếu đã được chép ra khi build.
    static ImageSource Load(string file, [CallerFilePath] string source = "")
    {
        string local = Path.Combine(AppContext.BaseDirectory, "Home", "Assets", file);
        string path = File.Exists(local) ? local : Path.Combine(Path.GetDirectoryName(source) ?? ".", "Assets", file);
        return ImageSource.FromPngFile(path);
    }

    /// <summary>Khối ảnh chỉ hiện dưới md; rộng 140vw, phần tràn bên phải bị cắt.</summary>
    public static Div Mobile(Window w) =>
        w.Div().Class("overflow-hidden md:hidden").Key("home-mobile")
            .Child(w.Image(Light).Class("h-auto w-[140vw] aspect-[2560/2764] shrink-0 dark:hidden"))
            .Child(w.Image(Dark).Class("hidden h-auto w-[140vw] aspect-[2560/2764] shrink-0 dark:block"));
}