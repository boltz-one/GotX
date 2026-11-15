using GotX;
using GotX.Components;

/// <summary>Kiểm tra hành vi các component nhóm Forms: bấm, kéo, gõ thật qua CheckHarness rồi đo state/event/pixel.</summary>
static class FormsChecks
{
    public static void Run(CheckHarness h)
    {
        FormsProbe p = null!;
        var w = h.Open(app => p = app.New(new FormsProbe()));
        Switches(h, w, p);
        Radios(h, w, p);
        Sliders(h, w, p);
        Toggles(h, w, p);
        TextEntries(h, w, p);
        FormsPickerChecks.Run(h);
        FormsKeyboardChecks.Run(h);
        FormsCalendarKeyChecks.Run(h);
    }

    static void Switches(CheckHarness h, Window w, FormsProbe p)
    {
        h.Click(w, "f-switch");
        h.Check("switch: bấm thì bật và phát CheckedChanged", p.Wifi.Checked && p.SwitchEvents == 1);
        h.Click(w, "f-switch");
        h.Check("switch: bấm lần nữa thì tắt", !p.Wifi.Checked && p.SwitchEvents == 2);
        h.Click(w, "f-switch-off");
        h.Check("switch: bị vô hiệu hoá thì bỏ qua click", !p.Locked.Checked && p.LockedEvents == 0);
    }

    // Mỗi mục radio cao 16px (vòng size-4), cách nhau gap-3 (12px).
    static Point RadioRing(Bounds group, int i) => new(group.X + 8, group.Y + 8 + i * 28);

    static void Radios(CheckHarness h, Window w, FormsProbe p)
    {
        var g = h.Box(w, "f-radio");
        h.Click(w, RadioRing(g, 2));
        h.Check($"radio: bấm mục thứ ba thì chọn nó ({p.LastRadio})", p.Plan.SelectedIndex == 2 && p.LastRadio == "Ba" && p.RadioEvents == 1);
        h.Click(w, RadioRing(g, 0));
        // Điểm trong vòng nhưng ngoài chấm giữa: mục chọn có nền primary (tối ở chế độ sáng), mục còn lại nền trắng.
        var on = h.Pixel(w, RadioRing(g, 0) with { X = g.X + 13 });
        var off = h.Pixel(w, RadioRing(g, 2) with { X = g.X + 13 });
        h.Check($"radio: chọn mục đầu thì mục ba bỏ chọn (pixel {on} / {off})",
            p.Plan.SelectedIndex == 0 && p.RadioEvents == 2 && on.R < 80 && off.R > 200);
        h.Click(w, RadioRing(h.Box(w, "f-radio-off"), 1));
        h.Check("radio: nhóm vô hiệu hoá bỏ qua click", p.PlanOff.SelectedIndex == 0);
    }

    static void Sliders(CheckHarness h, Window w, FormsProbe p)
    {
        var s = h.Box(w, "f-slider");
        float y = s.Y + s.Height / 2;
        h.Drag(w, new Point(s.X + s.Width * 0.2f, y), new Point(s.X + s.Width * 0.75f, y));
        h.Check($"slider: kéo tới 75% (bước 5) được 75 ({p.Volume.Value})", p.Volume.Value == 75 && p.SliderEvents > 1 && p.Commits == 1);
        h.Drag(w, new Point(s.X + s.Width * 0.75f, y), new Point(s.X + s.Width * 0.62f, y));
        h.Check($"slider: 62% làm tròn theo bước 5 thành 60 ({p.Volume.Value})", p.Volume.Value == 60);
        var t = h.Box(w, "f-slider-step");
        h.Click(w, new Point(t.X + t.Width * 0.73f, t.Y + t.Height / 2));
        h.Check($"slider: nhấn tại 73% (bước 10) được 70 ({p.Stepped.Value})", p.Stepped.Value == 70);
        h.Drag(w, new Point(t.X + 2, t.Y + t.Height / 2), new Point(t.Right + 80, t.Y + t.Height / 2));
        h.Check($"slider: kéo quá mép phải kẹp về Max ({p.Stepped.Value})", p.Stepped.Value == 100);
        var o = h.Box(w, "f-slider-off");
        h.Drag(w, new Point(o.X + 10, o.Y + o.Height / 2), new Point(o.Right - 10, o.Y + o.Height / 2));
        h.Check("slider: vô hiệu hoá thì kéo không đổi", p.SliderOff.Value == 20);
    }

    static Point Item(Bounds group, int i, int n) => new(group.X + group.Width * (i + 0.5f) / n, group.Y + group.Height / 2);

    static void Toggles(CheckHarness h, Window w, FormsProbe p)
    {
        h.Click(w, "f-toggle");
        h.Check("toggle: bấm thì nhấn và phát PressedChanged", p.Bold.Pressed && p.ToggleEvents == 1);
        h.Click(w, "f-toggle-off");
        h.Check("toggle: vô hiệu hoá bỏ qua click", !p.ToggleOff.Pressed);

        var g = h.Box(w, "f-single");
        h.Click(w, Item(g, 1, 3));
        h.Click(w, Item(g, 2, 3));
        h.Check("toggle-group một lựa chọn: bật C thì B tắt", !p.Single.IsPressed(1) && p.Single.IsPressed(2) && p.SingleEvents == 2);
        h.Click(w, Item(g, 2, 3));
        h.Check("toggle-group một lựa chọn: bấm lại nút đang bật thì không còn nút nào", p.Single.Selected.Count == 0);

        var m = h.Box(w, "f-multi");
        h.Click(w, Item(m, 0, 3));
        h.Click(w, Item(m, 2, 3));
        h.Check("toggle-group nhiều lựa chọn: A và C cùng bật", p.Multi.Selected is [0, 2]);
        h.Click(w, Item(m, 0, 3));
        h.Check("toggle-group nhiều lựa chọn: tắt A, C vẫn bật", p.Multi.Selected is [2]);
    }

    static void TextEntries(CheckHarness h, Window w, FormsProbe p)
    {
        h.Click(w, "f-input");
        h.Type(w, "Xin chào");
        h.Check($"input: gõ thì Text đổi và phát TextChanged ({p.Name.Text})", p.Name.Text == "Xin chào" && p.TextEvents > 0);
        h.Press(w, Key.Backspace);
        h.Press(w, Key.Enter);
        h.Check($"input: Backspace xoá một ký tự, Enter phát Submitted ({p.Submitted})", p.Submitted == "Xin chà");

        h.Click(w, "f-input-off");
        h.Type(w, "x");
        h.Check("input: vô hiệu hoá thì không nhận focus, không gõ được", p.NameOff.Text == "cố định" && w.Focused is null);

        h.Click(w, "f-textarea");
        h.Type(w, "dòng 1");
        h.Press(w, Key.Enter);
        h.Type(w, "dòng 2");
        h.Check("textarea: Enter xuống dòng", p.Bio.Text == "dòng 1\ndòng 2");

        h.Click(w, "f-otp");
        h.Type(w, "12a3");
        h.Check($"otp: chỉ nhận chữ số ({p.Otp.Value})", p.Otp.Value == "123" && p.OtpCompleted == 0);
        h.Type(w, "45678");
        h.Check($"otp: đủ 6 ô thì dừng và phát Completed ({p.Otp.Value})", p.Otp.Value == "123456" && p.OtpCompleted == 1);
        h.Press(w, Key.Backspace);
        h.Check($"otp: Backspace xoá ô cuối ({p.Otp.Value})", p.Otp.Value == "12345");
    }
}
