using GotX;
using GotX.Components;

/// <summary>Kiểm tra hành vi của <see cref="DataTable{T}"/> trên dữ liệu thanh toán mẫu của gallery (23 hàng).</summary>
static class DataTableChecks
{
    public static void Run(CheckHarness h)
    {
        var size = h.Size;
        h.Size = new Size(1100, 800); // ≥ lg để hàng phân trang hiện đủ
        DataTable<DemoPayment> t = null!;
        int sorts = 0, selections = 0, pages = 0;
        var actions = new List<string>();
        var w = h.Open(app =>
        {
            t = app.New(new DataTable<DemoPayment>
            {
                Columns = DataDemos.PaymentColumns(), Items = DataDemos.Payments, Selectable = true, FilterColumn = "email",
                RowActions = DataDemos.PaymentActions,
            });
            app.Subscribe<DataTable<DemoPayment>.SortChanged>(t, _ => sorts++);
            app.Subscribe<DataTable<DemoPayment>.SelectionChanged>(t, _ => selections++);
            app.Subscribe<DataTable<DemoPayment>.PageChanged>(t, _ => pages++);
            app.Subscribe<DataTable<DemoPayment>.RowAction>(t, e => actions.Add($"{e.Action.Text}:{e.Item.Id}"));
            return app.New(new FragmentView(w => w.Div().Class("size-full bg-background p-6 text-foreground").Child(w.View(t))));
        });
        var data = DataDemos.Payments;
        int n = data.Length;
        float Y(int index) => h.Box(w, t.RowKey(index)).Y;
        (int R, int G, int B) Corner(string key) { var b = h.Box(w, key); h.Wait(w, 0.3); return h.Pixel(w, new Point(b.X + 3, b.Y + 3)); }

        h.Check($"data-table: {n} hàng chia 3 trang, trang 1 hiện 10 hàng đầu ({t.RowCount}, {t.PageCount})",
            t.RowCount == n && t.PageCount == 3 && h.Exists(w, t.RowKey(9)) && !h.Exists(w, t.RowKey(10)));
        h.Check("data-table: cột ẩn sẵn (Payment ID) không có tiêu đề", !h.Exists(w, t.HeaderKey("id")) && h.Exists(w, t.HeaderKey("status")));
        var head = h.Box(w, t.HeaderKey("amount"));
        var row0 = h.Box(w, t.RowKey(0));
        h.Check($"data-table: cột số căn phải, nút tiêu đề nằm trong nửa phải bảng ({head.X} / {row0.X + row0.Width / 2})", head.X > row0.X + row0.Width / 2);

        // Sắp xếp bằng chuột: tăng dần rồi giảm dần; so với thứ tự tính độc lập bằng LINQ.
        var byEmail = Enumerable.Range(0, n).OrderBy(i => data[i].Email, StringComparer.OrdinalIgnoreCase).ToArray();
        h.Click(w, t.HeaderKey("email"));
        h.Check($"data-table: bấm tiêu đề Email sắp tăng dần ({t.SortColumn} {t.SortDirection})",
            t.SortColumn == "email" && t.SortDirection == SortDirection.Ascending && sorts == 1
            && t.RowAt(0) == byEmail[0] && t.RowAt(n - 1) == byEmail[^1] && Y(byEmail[0]) < Y(byEmail[1]));
        h.Check("data-table: nút tiêu đề giữ focus sau khi sắp xếp", w.IsFocused(t.HeaderKey("email")));
        h.Press(w, Key.Enter);
        h.Check($"data-table: Enter trên tiêu đề đang focus đảo thành giảm dần ({t.SortDirection})",
            t.SortDirection == SortDirection.Descending && sorts == 2 && t.RowAt(0) == byEmail[^1]);
        w.FocusKey(t.HeaderKey("amount"));
        h.Press(w, Key.Space);
        int cheapest = Enumerable.Range(0, n).OrderBy(i => data[i].Amount).ThenBy(i => i).First();
        h.Check($"data-table: Space trên tiêu đề Amount sắp theo số tiền ({t.SortColumn}, hàng đầu {data[t.RowAt(0)].Amount})",
            t.SortColumn == "amount" && t.RowAt(0) == cheapest);

        // Chọn hàng + ô "chọn cả trang" (gạch ngang khi mới chọn một phần).
        var off = Corner(t.SelectAllKey);
        int first = t.RowAt(0), second = t.RowAt(1);
        h.Click(w, t.RowCheckKey(first));
        var partial = Corner(t.SelectAllKey);
        h.Check($"data-table: bấm ô đánh dấu chọn hàng, ô tiêu đề chuyển sang trạng thái một phần ({off} → {partial})",
            t.IsSelected(first) && t.SelectedCount == 1 && selections == 1 && off.R > 200 && partial.R < 80 && Corner(t.RowCheckKey(first)).R < 80);
        h.Move(w, new Point(4, 4)); // rời chuột: hover:bg-muted/50 của Ui.TableRow thắng nền "đã chọn"
        h.Wait(w, 0.3);
        var rowBg = h.Pixel(w, new Point(h.Box(w, t.RowKey(first)).Right - 60, Y(first) + 4));
        h.Check($"data-table: hàng được chọn có nền muted ({rowBg})", rowBg.R is > 235 and < 250);
        h.Click(w, t.SelectAllKey);
        h.Check($"data-table: ô tiêu đề chọn hết 10 hàng của trang ({t.SelectedCount})", t.SelectedCount == 10 && t.IsSelected(second));
        h.Click(w, t.SelectAllKey);
        h.Check($"data-table: bấm lần nữa bỏ chọn cả trang ({t.SelectedCount})", t.SelectedCount == 0);

        // Focus và lựa chọn đi theo dữ liệu khi thứ tự đổi.
        t.PageSize = 30;
        h.Frames(w);
        w.FocusKey(t.RowCheckKey(first));
        h.Press(w, Key.Space);
        float before = Y(first);
        t.Sort("amount", SortDirection.Descending);
        h.Frames(w);
        h.Check($"data-table: sắp xếp lại thì hàng đã chọn vẫn được chọn, vẫn giữ focus và đổi chỗ ({before} → {Y(first)})",
            t.IsSelected(first) && w.IsFocused(t.RowCheckKey(first)) && Y(first) > before + 100 && t.RowAt(n - 1) == first);
        t.SetSelected(first, false);
        t.Sort(null, SortDirection.None);
        t.PageSize = 10;
        h.Frames(w);

        // Phân trang.
        pages = 0;
        h.Click(w, t.PreviousPageKey);
        h.Check("data-table: ở trang 1 nút lùi bị vô hiệu", t.Page == 1 && pages == 0);
        h.Click(w, t.NextPageKey);
        h.Check($"data-table: nút tiến sang trang 2 ({t.Page})", t.Page == 2 && pages == 1 && h.Exists(w, t.RowKey(10)) && !h.Exists(w, t.RowKey(0)));
        h.Click(w, t.LastPageKey);
        h.Check($"data-table: nút cuối tới trang 3 còn 3 hàng ({t.Page})", t.Page == 3 && h.Exists(w, t.RowKey(22)) && !h.Exists(w, t.RowKey(19)));
        h.Click(w, t.NextPageKey);
        h.Click(w, t.FirstPageKey);
        h.Check($"data-table: ở trang cuối nút tiến không làm gì; nút đầu về trang 1 ({t.Page}, {pages})", t.Page == 1 && pages == 3);
        h.Click(w, t.PageSizeMenu.TriggerKey);
        h.Press(w, Key.Down);
        h.Press(w, Key.Down);
        h.Press(w, Key.Enter);
        h.Check($"data-table: chọn 20 hàng mỗi trang bằng bàn phím ({t.PageSize}, {t.PageCount} trang)",
            t.PageSize == 20 && t.PageCount == 2 && h.Exists(w, t.RowKey(19)) && w.IsFocused(t.PageSizeMenu.TriggerKey));

        // Lọc theo email.
        w.Focus(t.FilterInput.Box);
        h.Type(w, "LI");
        int expected = data.Count(p => p.Email.Contains("li", StringComparison.OrdinalIgnoreCase));
        h.Check($"data-table: gõ vào ô lọc giữ lại hàng khớp, không phân biệt hoa thường ({t.RowCount} / {expected})",
            t.Filter == "LI" && t.RowCount == expected && expected is > 0 and < 23);
        h.Type(w, "zzz");
        h.Check("data-table: không hàng nào khớp thì hiện \"No results.\"", t.RowCount == 0 && h.Exists(w, t.EmptyKey) && t.PageCount == 1);
        t.Filter = "";
        h.Frames(w);
        h.Check($"data-table: xoá bộ lọc bằng code thì ô nhập cũng trống ({t.RowCount})", t.RowCount == n && t.FilterInput.Text == "");

        // Ẩn/hiện cột: Email không ẩn được nên menu chỉ có 3 dòng; dòng đầu là Status.
        h.Click(w, t.ColumnsMenu.TriggerKey);
        h.Press(w, Key.Down);
        h.Press(w, Key.Enter);
        h.Check($"data-table: bỏ tích Status trong menu cột thì cột biến mất, menu vẫn mở ({t.ColumnsMenu.Items.Count} dòng)",
            t.ColumnsMenu.Items.Count == 3 && t.ColumnsMenu.IsOpen && !t.Columns[0].Visible && !h.Exists(w, t.HeaderKey("status")));
        h.Press(w, Key.Escape);
        h.Check("data-table: Escape đóng menu cột, focus về nút", !t.ColumnsMenu.IsOpen && w.IsFocused(t.ColumnsMenu.TriggerKey));
        t.SetColumnVisible("id", true);
        h.Frames(w);
        h.Check("data-table: SetColumnVisible hiện cột Payment ID", h.Exists(w, t.HeaderKey("id")));

        // Menu thao tác của hàng: chuột rồi bàn phím.
        h.Click(w, t.RowActionKey(2));
        bool opened = t.RowMenu.IsOpen;
        h.Click(w, "d-action-copy");
        h.Check($"data-table: menu hàng mở bằng chuột, chọn dòng phát RowAction ({string.Join(", ", actions)})",
            opened && !t.RowMenu.IsOpen && actions.Count == 1 && actions[0] == "Copy payment ID:" + data[2].Id);
        w.FocusKey(t.RowActionKey(4));
        h.Press(w, Key.Enter);
        h.Press(w, Key.Down);
        h.Press(w, Key.Down);
        h.Press(w, Key.Enter);
        h.Check($"data-table: menu hàng dùng được bằng bàn phím, focus về nút \"⋯\" ({string.Join(", ", actions)})",
            actions.Count == 2 && actions[1] == "View customer:" + data[4].Id && w.IsFocused(t.RowActionKey(4)));
        h.Save(w, "data-table.png");
        h.Size = size;
    }
}
