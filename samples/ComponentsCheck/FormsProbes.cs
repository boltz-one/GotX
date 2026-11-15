using GotX;
using GotX.Components;

/// <summary>View thử cho các control bấm/kéo/gõ của nhóm Forms; mỗi control nằm trong một hộp có Key để tìm toạ độ.</summary>
sealed class FormsProbe : View
{
    public Switch Wifi = null!, Locked = null!;
    public RadioGroup Plan = null!, PlanOff = null!;
    public Slider Volume = null!, Stepped = null!, SliderOff = null!;
    public Toggle Bold = null!, ToggleOff = null!;
    public ToggleGroup Single = null!, Multi = null!;
    public Input Name = null!, NameOff = null!, Query = null!;
    public Checkbox Agree = null!;
    public int CheckEvents, GroupClicks;
    public Textarea Bio = null!;
    public InputOtp Otp = null!;
    public int SwitchEvents, LockedEvents, RadioEvents, SliderEvents, Commits, ToggleEvents, SingleEvents, TextEvents, OtpCompleted;
    public string Submitted = "", LastRadio = "";

    protected override void OnCreated()
    {
        Wifi = App.New(new Switch { Label = "Wi-Fi" });
        Locked = App.New(new Switch { Label = "Khoá", Disabled = true });
        Plan = App.New(new RadioGroup { Items = ["Một", "Hai", "Ba"] });
        PlanOff = App.New(new RadioGroup { Items = ["X", "Y"], SelectedIndex = 0, Disabled = true });
        Volume = App.New(new Slider { Min = 0, Max = 100, Step = 5 });
        Stepped = App.New(new Slider { Min = 0, Max = 100, Step = 10 });
        SliderOff = App.New(new Slider { Value = 20, Disabled = true });
        Bold = App.New(new Toggle { Label = "B" });
        ToggleOff = App.New(new Toggle { Label = "K", Disabled = true });
        Single = App.New(new ToggleGroup { Items = ["A", "B", "C"], Variant = ToggleVariant.Outline });
        Multi = App.New(new ToggleGroup { Items = ["A", "B", "C"], Multiple = true });
        Name = App.New(new Input { Placeholder = "Tên" });
        NameOff = App.New(new Input { Text = "cố định", Disabled = true });
        Bio = App.New(new Textarea());
        Otp = App.New(new InputOtp { Length = 6, GroupSize = 3 });
        Query = App.New(new Input { Placeholder = "Find", InGroup = true });
        Agree = App.New(new Checkbox { Label = "OK" });
        App.Subscribe<Checkbox.CheckedChanged>(Agree, _ => CheckEvents++);

        App.Subscribe<Switch.CheckedChanged>(Wifi, _ => SwitchEvents++);
        App.Subscribe<Switch.CheckedChanged>(Locked, _ => LockedEvents++);
        App.Subscribe<RadioGroup.ValueChanged>(Plan, e => { RadioEvents++; LastRadio = e.Value; });
        App.Subscribe<Slider.ValueChanged>(Volume, _ => SliderEvents++);
        App.Subscribe<Slider.ValueCommitted>(Volume, _ => Commits++);
        App.Subscribe<Toggle.PressedChanged>(Bold, _ => ToggleEvents++);
        App.Subscribe<ToggleGroup.ValueChanged>(Single, _ => SingleEvents++);
        App.Subscribe<TextEntry.TextChanged>(Name, _ => TextEvents++);
        App.Subscribe<TextEntry.Submitted>(Name, e => Submitted = e.Text);
        App.Subscribe<InputOtp.Completed>(Otp, _ => OtpCompleted++);
    }

    public override Div Render(Window w) =>
        w.Div().Class("size-full grid grid-cols-2 gap-6 p-6 bg-background text-foreground")
            .Child(w.Div().Class("flex flex-col items-start gap-5")
                .Child(w.Div().Key("f-switch").Child(w.View(Wifi)))
                .Child(w.Div().Key("f-switch-off").Child(w.View(Locked)))
                .Child(w.Div().Key("f-radio").Child(w.View(Plan)))
                .Child(w.Div().Key("f-radio-off").Child(w.View(PlanOff)))
                .Child(w.Div().Class("w-[200px]").Key("f-slider").Child(w.View(Volume)))
                .Child(w.Div().Class("w-[200px]").Key("f-slider-step").Child(w.View(Stepped)))
                .Child(w.Div().Class("w-[200px]").Key("f-slider-off").Child(w.View(SliderOff)))
                .Child(w.Div().Class("flex gap-2")
                    .Child(w.Div().Key("f-toggle").Child(w.View(Bold)))
                    .Child(w.Div().Key("f-toggle-off").Child(w.View(ToggleOff))))
                .Child(w.Div().Key("f-check").Child(w.View(Agree))))
            .Child(w.Div().Class("flex flex-col items-start gap-5")
                .Child(w.Div().Key("f-single").Child(w.View(Single)))
                .Child(w.Div().Key("f-multi").Child(w.View(Multi)))
                .Child(w.Div().Class("w-60").Key("f-input").Child(w.View(Name)))
                .Child(w.Div().Class("w-60").Key("f-input-off").Child(w.View(NameOff)))
                .Child(w.Div().Class("w-60").Key("f-textarea").Child(w.View(Bio)))
                .Child(w.Div().Key("f-otp").Child(w.View(Otp)))
                .Child(Ui.Label(w, "Name", Name).Key("f-label"))
                .Child(w.Div().Class("w-60").Child(Ui.InputGroup(w, Query).Key("f-group")
                    .Child(Ui.InputGroupAddon(w, Query).Key("f-addon").Child(Icon.Search(w)))
                    .Child(Ui.InputGroupControl(w, Query))
                    .Child(Ui.InputGroupAddon(w, InputGroupAlign.InlineEnd)
                        .Child(Ui.InputGroupButton(w, "Go").Key("f-group-go").OnClick(this, static (p, _) => p.GroupClicks++))))));
}

/// <summary>View thử cho lịch, native select và progress.</summary>
sealed class FormsPickerProbe : View
{
    public Calendar Calendar = null!;
    public NativeSelect Fruit = null!;
    public int MonthEvents, DayEvents, SelectEvents;

    protected override void OnCreated()
    {
        Calendar = App.New(new Calendar { Today = new DateOnly(2026, 11, 15), Month = new DateOnly(2026, 11, 1) });
        Fruit = App.New(new NativeSelect { Items = ["Táo", "Cam", "Xoài"] });
        App.Subscribe<Calendar.MonthChanged>(Calendar, _ => MonthEvents++);
        App.Subscribe<Calendar.SelectedChanged>(Calendar, _ => DayEvents++);
        App.Subscribe<NativeSelect.ValueChanged>(Fruit, _ => SelectEvents++);
    }

    public override Div Render(Window w) =>
        w.Div().Class("size-full flex flex-col items-start gap-6 p-6 bg-background text-foreground")
            .Child(w.Div().Key("f-calendar").Child(w.View(Calendar)))
            .Child(w.Div().Key("f-native").Child(w.View(Fruit)))
            .Child(w.Div().Class("w-[200px]").Key("f-progress").Child(Ui.Progress(w, 50)));
}
