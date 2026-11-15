using GotX;
using GotX.Components;

/// <summary>Kiểm tra Overlays phần focus bàn phím của các lớp modal / popover: focus vào khi mở, Tab bị giữ trong lớp modal, focus trả về nút mở.</summary>
static partial class OverlaysChecks
{
    static void FocusManagement(CheckHarness h)
    {
        Dialog dialog = null!;
        AlertDialog alert = null!;
        Sheet sheet = null!;
        Popover pop = null!;
        int cancelled = 0;
        var opener = new Opener();
        var w = Host(h, app =>
        {
            opener.Dialog = dialog = app.New(new Dialog { Title = "Sửa hồ sơ", Description = "Đổi tên rồi lưu." });
            dialog.Body = static w => w.Div().Class("flex").Child(Ui.Button(w, "Trong thân", ButtonVariant.Outline).Key("dlg-inner").Focusable());
            dialog.Footer = w => Ui.Button(w, "Lưu").Key("dlg-save").Focusable().OnClick(dialog, static (d, _) => d.Close());
            opener.Alert = alert = app.New(new AlertDialog { Title = "Xoá?", Description = "Không hoàn tác được." });
            app.Subscribe<AlertDialog.Cancelled>(alert, _ => cancelled++);
            opener.Sheet = sheet = app.New(new Sheet { Title = "Bộ lọc" });
            pop = app.New(new Popover { TriggerText = "Popover", Title = "Kích thước",
                Body = static w => w.Div().Class("flex").Child(Ui.Button(w, "Áp dụng", ButtonVariant.Outline).Key("pop-apply").Focusable()) });
        }, w =>
        {
            opener.Window = w;
            return w.Div().Class("flex gap-3")
                .Child(Ui.Button(w, "Dialog").Key("open-dialog").Focusable().OnClick(opener, static (o, _) => o.Dialog!.Open(o.Window)))
                .Child(Ui.Button(w, "Alert").Key("open-alert2").Focusable().OnClick(opener, static (o, _) => o.Alert!.Open(o.Window)))
                .Child(Ui.Button(w, "Sheet").Key("open-sheet").Focusable().OnClick(opener, static (o, _) => o.Sheet!.Open(o.Window)))
                .Child(w.View(pop))
                .Child(Ui.Button(w, "Sau").Key("after").Focusable());
        });

        // Dialog: mở bằng phím, focus vào tấm, Tab đi vòng bên trong
        h.Press(w, Key.Tab);
        h.Press(w, Key.Enter);
        h.Check("dialog: Enter trên nút mở → mở và focus chuyển vào tấm", dialog.IsOpen && w.IsFocused(dialog.ContentKey));
        var seen = new List<string>();
        string[] inside = [dialog.ContentKey, "dlg-inner", "dlg-save", dialog.CloseKey];
        for (int i = 0; i < 9; i++)
        {
            h.Press(w, Key.Tab);
            seen.Add(Array.Find(inside, w.IsFocused) ?? "NGOÀI");
        }
        h.Check($"dialog: Tab đi vòng thân → chân → nút × → tấm, không lọt ra trang phía sau ({string.Join(" ", seen.Take(5))})",
            !seen.Contains("NGOÀI") && seen.Take(5).SequenceEqual(["dlg-inner", "dlg-save", dialog.CloseKey, dialog.ContentKey, "dlg-inner"]));
        h.Press(w, Key.Tab, Modifiers.Shift);
        h.Press(w, Key.Tab, Modifiers.Shift);
        h.Check("dialog: Shift+Tab đi ngược và cũng bị giữ trong hộp thoại", Array.Exists(inside, w.IsFocused) && !w.IsFocused("open-dialog") && !w.IsFocused("after"));
        w.FocusKey(dialog.CloseKey);
        h.Frames(w);
        h.Save(w, "overlay-dialog-focus.png");
        h.Press(w, Key.Enter);
        h.Check("dialog: Enter trên nút × đóng, focus trở về nút mở (còn vòng focus)", !dialog.IsOpen && w.IsFocused("open-dialog") && w.FocusVisible);
        h.Press(w, Key.Space);
        h.Press(w, Key.Escape);
        h.Check("dialog: Space mở, Escape đóng, focus lại về nút mở", !dialog.IsOpen && w.IsFocused("open-dialog"));
        h.Click(w, "open-dialog");
        h.Check("dialog: mở bằng chuột thì tấm vẫn nhận focus nhưng không hiện vòng focus", dialog.IsOpen && w.IsFocused(dialog.ContentKey) && !w.FocusVisible);
        h.Press(w, Key.Tab);
        h.Press(w, Key.Tab);
        h.Press(w, Key.Enter);
        h.Check("dialog: Tab tới nút Lưu ở chân rồi Enter → nút chạy và đóng", !dialog.IsOpen);

        // AlertDialog: nút Huỷ nhận focus, chỉ có hai điểm dừng
        w.FocusKey("open-alert2");
        h.Frames(w);
        h.Press(w, Key.Enter);
        h.Check("alert-dialog: mở thì nút Huỷ nhận focus", alert.IsOpen && w.IsFocused(alert.CancelKey));
        h.Press(w, Key.Tab);
        bool onAction = w.IsFocused(alert.ActionKey);
        h.Press(w, Key.Tab);
        h.Check("alert-dialog: Tab chỉ đi giữa Huỷ và nút hành động", onAction && w.IsFocused(alert.CancelKey));
        h.Press(w, Key.Enter);
        h.Check("alert-dialog: Enter trên Huỷ → Cancelled, đóng, focus về nút mở", !alert.IsOpen && cancelled == 1 && w.IsFocused("open-alert2"));

        // Sheet
        w.FocusKey("open-sheet");
        h.Frames(w);
        h.Press(w, Key.Enter);
        h.Check("sheet: mở thì tấm nhận focus", sheet.IsOpen && w.IsFocused(sheet.ContentKey));
        h.Press(w, Key.Tab);
        bool onClose = w.IsFocused(sheet.CloseKey);
        h.Press(w, Key.Tab);
        h.Check("sheet: Tab tới nút × rồi quay vòng về tấm", onClose && w.IsFocused(sheet.ContentKey));
        h.Press(w, Key.Escape);
        h.Check("sheet: Escape đóng, focus về nút mở", !sheet.IsOpen && w.IsFocused("open-sheet"));

        // Popover (không modal)
        h.Press(w, Key.Tab);
        h.Check("popover: nút mở là điểm dừng Tab kế tiếp", w.IsFocused(pop.TriggerKey) && w.FocusVisible);
        h.Press(w, Key.Enter);
        h.Check("popover: Enter mở và focus vào tấm nổi", pop.IsOpen && w.IsFocused(pop.ContentKey));
        h.Press(w, Key.Tab);
        h.Check("popover: Tab đi tới phần tử focus được bên trong", w.IsFocused("pop-apply"));
        h.Press(w, Key.Escape);
        h.Check("popover: Escape đóng, focus về nút mở", !pop.IsOpen && w.IsFocused(pop.TriggerKey));
        h.Press(w, Key.Space);
        h.Click(w, new Point(h.Size.Width - 30, h.Size.Height - 30));
        h.Check("popover: bấm ra ngoài đóng và không kéo focus về nút", !pop.IsOpen && !w.IsFocused(pop.TriggerKey));
    }
}
