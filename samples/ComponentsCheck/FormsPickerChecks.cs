using GotX;
using GotX.Components;

/// <summary>Kiểm tra lịch, native select và progress.</summary>
static class FormsPickerChecks
{
    // Bố cục lịch: p-3 (12px), tiêu đề h-8, gap-2, hàng thứ h-8, gap-2, rồi lưới 6×7 ô 32px.
    static Point Cell(Bounds cal, int index) =>
        new(cal.X + 12 + (index % 7) * 32 + 16, cal.Y + 12 + 32 + 8 + 32 + 8 + (index / 7) * 32 + 16);

    public static void Run(CheckHarness h)
    {
        FormsPickerProbe p = null!;
        var w = h.Open(app => p = app.New(new FormsPickerProbe()));
        var cal = h.Box(w, "f-calendar");

        // 1/11/2026 là Chủ nhật → ô đầu là thứ Hai 26/10; 15/11 ở ô 20, 16/11 ở ô 21.
        var today = h.Pixel(w, Cell(cal, 20) with { Y = Cell(cal, 20).Y - 12 });
        var plain = h.Pixel(w, Cell(cal, 21) with { Y = Cell(cal, 21).Y - 12 });
        h.Check($"calendar: hôm nay có nền muted khác ô thường ({today} / {plain})", today != plain);

        h.Click(w, new Point(cal.Right - 12 - 16, cal.Y + 12 + 16));
        h.Check($"calendar: nút sau sang tháng 12 ({p.Calendar.Month})",
            p.Calendar.Month == new DateOnly(2026, 12, 1) && p.MonthEvents == 1);

        // 1/12/2026 là thứ Ba → ô đầu là 30/11, ô 17 là 17/12.
        h.Click(w, Cell(cal, 17));
        h.Wait(w, 0.3); // transition-colors của ô ngày
        var sel = h.Pixel(w, Cell(cal, 17) with { Y = Cell(cal, 17).Y - 12 });
        h.Check($"calendar: bấm ô chọn 17/12 và tô primary ({p.Calendar.Selected}, {sel})",
            p.Calendar.Selected == new DateOnly(2026, 12, 17) && p.DayEvents == 1 && sel.R < 80);

        h.Click(w, new Point(cal.X + 12 + 16, cal.Y + 12 + 16));
        h.Click(w, new Point(cal.X + 12 + 16, cal.Y + 12 + 16));
        h.Check($"calendar: nút trước lùi hai tháng ({p.Calendar.Month})", p.Calendar.Month == new DateOnly(2026, 10, 1));

        // 1/10/2026 là thứ Năm → ô 0 là 28/9 (ngày ngoài tháng): chọn và chuyển sang tháng 9.
        h.Click(w, Cell(cal, 0));
        h.Check($"calendar: bấm ngày ngoài tháng thì chọn và chuyển tháng ({p.Calendar.Selected}, {p.Calendar.Month})",
            p.Calendar.Selected == new DateOnly(2026, 9, 28) && p.Calendar.Month == new DateOnly(2026, 9, 1));

        // Native select: mở danh sách, chọn dòng hai (mỗi dòng 28px, đệm 4px như CoreChecks).
        h.Click(w, "f-native");
        h.Check("native-select: bấm thì mở danh sách", p.Fruit.IsOpen && w.Overlays.Count == 1);
        var list = w.Overlays[0].ContentBounds;
        h.Click(w, new Point(list.X + 20, list.Y + 4 + 28 + 14));
        h.Check($"native-select: chọn dòng hai, đóng và phát ValueChanged ({p.Fruit.Value})",
            p.Fruit.Value == "Cam" && !p.Fruit.IsOpen && p.SelectEvents == 1);

        NativeKeys(h, w, p.Fruit, p);

        // Progress 50%: một phần tư đầu là primary (tối), ba phần tư là muted (sáng).
        var bar = h.Box(w, "f-progress");
        var done = h.Pixel(w, new Point(bar.X + bar.Width * 0.25f, bar.Y + bar.Height / 2));
        var rest = h.Pixel(w, new Point(bar.X + bar.Width * 0.75f, bar.Y + bar.Height / 2));
        h.Check($"progress: 50% tô primary nửa trái ({done} / {rest})", done.R < 80 && rest.R > 200);
    }

    /// <summary>Native select bằng bàn phím (thừa hưởng từ Select): Tab tới có vòng focus, phím mở / đi / chọn / đóng, focus về hộp.</summary>
    static void NativeKeys(CheckHarness h, Window w, NativeSelect native, FormsPickerProbe p)
    {
        // Sau cú bấm chuột ở trên focus đang ở hộp nhưng không hiện vòng; Shift+Tab rồi Tab để tới lại bằng bàn phím.
        bool quiet = w.IsFocused(native.TriggerKey) && !w.FocusVisible;
        h.Press(w, Key.Tab, Modifiers.Shift);
        bool left = !w.IsFocused(native.TriggerKey);
        h.Press(w, Key.Tab);
        h.Check("native-select: bấm chuột thì focus không hiện vòng; Tab tới thì hiện vòng focus",
            quiet && left && w.IsFocused(native.TriggerKey) && w.FocusVisible && FormsKeyboardChecks.Ring(h, w, h.Box(w, native.TriggerKey)));
        h.Press(w, Key.Down);
        h.Check("native-select: ↓ mở danh sách, mục đang chọn được tô", native.IsOpen && native.ActiveIndex == 1 && !w.IsFocused(native.TriggerKey));
        h.Press(w, Key.Down);
        h.Press(w, Key.Enter);
        h.Check($"native-select: ↓ rồi Enter chọn mục kế, đóng, focus về hộp ({native.Value})",
            native.Value == "Xoài" && !native.IsOpen && p.SelectEvents == 2 && w.IsFocused(native.TriggerKey) && w.FocusVisible);
        h.Press(w, Key.Enter);
        bool opened = native.IsOpen;
        h.Press(w, Key.Home);
        bool atFirst = native.ActiveIndex == 0;
        h.Press(w, Key.Escape);
        h.Check("native-select: Enter mở, Home tô mục đầu, Escape đóng mà không đổi giá trị",
            opened && atFirst && !native.IsOpen && native.Value == "Xoài" && p.SelectEvents == 2 && w.IsFocused(native.TriggerKey));
        h.Press(w, Key.Space); h.Press(w, Key.Home); h.Press(w, Key.Space);
        h.Check($"native-select: Space mở và Space chọn ({native.Value})", native.Value == "Táo" && !native.IsOpen && p.SelectEvents == 3);
    }
}
