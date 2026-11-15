namespace GotX.Components;

/// <summary>Phần vẽ của <see cref="DataTable{T}"/>: hàng công cụ, bảng (lưới), hàng phân trang.</summary>
public sealed partial class DataTable<T>
{
    static readonly string[] HeadCell =
    [
        "flex h-10 min-w-0 items-center justify-start px-2 font-medium text-foreground", "flex h-10 min-w-0 items-center justify-center px-2 font-medium text-foreground",
        "flex h-10 min-w-0 items-center justify-end px-2 font-medium text-foreground",
    ];
    const string ControlCell = "flex items-center justify-center";

    public override Div Render(Window w)
    {
        _window = w;
        Refresh();
        var root = w.Div().Class("relative flex w-full flex-col gap-4");
        if (ShowToolbar) root.Child(Toolbar(w));
        root.Child(w.Div().Class(FrameClass).Child(Ui.Table(w).Child(HeaderRow(w)).Child(Body(w))));
        if (ShowPagination) root.Child(Footer(w));
        // View của menu hàng không có hình: đặt absolute để nó không chiếm một khoảng gap trong cột.
        if (RowActions is not null && _rowMenu is not null) root.Child(w.View(_rowMenu).Class("absolute"));
        return root;
    }

    Div Toolbar(Window w)
    {
        var bar = w.Div().Class("flex items-center gap-2");
        if (FilterColumn is not null) bar.Child(w.Div().Class("w-96 min-w-0").Child(w.View(FilterInput)));
        bar.Child(w.Div().Class("flex-1"));
        if (ToolbarExtra is { } extra) bar.Child(extra(w));
        if (_columnItems.Count > 0) bar.Child(w.View(ColumnsMenu));
        return bar;
    }

    Div HeaderRow(Window w)
    {
        var refs = Indexed<DataTable<T>>.Ensure(ref _columnRefs, this, _columns.Count);
        var row = Ui.TableRow(w).Class(Grid());
        if (HeaderRowClass.Length > 0) row.Class(HeaderRowClass);
        if (Selectable)
            row.Child(w.Div().Class(ControlCell).Child(DataTableParts.CheckBox(w, PageSelection()).Key(SelectAllKey)
                .OnClick(this, static (t, _) => t.SetPageSelected(t.PageSelection() != 1))));
        for (int c = 0; c < _columns.Count; c++)
        {
            var column = _columns[c];
            if (!column.Visible) continue;
            string key = Name(ref _headKeys, c, "head");
            if (column.Compare is null)
            {
                row.Child(w.Div().Class(HeadCell[(int)column.Align]).Key(key).Child(w.Div().Class("truncate").Child(column.Header)));
                continue;
            }
            // Tiêu đề sắp xếp được là một nút ghost (như ví dụ của shadcn); lề âm bù đệm của nút để chữ thẳng với chữ trong ô.
            bool sorted = c == _sortColumn;
            var icon = !sorted ? Icon.ChevronsUpDown(w).Class("text-muted-foreground")
                : _sortDirection == SortDirection.Ascending ? Icon.ArrowUp(w) : Icon.ArrowDown(w);
            row.Child(w.Div().Class(HeadCell[(int)column.Align]).Child(
                w.Div().Class(DataTableParts.SortButton).Focusable().Key(key)
                    .OnClick(refs[c], static (r, _) => r.Owner.ToggleSort(r.Index))
                    .Child(column.Header).Child(icon)));
        }
        if (RowActions is not null) row.Child(w.Div());
        return Ui.TableHeader(w).Child(row);
    }

    Div Body(Window w)
    {
        var body = Ui.TableBody(w);
        if (_count == 0)
            return body.Child(w.Div().Key(EmptyKey).Class("flex h-24 items-center justify-center text-sm").Child(EmptyText));
        var refs = Indexed<DataTable<T>>.Ensure(ref _rowRefs, this, _items.Count);
        string grid = Grid();
        int start = _page * _pageSize, end = Math.Min(_count, start + _pageSize);
        for (int p = start; p < end; p++)
        {
            int index = _order[p];
            var item = _items[index];
            var row = Ui.TableRow(w, _selected[index]).Class(grid).Key(RowKey(index));
            if (p == end - 1) row.Class("border-b-0");
            if (Selectable)
                row.Child(w.Div().Class(ControlCell).Child(DataTableParts.CheckBox(w, _selected[index] ? 1 : 0).Key(RowCheckKey(index))
                    .OnClick(refs[index], static (r, _) => r.Owner.SetSelected(r.Index, !r.Owner._selected[r.Index]))));
            foreach (var column in _columns)
            {
                if (!column.Visible) continue;
                var cell = Ui.TableCell(w, column.Align).Class("min-w-0");
                row.Child(column.Cell is { } draw ? cell.Child(draw(w, item))
                    : cell.Child(w.Div().Class("truncate").Child(column.Text?.Invoke(item) ?? "")));
            }
            if (RowActions is not null)
            {
                string key = RowActionKey(index);
                row.Child(w.Div().Class(ControlCell).Child(w.Div().Class(DataTableParts.ActionButton).Focusable().Key(key).Anchor(key)
                    .OnClick(refs[index], static (r, _) => r.Owner.OpenRowMenu(r.Index))
                    .OnKey(refs[index], static (r, e) => { if (e.Key != GotX.Key.Down) return false; r.Owner.OpenRowMenu(r.Index); return true; })
                    .Child(Icon.MoreHorizontal(w))));
            }
            body.Child(row);
        }
        return body;
    }

    Div PageButton(Window w, string key, Func<Window, Div> icon, bool disabled, bool wideOnly) =>
        w.Div().Class(DataTableParts.PageButton).Class(wideOnly ? "hidden lg:flex" : "").Focusable().Key(key).Disabled(disabled).Child(icon(w));

    Div Footer(Window w)
    {
        int last = PageCount;
        bool atStart = _page == 0, atEnd = _page >= last - 1;
        // Như dashboard-01: dưới lg chỉ còn "Page n of m" và hai nút lùi/tiến; dòng đã chọn, số hàng mỗi trang, nút đầu/cuối ẩn đi.
        var right = w.Div().Class("flex w-full items-center gap-8 lg:w-auto")
            .Child(w.Div().Class("hidden items-center gap-2 lg:flex")
                .Child(Ui.Label(w, RowsPerPageLabel).Class("whitespace-nowrap")).Child(w.View(PageSizeMenu)))
            .Child(w.Div().Class("flex items-center justify-center text-sm font-medium whitespace-nowrap").Child(PageText))
            .Child(w.Div().Class("flex-1 lg:hidden"))
            .Child(w.Div().Class("flex items-center gap-2")
                .Child(PageButton(w, FirstPageKey, DataTableParts.ChevronsLeft, atStart, true).OnClick(this, static (t, _) => t.Page = 1))
                .Child(PageButton(w, PreviousPageKey, Icon.ChevronLeft, atStart, false).OnClick(this, static (t, _) => t.Page--))
                .Child(PageButton(w, NextPageKey, Icon.ChevronRight, atEnd, false).OnClick(this, static (t, _) => t.Page++))
                .Child(PageButton(w, LastPageKey, DataTableParts.ChevronsRight, atEnd, true).OnClick(this, static (t, _) => t.Page = t.PageCount)));
        return w.Div().Class("flex items-center justify-between gap-4 px-2")
            .Child(w.Div().Class("hidden min-w-0 flex-1 text-sm text-muted-foreground lg:flex").Child(Selectable ? SelectionText : ""))
            .Child(right);
    }
}
