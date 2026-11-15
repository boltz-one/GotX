using GotX.Test;

/// <summary>Test các block của ví dụ samples/Shadcn; mỗi nhóm block đăng ký ở file riêng để từng file dưới 200 dòng.</summary>
static class ShadcnBlocksTests
{
    public static void Register(TestSuite suite)
    {
        ShadcnDashboardTests.Register(suite);
        ShadcnSidebarTests.Register(suite);
        ShadcnLoginTests.Register(suite);
    }
}
