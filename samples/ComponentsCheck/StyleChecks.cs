using GotX;
using GotX.Components;

/// <summary>
/// Kiểm tra hệ style (<see cref="ComponentStyles"/>): bảng Rhea đổi đúng kích thước/màu, phạm vi <c>Use</c> trả lại bảng cũ,
/// component có state giữ bảng lúc được tạo dù vẽ ngoài phạm vi, Select của rhea mở đè lên nút, style mặc định của app.
/// Ảnh mặc định (các trang gallery) không đổi vì bảng Default chép nguyên chuỗi cũ – so hash ảnh ở ngoài bộ kiểm tra này.
/// </summary>
static class StyleChecks
{
    static bool Near(float a, float b, float tol = 0.5f) => MathF.Abs(a - b) <= tol;

    /// <summary>Hai cột: trái style mặc định, phải style rhea (cùng các phần tử, khác bảng class).</summary>
    sealed class StyleProbe : View
    {
        public Checkbox RheaBox = null!;
        public Select RheaSelect = null!;

        protected override void OnCreated()
        {
            using var _ = ComponentStyles.Use(ComponentStyle.Rhea);
            RheaBox = App.New(new Checkbox());
            RheaSelect = App.New(new Select { Items = ["One", "Two", "Three"], SelectedIndex = 1 });
        }

        static Div Column(Window w, string prefix) =>
            w.Div().Class("flex w-72 flex-col gap-4 p-4")
                .Child(w.Div().Class("flex").Child(Ui.Button(w, "Button").Key(prefix + "button")))
                .Child(Ui.Card(w).Key(prefix + "card").Child(Ui.CardContent(w).Child(w.Div().Key(prefix + "card-body").Child("Body"))))
                .Child(Ui.Progress(w, 50).Key(prefix + "progress"));

        public override Div Render(Window w)
        {
            var root = w.Div().Class("flex gap-8 bg-background p-8").Child(Column(w, "d-"));
            using (ComponentStyles.Use(ComponentStyle.Rhea)) root.Child(Column(w, "r-"));
            // Vẽ ngoài phạm vi rhea: hai view con vẫn theo bảng đã nhớ lúc tạo.
            return root.Child(w.Div().Class("flex flex-col gap-4 p-4")
                .Child(w.Div().Class("flex").Key("r-checkbox").Child(w.View(RheaBox)))
                .Child(w.Div().Class("flex w-48").Key("r-select").Child(w.View(RheaSelect))));
        }
    }

    public static void Run(CheckHarness h)
    {
        // Đồng hồ ảo dùng chung: trả lại sau khi chạy để ảnh các trang gallery (spinner, animate-*) không đổi theo nhóm này.
        double now = h.Now;
        try { RunChecks(h); }
        finally { h.Now = now; }
    }

