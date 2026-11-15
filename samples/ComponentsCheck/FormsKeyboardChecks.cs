using GotX;
using GotX.Components;

/// <summary>
/// Kiểm tra bàn phím của nhóm Forms trên <see cref="FormsProbe"/>: đi một mạch bằng Tab từ control đầu tới cuối (thứ tự
/// Tab là thứ tự vẽ), ở mỗi control đo state sau phím, vị trí focus (<c>Window.IsFocused</c>) và pixel của vòng focus.
/// </summary>
static class FormsKeyboardChecks
{
    static int Lum((int R, int G, int B) c) => (c.R + c.G + c.B) / 3;

    /// <summary>Có vòng focus quanh hộp: pixel cách mép trái 2px (trong vòng 3px) sẫm hơn nền trắng. Chờ transition xong mới đo.</summary>
    public static bool Ring(CheckHarness h, Window w, Bounds box) => RingAt(h, w, new Point(box.X - 2, box.Y + box.Height / 2));

    public static bool RingAt(CheckHarness h, Window w, Point p)
    {
        h.Wait(w, 0.3);
        return Lum(h.Pixel(w, p)) < 242;
    }

    public static void Run(CheckHarness h)
    {
        FormsProbe p = null!;
        var w = h.Open(app => p = app.New(new FormsProbe()));
        string slider = "gotx-slider-" + p.Volume.Id.Value;
        SwitchAndRadio(h, w, p, slider);
        Slider(h, w, p, slider);
        ToggleAndCheckbox(h, w, p);
        ToggleGroups(h, w, p);
        TextControls(h, w, p);
    }

    static void SwitchAndRadio(CheckHarness h, Window w, FormsProbe p, string slider)
    {
        string sw = "gotx-switch-" + p.Wifi.Id.Value;
        var box = h.Box(w, "f-switch");
        h.Check("switch: chưa focus thì không có vòng focus", !Ring(h, w, box));
        h.Press(w, Key.Tab);
        h.Check("switch: Tab đầu tiên focus công tắc và hiện vòng focus", w.IsFocused(sw) && w.FocusVisible && Ring(h, w, box));
        h.Press(w, Key.Space);
        h.Check("switch: Space bật", p.Wifi.Checked && p.SwitchEvents == 1);
        h.Press(w, Key.Enter);
        h.Check("switch: Enter tắt", !p.Wifi.Checked && p.SwitchEvents == 2);
        h.Click(w, "f-switch");
        h.Check("switch: bấm chuột thì focus nhưng không hiện vòng focus", w.IsFocused(sw) && !w.FocusVisible && !Ring(h, w, box) && p.Wifi.Checked);

        string Radio(int i) => $"gotx-radio-{p.Plan.Id.Value}-{i}";
        var g = h.Box(w, "f-radio");
        Point Circle(int i) => new(g.X - 2, g.Y + 8 + i * 28);
        h.Press(w, Key.Tab);
        h.Check("radio: Tab bỏ qua công tắc vô hiệu hoá, vào mục đầu khi chưa chọn gì và không tự chọn",
            w.IsFocused(Radio(0)) && p.Plan.SelectedIndex == -1 && p.LockedEvents == 0);
        h.Press(w, Key.Down);
        h.Check($"radio: ↓ chuyển focus và chọn mục kế ({p.LastRadio})", w.IsFocused(Radio(1)) && p.Plan.SelectedIndex == 1 && p.LastRadio == "Hai");
        h.Press(w, Key.Right);
        h.Press(w, Key.Down);
        h.Check("radio: → rồi ↓ ở mục cuối quay vòng về mục đầu", w.IsFocused(Radio(0)) && p.Plan.SelectedIndex == 0);
        h.Press(w, Key.Up);
        h.Check("radio: ↑ ở mục đầu quay vòng xuống mục cuối", w.IsFocused(Radio(2)) && p.Plan.SelectedIndex == 2);
        h.Check("radio: vòng focus quanh nút tròn của mục đang focus, mục khác không có", RingAt(h, w, Circle(2)) && !RingAt(h, w, Circle(0)));
        h.Press(w, Key.Home);
        h.Check("radio: Home chọn mục đầu", p.Plan.SelectedIndex == 0 && w.IsFocused(Radio(0)));
        h.Press(w, Key.Tab);
        h.Check("radio: Tab rời cả nhóm trong một bước (bỏ qua nhóm vô hiệu hoá) tới slider", w.IsFocused(slider) && p.PlanOff.SelectedIndex == 0);
        h.Press(w, Key.Tab, Modifiers.Shift);
        // Theo thứ tự vẽ Shift+Tab rơi vào mục cuối (mục 2); nhóm phải đưa focus về mục đang chọn mà không đổi lựa chọn.
        h.Check("radio: Shift+Tab vào lại nhóm thì focus về mục đang chọn (mục đầu), không phải mục cuối", w.IsFocused(Radio(0)) && p.Plan.SelectedIndex == 0);
        h.Press(w, Key.Tab);
    }

