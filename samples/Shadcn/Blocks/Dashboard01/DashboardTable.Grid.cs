using GotX;
using GotX.Components;

/// <summary>
/// Lưới của bảng Outline. Core không có <c>display: table</c>: mỗi hàng là một hàng flex, bề rộng cột tính theo auto table
/// layout của trình duyệt – cột tay nắm cố định (w-8 → 44px), mỗi cột còn lại lấy bề rộng max-content của nội dung trang
/// hiện tại, phần thừa của khung chia theo tỉ lệ bề rộng đó (flex-basis = max-content, flex-grow = max-content); khung
/// hẹp hơn tổng thì cuộn ngang (overflow-x-auto của Table).
/// </summary>
public sealed partial class DashboardTable
{
    const float DragWidth = 44;
    const string Checkbox = "flex size-4 shrink-0 items-center justify-center rounded-[4px] border border-input cursor-pointer transition-shadow " +
        "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30";
    const string CheckboxOn = "flex size-4 shrink-0 items-center justify-center rounded-[4px] border border-primary bg-primary text-primary-foreground " +
        "cursor-pointer transition-shadow focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-primary";
    const string LinkClass = "flex h-9 items-center text-left text-sm font-medium text-foreground underline-offset-4 cursor-pointer " +
        "whitespace-nowrap rounded-md hover:underline focus-visible:ring-3 focus-visible:ring-ring/50";
    const string CellBadge = "inline-flex shrink-0 items-center justify-center gap-1 overflow-hidden rounded-full border border-border px-1.5 py-0.5 " +
        "text-xs font-medium whitespace-nowrap text-muted-foreground";

    // Cột theo thứ tự của bản gốc; chỉ số dùng cho mảng bề rộng.
    enum Col { Drag, Select, Header, Type, Status, Target, Limit, Reviewer, Actions }

    readonly float[] _widths = new float[9];
    readonly bool[] _visible = new bool[9];
    Indexed<DashboardTable>[]? _rowRefs;

    IEnumerable<DashboardSection> PageRows() => _sections.Skip(_page * _pageSize).Take(_pageSize);

    /// <summary>Bề rộng max-content của từng cột cho trang hiện tại (gồm p-2; ô chọn chỉ có pl-2).</summary>
    void MeasureColumns(Window w)
    {
        var sm = TextStyle.Default with { FontSize = 14, Weight = FontWeight.Medium };
        var xs = TextStyle.Default with { FontSize = 12, Weight = FontWeight.Medium };
        var plain = TextStyle.Default with { FontSize = 14 };
        float header = w.Text.Measure("Header", sm), status = w.Text.Measure("Status", sm), reviewer = w.Text.Measure("Reviewer", sm);
        bool select = false;
        foreach (var s in PageRows())
        {
            header = Math.Max(header, w.Text.Measure(s.Header, sm));
            status = Math.Max(status, 6 + 12 + 4 + w.Text.Measure(s.Status, xs) + 6 + 2);
            if (s.Assigned) reviewer = Math.Max(reviewer, w.Text.Measure(s.Reviewer, plain)); else select = true;
        }
        _visible[(int)Col.Drag] = _visible[(int)Col.Select] = _visible[(int)Col.Header] = _visible[(int)Col.Actions] = true;
        for (int i = 0; i < Hideable.Length; i++) _visible[(int)Col.Type + i] = _columnItems[i].Checked;
        _widths[(int)Col.Drag] = DragWidth;
        _widths[(int)Col.Select] = 8 + 16;
        _widths[(int)Col.Header] = header + 16;
        _widths[(int)Col.Type] = Math.Max(128, w.Text.Measure("Section Type", sm)) + 16;
        _widths[(int)Col.Status] = status + 16;
        _widths[(int)Col.Target] = _widths[(int)Col.Limit] = 64 + 16;
        _widths[(int)Col.Reviewer] = (select ? Math.Max(152, reviewer) : reviewer) + 16;
        _widths[(int)Col.Actions] = 32 + 16;
    }

