using GotX;
using GotX.Components;

/// <summary>
/// Thẻ "Payout Threshold" (payout-threshold.tsx): chọn tiền tệ, thanh tiến độ mức chi tối thiểu (bản gốc là Progress
/// tĩnh ở 25%, không phải slider), ghi chú và nút lưu. Save Threshold phát <see cref="Saved"/>.
/// </summary>
public sealed class PayoutThresholdCard : View
{
    /// <summary>Người dùng bấm Save Threshold.</summary>
    public readonly record struct Saved(string Currency, string Notes);

    static readonly string[] Currencies =
        ["USD — United States Dollar", "EUR — Euro", "GBP — British Pound", "JPY — Japanese Yen"];

    Select _currency = null!;
    Textarea _notes = null!;

    public Select Currency => _currency;
    public Textarea Notes => _notes;

    protected override void OnCreated()
    {
        using var _ = HomeStyle.Use();
        _currency = App.New(new Select { Items = Currencies, SelectedIndex = 0, TriggerClass = "w-full" });
        _notes = App.New(new Textarea { Placeholder = "Add any notes for this payout configuration...", MinBoxHeight = 100 });
    }

    void Save() => Emit(new Saved(_currency.Value ?? "", _notes.Text));

    public override Div Render(Window w)
    {
        using var _ = HomeStyle.Use();
        return HomeUi.Card(w).Key("payout-card")
            .Child(HomeUi.Header(w, hasAction: true)
                .Child(Ui.CardTitle(w, "Payout Threshold"))
                .Child(Ui.CardAction(w).Child(HomeCards.Dismiss(w, "dismiss-payout")))
                .Child(Ui.CardDescription(w, "Set the minimum balance required before a payout is triggered.")))
            .Child(HomeUi.Content(w).Child(Ui.FieldGroup(w)
                .Child(Ui.Field(w)
                    .Child(Ui.FieldLabel(w, "Preferred Currency"))
                    .Child(w.View(_currency)))
                .Child(Ui.Field(w)
                    .Child(w.Div().Class("flex items-baseline justify-between")
                        .Child(Ui.FieldLabel(w, "Minimum Payout Amount"))
                        .Child(w.Div().Class("text-2xl font-semibold tabular-nums").Child("$2500.00")))
                    .Child(Ui.Progress(w, 25).Key("payout-progress"))
                    .Child(w.Div().Class("flex items-center justify-between")
                        .Child(Ui.FieldDescription(w, "$50 (MIN)"))
                        .Child(Ui.FieldDescription(w, "$10,000 (MAX)"))))
                .Child(Ui.Field(w)
                    .Child(Ui.FieldLabel(w, "Notes", _notes))
                    .Child(w.Div().Key("payout-notes").Child(w.View(_notes))))))
            .Child(HomeUi.Footer(w)
                .Child(Ui.Button(w, "Save Threshold").Class("w-full").Key("save-threshold").OnClick(this, static (c, _) => c.Save())));
    }
}