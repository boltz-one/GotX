using GotX;
using GotX.Components;
using GotX.Test;

/// <summary>Cây ngữ nghĩa: vai trò, tên, trạng thái của component; hành động ngữ nghĩa; luật lint unnamed-control.</summary>
static class SemanticsTests
{
    internal sealed class Host : View
    {
        public Checkbox Check = null!;
        public Switch Toggle = null!;
        public RadioGroup Radios = null!;
        public Slider Volume = null!;
        public Tabs Tabs = null!;
        public Input Name = null!;
        public Select Pick = null!;
        public bool IconButtonLabel;
        public int Saves;

        protected override void OnCreated()
        {
            Check = App.New(new Checkbox { Label = "Accept terms", Checked = true });
            Toggle = App.New(new Switch { Label = "Wi-Fi" });
            Radios = App.New(new RadioGroup { Items = ["Small", "Large"], SelectedIndex = 1 });
            Volume = App.New(new Slider { Value = 40, AriaLabel = "Volume" });
            Tabs = App.New(new Tabs { Items = ["Account", "Password"], Content = static (w, i) => w.Div().Child(i == 0 ? "Account body" : "Password body") });
            Name = App.New(new Input { Placeholder = "Your name", Text = "Ada", Invalid = true });
            Pick = App.New(new Select { Items = ["Red", "Green"], SelectedIndex = 0, AriaLabel = "Color" });
        }

        public override Div Render(Window w)
        {
            var close = Ui.Button(w, ButtonVariant.Ghost, ButtonSize.Icon).Key("close").Child(Icon.X(w));
            if (IconButtonLabel) close.AriaLabel("Close");
            return w.Div().Class("flex w-96 flex-col gap-3 p-4")
                .Child(Ui.Button(w, "Save").Key("save").OnClick(this, static (h, _) => h.Saves++))
                .Child(close)
                .Child(Ui.Button(w, "Off", ButtonVariant.Outline).Key("off").Disabled())
                .Child(w.View(Check)).Child(w.View(Toggle)).Child(w.View(Radios)).Child(w.View(Volume))
                .Child(w.View(Tabs)).Child(w.View(Name)).Child(w.View(Pick))
                .Child(Ui.Progress(w, 30, "Upload", "30%"))
                .Child(Ui.Alert(w).Child(Ui.AlertTitle(w, "Heads up")));
        }
    }

