using GotX;

/// <summary>
/// Một trang của site ví dụ. <paramref name="Create"/> dựng view của trang (gọi một lần, khi trang được mở lần đầu).
/// <paramref name="FullBleed"/> = trang tự lo toàn bộ khung nhìn (block dashboard/sidebar/login); ngược lại shell cho trang cuộn dọc.
/// </summary>
public readonly record struct SitePage(string Id, string Title, Func<App, View> Create, bool FullBleed = false);