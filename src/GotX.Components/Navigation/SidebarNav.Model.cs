namespace GotX.Components;

/// <summary>Cách hiện mục con của một nhóm: gập/mở bằng nút cha có mũi tên xoay, hoặc luôn hiện dưới nút cha.</summary>
public enum SidebarSubMenus : byte { Collapsible, Always }

/// <summary>Kiểu khung sidebar của shadcn: dính mép có viền phải, hoặc "inset" (nền sidebar bao quanh vùng nội dung bo góc).</summary>
public enum SidebarVariant : byte { Sidebar, Inset }

/// <summary>
/// Một nút của <see cref="SidebarNav"/> (SidebarMenuItem + SidebarMenuButton).
/// <code>
/// new("Playground", SidebarIcons.SquareTerminal) { DefaultOpen = true, SubItems = ["History", "Starred"] }
/// new("Travel", SidebarIcons.Map) { Menu = [MenuItem.Action("View Project")], ActionOnHover = true }
/// </code>
/// </summary>
public sealed record SidebarNavItem(string Title, Func<Window, Div>? Icon = null)
{
    /// <summary>Chữ nhỏ ở mép phải (SidebarMenuBadge). Không hiện khi nút có nút phụ.</summary>
    public string? Badge { get; init; }

    /// <summary>Tiêu đề các mục con (SidebarMenuSub); rỗng = nút thường.</summary>
    public IReadOnlyList<string> SubItems { get; init; } = [];

    /// <summary>Mở sẵn danh sách con (chỉ có nghĩa với <see cref="SidebarSubMenus.Collapsible"/>).</summary>
    public bool DefaultOpen { get; init; }

    /// <summary>Chữ đậm vừa (font-medium) – nút cha của danh sách con luôn mở trong sidebar-03.</summary>
    public bool Emphasis { get; init; }

    /// <summary>Chữ mờ 70% – nút phụ kiểu "More".</summary>
    public bool Muted { get; init; }

    /// <summary>
    /// Icon của nút phụ ở mép phải (SidebarMenuAction). Có <see cref="Menu"/> mà để trống thì dùng ba chấm ngang.
    /// Không có <see cref="Menu"/>: bấm nút phụ phát <see cref="SidebarNav.ActionInvoked"/>.
    /// </summary>
    public Func<Window, Div>? Action { get; init; }

    /// <summary>Nút phụ chỉ hiện khi rê chuột vào hàng, khi nó được focus bằng bàn phím hoặc khi menu của nó đang mở.</summary>
    public bool ActionOnHover { get; init; }

    /// <summary>
    /// Các dòng của menu mở từ nút phụ (đặt bên phải nút, như <c>side="right" align="start"</c>). Chọn một dòng phát
    /// <see cref="SidebarNav.MenuSelected"/>. Dùng chung một danh sách cho nhiều hàng được.
    /// </summary>
    public IReadOnlyList<MenuItem>? Menu { get; init; }

    /// <summary>Chữ cái đầu – icon thay thế khi thu gọn mà nút không có icon; tính một lần để không cắt chuỗi mỗi frame.</summary>
    public string Initial { get; } = Title.Length > 0 ? Title[..1].ToUpperInvariant() : "";

    internal bool HasAction => Action is not null || Menu is not null;
}

/// <summary>Một nhóm nút (SidebarGroup). <paramref name="Label"/> null = không có nhãn; nhãn ẩn khi sidebar thu gọn.</summary>
public sealed record SidebarNavGroup(string? Label, IReadOnlyList<SidebarNavItem> Items)
{
    /// <summary>Cách hiện mục con của các nút trong nhóm.</summary>
    public SidebarSubMenus SubMenus { get; init; }

    /// <summary>Ẩn cả nhóm khi sidebar thu gọn còn icon (<c>group-data-[collapsible=icon]:hidden</c>).</summary>
    public bool HideWhenCollapsed { get; init; }

    /// <summary>Đẩy nhóm xuống đáy vùng menu (<c>mt-auto</c> của shadcn) – nav phụ của dashboard.</summary>
    public bool PushToBottom { get; init; }
}
