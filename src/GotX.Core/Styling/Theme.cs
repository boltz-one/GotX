namespace GotX;

/// <summary>
/// Bộ token thiết kế, tương ứng khối <c>@theme</c> của Tailwind v4 (--spacing, --color-*, --radius-*,
/// --shadow-*, --text-*, --breakpoint-*, --container-*…). Utility chỉ là "tên + giá trị tra trong theme".
/// Có thể sửa hoặc thêm token lúc khởi động: <c>Theme.Colors["brand-500"] = Rgba.FromHex(0x0ea5e9)</c>.
/// Bảng màu = Tailwind v4 (packages/tailwindcss/theme.css, nhánh main, lấy 2026-11-15): OKLCH đổi sang sRGB 8 bit bằng ma trận OKLab chuẩn,
/// kẹp vào gam sRGB (hue `none` = 0). Ví dụ red-600 = #e7000b, indigo-600 = #4f39f6. Token ngữ nghĩa của ComponentTheme là giá trị đặt tay, không đổi theo bảng này.
/// </summary>
public static class Theme
{
    /// <summary>--spacing: mỗi bậc = 0.25rem, nên <c>p-4</c> = 1rem, <c>p-17</c> = 4.25rem.</summary>
    public static float SpacingRem { get; set; } = 0.25f;

    public static readonly string[] ColorFamilies =
    [
        "slate", "gray", "zinc", "neutral", "stone", "red", "orange", "amber", "yellow", "lime", "green",
        "emerald", "teal", "cyan", "sky", "blue", "indigo", "violet", "purple", "fuchsia", "pink", "rose",
    ];

