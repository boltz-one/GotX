using GotX;
using GotX.Components;
using GotX.Test;

/// <summary>Test phần bảng của dashboard-01 (sắp xếp, chọn, phân trang, cột, menu hàng, tấm chi tiết), đường qua SiteShell và hiệu năng.</summary>
static class ShadcnDashboardTableTests
{
    public static void Register(TestSuite suite, Func<UiTest, Size?, Dashboard01Block> open)
    {
        suite.Add("shadcn/dashboard-01/table-sort-select-paginate", t =>
        {
            var table = open(t, null).Table.Table;
            t.Scroll(Dashboard01Block.ContentKey, 2000);                   // bảng nằm dưới thẻ và biểu đồ
            t.Advance(0.6);
            t.Assert.Equal(table.RowCount, 68, "số hàng như data.json của bản gốc");
            t.Assert.TextVisible("Page 1 of 7");
            t.Assert.TextVisible("0 of 68 row(s) selected.");

            t.Click(table.HeaderKey("target"));
            t.Assert.Equal(table.SortDirection, SortDirection.Ascending, "bấm tiêu đề Target sắp tăng dần");
            int low = table.RowAt(0), high = table.RowAt(67);
            t.Assert.True(table.Items[low].Target <= table.Items[table.RowAt(1)].Target && table.Items[low].Target <= table.Items[high].Target,
                $"hàng đầu có Target nhỏ nhất ({table.Items[low].Target} … {table.Items[high].Target})");
            t.Assert.True(t.Box(table.RowKey(low)).Y < t.Box(table.RowKey(table.RowAt(1))).Y, "hàng nhỏ nhất đứng trên cùng");
            t.Press(Key.Enter);                                            // nút tiêu đề vẫn giữ focus
            t.Assert.Equal(table.SortDirection, SortDirection.Descending, "Enter đảo chiều sắp xếp");
            t.Assert.Equal(table.RowAt(0) == high || table.Items[table.RowAt(0)].Target == table.Items[high].Target, true, "hàng lớn nhất lên đầu");

            int first = table.RowAt(0), second = table.RowAt(1);
            t.Click(table.RowCheckKey(first));
            Focus(t, table.RowCheckKey(second));
            t.Press(Key.Space);
            t.Assert.Equal(table.SelectedCount, 2, "chọn hai hàng (chuột + Space)");
            t.Assert.TextVisible("2 of 68 row(s) selected.");
            t.Click(table.NextPageKey);
            t.Assert.Equal(table.Page, 2, "nút tiến sang trang 2");
            t.Assert.TextVisible("Page 2 of 7");
            t.Assert.NotExists(table.RowKey(first));
            t.Assert.True(table.IsSelected(first) && table.SelectedCount == 2, "lựa chọn giữ nguyên khi đổi trang");
            t.Click(table.SelectAllKey);
            t.Assert.Equal(table.SelectedCount, 12, "ô tiêu đề chọn thêm 10 hàng của trang 2");
            t.Click(table.LastPageKey);
            t.Assert.Equal(table.Page, 7, "nút cuối tới trang 7");
            t.Assert.True(t.Exists(table.RowKey(table.RowAt(67))) && !t.Exists(table.RowKey(table.RowAt(59))), "trang cuối còn 8 hàng");
            t.Assert.NoLint(ShadcnDashboardTests.TabsQuirk());
        });

        suite.Add("shadcn/dashboard-01/table-columns-actions-detail", t =>
        {
            var view = open(t, null).Table;
            var table = view.Table;
            t.Scroll(Dashboard01Block.ContentKey, 2000);
            t.Advance(0.6);
            t.Assert.Exists(table.HeaderKey("reviewer"));
            t.Click(table.ColumnsMenu.TriggerKey);
            t.Press(Key.End);                                              // dòng cuối của menu cột là Reviewer
            t.Press(Key.Enter);
            t.Assert.True(table.ColumnsMenu.IsOpen, "menu cột vẫn mở sau khi bật/tắt");
            t.Press(Key.Escape);
            t.Assert.NotExists(table.HeaderKey("reviewer"));
            t.Assert.Focused(table.ColumnsMenu.TriggerKey);

            t.Click(table.RowActionKey(1));
            t.Click("dashboard-row-edit");
            t.Assert.Equal(view.LastAction, "Edit: Table of contents", "menu hàng: Edit");
            Focus(t, table.RowActionKey(0));
            t.Press(Key.Enter);
            t.Press(Key.End);
            t.Press(Key.Enter);
            t.Assert.Equal(view.LastAction, "Delete: Cover page", "menu hàng bằng bàn phím: Delete");
            t.Assert.Equal(table.RowCount, 67, "Delete xoá hàng");
            t.Click(DashboardTable.AddKey);
            t.Assert.Equal(table.RowCount, 68, "Add Section thêm một hàng");
            t.Assert.Equal(table.Items[67].Header, "New section 69", "hàng mới ở cuối danh sách");

            var section = table.Items[table.RowAt(0)];
            t.Click(section.LinkKey);
            t.Assert.True(view.Sheet.IsOpen && view.Viewing == section, "bấm link tiêu đề mở tấm chi tiết");
            t.Assert.TextVisible("Trending up by 5.2% this month");
            t.Click(DashboardTable.DoneKey);
            t.Assert.True(!view.Sheet.IsOpen, "nút Done đóng tấm");
            t.Assert.Focused(section.LinkKey);
        });

        suite.Add("shadcn/dashboard-01/narrow-window", t =>
        {
            var b = open(t, new Size(1024, 768));
            var cards = t.Box("dashboard-cards");
            t.Assert.True(cards.Height > 250, $"ở 1024px các thẻ xếp 2 cột × 2 hàng (cao {cards.Height})");
            t.Scroll(Dashboard01Block.ContentKey, 3000);
            t.Advance(0.6);
            t.Assert.True(t.Window.GetScroll(Dashboard01Block.ContentKey) is { Offset.Y: > 300 }, "vùng nội dung tự cuộn");
            t.Assert.Visible(b.Table.Table.NextPageKey);
            t.Assert.True(t.Window.GetScroll(b.Nav.ContentKey) is null or { Offset.Y: 0 }, "sidebar không cuộn theo");
        });

        suite.Add("shadcn/dashboard-01/through-site-shell", t =>
        {
            var shell = t.Open(new SiteShell(), new Size(1440, 900));
            t.Click("nav-blocks");
            t.Click("block-dashboard-01");
            t.Assert.Equal(shell.Current, "dashboard-01", "trang đang mở");
            t.Assert.TextVisible("Total Visitors");
            t.Assert.Near(t.Box(Dashboard01Block.SidebarKey).Width, 256, 0.5f, "bề rộng sidebar trong shell");
            t.Click(Dashboard01Block.TriggerKey);
            t.Assert.NotExists(Dashboard01Block.SidebarKey);
            t.ClickText("Last 30 days");
            t.Assert.TextVisible("Jun 1");
            t.Assert.NoLint(ShadcnDashboardTests.TabsQuirk());
        });

        suite.Add("shadcn/dashboard-01/perf", t =>
        {
            var b = open(t, null);
            var table = b.Table.Table;
            t.Scroll(Dashboard01Block.ContentKey, 2000);
            t.Advance(0.6);
            t.Assert.True(t.Settle(), "block đứng yên sau khi mở");
            t.Perf.ExpectIdle();
            // Sắp xếp: DataTable + DashboardTable + block + vỏ (Notify làm bẩn cả chuỗi view cha), hai frame của một cú bấm.
            t.Perf.ExpectRenders(() => t.Click(table.HeaderKey("limit")), max: 10);
            t.Perf.ExpectRenders(() => t.Click(table.RowCheckKey(table.RowAt(0))), max: 10);
            t.Perf.ExpectRenders(() => t.Click(table.NextPageKey), max: 10);
            // Đổi khoảng thời gian: ToggleGroup + Chart + thẻ biểu đồ + chuỗi view cha, hai frame của cú bấm (bảng còn đang
            // chuyển màu hàng vừa rời chuột nên cũng vẽ lại). Transition của nút chạy tiếp ~150 ms sau đó, không tính ở đây.
            t.Scroll(Dashboard01Block.ContentKey, -3000);
            t.Advance(0.6);
            t.Perf.ExpectRenders(() => t.ClickText("Last 7 days"), max: 16);
            t.Assert.True(t.Settle(), "đứng yên lại sau các thao tác");
            t.Perf.ExpectIdle();
            var report = t.Perf.Measure(frames: 30, raster: false);
            t.Assert.True(report.AllocatedBytes.Median <= 0, $"frame rảnh không cấp phát (trung vị {Fmt.Bytes((long)report.AllocatedBytes.Median)})");
            t.Perf.ExpectReplayMatchesFresh();
        });
    }

    static void Focus(UiTest t, string key) { t.Window.FocusKey(key); t.Frames(); }
}
