namespace GotX.Components;

/// <summary>
/// Token thiết kế của shadcn/ui (bảng "neutral"): màu ngữ nghĩa có giá trị sáng/tối và thang bo góc theo --radius 0.625rem.
/// Gọi <see cref="Install"/> một lần lúc khởi động, trước khi render bất kỳ component nào.
/// </summary>
public static class ComponentTheme
{
    static bool s_installed;

    /// <summary>
    /// Như <see cref="Install()"/>, và đặt <paramref name="style"/> làm style mặc định của mọi component trong app
    /// (gọi lại để đổi). Một phần giao diện dùng style khác thì bọc bằng <see cref="ComponentStyles.Use(ComponentStyle)"/>.
    /// </summary>
    public static void Install(ComponentStyle style)
    {
        Install();
        ComponentStyles.SetDefault(ComponentStyles.Sheet(style));
    }

    /// <summary>Style mặc định hiện tại của app.</summary>
    public static ComponentStyle Style => ComponentStyles.AppDefault.Style;

    /// <summary>
    /// Đổi style của app lúc đang chạy (vd. khi đổi trang) và vẽ lại mọi view của mọi cửa sổ. Component được tạo trong
    /// <see cref="ComponentStyles.Use(ComponentStyle)"/> giữ style của phạm vi đó.
    /// </summary>
    public static void Use(App app, ComponentStyle style)
    {
        Install();
        var sheet = ComponentStyles.Sheet(style);
        if (ReferenceEquals(ComponentStyles.AppDefault, sheet)) return;
        ComponentStyles.SetDefault(sheet);
        app.InvalidateAllWindows();
    }

    public static void Install()
    {
        if (s_installed) return;
        s_installed = true;

        static Rgba Hex(uint rgb) => Rgba.FromHex(rgb);
        var white10 = Rgba.White.WithAlpha(0.10f);
        var white15 = Rgba.White.WithAlpha(0.15f);

        Theme.SetColor("background", Hex(0xffffff), Hex(0x0a0a0a));
        Theme.SetColor("foreground", Hex(0x0a0a0a), Hex(0xfafafa));
        Theme.SetColor("card", Hex(0xffffff), Hex(0x171717));
        Theme.SetColor("card-foreground", Hex(0x0a0a0a), Hex(0xfafafa));
        Theme.SetColor("popover", Hex(0xffffff), Hex(0x171717));
        Theme.SetColor("popover-foreground", Hex(0x0a0a0a), Hex(0xfafafa));
        Theme.SetColor("primary", Hex(0x171717), Hex(0xe5e5e5));
        Theme.SetColor("primary-foreground", Hex(0xfafafa), Hex(0x171717));
        Theme.SetColor("secondary", Hex(0xf5f5f5), Hex(0x262626));
        Theme.SetColor("secondary-foreground", Hex(0x171717), Hex(0xfafafa));
        Theme.SetColor("muted", Hex(0xf5f5f5), Hex(0x262626));
        Theme.SetColor("muted-foreground", Hex(0x737373), Hex(0xa1a1a1));
        Theme.SetColor("accent", Hex(0xf5f5f5), Hex(0x262626));
        Theme.SetColor("accent-foreground", Hex(0x171717), Hex(0xfafafa));
        Theme.SetColor("destructive", Hex(0xe7000b), Hex(0xff6467));
        Theme.SetColor("border", Hex(0xe5e5e5), white10);
        Theme.SetColor("input", Hex(0xe5e5e5), white15);
        Theme.SetColor("ring", Hex(0xa1a1a1), Hex(0x737373));
        Theme.SetColor("chart-1", Hex(0xd4d4d4), Hex(0xd4d4d4));
        Theme.SetColor("chart-2", Hex(0x737373), Hex(0x737373));
        Theme.SetColor("chart-3", Hex(0x525252), Hex(0x525252));
        Theme.SetColor("chart-4", Hex(0x404040), Hex(0x404040));
        Theme.SetColor("chart-5", Hex(0x262626), Hex(0x262626));
        Theme.SetColor("sidebar", Hex(0xfafafa), Hex(0x171717));
        Theme.SetColor("sidebar-foreground", Hex(0x0a0a0a), Hex(0xfafafa));
        Theme.SetColor("sidebar-primary", Hex(0x171717), Hex(0x1447e6));
        Theme.SetColor("sidebar-primary-foreground", Hex(0xfafafa), Hex(0xfafafa));
        Theme.SetColor("sidebar-accent", Hex(0xf5f5f5), Hex(0x262626));
        Theme.SetColor("sidebar-accent-foreground", Hex(0x171717), Hex(0xfafafa));
        Theme.SetColor("sidebar-border", Hex(0xe5e5e5), white10);
        Theme.SetColor("sidebar-ring", Hex(0xa1a1a1), Hex(0x737373));

        // --radius: 0.625rem → sm ×0.6, md ×0.8, lg ×1, xl ×1.4, 2xl ×1.8, 3xl ×2.2, 4xl ×2.6
        Theme.Radius["sm"] = 6; Theme.Radius[""] = 6; Theme.Radius["md"] = 8; Theme.Radius["lg"] = 10;
        Theme.Radius["xl"] = 14; Theme.Radius["2xl"] = 18; Theme.Radius["3xl"] = 22; Theme.Radius["4xl"] = 26;
    }
}
