using GotX;
using GotX.Components;

/// <summary>Phần chọn lựa của trang Forms: radio, switch, slider, progress, toggle, native select, lịch.</summary>
public sealed partial class FormsPage
{
    Switch _news = null!, _wifi = null!, _airplane = null!, _small = null!, _locked = null!;
    RadioGroup _plan = null!, _size = null!, _radioOff = null!;
    Slider _volume = null!, _stepped = null!, _sliderOff = null!;
    Toggle _bold = null!, _italic = null!, _outline = null!, _sm = null!, _lg = null!, _toggleOff = null!;
    ToggleGroup _align = null!, _format = null!, _small3 = null!;
    NativeSelect _country = null!, _countryOff = null!;
    Calendar _calendar = null!;
    string _volumeText = "Âm lượng: 50", _steppedText = "Bước 10: 30", _dateText = "Chưa chọn ngày";

    void CreateChoices()
    {
        _news = App.New(new Switch { Checked = true });
        _wifi = App.New(new Switch { Label = "Wi-Fi", Checked = true });
        _airplane = App.New(new Switch { Label = "Chế độ máy bay" });
        _small = App.New(new Switch { Label = "Cỡ nhỏ", Size = SwitchSize.Sm, Checked = true });
        _locked = App.New(new Switch { Label = "Vô hiệu hoá", Disabled = true, Checked = true });

        _plan = App.New(new RadioGroup { Items = ["Miễn phí", "Chuyên nghiệp", "Doanh nghiệp"], SelectedIndex = 1 });
        _size = App.New(new RadioGroup { Items = ["Nhỏ", "Vừa", "Lớn"], SelectedIndex = 0, Horizontal = true });
        _radioOff = App.New(new RadioGroup { Items = ["Khoá A", "Khoá B"], SelectedIndex = 0, Disabled = true });

        _volume = App.New(new Slider { Value = 50 });
        _stepped = App.New(new Slider { Step = 10, Value = 30 });
        _sliderOff = App.New(new Slider { Value = 70, Disabled = true });
        App.Subscribe<Slider.ValueChanged>(_volume, e => { _volumeText = $"Âm lượng: {e.Value:0}"; Notify(); });
        App.Subscribe<Slider.ValueChanged>(_stepped, e => { _steppedText = $"Bước 10: {e.Value:0}"; Notify(); });

        _bold = App.New(new Toggle { Content = static w => Icon.Bold(w), Pressed = true });
        _italic = App.New(new Toggle { Content = static w => Icon.Italic(w), Label = "Nghiêng" });
        _outline = App.New(new Toggle { Label = "Viền", Variant = ToggleVariant.Outline, Content = static w => Icon.Star(w) });
        _sm = App.New(new Toggle { Label = "Nhỏ", Size = ToggleSize.Sm, Variant = ToggleVariant.Outline, Pressed = true });
        _lg = App.New(new Toggle { Label = "Lớn", Size = ToggleSize.Lg, Variant = ToggleVariant.Outline });
        _toggleOff = App.New(new Toggle { Label = "Khoá", Disabled = true });

        _align = App.New(new ToggleGroup { Items = ["Trái", "Giữa", "Phải"], Variant = ToggleVariant.Outline });
        _align.SetPressed(0, true);
        _format = App.New(new ToggleGroup { Items = ["Đậm", "Nghiêng", "Gạch"], Multiple = true });
        _format.SetPressed(0, true);
        _format.SetPressed(2, true);
        _small3 = App.New(new ToggleGroup { Items = ["N", "T", "T"], Variant = ToggleVariant.Outline, Size = ToggleSize.Sm, Disabled = true });

        _country = App.New(new NativeSelect { Items = ["Việt Nam", "Nhật Bản", "Hàn Quốc", "Singapore"], Placeholder = "Chọn quốc gia" });
        _countryOff = App.New(new NativeSelect { Items = ["Việt Nam"], SelectedIndex = 0, Disabled = true });

        _calendar = App.New(new Calendar());
        _calendar.Selected = _calendar.Today;
        _dateText = "Đã chọn: " + _calendar.Today.ToString("dd/MM/yyyy");
        App.Subscribe<Calendar.SelectedChanged>(_calendar, e => { _dateText = "Đã chọn: " + e.Date.ToString("dd/MM/yyyy"); Notify(); });
    }

    Div Choices(Window w) => Demo.Section(w, "Radio group · Switch · Slider · Progress", "Chọn một, bật/tắt, kéo giá trị, tiến độ.")
        .Child(Demo.Row(w).Class("items-start gap-10")
            .Child(w.View(_plan)).Child(w.View(_size)).Child(w.View(_radioOff)))
        .Child(Demo.Row(w).Class("gap-6")
            .Child(w.View(_wifi)).Child(w.View(_airplane)).Child(w.View(_small)).Child(w.View(_locked)))
        .Child(w.Div().Class("grid grid-cols-3 gap-8")
            .Child(Captioned(w, _volumeText, w.View(_volume)))
            .Child(Captioned(w, _steppedText, w.View(_stepped)))
            .Child(Captioned(w, "Vô hiệu hoá", w.View(_sliderOff))))
        .Child(w.Div().Class("grid grid-cols-3 gap-8")
            .Child(Ui.Progress(w, 15))
            .Child(Ui.Progress(w, 66))
            .Child(Ui.Progress(w, 40, "Đang tải lên", "40%")));

    static Div Captioned(Window w, string caption, Div content) =>
        w.Div().Class("flex flex-col gap-3").Child(w.Div().Class("text-sm text-muted-foreground").Child(caption)).Child(content);

    Div Toggles(Window w) => Demo.Section(w, "Toggle · Toggle group", "Default/Outline, sm/default/lg, đang nhấn, vô hiệu hoá; nhóm chọn một và chọn nhiều.")
        .Child(Demo.Row(w)
            .Child(w.View(_bold)).Child(w.View(_italic)).Child(w.View(_outline))
            .Child(w.View(_sm)).Child(w.View(_lg)).Child(w.View(_toggleOff)))
        .Child(Demo.Row(w).Class("gap-6")
            .Child(Captioned(w, "Một lựa chọn (outline)", w.View(_align)))
            .Child(Captioned(w, "Nhiều lựa chọn", w.View(_format)))
            .Child(Captioned(w, "Nhỏ, vô hiệu hoá", w.View(_small3))));

    Div Pickers(Window w) => Demo.Section(w, "Native select · Calendar", "Hộp chọn dựng trên Select; lịch tháng, hôm nay tô nền, ngày chọn tô đậm.")
        .Child(Demo.Row(w).Class("items-start gap-10")
            .Child(w.Div().Class("flex flex-col gap-3")
                .Child(w.View(_country)).Child(w.View(_countryOff)))
            .Child(w.Div().Class("flex flex-col gap-3")
                .Child(w.Div().Class("rounded-lg border border-border").Child(w.View(_calendar)))
                .Child(w.Div().Class("text-sm text-muted-foreground").Child(_dateText))));
}