    float TotalWidth()
    {
        float sum = 0;
        for (int i = 0; i < _widths.Length; i++) if (_visible[i]) sum += _widths[i];
        return sum;
    }

    /// <summary>Một ô: basis = max-content, grow theo tỉ lệ (cột tay nắm không giãn).</summary>
    Div Cell(Window w, Col col, string classes)
    {
        var cell = w.Div().Class(classes);
        float width = _widths[(int)col];
        cell.Style.FlexBasis = Length.Px(width);
        cell.Style.FlexGrowValue = col == Col.Drag ? 0 : width;
        cell.Style.FlexShrinkValue = 0;
        return cell;
    }

    Div HeaderRow(Window w)
    {
        int on = PageRows().Count(s => _selected.Contains(s.Id));
        int count = PageRows().Count();
        bool all = count > 0 && on == count, some = on > 0 && !all;
        // Ô chọn tất cả: indeterminate của Radix vẫn hiện dấu tích nhưng không tô nền (chỉ data-[state=checked] có nền).
        var box = w.Div().Key(SelectAllKey).Focusable().Role(SemanticRole.Checkbox).AriaLabel("Select all").AriaState(SemanticState.Checked, all).Class(all ? CheckboxOn : Checkbox)
            .OnClick(this, static (t, _) => t.TogglePage())
            .OnKey(this, static (_, e) => e.Key == Key.Enter); // Radix Checkbox: Enter không bấm
        if (all || some) box.Child(Icon.Check(w).Class("size-3.5"));
        var row = w.Div().Class("flex h-10 border-b border-border bg-muted");
        row.Child(Cell(w, Col.Drag, V4.TableHead));
        row.Child(Cell(w, Col.Select, "flex h-10 items-center justify-center pl-2").Child(box));
        row.Child(Cell(w, Col.Header, V4.TableHead).Key("head-header").Child("Header"));
        if (_visible[(int)Col.Type]) row.Child(Cell(w, Col.Type, V4.TableHead).Key("head-type").Child("Section Type"));
        if (_visible[(int)Col.Status]) row.Child(Cell(w, Col.Status, V4.TableHead).Key("head-status").Child("Status"));
        if (_visible[(int)Col.Target]) row.Child(Cell(w, Col.Target, V4.TableHead + " justify-end").Key("head-target").Child("Target"));
        if (_visible[(int)Col.Limit]) row.Child(Cell(w, Col.Limit, V4.TableHead + " justify-end").Key("head-limit").Child("Limit"));
        if (_visible[(int)Col.Reviewer]) row.Child(Cell(w, Col.Reviewer, V4.TableHead).Key("head-reviewer").Child("Reviewer"));
        return row.Child(Cell(w, Col.Actions, V4.TableHead));
    }

    void TogglePage()
    {
        var rows = PageRows().ToList();
        bool all = rows.All(s => _selected.Contains(s.Id));
        foreach (var s in rows) { if (all) _selected.Remove(s.Id); else _selected.Add(s.Id); }
        Notify();
    }

    void ToggleRow(int id)
    {
        if (!_selected.Remove(id)) _selected.Add(id);
        Notify();
    }

    Div TableBox(Window w)
    {
        MeasureColumns(w);
        var table = w.Div().Class("flex w-full flex-col text-sm").Child(HeaderRow(w));
        table.Style.MinWidth = Length.Px(TotalWidth());
        var rows = PageRows().ToList();
        if (rows.Count == 0)
            table.Child(w.Div().Class("flex h-24 items-center justify-center p-2").Child("No results."));
        for (int i = 0; i < rows.Count; i++) table.Child(BodyRow(w, rows[i], i, i == rows.Count - 1));
        return w.Div().Key("table-container").Class("overflow-hidden rounded-lg border border-border")
            .Child(w.Div().Key("table-scroll").Class("relative w-full overflow-x-auto").Child(table));
    }

