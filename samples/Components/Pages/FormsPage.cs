using GotX;
using GotX.Components;

/// <summary>
/// Trang gallery nhóm Forms. Các component có state được tạo một lần trong OnCreated; phần chọn lựa
/// (radio, switch, slider, toggle, select, lịch) nằm ở <c>Forms/FormsPage.Choices.cs</c>.
/// </summary>
public sealed partial class FormsPage : View
{
    Input _name = null!, _email = null!, _filled = null!, _bad = null!, _off = null!, _search = null!, _url = null!, _send = null!;
    Textarea _bio = null!, _badBio = null!, _offBio = null!, _prompt = null!;
    InputOtp _otp = null!, _otpFilled = null!, _otpOff = null!;
    string _otpState = "Chưa nhập";

    protected override void OnCreated()
    {
        _name = App.New(new Input { Placeholder = "Nguyễn Văn A" });
        _email = App.New(new Input { Placeholder = "ban@example.com" });
        _filled = App.New(new Input { Text = "Đã có nội dung" });
        _bad = App.New(new Input { Text = "sai@", Invalid = true });
        _off = App.New(new Input { Placeholder = "Không sửa được", Disabled = true });
        _search = App.New(new Input { Placeholder = "Tìm kiếm…", InGroup = true });
        _url = App.New(new Input { Placeholder = "ten-mien", InGroup = true });
        _send = App.New(new Input { Placeholder = "Nhập tin nhắn", InGroup = true });
        _bio = App.New(new Textarea { Placeholder = "Giới thiệu ngắn về bạn" });
        _badBio = App.New(new Textarea { Text = "Quá ngắn", Invalid = true });
        _offBio = App.New(new Textarea { Text = "Không sửa được", Disabled = true });
        _prompt = App.New(new Textarea { Placeholder = "Hỏi bất cứ điều gì…", InGroup = true });
        _otp = App.New(new InputOtp { Length = 6, GroupSize = 3 });
        _otpFilled = App.New(new InputOtp { Length = 4, Value = "12" });
        _otpOff = App.New(new InputOtp { Length = 4, Value = "0000", Disabled = true });
        App.Subscribe<InputOtp.ValueChanged>(_otp, e => { _otpState = e.Value.Length == 0 ? "Chưa nhập" : "Đang nhập: " + e.Value; Notify(); });
        App.Subscribe<InputOtp.Completed>(_otp, e => { _otpState = "Đủ mã: " + e.Value; Notify(); });
        CreateChoices();
    }

    public override Div Render(Window w) => Demo.Page(w)
        .Child(Inputs(w))
        .Child(Fields(w))
        .Child(InputGroups(w))
        .Child(Otp(w))
        .Child(Choices(w))
        .Child(Toggles(w))
        .Child(Pickers(w));

    Div Inputs(Window w) => Demo.Section(w, "Input · Textarea · Label", "Mặc định, có nội dung, lỗi (Invalid), vô hiệu hoá.")
        .Child(Demo.Row(w)
            .Child(Labeled(w, "Họ tên", _name)).Child(Labeled(w, "Có sẵn", _filled))
            .Child(Labeled(w, "Lỗi", _bad)).Child(Labeled(w, "Vô hiệu hoá", _off)))
        .Child(Demo.Row(w)
            .Child(Labeled(w, "Giới thiệu", _bio)).Child(Labeled(w, "Lỗi", _badBio)).Child(Labeled(w, "Vô hiệu hoá", _offBio)));

    static Div Labeled(Window w, string label, TextEntry entry) =>
        w.Div().Class("flex w-52 flex-col gap-2").Child(Ui.Label(w, label, entry)).Child(w.View(entry));

    Div Fields(Window w) => Demo.Section(w, "Field", "FieldSet, legend, mô tả, lỗi, field ngang, đường phân cách.")
        .Child(w.Div().Class("w-96").Child(Ui.FieldSet(w)
            .Child(Ui.FieldLegend(w, "Thông tin tài khoản"))
            .Child(Ui.FieldDescription(w, "Dùng để đăng nhập và nhận thông báo."))
            .Child(Ui.FieldGroup(w)
                .Child(Ui.Field(w).Child(Ui.FieldLabel(w, "Email", _email)).Child(w.View(_email))
                    .Child(Ui.FieldDescription(w, "Chúng tôi không chia sẻ email của bạn.")))
                .Child(Ui.Field(w, invalid: true).Child(Ui.FieldLabel(w, "Mật khẩu"))
                    .Child(Ui.FieldError(w, ["Ít nhất 8 ký tự", "Phải có một chữ số"])))
                .Child(Ui.FieldSeparator(w, "Hoặc"))
                .Child(Ui.Field(w, FieldOrientation.Horizontal)
                    .Child(Ui.FieldContent(w).Child(Ui.FieldLabel(w, "Nhận bản tin"))
                        .Child(Ui.FieldDescription(w, "Mỗi tuần một email.")))
                    .Child(w.View(_news))))));

    Div InputGroups(Window w) => Demo.Section(w, "Input group", "Phần phụ đầu/cuối: icon, chữ, nút; phần phụ dưới textarea.")
        .Child(Demo.Row(w)
            .Child(w.Div().Class("w-64").Child(Ui.InputGroup(w, _search)
                .Child(Ui.InputGroupAddon(w, _search).Child(Icon.Search(w)))
                .Child(Ui.InputGroupControl(w, _search))
                .Child(Ui.InputGroupAddon(w, _search, InputGroupAlign.InlineEnd).Child(Ui.InputGroupText(w, "12 kết quả")))))
            .Child(w.Div().Class("w-72").Child(Ui.InputGroup(w, _url)
                .Child(Ui.InputGroupAddon(w, _url).Child(Ui.InputGroupText(w, "https://")))
                .Child(Ui.InputGroupControl(w, _url))
                .Child(Ui.InputGroupAddon(w, _url, InputGroupAlign.InlineEnd).Child(Ui.InputGroupText(w, ".com")))))
            .Child(w.Div().Class("w-64").Child(Ui.InputGroup(w, _send)
                .Child(Ui.InputGroupControl(w, _send))
                .Child(Ui.InputGroupAddon(w, InputGroupAlign.InlineEnd)
                    .Child(Ui.InputGroupButton(w, variant: ButtonVariant.Secondary).Child(Icon.Send(w).Class("size-3")))))))
        .Child(w.Div().Class("w-96").Child(Ui.InputGroup(w, _prompt, stacked: true)
            .Child(Ui.InputGroupControl(w, _prompt))
            .Child(Ui.InputGroupAddon(w, _prompt, InputGroupAlign.BlockEnd)
                .Child(Ui.InputGroupButton(w).Child(Icon.Paperclip(w).Class("size-3")))
                .Child(w.Div().Class("flex-1"))
                .Child(Ui.InputGroupText(w, "52% đã dùng")))));

    Div Otp(Window w) => Demo.Section(w, "Input OTP", "6 ô chia hai nhóm; 4 ô đã điền một phần; vô hiệu hoá.")
        .Child(Demo.Row(w).Child(w.View(_otp)).Child(w.Div().Class("text-sm text-muted-foreground").Child(_otpState)))
        .Child(Demo.Row(w).Child(w.View(_otpFilled)).Child(w.View(_otpOff)));
}
