using GotX;
using GotX.Components;

/// <summary>Kiểm tra hành vi Overlays (phần combobox, command, toast – lọc, bấm chuột, tự đóng theo thời gian).</summary>
static partial class OverlaysChecks
{
    static void Filtering(CheckHarness h)
    {
        Combobox combo = null!;
        Command cmd = null!, palette = null!;
        var cmdPicked = new List<string>();
        var w = Host(h, app =>
        {
            combo = app.New(new Combobox { Items = ["Next.js", "SvelteKit", "Nuxt.js", "Remix", "Astro"], Placeholder = "Chọn framework…" });
            CommandGroup[] groups =
            [
                new("Gợi ý", [new("Lịch", Key: "cmd-cal"), new("Tìm emoji", Key: "cmd-emoji"), new("Máy tính", Disabled: true, Key: "cmd-calc")]),
                new("Cài đặt", [new("Hồ sơ", "Ctrl+P", Key: "cmd-profile"), new("Cài đặt", "Ctrl+S", Key: "cmd-settings")]),
            ];
            cmd = app.New(new Command { Groups = groups, PanelClass = "w-80 border border-border" });
            palette = app.New(new Command { Groups = groups });
            app.Subscribe<Command.ItemSelected>(cmd, e => cmdPicked.Add(e.Item.Text));
        }, w => w.Div().Class("flex items-start gap-8").Child(w.View(combo)).Child(w.View(cmd)));

        // Combobox: mở, focus ô gõ, gõ thì danh sách hẹp lại
        h.Click(w, CheckHarness.Center(Anchor(w, combo.AnchorName)));
        h.Check("combobox: bấm nút thì mở tấm và focus ô gõ", combo.IsOpen && w.Focused == combo.Panel.Input && h.Exists(w, combo.ItemKey(4)));
        h.Type(w, "x");
        h.Check($"combobox: gõ \"x\" thì còn 3 dòng ({combo.Panel.VisibleCount})", combo.Panel.VisibleCount == 3
            && h.Exists(w, combo.ItemKey(0)) && !h.Exists(w, combo.ItemKey(1)) && h.Exists(w, combo.ItemKey(3)) && !h.Exists(w, combo.ItemKey(4)));
        h.Save(w, "overlay-combobox.png");
        h.Type(w, "t");
        h.Check("combobox: gõ tiếp \"xt\" thì còn Next.js, Nuxt.js", combo.Panel.VisibleCount == 2 && !h.Exists(w, combo.ItemKey(3)));
        h.Click(w, combo.ItemKey(2));
        h.Check($"combobox: bấm dòng thì chọn rồi đóng ({combo.Value})", combo.Value == "Nuxt.js" && !combo.IsOpen && w.Overlays.Count == 0);
        h.Click(w, CheckHarness.Center(Anchor(w, combo.AnchorName)));
        h.Check("combobox: mở lại thì chuỗi lọc đã xoá, đủ 5 dòng", combo.IsOpen && combo.Panel.Filter == "" && combo.Panel.VisibleCount == 5);
        h.Type(w, "zzz");
        h.Check("combobox: không khớp gì thì danh sách rỗng", combo.Panel.VisibleCount == 0 && !h.Exists(w, combo.ItemKey(0)));
        h.Press(w, Key.Escape);
        h.Check("combobox: Escape đóng, giá trị giữ nguyên", !combo.IsOpen && combo.Value == "Nuxt.js");

        // Command nhúng thẳng
        w.Focus(cmd.Input);
        h.Type(w, "cài");
        h.Check($"command: gõ \"cài\" thì chỉ còn dòng khớp, nhóm Gợi ý ẩn ({cmd.VisibleCount})",
            cmd.VisibleCount == 1 && h.Exists(w, "cmd-settings") && !h.Exists(w, "cmd-cal") && !h.Exists(w, "cmd-profile"));
        h.Press(w, Key.Enter);
        h.Check("command: Enter chọn dòng khớp đầu tiên", cmdPicked is ["Cài đặt"]);
        cmd.Reset();
        h.Frames(w);
        h.Click(w, "cmd-calc");
        h.Check("command: dòng bị tắt không chọn được", cmdPicked.Count == 1);
        h.Click(w, "cmd-emoji");
        h.Check("command: bấm dòng thì phát ItemSelected", cmdPicked is ["Cài đặt", "Tìm emoji"]);

        // Command dạng hộp thoại
        palette.OpenDialog(w);
        h.Frames(w);
        h.Check("command dialog: mở modal và focus ô gõ", palette.IsDialogOpen && w.Overlays[^1].Options.Modal && w.Focused == palette.Input);
        h.Type(w, "hồ");
        h.Check("command dialog: lọc khi gõ", palette.VisibleCount == 1);
        h.Save(w, "overlay-command-dialog.png");
        h.Press(w, Key.Escape);
        h.Check("command dialog: Escape đóng", !palette.IsDialogOpen && w.Overlays.Count == 0);
    }

