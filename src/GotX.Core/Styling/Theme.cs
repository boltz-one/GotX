namespace GotX;

/// <summary>
/// Bộ token thiết kế, tương ứng khối <c>@theme</c> của Tailwind v4 (--spacing, --color-*, --radius-*,
/// --shadow-*, --text-*, --breakpoint-*, --container-*…). Utility chỉ là "tên + giá trị tra trong theme".
/// Có thể sửa hoặc thêm token lúc khởi động: <c>Theme.Colors["brand-500"] = Rgba.FromHex(0x0ea5e9)</c>.
/// Bảng màu dùng giá trị hex sRGB của Tailwind v3; v4 định nghĩa bằng OKLCH nên hơi rực hơn một chút.
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
        [0xf8fafc, 0xf1f5f9, 0xe2e8f0, 0xcbd5e1, 0x94a3b8, 0x64748b, 0x475569, 0x334155, 0x1e293b, 0x0f172a, 0x020617],
        [0xf9fafb, 0xf3f4f6, 0xe5e7eb, 0xd1d5db, 0x9ca3af, 0x6b7280, 0x4b5563, 0x374151, 0x1f2937, 0x111827, 0x030712],
        [0xfafafa, 0xf4f4f5, 0xe4e4e7, 0xd4d4d8, 0xa1a1aa, 0x71717a, 0x52525b, 0x3f3f46, 0x27272a, 0x18181b, 0x09090b],
        [0xfafafa, 0xf5f5f5, 0xe5e5e5, 0xd4d4d4, 0xa3a3a3, 0x737373, 0x525252, 0x404040, 0x262626, 0x171717, 0x0a0a0a],
        [0xfafaf9, 0xf5f5f4, 0xe7e5e4, 0xd6d3d1, 0xa8a29e, 0x78716c, 0x57534e, 0x44403c, 0x292524, 0x1c1917, 0x0c0a09],
        [0xfef2f2, 0xfee2e2, 0xfecaca, 0xfca5a5, 0xf87171, 0xef4444, 0xdc2626, 0xb91c1c, 0x991b1b, 0x7f1d1d, 0x450a0a],
        [0xfff7ed, 0xffedd5, 0xfed7aa, 0xfdba74, 0xfb923c, 0xf97316, 0xea580c, 0xc2410c, 0x9a3412, 0x7c2d12, 0x431407],
        [0xfffbeb, 0xfef3c7, 0xfde68a, 0xfcd34d, 0xfbbf24, 0xf59e0b, 0xd97706, 0xb45309, 0x92400e, 0x78350f, 0x451a03],
        [0xfefce8, 0xfef9c3, 0xfef08a, 0xfde047, 0xfacc15, 0xeab308, 0xca8a04, 0xa16207, 0x854d0e, 0x713f12, 0x422006],
        [0xf7fee7, 0xecfccb, 0xd9f99d, 0xbef264, 0xa3e635, 0x84cc16, 0x65a30d, 0x4d7c0f, 0x3f6212, 0x365314, 0x1a2e05],
        [0xf0fdf4, 0xdcfce7, 0xbbf7d0, 0x86efac, 0x4ade80, 0x22c55e, 0x16a34a, 0x15803d, 0x166534, 0x14532d, 0x052e16],
        [0xecfdf5, 0xd1fae5, 0xa7f3d0, 0x6ee7b7, 0x34d399, 0x10b981, 0x059669, 0x047857, 0x065f46, 0x064e3b, 0x022c22],
        [0xf0fdfa, 0xccfbf1, 0x99f6e4, 0x5eead4, 0x2dd4bf, 0x14b8a6, 0x0d9488, 0x0f766e, 0x115e59, 0x134e4a, 0x042f2e],
        [0xecfeff, 0xcffafe, 0xa5f3fc, 0x67e8f9, 0x22d3ee, 0x06b6d4, 0x0891b2, 0x0e7490, 0x155e75, 0x164e63, 0x083344],
        [0xf0f9ff, 0xe0f2fe, 0xbae6fd, 0x7dd3fc, 0x38bdf8, 0x0ea5e9, 0x0284c7, 0x0369a1, 0x075985, 0x0c4a6e, 0x082f49],
        [0xeff6ff, 0xdbeafe, 0xbfdbfe, 0x93c5fd, 0x60a5fa, 0x3b82f6, 0x2563eb, 0x1d4ed8, 0x1e40af, 0x1e3a8a, 0x172554],
        [0xeef2ff, 0xe0e7ff, 0xc7d2fe, 0xa5b4fc, 0x818cf8, 0x6366f1, 0x4f46e5, 0x4338ca, 0x3730a3, 0x312e81, 0x1e1b4b],
        [0xf5f3ff, 0xede9fe, 0xddd6fe, 0xc4b5fd, 0xa78bfa, 0x8b5cf6, 0x7c3aed, 0x6d28d9, 0x5b21b6, 0x4c1d95, 0x2e1065],
        [0xfaf5ff, 0xf3e8ff, 0xe9d5ff, 0xd8b4fe, 0xc084fc, 0xa855f7, 0x9333ea, 0x7e22ce, 0x6b21a8, 0x581c87, 0x3b0764],
        [0xfdf4ff, 0xfae8ff, 0xf5d0fe, 0xf0abfc, 0xe879f9, 0xd946ef, 0xc026d3, 0xa21caf, 0x86198f, 0x701a75, 0x4a044e],
        [0xfdf2f8, 0xfce7f3, 0xfbcfe8, 0xf9a8d4, 0xf472b6, 0xec4899, 0xdb2777, 0xbe185d, 0x9d174d, 0x831843, 0x500724],
        [0xfff1f2, 0xffe4e6, 0xfecdd3, 0xfda4af, 0xfb7185, 0xf43f5e, 0xe11d48, 0xbe123c, 0x9f1239, 0x881337, 0x4c0519],
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
