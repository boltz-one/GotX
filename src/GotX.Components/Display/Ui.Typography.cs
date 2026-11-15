namespace GotX.Components;

/// <summary>
/// Kiểu chữ của trang "Typography" trong shadcn/ui: bản gốc không phải component mà là các chuỗi class gắn lên thẻ HTML;
/// ở đây mỗi kiểu là một hàm trả về <see cref="Div"/> để người gọi không phải nhớ chuỗi class.
/// Bỏ <c>scroll-m-20</c> (không có neo cuộn theo hash), <c>text-balance</c> (Core chưa cân dòng) và các khoảng cách kiểu
/// <c>[&amp;:not(:first-child)]:mt-6</c> / <c>first:mt-0</c> (không có variant cấu trúc): khoảng cách giữa các đoạn do
/// khung chứa quyết định – dùng <see cref="Prose"/>. Bảng của trang gốc đã có <c>Ui.Table</c>.
/// </summary>
public static partial class Ui
{
    /// <summary>Khung bài viết: các khối xếp dọc cách nhau 24px (thay cho <c>mt-6</c> của từng khối trong bản gốc).</summary>
    public static Div Prose(Window w) => w.Div().Class("flex flex-col gap-6 text-foreground");

    /// <summary>Tiêu đề cấp 1. Bản gốc căn giữa (<c>text-center</c>); muốn căn trái thì nối <c>.Class("text-left")</c>.</summary>
    public static Div H1(Window w, string text) =>
        w.Div().Class("text-center text-4xl font-extrabold tracking-tight").Child(text);

    /// <summary>Tiêu đề cấp 2, có đường kẻ dưới.</summary>
    public static Div H2(Window w, string text) =>
        w.Div().Class("border-b border-border pb-2 text-3xl font-semibold tracking-tight").Child(text);

    /// <summary>Tiêu đề cấp 3.</summary>
    public static Div H3(Window w, string text) => w.Div().Class("text-2xl font-semibold tracking-tight").Child(text);

    /// <summary>Tiêu đề cấp 4.</summary>
    public static Div H4(Window w, string text) => w.Div().Class("text-xl font-semibold tracking-tight").Child(text);

    /// <summary>Đoạn văn, dòng cao 28px.</summary>
    public static Div P(Window w, string text) => w.Div().Class("leading-7").Child(text);

    /// <summary>Đoạn văn rỗng để tự ghép chữ với <see cref="InlineCode"/> (hàng tự xuống dòng theo từng mảnh).</summary>
    public static Div P(Window w) => w.Div().Class("flex flex-wrap items-baseline gap-x-1 leading-7");

    /// <summary>Đoạn dẫn nhập: chữ lớn, màu phụ.</summary>
    public static Div Lead(Window w, string text) => w.Div().Class("text-xl text-muted-foreground").Child(text);

    /// <summary>Chữ lớn đậm (nhấn một câu hỏi, một con số).</summary>
    public static Div Large(Window w, string text) => w.Div().Class("text-lg font-semibold").Child(text);

    /// <summary>Chữ nhỏ đậm vừa (nhãn, chú thích ngắn).</summary>
    public static Div Small(Window w, string text) => w.Div().Class("text-sm leading-none font-medium").Child(text);

    /// <summary>Chữ nhỏ màu phụ.</summary>
    public static Div Muted(Window w, string text) => w.Div().Class("text-sm text-muted-foreground").Child(text);

    /// <summary>Trích dẫn: gạch dọc bên trái, chữ nghiêng.</summary>
    public static Div Blockquote(Window w, string text) =>
        w.Div().Class("border-l-2 border-border pl-6 italic").Child(text);

    /// <summary>Mã trong dòng: nền muted, font mono đậm.</summary>
    public static Div InlineCode(Window w, string text) =>
        w.Div().Class("rounded bg-muted px-[0.3rem] py-[0.2rem] font-mono text-sm font-semibold").Child(text);

    /// <summary>
    /// Danh sách gạch đầu dòng (<c>ml-6 list-disc [&amp;>li]:mt-2</c>). Core không có <c>list-style</c>, nên chấm tròn là
    /// một hộp 6px đặt ngang dòng chữ đầu tiên của mục; mục dài vẫn xuống dòng thẳng lề.
    /// </summary>
    public static Div List(Window w, IReadOnlyList<string> items)
    {
        var list = w.Div().Class("flex flex-col gap-2 pl-2");
        for (int i = 0; i < items.Count; i++)
            list.Child(w.Div().Class("flex items-start gap-2.5 leading-7")
                .Child(w.Div().Class("mt-[11px] size-1.5 shrink-0 rounded-full bg-foreground"))
                .Child(w.Div().Class("min-w-0 flex-1").Child(items[i])));
        return list;
    }
}
