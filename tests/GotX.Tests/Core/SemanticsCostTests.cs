using GotX;
using GotX.Test;

/// <summary>Ngữ nghĩa không tốn gì khi không ai cần: Node không to thêm, frame thường không cấp phát thêm.</summary>
static class SemanticsCostTests
{
    // Kích thước Node ở HEAD (trước khi thêm Node.Sem): cột Sem nằm gọn trong phần đệm nên không đổi.
    const int HeadNodeBytes = 1416;

    public static void Register(TestSuite suite)
    {
        suite.Add("core/semantics/zero-cost-when-unused", t =>
        {
            t.Assert.True(SemanticsTree.NodeSizeBytes == HeadNodeBytes, $"Node = {SemanticsTree.NodeSizeBytes} byte (HEAD: {HeadNodeBytes})");
            // Cùng một cây, một bản gắn đủ Role/AriaLabel/AriaState, một bản trần: 1000 frame (cache tắt, render lại hết) cấp phát y hệt.
            long bare = Allocated(false), tagged = Allocated(true);
            t.Assert.Equal(tagged - bare, 0L, $"chênh cấp phát 1000 frame giữa view có ngữ nghĩa và không (trần {bare} B, gắn {tagged} B)");
            t.Notes.Add($"Node {SemanticsTree.NodeSizeBytes} B; 1000 frame: trần {bare} B, gắn ngữ nghĩa {tagged} B");
        });

        suite.Add("core/semantics/on-demand-reuses-buffers", t =>
        {
            t.Open(app => app.New(new SemanticsTests.Host()));
            var first = t.Window.Semantics(t.Size, t.Scale);
            for (int i = 0; i < 20; i++) t.Window.Semantics(t.Size, t.Scale); // làm nóng
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 50; i++) t.Window.Semantics(t.Size, t.Scale);
            long perBuild = (GC.GetAllocatedBytesForCurrentThread() - before) / 50;
            t.Assert.True(ReferenceEquals(first, t.Window.Semantics(t.Size, t.Scale)), "dùng lại cùng một SemanticsTree");
            t.Assert.True(perBuild < 64 * 1024, $"mỗi lần dựng cấp phát {perBuild} B (chủ yếu chuỗi tên); bộ đệm node/danh sách được tái dùng");
            t.Notes.Add($"dựng ngữ nghĩa lại: {perBuild} B/lần ({first.Count} node)");
        });
    }

    static long Allocated(bool tagged)
    {
        var app = new App();
        var window = new Window(app, TestFonts.Shared) { CacheEnabled = false };
        window.Root = app.New(new Plain { Tagged = tagged });
        var size = new Size(400, 300);
        for (int i = 0; i < 50; i++) window.Draw(size, 1);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++) window.Draw(size, 1);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    sealed class Plain : View
    {
        public bool Tagged;

        public override Div Render(Window w)
        {
            var root = w.Div().Class("flex flex-col gap-2 p-4");
            for (int i = 0; i < 20; i++)
            {
                var row = w.Div().Class("flex h-8 items-center").Focusable().Child("row");
                if (Tagged) row.Role(SemanticRole.Button).AriaLabel("row").AriaState(SemanticState.Selected, i == 3).AriaRange(0, 1, 0.5f).AriaDescribedBy("x").AriaValue("v");
                root.Child(row);
            }
            return root;
        }
    }
}
