namespace GotX.Components;

/// <summary>
/// Style registry của shadcn mà component vẽ theo. Default = bảng class GotX vẫn dùng (khớp các block new-york-v4);
/// Rhea = style "rhea" (trang chủ ui.shadcn.com: control cao 32px bo 18px, nền input/50, thẻ bo 24px đệm 20px);
/// NewYorkV4 = registry/new-york-v4 (các block: control cao 36px bo md, shadow-xs, thẻ có viền đệm 24px).
/// </summary>
public enum ComponentStyle : byte { Default, Rhea, NewYorkV4 }

/// <summary>
/// Chọn bảng class (<see cref="ComponentStyleSheet"/>) cho component. Như React chọn style theo chỗ import
/// (<c>@/styles/base-rhea/ui/button</c>), một trang có thể trộn style: <see cref="ComponentTheme.Install(ComponentStyle)"/>
/// / <see cref="ComponentTheme.Use(App, ComponentStyle)"/> đặt style mặc định của cả app (đổi được lúc chạy),
/// <see cref="Use(ComponentStyle)"/> đè trong một phạm vi (thân Render / OnCreated của một view). Component có state
/// (Checkbox, Select…) được tạo trong một phạm vi thì ghim bảng của phạm vi đó (chúng vẽ lại riêng, ngoài phạm vi của view
/// cha); tạo ngoài phạm vi thì theo style chung. Mọi bảng dựng sẵn: lúc vẽ chỉ có một lần đọc biến + tra mảng.
/// </summary>
public static class ComponentStyles
{
    [ThreadStatic] static ComponentStyleSheet? t_scope;
    static ComponentStyleSheet s_default = ComponentStyleSheet.Default;

    /// <summary>Bảng đang hiệu lực: bảng của phạm vi gần nhất, không có thì bảng mặc định của app.</summary>
    public static ComponentStyleSheet Current => t_scope ?? s_default;

    /// <summary>Bảng dựng sẵn của một style.</summary>
    public static ComponentStyleSheet Sheet(ComponentStyle style) => style switch
    {
        ComponentStyle.Rhea => ComponentStyleSheet.Rhea,
        ComponentStyle.NewYorkV4 => ComponentStyleSheet.NewYorkV4,
        _ => ComponentStyleSheet.Default,
    };

    /// <summary>Đè style trong phạm vi <c>using</c>: <c>using var _ = ComponentStyles.Use(ComponentStyle.Rhea);</c>.</summary>
    public static Scope Use(ComponentStyle style) => Use(Sheet(style));

    /// <inheritdoc cref="Use(ComponentStyle)"/>
    public static Scope Use(ComponentStyleSheet sheet)
    {
        var previous = t_scope;
        t_scope = sheet;
        return new Scope(previous);
    }

    internal static void SetDefault(ComponentStyleSheet sheet) => s_default = sheet;

    /// <summary>Bảng của phạm vi Use đang mở (null = không có): component có state ghim bảng này lúc được tạo.</summary>
    internal static ComponentStyleSheet? Scoped => t_scope;

    /// <summary>Style mặc định của app (không tính phạm vi).</summary>
    internal static ComponentStyleSheet AppDefault => s_default;

    // Viết tắt cho component: Ui.* đọc bảng hiện hành bằng S.
    internal static ComponentStyleSheet S => t_scope ?? s_default;

    /// <summary>Phạm vi của <see cref="Use(ComponentStyle)"/>: Dispose trả lại bảng trước đó.</summary>
    public readonly struct Scope(ComponentStyleSheet? previous) : IDisposable
    {
        public void Dispose() => t_scope = previous;
    }
}