    static void Slider(CheckHarness h, Window w, FormsProbe p, string slider)
    {
        var s = p.Volume; // bước 5, LargeStep 10
        h.Check("slider: đang giữ focus sau khi Tab khỏi radio", w.IsFocused(slider) && s.Value == 0);
        h.Press(w, Key.Right);
        h.Check($"slider: → tăng một bước và phát ValueCommitted ({s.Value})", s.Value == 5 && p.Commits == 1);
        h.Press(w, Key.Up);
        h.Press(w, Key.Left);
        h.Press(w, Key.Down);
        h.Check($"slider: ↑ tăng, ← và ↓ giảm, mỗi lần đổi một ValueCommitted ({s.Value}, {p.Commits})", s.Value == 0 && p.Commits == 4);
        h.Press(w, Key.PageUp);
        h.Press(w, Key.Right, Modifiers.Shift);
        h.Check($"slider: PageUp và Shift+→ nhảy bước lớn 10 ({s.Value})", s.Value == 20);
        h.Press(w, Key.PageDown);
        h.Check($"slider: PageDown lùi bước lớn ({s.Value})", s.Value == 10);
        h.Press(w, Key.End);
        int commits = p.Commits;
        h.Press(w, Key.Right);
        h.Check($"slider: End về Max; → ở Max không đổi và không phát thêm sự kiện ({s.Value})", s.Value == 100 && p.Commits == commits);
        h.Press(w, Key.Home);
        h.Check($"slider: Home về Min ({s.Value})", s.Value == 0);
        var box = h.Box(w, "f-slider");
        // Con chạy 12px nằm giữa mép trái thanh khi giá trị = Min; vòng focus 3px bao ngoài nó.
        h.Check("slider: vòng focus vẽ quanh con chạy", RingAt(h, w, new Point(box.X - 8, box.Y + box.Height / 2)));
        h.Press(w, Key.Tab);
        h.Check("slider: Tab sang slider kế; con chạy cũ hết vòng focus",
            w.IsFocused("gotx-slider-" + p.Stepped.Id.Value) && !RingAt(h, w, new Point(box.X - 8, box.Y + box.Height / 2)));
        h.Press(w, Key.Right);
        h.Check($"slider bước 10: → được 10 ({p.Stepped.Value})", p.Stepped.Value == 10);
    }

    static void ToggleAndCheckbox(CheckHarness h, Window w, FormsProbe p)
    {
        h.Press(w, Key.Tab);
        var toggle = h.Box(w, "f-toggle");
        h.Check("toggle: Tab bỏ qua slider vô hiệu hoá, tới nút toggle có vòng focus", p.SliderOff.Value == 20 && w.HasElementFocus && Ring(h, w, toggle));
        h.Press(w, Key.Space);
        h.Check("toggle: Space nhấn", p.Bold.Pressed && p.ToggleEvents == 1);
        h.Press(w, Key.Enter);
        h.Check("toggle: Enter nhả", !p.Bold.Pressed && p.ToggleEvents == 2);

        h.Press(w, Key.Tab);
        string check = "gotx-checkbox-" + p.Agree.Id.Value;
        var box = h.Box(w, "f-check");
        h.Check("checkbox: Tab bỏ qua toggle vô hiệu hoá, focus ô đánh dấu và hiện vòng focus",
            w.IsFocused(check) && !p.ToggleOff.Pressed && Ring(h, w, box) && !Ring(h, w, toggle));
        h.Press(w, Key.Space);
        h.Check("checkbox: Space đánh dấu và phát CheckedChanged", p.Agree.Checked && p.CheckEvents == 1);
        h.Check("checkbox: đã đánh dấu vẫn còn vòng focus", Ring(h, w, box));
        h.Press(w, Key.Enter);
        h.Check("checkbox: Enter bỏ đánh dấu", !p.Agree.Checked && p.CheckEvents == 2);
    }