    Div BodyRow(Window w, DashboardSection s, int index, bool last)
    {
        var refs = Indexed<DashboardTable>.Ensure(ref _rowRefs, this, _sections.Max(static x => x.Id) + 1);
        var r = refs[s.Id];
        bool selected = _selected.Contains(s.Id);
        var row = w.Div().Key(s.RowKey).Class(selected ? V4.TableRowSelected : V4.TableRow);
        if (last) row.Class("border-b-0");
        // Đang kéo: hàng được kéo đi theo con trỏ (opacity-80), các hàng khác trượt nhường chỗ (transition của dnd-kit).
        if (_dragId >= 0)
        {
            row.Class(s.Id == _dragId ? "opacity-80" : "transition-transform duration-200");
            row.Style.TranslateY = Length.Px(DragOffset(index, s.Id));
        }
        var controls = Controls(s);

        row.Child(Cell(w, Col.Drag, V4.TableCell)
            .Child(Ui.Button(w, ButtonVariant.Ghost, ButtonSize.Icon).Class("size-7 text-muted-foreground hover:bg-transparent dark:hover:bg-transparent").Key(s.DragKey).AriaLabel("Drag to reorder")
                .OnDrag(r, static (x, e) => x.Owner.Drag(x.Index, e)).OnKey(r, static (x, e) => x.Owner.DragKey(x.Index, e))
                .Child(w.Path(DashboardIcons.GripVerticalPath).Class("size-3 shrink-0 text-muted-foreground"))));
        var box = w.Div().Key(s.SelectKey).Focusable().Role(SemanticRole.Checkbox).AriaLabel("Select row").AriaState(SemanticState.Checked, selected).Class(selected ? CheckboxOn : Checkbox)
            .OnClick(r, static (x, _) => x.Owner.ToggleRow(x.Index))
            .OnKey(r, static (_, e) => e.Key == Key.Enter);
        if (selected) box.Child(Icon.Check(w).Class("size-3.5"));
        row.Child(Cell(w, Col.Select, "flex items-center justify-center py-2 pl-2").Child(box));
        row.Child(Cell(w, Col.Header, V4.TableCell)
            .Child(w.Div().Key(s.LinkKey).Focusable().Class(LinkClass).OnClick(r, static (x, _) => x.Owner.OpenDetail(x.Index)).Child(s.Header)));
        if (_visible[(int)Col.Type])
            row.Child(Cell(w, Col.Type, V4.TableCell).Child(w.Div().Class("flex w-32").Child(w.Div().Class(CellBadge).Child(s.Type))));
        if (_visible[(int)Col.Status])
            row.Child(Cell(w, Col.Status, V4.TableCell).Child(w.Div().Class(CellBadge)
                .Child(s.Done ? w.Path(DashboardIcons.CircleCheckFilledPath).Class("size-3 shrink-0 text-green-500 dark:text-green-400")
                              : w.Path(DashboardIcons.LoaderPath).Class("size-3 shrink-0"))
                .Child(s.Status)));
        if (_visible[(int)Col.Target]) row.Child(Cell(w, Col.Target, V4.TableCell).Child(w.View(controls.Target).Key(s.TargetKey).Class("w-16")));
        if (_visible[(int)Col.Limit]) row.Child(Cell(w, Col.Limit, V4.TableCell).Child(w.View(controls.Limit).Key(s.LimitKey).Class("w-16")));
        if (_visible[(int)Col.Reviewer])
            row.Child(Cell(w, Col.Reviewer, V4.TableCell).Child(controls.Reviewer is { } reviewer
                ? w.View(reviewer).Key(s.ReviewerKey).Class("grid w-38")
                : w.Div().Child(s.Reviewer)));
        return row.Child(Cell(w, Col.Actions, V4.TableCell).Child(w.View(controls.Menu).Key(s.MenuKey)));
    }
}