    public static void Register(TestSuite suite)
    {
        suite.Add("core/semantics/component-roles-names-states", t =>
        {
            t.Open(app => app.New(new Host()));
            var s = t.Semantics;
            Check(t, s.FindByRole(SemanticRole.Button, "Save"), SemanticState.None, "nút Save");
            var off = s.FindByRole(SemanticRole.Button, "Off");
            t.Assert.True(off is { Disabled: true, Focusable: false }, "nút bị vô hiệu: Disabled, không Focusable");
            Check(t, s.FindByRole(SemanticRole.Checkbox, "Accept terms"), SemanticState.Checked, "checkbox đã chọn");
            Check(t, s.FindByRole(SemanticRole.Switch, "Wi-Fi"), SemanticState.None, "switch tắt");
            Check(t, s.FindByRole(SemanticRole.Radio, "Large"), SemanticState.Checked, "radio đang chọn");
            Check(t, s.FindByRole(SemanticRole.Radio, "Small"), SemanticState.None, "radio không chọn");
            var slider = s.FindByRole(SemanticRole.Slider, "Volume");
            t.Assert.True(slider is { HasRange: true, Now: 40, Min: 0, Max: 100 }, $"slider có khoảng 0..100, giá trị 40 ({slider})");
            Check(t, s.FindByRole(SemanticRole.Tab, "Account"), SemanticState.Selected, "tab đang chọn");
            Check(t, s.FindByRole(SemanticRole.Tab, "Password"), SemanticState.None, "tab còn lại");
            t.Assert.True(s.FindByRole(SemanticRole.TabList) is not null && s.FindByRole(SemanticRole.TabPanel, "Account") is not null, "tablist + tabpanel");
            var box = s.FindByRole(SemanticRole.Textbox, "Your name");
            t.Assert.True(box is { Value: "Ada", Caret: 3 } && box.Has(SemanticState.Invalid), $"ô nhập: tên từ placeholder, giá trị, caret, invalid ({box})");
            var combo = s.FindByRole(SemanticRole.ComboBox, "Color");
            t.Assert.True(combo is { Value: "Red" } && !combo.Has(SemanticState.Expanded), $"combobox có giá trị ({combo})");
            var bar = s.FindByRole(SemanticRole.ProgressBar, "Upload");
            t.Assert.True(bar is { Now: 30, Max: 100 }, "progressbar có giá trị");
            t.Assert.True(s.FindByRole(SemanticRole.Alert, "Heads up") is not null, "alert tên từ tiêu đề");
        });

        suite.Add("core/semantics/state-follows-interaction", t =>
        {
            t.Open(app => app.New(new Host()));
            t.Click("save");
            var host = (Host)t.Window.Root!;
            host.Toggle.Checked = true;
            host.Tabs.SelectedIndex = 1;
            t.Frames(2);
            var s = t.Semantics;
            Check(t, s.FindByRole(SemanticRole.Switch, "Wi-Fi"), SemanticState.Checked, "switch bật sau khi đổi");
            Check(t, s.FindByRole(SemanticRole.Tab, "Password"), SemanticState.Selected, "tab mới được chọn");
            t.Assert.True(s.FocusedNode() is { Role: SemanticRole.Button, Name: "Save" }, "focus nằm trên nút Save");
        });

        suite.Add("core/semantics/actions-use-real-input", t =>
        {
            t.Open(app => app.New(new Host()));
            var host = (Host)t.Window.Root!;
            var s = t.Semantics;
            t.Assert.True(t.Window.PerformSemanticAction(s.FindByRole(SemanticRole.Switch, "Wi-Fi")!.Id, SemanticAction.Invoke), "Invoke switch");
            t.Assert.True(host.Toggle.Checked, "switch bật bằng hành động ngữ nghĩa");
            t.Assert.True(t.Window.PerformSemanticAction(s.FindByRole(SemanticRole.Slider)!.Id, SemanticAction.SetRange, 80), "SetRange slider");
            t.Assert.True(Math.Abs(host.Volume.Value - 80) <= 2, $"slider ~80 (thực tế {host.Volume.Value})");
            t.Assert.True(t.Window.PerformSemanticAction(s.FindByRole(SemanticRole.Textbox)!.Id, SemanticAction.SetValue, 0, "Grace"), "SetValue ô nhập");
            t.Assert.Equal(host.Name.Text, "Grace", "ô nhập nhận giá trị");
            t.Window.PerformSemanticAction(s.FindByRole(SemanticRole.Textbox)!.Id, SemanticAction.Focus);
            t.Frames(2);
            t.Assert.True(t.Semantics.FocusedNode() is { Role: SemanticRole.Textbox }, "Focus đưa focus vào ô nhập");
        });

        suite.Add("core/semantics/lint-unnamed-control", t =>
        {
            t.Open(app => app.New(new Host()));
            var issues = t.Lint(new LintOptions().Only("unnamed-control"));
            t.Assert.Equal(issues.Count, 1, "đúng một điều khiển không tên");
            t.Assert.True(issues.Count == 1 && issues[0].Severity == Severity.Warning && issues[0].Path.Contains("close"), "nút × chỉ có icon bị cảnh báo (warning)");
            t.Open(app => { var h = app.New(new Host()); h.IconButtonLabel = true; return h; });
            t.Assert.Equal(t.Lint(new LintOptions().Only("unnamed-control")).Count, 0, "có AriaLabel thì hết cảnh báo");
            t.Open(app => app.New(new Host()));
            t.Assert.Equal(t.Lint(new LintOptions().Only("unnamed-control").Suppress("unnamed-control", "close")).Count, 0, "Suppress theo mẫu đường dẫn");
        });
    }

    static void Check(UiTest t, SemanticsNode? n, SemanticState expected, string what)
    {
        if (n is null) { t.Fail($"{what}: không tìm thấy node ngữ nghĩa"); return; }
        var mask = SemanticState.Checked | SemanticState.Selected | SemanticState.Expanded;
        t.Assert.True((n.States & mask) == expected, $"{what}: trạng thái {n.States & mask}, cần {expected}");
    }
}
