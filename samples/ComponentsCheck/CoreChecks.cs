using GotX;
using GotX.Components;

/// <summary>Kiểm tra nền: định vị nổi, lớp overlay, sự kiện hover/kéo, token màu sáng/tối, và các component mẫu.</summary>
static class CoreChecks
{
    public static void Run(CheckHarness h)
    {
        Floating(h);
        Tokens(h);
        Overlays(h);
        Events(h);
    }

    static void Floating(CheckHarness h)
    {
        var viewport = new Bounds(0, 0, 800, 600);
        var size = new Size(200, 100);
        var below = GotX.Floating.Place(new Bounds(100, 100, 80, 30), size, viewport, Placement.BottomStart, 4, 8, out var used);
        h.Check("floating: đặt dưới, mép trái thẳng anchor", used == Placement.BottomStart && below.X == 100 && below.Y == 134);
        var flipped = GotX.Floating.Place(new Bounds(100, 540, 80, 30), size, viewport, Placement.BottomStart, 4, 8, out used);
        h.Check("floating: sát đáy thì lật lên trên", used == Placement.TopStart && flipped.Bottom == 536);
        var shifted = GotX.Floating.Place(new Bounds(760, 100, 30, 30), size, viewport, Placement.Bottom, 4, 8, out _);
        h.Check("floating: sát mép phải thì trượt vào trong", shifted.Right == 792);
        var right = GotX.Floating.Place(new Bounds(100, 100, 80, 30), size, viewport, Placement.Right, 4, 8, out used);
        h.Check("floating: bên phải, căn giữa theo chiều dọc", used == Placement.Right && right.X == 184 && right.Y == 65);
    }

    static void Tokens(CheckHarness h)
    {
        var light = new VariantContext(false, 900, 700, 16);
        var dark = light with { Dark = true };
        var a = Tailwind.Resolve("bg-primary text-primary-foreground", light);
        var b = Tailwind.Resolve("bg-primary text-primary-foreground", dark);
        h.Check("token: bg-primary đổi màu theo dark mode không cần dark:", a.Background.R < 0.2f && b.Background.R > 0.8f);
        h.Check("token: text-primary-foreground cũng đổi", a.TextColorValue!.Value.R > 0.9f && b.TextColorValue!.Value.R < 0.2f);
        var half = Tailwind.Resolve("bg-primary/50", dark);
        h.Check("token: độ mờ /50 áp cho cả màu dark", MathF.Abs(half.Background.A - 0.5f) < 0.01f && half.Background.R > 0.8f);
        h.Check("token: rounded-lg theo --radius của shadcn (10px)", Tailwind.Resolve("rounded-lg", light).CornerRadii.TopLeft == 10);
    }

    static void Overlays(CheckHarness h)
    {
        ProbeView probe = null!;
        var w = h.Open(app => probe = app.New(new ProbeView()));

        // Checkbox
        h.Click(w, "probe-check");
        h.Check("checkbox: bấm thì đánh dấu và phát CheckedChanged", probe.Agree.Checked && probe.CheckEvents == 1);

        // Dialog: modal chặn phần bên dưới
        h.Click(w, "probe-open");
        h.Check("dialog: mở sau khi bấm nút", probe.Dialog.IsOpen && w.Overlays.Count == 1);
        int before = probe.UnderClicks;
        h.Click(w, CheckHarness.Center(h.Box(w, "probe-under")));
        h.Check("dialog: bấm ra ngoài không lọt xuống nút bên dưới, và đóng hộp thoại", probe.UnderClicks == before && !probe.Dialog.IsOpen);
        h.Click(w, "probe-open");
        h.Press(w, Key.Escape);
        h.Check("dialog: Escape đóng", !probe.Dialog.IsOpen && probe.DialogEvents == 4);
        h.Click(w, "probe-open");
        h.Click(w, "probe-confirm");
        h.Check("dialog: nút trong hộp thoại nhận click", probe.Confirmed == 1 && !probe.Dialog.IsOpen);
        h.Click(w, "probe-under");
        h.Check("dialog: đóng rồi thì nút bên dưới bấm lại được", probe.UnderClicks == before + 1);

        // Select: danh sách neo dưới nút, chọn rồi đóng
        var trigger = w.AnchorBounds("gotx-select-" + probe.Fruit.Id.Value) ?? throw new Exception("thiếu anchor của select");
        h.Click(w, CheckHarness.Center(trigger));
        h.Check("select: mở danh sách ngay dưới nút, rộng ít nhất bằng nút", probe.Fruit.IsOpen
            && w.Overlays[0].ContentBounds is var list && MathF.Abs(list.Y - (trigger.Bottom + 4)) < 0.5f
            && MathF.Abs(list.X - trigger.X) < 0.5f && list.Width >= trigger.Width - 0.5f);
        var listBox = w.Overlays[0].ContentBounds;
        h.Click(w, new Point(listBox.X + 20, listBox.Y + 4 + 28 + 14)); // dòng thứ hai (mỗi dòng cao 28px, đệm 4px)
        h.Check($"select: chọn dòng thứ hai rồi đóng ({probe.Fruit.Value})", probe.Fruit.Value == "Cam" && !probe.Fruit.IsOpen && probe.SelectEvents == 1);
        h.Click(w, CheckHarness.Center(trigger));
        h.Click(w, new Point(850, 650));
        h.Check("select: bấm ra ngoài thì đóng, giá trị giữ nguyên", !probe.Fruit.IsOpen && probe.Fruit.Value == "Cam");

        // Select sát đáy cửa sổ: danh sách lật lên trên
        var low = h.Open(app => app.New(new LowSelectView()));
        var lowTrigger = low.AnchorBounds("gotx-select-" + ((LowSelectView)low.Root!).Pick.Id.Value)!.Value;
        h.Click(low, CheckHarness.Center(lowTrigger));
        h.Check("select: sát đáy cửa sổ thì danh sách lật lên trên nút",
            low.Overlays.Count == 1 && low.Overlays[0].ActualPlacement == Placement.TopStart && low.Overlays[0].ContentBounds.Bottom <= lowTrigger.Y);
    }

