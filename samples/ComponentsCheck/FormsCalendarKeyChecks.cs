using GotX;
using GotX.Components;

/// <summary>Kiểm tra bàn phím của Calendar trên <see cref="FormsPickerProbe"/> (hôm nay 15/11/2026, đang hiện tháng 11/2026).</summary>
static class FormsCalendarKeyChecks
{
    public static void Run(CheckHarness h)
    {
        FormsPickerProbe p = null!;
        var w = h.Open(app => p = app.New(new FormsPickerProbe()));
        var cal = p.Calendar;
        string id = "gotx-calendar-" + cal.Id.Value;

        int FocusedCell()
        {
            for (int i = 0; i < 42; i++) if (w.IsFocused($"{id}-{i}")) return i;
            return -1;
        }
        // Ngày của ô đang focus: ô đầu lưới là thứ Hai của tuần chứa mồng 1.
        DateOnly? FocusedDay() => FocusedCell() is >= 0 and var i ? cal.Month.AddDays(-(((int)cal.Month.DayOfWeek + 6) % 7) + i) : null;
        static DateOnly D(int y, int m, int d) => new(y, m, d);

        h.Press(w, Key.Tab);
        h.Press(w, Key.Enter);
        h.Check($"calendar: Tab tới nút tháng trước, Enter lùi một tháng ({cal.Month})", w.IsFocused(id + "-prev") && cal.Month == D(2026, 10, 1));
        h.Press(w, Key.Tab);
        h.Press(w, Key.Space);
        h.Check($"calendar: Tab tới nút tháng sau, Space tiến một tháng ({cal.Month})", w.IsFocused(id + "-next") && cal.Month == D(2026, 11, 1) && p.MonthEvents == 2);

        h.Press(w, Key.Tab);
        h.Check($"calendar: Tab vào lưới thì focus ở hôm nay (chưa chọn ngày nào), không phải ô đầu lưới ({FocusedDay()})",
            FocusedDay() == D(2026, 11, 15) && cal.Selected is null);
        h.Press(w, Key.Right);
        h.Press(w, Key.Down);
        h.Check($"calendar: → một ngày, ↓ một tuần ({FocusedDay()})", FocusedDay() == D(2026, 11, 23));
        h.Press(w, Key.End);
        var end = FocusedDay();
        h.Press(w, Key.Home);
        h.Check($"calendar: End tới Chủ nhật, Home về thứ Hai của tuần ({end}, {FocusedDay()})", end == D(2026, 11, 29) && FocusedDay() == D(2026, 11, 23));
        h.Press(w, Key.Left);
        h.Press(w, Key.Up);
        h.Check($"calendar: ← lùi một ngày, ↑ lùi một tuần ({FocusedDay()})", FocusedDay() == D(2026, 11, 15));
        h.Press(w, Key.Enter);
        h.Check($"calendar: Enter chọn ngày đang focus ({cal.Selected})", cal.Selected == D(2026, 11, 15) && p.DayEvents == 1);

        // Vòng focus của ô ngày tràn 3px sang ô bên trái (ô 14/11, nền trong suốt).
        var box = h.Box(w, "f-calendar");
        int cell = FocusedCell();
        var left = new Point(box.X + 12 + (cell % 7) * 32 - 2, box.Y + 12 + 32 + 8 + 32 + 8 + (cell / 7) * 32 + 16);
        h.Check("calendar: ô ngày đang focus có vòng focus", FormsKeyboardChecks.RingAt(h, w, left));

        h.Press(w, Key.Down);
        h.Press(w, Key.Down);
        h.Press(w, Key.Down);
        h.Check($"calendar: ↓ quá cuối tháng thì lật sang tháng sau, focus theo ngày ({FocusedDay()}, {cal.Month})",
            FocusedDay() == D(2026, 12, 6) && cal.Month == D(2026, 12, 1) && cal.Selected == D(2026, 11, 15));
        h.Press(w, Key.PageDown);
        h.Check($"calendar: PageDown sang tháng sau, giữ ngày ({FocusedDay()})", FocusedDay() == D(2027, 1, 6) && cal.Month == D(2027, 1, 1));
        h.Press(w, Key.PageUp, Modifiers.Shift);
        h.Press(w, Key.PageUp);
        h.Check($"calendar: Shift+PageUp lùi một năm, PageUp lùi một tháng ({FocusedDay()})", FocusedDay() == D(2025, 12, 6) && cal.Month == D(2025, 12, 1));
        h.Press(w, Key.Space);
        h.Check($"calendar: Space chọn ngày đang focus ({cal.Selected})", cal.Selected == D(2025, 12, 6) && p.DayEvents == 2);

        h.Press(w, Key.Tab);
        h.Check("calendar: Tab rời lưới ngày trong một bước", FocusedCell() < 0);
        h.Press(w, Key.Tab, Modifiers.Shift);
        h.Check($"calendar: Shift+Tab quay lại đúng ngày focus lần trước ({FocusedDay()})", FocusedDay() == D(2025, 12, 6));

        // Bấm chuột vào ngày ngoài tháng: chọn, lật tháng, focus đi theo ngày đó (ô vừa bấm giờ là ngày khác) mà không hiện vòng focus.
        // 1/12/2025 là thứ Hai nên ô đầu lưới thuộc tháng; ô cuối (hàng 6, cột CN) là 11/1/2026.
        h.Click(w, new Point(box.X + 12 + 6 * 32 + 16, box.Y + 12 + 32 + 8 + 32 + 8 + 5 * 32 + 16));
        h.Check($"calendar: bấm chuột ngày ngoài tháng thì chọn, lật tháng, focus theo ngày và không hiện vòng focus ({cal.Selected}, {FocusedDay()})",
            cal.Selected == D(2026, 1, 11) && cal.Month == D(2026, 1, 1) && FocusedDay() == D(2026, 1, 11) && !w.FocusVisible);
    }
}
