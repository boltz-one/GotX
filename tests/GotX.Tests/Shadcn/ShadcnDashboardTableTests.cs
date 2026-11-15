using GotX;
using GotX.Components;
using GotX.Test;

/// <summary>Test phần bảng của dashboard-01 (chọn, phân trang, cột, menu hàng, drawer chi tiết), đường qua SiteShell và hiệu năng.</summary>
static class ShadcnDashboardTableTests
{
    public static void Register(TestSuite suite, Func<UiTest, Size?, Dashboard01Block> open)
    {
        suite.Add("shadcn/dashboard-01/table-select-paginate", t =>
        {
            var table = open(t, null).Table;
            t.Scroll(Dashboard01Block.ContentKey, 2000);                   // bảng nằm dưới thẻ và biểu đồ
            t.Advance(0.6);
            t.Assert.Equal(table.Sections.Count, 68, "số hàng như data.json của bản gốc");
            t.Assert.TextVisible("Page 1 of 7");
            t.Assert.TextVisible("0 of 68 row(s) selected.");
            t.Assert.True(t.Tree.FindText("Header") is not null, "tiêu đề cột Header");

            var first = table.Sections[0];
            var second = table.Sections[1];
            t.Click(first.SelectKey);
            Focus(t, second.SelectKey);
            t.Press(Key.Enter);                                            // Radix Checkbox: Enter không đổi trạng thái
            t.Assert.Equal(table.SelectedCount, 1, "Enter trên ô chọn không bấm");
            t.Press(Key.Space);
            t.Assert.Equal(table.SelectedCount, 2, "chọn hai hàng (chuột + Space)");
            t.Assert.TextVisible("2 of 68 row(s) selected.");
            t.Click(DashboardTable.NextPageKey);
            t.Assert.Equal(table.Page, 2, "nút tiến sang trang 2");
            t.Assert.TextVisible("Page 2 of 7");
            t.Assert.NotExists(first.RowKey);
            t.Assert.True(table.IsSelected(first.Id) && table.SelectedCount == 2, "lựa chọn giữ nguyên khi đổi trang");
            t.Click(DashboardTable.SelectAllKey);
            t.Assert.Equal(table.SelectedCount, 12, "ô tiêu đề chọn thêm 10 hàng của trang 2");
            t.Click(DashboardTable.LastPageKey);
            t.Assert.Equal(table.Page, 7, "nút cuối tới trang 7");
            t.Assert.True(t.Exists(table.Sections[67].RowKey) && !t.Exists(table.Sections[59].RowKey), "trang cuối còn 8 hàng");
            t.Assert.NoLint();
        });

        suite.Add("shadcn/dashboard-01/table-columns-actions-detail", t =>
        {
            var table = open(t, null).Table;
            t.Scroll(Dashboard01Block.ContentKey, 2000);
            t.Advance(0.6);
            t.Assert.Exists(DashboardTable.HeadKey("reviewer"));
            t.Click(table.ColumnsMenu.TriggerKey);
            t.Press(Key.End);                                              // dòng cuối của menu cột là Reviewer
            t.Press(Key.Enter);
            t.Assert.True(table.ColumnsMenu.IsOpen, "menu cột vẫn mở sau khi bật/tắt");
            t.Press(Key.Escape);
            t.Assert.NotExists(DashboardTable.HeadKey("reviewer"));
            t.Assert.Focused(table.ColumnsMenu.TriggerKey);

            t.Click(table.RowMenu(2).TriggerKey);
            t.Click("dashboard-row-edit");
            t.Assert.Equal(table.LastAction, "Edit: Table of contents", "menu hàng: Edit");
            Focus(t, table.RowMenu(1).TriggerKey);
            t.Press(Key.Enter);
            t.Press(Key.End);
            t.Press(Key.Enter);
            t.Assert.Equal(table.LastAction, "Delete: Cover page", "menu hàng bằng bàn phím: Delete");
            t.Assert.Equal(table.Sections.Count, 68, "Delete của bản gốc không gắn hành vi: hàng còn nguyên");
            t.Click(DashboardTable.AddKey);
            t.Assert.Equal(table.Sections.Count, 69, "Add Section thêm một hàng");
            t.Assert.Equal(table.Sections[68].Header, "New section 69", "hàng mới ở cuối danh sách");

            var section = table.Sections[0];
            t.Click(section.LinkKey);
            t.Assert.True(table.Drawer.IsOpen && table.Viewing == section, "bấm link tiêu đề mở drawer chi tiết");
            t.Assert.TextVisible("Trending up by 5.2% this month");
            t.Click(DashboardTable.DoneKey);
            t.Assert.True(!table.Drawer.IsOpen, "nút Done đóng drawer");
            t.Assert.Focused(section.LinkKey);
        });

        suite.Add("shadcn/dashboard-01/drag-reorder", t =>
        {
            var table = open(t, null).Table;
            t.Scroll(Dashboard01Block.ContentKey, 2000);
            t.Advance(0.6);
            var cover = table.Sections[0];
            // Chuột: kéo tay nắm của hàng 1 xuống hai hàng (53px mỗi hàng) rồi thả.
            var from = t.Box(cover.DragKey);
            var start = new Point(from.X + from.Width / 2, from.Y + from.Height / 2);
            t.Drag(start, new Point(start.X, start.Y + 2 * 53), steps: 6);
            t.Advance(0.4);
            t.Assert.Equal(table.Sections[2], cover, "Cover page xuống vị trí thứ 3");
            t.Assert.Equal(table.DraggingId, -1, "thả xong thì hết kéo");
            // Bàn phím (KeyboardSensor): Space nhấc, ↑ dời, Space thả; Escape huỷ.
            Focus(t, cover.DragKey);
            t.Press(Key.Space);
            t.Press(Key.Up);
            t.Press(Key.Up);
            t.Press(Key.Space);
            t.Assert.Equal(table.Sections[0], cover, "↑↑ đưa hàng về đầu");
            t.Press(Key.Space);
            t.Press(Key.Down);
            t.Press(Key.Escape);
            t.Assert.Equal(table.Sections[0], cover, "Escape huỷ, giữ thứ tự");
            t.Assert.NoLint();
        });

        suite.Add("shadcn/dashboard-01/narrow-window", t =>
        {
            var b = open(t, new Size(1024, 768));
            var cards = t.Box("section-cards");
            t.Assert.True(cards.Height > 250, $"ở 1024px các thẻ xếp 2 cột × 2 hàng (cao {cards.Height})");
            t.Scroll(Dashboard01Block.ContentKey, 3000);
            t.Advance(0.6);
            t.Assert.True(t.Window.GetScroll(Dashboard01Block.ContentKey) is { Offset.Y: > 300 }, "vùng nội dung tự cuộn");
            t.Assert.Visible(DashboardTable.NextPageKey);
            t.Assert.True(t.Find(DashboardTable.ViewSelectKey) is { Visible.IsEmpty: false } && t.Find(DashboardTable.TabsKey) is not { Visible.IsEmpty: false }, "dưới @4xl/main: Select thay cho thanh tab");
            t.Assert.True(t.Window.GetScroll(b.Nav.ContentKey) is null or { Offset.Y: 0 }, "sidebar không cuộn theo");
        });

        suite.Add("shadcn/dashboard-01/through-site-shell", t =>
        {
            var shell = t.Open(new SiteShell(), new Size(1440, 900));
            t.Click("nav-blocks");
            t.Click("block-dashboard-01");
            t.Assert.Equal(shell.Current, "dashboard-01", "trang đang mở");
            t.Assert.TextVisible("Total Visitors");
            t.Assert.Near(t.Box(Dashboard01Block.SidebarKey).Width, 288, 0.5f, "bề rộng sidebar trong shell");
            t.Click(Dashboard01Block.TriggerKey);
            t.Assert.NotExists(Dashboard01Block.SidebarKey);
            t.Click("toggle-30d");
            t.Assert.TextVisible("Jun 30");
            t.Assert.NoLint();
        });

        suite.Add("shadcn/dashboard-01/perf", t =>
        {
            var b = open(t, null);
            var table = b.Table;
            t.Scroll(Dashboard01Block.ContentKey, 2000);
            t.Advance(0.6);
            t.Assert.True(t.Settle(), "block đứng yên sau khi mở");
            t.Perf.ExpectIdle();
            // Mỗi hàng có view riêng (2 Input + menu "⋯" + Select người duyệt): bảng vẽ lại thì Core đo lại chúng, nên trần
            // tính theo một trang 10 hàng × 2 frame của cú bấm, không phải theo số view cố định như bảng không có ô nhập.
            t.Perf.ExpectRenders(() => t.Click(table.Sections[0].SelectKey), max: 90);
            t.Perf.ExpectRenders(() => t.Click(DashboardTable.NextPageKey), max: 130);
            t.Scroll(Dashboard01Block.ContentKey, -3000);
            t.Advance(0.6);
            t.Perf.ExpectRenders(() => t.Click("toggle-7d"), max: 110);
            t.Assert.True(t.Settle(), "đứng yên lại sau các thao tác");
            t.Perf.ExpectIdle();
            var report = t.Perf.Measure(frames: 30, raster: false);
            t.Assert.True(report.AllocatedBytes.Median <= 0, $"frame rảnh không cấp phát (trung vị {Fmt.Bytes((long)report.AllocatedBytes.Median)})");
            t.Perf.ExpectReplayMatchesFresh();
        });
    }

    static void Focus(UiTest t, string key) { t.Window.FocusKey(key); t.Frames(); }
}
