using GotX;
using GotX.Components;
using GotX.Test;

/// <summary>DataTable ở chế độ ảo hoá (Virtualized) trên 100.000 hàng thật: node bị chặn, cuộn, lọc, sắp xếp, chọn hàng.</summary>
static class VirtualDataTableTests
{
    sealed record Rec(int Id, string Name, int Amount);

    sealed class Harness
    {
        public DataTable<Rec> Table = null!;
        public Rec[] Rows = [];
    }

    static Harness Open(UiTest t)
    {
        var h = new Harness { Rows = Enumerable.Range(0, 100_000).Select(i => new Rec(i, "user" + i + "@example.com", (i * 7919) % 100_000)).ToArray() };
        t.Open(app =>
        {
            h.Table = app.New(new DataTable<Rec>
            {
                Columns =
                [
                    new DataColumn<Rec>("id", "Id") { Width = "80px", Text = r => r.Id.ToString(), Compare = DataColumn<Rec>.By(r => r.Id) },
                    new DataColumn<Rec>("email", "Email") { Text = r => r.Name, Compare = DataColumn<Rec>.ByText(r => r.Name) },
                    new DataColumn<Rec>("amount", "Amount") { Width = "100px", Text = r => r.Amount.ToString(), Compare = DataColumn<Rec>.By(r => r.Amount) },
                ],
                Items = h.Rows, Selectable = true, FilterColumn = "email", Virtualized = true, VirtualClass = "h-80",
            });
            return app.New(new FragmentView(w => w.Div().Class("size-full bg-background p-4 text-foreground").Child(w.View(h.Table))));
        }, new Size(900, 700));
        t.Settle();
        return h;
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("virtual-list/data-table/100k-rows-bounded-nodes-and-scroll-to-end", t =>
        {
            var h = Open(t);
            t.Assert.True(t.Tree.Nodes.Count < 500, $"DataTable 100.000 hàng: {t.Tree.Nodes.Count} node < 500");
            t.Assert.Equal(h.Table.RowCount, 100_000, "RowCount");
            t.Assert.Exists(h.Table.RowKey(0));
            t.Assert.NotExists(h.Table.RowKey(99_999));
            t.Assert.NotExists(h.Table.NextPageKey);
            var area = t.Get(h.Table.List!.ScrollKey).Screen;
            var centre = new Point(area.X + 200, area.Y + 100);
            var report = t.Perf.Measure(frames: 40, raster: false, each: i =>
                t.Window.DispatchInput(new InputEvent(InputKind.Scroll, centre, Delta: new Point(0, 100_000))));
            t.Assert.True(report.Total.Median < 120, $"cuộn bảng 100k hàng: trung vị Draw {report.Total.Median:0.0} ms < 120 ms ({report})");
            t.Scroll(centre, 100_000_000f);
            t.Assert.Exists(h.Table.RowKey(99_999));
            t.Assert.True(t.Tree.Nodes.Count < 500, $"ở cuối: {t.Tree.Nodes.Count} node < 500");
            t.Assert.TextContains(h.Table.RowKey(99_999), "user99999@example.com");
            t.Perf.ExpectIdle();
            t.Assert.NoLint();
        });

        suite.Add("virtual-list/data-table/filter-sort-select", t =>
        {
            var h = Open(t);
            h.Table.Sort("amount", SortDirection.Descending);
            t.Settle();
            int top = h.Table.RowAt(0);
            t.Assert.Equal(h.Rows[top].Amount, h.Rows.Max(r => r.Amount), "sắp xếp giảm dần theo amount: hàng đầu là lớn nhất");
            t.Assert.Exists(h.Table.RowKey(top));
            h.Table.Sort(null, SortDirection.None);
            h.Table.Filter = "user9999@";
            t.Settle();
            t.Assert.Equal(h.Table.RowCount, 1, "lọc user9999@ còn đúng 1 hàng");
            t.Assert.Exists(h.Table.RowKey(9999));
            h.Table.Filter = "user1234";
            t.Settle();
            int expected = h.Rows.Count(r => r.Name.Contains("user1234", StringComparison.OrdinalIgnoreCase));
            t.Assert.Equal(h.Table.RowCount, expected, "lọc user1234 khớp LINQ");
            t.Click(h.Table.RowCheckKey(h.Table.RowAt(0)));
            t.Assert.Equal(h.Table.SelectedCount, 1, "bấm ô đánh dấu của hàng đầu");
            t.Click(h.Table.SelectAllKey);
            t.Assert.Equal(h.Table.SelectedCount, expected, "ô tiêu đề chọn mọi hàng sau lọc");
            h.Table.Filter = "";
            t.Settle();
            t.Assert.Equal(h.Table.SelectedCount, expected, "xoá lọc: lựa chọn theo dữ liệu không mất");
            t.Assert.True(t.Tree.Nodes.Count < 500, "node vẫn bị chặn");
        });
    }
}