    static void Events(CheckHarness h)
    {
        ProbeView probe = null!;
        var w = h.Open(app => probe = app.New(new ProbeView()));
        var hover = h.Box(w, "probe-hover");
        h.Move(w, CheckHarness.Center(hover));
        h.Move(w, new Point(hover.Right + 60, hover.Bottom + 60));
        h.Check($"OnHover: vào rồi ra ({probe.HoverLog})", probe.HoverLog == "vào,ra");

        var track = h.Box(w, "probe-track");
        h.Drag(w, new Point(track.X + 20, track.Y + 5), new Point(track.X + 150, track.Y + 5));
        h.Check($"OnDrag: Start → Move → End, tỉ lệ cuối 0.75 ({probe.DragPhases}, {probe.DragFraction:F2})",
            probe.DragPhases == "Start,Move,Move,Move,Move,End" && MathF.Abs(probe.DragFraction - 0.75f) < 0.01f);
    }
}

/// <summary>View thử nghiệm chứa các component mẫu và phần tử nhận sự kiện.</summary>
sealed class ProbeView : View
{
    public Checkbox Agree = null!;
    public Dialog Dialog = null!;
    public Select Fruit = null!;
    public int CheckEvents, DialogEvents, SelectEvents, UnderClicks, Confirmed;
    public string HoverLog = "", DragPhases = "";
    public float DragFraction;
    Window? _window;

    protected override void OnCreated()
    {
        Agree = App.New(new Checkbox { Label = "Đồng ý" });
        Fruit = App.New(new Select { Items = ["Táo", "Cam", "Xoài"], Placeholder = "Chọn quả" });
        Dialog = App.New(new Dialog
        {
            Title = "Xác nhận", Description = "Bạn có chắc không?",
            Footer = w => Ui.Button(w, "Đồng ý").Key("probe-confirm").OnClick(this, static (v, _) => { v.Confirmed++; v.Dialog.Close(); }),
        });
        App.Subscribe<Checkbox.CheckedChanged>(Agree, _ => CheckEvents++);
        App.Subscribe<Dialog.OpenChanged>(Dialog, _ => DialogEvents++);
        App.Subscribe<Select.ValueChanged>(Fruit, _ => SelectEvents++);
    }

    public override Div Render(Window w)
    {
        _window = w;
        return w.Div().Class("size-full flex flex-col items-start gap-6 p-8 bg-background text-foreground")
            .Child(w.Div().Key("probe-check").Child(w.View(Agree)))
            .Child(Ui.Button(w, "Mở hộp thoại").Key("probe-open").OnClick(this, static (v, _) => v.Dialog.Open(v._window!)))
            .Child(Ui.Button(w, "Nút bên dưới", ButtonVariant.Outline).Key("probe-under").OnClick(this, static (v, _) => v.UnderClicks++))
            .Child(w.View(Fruit))
            .Child(w.Div().Class("w-32 h-10 rounded-md bg-muted").Key("probe-hover")
                .OnHover(this, static (v, inside) => v.HoverLog += (v.HoverLog.Length > 0 ? "," : "") + (inside ? "vào" : "ra")))
            .Child(w.Div().Class("w-[200px] h-[10px] rounded-full bg-muted").Key("probe-track")
                .OnDrag(this, static (v, e) =>
                {
                    v.DragPhases += (v.DragPhases.Length > 0 ? "," : "") + e.Phase;
                    v.DragFraction = e.FractionX;
                }));
    }
}

/// <summary>Một Select đặt sát đáy cửa sổ.</summary>
sealed class LowSelectView : View
{
    public Select Pick = null!;
    protected override void OnCreated() => Pick = App.New(new Select { Items = ["Một", "Hai", "Ba", "Bốn"] });
    public override Div Render(Window w) =>
        w.Div().Class("size-full flex flex-col justify-end items-start p-4 bg-background text-foreground").Child(w.View(Pick));
}