    static void RunChecks(CheckHarness h)
    {
        // Mọi chuỗi của bảng rhea đều biên dịch được (class lạ sẽ hiện ở dòng "Class chưa hỗ trợ" cuối báo cáo).
        foreach (var sheet in new[] { ComponentStyleSheet.Rhea, ComponentStyleSheet.NewYorkV4 })
        {
            int strings = 0;
            foreach (var p in typeof(ComponentStyleSheet).GetProperties())
                foreach (string s in p.GetValue(sheet) switch { string one => [one], string[] many => many, _ => [] })
                { Tailwind.Get(s); strings++; }
            h.Check($"bảng {sheet.Style}: {strings} chuỗi class đã biên dịch", strings > 100);
        }

        h.Check("phạm vi: ngoài Use là bảng mặc định", ComponentStyles.Current == ComponentStyleSheet.Default);
        using (ComponentStyles.Use(ComponentStyle.Rhea))
        {
            using (ComponentStyles.Use(ComponentStyle.Default))
                h.Check("phạm vi lồng nhau: trong cùng thắng", ComponentStyles.Current == ComponentStyleSheet.Default);
            h.Check("phạm vi lồng nhau: thoát trong cùng trả lại Rhea", ComponentStyles.Current == ComponentStyleSheet.Rhea);
        }
        h.Check("thoát phạm vi trả lại bảng mặc định", ComponentStyles.Current == ComponentStyleSheet.Default);

        ComponentTheme.Install(ComponentStyle.Rhea);
        bool installed = Ui.ButtonClasses().Contains("rounded-2xl");
        ComponentTheme.Install(ComponentStyle.Default);
        h.Check("Install(style) đổi bảng mặc định của app và đổi lại được", installed && !Ui.ButtonClasses().Contains("rounded-2xl"));

        var probe = (StyleProbe)null!;
        var w = h.Open(app => probe = app.New(new StyleProbe()));
        h.Check($"nút: mặc định cao 32 (h-8), rhea cũng h-8 nhưng đệm px-3 nên rộng hơn ({h.Box(w, "d-button").Width} / {h.Box(w, "r-button").Width})",
            Near(h.Box(w, "d-button").Height, 32) && Near(h.Box(w, "r-button").Height, 32) && Near(h.Box(w, "r-button").Width - h.Box(w, "d-button").Width, 4));
        var rb = h.Box(w, "r-button");
        h.Check("nút rhea bo 2xl (18px): góc ngoài là nền trang", h.Pixel(w, new Point(rb.X + 1, rb.Y + 1)).R > 200);
        h.Check($"card: đệm 16 (mặc định) / 20 (rhea, --card-spacing 5)",
            Near(h.Box(w, "d-card-body").X - h.Box(w, "d-card").X, 16) && Near(h.Box(w, "r-card-body").X - h.Box(w, "r-card").X, 20)
            && Near(h.Box(w, "r-card-body").Y - h.Box(w, "r-card").Y, 20));
        h.Check("progress: h-1 mặc định, h-2 rhea", Near(h.Box(w, "d-progress").Height, 4) && Near(h.Box(w, "r-progress").Height, 8));

        // Checkbox tạo trong phạm vi rhea: ô chưa chọn có nền input/90 (#e5e5e5 · 0.9 trên trắng) dù vẽ ngoài phạm vi.
        var box = h.Box(w, "r-checkbox");
        var px = h.Pixel(w, new Point(box.X + 8, box.Y + box.Height / 2));
        h.Check($"checkbox giữ style lúc tạo: nền input/90 ({px.R})", px.R is > 220 and < 240);

        // Select rhea (Base UI alignItemWithTrigger): danh sách mở đè lên nút, mục đang chọn (Two) nằm đúng chỗ nút.
        var trigger = h.Box(w, probe.RheaSelect.TriggerKey);
        probe.RheaSelect.Open(); h.Frames(w);
        var item = h.Box(w, probe.RheaSelect.ItemKey(1));
        h.Check($"select rhea: mục đang chọn trùng nút (Δy {item.Y - trigger.Y}, Δx chữ {item.X - trigger.X})",
            Near(item.Y, trigger.Y, 1) && Near(item.Height, trigger.Height, 1));
        h.Save(w, "style-probe.png");

        // new-york-v4 và đổi style lúc chạy: view không ghim style vẽ lại theo ComponentTheme.Use(app, …).
        var live = h.Open(app => app.New(new LiveProbe()));
        float before = h.Box(live, "live-button").Height;
        ComponentTheme.Use(live.App, ComponentStyle.NewYorkV4); h.Frames(live);
        float v4 = h.Box(live, "live-button").Height, card = h.Box(live, "live-body").X - h.Box(live, "live-card").X;
        bool boxV4 = h.Box(live, "live-input").Height is var ih && Near(ih, 36);
        float toggleV4 = h.Box(live, "live-toggle").Height;
        var avatar = h.Box(live, "live-avatar");
        float accordionV4 = h.Box(live, ((LiveProbe)live.Root!).Accordion.TriggerKey(0)).Height;
        bool ringless = h.Pixel(live, new Point(avatar.X + 16, avatar.Y + 0.5f)).R > 200; // v4: không có viền after:border
        ComponentTheme.Use(live.App, ComponentStyle.Default); h.Frames(live);
        h.Check($"đổi style lúc chạy: nút 32 → 36 (v4 h-9) → 32 ({before}/{v4}/{h.Box(live, "live-button").Height})",
            Near(before, 32) && Near(v4, 36) && Near(h.Box(live, "live-button").Height, 32));
        h.Check($"v4: card đệm 24 (px-6), ô nhập cao 36 ({card})", Near(card, 25) && boxV4);
        h.Check($"v4: toggle cao 36 (h-9) ({toggleV4}), toggle mặc định 32 ({h.Box(live, "live-toggle").Height})",
            Near(toggleV4, 36) && Near(h.Box(live, "live-toggle").Height, 32));
        h.Check($"v4: avatar size-8 không viền phủ ({avatar.Width})", Near(avatar.Width, 32) && ringless);
        float accordionDefault = h.Box(live, ((LiveProbe)live.Root!).Accordion.TriggerKey(0)).Height;
        h.Check($"v4: accordion py-4 thay py-2.5 – nút cao hơn 12px ({accordionV4} / {accordionDefault})", Near(accordionV4 - accordionDefault, 12));
        h.Check("ComponentTheme.Style trở lại Default", ComponentTheme.Style == ComponentStyle.Default);
    }

    /// <summary>View không ghim style: nút, card, ô nhập tạo ngoài mọi phạm vi Use.</summary>
    sealed class LiveProbe : View
    {
        Input _input = null!;
        Toggle _toggle = null!;
        public Accordion Accordion { get; private set; } = null!;
        protected override void OnCreated() { _input = App.New(new Input { Placeholder = "x" }); _toggle = App.New(new Toggle { Label = "B" }); Accordion = App.New(new Accordion { Items = [new AccordionItem("Q", "A")] }); }
        public override Div Render(Window w) => w.Div().Class("flex w-80 flex-col items-start gap-4 p-4")
            .Child(Ui.Button(w, "Live").Key("live-button"))
            .Child(Ui.Card(w).Class("w-full").Key("live-card").Child(Ui.CardContent(w).Child(w.Div().Key("live-body").Child("Body"))))
            .Child(w.Div().Class("w-full").Key("live-input").Child(w.View(_input)))
            .Child(w.Div().Class("flex").Key("live-toggle").Child(w.View(_toggle)))
            .Child(Ui.Avatar(w, "AB").Key("live-avatar"))
            .Child(w.Div().Class("w-full").Child(w.View(Accordion)));
    }
}
