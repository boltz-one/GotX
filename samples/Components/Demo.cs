using GotX;

/// <summary>Khung trình bày dùng chung cho các trang gallery.</summary>
public static class Demo
{
    /// <summary>Một mục của trang: tiêu đề + mô tả, nội dung thêm bằng <c>.Child(...)</c>.</summary>
    public static Div Section(Window w, string title, string description = "")
    {
        var section = w.Div().Class("flex flex-col gap-3 p-4 rounded-xl border border-border bg-card text-card-foreground");
        var header = w.Div().Class("flex flex-col gap-1").Child(w.Div().Class("text-sm font-semibold").Child(title));
        if (description.Length > 0) header.Child(w.Div().Class("text-xs text-muted-foreground").Child(description));
        return section.Child(header);
    }

    /// <summary>Hàng các phần tử mẫu, tự xuống dòng.</summary>
    public static Div Row(Window w) => w.Div().Class("flex flex-wrap items-center gap-3");

    /// <summary>Khung trang: các mục xếp dọc.</summary>
    public static Div Page(Window w) => w.Div().Class("flex flex-col gap-4 p-6 bg-background text-foreground");
}