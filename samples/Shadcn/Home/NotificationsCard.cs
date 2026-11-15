using GotX;
using GotX.Components;

/// <summary>
/// Thẻ "Notifications" (notification-settings.tsx): bốn lựa chọn dạng checkbox kèm nhãn và mô tả (bản gốc dùng Checkbox,
/// không phải Switch). Bấm nhãn cũng đổi ô như <c>htmlFor</c>. Save Preferences phát <see cref="Saved"/>.
/// </summary>
public sealed class NotificationsCard : View
{
    /// <summary>Người dùng bấm Save Preferences; <paramref name="Enabled"/> theo thứ tự các dòng.</summary>
    public readonly record struct Saved(bool[] Enabled);

    static readonly (string Id, string Label, string Description, bool On)[] Options =
    [
        ("transactions", "Transaction alerts", "Deposits, withdrawals, and transfers.", true),
        ("security", "Security alerts", "Login attempts and account changes.", true),
        ("goals", "Goal milestones", "Updates at 25%, 50%, 75%, and 100%.", false),
        ("market", "Market updates", "Daily portfolio summary and price alerts.", false),
    ];

    // Key dựng sẵn cho từng dòng (ô và nhãn) để không nối chuỗi mỗi frame.
    static readonly string[] BoxKeys = Array.ConvertAll(Options, o => "notify-" + o.Id);
    static readonly string[] LabelKeys = Array.ConvertAll(Options, o => "notify-label-" + o.Id);

    readonly Checkbox[] _boxes = new Checkbox[Options.Length];

    /// <summary>Ô của dòng thứ <paramref name="index"/> (0 = Transaction alerts).</summary>
    public Checkbox Option(int index) => _boxes[index];

    protected override void OnCreated()
    {
        for (int i = 0; i < _boxes.Length; i++) _boxes[i] = App.New(new Checkbox { Checked = Options[i].On });
    }

    void Save() => Emit(new Saved(Array.ConvertAll(_boxes, b => b.Checked)));

    public override Div Render(Window w)
    {
        var group = HomeUi.FieldGroup(w);
        for (int i = 0; i < Options.Length; i++)
            group.Child(Ui.Field(w, FieldOrientation.Horizontal).Class("items-start gap-3")
                // pt-0.5: ô 16px thẳng hàng với dòng nhãn cao 20px.
                .Child(w.Div().Class("flex pt-0.5").Key(BoxKeys[i]).Child(w.View(_boxes[i])))
                .Child(Ui.FieldContent(w)
                    .Child(Ui.FieldLabel(w, Options[i].Label).Class("cursor-pointer").Key(LabelKeys[i])
                        .OnClick(_boxes[i], static (b, _) => b.Toggle()))
                    .Child(Ui.FieldDescription(w, Options[i].Description))));

        return HomeUi.Card(w).Key("notifications-card")
            .Child(HomeUi.Header(w)
                .Child(Ui.CardTitle(w, "Notifications"))
                .Child(Ui.CardDescription(w, "Choose which email and push alerts you want to receive.")))
            .Child(HomeUi.Content(w).Child(group))
            .Child(HomeUi.Footer(w)
                .Child(Ui.Button(w, "Save Preferences").Class("w-full").Key("save-preferences").OnClick(this, static (c, _) => c.Save())));
    }
}
