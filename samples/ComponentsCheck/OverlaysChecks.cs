using GotX;
using GotX.Components;

/// <summary>Kiểm tra hành vi các component nhóm Overlays (phần hộp thoại, tấm cạnh, popover, tooltip, hover card).</summary>
static partial class OverlaysChecks
{
    public static void Run(CheckHarness h)
    {
        AlertDialogs(h);
        Panels(h);
        Popovers(h);
        Hovering(h);
        Menus(h);
        ContextMenus(h);
        Menubars(h);
        Navigation(h);
        Filtering(h);
        Toasts(h);
        FocusManagement(h);
        MenuKeys(h);
        MenubarKeys(h);
        ListKeys(h);
        LongSelect(h);
        NavigationKeys(h);
    }

    /// <summary>Mở cửa sổ có trang chứa các phần tử do <paramref name="build"/> trả về (tạo component trong <paramref name="setup"/>).</summary>
    static Window Host(CheckHarness h, Action<App> setup, Func<Window, Div> build) =>
        h.Open(app =>
        {
            setup(app);
            return app.New(new FragmentView(w =>
                w.Div().Class("size-full flex flex-col items-start gap-4 p-8 pt-24 bg-background text-foreground").Child(build(w))));
        });

    static Bounds Anchor(Window w, string name) => w.AnchorBounds(name) ?? throw new InvalidOperationException("thiếu anchor " + name);

    static bool Near(float a, float b) => MathF.Abs(a - b) < 0.5f;

    static void AlertDialogs(CheckHarness h)
    {
        AlertDialog alert = null!;
        var opener = new Opener();
        int confirmed = 0, cancelled = 0;
        var w = Host(h, app =>
        {
            opener.Alert = alert = app.New(new AlertDialog { Title = "Xoá dự án?", Description = "Không hoàn tác được.", ActionText = "Xoá", ActionVariant = ButtonVariant.Destructive });
            app.Subscribe<AlertDialog.Confirmed>(alert, _ => confirmed++);
            app.Subscribe<AlertDialog.Cancelled>(alert, _ => cancelled++);
        }, w => { opener.Window = w; return Ui.Button(w, "Xoá").Key("open-alert").OnClick(opener, static (o, _) => o.Alert!.Open(o.Window)); });

        h.Click(w, "open-alert");
        h.Check("alert-dialog: bấm nút thì mở (modal)", alert.IsOpen && w.Overlays.Count == 1 && w.Overlays[0].Options.Modal);
        h.Save(w, "overlay-alert-dialog.png");
        h.Click(w, new Point(20, 20));
        h.Check("alert-dialog: bấm ra ngoài KHÔNG đóng", alert.IsOpen && cancelled == 0);
        h.Click(w, alert.CancelKey);
        h.Check("alert-dialog: nút Huỷ đóng và phát Cancelled", !alert.IsOpen && cancelled == 1 && confirmed == 0);
        h.Click(w, "open-alert");
        h.Press(w, Key.Escape);
        h.Check("alert-dialog: Escape đóng và tính là huỷ", !alert.IsOpen && cancelled == 2);
        h.Click(w, "open-alert");
        h.Click(w, alert.ActionKey);
        h.Check("alert-dialog: nút hành động phát Confirmed rồi đóng", !alert.IsOpen && confirmed == 1 && cancelled == 2);
    }