    static void ToggleGroups(CheckHarness h, Window w, FormsProbe p)
    {
        string Single(int i) => $"gotx-toggle-group-{p.Single.Id.Value}-{i}";
        string Multi(int i) => $"gotx-toggle-group-{p.Multi.Id.Value}-{i}";
        h.Press(w, Key.Tab);
        h.Check("toggle-group: Tab vào nút đầu của nhóm", w.IsFocused(Single(0)));
        h.Press(w, Key.Right);
        h.Check("toggle-group: → chỉ chuyển focus, không bật nút", w.IsFocused(Single(1)) && p.Single.Selected.Count == 0 && p.SingleEvents == 0);
        h.Press(w, Key.Space);
        h.Check("toggle-group: Space bật nút đang focus", p.Single.IsPressed(1) && p.SingleEvents == 1);
        h.Press(w, Key.End);
        h.Press(w, Key.Right);
        h.Check("toggle-group: End tới nút cuối, → quay vòng về nút đầu", w.IsFocused(Single(0)));
        h.Press(w, Key.Left);
        h.Press(w, Key.Enter);
        h.Check("toggle-group: ← quay vòng tới nút cuối, Enter bật nó và tắt nút kia (chọn một)",
            w.IsFocused(Single(2)) && p.Single.Selected is [2]);
        var g = h.Box(w, "f-single");
        h.Check("toggle-group: vòng focus ở nút đang focus (mép phải nhóm), không ở nút đầu",
            RingAt(h, w, new Point(g.Right + 1, g.Y + g.Height / 2)) && !Ring(h, w, g));
        h.Press(w, Key.Tab);
        h.Check("toggle-group: Tab rời nhóm trong một bước, sang nhóm kế", w.IsFocused(Multi(0)));
        h.Press(w, Key.Tab, Modifiers.Shift);
        h.Check("toggle-group: Shift+Tab quay lại nút focus lần trước", w.IsFocused(Single(2)));
        h.Press(w, Key.Home);
        h.Press(w, Key.Tab);
        h.Press(w, Key.Right);
        h.Press(w, Key.Space);
        h.Press(w, Key.Right);
        h.Press(w, Key.Space);
        h.Check("toggle-group nhiều lựa chọn: →, Space, →, Space bật hai nút sau", p.Multi.Selected is [1, 2] && w.IsFocused(Multi(2)));
    }

    static void TextControls(CheckHarness h, Window w, FormsProbe p)
    {
        h.Press(w, Key.Tab);
        var input = h.Box(w, "f-input");
        h.Check("input: Tab từ nhóm toggle vào ô nhập, có vòng focus", p.Name.IsFocused && !w.HasElementFocus && Ring(h, w, input));
        h.Type(w, "ab");
        h.Press(w, Key.Tab);
        h.Check("textarea: Tab bỏ qua ô vô hiệu hoá; ô trước giữ chữ và mất vòng focus",
            p.Bio.IsFocused && p.Name.Text == "ab" && p.NameOff.Text == "cố định" && !Ring(h, w, input) && Ring(h, w, h.Box(w, "f-textarea")));
        h.Press(w, Key.Tab);
        var otp = h.Box(w, "f-otp");
        h.Check("otp: Tab vào ô mã, ô đầu tiên là ô hoạt động (có vòng focus)", !p.Bio.IsFocused && w.Focused is not null && Ring(h, w, otp));
        h.Type(w, "7");
        h.Check($"otp: gõ sau khi Tab vào ({p.Otp.Value}); ô hoạt động dời sang ô hai", p.Otp.Value == "7" && !Ring(h, w, otp));

        h.Press(w, Key.Tab);
        var group = h.Box(w, "f-group");
        h.Check("input-group: Tab vào ô bên trong thì cả nhóm có vòng focus", p.Query.IsFocused && Ring(h, w, group));
        h.Press(w, Key.Tab);
        h.Check("input-group: Tab tiếp tới nút trong nhóm; vòng của nhóm tắt, nút có vòng riêng",
            w.IsFocused("f-group-go") && !p.Query.IsFocused && !Ring(h, w, group) && Ring(h, w, h.Box(w, "f-group-go")));
        h.Press(w, Key.Enter);
        h.Check("input-group: Enter bấm nút đang focus", p.GroupClicks == 1);
        h.Click(w, "f-addon");
        h.Check("input-group: bấm phần phụ (icon) thì focus ô nhập", p.Query.IsFocused);
        h.Click(w, "f-label");
        h.Check("label: bấm nhãn gắn với ô thì ô đó nhận focus", p.Name.IsFocused && !p.Query.IsFocused);
        h.Press(w, Key.Tab, Modifiers.Shift);
        h.Check("Shift+Tab từ ô nhập quay về nhóm toggle đứng trước nó", w.IsFocused($"gotx-toggle-group-{p.Multi.Id.Value}-2"));
    }
}
