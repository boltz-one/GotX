using GotX;
using GotX.Text;

/// <summary>Trạng thái dùng chung của các phần kiểm tra headless: bộ đếm đạt/lỗi, đồng hồ ảo, font, nơi ghi PNG.</summary>
sealed class Checker(string outDir, float scale)
{
    /// <summary>Ngữ cảnh variant mặc định của các kiểm tra class: sáng, viewport 1000×700, rem 16.</summary>
    public static readonly VariantContext Wide = new(false, 1000, 700, 16);

    public readonly string OutDir = outDir;
    public readonly float Scale = scale;

    /// <summary>Có sau <see cref="LoadFonts"/>.</summary>
    public TextSystem Text = null!;

    /// <summary>Đồng hồ ảo (giây) mà mọi cửa sổ thử dùng làm <c>Clock</c>.</summary>
    public double Now;

    public int Pass, Fail;

    public void Check(string name, bool ok) { if (ok) Pass++; else { Fail++; Console.WriteLine($"  ✗ {name}"); } }

    public static bool Near(float a, float b) => MathF.Abs(a - b) < 0.01f;

    public static Style R(string c, VariantContext? ctx = null, StateVariant st = StateVariant.None) => Tailwind.Resolve(c, ctx ?? Wide, st);

    /// <summary>Nạp font hệ thống (Segoe UI, hoặc DejaVu trên Linux); false nếu không tìm thấy font nào.</summary>
    public bool LoadFonts()
    {
        var text = Text = new TextSystem();
        string fonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        string[][] candidates =
        [
            [Path.Combine(fonts, "segoeui.ttf"), Path.Combine(fonts, "seguisb.ttf"), Path.Combine(fonts, "segoeuib.ttf")],
            ["/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", "", "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"],
        ];
        foreach (var set in candidates)
        {
            text.TryAddFont(set[0], FontWeight.Normal);
            if (set[1] != "") text.TryAddFont(set[1], FontWeight.Semibold);
            if (set[2] != "") text.TryAddFont(set[2], FontWeight.Bold);
            if (text.HasFonts) break;
        }
        return text.HasFonts;
    }
}