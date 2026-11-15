/// <summary>Danh sách block (dashboard, sidebar, login…) hiện trên hàng chọn của shell, theo thứ tự hiển thị.</summary>
public static class BlockCatalog
{
    public static readonly SitePage[] All =
    [
        Dashboard01Block.Page, Sidebar07Block.Page, Sidebar03Block.Page, Login03Block.Page, Login04Block.Page, Signup03Block.Page, Signup04Block.Page,
    ];
}
