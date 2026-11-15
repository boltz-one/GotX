namespace GotX.Components;

/// <summary>Căn nội dung ô theo chiều ngang.</summary>
public enum TableAlign : byte { Left, Center, Right }

/// <summary>
/// Bảng. GotX không có <c>display: table</c>, nên mỗi hàng là một lưới: người gọi nối cùng một
/// <c>.Class("grid-cols-[...]")</c> lên mọi hàng (dùng cột px / fr, không dùng auto) để các cột thẳng hàng.
/// Selector kiểu <c>[&amp;_tr]:border-b</c> không có, nên viền nằm trên từng hàng; hàng cuối thêm <c>.Class("border-b-0")</c>.
/// </summary>
public static partial class Ui
{
    const string RowBase = "grid items-center border-b border-border transition-colors hover:bg-muted/50";
    // data-[state=selected]:bg-muted đứng sau hover:bg-muted/50 trong CSS của Tailwind nên thắng khi rê chuột: hàng được
    // chọn không mang hover (variant hover của GotX vẽ đè lên nền thường).
    const string RowSelected = "grid items-center border-b border-border bg-muted transition-colors";

    static readonly string[] HeadClasses =
    [
        "flex h-10 items-center justify-start px-2 font-medium whitespace-nowrap text-foreground",
        "flex h-10 items-center justify-center px-2 font-medium whitespace-nowrap text-foreground",
        "flex h-10 items-center justify-end px-2 font-medium whitespace-nowrap text-foreground",
    ];

    static readonly string[] CellClasses =
    [
        "flex items-center justify-start p-2 whitespace-nowrap",
        "flex items-center justify-center p-2 whitespace-nowrap",
        "flex items-center justify-end p-2 whitespace-nowrap",
    ];

    /// <summary>Khung bảng: các phần (header/body/footer/caption) xếp dọc, chữ cỡ sm.</summary>
    public static Div Table(Window w) => w.Div().Class("flex w-full flex-col text-sm");

    /// <summary>Nhóm hàng tiêu đề.</summary>
    public static Div TableHeader(Window w) => w.Div().Class("flex flex-col");

    /// <summary>Nhóm hàng dữ liệu.</summary>
    public static Div TableBody(Window w) => w.Div().Class("flex flex-col");

    /// <summary>Nhóm hàng tổng kết: nền muted, viền trên, chữ đậm vừa.</summary>
    public static Div TableFooter(Window w) => w.Div().Class("flex flex-col border-t border-border bg-muted/50 font-medium");

    /// <summary>Một hàng (lưới); <paramref name="selected"/> tô nền muted như <c>data-state=selected</c>.</summary>
    public static Div TableRow(Window w, bool selected = false) =>
        w.Div().Class(selected ? RowSelected : RowBase);

    /// <summary>Ô tiêu đề cột.</summary>
    public static Div TableHead(Window w, string text, TableAlign align = TableAlign.Left) =>
        w.Div().Class(HeadClasses[(int)align]).Child(text);

    /// <summary>Ô dữ liệu rỗng – tự thêm nội dung (badge, nút…).</summary>
    public static Div TableCell(Window w, TableAlign align = TableAlign.Left) => w.Div().Class(CellClasses[(int)align]);

    /// <summary>Ô dữ liệu chữ.</summary>
    public static Div TableCell(Window w, string text, TableAlign align = TableAlign.Left) => TableCell(w, align).Child(text);

    /// <summary>Chú thích dưới bảng.</summary>
    public static Div TableCaption(Window w, string text) =>
        w.Div().Class("mt-4 text-center text-sm text-muted-foreground").Child(text);
}
