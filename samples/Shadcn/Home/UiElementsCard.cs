using GotX;
using GotX.Components;

/// <summary>
/// Thẻ "UI elements" (ui-elements.tsx): bảng mẫu các control – nút, ô nhập, badge, radio, checkbox, switch, hộp thoại xác
/// nhận và nhóm nút có menu. Các phần chỉ hiện ở breakpoint 4xl của bản gốc (badge Outline, checkbox và switch thứ hai)
/// không có ở đây vì Core chỉ có tới 2xl.
/// </summary>
public sealed class UiElementsCard : View
{
    // Nửa phải của nhóm nút: phần tử bọc của DropdownMenu mới là nút, nên chỗ này chỉ là hình dáng (không tự focus).
    static readonly string ActionsTrigger = Ui.ButtonClasses(ButtonVariant.Outline, ButtonSize.Icon) + " rounded-l-none border-l-0";

    Input _name = null!;
    Textarea _message = null!;
    RadioGroup _fruit = null!;
    Checkbox _alerts = null!;
    Switch _compact = null!;
    AlertDialog _dialog = null!;
    DropdownMenu _actions = null!;
    Window? _window;

    public Input Name => _name;
    public Textarea Message => _message;
    public RadioGroup Fruit => _fruit;
    public Checkbox Alerts => _alerts;
    public Switch Compact => _compact;
    public AlertDialog Dialog => _dialog;
    public DropdownMenu Actions => _actions;

    protected override void OnCreated()
    {
        _name = App.New(new Input { Placeholder = "Name", InGroup = true });
        _message = App.New(new Textarea { Placeholder = "Message" });
        // Hai nút radio không nhãn (bản gốc chỉ có aria-label Apple / Banana).
        _fruit = App.New(new RadioGroup { Items = ["", ""], SelectedIndex = 0, Horizontal = true });
        _alerts = App.New(new Checkbox { Checked = true });
        _compact = App.New(new Switch { Checked = true });
        _dialog = App.New(new AlertDialog
        {
            Title = "Allow accessory to connect?",
            Description = "Do you want to allow the USB accessory to connect to this device and your data?",
            CancelText = "Don't allow", ActionText = "Allow", Size = AlertDialogSize.Sm,
        });
        _actions = App.New(new DropdownMenu
        {
            Placement = Placement.TopEnd,
            Trigger = static w => w.Div().Class(ActionsTrigger).Child(Icon.ChevronUp(w)),
            Items =
            [
                MenuItem.Label("Quick Actions"), MenuItem.Action("Mute Conversation", key: "ui-action-mute"),
                MenuItem.Action("Mark as Read"), MenuItem.Action("Block User"), MenuItem.Separator(),
                MenuItem.Action("Delete Conversation", destructive: true),
            ],
        });
    }

    void OpenDialog() { if (_window is { } w) _dialog.Open(w); }

    public override Div Render(Window w)
    {
        _window = w;
        return HomeUi.Card(w).Key("ui-elements-card").Child(HomeUi.Content(w).Class("flex flex-col gap-6")
            .Child(w.Div().Class("flex gap-2")
                .Child(Ui.Button(w).Key("ui-button").Child("Button").Child(Icon.ArrowRight(w)))
                .Child(Ui.Button(w, "Secondary", ButtonVariant.Secondary).Key("ui-secondary"))
                .Child(Ui.Button(w, "Outline", ButtonVariant.Outline).Key("ui-outline")))
            .Child(HomeUi.FieldGroup(w)
                .Child(HomeUi.Field(w).Child(Ui.InputGroup(w, _name).Key("ui-name")
                    .Child(Ui.InputGroupControl(w, _name))
                    .Child(Ui.InputGroupAddon(w, _name, InputGroupAlign.InlineEnd).Child(Icon.Search(w)))))
                .Child(HomeUi.Field(w).Child(w.Div().Key("ui-message").Child(w.View(_message)))))
            // justify-between thay cho ml-auto của nhóm radio: hai cụm dạt về hai đầu hàng.
            .Child(w.Div().Class("flex items-center justify-between gap-2")
                .Child(w.Div().Class("flex shrink-0 gap-2")
                    .Child(Ui.Badge(w, "Badge"))
                    .Child(Ui.Badge(w, "Secondary", BadgeVariant.Secondary)))
                .Child(w.Div().Class("flex shrink-0 items-center gap-1.5")
                    .Child(w.Div().Class("flex shrink-0").Key("ui-radio").Child(w.View(_fruit)))
                    .Child(w.Div().Class("flex shrink-0").Key("ui-checkbox").Child(w.View(_alerts)))
                    .Child(w.Div().Class("flex shrink-0").Key("ui-switch").Child(w.View(_compact)))))
            .Child(w.Div().Class("flex items-center gap-4")
                .Child(Ui.Button(w, "Alert Dialog", ButtonVariant.Outline).Key("ui-alert-open").OnClick(this, static (c, _) => c.OpenDialog()))
                .Child(w.Div().Class("flex-1")) // thay cho ml-auto của nhóm nút
                .Child(Ui.ButtonGroup(w,
                    Ui.Button(w, "Button Group", ButtonVariant.Outline).Key("ui-group-button"),
                    w.View(_actions)))));
    }
}
