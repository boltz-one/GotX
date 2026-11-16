namespace GotX.Components;

/// <summary>Chiều sắp xếp của một cột trong <see cref="DataTable{T}"/>.</summary>
public enum SortDirection : byte { None, Ascending, Descending }

/// <summary>
/// Định nghĩa một cột của <see cref="DataTable{T}"/> (tương đương <c>ColumnDef</c> của TanStack Table trong bản gốc).
/// <code>
/// new DataColumn&lt;Payment&gt;("email", "Email") { Text = p =&gt; p.Email, Compare = DataColumn&lt;Payment&gt;.By(p =&gt; p.Email) },
/// new DataColumn&lt;Payment&gt;("amount", "Amount") { Width = "120px", Align = TableAlign.Right,
///     Cell = (w, p) =&gt; w.Div().Class("font-medium").Child(p.AmountText), Compare = DataColumn&lt;Payment&gt;.By(p =&gt; p.Amount) }
/// </code>
/// </summary>
public sealed class DataColumn<T>(string id, string header)
{
    /// <summary>Tên định danh (dùng cho sắp xếp, lọc, key của ô tiêu đề).</summary>
    public string Id { get; } = id;

    /// <summary>Chữ tiêu đề; cũng là nhãn trong menu ẩn/hiện cột.</summary>
    public string Header { get; } = header;

    /// <summary>
    /// Bề rộng cột dưới dạng một track của lưới: <c>"1fr"</c>, <c>"2fr"</c>, <c>"120px"</c>. Bảng là lưới (Core không có
    /// <c>display: table</c>) nên cột không tự co theo nội dung – không dùng <c>auto</c>.
    /// </summary>
    public string Width { get; init; } = "1fr";

    public TableAlign Align { get; init; }

    /// <summary>Chữ của ô; cũng là giá trị được so khi lọc theo cột này. Bỏ trống thì phải có <see cref="Cell"/>.</summary>
    public Func<T, string>? Text { get; init; }

    /// <summary>Tự vẽ nội dung ô (badge, nút, ô nhập…). Kết quả được đặt trong ô đã căn theo <see cref="Align"/>.</summary>
    public Func<Window, T, Div>? Cell { get; init; }

    /// <summary>Phép so để sắp xếp tăng dần; null = cột không sắp xếp được (tiêu đề là chữ thường, không phải nút).</summary>
    public Comparison<T>? Compare { get; init; }

    /// <summary>Cột có mặt trong menu ẩn/hiện cột hay không.</summary>
    public bool CanHide { get; init; } = true;

    /// <summary>Đang hiện hay không. Đổi lúc chạy qua <see cref="DataTable{T}.SetColumnVisible"/> để bảng vẽ lại.</summary>
    public bool Visible { get; internal set; } = true;

    /// <summary>Tạo cột ẩn sẵn (người dùng bật lại trong menu cột).</summary>
    public bool InitiallyHidden { init => Visible = !value; }

    /// <summary>Phép so theo một khoá (bộ chọn khoá sắp xếp): <c>Compare = DataColumn&lt;T&gt;.By(x =&gt; x.Amount)</c>.</summary>
    public static Comparison<T> By<TKey>(Func<T, TKey> key) where TKey : IComparable<TKey> =>
        (a, b) => Comparer<TKey>.Default.Compare(key(a), key(b));

    /// <summary>Phép so chuỗi không phân biệt hoa thường, theo thứ tự chữ cái (không theo mã ký tự).</summary>
    public static Comparison<T> ByText(Func<T, string> key) =>
        (a, b) => string.Compare(key(a), key(b), StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Class và mảnh vẽ dùng chung cho mọi <see cref="DataTable{T}"/>. Tách khỏi lớp generic để các chuỗi và
/// <see cref="VectorPath"/> chỉ được tạo một lần chứ không phải một lần cho mỗi kiểu <c>T</c>.
/// </summary>
static class DataTableParts
{
    const string Ring = " focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50";

    // Cùng hình với Forms/Checkbox.cs; ở đây chính ô là phần tử focus (không có nhãn) nên dùng được focus-visible:.
    const string BoxBase = "flex size-4 shrink-0 items-center justify-center rounded-[4px] border cursor-pointer transition-colors";
    public const string BoxOff = BoxBase + " border-input dark:bg-input/30" + Ring;
    public const string BoxOn = BoxBase + " border-primary bg-primary text-primary-foreground" + Ring;

    public static readonly string SortButton = Ui.ButtonClasses(ButtonVariant.Ghost, ButtonSize.Sm) + " -mx-2.5";
    public static readonly string PageButton = Ui.ButtonClasses(ButtonVariant.Outline, ButtonSize.Icon);
    public static readonly string ActionButton = Ui.ButtonClasses(ButtonVariant.Ghost, ButtonSize.Icon) + " text-muted-foreground";
    public static readonly string ColumnsTrigger = Ui.ButtonClasses(ButtonVariant.Outline, ButtonSize.Sm);
    public const string SizeTrigger =
        "flex h-8 w-20 items-center justify-between gap-1.5 rounded-lg border border-input bg-transparent pl-2.5 pr-2 text-sm " +
        "whitespace-nowrap cursor-pointer transition-colors hover:bg-muted/50 dark:bg-input/30 dark:hover:bg-input/50";

    static readonly VectorPath First = VectorPath.Icon("m11 17-5-5 5-5", "m18 17-5-5 5-5");
    static readonly VectorPath Last = VectorPath.Icon("m6 17 5-5-5-5", "m13 17 5-5-5-5");
    static readonly VectorPath Columns = VectorPath.Icon("rect 3 3 18 18 2", "M9 3v18", "M15 3v18");

    public static Div ChevronsLeft(Window w) => w.Path(First).Class("size-4 shrink-0");
    public static Div ChevronsRight(Window w) => w.Path(Last).Class("size-4 shrink-0");
    public static Div ColumnsIcon(Window w) => w.Path(Columns).Class("size-4 shrink-0");

    /// <summary>Ô đánh dấu: 0 = trống, 1 = đã chọn, 2 = chọn một phần (gạch ngang, <c>indeterminate</c>).</summary>
    public static Div CheckBox(Window w, int state, string? label = null)
    {
        var box = w.Div().Class(state == 0 ? BoxOff : BoxOn).Focusable().Role(SemanticRole.Checkbox)
            .AriaState(SemanticState.Checked, state == 1).AriaState(SemanticState.Mixed, state == 2).AriaLabel(label);
        if (state == 1) box.Child(Icon.Check(w).Class("size-3.5"));
        else if (state == 2) box.Child(Icon.Minus(w).Class("size-3.5"));
        return box;
    }
}
