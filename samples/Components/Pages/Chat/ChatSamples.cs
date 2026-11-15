using GotX;
using GotX.Components;

/// <summary>Nội dung mẫu cho trang Chat (đoạn hội thoại, code, các bước trace, câu hỏi) – dùng chung với ChatChecks.</summary>
public static class ChatSamples
{
    public const string Fib =
        "// Dãy Fibonacci bằng vòng lặp\nstatic long Fib(int n)\n{\n    long a = 0, b = 1;\n    for (int i = 0; i < n; i++)\n        (a, b) = (b, a + b);\n    return a;\n}\n";

    public const string Py =
        "def tong(ds):\n    # bỏ qua giá trị None\n    return sum(x for x in ds if x is not None)\n\nprint(tong([1, None, 2]))";

    public const string Log =
        "error CS0103: The name 'fibb' does not exist in the current context\n    at Program.cs(12,17)\nBuild FAILED.";

    public static readonly IReadOnlyList<TraceStep> Steps =
    [
        new("Đọc yêu cầu", TraceStatus.Done, "0.4s"),
        new("Tìm hàm Fib trong mã nguồn", TraceStatus.Done, "1.2s", "Tìm thấy 2 kết quả trong src/Math/Fib.cs"),
        new("Chạy kiểm thử", TraceStatus.Error, "3.1s", "1 kiểm thử thất bại: Fib(50) tràn số"),
        new("Viết câu trả lời", TraceStatus.Done, "2.0s"),
    ];

    public static readonly IReadOnlyList<TraceStep> RunningSteps =
    [
        new("Liệt kê thư mục", TraceStatus.Done, "0.2s"),
        new("Đọc 14 tệp", TraceStatus.Running),
        new("Tổng hợp", TraceStatus.Pending),
    ];

    public static readonly IReadOnlyList<Question> Questions =
    [
        new("Bạn muốn dùng ngôn ngữ nào?", [new("C#", "Ví dụ trong .NET"), new("TypeScript"), new("Python")]),
        new("Cần những phần nào?", [new("Giải thích"), new("Mã nguồn"), new("Kiểm thử")], Multiple: true,
            Description: "Chọn một hoặc nhiều."),
        new("Mức độ chi tiết?", [new("Ngắn gọn"), new("Đầy đủ")]),
    ];

    /// <summary>Hàng nút hành động dưới tin của trợ lý.</summary>
    public static Div Actions(Window w) =>
        Ui.MessageFooter(w)
            .Child(Ui.Button(w, ButtonVariant.Ghost, ButtonSize.IconXs).Child(Icon.Copy(w).Class("size-3")))
            .Child(Ui.Button(w, ButtonVariant.Ghost, ButtonSize.IconXs).Child(Icon.ThumbsUp(w).Class("size-3")))
            .Child(Ui.Button(w, ButtonVariant.Ghost, ButtonSize.IconXs).Child(Icon.ThumbsDown(w).Class("size-3")))
            .Child(Ui.Button(w, ButtonVariant.Ghost, ButtonSize.IconXs).Child(Icon.RotateCcw(w).Class("size-3")));

    public static Func<Window, Div> User(string text) => w =>
        Ui.Message(w, ChatAlign.End, Ui.MessageContent(w, ChatAlign.End).Child(Ui.Bubble(w, text, BubbleVariant.Default, ChatAlign.End)));

    public static Func<Window, Div> Reply(string text) => w =>
        Ui.Message(w, ChatAlign.Start, Ui.MessageContent(w).Child(Ui.Bubble(w, text, BubbleVariant.Ghost)), Ui.MessageAvatar(w, "AI"));

    /// <summary>Đoạn hội thoại mẫu: đủ dài để cuộn được.</summary>
    public static IEnumerable<Func<Window, Div>> Thread(CodeBlock code, Trace trace)
    {
        yield return w => Ui.Marker(w, "Hôm nay", MarkerVariant.Separator);
        yield return User("Viết giúp mình hàm tính số Fibonacci thứ n bằng C#.");
        yield return w => Ui.Message(w, ChatAlign.Start, Ui.MessageContent(w)
                .Child(Ui.MessageHeader(w, ghost: true).Child("Trợ lý"))
                .Child(w.View(trace))
                .Child(Ui.Bubble(w, "Đây là cách làm bằng vòng lặp, chạy O(n) và không đệ quy:", BubbleVariant.Ghost))
                .Child(w.Div().Class("w-full").Child(w.View(code)))
                .Child(Actions(w)),
            Ui.MessageAvatar(w, "AI", hasFooter: true));
        yield return User("Với n = 100 thì sao?");
        yield return Reply("Fib(100) vượt quá long. Hãy dùng System.Numerics.BigInteger cho a và b.");
        yield return User("Cảm ơn nhé!");
        yield return Reply("Không có gì. Cần gì cứ hỏi tiếp nhé.");
    }
}