    static void Toasts(CheckHarness h)
    {
        Toaster toaster = null!;
        var dismissed = new List<int>();
        var actions = new List<int>();
        var w = Host(h, app =>
        {
            toaster = app.New(new Toaster { AutoDismiss = false });
            app.Subscribe<Toaster.Dismissed>(toaster, e => dismissed.Add(e.Id));
            app.Subscribe<Toaster.ActionClicked>(toaster, e => actions.Add(e.Id));
        }, w => w.Div());
        float W = h.Size.Width, H = h.Size.Height;

        int a = toaster.Show(w, "Đã lưu", "Thay đổi đã được lưu.", ToastVariant.Success);
        int b = toaster.Show(w, new Toast { Title = "Đã xoá thư mục", Description = "3 tệp", ActionText = "Hoàn tác" });
        h.Frames(w);
        var ta = toaster.Toasts[0];
        var tb = toaster.Toasts[1];
        var ba = h.Box(w, ta.Key);
        var bb = h.Box(w, tb.Key);
        h.Check($"toast: hai thông báo chồng ở góc dưới phải, mới nhất sát góc ({bb.Right:F0},{bb.Bottom:F0})",
            toaster.Toasts.Count == 2 && Near(bb.Right, W - 16) && Near(bb.Bottom, H - 16) && ba.Bottom <= bb.Y && Near(ba.Right, bb.Right));
        h.Save(w, "overlay-toast.png");
        h.Click(w, ta.CloseKey);
        h.Check("toast: nút × đóng đúng thông báo đó", toaster.Toasts.Count == 1 && toaster.Toasts[0].Id == b && dismissed is [var d] && d == a);
        h.Check("toast: thông báo còn lại dời xuống góc", Near(h.Box(w, tb.Key).Bottom, H - 16));
        h.Click(w, tb.ActionKey);
        h.Check("toast: nút hành động phát ActionClicked rồi đóng; hết thông báo thì gỡ overlay",
            actions is [var x] && x == b && toaster.Toasts.Count == 0 && w.Overlays.Count == 0);

        // Tự đóng bằng hẹn giờ của App; thời gian trôi theo đồng hồ ảo của harness nên kết quả tất định
        toaster.AutoDismiss = true;
        h.Move(w, new Point(30, 30)); // con trỏ còn nằm ở góc thông báo từ cú bấm trước: dời đi để lần rê vào sau là "vào" thật
        var second = TimeSpan.FromSeconds(1);
        toaster.Show(w, new Toast { Title = "Tự đóng", Duration = second });
        h.Wait(w, 0.5);
        bool shown = toaster.Toasts.Count == 1;
        h.Wait(w, 0.6);
        h.Check("toast: AutoDismiss đóng thông báo sau Duration, không sớm hơn", shown && toaster.Toasts.Count == 0 && w.Overlays.Count == 0);

        var paused = new Toast { Title = "Tạm dừng", Duration = second };
        toaster.Show(w, paused);
        h.Wait(w, 0.5);
        h.Move(w, CheckHarness.Center(h.Box(w, paused.Key)));
        h.Wait(w, 2);
        h.Check("toast: rê chuột lên thì đếm ngược tạm dừng (quá hạn vẫn còn)", toaster.IsPaused && toaster.Toasts.Count == 1 && w.App.PendingTimers == 0);
        h.Move(w, new Point(30, 30));
        h.Wait(w, 0.3);
        bool resumed = !toaster.IsPaused && toaster.Toasts.Count == 1;
        h.Wait(w, 0.3);
        h.Check("toast: chuột rời thì chạy tiếp phần còn lại (~0.5 giây) rồi mới đóng", resumed && toaster.Toasts.Count == 0);

        int early = toaster.Show(w, new Toast { Title = "Đóng sớm", Duration = second });
        toaster.Show(w, new Toast { Title = "Đang tải", Variant = ToastVariant.Loading });
        h.Frames(w);
        int timers = w.App.PendingTimers;
        toaster.Dismiss(early);
        h.Wait(w, 1.5);
        h.Check($"toast: đóng sớm thì huỷ hẹn giờ; thông báo Loading không tự đóng ({timers} → {w.App.PendingTimers})",
            timers == 1 && w.App.PendingTimers == 0 && toaster.Toasts is [{ Variant: ToastVariant.Loading }]);
        w.FocusKey(toaster.Toasts[0].CloseKey);
        h.Frames(w);
        h.Press(w, Key.Enter);
        h.Check("toast: nút × focus được, Enter đóng thông báo", toaster.Toasts.Count == 0 && w.Overlays.Count == 0);
    }
}
