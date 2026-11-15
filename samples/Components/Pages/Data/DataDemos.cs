using System.Globalization;
using GotX;
using GotX.Components;

/// <summary>Một khoản thanh toán – dữ liệu mẫu của ví dụ Data Table trong tài liệu shadcn/ui.</summary>
public sealed record DemoPayment(string Id, int Amount, string Status, string Email)
{
    /// <summary>Số tiền đã định dạng, tính một lần để ô không định dạng lại mỗi lần vẽ.</summary>
    public string AmountText { get; } = "$" + Amount.ToString("N2", CultureInfo.InvariantCulture);
}

/// <summary>Dữ liệu và các mục tĩnh của trang gallery "Dữ liệu" (Typography, Date Picker, Data Table).</summary>
public static class DataDemos
{
    // Năm hàng đầu là dữ liệu của tài liệu gốc; phần còn lại sinh theo công thức cố định để có nhiều trang mà ảnh chụp không đổi.
    public static readonly DemoPayment[] Payments = BuildPayments();

    static DemoPayment[] BuildPayments()
    {
        string[] status = ["pending", "processing", "success", "failed"];
        string[] names = ["olivia", "liam", "emma", "noah", "ava", "mason", "sophia", "lucas", "mia", "ethan", "isla", "jack", "nora", "owen", "zoe", "ryan", "lily", "adam"];
        var list = new List<DemoPayment>
        {
            new("m5gr84i9", 316, "success", "ken99@example.com"), new("3u1reuv4", 242, "success", "Abe45@example.com"),
            new("derv1ws0", 837, "processing", "Monserrat44@example.com"), new("5kma53ae", 874, "success", "Silas22@example.com"),
            new("bhqecj4p", 721, "failed", "carmella@example.com"),
        };
        for (int i = 0; i < names.Length; i++)
            list.Add(new($"p{(i * 7919 + 1231) % 100000:00000}", 95 + (i * 137) % 860, status[i % 4], names[i] + "@example.com"));
        return [.. list];
    }

    public static IReadOnlyList<DataColumn<DemoPayment>> PaymentColumns() =>
    [
        new("status", "Status") { Width = "140px", Cell = static (w, p) => w.Div().Class("capitalize").Child(p.Status), Text = static p => p.Status },
        new("email", "Email") { Width = "2fr", Text = static p => p.Email, Compare = DataColumn<DemoPayment>.ByText(static p => p.Email), CanHide = false },
        new("id", "Payment ID") { Text = static p => p.Id, InitiallyHidden = true },
        new("amount", "Amount")
        {
            Width = "140px", Align = TableAlign.Right, Compare = DataColumn<DemoPayment>.By(static p => p.Amount),
            Cell = static (w, p) => w.Div().Class("font-medium").Child(p.AmountText),
        },
    ];

    public static readonly MenuItem[] PaymentActions =
    [
        MenuItem.Label("Actions"), MenuItem.Action("Copy payment ID", key: "d-action-copy"), MenuItem.Separator(),
        MenuItem.Action("View customer"), MenuItem.Action("View payment details"),
    ];

    /// <summary>Mục Typography: nội dung của trang Typography trong tài liệu shadcn/ui, rút gọn.</summary>
    public static Div Typography(Window w) => Demo.Section(w, "Typography", "H1–H4, P, Lead, Large, Small, Muted, Blockquote, InlineCode, List.")
        .Child(Ui.Prose(w).Key("d-prose")
            .Child(Ui.H1(w, "Taxing Laughter: The Joke Tax Chronicles").Key("d-h1"))
            .Child(Ui.Lead(w, "Once upon a time, in a far-off land, there was a very lazy king who spent all day lounging on his throne."))
            .Child(Ui.H2(w, "The King's Plan").Key("d-h2"))
            .Child(Ui.P(w, "The king thought long and hard, and finally came up with a brilliant plan: he would tax the jokes in the kingdom."))
            .Child(Ui.Blockquote(w, "\"After all,\" he said, \"everyone enjoys a good joke, so it's only fair that they should pay for the privilege.\"").Key("d-quote"))
            .Child(Ui.H3(w, "The Joke Tax"))
            .Child(Ui.P(w, "The king's subjects were not amused. They grumbled and complained, but the king was firm:"))
            .Child(Ui.List(w, ["1st level of puns: 5 gold coins", "2nd level of jokes: 10 gold coins", "3rd level of one-liners: 20 gold coins"]).Key("d-list"))
            .Child(Ui.H4(w, "People stopped telling jokes"))
            .Child(Ui.P(w).Child("Install it with").Child(Ui.InlineCode(w, "@radix-ui/react-alert-dialog").Key("d-code")).Child("and you are done."))
            .Child(w.Div().Class("flex flex-wrap items-center gap-6")
                .Child(Ui.Large(w, "Are you absolutely sure?"))
                .Child(Ui.Small(w, "Email address"))
                .Child(Ui.Muted(w, "Enter your email address."))));
}
