using GotX.Text;

namespace GotX.Test;

/// <summary>
/// Hệ thống chữ dùng chung cho mọi test: nạp font một lần (đọc file TTF là phần chậm nhất khi mở cửa sổ headless).
/// Windows: Segoe UI thường/đậm vừa/đậm/nghiêng + Consolas làm Mono. Hệ khác: DejaVu.
/// </summary>
public static class TestFonts
{
    static TextSystem? s_shared;

    public static TextSystem Shared => s_shared ??= Load();

    static TextSystem Load()
    {
        var text = new TextSystem();
        string fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        const string dejavu = "/usr/share/fonts/truetype/dejavu/";
        if (text.TryAddFont(Path.Combine(fonts, "segoeui.ttf"), FontWeight.Normal))
        {
            text.TryAddFont(Path.Combine(fonts, "seguisb.ttf"), FontWeight.Semibold);
            text.TryAddFont(Path.Combine(fonts, "segoeuib.ttf"), FontWeight.Bold);
            text.TryAddFont(Path.Combine(fonts, "segoeuii.ttf"), FontWeight.Normal, FontFamily.Sans, italic: true);
            text.TryAddFont(Path.Combine(fonts, "consola.ttf"), FontWeight.Normal, FontFamily.Mono);
            text.TryAddFont(Path.Combine(fonts, "consolab.ttf"), FontWeight.Bold, FontFamily.Mono);
        }
        else if (text.TryAddFont(dejavu + "DejaVuSans.ttf", FontWeight.Normal))
        {
            text.TryAddFont(dejavu + "DejaVuSans-Bold.ttf", FontWeight.Bold);
            text.TryAddFont(dejavu + "DejaVuSans-Oblique.ttf", FontWeight.Normal, FontFamily.Sans, italic: true);
            text.TryAddFont(dejavu + "DejaVuSansMono.ttf", FontWeight.Normal, FontFamily.Mono);
            text.TryAddFont(dejavu + "DejaVuSansMono-Bold.ttf", FontWeight.Bold, FontFamily.Mono);
        }
        if (!text.HasFonts) throw new InvalidOperationException("GotX.Test: không tìm thấy font TrueType (Segoe UI hoặc DejaVu).");
        return text;
    }
}
