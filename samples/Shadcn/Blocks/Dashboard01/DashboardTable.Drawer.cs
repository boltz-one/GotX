using GotX;
using GotX.Components;

/// <summary>
/// Drawer chi tiết của một hàng (TableCellViewer, <c>direction="right"</c> trên desktop): tiêu đề + mô tả, dòng xu hướng,
/// đoạn mô tả, form Header / Type / Status / Target / Limit / Reviewer, chân drawer Submit + Done. Control của form tạo
/// một lần và nạp lại giá trị mỗi lần mở (defaultValue của bản gốc).
/// </summary>
public sealed partial class DashboardTable
{
    static readonly string[] Types =
        ["Table of Contents", "Executive Summary", "Technical Approach", "Design", "Capabilities", "Focus Documents", "Narrative", "Cover Page"];
    static readonly string[] Statuses = ["Done", "In Progress", "Not Started"];
    static readonly string[] AllReviewers = ["Eddie Lake", "Jamik Tashpulatov", "Emily Whalen"];

    Input _formHeader = null!, _formTarget = null!, _formLimit = null!;
    Select _formType = null!, _formStatus = null!, _formReviewer = null!;

    public const string DoneKey = "drawer-done", SubmitKey = "drawer-submit";

    void CreateDrawerForm()
    {
        _formHeader = FormInput();
        _formTarget = FormInput();
        _formLimit = FormInput();
        _formType = App.New(new Select { Items = Types, Placeholder = "Select a type", TriggerClass = "w-full" });
        _formStatus = App.New(new Select { Items = Statuses, Placeholder = "Select a status", TriggerClass = "w-full" });
        _formReviewer = App.New(new Select { Items = AllReviewers, Placeholder = "Select a reviewer", TriggerClass = "w-full" });
        App.Subscribe<SidePanel.OpenChanged>(_drawer, e => { if (e.Open) LoadForm(); });
    }

    Input FormInput()
    {
        return App.New(new Input());
    }

    /// <summary>defaultValue / Select defaultValue của hàng đang xem; giá trị không có trong danh sách → placeholder.</summary>
    void LoadForm()
    {
        if (_viewing is not { } s) return;
        _formHeader.Text = s.Header;
        _formTarget.Text = s.Target;
        _formLimit.Text = s.Limit;
        _formType.SelectedIndex = Array.IndexOf(Types, s.Type);
        _formStatus.SelectedIndex = Array.IndexOf(Statuses, s.Status);
        _formReviewer.SelectedIndex = Array.IndexOf(AllReviewers, s.Reviewer);
    }

    static Div Labeled(Window w, string label, Div control) =>
        w.Div().Class("flex flex-col gap-3").Child(w.Div().Class("flex items-center gap-2 text-sm leading-none font-medium").Child(label)).Child(control);

    Div DrawerBody(Window w)
    {
        var body = w.Div().Class("flex flex-col gap-4 overflow-y-auto px-4 text-sm");
        if (_viewing is null) return body;
        return body
            .Child(w.Div().Class("h-px w-full shrink-0 bg-border"))
            .Child(w.Div().Class("grid gap-2")
                .Child(w.Div().Class("flex gap-2 leading-none font-medium").Child("Trending up by 5.2% this month").Child(DashboardIcons.TrendingUp(w)))
                .Child(w.Div().Class("text-muted-foreground").Child(
                    "Showing total visitors for the last 6 months. This is just some random text to test the layout. It spans multiple lines and should wrap around.")))
            .Child(w.Div().Class("h-px w-full shrink-0 bg-border"))
            .Child(w.Div().Class("flex flex-col gap-4")
                .Child(Labeled(w, "Header", w.View(_formHeader)))
                .Child(w.Div().Class("grid grid-cols-2 gap-4")
                    .Child(Labeled(w, "Type", w.View(_formType).Class("grid w-full")))
                    .Child(Labeled(w, "Status", w.View(_formStatus).Class("grid w-full"))))
                .Child(w.Div().Class("grid grid-cols-2 gap-4")
                    .Child(Labeled(w, "Target", w.View(_formTarget)))
                    .Child(Labeled(w, "Limit", w.View(_formLimit))))
                .Child(Labeled(w, "Reviewer", w.View(_formReviewer).Class("grid w-full"))));
    }

    Div DrawerFooter(Window w) =>
        w.Div().Class("flex flex-col gap-2")
            // Submit nằm ngoài <form> của bản gốc nên bấm không làm gì.
            .Child(Ui.Button(w).Key(SubmitKey).Child("Submit"))
            .Child(Ui.Button(w, ButtonVariant.Outline).Key(DoneKey).OnClick(this, static (t, _) => t._drawer.Close()).Child("Done"));
}
