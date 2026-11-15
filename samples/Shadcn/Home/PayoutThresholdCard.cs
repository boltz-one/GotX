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
        _currency = App.New(new Select { Items = Currencies, SelectedIndex = 0 });
        _notes = App.New(new Textarea { Placeholder = "Add any notes for this payout configuration..." });
    }

    void Save() => Emit(new Saved(_currency.Value ?? "", _notes.Text));

    public override Div Render(Window w) =>
        HomeUi.Card(w).Key("payout-card")
            .Child(HomeUi.Header(w, hasAction: true)
                .Child(Ui.CardTitle(w, "Payout Threshold"))
                .Child(Ui.CardAction(w).Child(HomeUi.Dismiss(w, "dismiss-payout")))
                .Child(Ui.CardDescription(w, "Set the minimum balance required before a payout is triggered.")))
            .Child(HomeUi.Content(w).Child(HomeUi.FieldGroup(w)
                .Child(HomeUi.Field(w)
                    .Child(Ui.FieldLabel(w, "Preferred Currency"))
                    .Child(w.View(_currency)))
                .Child(HomeUi.Field(w)
                    .Child(w.Div().Class("flex items-center justify-between gap-2")
                        .Child(Ui.FieldLabel(w, "Minimum Payout Amount"))
                        .Child(w.Div().Class("text-2xl font-semibold").Child("$2500.00")))
                    .Child(Ui.Progress(w, 25).Class("h-2").Key("payout-progress"))
                    .Child(w.Div().Class("flex items-center justify-between")
                        .Child(Ui.FieldDescription(w, "$50 (MIN)"))
                        .Child(Ui.FieldDescription(w, "$10,000 (MAX)"))))
                .Child(HomeUi.Field(w)
                    .Child(Ui.FieldLabel(w, "Notes", _notes))
                    .Child(w.Div().Key("payout-notes").Child(w.View(_notes))))))
            .Child(HomeUi.Footer(w)
                .Child(Ui.Button(w, "Save Threshold").Class("w-full").Key("save-threshold").OnClick(this, static (c, _) => c.Save())));
}
