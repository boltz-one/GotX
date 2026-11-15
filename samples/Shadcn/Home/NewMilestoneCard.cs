using GotX;
using GotX.Components;

/// <summary>
/// Thẻ "Set a new milestone" (new-milestone.tsx): ba ô nhập và hai nút. Bản gốc chỉ trưng bày; ở đây nút hoạt động thật:
/// Create Goal phát <see cref="GoalCreated"/> với nội dung các ô, Cancel đưa các ô về giá trị ban đầu.
/// </summary>
public sealed class NewMilestoneCard : View
{
    /// <summary>Người dùng bấm Create Goal.</summary>
    public readonly record struct GoalCreated(string Name, string Amount, string Date);

    const string DefaultAmount = "$15,000", DefaultDate = "Dec 2025";

    Input _name = null!, _amount = null!, _date = null!;

    public Input GoalName => _name;
    public Input TargetAmount => _amount;
    public Input TargetDate => _date;

    protected override void OnCreated()
    {
        using var _ = HomeStyle.Use();
        _name = App.New(new Input { Placeholder = "e.g. New Car, Home Downpayment" });
        _amount = App.New(new Input { Text = DefaultAmount });
        _date = App.New(new Input { Text = DefaultDate });
    }

    void Create() => Emit(new GoalCreated(_name.Text, _amount.Text, _date.Text));

    void Cancel()
    {
        _name.Text = "";
        _amount.Text = DefaultAmount;
        _date.Text = DefaultDate;
    }

    Div Entry(Window w, string label, Input input, string key) =>
        Ui.Field(w)
            .Child(Ui.FieldLabel(w, label, input))
            .Child(w.Div().Key(key).Child(w.View(input)));

    public override Div Render(Window w)
    {
        using var _ = HomeStyle.Use();
        return HomeUi.Card(w).Key("milestone-card")
            .Child(HomeUi.Header(w)
                .Child(Ui.CardTitle(w, "Set a new milestone"))
                .Child(Ui.CardDescription(w, "Define your financial target and we'll help you pace your savings.")))
            .Child(HomeUi.Content(w).Child(Ui.FieldGroup(w)
                .Child(Entry(w, "Goal Name", _name, "goal-name"))
                .Child(w.Div().Class("grid grid-cols-2 gap-3")
                    .Child(Entry(w, "Target Amount", _amount, "target-amount"))
                    .Child(Entry(w, "Target Date", _date, "target-date")))))
            .Child(HomeUi.Footer(w).Class("flex-col gap-2")
                .Child(Ui.Button(w, "Create Goal").Class("w-full").Key("create-goal").OnClick(this, static (c, _) => c.Create()))
                .Child(Ui.Button(w, "Cancel", ButtonVariant.Outline).Class("w-full").Key("cancel-goal").OnClick(this, static (c, _) => c.Cancel())));
    }
}