    static void Panels(CheckHarness h)
    {
        Sheet sheet = null!;
        Drawer drawer = null!;
        var w = Host(h, app =>
        {
            sheet = app.New(new Sheet { Title = "Sửa hồ sơ", Description = "Thay đổi rồi bấm Lưu." });
            drawer = app.New(new Drawer { Title = "Mục tiêu", Description = "Đặt mục tiêu hằng ngày." });
        }, w => w.Div());
        float W = h.Size.Width, H = h.Size.Height;

        (PanelSide Side, Func<Bounds, bool> Edge, Point Outside, Point EdgePixel)[] cases =
        [
            (PanelSide.Right, b => Near(b.Right, W) && Near(b.Y, 0) && Near(b.Height, H) && b.Width < W, new Point(40, H / 2), new Point(W - 6, H / 2)),
            (PanelSide.Left, b => Near(b.X, 0) && Near(b.Y, 0) && Near(b.Height, H) && b.Width < W, new Point(W - 40, H / 2), new Point(6, H / 2)),
            (PanelSide.Top, b => Near(b.Y, 0) && Near(b.X, 0) && Near(b.Width, W) && b.Height < H, new Point(W / 2, H - 40), new Point(W - 6, 6)),
            (PanelSide.Bottom, b => Near(b.Bottom, H) && Near(b.X, 0) && Near(b.Width, W) && b.Height < H, new Point(W / 2, 40), new Point(W - 6, H - 6)),
        ];
        foreach (var (side, edge, outside, edgePixel) in cases)
        {
            sheet.Side = side;
            sheet.Open(w);
            h.Frames(w);
            var b = sheet.PanelBounds;
            h.Check($"sheet {side}: bám đúng cạnh cửa sổ ({b.X:F0},{b.Y:F0} {b.Width:F0}×{b.Height:F0})", sheet.IsOpen && edge(b));
            // Đo pixel thật: sát mép cửa sổ phía đó phải là nền tấm (trắng), giữa vùng còn lại là backdrop (xám).
            var (pr, _, _) = h.Pixel(w, edgePixel);
            var (br, _, _) = h.Pixel(w, outside);
            h.Check($"sheet {side}: tấm vẽ sát mép cửa sổ, ngoài là backdrop (mép {pr}, ngoài {br})", pr > 240 && br < 160);
            if (side is PanelSide.Right or PanelSide.Bottom) h.Save(w, $"overlay-sheet-{side.ToString().ToLowerInvariant()}.png");
            h.Click(w, outside);
            h.Check($"sheet {side}: bấm ra ngoài thì đóng", !sheet.IsOpen && w.Overlays.Count == 0);
        }

        drawer.Open(w);
        h.Frames(w);
        var d = drawer.PanelBounds;
        h.Check($"drawer: mặc định bám đáy, rộng hết ({d.Y:F0}..{d.Bottom:F0})", Near(d.Bottom, H) && Near(d.Width, W) && d.Height < H / 2);
        h.Save(w, "overlay-drawer.png");
        h.Press(w, Key.Escape);
        h.Check("drawer: Escape đóng", !drawer.IsOpen);
        drawer.Side = PanelSide.Right;
        drawer.Open(w);
        h.Frames(w);
        h.Check("drawer: cạnh phải bám mép phải", Near(drawer.PanelBounds.Right, W) && Near(drawer.PanelBounds.Height, H));
        drawer.Close();
    }

    static void Popovers(CheckHarness h)
    {
        Popover pop = null!;
        int changes = 0;
        var w = Host(h, app =>
        {
            pop = app.New(new Popover { TriggerText = "Kích thước", Title = "Kích thước", Description = "Đặt kích thước cho lớp." });
            app.Subscribe<Popover.OpenChanged>(pop, _ => changes++);
        }, w => w.View(pop));

        var trigger = Anchor(w, pop.AnchorName);
        h.Click(w, CheckHarness.Center(trigger));
        var c = pop.Overlay?.ContentBounds ?? default;
        // Căn giữa theo nút nhưng nút sát mép trái nên tấm bị trượt vào trong: vẫn phủ trọn bề ngang nút.
        h.Check($"popover: bấm nút thì mở ngay dưới nút ({c.X:F0},{c.Y:F0})",
            pop.IsOpen && Near(c.Y, trigger.Bottom + 4) && c.X <= trigger.X && c.Right >= trigger.Right && c.X >= 7.5f);
        h.Save(w, "overlay-popover.png");
        h.Click(w, CheckHarness.Center(trigger));
        h.Check("popover: bấm lại nút thì đóng", !pop.IsOpen);
        h.Click(w, CheckHarness.Center(trigger));
        h.Click(w, new Point(h.Size.Width - 30, h.Size.Height - 30));
        h.Check("popover: bấm ra ngoài thì đóng", !pop.IsOpen);
        h.Click(w, CheckHarness.Center(trigger));
        h.Press(w, Key.Escape);
        h.Check("popover: Escape đóng; OpenChanged phát mỗi lần mở/đóng", !pop.IsOpen && changes == 6);
    }
}