    public static readonly int[] Shades = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 950];

    static readonly uint[][] PaletteHex =
    [
        [0xf8fafc, 0xf1f5f9, 0xe2e8f0, 0xcad5e2, 0x90a1b9, 0x62748e, 0x45556c, 0x314158, 0x1d293d, 0x0f172b, 0x020618],
        [0xf9fafb, 0xf3f4f6, 0xe5e7eb, 0xd1d5dc, 0x99a1af, 0x6a7282, 0x4a5565, 0x364153, 0x1e2939, 0x101828, 0x030712],
        [0xfafafa, 0xf4f4f5, 0xe4e4e7, 0xd4d4d8, 0x9f9fa9, 0x71717b, 0x52525c, 0x3f3f46, 0x27272a, 0x18181b, 0x09090b],
        [0xfafafa, 0xf5f5f5, 0xe5e5e5, 0xd4d4d4, 0xa1a1a1, 0x737373, 0x525252, 0x404040, 0x262626, 0x171717, 0x0a0a0a],
        [0xfafaf9, 0xf5f5f4, 0xe7e5e4, 0xd6d3d1, 0xa6a09b, 0x79716b, 0x57534d, 0x44403b, 0x292524, 0x1c1917, 0x0c0a09],
        [0xfef2f2, 0xffe2e2, 0xffc9c9, 0xffa2a2, 0xff6467, 0xfb2c36, 0xe7000b, 0xc10007, 0x9f0712, 0x82181a, 0x460809],
        [0xfff7ed, 0xffedd4, 0xffd6a7, 0xffb86a, 0xff8904, 0xff6900, 0xf54900, 0xca3500, 0x9f2d00, 0x7e2a0c, 0x441306],
        [0xfffbeb, 0xfef3c6, 0xfee685, 0xffd230, 0xffb900, 0xfe9a00, 0xe17100, 0xbb4d00, 0x973c00, 0x7b3306, 0x461901],
        [0xfefce8, 0xfef9c2, 0xfff085, 0xffdf20, 0xfdc700, 0xf0b100, 0xd08700, 0xa65f00, 0x894b00, 0x733e0a, 0x432004],
        [0xf7fee7, 0xecfcca, 0xd8f999, 0xbbf451, 0x9ae600, 0x7ccf00, 0x5ea500, 0x497d00, 0x3c6300, 0x35530e, 0x192e03],
        [0xf0fdf4, 0xdcfce7, 0xb9f8cf, 0x7bf1a8, 0x05df72, 0x00c950, 0x00a63e, 0x008236, 0x016630, 0x0d542b, 0x032e15],
        [0xecfdf5, 0xd0fae5, 0xa4f4cf, 0x5ee9b5, 0x00d492, 0x00bc7d, 0x009966, 0x007a55, 0x006045, 0x004f3b, 0x002c22],
        [0xf0fdfa, 0xcbfbf1, 0x96f7e4, 0x46ecd5, 0x00d5be, 0x00bba7, 0x009689, 0x00786f, 0x005f5a, 0x0b4f4a, 0x022f2e],
        [0xecfeff, 0xcefafe, 0xa2f4fd, 0x53eafd, 0x00d3f2, 0x00b8db, 0x0092b8, 0x007595, 0x005f78, 0x104e64, 0x053345],
        [0xf0f9ff, 0xdff2fe, 0xb8e6fe, 0x74d4ff, 0x00bcff, 0x00a6f4, 0x0084d1, 0x0069a8, 0x00598a, 0x024a70, 0x052f4a],
        [0xeff6ff, 0xdbeafe, 0xbedbff, 0x8ec5ff, 0x51a2ff, 0x2b7fff, 0x155dfc, 0x1447e6, 0x193cb8, 0x1c398e, 0x162456],
        [0xeef2ff, 0xe0e7ff, 0xc6d2ff, 0xa3b3ff, 0x7c86ff, 0x615fff, 0x4f39f6, 0x432dd7, 0x372aac, 0x312c85, 0x1e1a4d],
        [0xf5f3ff, 0xede9fe, 0xddd6ff, 0xc4b4ff, 0xa684ff, 0x8e51ff, 0x7f22fe, 0x7008e7, 0x5d0ec0, 0x4d179a, 0x2f0d68],
        [0xfaf5ff, 0xf3e8ff, 0xe9d4ff, 0xdab2ff, 0xc27aff, 0xad46ff, 0x9810fa, 0x8200db, 0x6e11b0, 0x59168b, 0x3c0366],
        [0xfdf4ff, 0xfae8ff, 0xf6cfff, 0xf4a8ff, 0xed6aff, 0xe12afb, 0xc800de, 0xa800b7, 0x8a0194, 0x721378, 0x4b004f],
        [0xfdf2f8, 0xfce7f3, 0xfccee8, 0xfda5d5, 0xfb64b6, 0xf6339a, 0xe60076, 0xc6005c, 0xa3004c, 0x861043, 0x510424],
        [0xfff1f2, 0xffe4e6, 0xffccd3, 0xffa1ad, 0xff637e, 0xff2056, 0xec003f, 0xc70036, 0xa50036, 0x8b0836, 0x4d0218],
    ];

    /// <summary>--color-*: "blue-500" → màu. Thêm màu thương hiệu tại đây.</summary>
    public static readonly Dictionary<string, Rgba> Colors = BuildColors();

    static Dictionary<string, Rgba> BuildColors()
    {
        var d = new Dictionary<string, Rgba>(StringComparer.Ordinal)
        {
            ["white"] = Rgba.White, ["black"] = Rgba.Black, ["transparent"] = Rgba.Transparent,
        };
        for (int f = 0; f < ColorFamilies.Length; f++)
            for (int s = 0; s < Shades.Length; s++)
                d[$"{ColorFamilies[f]}-{Shades[s]}"] = Rgba.FromHex(PaletteHex[f][s]);
        return d;
    }

    public static Rgba Color(string name) => Colors[name];

    /// <summary>
    /// Giá trị ở chế độ tối của các token ngữ nghĩa (giống biến CSS đổi theo <c>.dark</c>): <c>bg-primary</c> tự đổi màu
    /// khi Window.DarkMode bật, không cần viết <c>dark:</c>. Token không có ở đây dùng chung một màu cho cả hai chế độ.
    /// </summary>
    public static readonly Dictionary<string, Rgba> DarkColors = new(StringComparer.Ordinal);

    /// <summary>Khai báo một token màu ngữ nghĩa với giá trị sáng và tối. Gọi lúc khởi động, trước khi dùng class.</summary>
    public static void SetColor(string name, Rgba light, Rgba dark)
    {
        Colors[name] = light;
        DarkColors[name] = dark;
    }

    /// <summary>--radius-* (px).</summary>
    public static readonly Dictionary<string, float> Radius = new(StringComparer.Ordinal)
    {
        ["none"] = 0, ["xs"] = 2, ["sm"] = 4, [""] = 4, ["md"] = 6, ["lg"] = 8, ["xl"] = 12,
        ["2xl"] = 16, ["3xl"] = 24, ["4xl"] = 32, ["full"] = 9999,
    };

    /// <summary>--shadow-* : mỗi bậc gồm 1–2 lớp (giá trị của Tailwind v4).</summary>
    public static BoxShadow[] ShadowLayers(ShadowPreset p) => p switch
    {
        ShadowPreset.TwoXs => [new(Rgba.Black.WithAlpha(0.05f), 0, 1, 0, 0)],
        ShadowPreset.Xs => [new(Rgba.Black.WithAlpha(0.05f), 0, 1, 2, 0)],
        ShadowPreset.Sm => [new(Rgba.Black.WithAlpha(0.1f), 0, 1, 3, 0), new(Rgba.Black.WithAlpha(0.1f), 0, 1, 2, -1)],
        ShadowPreset.Md => [new(Rgba.Black.WithAlpha(0.1f), 0, 4, 6, -1), new(Rgba.Black.WithAlpha(0.1f), 0, 2, 4, -2)],
        ShadowPreset.Lg => [new(Rgba.Black.WithAlpha(0.1f), 0, 10, 15, -3), new(Rgba.Black.WithAlpha(0.1f), 0, 4, 6, -4)],
        ShadowPreset.Xl => [new(Rgba.Black.WithAlpha(0.1f), 0, 20, 25, -5), new(Rgba.Black.WithAlpha(0.1f), 0, 8, 10, -6)],
        ShadowPreset.TwoXl => [new(Rgba.Black.WithAlpha(0.25f), 0, 25, 50, -12)],
        _ => [],
    };

    internal static readonly BoxShadow[][] ShadowTable =
        Enumerable.Range(0, 9).Select(i => ShadowLayers((ShadowPreset)i)).ToArray();

    public static readonly Dictionary<string, ShadowPreset> ShadowNames = new(StringComparer.Ordinal)
    {
        ["none"] = ShadowPreset.None, ["2xs"] = ShadowPreset.TwoXs, ["xs"] = ShadowPreset.Xs, ["sm"] = ShadowPreset.Sm,
        [""] = ShadowPreset.Sm, ["md"] = ShadowPreset.Md, ["lg"] = ShadowPreset.Lg, ["xl"] = ShadowPreset.Xl,
        ["2xl"] = ShadowPreset.TwoXl,
    };

    /// <summary>--text-*: (cỡ chữ rem, line-height rem).</summary>
    public static readonly Dictionary<string, (float size, float leading)> FontSizes = new(StringComparer.Ordinal)
    {
        ["xs"] = (0.75f, 1f), ["sm"] = (0.875f, 1.25f), ["base"] = (1f, 1.5f), ["lg"] = (1.125f, 1.75f),
        ["xl"] = (1.25f, 1.75f), ["2xl"] = (1.5f, 2f), ["3xl"] = (1.875f, 2.25f), ["4xl"] = (2.25f, 2.5f),
        ["5xl"] = (3f, 3f), ["6xl"] = (3.75f, 3.75f), ["7xl"] = (4.5f, 4.5f), ["8xl"] = (6f, 6f), ["9xl"] = (8f, 8f),
    };

    /// <summary>--leading-*: hệ số nhân cỡ chữ.</summary>
    public static readonly Dictionary<string, float> Leading = new(StringComparer.Ordinal)
    {
        ["none"] = 1f, ["tight"] = 1.25f, ["snug"] = 1.375f, ["normal"] = 1.5f, ["relaxed"] = 1.625f, ["loose"] = 2f,
    };

    public static readonly Dictionary<string, FontWeight> FontWeights = new(StringComparer.Ordinal)
    {
        ["thin"] = FontWeight.Thin, ["extralight"] = FontWeight.ExtraLight, ["light"] = FontWeight.Light,
        ["normal"] = FontWeight.Normal, ["medium"] = FontWeight.Medium, ["semibold"] = FontWeight.Semibold,
        ["bold"] = FontWeight.Bold, ["extrabold"] = FontWeight.ExtraBold, ["black"] = FontWeight.Black,
    };

    /// <summary>--breakpoint-* (rem): sm 40, md 48, lg 64, xl 80, 2xl 96.</summary>
    public static readonly Dictionary<string, float> Breakpoints = new(StringComparer.Ordinal)
    {
        ["sm"] = 40, ["md"] = 48, ["lg"] = 64, ["xl"] = 80, ["2xl"] = 96,
    };

    static float[] s_customBreakpoints = [];
    static readonly Lock s_bpLock = new();

    /// <summary>Breakpoint tuỳ ý đã gặp (min-[…]: / max-[…]:, rem) – cũng tính vào VariantContext.BreakpointBucket để cache view đúng.</summary>
    public static ReadOnlySpan<float> CustomBreakpoints => s_customBreakpoints;

    internal static void AddCustomBreakpoint(float rem)
    {
        lock (s_bpLock)
        {
            if (Array.IndexOf(s_customBreakpoints, rem) >= 0) return;
            s_customBreakpoints = [.. s_customBreakpoints, rem]; // chép khi ghi: luồng đọc không cần khoá
        }
    }

    /// <summary>--container-* (rem), dùng cho max-w-*, w-*.</summary>
    public static readonly Dictionary<string, float> Containers = new(StringComparer.Ordinal)
    {
        ["3xs"] = 16, ["2xs"] = 18, ["xs"] = 20, ["sm"] = 24, ["md"] = 28, ["lg"] = 32, ["xl"] = 36,
        ["2xl"] = 42, ["3xl"] = 48, ["4xl"] = 56, ["5xl"] = 64, ["6xl"] = 72, ["7xl"] = 80,
    };
}
